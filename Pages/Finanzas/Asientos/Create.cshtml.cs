using System.ComponentModel.DataAnnotations;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.Asientos
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class LineaInput
        {
            public int PlanCuentaId { get; set; }
            public decimal? Debe { get; set; }
            public decimal? Haber { get; set; }
            [StringLength(200)] public string? Detalle { get; set; }
        }

        public class InputModel
        {
            public DateTime Fecha { get; set; } = DateTime.Today;
            [Required(ErrorMessage = "El concepto es obligatorio"), StringLength(300)]
            public string Concepto { get; set; } = "";
            [StringLength(100)] public string? Referencia { get; set; }
            public List<LineaInput> Lineas { get; set; } = new();
        }

        [BindProperty] public InputModel Input { get; set; } = new();
        public List<PlanCuenta> Cuentas { get; set; } = new();
        public string NumeroPreview { get; set; } = "";

        private bool Autorizado() =>
            PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Finanzas", "Asientos", "crear");

        private void CargarListas()
        {
            Cuentas = _context.PlanCuentas.Where(c => c.Activa && c.EsMovimiento).OrderBy(c => c.Codigo).ToList();
            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "AsientoContable");
        }

        public IActionResult OnGet()
        {
            if (!Autorizado())
            {
                TempData["Error"] = "No tienes permiso para crear asientos";
                return RedirectToPage("/Finanzas/Asientos/Index");
            }
            CargarListas();
            Input.Lineas.Add(new LineaInput());
            Input.Lineas.Add(new LineaInput());
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!Autorizado())
            {
                TempData["Error"] = "No tienes permiso para crear asientos";
                return RedirectToPage("/Finanzas/Asientos/Index");
            }
            CargarListas();

            var lineas = Input.Lineas.Where(l => l.PlanCuentaId > 0 || (l.Debe ?? 0) > 0 || (l.Haber ?? 0) > 0).ToList();
            var cuentasValidas = Cuentas.Select(c => c.Id).ToHashSet();

            if (lineas.Count < 2) ModelState.AddModelError(string.Empty, "Un asiento necesita al menos dos líneas");
            if (lineas.Any(l => l.PlanCuentaId <= 0 || !cuentasValidas.Contains(l.PlanCuentaId)))
                ModelState.AddModelError(string.Empty, "Todas las líneas deben tener una cuenta válida (cuentas de movimiento)");
            if (lineas.Any(l => (l.Debe ?? 0) < 0 || (l.Haber ?? 0) < 0))
                ModelState.AddModelError(string.Empty, "Los montos no pueden ser negativos");
            if (lineas.Any(l => (l.Debe ?? 0) > 0 && (l.Haber ?? 0) > 0))
                ModelState.AddModelError(string.Empty, "Una línea no puede tener Debe y Haber al mismo tiempo");
            if (lineas.Any(l => (l.Debe ?? 0) == 0 && (l.Haber ?? 0) == 0))
                ModelState.AddModelError(string.Empty, "Cada línea debe tener un monto en Debe o en Haber");

            var totalDebe = Math.Round(lineas.Sum(l => l.Debe ?? 0), 2);
            var totalHaber = Math.Round(lineas.Sum(l => l.Haber ?? 0), 2);
            if (totalDebe != totalHaber)
                ModelState.AddModelError(string.Empty, $"El asiento no cuadra: Debe L. {totalDebe:N2} ≠ Haber L. {totalHaber:N2}");
            if (totalDebe <= 0)
                ModelState.AddModelError(string.Empty, "El asiento no puede estar en cero");

            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, Input.Fecha);
            if (!periodoOk) ModelState.AddModelError(string.Empty, periodoError!);

            if (!ModelState.IsValid)
            {
                while (Input.Lineas.Count < 2) Input.Lineas.Add(new LineaInput());
                return Page();
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            using var tx = _context.Database.BeginTransaction();
            var asiento = new AsientoContable
            {
                Numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "AsientoContable"),
                Fecha = Input.Fecha.Date,
                Concepto = Input.Concepto.Trim(),
                Referencia = Input.Referencia,
                Estado = "Activo",
                UsuarioId = usuario?.Id
            };
            _context.AsientosContables.Add(asiento);
            _context.SaveChanges();

            foreach (var l in lineas)
            {
                _context.DetallesAsiento.Add(new DetalleAsiento
                {
                    AsientoContableId = asiento.Id,
                    PlanCuentaId = l.PlanCuentaId,
                    Debe = Math.Round(l.Debe ?? 0, 2),
                    Haber = Math.Round(l.Haber ?? 0, 2),
                    Detalle = l.Detalle
                });
            }
            _context.SaveChanges();
            tx.Commit();

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Crear asiento contable",
                $"Registró el asiento {asiento.Numero} por L. {totalDebe:N2}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Asiento {asiento.Numero} registrado";
            return RedirectToPage("/Finanzas/Asientos/Details", new { id = asiento.Id });
        }
    }
}
