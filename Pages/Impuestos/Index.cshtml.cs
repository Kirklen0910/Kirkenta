using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Impuestos
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Impuesto> Impuestos { get; set; } = new();

        public bool PuedeCrear { get; private set; }
        public bool PuedeEditar { get; private set; }
        public bool PuedeEliminar { get; private set; }
        public bool PuedeImportar { get; private set; }
        public bool PuedeExportar { get; private set; }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (!Kirkenta.Helpers.PermisoHelper.UsuarioTieneAcceso(_context, User.Identity?.Name, "Configuracion", "Impuestos"))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            string? usr = User.Identity?.Name;
            PuedeCrear = Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, usr, "Configuracion", "Impuestos", "crear");
            PuedeEditar = Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, usr, "Configuracion", "Impuestos", "editar");
            PuedeEliminar = Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, usr, "Configuracion", "Impuestos", "eliminar");
            PuedeImportar = PuedeCrear;
            PuedeExportar = Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, usr, "Configuracion", "Impuestos", "ver");

            Impuestos = _context.Impuestos.OrderBy(i => i.Porcentaje).ToList();
            return Page();
        }
    }
}