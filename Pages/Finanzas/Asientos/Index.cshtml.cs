using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.Asientos
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
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        public List<AsientoContable> Asientos { get; set; } = new();
        public Dictionary<int, decimal> Totales { get; set; } = new();
        public bool PuedeCrear { get; set; }
        public int TotalMes { get; set; }
        public int TotalAnulados { get; set; }

        public IActionResult OnGet()
        {
            var user = User.Identity?.Name;
            if (!PermisoHelper.UsuarioTienePermiso(_context, user, "Finanzas", "Asientos", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver asientos contables";
                return RedirectToPage("/Index");
            }
            PuedeCrear = PermisoHelper.UsuarioTienePermiso(_context, user, "Finanzas", "Asientos", "crear");

            var query = _context.AsientosContables.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Estado)) query = query.Where(a => a.Estado == Estado);
            if (Desde.HasValue) query = query.Where(a => a.Fecha >= Desde.Value.Date);
            if (Hasta.HasValue) query = query.Where(a => a.Fecha < Hasta.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                query = query.Where(a => a.Numero.Contains(s) || a.Concepto.Contains(s) || (a.Referencia != null && a.Referencia.Contains(s)));
            }

            Pager = new PagerInfo(Pagina, 50, query.Count());
            Asientos = query.OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id)
                .Skip(Pager.Skip).Take(Pager.PageSize).ToList();

            var ids = Asientos.Select(a => a.Id).ToList();
            Totales = _context.DetallesAsiento.Where(d => ids.Contains(d.AsientoContableId))
                .GroupBy(d => d.AsientoContableId)
                .Select(g => new { g.Key, Total = g.Sum(x => x.Debe) })
                .ToDictionary(x => x.Key, x => x.Total);

            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            TotalMes = _context.AsientosContables.Count(a => a.Estado == "Activo" && a.Fecha >= inicioMes);
            TotalAnulados = _context.AsientosContables.Count(a => a.Estado == "Anulado");
            return Page();
        }

        public IActionResult OnPostSincronizar()
        {
            var user = User.Identity?.Name;
            if (!PermisoHelper.UsuarioTienePermiso(_context, user, "Finanzas", "Asientos", "crear"))
            {
                TempData["Error"] = "No tienes permiso para generar asientos";
                return RedirectToPage();
            }

            var usuarioId = _context.Usuarios.Where(u => u.Username == user).Select(u => (int?)u.Id).FirstOrDefault();
            var r = AsientoAutomaticoHelper.Sincronizar(_context, usuarioId);

            var msg = $"Asientos automáticos: {r.Creados} nuevos, {r.Reemplazados} reemplazados, {r.Anulados} anulados.";
            if (r.SinCuenta > 0) msg += $" {r.SinCuenta} movimiento(s) sin cuenta contable asignada (revisa el plan de cuentas y las categorías).";
            if (r.HayMasPendientes) msg += " Quedan más pendientes: vuelve a presionar el botón.";
            TempData["Success"] = msg;
            return RedirectToPage();
        }
    }
}
