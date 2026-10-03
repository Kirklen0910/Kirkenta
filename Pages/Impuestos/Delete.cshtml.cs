using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Impuestos
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Impuesto Impuesto { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Impuestos/Index");
            }

            var impuesto = _context.Impuestos.FirstOrDefault(i => i.Id == id);
            if (impuesto == null)
            {
                TempData["Error"] = "Impuesto no encontrado";
                return RedirectToPage("/Impuestos/Index");
            }

            Impuesto = impuesto;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Impuestos/Index");
            }

            var impuesto = _context.Impuestos.FirstOrDefault(i => i.Id == id);
            if (impuesto == null)
            {
                TempData["Error"] = "Impuesto no encontrado";
                return RedirectToPage("/Impuestos/Index");
            }

            // Quitar el impuesto de los productos
            var productos = _context.Productos.Where(p => p.ImpuestoId == id).ToList();
            foreach (var p in productos) p.ImpuestoId = null;

            var nombre = impuesto.Nombre;
            _context.Impuestos.Remove(impuesto);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar impuesto",
                $"Eliminó el impuesto '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Impuesto '{nombre}' eliminado";
            return RedirectToPage("/Impuestos/Index");
        }
    }
}