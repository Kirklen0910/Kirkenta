using Kirkenta.Data;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Devolución de mercadería a un proveedor: baja el stock (kardex) y reduce lo que se le debe.
    /// Si la orden ya estaba pagada, el excedente queda como "por recuperar" del proveedor.
    /// </summary>
    public static class DevolucionProveedorHelper
    {
        public class ItemSolicitud
        {
            public int DetalleOrdenId { get; set; }
            public decimal Cantidad { get; set; }
        }

        public class Solicitud
        {
            public int OrdenCompraId { get; set; }
            public string? Motivo { get; set; }
            public int UsuarioId { get; set; }
            public List<ItemSolicitud> Items { get; set; } = new();
        }

        public static Dictionary<int, decimal> YaDevueltoPorDetalle(ApplicationDbContext context, int ordenId)
        {
            return (from dd in context.DetalleDevolucionesProveedor
                    join d in context.DevolucionesProveedor on dd.DevolucionId equals d.Id
                    where d.OrdenCompraId == ordenId && d.Estado == "Aplicada" && dd.DetalleOrdenCompraId != null
                    select new { dd.DetalleOrdenCompraId, dd.Cantidad })
                   .AsEnumerable()
                   .GroupBy(x => x.DetalleOrdenCompraId!.Value)
                   .ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));
        }

        /// <summary>Costo unitario neto (en moneda local) de una línea de compra.</summary>
        public static decimal CostoUnitarioLocal(DetalleOrdenCompra d, decimal tipoCambio)
        {
            if (d.Cantidad <= 0) return 0;
            return Math.Round(d.Total / d.Cantidad * tipoCambio, 4);
        }

        public static (DevolucionProveedor? devolucion, string? error) Crear(ApplicationDbContext context, Solicitud s)
        {
            var orden = context.OrdenesCompra.FirstOrDefault(o => o.Id == s.OrdenCompraId);
            if (orden == null) return (null, "Orden de compra no encontrada.");
            // Una orden ya pagada también admite devolución: lo devuelto queda "por recuperar" del proveedor
            if (orden.Estado != "Recibida" && orden.Estado != "RecibidaParcial" && orden.Estado != "Pagada")
                return (null, "Solo se puede devolver mercadería de órdenes recibidas.");
            if (string.IsNullOrWhiteSpace(s.Motivo)) return (null, "El motivo es obligatorio.");

            var items = s.Items.Where(i => i.Cantidad > 0).ToList();
            if (items.Count == 0) return (null, "Indica la cantidad a devolver de al menos un producto.");

            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(context, DateTime.Today);
            if (!periodoOk) return (null, periodoError);

            var detalles = context.DetalleOrdenesCompra.Where(d => d.OrdenCompraId == orden.Id).ToList();
            var yaDevuelto = YaDevueltoPorDetalle(context, orden.Id);

            var lineas = new List<(DetalleOrdenCompra det, Producto prod, decimal cantidad, decimal costo)>();
            foreach (var it in items)
            {
                var det = detalles.FirstOrDefault(d => d.Id == it.DetalleOrdenId);
                if (det == null) return (null, "Una de las líneas no pertenece a esta orden.");

                var prod = context.Productos.FirstOrDefault(p => p.Id == det.ProductoId);
                if (prod == null) return (null, "Producto no encontrado.");

                var disponible = det.CantidadRecibida - (yaDevuelto.TryGetValue(det.Id, out var yd) ? yd : 0);
                if (it.Cantidad > disponible)
                    return (null, $"No puedes devolver {it.Cantidad:0.##} de '{prod.Nombre}'; solo hay {disponible:0.##} recibidas sin devolver.");

                if (prod.Stock < it.Cantidad)
                    return (null, $"Stock insuficiente de '{prod.Nombre}' para devolver (stock actual {prod.Stock:0.##}).");

                lineas.Add((det, prod, it.Cantidad, CostoUnitarioLocal(det, orden.TipoCambio)));
            }

            decimal total = Math.Round(lineas.Sum(l => l.costo * l.cantidad), 2, MidpointRounding.AwayFromZero);
            if (total <= 0) return (null, "El total de la devolución debe ser mayor a cero.");

            var abrimosTx = context.Database.CurrentTransaction == null;
            var tx = abrimosTx ? context.Database.BeginTransaction() : null;

            try
            {
                // Bloqueo de la orden y revalidación con datos frescos (dos devoluciones simultáneas no pueden
                // devolver más de lo recibido ni dejar el saldo mal)
                if (context.Database.IsRelational())
                {
                    context.OrdenesCompra.Where(o => o.Id == orden.Id)
                        .ExecuteUpdate(u => u.SetProperty(o => o.Estado, o => o.Estado));
                    context.Entry(orden).Reload();
                    var yaDevuelto2 = YaDevueltoPorDetalle(context, orden.Id);
                    foreach (var (det, prod, cantidad, _) in lineas)
                    {
                        context.Entry(det).Reload();
                        context.Entry(prod).Reload();
                        var disp = det.CantidadRecibida - (yaDevuelto2.TryGetValue(det.Id, out var y2) ? y2 : 0);
                        if (cantidad > disp || prod.Stock < cantidad)
                        {
                            tx?.Rollback();
                            return (null, $"Otra persona registró movimientos de '{prod.Nombre}' al mismo tiempo; revisa lo disponible e inténtalo de nuevo.");
                        }
                    }
                }

                var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "DevolucionProveedor");

                // El saldo de la orden está en la moneda de la orden; el total aquí en moneda local.
                var totalEnMonedaOrden = orden.TipoCambio > 0 ? Math.Round(total / orden.TipoCambio, 2, MidpointRounding.AwayFromZero) : total;
                var aplicado = Math.Min(totalEnMonedaOrden, orden.Saldo);
                var porRecuperar = totalEnMonedaOrden - aplicado;

                var dev = new DevolucionProveedor
                {
                    Numero = numero,
                    OrdenCompraId = orden.Id,
                    ProveedorId = orden.ProveedorId,
                    Fecha = DateTime.Now,
                    Motivo = s.Motivo,
                    Total = total,
                    Estado = "Aplicada",
                    UsuarioCreoId = s.UsuarioId,
                    MontoAplicadoSaldo = aplicado,
                    MontoPorRecuperar = porRecuperar
                };
                context.DevolucionesProveedor.Add(dev);
                context.SaveChanges();

                foreach (var (det, prod, cantidad, costo) in lineas)
                {
                    context.DetalleDevolucionesProveedor.Add(new DetalleDevolucionProveedor
                    {
                        DevolucionId = dev.Id,
                        ProductoId = prod.Id,
                        DetalleOrdenCompraId = det.Id,
                        Cantidad = cantidad,
                        PrecioUnitario = costo,
                        Total = Math.Round(costo * cantidad, 2, MidpointRounding.AwayFromZero)
                    });

                    InventarioHelper.Registrar(context, prod, -cantidad, "DevolucionProveedor",
                        numero, dev.Id, s.UsuarioId, costo, $"Devolución a proveedor (orden {orden.Numero})");
                }

                orden.Saldo -= aplicado;
                if (orden.Saldo < 0) orden.Saldo = 0;
                // Si la devolución dejó la orden sin saldo, ya no hay nada que pagar (igual que al pagarla completa)
                if (orden.Saldo <= 0 && orden.Estado == "Recibida") orden.Estado = "Pagada";
                orden.Notas = (string.IsNullOrWhiteSpace(orden.Notas) ? "" : orden.Notas + "\n")
                    + $"[Devolución {numero} {DateTime.Now:dd/MM/yyyy}] L. {total:N2}" +
                      (porRecuperar > 0 ? $" — por recuperar del proveedor: {porRecuperar:N2}" : "");

                context.SaveChanges();
                tx?.Commit();
                return (dev, null);
            }
            catch (Exception ex)
            {
                tx?.Rollback();
                Console.WriteLine($"[DevolucionProveedor] Error: {ex.Message}");
                return (null, $"Error al registrar la devolución: {ex.Message}");
            }
            finally
            {
                tx?.Dispose();
            }
        }
    }
}
