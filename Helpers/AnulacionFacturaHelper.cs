using Kirkenta.Data;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Anulación de una factura que NO viene de una venta (factura de cotización o de pedido):
    /// regresa el stock (kardex), anula los movimientos financieros (pagos y costo de ventas),
    /// reabre la cotización / el pedido de origen y marca la factura como anulada.
    /// El número fiscal queda consumido. Las facturas de una venta se anulan desde la venta.
    /// </summary>
    public static class AnulacionFacturaHelper
    {
        public static (bool ok, string? error) Anular(ApplicationDbContext context, int facturaId, string motivo, int usuarioId)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                return (false, "Debes indicar el motivo de la anulación.");

            var factura = context.Facturas.FirstOrDefault(f => f.Id == facturaId);
            if (factura == null) return (false, "Factura no encontrada.");
            if (factura.Estado == "Anulada") return (false, "La factura ya está anulada.");
            if (factura.VentaId.HasValue)
                return (false, "Esta factura pertenece a una venta; anula la venta desde su detalle.");

            var (okFac, errFac) = CierreContableHelper.ValidarFecha(context, factura.Fecha);
            if (!okFac) return (false, $"La factura pertenece a un período cerrado. {errFac}");

            var (okHoy, errHoy) = CierreContableHelper.ValidarFecha(context, DateTime.Today);
            if (!okHoy) return (false, errHoy);

            var movimientos = context.MovimientosFinancieros
                .Where(m => m.Estado == "Activo" && m.Origen == "Factura" && m.OrigenId == facturaId)
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
                // 1) Inventario de vuelta
                var detalles = context.DetalleFacturas.Where(d => d.FacturaId == facturaId).ToList();
                foreach (var d in detalles)
                {
                    var prod = context.Productos.FirstOrDefault(p => p.Id == d.ProductoId);
                    if (prod == null) continue;
                    InventarioHelper.Registrar(context, prod, d.Cantidad, "AnulacionFactura",
                        factura.Numero, factura.Id, usuarioId, prod.PrecioCompra,
                        $"Anulación de factura {factura.Numero}");
                }

                // 2) Finanzas: pagos cobrados y costo de ventas
                foreach (var m in movimientos)
                {
                    m.Estado = "Anulado";
                    m.MotivoAnulacion = $"Anulación de factura {factura.Numero}: {motivo}";
                    m.FechaAnulacion = DateTime.Now;
                    m.UsuarioAnuloId = usuarioId;
                    SaldoHelper.Revertir(context, m);
                }

                // 3) Reabrir cotización / pedido de origen
                foreach (var cot in context.Cotizaciones.Where(c => c.FacturaId == facturaId))
                {
                    cot.Estado = "Aceptada";
                    cot.FacturaId = null;
                    cot.FechaConversion = null;
                }
                foreach (var ped in context.Pedidos.Where(p => p.FacturaId == facturaId))
                {
                    ped.FacturaId = null;
                }

                // 4) Factura
                factura.Estado = "Anulada";
                factura.Saldo = 0;
                factura.Notas = string.IsNullOrWhiteSpace(factura.Notas)
                    ? $"[Anulada {DateTime.Now:dd/MM/yyyy HH:mm}] {motivo}"
                    : $"{factura.Notas}\n[Anulada {DateTime.Now:dd/MM/yyyy HH:mm}] {motivo}";

                context.SaveChanges();
                tx?.Commit();
                return (true, null);
            }
            catch (Exception ex)
            {
                tx?.Rollback();
                Console.WriteLine($"[AnulacionFactura] Error: {ex.Message}");
                return (false, $"Error al anular la factura: {ex.Message}");
            }
            finally
            {
                tx?.Dispose();
            }
        }
    }
}
