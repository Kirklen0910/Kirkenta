using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Productos
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Producto Producto { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Productos/Index");
            }

            var producto = _context.Productos.FirstOrDefault(p => p.Id == id);
            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado";
                return RedirectToPage("/Productos/Index");
            }

            Producto = producto;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Productos/Index");
            }

            var producto = _context.Productos.FirstOrDefault(p => p.Id == id);
            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado";
                return RedirectToPage("/Productos/Index");
            }

            // Verificar que no esté en ventas
            if (_context.DetalleVentas.Any(d => d.ProductoId == id))
            {
                TempData["Error"] = "No se puede eliminar un producto que tiene ventas registradas";
                return RedirectToPage("/Productos/Index");
            }

            var nombre = producto.Nombre;
            _context.Productos.Remove(producto);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar producto",
                $"Eliminó el producto '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Producto '{nombre}' eliminado";
            return RedirectToPage("/Productos/Index");
        }
    }
}