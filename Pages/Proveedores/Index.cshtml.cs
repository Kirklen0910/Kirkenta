using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
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

        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
        [BindProperty(SupportsGet = true)] public string? Condicion { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

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

            var query = _context.Proveedores.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                query = query.Where(p => p.Nombre.Contains(s)
                    || (p.RTN != null && p.RTN.Contains(s))
                    || (p.Email != null && p.Email.Contains(s))
                    || (p.Codigo != null && p.Codigo.Contains(s)));
            }
            if (Estado == "activo") query = query.Where(p => p.Activo);
            else if (Estado == "inactivo") query = query.Where(p => !p.Activo);
            if (!string.IsNullOrWhiteSpace(Condicion)) query = query.Where(p => p.CondicionPago == Condicion);

            Pager = new PagerInfo(Pagina, 50, query.Count());
            Proveedores = query.OrderBy(p => p.Nombre).Skip(Pager.Skip).Take(Pager.PageSize).ToList();
            Monedas = _context.Monedas.Where(m => m.Activa).ToList();
        }
    }
}