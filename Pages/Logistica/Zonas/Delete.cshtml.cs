using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Logistica.Zonas
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public ZonaEnvio Zona { get; set; } = new();
        public int EnviosAsociados { get; set; }
        public bool PuedeEliminar => EnviosAsociados == 0;

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "ZonasDelete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar zonas";
                return RedirectToPage("/Logistica/Zonas/Index");
            }

            var zona = _context.ZonasEnvio.FirstOrDefault(z => z.Id == id);
            if (zona == null)
            {
                TempData["Error"] = "Zona no encontrada";
                return RedirectToPage("/Logistica/Zonas/Index");
            }

            Zona = zona;
            EnviosAsociados = _context.Envios.Count(e => e.ZonaId == id);

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "ZonasDelete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar zonas";
                return RedirectToPage("/Logistica/Zonas/Index");
            }

            var zona = _context.ZonasEnvio.FirstOrDefault(z => z.Id == id);
            if (zona == null)
            {
                TempData["Error"] = "Zona no encontrada";
                return RedirectToPage("/Logistica/Zonas/Index");
            }

            // Bloquear si tiene envíos asociados
            if (_context.Envios.Any(e => e.ZonaId == id))
            {
                TempData["Error"] = "No se puede eliminar: la zona tiene envíos asociados. Desactívala en su lugar.";
                return RedirectToPage("/Logistica/Zonas/Index");
            }

            var nombre = zona.Nombre;
            _context.ZonasEnvio.Remove(zona);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar zona de envío",
                $"Eliminó la zona '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Zona '{nombre}' eliminada correctamente";
            return RedirectToPage("/Logistica/Zonas/Index");
        }
    }
}