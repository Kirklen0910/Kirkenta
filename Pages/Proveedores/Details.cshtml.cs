using Kirkenta.Helpers;
using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Proveedores
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Proveedor Proveedor { get; set; } = new();
        public decimal TotalComprado { get; set; }
        public int CantidadCompras { get; set; }
        public decimal SaldoPendiente { get; set; }
        public DateTime? UltimaCompra { get; set; }
        public List<OrdenCompra> UltimasOrdenes { get; set; } = new();
        public List<ProductoComprado> ProductosComprados { get; set; } = new();

        public class ProductoComprado
        {
            public int ProductoId { get; set; }
            public string Nombre { get; set; } = "";
            public decimal Cantidad { get; set; }
            public decimal UltimoCosto { get; set; }
            public DateTime UltimaCompra { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            if (!(PermisoHelper.UsuarioTieneAcceso(_context, User.Identity?.Name, "Compras", "Proveedores")))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var p = _context.Proveedores.FirstOrDefault(x => x.Id == id);
            if (p == null)
            {
                TempData["Error"] = "Proveedor no encontrado";
                return RedirectToPage("/Proveedores/Index");
            }

            Proveedor = p;

            var ordenes = _context.OrdenesCompra
                .Where(o => o.ProveedorId == id && o.Estado != "Cancelada")
                .OrderByDescending(o => o.Fecha)
                .ToList();

            CantidadCompras = ordenes.Count;
            TotalComprado = ordenes.Sum(o => o.Total);
            SaldoPendiente = ordenes.Sum(o => o.Saldo);
            UltimaCompra = ordenes.FirstOrDefault()?.Fecha;
            UltimasOrdenes = ordenes.Take(25).ToList();

            // Productos que le compramos a este proveedor (órdenes vigentes), con el último costo
            var lineas = (from d in _context.DetalleOrdenesCompra
                          join o in _context.OrdenesCompra on d.OrdenCompraId equals o.Id
                          where o.ProveedorId == id && o.Estado != "Cancelada" && o.Estado != "Borrador"
                          select new { d.ProductoId, d.Cantidad, d.PrecioUnitario, o.Fecha, d.Id })
                         .ToList();
            var agrupado = lineas.GroupBy(x => x.ProductoId)
                .Select(g =>
                {
                    var ultima = g.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id).First();
                    return new { ProductoId = g.Key, Cantidad = g.Sum(x => x.Cantidad), ultima.PrecioUnitario, ultima.Fecha };
                })
                .OrderByDescending(x => x.Fecha)
                .Take(20)
                .ToList();
            var prodIds = agrupado.Select(a => a.ProductoId).ToList();
            var nombres = _context.Productos.Where(pr => prodIds.Contains(pr.Id)).ToDictionary(pr => pr.Id, pr => pr.Nombre);
            ProductosComprados = agrupado.Select(a => new ProductoComprado
            {
                ProductoId = a.ProductoId,
                Nombre = nombres.TryGetValue(a.ProductoId, out var n) ? n : "(producto eliminado)",
                Cantidad = a.Cantidad,
                UltimoCosto = a.PrecioUnitario,
                UltimaCompra = a.Fecha
            }).ToList();

            return Page();
        }
    }
}