using Kirkenta.Helpers;
using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Configuracion
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public ConfiguracionEmpresa Empresa { get; set; } = new();
        public bool PuedeEditar { get; set; }


        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            PuedeEditar = PermisoHelper.TienePermiso(_context, currentRol, "Configuracion", "Empresa", "editar");
            if (!PermisoHelper.TienePermiso(_context, currentRol, "Configuracion", "Empresa", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver esta sección";
                return RedirectToPage("/Index");
            }

            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault() ?? new ConfiguracionEmpresa();
            return Page();
        }
    }
}