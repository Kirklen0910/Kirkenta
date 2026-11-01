using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.DevolucionesProveedor
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public DevolucionProveedor Devolucion { get; set; } = new();
        public Proveedor? Proveedor { get; set; }
        public OrdenCompra? Orden { get; set; }
        public List<(DetalleDevolucionProveedor det, string producto)> Items { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Compras", "DevolucionesProveedor", "ver"))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var dev = _context.DevolucionesProveedor.FirstOrDefault(d => d.Id == id);
            if (dev == null)
            {
                TempData["Error"] = "Devolución no encontrada";
                return RedirectToPage("/DevolucionesProveedor/Index");
            }

            Devolucion = dev;
            Proveedor = _context.Proveedores.FirstOrDefault(p => p.Id == dev.ProveedorId);
            Orden = _context.OrdenesCompra.FirstOrDefault(o => o.Id == dev.OrdenCompraId);

            var productos = _context.Productos.ToDictionary(p => p.Id, p => p.Nombre);
            Items = _context.DetalleDevolucionesProveedor.Where(d => d.DevolucionId == id).ToList()
                .Select(d => (d, productos.TryGetValue(d.ProductoId, out var n) ? n : $"Producto {d.ProductoId}"))
                .ToList();
            return Page();
        }
    }
}
