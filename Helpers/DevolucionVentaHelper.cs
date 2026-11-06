using Kirkenta.Data;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Devoluciones parciales/totales de una venta:
    ///  - Reingresa la mercadería al inventario (kardex) si corresponde.
    ///  - Reduce el saldo si la venta fue a crédito; el resto se reembolsa (egreso real en caja).
    ///  - Revierte el costo de ventas (COGS) de lo que regresó a inventario.
    /// </summary>
    public static class DevolucionVentaHelper
    {
        public class ItemSolicitud
        {
            public int DetalleVentaId { get; set; }
            public decimal Cantidad { get; set; }
        }

        public class Solicitud
        {
            public int VentaId { get; set; }
            public List<ItemSolicitud> Items { get; set; } = new();
            public string? Motivo { get; set; }
            public bool ReingresaInventario { get; set; } = true;
            public string? FormaReembolso { get; set; }
            public int UsuarioId { get; set; }
        }

        /// <summary>Cantidad ya devuelta por línea de venta (solo devoluciones aplicadas).</summary>
        public static Dictionary<int, decimal> YaDevueltoPorDetalle(ApplicationDbContext context, int ventaId)
        {
            return (from dd in context.DetalleDevoluciones
                    join d in context.Devoluciones on dd.DevolucionId equals d.Id
                    where d.VentaId == ventaId && d.Estado == "Aplicada" && dd.DetalleVentaId != null
                    select new { dd.DetalleVentaId, dd.Cantidad })
                   .AsEnumerable()
                   .GroupBy(x => x.DetalleVentaId!.Value)
                   .ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));
        }

        public record LineaDevuelta(int ProductoId, decimal Cantidad, decimal Base, decimal Costo);

        /// <summary>
        /// Lo devuelto en un período, medido igual que las ventas: sin ISV. El precio guardado en la devolución
        /// lleva ISV, y restarlo de ventas sin ISV dejaba la utilidad más baja de lo real. El costo solo cuenta
        /// si la mercadería regresó al inventario.
        /// </summary>
        public static List<LineaDevuelta> DevueltoSinIsv(ApplicationDbContext context, DateTime desde, DateTime hastaExcl)
        {
            var filas = (from dd in context.DetalleDevoluciones.AsNoTracking()
                         join dev in context.Devoluciones.AsNoTracking() on dd.DevolucionId equals dev.Id
                         where dev.Fecha >= desde && dev.Fecha < hastaExcl && dev.Estado == "Aplicada"
                         select new { dd.ProductoId, dd.Cantidad, dd.PrecioUnitario, dd.CostoUnitario, dd.DetalleVentaId, dev.ReingresaInventario }).ToList();
            var ids = filas.Where(f => f.DetalleVentaId.HasValue).Select(f => f.DetalleVentaId!.Value).Distinct().ToList();
            var lineas = context.DetalleVentas.AsNoTracking().Where(d => ids.Contains(d.Id)).ToDictionary(d => d.Id);
            return filas.Select(f =>
            {
                var unitario = f.DetalleVentaId.HasValue && lineas.TryGetValue(f.DetalleVentaId.Value, out var l) && l.Cantidad > 0
                    ? l.Subtotal / l.Cantidad : f.PrecioUnitario;
                return new LineaDevuelta(f.ProductoId, f.Cantidad, f.Cantidad * unitario, f.ReingresaInventario ? f.Cantidad * f.CostoUnitario : 0m);
            }).ToList();
        }

        /// <summary>
        /// Factor que prorratea el descuento general de la venta entre sus líneas
        /// (el envío no se devuelve). Siempre entre 0 y 1.
        /// </summary>
        public static decimal FactorProrrateo(Venta venta, List<DetalleVenta> detalles)
        {
            var sumaLineas = detalles.Sum(d => d.Total);
            if (sumaLineas <= 0) return 1m;
            var totalProductos = venta.Total - venta.MontoEnvio;
            var f = totalProductos / sumaLineas;
            if (f > 1m) f = 1m;
            if (f < 0m) f = 0m;
            return f;
        }

        public static decimal PrecioUnitarioDevolucion(DetalleVenta d, decimal factor)
        {
            if (d.Cantidad <= 0) return 0;
            return Math.Round(d.Total / d.Cantidad * factor, 4);
        }

        public static (Devolucion? devolucion, string? error) Crear(ApplicationDbContext context, Solicitud s)
        {
            var venta = context.Ventas.FirstOrDefault(v => v.Id == s.VentaId);
            if (venta == null) return (null, "Venta no encontrada.");
            if (venta.Estado == "Anulada") return (null, "No se puede devolver una venta anulada.");
            if (!string.IsNullOrEmpty(venta.LoteMigracion))
                return (null, "Esta venta viene de la migración del sistema anterior (no movió stock ni caja): no admite devolución automática. Registra un ajuste de inventario si hace falta.");

            var items = s.Items.Where(i => i.Cantidad > 0).ToList();
            if (items.Count == 0) return (null, "Indica la cantidad a devolver de al menos un producto.");

            if (string.IsNullOrWhiteSpace(s.Motivo)) return (null, "El motivo de la devolución es obligatorio.");

            // Cambio / saldo a favor: el crédito queda a nombre del cliente, así que debe estar identificado
            var aSaldoFavor = string.Equals(s.FormaReembolso?.Trim(), SaldoFavorHelper.NombreMetodo, StringComparison.OrdinalIgnoreCase);
            if (aSaldoFavor && !venta.ClienteId.HasValue)
                return (null, "El saldo a favor necesita un cliente identificado. Esta venta fue a consumidor final: reembolsa en efectivo o tarjeta.");

            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(context, DateTime.Today);
            if (!periodoOk) return (null, periodoError);

            var detalles = context.DetalleVentas.Where(d => d.VentaId == venta.Id).ToList();
            var yaDevuelto = YaDevueltoPorDetalle(context, venta.Id);
            var factor = FactorProrrateo(venta, detalles);

            var lineas = new List<(DetalleVenta det, decimal cantidad, decimal precio)>();
            foreach (var it in items)
            {
                var det = detalles.FirstOrDefault(d => d.Id == it.DetalleVentaId);
                if (det == null) return (null, "Una de las líneas no pertenece a esta venta.");

                var disponible = det.Cantidad - (yaDevuelto.TryGetValue(det.Id, out var yd) ? yd : 0);
                if (it.Cantidad > disponible)
                    return (null, $"No puedes devolver {it.Cantidad:0.##} de la línea #{det.Id}; solo quedan {disponible:0.##} por devolver.");

                lineas.Add((det, it.Cantidad, PrecioUnitarioDevolucion(det, factor)));
            }

            decimal total = Math.Round(lineas.Sum(l => l.precio * l.cantidad), 2, MidpointRounding.AwayFromZero);
            if (total <= 0) return (null, "El total de la devolución debe ser mayor a cero.");

            var devueltoPrevio = context.Devoluciones
                .Where(d => d.VentaId == venta.Id && d.Estado == "Aplicada")
                .Sum(d => (decimal?)d.Total) ?? 0m;

            // Si con esta devolución ya regresa TODO lo vendido, el total es exactamente lo que falta por devolver:
            // tres devoluciones de 3.33 sobre una venta de 10.00 dejaban un centavo sin devolver al cliente.
            var porLinea = items.GroupBy(i => i.DetalleVentaId).ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));
            var todoRegresa = detalles.All(d =>
                d.Cantidad - (yaDevuelto.TryGetValue(d.Id, out var yd2) ? yd2 : 0) - porLinea.GetValueOrDefault(d.Id) <= 0);
            if (todoRegresa && venta.MontoEnvio == 0)
            {
                var resto = Math.Round(venta.Total - devueltoPrevio, 2, MidpointRounding.AwayFromZero);
                if (resto > 0 && Math.Abs(resto - total) <= 0.05m) total = resto;
            }
            if (devueltoPrevio + total > (venta.Total - venta.MontoEnvio) + 0.05m)
                return (null, "El total devuelto supera el valor de los productos de la venta.");

            var factura = context.Facturas.FirstOrDefault(f => f.VentaId == venta.Id && f.Estado != "Anulada");

            var abrimosTx = context.Database.CurrentTransaction == null;
            var tx = abrimosTx ? context.Database.BeginTransaction() : null;

            try
            {
                // Bloqueo de la venta y revalidacion: dos devoluciones simultaneas de la misma venta
                // ya no pueden pasar ambas el chequeo de cantidad disponible.
                if (context.Database.IsRelational())
                {
                    context.Ventas.Where(v => v.Id == venta.Id)
                        .ExecuteUpdate(u => u.SetProperty(v => v.Estado, v => v.Estado));

                    var yaDevuelto2 = YaDevueltoPorDetalle(context, venta.Id);
                    foreach (var l in lineas)
                    {
                        var disp = l.det.Cantidad - (yaDevuelto2.TryGetValue(l.det.Id, out var y2) ? y2 : 0);
                        if (l.cantidad > disp)
                        {
                            tx?.Rollback();
                            return (null, $"Otra devolución de esta venta se registró al mismo tiempo; de la línea #{l.det.Id} solo quedan {disp:0.##} por devolver.");
                        }
                    }
                    var devueltoPrevio2 = context.Devoluciones
                        .Where(d => d.VentaId == venta.Id && d.Estado == "Aplicada")
                        .Sum(d => (decimal?)d.Total) ?? 0m;
                    if (devueltoPrevio2 + total > (venta.Total - venta.MontoEnvio) + 0.05m)
                    {
                        tx?.Rollback();
                        return (null, "El total devuelto supera el valor de los productos de la venta.");
                    }
                }

                // Nota de crédito fiscal (CAI y rango propios) si está configurada; si no, serie interna
                var fiscalNc = NotaCreditoFiscalHelper.Reservar(context);
                if (fiscalNc.Activa && !fiscalNc.Ok)
                {
                    tx?.Rollback();
                    return (null, fiscalNc.Error ?? "No se pudo asignar el número fiscal de la nota de crédito.");
                }
                var numero = fiscalNc.Activa
                    ? fiscalNc.Numero!
                    : NumeroDocumentoHelper.GenerarSiguiente(context, "Devolucion");

                // ¿Cuánto del total reduce saldo a crédito y cuánto se reembolsa?
                decimal aplicadoSaldo = 0;
                if (venta.EsCredito && venta.SaldoPendiente > 0)
                    aplicadoSaldo = Math.Min(total, venta.SaldoPendiente);
                decimal reembolso = total - aplicadoSaldo;

                // Si la venta se pagó en parte con saldo a favor, esa parte vuelve al saldo a favor del cliente
                // y NO sale en efectivo de la caja (la caja nunca la recibió).
                decimal haciaSaldoFavor = aSaldoFavor ? reembolso : 0m;
                if (!aSaldoFavor && reembolso > 0 && !venta.EsCredito && venta.ClienteId.HasValue && venta.SaldoFavorAplicado > 0)
                {
                    var efectivoPagado = Math.Max(0m, venta.Total - venta.SaldoFavorAplicado);
                    var yaReembolsado = context.Devoluciones
                        .Where(d => d.VentaId == venta.Id && d.Estado == "Aplicada")
                        .Sum(d => (decimal?)d.MontoReembolsado) ?? 0m;
                    var maxEfectivo = Math.Max(0m, efectivoPagado - yaReembolsado);
                    if (reembolso > maxEfectivo)
                        haciaSaldoFavor = Math.Round(reembolso - maxEfectivo, 2, MidpointRounding.AwayFromZero);
                }
                decimal reembolsoEfectivo = reembolso - haciaSaldoFavor;

                var dev = new Devolucion
                {
                    Numero = numero,
                    VentaId = venta.Id,
                    FacturaId = factura?.Id,
                    ClienteId = venta.ClienteId ?? ClienteGenericoHelper.Id(context),
                    Fecha = DateTime.Now,
                    Motivo = s.Motivo,
                    Total = total,
                    Estado = "Aplicada",
                    UsuarioCreoId = s.UsuarioId,
                    ReingresaInventario = s.ReingresaInventario,
                    MontoAplicadoSaldo = aplicadoSaldo,
                    MontoReembolsado = reembolsoEfectivo,
                    MontoSaldoFavor = haciaSaldoFavor,
                    FormaReembolso = reembolsoEfectivo > 0 ? (s.FormaReembolso ?? "Efectivo")
                                   : haciaSaldoFavor > 0 ? SaldoFavorHelper.NombreMetodo : null,
                    CAI = fiscalNc.Activa ? fiscalNc.CAI : null,
                    RangoInicial = fiscalNc.Activa ? fiscalNc.RangoInicial : null,
                    RangoFinal = fiscalNc.Activa ? fiscalNc.RangoFinal : null,
                    FechaLimiteEmision = fiscalNc.Activa ? fiscalNc.FechaLimiteEmision : null
                };
                context.Devoluciones.Add(dev);
                context.SaveChanges();

                // Lo que no se reembolsa ni baja un saldo a crédito queda como saldo a favor del cliente:
                // contablemente es una deuda de la empresa con el cliente (pasivo) que se irá reduciendo
                // cuando el cliente lo use en otra compra. La venta sí baja (Devoluciones en ventas).
                if (haciaSaldoFavor > 0)
                {
                    SaldoFavorHelper.Acreditar(context, venta.ClienteId!.Value, haciaSaldoFavor,
                        $"Devolución {numero} de la venta {venta.Numero}", dev.Id, venta.Id, s.UsuarioId);

                    var (movSaldo, errSaldo) = MovimientoAutomaticoHelper.RegistrarEgresoDevolucion(
                        context, dev.Id, numero, haciaSaldoFavor, s.UsuarioId, SaldoFavorHelper.NombreMetodo,
                        MovimientoAutomaticoHelper.ResolverCuentaCajaActiva(context), soloContable: true);
                    if (movSaldo == null)
                    {
                        tx?.Rollback();
                        return (null, $"No se pudo registrar la devolución a saldo a favor en Finanzas. {errSaldo}");
                    }
                }

                decimal costoRevertido = 0;
                foreach (var (det, cantidad, precio) in lineas)
                {
                    context.DetalleDevoluciones.Add(new DetalleDevolucion
                    {
                        DevolucionId = dev.Id,
                        ProductoId = det.ProductoId,
                        DetalleVentaId = det.Id,
                        Cantidad = cantidad,
                        PrecioUnitario = precio,
                        Total = Math.Round(precio * cantidad, 2, MidpointRounding.AwayFromZero),
                        CostoUnitario = det.CostoUnitarioSnapshot
                    });

                    if (s.ReingresaInventario)
                    {
                        var prod = context.Productos.FirstOrDefault(p => p.Id == det.ProductoId);
                        if (prod != null)
                        {
                            InventarioHelper.Registrar(context, prod, cantidad, "Devolucion",
                                numero, dev.Id, s.UsuarioId, det.CostoUnitarioSnapshot,
                                $"Devolución de venta {venta.Numero}");
                            costoRevertido += det.CostoUnitarioSnapshot * cantidad;
                        }
                    }
                }
                dev.CostoRevertido = Math.Round(costoRevertido, 2, MidpointRounding.AwayFromZero);

                // ===== Saldo a crédito =====
                if (aplicadoSaldo > 0)
                {
                    venta.SaldoPendiente -= aplicadoSaldo;
                    if (venta.SaldoPendiente < 0) venta.SaldoPendiente = 0;

                    if (factura != null)
                    {
                        factura.Saldo -= aplicadoSaldo;
                        if (factura.Saldo < 0) factura.Saldo = 0;
                        factura.Estado = factura.Saldo == 0 ? "Pagada" : "PagadaParcial";
                    }
                }

                context.SaveChanges();

                // ===== Reembolso real =====
                if (reembolsoEfectivo > 0)
                {
                    var cuentaId = MovimientoAutomaticoHelper.ResolverCuentaCajaActiva(context);
                    var (mov, err) = MovimientoAutomaticoHelper.RegistrarEgresoDevolucion(
                        context, dev.Id, numero, reembolsoEfectivo, s.UsuarioId, s.FormaReembolso ?? "Efectivo", cuentaId);
                    if (mov == null)
                    {
                        tx?.Rollback();
                        return (null, $"No se pudo registrar el reembolso en Finanzas. {err}");
                    }
                    dev.MovimientoReembolsoId = mov.Id;
                }

                // ===== Reversa del costo de ventas =====
                if (dev.CostoRevertido > 0 && venta.CostoRegistrado)
                {
                    var (movCosto, errCosto) = MovimientoAutomaticoHelper.RegistrarReversionCostoVenta(
                        context, dev.Id, numero, dev.CostoRevertido, s.UsuarioId,
                        MovimientoAutomaticoHelper.ResolverCuentaCajaActiva(context));
                    if (movCosto == null)
                    {
                        tx?.Rollback();
                        return (null, $"No se pudo revertir el costo de ventas. {errCosto}");
                    }
                    dev.MovimientoCostoId = movCosto.Id;
                }

                context.SaveChanges();
                tx?.Commit();
                return (dev, null);
            }
            catch (Exception ex)
            {
                tx?.Rollback();
                Console.WriteLine($"[Devolucion] Error: {ex.Message}");
                return (null, $"Error al registrar la devolución: {ex.Message}");
            }
            finally
            {
                tx?.Dispose();
            }
        }
    }
}
