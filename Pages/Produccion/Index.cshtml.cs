using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Produccion
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
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        public List<OrdenProduccion> Ordenes { get; set; } = new();
        public Dictionary<int, string> Productos { get; set; } = new();
        public bool PuedeCrear { get; set; }

        public int Planificadas { get; set; }
        public int EnProceso { get; set; }
        public int TerminadasMes { get; set; }
        public decimal UnidadesMes { get; set; }

        public IActionResult OnGet()
        {
            var user = User.Identity?.Name;
            if (!PermisoHelper.UsuarioTienePermiso(_context, user, "Produccion", "Index", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver producción";
                return RedirectToPage("/Index");
            }
            PuedeCrear = PermisoHelper.UsuarioTienePermiso(_context, user, "Produccion", "Index", "crear");

            var query = _context.OrdenesProduccion.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Estado)) query = query.Where(o => o.Estado == Estado);
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                var prodIds = _context.Productos.Where(p => p.Nombre.Contains(s)).Select(p => p.Id).ToList();
                query = query.Where(o => o.Numero.Contains(s) || prodIds.Contains(o.ProductoId));
            }

            Pager = new PagerInfo(Pagina, 50, query.Count());
            Ordenes = query.OrderByDescending(o => o.Fecha).ThenByDescending(o => o.Id)
                .Skip(Pager.Skip).Take(Pager.PageSize).ToList();

            var ids = Ordenes.Select(o => o.ProductoId).Distinct().ToList();
            Productos = _context.Productos.Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Nombre);

            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            Planificadas = _context.OrdenesProduccion.Count(o => o.Estado == "Planificada");
            EnProceso = _context.OrdenesProduccion.Count(o => o.Estado == "EnProceso");
            var terminadas = _context.OrdenesProduccion.Where(o => o.Estado == "Terminada" && o.FechaFin >= inicioMes);
            TerminadasMes = terminadas.Count();
            UnidadesMes = terminadas.Sum(o => (decimal?)o.CantidadProducida) ?? 0;
            return Page();
        }
    }
}
