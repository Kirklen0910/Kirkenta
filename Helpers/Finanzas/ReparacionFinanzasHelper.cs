using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Correcciones únicas e idempotentes sobre datos de Finanzas generados
    /// antes del fix del 23/10/2026. Se ejecuta al arrancar (Program.cs).
    ///
    /// 1) COGS: los movimientos de costo de ventas ya descontaron su monto del
    ///    SaldoActual de la caja. Se les devuelve ese monto y se marcan con
    ///    SaldoHelper.MarcaSoloContable para no procesarlos otra vez.
    ///    (Los nuevos COGS nacen ya marcados.)
    ///
    /// 2) Ventas a crédito que se facturaron: se había creado un ingreso en la
    ///    cuenta Cuentas por Cobrar y la venta quedó "registrada", bloqueando el
    ///    cobro. Se anula ese ingreso y se reabre la venta para poder cobrarla.
    /// </summary>
    public static class ReparacionFinanzasHelper
    {
        public static (int cogsCorregidos, int creditosReabiertos) Ejecutar(ApplicationDbContext context)
        {
            int cogs = CorregirSaldosCogs(context);
            int creditos = ReabrirCreditosFacturados(context);
            try
            {
                var facturasCorregidas = CorregirDescuentoFacturas(context);
                if (facturasCorregidas > 0)
                    Console.WriteLine($"[Reparacion] Descuento: {facturasCorregidas} facturas de ventas sin el descuento de sus líneas.");
            }
            catch (Exception ex)
            {
                context.ChangeTracker.Clear();
                Console.WriteLine($"[Reparacion] Descuento de facturas: {ex.Message}");
            }
            try
            {
                var (ingresos, devoluciones) = CorregirSaldoFavor(context);
                if (ingresos + devoluciones > 0)
                    Console.WriteLine($"[Reparacion] Saldo a favor: {ingresos} ingresos marcados como no efectivo, {devoluciones} devoluciones asentadas.");
            }
            catch (Exception ex)
            {
                context.ChangeTracker.Clear();
                Console.WriteLine($"[Reparacion] Saldo a favor: {ex.Message}");
            }
            return (cogs, creditos);
        }

        /// <summary>
        /// Las facturas emitidas desde una venta del POS copiaban solo el descuento general en monto y se perdía el
        /// descuento por línea: la factura mostraba Subtotal + ISV sin descuento aunque el total sí lo tenía.
        /// Se iguala el descuento de la factura al de su venta (cabecera + líneas). Idempotente.
        /// </summary>
        private static int CorregirDescuentoFacturas(ApplicationDbContext context)
        {
            var desc = context.DetalleVentas
                .GroupBy(d => d.VentaId)
                .Select(g => new { VentaId = g.Key, Total = g.Sum(x => x.Descuento) })
                .Where(x => x.Total > 0)
                .ToList()
                .ToDictionary(x => x.VentaId, x => x.Total);
            if (desc.Count == 0) return 0;

            var ids = desc.Keys.ToList();
            var facturas = context.Facturas.Where(f => f.VentaId != null && ids.Contains(f.VentaId.Value)).ToList();
            var cabeceras = context.Ventas.Where(v => ids.Contains(v.Id)).ToDictionary(v => v.Id, v => v.Descuento);
            int n = 0;
            foreach (var f in facturas)
            {
                var esperado = Math.Round(desc[f.VentaId!.Value] + cabeceras.GetValueOrDefault(f.VentaId.Value), 2);
                if (f.Descuento == esperado) continue;
                f.Descuento = esperado;
                n++;
            }
            if (n > 0) context.SaveChanges();
            return n;
        }

        /// <summary>
        /// 3) Saldo a favor de clientes:
        ///    - Las ventas pagadas con saldo a favor sumaban ese monto a la caja aunque no entró dinero:
        ///      se les devuelve el monto a la caja y se marcan como solo contables.
        ///    - Las devoluciones acreditadas a saldo a favor no tenían movimiento (la venta no bajaba):
        ///      se les crea el movimiento contable (devolución en ventas, sin mover caja).
        /// </summary>
        private static (int ingresos, int devoluciones) CorregirSaldoFavor(ApplicationDbContext context)
        {
            var nombre = Kirkenta.Helpers.SaldoFavorHelper.NombreMetodo;

            var ingresos = context.MovimientosFinancieros
                .Where(m => m.Estado == "Activo" && m.Tipo == "Ingreso" && m.FormaPago == nombre
                         && m.Origen == "Venta"
                         && (m.Notas == null || !m.Notas.Contains(SaldoHelper.MarcaSoloContable)))
                .ToList();
            foreach (var mov in ingresos)
            {
                var cuenta = context.CuentasFinancieras.FirstOrDefault(c => c.Id == mov.CuentaId);
                if (cuenta != null) cuenta.SaldoActual -= mov.Monto;
                mov.Notas = string.IsNullOrWhiteSpace(mov.Notas)
                    ? SaldoHelper.MarcaSoloContable
                    : $"{mov.Notas} {SaldoHelper.MarcaSoloContable}";
            }
            if (ingresos.Count > 0) context.SaveChanges();

            int devoluciones = 0;
            var devs = context.Devoluciones
                .Where(d => d.Estado == "Aplicada" && d.MontoSaldoFavor > 0)
                .ToList();
            foreach (var d in devs)
            {
                bool existe = context.MovimientosFinancieros.Any(m => m.Origen == "Devolucion" && m.OrigenId == d.Id
                                                                  && m.FormaPago == nombre);
                if (existe) continue;
                var (mov, _) = MovimientoAutomaticoHelper.RegistrarEgresoDevolucion(
                    context, d.Id, d.Numero, d.MontoSaldoFavor, d.UsuarioCreoId ?? 1, nombre,
                    MovimientoAutomaticoHelper.ResolverCuentaCajaActiva(context), soloContable: true);
                if (mov == null) continue;
                mov.Fecha = d.Fecha;
                context.SaveChanges();
                devoluciones++;
            }
            return (ingresos.Count, devoluciones);
        }

        private static int CorregirSaldosCogs(ApplicationDbContext context)
        {
            var categoriasCogsIds = context.CategoriasFinancieras
                .Where(c => c.PlanCuentaId != null
                         && context.PlanCuentas.Any(p => p.Id == c.PlanCuentaId && p.Codigo == "5.1.01"))
                .Select(c => c.Id)
                .ToList();

            if (categoriasCogsIds.Count == 0) return 0;

            var pendientes = context.MovimientosFinancieros
                .Where(m => m.Estado == "Activo"
                         && m.Tipo == "Egreso"
                         && m.Origen == "Venta"
                         && m.EsAutomatico
                         && m.CategoriaId != null
                         && categoriasCogsIds.Contains(m.CategoriaId.Value)
                         && (m.Notas == null || !m.Notas.Contains(SaldoHelper.MarcaSoloContable)))
                .ToList();

            if (pendientes.Count == 0) return 0;

            foreach (var mov in pendientes)
            {
                var cuenta = context.CuentasFinancieras.FirstOrDefault(c => c.Id == mov.CuentaId);
                if (cuenta != null)
                {
                    cuenta.SaldoActual += mov.Monto;
                }

                mov.Notas = string.IsNullOrWhiteSpace(mov.Notas)
                    ? SaldoHelper.MarcaSoloContable
                    : $"{mov.Notas} {SaldoHelper.MarcaSoloContable}";
            }

            context.SaveChanges();
            return pendientes.Count;
        }

        private static int ReabrirCreditosFacturados(ApplicationDbContext context)
        {
            var cuentasCxcIds = context.CuentasFinancieras
                .Where(c => c.Subtipo == "CuentasPorCobrar")
                .Select(c => c.Id)
                .ToList();

            if (cuentasCxcIds.Count == 0) return 0;

            var ventas = context.Ventas
                .Where(v => v.EsCredito
                         && v.MovimientoRegistrado
                         && v.SaldoPendiente > 0
                         && v.MovimientoIngresoId != null)
                .ToList();

            int reabiertas = 0;

            foreach (var venta in ventas)
            {
                var mov = context.MovimientosFinancieros
                    .FirstOrDefault(m => m.Id == venta.MovimientoIngresoId);

                if (mov == null || mov.Estado != "Activo") continue;
                if (!cuentasCxcIds.Contains(mov.CuentaId)) continue;

                mov.Estado = "Anulado";
                mov.MotivoAnulacion = "Corrección automática: el ingreso de una venta a crédito se registra al cobrar, no al facturar.";
                mov.FechaAnulacion = DateTime.Now;
                SaldoHelper.Revertir(context, mov);

                venta.MovimientoRegistrado = false;
                venta.FechaMovimiento = null;
                venta.MovimientoIngresoId = null;

                reabiertas++;
            }

            if (reabiertas > 0) context.SaveChanges();
            return reabiertas;
        }
    }
}