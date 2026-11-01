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
            if (!PermisoHelper.TienePermiso(_context, currentRol, "Configuracion", "Impuestos", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar aquí";
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
            if (!PermisoHelper.TienePermiso(_context, currentRol, "Configuracion", "Impuestos", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar aquí";
                return RedirectToPage("/Impuestos/Index");
            }

            var impuesto = _context.Impuestos.FirstOrDefault(i => i.Id == id);
            if (impuesto == null)
            {
                TempData["Error"] = "Impuesto no encontrado";
                return RedirectToPage("/Impuestos/Index");
            }

            // El impuesto predeterminado es el que se aplica a las ventas nuevas: sin él, el ISV sale en 0.
            if (impuesto.EsPredeterminado)
            {
                TempData["Error"] = "No se puede eliminar el impuesto predeterminado. Marca otro como predeterminado primero.";
                return RedirectToPage("/Impuestos/Index");
            }

            // Si es el único impuesto activo, tampoco: quedaría el sistema sin ISV.
            if (impuesto.Activo && !_context.Impuestos.Any(i => i.Id != id && i.Activo))
            {
                TempData["Error"] = "No se puede eliminar el único impuesto activo del sistema.";
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