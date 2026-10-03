using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Proveedores
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Proveedor> Proveedores { get; set; } = new();
        public List<Moneda> Monedas { get; set; } = new();
        public bool PuedeExportar { get; set; }
        public bool PuedeImportar { get; set; }

        public void OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "Proveedores", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return;
            }

            PuedeExportar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Compras", "ProveedoresExport", "ver");
            PuedeImportar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Compras", "ProveedoresImport", "crear");

            Proveedores = _context.Proveedores.OrderBy(p => p.Nombre).ToList();
            Monedas = _context.Monedas.Where(m => m.Activa).ToList();
        }
    }
}