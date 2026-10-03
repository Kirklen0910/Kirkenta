using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Kirkenta.Pages.Compras
{
    public class HistorialProductoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public HistorialProductoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Producto Producto { get; set; } = new();
        public List<CompraItem> Compras { get; set; } = new();

        public decimal CantidadTotalComprada { get; set; }
        public decimal TotalInvertido { get; set; }
        public decimal PrecioPromedio { get; set; }
        public decimal VariacionPrecio { get; set; }

        public string PreciosJson { get; set; } = "{}";

        public class CompraItem
        {
            public int OrdenId { get; set; }
            public string NumeroOrden { get; set; } = "";
            public DateTime Fecha { get; set; }
            public string ProveedorNombre { get; set; } = "";
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Total { get; set; }
            public string Estado { get; set; } = "";
        }

        public IActionResult OnGet(int productoId)
        {
            var producto = _context.Productos.FirstOrDefault(p => p.Id == productoId);
            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado";
                return RedirectToPage("/Productos/Index");
            }

            Producto = producto;

            var proveedores = _context.Proveedores.ToList();

            // Obtener todas las compras de este producto
            var detalles = _context.DetalleOrdenesCompra
                .Where(d => d.ProductoId == productoId)
                .ToList();

            var ordenIds = detalles.Select(d => d.OrdenCompraId).Distinct().ToList();
            var ordenes = _context.OrdenesCompra
                .Where(o => ordenIds.Contains(o.Id))
                .OrderByDescending(o => o.Fecha)
                .ToList();

            Compras = new List<CompraItem>();

            foreach (var orden in ordenes)
            {
                var itemsOrden = detalles.Where(d => d.OrdenCompraId == orden.Id).ToList();
                foreach (var item in itemsOrden)
                {
                    Compras.Add(new CompraItem
                    {
                        OrdenId = orden.Id,
                        NumeroOrden = orden.Numero,
                        Fecha = orden.Fecha,
                        ProveedorNombre = proveedores.FirstOrDefault(p => p.Id == orden.ProveedorId)?.Nombre ?? "—",
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.PrecioUnitario,
                        Total = item.Total,
                        Estado = orden.Estado
                    });
                }
            }

            // KPIs
            CantidadTotalComprada = Compras.Sum(c => c.Cantidad);
            TotalInvertido = Compras.Sum(c => c.Total);
            PrecioPromedio = Compras.Count > 0 ? Compras.Average(c => c.PrecioUnitario) : 0;

            // Variación de precio (último precio vs promedio)
            if (PrecioPromedio > 0 && Compras.Count > 0)
            {
                var ultimoPrecio = Compras.First().PrecioUnitario;
                VariacionPrecio = ((ultimoPrecio - PrecioPromedio) / PrecioPromedio) * 100;
            }

            // Gráfico de evolución de precios (ordenado por fecha ascendente)
            var preciosPorFecha = Compras
                .OrderBy(c => c.Fecha)
                .Select(c => new
                {
                    Fecha = c.Fecha.ToString("dd/MM/yy"),
                    Precio = c.PrecioUnitario
                })
                .ToList();

            PreciosJson = JsonSerializer.Serialize(new
            {
                labels = preciosPorFecha.Select(x => x.Fecha).ToArray(),
                data = preciosPorFecha.Select(x => x.Precio).ToArray()
            });

            return Page();
        }
    }
}