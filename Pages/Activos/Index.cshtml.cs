using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Activos
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public static readonly string[] Categorias =
            { "Mobiliario", "Equipo de cómputo", "Vehículos", "Maquinaria", "Otro" };

        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public string? Categoria { get; set; }
        [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        public List<ActivoFijo> Activos { get; set; } = new();
        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }

        public int TotalActivos { get; set; }
        public decimal CostoTotal { get; set; }
        public decimal DepreciacionTotal { get; set; }
        public decimal ValorLibros { get; set; }

        public IActionResult OnGet()
        {
            var user = User.Identity?.Name;
            if (!PermisoHelper.UsuarioTienePermiso(_context, user, "Activos", "Index", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver los activos";
                return RedirectToPage("/Index");
            }
            PuedeCrear = PermisoHelper.UsuarioTienePermiso(_context, user, "Activos", "Index", "crear");
            PuedeEditar = PermisoHelper.UsuarioTienePermiso(_context, user, "Activos", "Index", "editar");

            var query = _context.ActivosFijos.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                query = query.Where(a => a.Codigo.Contains(s) || a.Nombre.Contains(s)
                    || (a.NumeroSerie != null && a.NumeroSerie.Contains(s))
                    || (a.Responsable != null && a.Responsable.Contains(s)));
            }
            if (!string.IsNullOrWhiteSpace(Categoria)) query = query.Where(a => a.Categoria == Categoria);
            if (!string.IsNullOrWhiteSpace(Estado)) query = query.Where(a => a.Estado == Estado);

            Pager = new PagerInfo(Pagina, 50, query.Count());
            Activos = query.OrderBy(a => a.Codigo).Skip(Pager.Skip).Take(Pager.PageSize).ToList();

            var vigentes = _context.ActivosFijos.Where(a => a.Estado == "Activo");
            TotalActivos = vigentes.Count();
            CostoTotal = vigentes.Sum(a => (decimal?)a.CostoAdquisicion) ?? 0;
            DepreciacionTotal = vigentes.Sum(a => (decimal?)a.DepreciacionAcumulada) ?? 0;
            ValorLibros = CostoTotal - DepreciacionTotal;
            return Page();
        }
    }
}
