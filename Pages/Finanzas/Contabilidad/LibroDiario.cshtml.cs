using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.Contabilidad
{
    public class LibroDiarioModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LibroDiarioModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class Partida
        {
            public AsientoContable Asiento { get; set; } = new();
            public List<DetalleAsiento> Lineas { get; set; } = new();
        }

        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        public List<Partida> Partidas { get; set; } = new();
        public Dictionary<int, PlanCuenta> Cuentas { get; set; } = new();
        public decimal TotalDebe { get; set; }
        public decimal TotalHaber { get; set; }

        public IActionResult OnGet()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Finanzas", "LibroDiario", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver el libro diario";
                return RedirectToPage("/Index");
            }

            AsientoAutomaticoHelper.SincronizarSilencioso(_context);

            var hoy = DateTime.Today;
            var desde = (Desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
            var hasta = (Hasta ?? hoy).Date;
            Desde = desde;
            Hasta = hasta;

            var query = _context.AsientosContables
                .Where(a => a.Estado == "Activo" && a.Fecha >= desde && a.Fecha < hasta.AddDays(1));

            var idsRango = query.Select(a => a.Id).ToList();
            var detallesRango = _context.DetallesAsiento.Where(d => idsRango.Contains(d.AsientoContableId)).ToList();
            TotalDebe = detallesRango.Sum(d => d.Debe);
            TotalHaber = detallesRango.Sum(d => d.Haber);

            Pager = new PagerInfo(Pagina, 30, idsRango.Count);
            var asientos = query.OrderBy(a => a.Fecha).ThenBy(a => a.Id).Skip(Pager.Skip).Take(Pager.PageSize).ToList();
            var ids = asientos.Select(a => a.Id).ToList();
            var lineas = detallesRango.Where(d => ids.Contains(d.AsientoContableId)).OrderBy(d => d.Id).ToList();

            var cuentaIds = lineas.Select(l => l.PlanCuentaId).Distinct().ToList();
            Cuentas = _context.PlanCuentas.Where(c => cuentaIds.Contains(c.Id)).ToDictionary(c => c.Id);

            Partidas = asientos.Select(a => new Partida
            {
                Asiento = a,
                Lineas = lineas.Where(l => l.AsientoContableId == a.Id).ToList()
            }).ToList();
            return Page();
        }
    }
}
