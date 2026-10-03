using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Clientes
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Cliente Cliente { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Clientes", "eliminar") && currentRol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para eliminar clientes";
                return RedirectToPage("/Clientes/Index");
            }

            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);
            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado";
                return RedirectToPage("/Clientes/Index");
            }

            // Verificar que no tenga ventas
            if (_context.Ventas.Any(v => v.ClienteId == id))
            {
                TempData["Error"] = "No se puede eliminar un cliente que tiene ventas asociadas";
                return RedirectToPage("/Clientes/Index");
            }

            Cliente = cliente;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Clientes", "eliminar") && currentRol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para eliminar clientes";
                return RedirectToPage("/Clientes/Index");
            }

            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);
            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado";
                return RedirectToPage("/Clientes/Index");
            }

            if (_context.Ventas.Any(v => v.ClienteId == id))
            {
                TempData["Error"] = "No se puede eliminar un cliente que tiene ventas asociadas";
                return RedirectToPage("/Clientes/Index");
            }

            var nombre = cliente.Nombre;
            _context.Clientes.Remove(cliente);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar cliente",
                $"Eliminó al cliente '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cliente '{nombre}' eliminado correctamente";
            return RedirectToPage("/Clientes/Index");
        }
    }
}