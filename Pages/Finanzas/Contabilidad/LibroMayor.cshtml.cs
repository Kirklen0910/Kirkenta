using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.Contabilidad
{
    public class LibroMayorModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LibroMayorModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class Linea
        {
            public DateTime Fecha { get; set; }
            public int AsientoId { get; set; }
            public string Numero { get; set; } = "";
            public string Concepto { get; set; } = "";
            public string? Detalle { get; set; }
            public decimal Debe { get; set; }
            public decimal Haber { get; set; }
            public decimal Saldo { get; set; }
        }

        [BindProperty(SupportsGet = true)] public int? CuentaId { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }

        public List<PlanCuenta> Cuentas { get; set; } = new();
        public PlanCuenta? Cuenta { get; set; }
        public List<Linea> Lineas { get; set; } = new();
        public decimal SaldoInicial { get; set; }
        public decimal TotalDebe { get; set; }
        public decimal TotalHaber { get; set; }
        public decimal SaldoFinal { get; set; }

        public IActionResult OnGet()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Finanzas", "LibroMayor", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver el libro mayor";
                return RedirectToPage("/Index");
            }

            AsientoAutomaticoHelper.SincronizarSilencioso(_context);

            var hoy = DateTime.Today;
            var desde = (Desde ?? new DateTime(hoy.Year, 1, 1)).Date;
            var hasta = (Hasta ?? hoy).Date;
            Desde = desde;
            Hasta = hasta;

            Cuentas = _context.PlanCuentas.Where(c => c.Activa && c.EsMovimiento).OrderBy(c => c.Codigo).ToList();
            if (!CuentaId.HasValue) return Page();

            Cuenta = _context.PlanCuentas.FirstOrDefault(c => c.Id == CuentaId.Value);
            if (Cuenta == null) return Page();

            var signo = Cuenta.Naturaleza == "Deudora" ? 1m : -1m;
            var cuentaId = Cuenta.Id;

            var movimientos = (from d in _context.DetallesAsiento
                               join a in _context.AsientosContables on d.AsientoContableId equals a.Id
                               where a.Estado == "Activo" && d.PlanCuentaId == cuentaId && a.Fecha < hasta.AddDays(1)
                               orderby a.Fecha, a.Id, d.Id
                               select new { a.Fecha, AsientoId = a.Id, a.Numero, a.Concepto, d.Detalle, d.Debe, d.Haber }).ToList();

            var antes = movimientos.Where(m => m.Fecha < desde).ToList();
            SaldoInicial = signo * (antes.Sum(m => m.Debe) - antes.Sum(m => m.Haber));

            var saldo = SaldoInicial;
            foreach (var m in movimientos.Where(m => m.Fecha >= desde))
            {
                saldo += signo * (m.Debe - m.Haber);
                Lineas.Add(new Linea
                {
                    Fecha = m.Fecha, AsientoId = m.AsientoId, Numero = m.Numero, Concepto = m.Concepto,
                    Detalle = m.Detalle, Debe = m.Debe, Haber = m.Haber, Saldo = saldo
                });
            }
            TotalDebe = Lineas.Sum(l => l.Debe);
            TotalHaber = Lineas.Sum(l => l.Haber);
            SaldoFinal = saldo;
            return Page();
        }
    }
}
