using Microsoft.AspNetCore.Mvc;
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

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Tipo { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Estado { get; set; }

        [BindProperty(SupportsGet = true, Name = "p")]
        public int Pagina { get; set; } = 1;

        public PagerInfo Pager { get; set; } = new();
        public List<Cliente> Clientes { get; set; } = new();
        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }
        public bool PuedeExportar { get; set; }
        public bool PuedeImportar { get; set; }
        public string EmpresaNombre { get; set; } = "Mi Empresa";

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Clientes", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver clientes";
                return RedirectToPage("/Index");
            }


            PuedeCrear = PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Clientes", "crear") || currentRol == "Admin";
            PuedeEditar = PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Clientes", "editar") || currentRol == "Admin";
            PuedeEliminar = PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Clientes", "eliminar") || currentRol == "Admin";
            PuedeExportar = PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "ClientesExport", "ver") || currentRol == "Admin";
            PuedeImportar = PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "ClientesImport", "crear") || currentRol == "Admin";

            EmpresaNombre = _context.ConfiguracionEmpresa.Select(e => e.Nombre).FirstOrDefault() ?? "Mi Empresa";

            var query = _context.Clientes.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                query = query.Where(c => c.Nombre.Contains(s) ||
                                         (c.RTN != null && c.RTN.Contains(s)) ||
                                         (c.Email != null && c.Email.Contains(s)) ||
                                         (c.Codigo != null && c.Codigo.Contains(s)));
            }
            if (!string.IsNullOrWhiteSpace(Tipo)) query = query.Where(c => c.TipoCliente == Tipo);
            if (Estado == "activo") query = query.Where(c => c.Activo);
            else if (Estado == "inactivo") query = query.Where(c => !c.Activo);

            Pager = new PagerInfo(Pagina, 50, query.Count());
            Clientes = query.OrderBy(c => c.Nombre).Skip(Pager.Skip).Take(Pager.PageSize).ToList();

            return Page();
        }
    }
}