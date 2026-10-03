using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Productos
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Producto> Productos { get; set; } = new();
        public List<Categoria> Categorias { get; set; } = new();
        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }
        public bool PuedeExportar { get; set; }
        public bool PuedeImportar { get; set; }

        public void OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            PuedeCrear = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "crear");
            PuedeEditar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "editar");
            PuedeEliminar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "eliminar");
            PuedeExportar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "ProductosExport", "ver");
            PuedeImportar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "ProductosImport", "crear");

            Productos = _context.Productos.OrderBy(p => p.Nombre).ToList();
            Categorias = _context.Categorias.OrderBy(c => c.Nombre).ToList();
        }
    }
}