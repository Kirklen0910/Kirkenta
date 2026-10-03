using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Usuarios
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Usuario> Usuarios { get; set; } = new();
        public List<Rol> Roles { get; set; } = new();

        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }
        public bool PuedeGestionarRoles { get; set; }

        public void OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            // Verificar permisos
            PuedeCrear = PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Create", "crear");
            PuedeEditar = PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Edit", "editar");
            PuedeEliminar = PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Delete", "eliminar");
            PuedeGestionarRoles = currentRol == "Admin";

            Usuarios = _context.Usuarios.OrderBy(u => u.Username).ToList();
            Roles = _context.Roles.OrderBy(r => r.Nombre).ToList();
        }
    }
}