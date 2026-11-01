using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.UnidadesMedida
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<UnidadMedida> Unidades { get; set; } = new();

        public bool PuedeCrear { get; private set; }
        public bool PuedeEditar { get; private set; }
        public bool PuedeEliminar { get; private set; }
        public bool PuedeImportar { get; private set; }
        public bool PuedeExportar { get; private set; }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (!Kirkenta.Helpers.PermisoHelper.UsuarioTieneAcceso(_context, User.Identity?.Name, "Inventario", "UnidadesMedida"))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            string? usr = User.Identity?.Name;
            PuedeCrear = Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, usr, "Inventario", "UnidadesMedida", "crear");
            PuedeEditar = Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, usr, "Inventario", "UnidadesMedida", "editar");
            PuedeEliminar = Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, usr, "Inventario", "UnidadesMedida", "eliminar");
            PuedeImportar = PuedeCrear;
            PuedeExportar = Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, usr, "Inventario", "UnidadesMedida", "ver");

            Unidades = _context.UnidadesMedida.OrderBy(u => u.Nombre).ToList();
            return Page();
        }
    }
}