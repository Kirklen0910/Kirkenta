using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Clientes
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Cliente> Clientes { get; set; } = new();
        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }
        public bool PuedeExportar { get; set; }
        public bool PuedeImportar { get; set; }

        public void OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            PuedeCrear = PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Clientes", "crear") || currentRol == "Admin";
            PuedeEditar = PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Clientes", "editar") || currentRol == "Admin";
            PuedeEliminar = PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Clientes", "eliminar") || currentRol == "Admin";
            PuedeExportar = PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "ClientesExport", "ver") || currentRol == "Admin";
            PuedeImportar = PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "ClientesImport", "crear") || currentRol == "Admin";

            Clientes = _context.Clientes.OrderBy(c => c.Nombre).ToList();
        }
    }
}