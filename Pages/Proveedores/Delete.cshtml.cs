using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Proveedores
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Proveedor Proveedor { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "Proveedores", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Proveedores/Index");
            }

            var p = _context.Proveedores.FirstOrDefault(x => x.Id == id);
            if (p == null)
            {
                TempData["Error"] = "Proveedor no encontrado";
                return RedirectToPage("/Proveedores/Index");
            }

            if (_context.OrdenesCompra.Any(o => o.ProveedorId == id))
            {
                TempData["Error"] = "No se puede eliminar: tiene órdenes de compra asociadas. Desactívalo mejor.";
                return RedirectToPage("/Proveedores/Index");
            }

            Proveedor = p;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "Proveedores", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Proveedores/Index");
            }

            var p = _context.Proveedores.FirstOrDefault(x => x.Id == id);
            if (p == null)
            {
                TempData["Error"] = "Proveedor no encontrado";
                return RedirectToPage("/Proveedores/Index");
            }

            if (_context.OrdenesCompra.Any(o => o.ProveedorId == id))
            {
                TempData["Error"] = "No se puede eliminar: tiene órdenes de compra asociadas";
                return RedirectToPage("/Proveedores/Index");
            }

            var nombre = p.Nombre;
            _context.Proveedores.Remove(p);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar proveedor",
                $"Eliminó al proveedor '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Proveedor '{nombre}' eliminado";
            return RedirectToPage("/Proveedores/Index");
        }
    }
}