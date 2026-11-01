using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Entregas parciales en bodega: calcula lo pendiente por producto y emite/anula comprobantes.
    /// Pendiente = vendido − devuelto − entregado (comprobantes vigentes).
    /// </summary>
    public static class EntregaBodegaHelper
    {
        public class LineaEntrega
        {
            public int DetalleVentaId { get; set; }
            public decimal Cantidad { get; set; }
        }

        public class LineaPendiente
        {
            public int DetalleVentaId { get; set; }
            public int ProductoId { get; set; }
            public string Producto { get; set; } = "";
            public decimal Vendida { get; set; }
            public decimal Devuelta { get; set; }
            public decimal Entregada { get; set; }
            public decimal Pendiente { get; set; }
        }

        public static List<LineaPendiente> Lineas(ApplicationDbContext context, int ventaId)
        {
            var detalles = context.DetalleVentas.AsNoTracking().Where(d => d.VentaId == ventaId).OrderBy(d => d.Id).ToList();
            var ids = detalles.Select(d => d.ProductoId).Distinct().ToList();
            var nombres = context.Productos.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Nombre);
            var devueltas = DevolucionVentaHelper.YaDevueltoPorDetalle(context, ventaId);

            var entregadas = (from de in context.DetallesEntregaBodega
                              join e in context.EntregasBodega on de.EntregaBodegaId equals e.Id
                              where e.VentaId == ventaId && !e.Anulada
                              select new { de.DetalleVentaId, de.Cantidad })
                             .AsEnumerable()
                             .GroupBy(x => x.DetalleVentaId)
                             .ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));

            return detalles.Select(d =>
            {
                var dev = devueltas.TryGetValue(d.Id, out var a) ? a : 0m;
                var ent = entregadas.TryGetValue(d.Id, out var b) ? b : 0m;
                return new LineaPendiente
                {
                    DetalleVentaId = d.Id,
                    ProductoId = d.ProductoId,
                    Producto = nombres.GetValueOrDefault(d.ProductoId, "—"),
                    Vendida = d.Cantidad,
                    Devuelta = dev,
                    Entregada = ent,
                    Pendiente = Math.Max(0m, d.Cantidad - dev - ent)
                };
            }).ToList();
        }

        public static decimal TotalPendiente(ApplicationDbContext context, int ventaId)
            => Lineas(context, ventaId).Sum(l => l.Pendiente);

        /// <summary>Emite un comprobante de entrega (parcial o total). Devuelve (entrega, error).</summary>
        public static (EntregaBodega? entrega, string? error) Crear(ApplicationDbContext context, int ventaId,
            List<LineaEntrega> items, string? recibidoPor, string? notas, int usuarioId)
        {
            var venta = context.Ventas.FirstOrDefault(v => v.Id == ventaId);
            if (venta == null) return (null, "Venta no encontrada.");
            if (venta.Estado == "Anulada") return (null, "La venta está anulada: no se puede entregar mercadería.");

            var pedidos = items.Where(i => i.Cantidad > 0).ToList();
            if (pedidos.Count == 0) return (null, "Indica la cantidad a entregar de al menos un producto.");
            if (pedidos.GroupBy(p => p.DetalleVentaId).Any(g => g.Count() > 1)) return (null, "Hay una línea repetida.");

            var abrimosTx = context.Database.CurrentTransaction == null;
            var tx = abrimosTx ? context.Database.BeginTransaction() : null;
            try
            {
                // Bloqueo de la venta: dos bodegueros entregando a la vez no pueden pasarse de lo pendiente
                if (context.Database.IsRelational())
                {
                    context.Ventas.Where(v => v.Id == ventaId)
                        .ExecuteUpdate(u => u.SetProperty(v => v.Estado, v => v.Estado));
                }

                var lineas = Lineas(context, ventaId);
                foreach (var it in pedidos)
                {
                    var l = lineas.FirstOrDefault(x => x.DetalleVentaId == it.DetalleVentaId);
                    if (l == null) { tx?.Rollback(); return (null, "Una de las líneas no pertenece a esta venta."); }
                    if (it.Cantidad > l.Pendiente + 0.0001m)
                    {
                        tx?.Rollback();
                        return (null, $"De '{l.Producto}' solo quedan {l.Pendiente:0.####} por entregar.");
                    }
                }

                var entrega = new EntregaBodega
                {
                    Numero = "TMP-" + Guid.NewGuid().ToString("N"),
                    Fecha = DateTime.Now,
                    VentaId = ventaId,
                    ClienteId = venta.ClienteId,
                    RecibidoPor = string.IsNullOrWhiteSpace(recibidoPor) ? null : recibidoPor.Trim(),
                    Notas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim(),
                    UsuarioId = usuarioId
                };
                context.EntregasBodega.Add(entrega);
                context.SaveChanges();
                entrega.Numero = $"ENT-{entrega.Id:D5}";

                foreach (var it in pedidos)
                {
                    var l = lineas.First(x => x.DetalleVentaId == it.DetalleVentaId);
                    context.DetallesEntregaBodega.Add(new DetalleEntregaBodega
                    {
                        EntregaBodegaId = entrega.Id,
                        DetalleVentaId = it.DetalleVentaId,
                        ProductoId = l.ProductoId,
                        Cantidad = it.Cantidad
                    });
                }

                // Desde que hay una entrega, la venta queda bajo control de entregas
                venta.ControlEntrega = true;
                context.SaveChanges();
                tx?.Commit();
                return (entrega, null);
            }
            catch (Exception ex)
            {
                tx?.Rollback();
                return (null, $"Error al registrar la entrega: {ex.Message}");
            }
            finally
            {
                tx?.Dispose();
            }
        }

        /// <summary>Anula un comprobante: la cantidad vuelve a quedar pendiente por entregar.</summary>
        public static string? Anular(ApplicationDbContext context, int entregaId, string? motivo, int usuarioId)
        {
            if (string.IsNullOrWhiteSpace(motivo)) return "Escribe el motivo de la anulación.";
            var e = context.EntregasBodega.FirstOrDefault(x => x.Id == entregaId);
            if (e == null) return "Comprobante no encontrado.";
            if (e.Anulada) return "El comprobante ya está anulado.";

            e.Anulada = true;
            e.MotivoAnulacion = motivo.Trim();
            e.FechaAnulacion = DateTime.Now;
            e.UsuarioAnuloId = usuarioId;
            context.SaveChanges();
            return null;
        }
    }
}
