using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.MetodosPago
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public MetodoPago Metodo { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/MetodosPago/Index");
            }

            var metodo = _context.MetodosPago.FirstOrDefault(m => m.Id == id);
            if (metodo == null)
            {
                TempData["Error"] = "Método no encontrado";
                return RedirectToPage("/MetodosPago/Index");
            }

            Metodo = metodo;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/MetodosPago/Index");
            }

            var metodo = _context.MetodosPago.FirstOrDefault(m => m.Id == id);
            if (metodo == null)
            {
                TempData["Error"] = "Método no encontrado";
                return RedirectToPage("/MetodosPago/Index");
            }

            // Verificar que no tenga pagos
            if (_context.Pagos.Any(p => p.MetodoPagoId == id))
            {
                TempData["Error"] = "No se puede eliminar un método que tiene pagos registrados";
                return RedirectToPage("/MetodosPago/Index");
            }

            var nombre = metodo.Nombre;
            _context.MetodosPago.Remove(metodo);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar método de pago",
                $"Eliminó el método '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Método '{nombre}' eliminado";
            return RedirectToPage("/MetodosPago/Index");
        }
    }
}