using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Impuestos
{
    /// <summary>
    /// Registra el pago mensual del ISV a la SAR. El egreso se aplica contra "ISV por pagar" (2.1.02.001),
    /// no es un gasto: baja el pasivo y la caja/banco, y no toca la utilidad.
    /// </summary>
    public class PagarIsvModel : PageModel
    {
        public const string OrigenPago = "PagoISV";

        private readonly ApplicationDbContext _context;
        public PagarIsvModel(ApplicationDbContext context) { _context = context; }

        [BindProperty(SupportsGet = true)] public int? Anio { get; set; }
        [BindProperty(SupportsGet = true)] public int? Mes { get; set; }
        [BindProperty(SupportsGet = true)] public decimal? SaldoFavor { get; set; }
        [BindProperty(SupportsGet = true)] public decimal? Retenciones { get; set; }

        [BindProperty, Range(0.01, 1000000000, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal Monto { get; set; }

        [BindProperty, Required(ErrorMessage = "Selecciona la cuenta de donde sale el pago")]
        public int? CuentaId { get; set; }

        [BindProperty, DataType(DataType.Date)]
        public DateTime FechaPago { get; set; } = DateTime.Today;

        [BindProperty, StringLength(100)]
        public string? Referencia { get; set; }

        [BindProperty]
        public bool PagarDeTodosModos { get; set; }

        public ResumenISV Resumen { get; set; } = new();
        public List<SelectListItem> Cuentas { get; set; } = new();
        public List<MovimientoFinanciero> PagosPrevios { get; set; } = new();
        public decimal PagadoPeriodo => PagosPrevios.Sum(p => p.Monto);
        public string NombrePeriodo => $"{DeclaracionISVModel_Meses[Resumen.Desde.Month - 1]} de {Resumen.Desde.Year}";
        private static readonly string[] DeclaracionISVModel_Meses = { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };

        private bool TienePermiso()
        {
            var user = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var rol = user?.Rol ?? "Pendiente";
            return rol == "Admin" || PermisoHelper.TienePermiso(_context, rol, "Finanzas", "MovimientosCreate", "crear");
        }

        private int CodigoPeriodo => (Anio ?? 0) * 100 + (Mes ?? 0);

        private void Cargar()
        {
            var baseMes = DateTime.Today.AddMonths(-1);
            var anio = Anio is >= 2000 and <= 2100 ? Anio.Value : baseMes.Year;
            var mes = Mes is >= 1 and <= 12 ? Mes.Value : baseMes.Month;
            Anio = anio; Mes = mes;

            var desde = new DateTime(anio, mes, 1);
            var hastaExcl = desde.AddMonths(1);
            var ventas = DeclaracionISVHelper.Ventas(_context, desde, hastaExcl);
            var compras = DeclaracionISVHelper.Compras(_context, desde, hastaExcl);
            Resumen = DeclaracionISVHelper.Resumir(desde, hastaExcl.AddDays(-1), ventas, compras,
                SaldoFavor ?? 0, Retenciones ?? 0,
                DeclaracionISVHelper.DevolucionesDelPeriodo(_context, desde, hastaExcl),
                DeclaracionISVHelper.IsvDevolucionesDelPeriodo(_context, desde, hastaExcl));

            var codigo = CodigoPeriodo;
            PagosPrevios = _context.MovimientosFinancieros
                .Where(m => m.Estado == "Activo" && m.Origen == OrigenPago && m.OrigenId == codigo)
                .OrderBy(m => m.Fecha).ToList();

            Cuentas = _context.CuentasFinancieras
                .Where(c => c.Activa && (c.Subtipo == null || c.Subtipo != "CuentasPorCobrar"))
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre).ToList()
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = $"{c.Nombre} (L. {c.SaldoActual:N2})" })
                .ToList();
        }

        public IActionResult OnGet()
        {
            if (!TienePermiso()) { TempData["Error"] = "No tienes permiso para registrar pagos de ISV"; return RedirectToPage("/Finanzas/Index"); }
            Cargar();
            Monto = Math.Max(0, Resumen.IsvAPagar - PagadoPeriodo);
            Referencia = $"ISV {Resumen.Desde:yyyy-MM}";
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!TienePermiso()) { TempData["Error"] = "No tienes permiso para registrar pagos de ISV"; return RedirectToPage("/Finanzas/Index"); }
            Cargar();

            if (!ModelState.IsValid) return Page();

            if (PagosPrevios.Count > 0 && !PagarDeTodosModos)
            {
                ModelState.AddModelError("", $"Este período ya tiene pagos registrados por L. {PagadoPeriodo:N2}. Marca la casilla si quieres registrar otro pago.");
                return Page();
            }

            var cuenta = _context.CuentasFinancieras.FirstOrDefault(c => c.Id == CuentaId && c.Activa);
            if (cuenta == null) { ModelState.AddModelError(nameof(CuentaId), "Cuenta no encontrada o inactiva"); return Page(); }
            if (cuenta.SaldoActual < Monto)
            {
                ModelState.AddModelError(nameof(Monto), $"La cuenta '{cuenta.Nombre}' solo tiene L. {cuenta.SaldoActual:N2} disponible");
                return Page();
            }

            var user = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            using var tx = _context.Database.BeginTransaction();
            try
            {
                var categoria = MovimientoManualHelper.ObtenerOCrearCategoria(
                    _context, "Egreso", "Pago de ISV (SAR)", "2.1.02.001", "#991b1b");

                var (mov, error) = MovimientoManualHelper.Crear(
                    _context, "Egreso", FechaPago.Date == DateTime.Today ? DateTime.Now : FechaPago.Date.AddHours(12),
                    cuenta.Id, null, categoria.Id, Monto,
                    $"Pago de ISV a la SAR — {NombrePeriodo}", string.IsNullOrWhiteSpace(Referencia) ? null : Referencia.Trim(),
                    OrigenPago, CodigoPeriodo, user?.Id);

                if (mov == null)
                {
                    tx.Rollback();
                    _context.ChangeTracker.Clear();
                    ModelState.AddModelError("", error ?? "No se pudo registrar el pago");
                    Cargar();
                    return Page();
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                _context.ChangeTracker.Clear();
                Console.WriteLine($"[PagarIsv] {ex.Message}");
                ModelState.AddModelError("", "No se pudo registrar el pago: " + ex.Message);
                Cargar();
                return Page();
            }

            TempData["Success"] = $"Pago de ISV de {NombrePeriodo} registrado por L. {Monto:N2}";
            return RedirectToPage("/Finanzas/Impuestos/PagarIsv", new { anio = Anio, mes = Mes, saldoFavor = SaldoFavor, retenciones = Retenciones });
        }
    }
}
