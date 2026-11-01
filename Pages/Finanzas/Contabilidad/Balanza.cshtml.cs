using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.Contabilidad
{
    public class BalanzaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BalanzaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class Fila
        {
            public string Codigo { get; set; } = "";
            public string Nombre { get; set; } = "";
            public decimal InicialDeudor { get; set; }
            public decimal InicialAcreedor { get; set; }
            public decimal Debe { get; set; }
            public decimal Haber { get; set; }
            public decimal FinalDeudor { get; set; }
            public decimal FinalAcreedor { get; set; }
        }

        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }

        public List<Fila> Filas { get; set; } = new();
        public Fila Totales { get; set; } = new();
        public bool Cuadra => Totales.Debe == Totales.Haber && Totales.FinalDeudor == Totales.FinalAcreedor;

        public IActionResult OnGet()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Finanzas", "Balanza", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver la balanza";
                return RedirectToPage("/Index");
            }

            AsientoAutomaticoHelper.SincronizarSilencioso(_context);

            var hoy = DateTime.Today;
            var desde = (Desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
            var hasta = (Hasta ?? hoy).Date;
            Desde = desde;
            Hasta = hasta;

            var movimientos = (from d in _context.DetallesAsiento
                               join a in _context.AsientosContables on d.AsientoContableId equals a.Id
                               where a.Estado == "Activo" && a.Fecha < hasta.AddDays(1)
                               select new { d.PlanCuentaId, a.Fecha, d.Debe, d.Haber }).ToList();

            var cuentaIds = movimientos.Select(m => m.PlanCuentaId).Distinct().ToList();
            var cuentas = _context.PlanCuentas.Where(c => cuentaIds.Contains(c.Id)).OrderBy(c => c.Codigo).ToList();

            foreach (var c in cuentas)
            {
                var propios = movimientos.Where(m => m.PlanCuentaId == c.Id).ToList();
                var inicial = propios.Where(m => m.Fecha < desde).Sum(m => m.Debe - m.Haber);
                var debe = propios.Where(m => m.Fecha >= desde).Sum(m => m.Debe);
                var haber = propios.Where(m => m.Fecha >= desde).Sum(m => m.Haber);
                var final = inicial + debe - haber;

                Filas.Add(new Fila
                {
                    Codigo = c.Codigo,
                    Nombre = c.Nombre,
                    InicialDeudor = inicial > 0 ? inicial : 0,
                    InicialAcreedor = inicial < 0 ? -inicial : 0,
                    Debe = debe,
                    Haber = haber,
                    FinalDeudor = final > 0 ? final : 0,
                    FinalAcreedor = final < 0 ? -final : 0
                });
            }

            Totales = new Fila
            {
                InicialDeudor = Filas.Sum(f => f.InicialDeudor),
                InicialAcreedor = Filas.Sum(f => f.InicialAcreedor),
                Debe = Filas.Sum(f => f.Debe),
                Haber = Filas.Sum(f => f.Haber),
                FinalDeudor = Filas.Sum(f => f.FinalDeudor),
                FinalAcreedor = Filas.Sum(f => f.FinalAcreedor)
            };
            return Page();
        }
    }
}
