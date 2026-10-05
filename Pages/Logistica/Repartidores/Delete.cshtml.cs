using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Logistica.Repartidores
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Repartidor Repartidor { get; set; } = new();
        public int TotalRutas { get; set; }
        public int RutasActivas { get; set; }
        public int TotalEnvios { get; set; }

        public bool PuedeEliminar =>
            TotalRutas == 0 &&
            RutasActivas == 0 &&
            TotalEnvios == 0;

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RepartidoresDelete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar repartidores";
                return RedirectToPage("/Logistica/Repartidores/Index");
            }

            var repartidor = _context.Repartidores.FirstOrDefault(r => r.Id == id);
            if (repartidor == null)
            {
                TempData["Error"] = "Repartidor no encontrado";
                return RedirectToPage("/Logistica/Repartidores/Index");
            }

            Repartidor = repartidor;
            CargarContadores(id);

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RepartidoresDelete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar repartidores";
                return RedirectToPage("/Logistica/Repartidores/Index");
            }

            var repartidor = _context.Repartidores.FirstOrDefault(r => r.Id == id);
            if (repartidor == null)
            {
                TempData["Error"] = "Repartidor no encontrado";
                return RedirectToPage("/Logistica/Repartidores/Index");
            }

            // Bloquear si tiene rutas o envíos asociados
            var totalRutas = _context.Rutas.Count(r => r.RepartidorId == id);
            if (totalRutas > 0)
            {
                TempData["Error"] = "No se puede eliminar: el repartidor tiene rutas asignadas. Desactívalo en su lugar.";
                return RedirectToPage("/Logistica/Repartidores/Index");
            }

            var totalEnvios = _context.Envios.Count(e => e.RepartidorId == id);
            if (totalEnvios > 0)
            {
                TempData["Error"] = "No se puede eliminar: el repartidor tiene envíos asignados. Desactívalo en su lugar.";
                return RedirectToPage("/Logistica/Repartidores/Index");
            }

            var codigo = repartidor.Codigo;
            var nombre = repartidor.Nombre;

            _context.Repartidores.Remove(repartidor);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar repartidor",
                $"Eliminó al repartidor {codigo} — {nombre}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Repartidor '{nombre}' eliminado correctamente";
            return RedirectToPage("/Logistica/Repartidores/Index");
        }

        private void CargarContadores(int repartidorId)
        {
            TotalRutas = _context.Rutas.Count(r => r.RepartidorId == repartidorId);
            RutasActivas = _context.Rutas
                .Count(r => r.RepartidorId == repartidorId
                         && (r.Estado == "Borrador" || r.Estado == "EnReparto"));
            TotalEnvios = _context.Envios.Count(e => e.RepartidorId == repartidorId);
        }
    }
}