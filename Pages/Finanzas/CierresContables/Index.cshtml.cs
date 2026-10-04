using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.CierresContables
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CierreContable> Meses { get; set; } = new();
        public int AnioSeleccionado { get; set; }

        public bool PuedeCerrar { get; set; }
        public bool PuedeReabrir { get; set; }

        public IActionResult OnGet(int? anio)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresContables", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Index");
            }

            PuedeCerrar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresContablesCreate", "crear");
            PuedeReabrir = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresContablesReabrir", "editar");

            AnioSeleccionado = anio ?? DateTime.Today.Year;
            Meses = CierreContableHelper.ObtenerEstadoAnual(_context, AnioSeleccionado);

            return Page();
        }
    }
}