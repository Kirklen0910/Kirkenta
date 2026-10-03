using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Series
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public SerieDocumento Serie { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Series/Index");
            }

            var serie = _context.SeriesDocumentos.FirstOrDefault(s => s.Id == id);
            if (serie == null)
            {
                TempData["Error"] = "Serie no encontrada";
                return RedirectToPage("/Series/Index");
            }

            Serie = serie;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Series/Index");
            }

            var serie = _context.SeriesDocumentos.FirstOrDefault(s => s.Id == id);
            if (serie == null)
            {
                TempData["Error"] = "Serie no encontrada";
                return RedirectToPage("/Series/Index");
            }

            var nombre = serie.Nombre;
            _context.SeriesDocumentos.Remove(serie);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar serie",
                $"Eliminó la serie '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Serie '{nombre}' eliminada";
            return RedirectToPage("/Series/Index");
        }
    }
}