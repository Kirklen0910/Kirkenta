using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Cotizaciones
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Cotizacion Cotizacion { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Cotizaciones", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Cotizaciones/Index");
            }

            var cotizacion = _context.Cotizaciones.FirstOrDefault(c => c.Id == id);
            if (cotizacion == null)
            {
                TempData["Error"] = "Cotización no encontrada";
                return RedirectToPage("/Cotizaciones/Index");
            }

            if (cotizacion.Estado == "Convertida")
            {
                TempData["Error"] = "No se puede eliminar una cotización convertida";
                return RedirectToPage("/Cotizaciones/Index");
            }

            Cotizacion = cotizacion;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Cotizaciones", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Cotizaciones/Index");
            }

            var cotizacion = _context.Cotizaciones.FirstOrDefault(c => c.Id == id);
            if (cotizacion == null)
            {
                TempData["Error"] = "Cotización no encontrada";
                return RedirectToPage("/Cotizaciones/Index");
            }

            if (cotizacion.Estado == "Convertida")
            {
                TempData["Error"] = "No se puede eliminar una cotización convertida";
                return RedirectToPage("/Cotizaciones/Index");
            }

            var numero = cotizacion.Numero;
            _context.Cotizaciones.Remove(cotizacion);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar cotización",
                $"Eliminó la cotización {numero}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cotización {numero} eliminada";
            return RedirectToPage("/Cotizaciones/Index");
        }
    }
}