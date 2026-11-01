using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.DevolucionesProveedor
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        private const int TamanoPagina = 50;

        [BindProperty(SupportsGet = true, Name = "p")]
        public int Pagina { get; set; } = 1;

        public PagerInfo Pager { get; set; } = new();

        public List<DevolucionProveedor> Devoluciones { get; set; } = new();
        public Dictionary<int, string> Proveedores { get; set; } = new();
        public Dictionary<int, string> Ordenes { get; set; } = new();
        public bool PuedeCrear { get; set; }

        public IActionResult OnGet()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Compras", "DevolucionesProveedor", "ver"))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            PuedeCrear = PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Compras", "DevolucionesProveedorCreate", "crear");
            Pager = new PagerInfo(Pagina, TamanoPagina, _context.DevolucionesProveedor.Count());
            Devoluciones = _context.DevolucionesProveedor.OrderByDescending(d => d.Fecha)
                .Skip(Pager.Skip).Take(TamanoPagina).ToList();
            var idsProv = Devoluciones.Select(d => d.ProveedorId).Distinct().ToList();
            var idsOrd = Devoluciones.Select(d => d.OrdenCompraId).Distinct().ToList();
            Proveedores = _context.Proveedores.Where(p => idsProv.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Nombre);
            Ordenes = _context.OrdenesCompra.Where(o => idsOrd.Contains(o.Id)).ToDictionary(o => o.Id, o => o.Numero);
            return Page();
        }
    }
}
