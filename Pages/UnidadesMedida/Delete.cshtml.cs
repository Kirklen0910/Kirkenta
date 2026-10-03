using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.UnidadesMedida
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public UnidadMedida Unidad { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            var unidad = _context.UnidadesMedida.FirstOrDefault(u => u.Id == id);
            if (unidad == null)
            {
                TempData["Error"] = "Unidad no encontrada";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            Unidad = unidad;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            var unidad = _context.UnidadesMedida.FirstOrDefault(u => u.Id == id);
            if (unidad == null)
            {
                TempData["Error"] = "Unidad no encontrada";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            var nombre = unidad.Nombre;
            _context.UnidadesMedida.Remove(unidad);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar unidad de medida",
                $"Eliminó la unidad '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Unidad '{nombre}' eliminada";
            return RedirectToPage("/UnidadesMedida/Index");
        }
    }
}