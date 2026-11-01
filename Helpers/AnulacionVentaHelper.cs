using Kirkenta.Data;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Anulación completa de una venta: regresa el stock (kardex), anula los movimientos
    /// financieros asociados (ingreso, cambio, costo y cobros), cancela el envío pendiente y
    /// marca la factura como anulada. El número de factura (y su CAI) queda consumido.
    /// </summary>
    public static class AnulacionVentaHelper
    {
        public static (bool ok, string? error) Anular(ApplicationDbContext context, int ventaId, string motivo, int usuarioId)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                return (false, "Debes indicar el motivo de la anulación.");

            var venta = context.Ventas.FirstOrDefault(v => v.Id == ventaId);
            if (venta == null) return (false, "Venta no encontrada.");
            if (venta.Estado == "Anulada") return (false, "La venta ya está anulada.");

            if (!string.IsNullOrEmpty(venta.LoteMigracion))
                return (false, "Esta venta viene de la migración del sistema anterior (no movió stock ni caja), por eso no se puede anular. Si se importó por error, usa Migración → Deshacer lote.");

            if (context.Devoluciones.Any(d => d.VentaId == ventaId && d.Estado == "Aplicada"))
                return (false, "La venta tiene devoluciones aplicadas; no puede anularse completa.");

            if (context.EntregasBodega.Any(e => e.VentaId == ventaId && !e.Anulada))
                return (false, "La venta tiene comprobantes de entrega en bodega vigentes. Anúlalos primero (o registra una devolución si la mercadería ya salió).");

            var (okVenta, errVenta) = CierreContableHelper.ValidarFecha(context, venta.Fecha);
            if (!okVenta) return (false, $"La venta pertenece a un período cerrado. {errVenta}");

            var (okHoy, errHoy) = CierreContableHelper.ValidarFecha(context, DateTime.Today);
            if (!okHoy) return (false, errHoy);

            var envios = context.Envios.Where(e => e.VentaId == ventaId).ToList();
            if (envios.Any(e => e.Estado == "Entregado" || e.Estado == "EnRuta"))
                return (false, "El envío de esta venta ya salió o fue entregado; no se puede anular.");

            var movimientos = context.MovimientosFinancieros
                .Where(m => m.Estado == "Activo" && m.Origen == "Venta" && m.OrigenId == ventaId)
                .ToList();

            foreach (var m in movimientos)
            {
                var (okMov, errMov) = CierreContableHelper.ValidarFecha(context, m.Fecha);
                if (!okMov) return (false, $"El movimiento {m.Numero} pertenece a un período cerrado. {errMov}");
            }

            var abrimosTx = context.Database.CurrentTransaction == null;
            var tx = abrimosTx ? context.Database.BeginTransaction() : null;

            try
            {
                // 0) Reclamo atómico: si dos personas anulan la misma venta a la vez, solo una continúa
                // (antes las dos devolvían el stock y revertían el dinero).
                if (context.Database.IsRelational())
                {
                    var filas = context.Ventas
                        .Where(v => v.Id == ventaId && v.Estado != "Anulada")
                        .ExecuteUpdate(u => u.SetProperty(v => v.Estado, "Anulada"));
                    if (filas == 0)
                    {
                        tx?.Rollback();
                        return (false, "La venta ya está anulada.");
                    }
                    context.Entry(venta).Reload();

                    // Los movimientos se vuelven a leer ya con la venta bloqueada
                    movimientos = context.MovimientosFinancieros
                        .Where(m => m.Estado == "Activo" && m.Origen == "Venta" && m.OrigenId == ventaId)
                        .ToList();
                }

                // 1) Inventario de vuelta
                var detalles = context.DetalleVentas.Where(d => d.VentaId == ventaId).ToList();
                foreach (var d in detalles)
                {
                    var prod = context.Productos.FirstOrDefault(p => p.Id == d.ProductoId);
                    if (prod == null) continue;
                    InventarioHelper.Registrar(context, prod, d.Cantidad, "AnulacionVenta",
                        venta.Numero, venta.Id, usuarioId, d.CostoUnitarioSnapshot,
                        $"Anulación de venta {venta.Numero}");
                }

                // 2) Finanzas: anular todos los movimientos de la venta (ingreso, cambio, costo, cobros)
                foreach (var m in movimientos)
                {
                    m.Estado = "Anulado";
                    m.MotivoAnulacion = $"Anulación de venta {venta.Numero}: {motivo}";
                    m.FechaAnulacion = DateTime.Now;
                    m.UsuarioAnuloId = usuarioId;
                    SaldoHelper.Revertir(context, m);
                }

                // 3) Envío pendiente
                foreach (var e in envios.Where(e => e.Estado != "Cancelado"))
                    e.Estado = "Cancelado";

                // 4) Factura
                var facturas = context.Facturas.Where(f => f.VentaId == ventaId && f.Estado != "Anulada").ToList();
                foreach (var f in facturas)
                {
                    f.Estado = "Anulada";
                    f.Saldo = 0;
                    f.Notas = string.IsNullOrWhiteSpace(f.Notas)
                        ? $"[Anulada {DateTime.Now:dd/MM/yyyy}] {motivo}"
                        : $"{f.Notas}\n[Anulada {DateTime.Now:dd/MM/yyyy}] {motivo}";
                }

                // 5) Venta
                // El saldo a favor que se usó en esta venta regresa al cliente
                if (venta.SaldoFavorAplicado > 0 && venta.ClienteId.HasValue)
                    SaldoFavorHelper.Acreditar(context, venta.ClienteId.Value, venta.SaldoFavorAplicado,
                        $"Anulación de venta {venta.Numero}", null, venta.Id, usuarioId, "Reverso");

                venta.Estado = "Anulada";
                venta.SaldoPendiente = 0;
                venta.Notas = string.IsNullOrWhiteSpace(venta.Notas)
                    ? $"[Anulada {DateTime.Now:dd/MM/yyyy HH:mm}] {motivo}"
                    : $"{venta.Notas}\n[Anulada {DateTime.Now:dd/MM/yyyy HH:mm}] {motivo}";

                context.SaveChanges();
                tx?.Commit();
                return (true, null);
            }
            catch (Exception ex)
            {
                tx?.Rollback();
                Console.WriteLine($"[AnulacionVenta] Error: {ex.Message}");
                return (false, $"Error al anular la venta: {ex.Message}");
            }
            finally
            {
                tx?.Dispose();
            }
        }
    }
}
