using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Ajustes de devengado para los reportes contables.
    ///
    /// Los movimientos financieros registran solo dinero real (cobros y pagos). Pero contablemente una venta
    /// a crédito es ingreso (y paga ISV) el día que se factura, no el día que se cobra, y una compra recibida
    /// sin pagar ya es inventario, ya es deuda con el proveedor y ya da crédito fiscal.
    /// Este helper calcula, sin tocar los movimientos:
    ///   - el ingreso neto devengado en cada año (ventas a crédito y facturas sin venta, sin ISV ni envío),
    ///     menos lo que ya entró como cobro;
    ///   - cuentas por cobrar (saldo pendiente de esos documentos), con su ISV y envío pendientes;
    ///   - cuentas por pagar (compras recibidas sin pagar), con su inventario y su crédito fiscal.
    /// </summary>
    public static class DevengoHelper
    {
        public class Devengo
        {
            /// <summary>Ingreso neto devengado menos ingreso neto ya cobrado, por año.</summary>
            public Dictionary<int, decimal> IngresoPorAnio { get; } = new();
            public decimal CuentasPorCobrar { get; set; }
            public decimal IsvPendienteVentas { get; set; }
            public decimal EnvioPendienteVentas { get; set; }
            public decimal CuentasPorPagar { get; set; }
            public decimal InventarioPendiente { get; set; }
            public decimal IsvCreditoPendiente { get; set; }
            public decimal AjusteIngreso => IngresoPorAnio.Values.Sum();
        }

        /// <summary>Lo que todavía se le debe a un proveedor por mercadería ya recibida.</summary>
        public record CompraPendiente(int OrdenId, string Numero, DateTime Fecha, decimal Pendiente, decimal Isv, decimal Inventario);

        /// <summary>
        /// Órdenes de compra con mercadería recibida (completa o parcial) que aún no se ha pagado:
        /// valor recibido menos pagos al proveedor menos devoluciones aplicadas a su saldo.
        /// En una recepción parcial solo cuenta lo que ya llegó (cantidad recibida de cada línea).
        /// </summary>
        public static List<CompraPendiente> ComprasPendientes(ApplicationDbContext context, DateTime hastaExcl)
        {
            var res = new List<CompraPendiente>();
            var ordenes = context.OrdenesCompra.AsNoTracking()
                .Where(o => (o.Estado == "Recibida" || o.Estado == "RecibidaParcial") && o.Total > 0
                            && (o.FechaRecepcion ?? o.Fecha) < hastaExcl)
                .Select(o => new { o.Id, o.Numero, o.Estado, o.Total, o.Impuestos, Fecha = o.FechaRecepcion ?? o.Fecha }).ToList();
            if (ordenes.Count == 0) return res;

            var ids = ordenes.Select(o => o.Id).ToList();
            var pagado = context.PagosProveedor.AsNoTracking()
                .Where(p => p.OrdenCompraId != null && ids.Contains(p.OrdenCompraId.Value) && p.Fecha < hastaExcl)
                .Select(p => new { Id = p.OrdenCompraId!.Value, p.Monto }).ToList()
                .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));
            var devuelto = context.DevolucionesProveedor.AsNoTracking()
                .Where(d => d.Estado == "Aplicada" && ids.Contains(d.OrdenCompraId) && d.Fecha < hastaExcl)
                .Select(d => new { Id = d.OrdenCompraId, d.MontoAplicadoSaldo }).ToList()
                .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.Sum(x => x.MontoAplicadoSaldo));

            var idsParciales = ordenes.Where(o => o.Estado == "RecibidaParcial").Select(o => o.Id).ToList();
            var detallesParciales = idsParciales.Count == 0
                ? new Dictionary<int, List<Models.DetalleOrdenCompra>>()
                : context.DetalleOrdenesCompra.AsNoTracking().Where(d => idsParciales.Contains(d.OrdenCompraId)).ToList()
                    .GroupBy(d => d.OrdenCompraId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var o in ordenes)
            {
                decimal recibido, isvRecibido;
                if (o.Estado == "RecibidaParcial")
                {
                    recibido = 0m; isvRecibido = 0m;
                    if (detallesParciales.TryGetValue(o.Id, out var dets))
                        foreach (var d in dets)
                        {
                            if (d.Cantidad <= 0 || d.CantidadRecibida <= 0) continue;
                            var frac = Math.Min(1m, d.CantidadRecibida / d.Cantidad);
                            recibido += d.Total * frac;
                            isvRecibido += Math.Max(0m, d.Total - (d.Subtotal - d.Descuento)) * frac;
                        }
                }
                else
                {
                    recibido = o.Total;
                    isvRecibido = Math.Max(0m, o.Impuestos);
                }
                if (recibido <= 0) continue;

                pagado.TryGetValue(o.Id, out var pg);
                devuelto.TryGetValue(o.Id, out var dv);
                var pend = Math.Round(Math.Max(0m, recibido - pg - dv), 2);
                if (pend <= 0) continue;

                var isv = Math.Round(pend * Math.Min(1m, isvRecibido / recibido), 2);
                res.Add(new CompraPendiente(o.Id, o.Numero, o.Fecha, pend, isv, pend - isv));
            }
            return res;
        }

        private record Doc(int Id, DateTime Fecha, decimal Total, decimal Isv, decimal Envio);

        /// <param name="desde">Si se indica, el ajuste de ingreso solo cuenta desde esa fecha (Estado de Resultados).</param>
        /// <param name="hastaExcl">Fecha de corte (exclusiva).</param>
        public static Devengo Calcular(ApplicationDbContext context, DateTime? desde, DateTime hastaExcl)
        {
            var r = new Devengo();

            // ===== Documentos por cobrar: ventas a crédito + facturas que no vienen de una venta =====
            var ventas = context.Ventas.AsNoTracking()
                .Where(v => v.EsCredito && v.Estado != "Anulada" && v.Estado != "Cancelada" && v.Total > 0 && v.Fecha < hastaExcl)
                .Select(v => new { v.Id, v.Fecha, v.Total, v.Impuestos, v.MontoEnvio }).ToList()
                .Select(v => new Doc(v.Id, v.Fecha, v.Total, v.Impuestos, v.MontoEnvio)).ToList();

            var facturas = context.Facturas.AsNoTracking()
                .Where(f => f.VentaId == null && f.Estado != "Anulada" && f.Estado != "Borrador" && f.Total > 0 && f.Fecha < hastaExcl)
                .Select(f => new { f.Id, f.Fecha, f.Total, f.Impuestos, f.MontoEnvio }).ToList()
                .Select(f => new Doc(f.Id, f.Fecha, f.Total, f.Impuestos, f.MontoEnvio)).ToList();

            // Cobros de ventas a crédito: registrados contra la venta o (solo) contra su factura
            var cobrosDirectos = (from p in context.Pagos.AsNoTracking()
                                  join v in context.Ventas.AsNoTracking() on p.VentaId equals (int?)v.Id
                                  where v.EsCredito && p.Fecha < hastaExcl
                                  select new { Id = v.Id, p.Fecha, p.Monto }).ToList();
            var cobrosPorFactura = (from p in context.Pagos.AsNoTracking()
                                    join f in context.Facturas.AsNoTracking() on p.FacturaId equals (int?)f.Id
                                    join v in context.Ventas.AsNoTracking() on f.VentaId equals (int?)v.Id
                                    where p.VentaId == null && v.EsCredito && p.Fecha < hastaExcl
                                    select new { Id = v.Id, p.Fecha, p.Monto }).ToList();
            // Devoluciones que bajaron la cuenta por cobrar: funcionan como un cobro sin dinero
            // (reducen lo que el cliente debe, la venta devengada y su ISV).
            var devolucionesAplicadas = (from d in context.Devoluciones.AsNoTracking()
                                         join v in context.Ventas.AsNoTracking() on d.VentaId equals (int?)v.Id
                                         where d.Estado == "Aplicada" && d.MontoAplicadoSaldo > 0 && v.EsCredito && d.Fecha < hastaExcl
                                         select new { Id = v.Id, d.Fecha, Monto = d.MontoAplicadoSaldo }).ToList();
            var cobrosVenta = cobrosDirectos.Concat(cobrosPorFactura).Concat(devolucionesAplicadas)
                .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.Select(x => (x.Fecha, x.Monto)).ToList());

            var cobrosFactura = (from p in context.Pagos.AsNoTracking()
                                 join f in context.Facturas.AsNoTracking() on p.FacturaId equals (int?)f.Id
                                 where p.VentaId == null && f.VentaId == null && p.Fecha < hastaExcl
                                 select new { Id = f.Id, p.Fecha, p.Monto }).ToList()
                .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.Select(x => (x.Fecha, x.Monto)).ToList());

            void Procesar(List<Doc> docs, Dictionary<int, List<(DateTime Fecha, decimal Monto)>> cobros)
            {
                foreach (var d in docs)
                {
                    var neto = d.Total - d.Isv - d.Envio;
                    cobros.TryGetValue(d.Id, out var lista);
                    lista ??= new();

                    // Se devenga al facturar...
                    if (!desde.HasValue || d.Fecha >= desde.Value)
                        r.IngresoPorAnio[d.Fecha.Year] = (r.IngresoPorAnio.TryGetValue(d.Fecha.Year, out var a) ? a : 0m) + neto;

                    // ...y lo cobrado ya entró (por su parte neta) en los movimientos
                    foreach (var c in lista)
                    {
                        if (desde.HasValue && c.Fecha < desde.Value) continue;
                        r.IngresoPorAnio[c.Fecha.Year] = (r.IngresoPorAnio.TryGetValue(c.Fecha.Year, out var b) ? b : 0m) - c.Monto * neto / d.Total;
                    }

                    var pendiente = Math.Max(0m, d.Total - lista.Sum(c => c.Monto));
                    if (pendiente <= 0) continue;
                    r.CuentasPorCobrar += pendiente;
                    r.IsvPendienteVentas += pendiente * d.Isv / d.Total;
                    r.EnvioPendienteVentas += pendiente * d.Envio / d.Total;
                }
            }
            Procesar(ventas, cobrosVenta);
            Procesar(facturas, cobrosFactura);

            foreach (var k in r.IngresoPorAnio.Keys.ToList())
                r.IngresoPorAnio[k] = Math.Round(r.IngresoPorAnio[k], 2);
            r.CuentasPorCobrar = Math.Round(r.CuentasPorCobrar, 2);
            r.IsvPendienteVentas = Math.Round(r.IsvPendienteVentas, 2);
            r.EnvioPendienteVentas = Math.Round(r.EnvioPendienteVentas, 2);

            // ===== Compras recibidas (total o parcialmente) sin pagar =====
            foreach (var c in ComprasPendientes(context, hastaExcl))
            {
                r.CuentasPorPagar += c.Pendiente;
                r.IsvCreditoPendiente += c.Isv;
                r.InventarioPendiente += c.Inventario;
            }
            r.CuentasPorPagar = Math.Round(r.CuentasPorPagar, 2);
            r.IsvCreditoPendiente = Math.Round(r.IsvCreditoPendiente, 2);
            r.InventarioPendiente = Math.Round(r.InventarioPendiente, 2);

            return r;
        }
    }
}
