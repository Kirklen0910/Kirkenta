using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Movimientos
{
    public class AnularModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AnularModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public MovimientoFinanciero Movimiento { get; set; } = new();
        public string CuentaNombre { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Debes indicar el motivo de anulación")]
        [StringLength(500)]
        public string Motivo { get; set; } = "";

        /// <summary>
        /// Los movimientos que nacen de un documento se anulan desde él: si se anulan aquí, el documento seguiría
        /// mostrándose como cobrado o pagado (una nómina «pagada» sin egreso, un vale que se le descuenta al empleado
        /// sin haberlo recibido, un cierre cuyo depósito ya no existe).
        /// </summary>
        private static readonly string[] OrigenesDeDocumento =
            { "Venta", "Factura", "Devolucion", "PagoProveedor", "Apertura", "AperturaCaja", "CierreCaja", "Nomina", "NominaRetencion", "Vale", "Depreciacion" };

        private static bool MovimientoAnulable(MovimientoFinanciero mov, out string? motivo)
        {
            motivo = null;
            if (OrigenesDeDocumento.Contains(mov.Origen))
            {
                motivo = $"El movimiento {mov.Numero} viene de un documento ({mov.Origen}). Anula o revierte el documento original para que todo quede consistente.";
                return false;
            }
            return true;
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "MovimientosAnular", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para anular movimientos";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            var mov = _context.MovimientosFinancieros.FirstOrDefault(m => m.Id == id);
            if (mov == null)
            {
                TempData["Error"] = "Movimiento no encontrado";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            if (mov.Estado != "Activo")
            {
                TempData["Error"] = "Este movimiento ya fue anulado";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            if (!MovimientoAnulable(mov, out var motivoBloqueo))
            {
                TempData["Error"] = motivoBloqueo;
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            // ===== VALIDACIÓN: período contable cerrado =====
            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, mov.Fecha);
            if (!periodoOk)
            {
                TempData["Error"] = $"No puedes anular este movimiento porque pertenece a un período cerrado. {periodoError}";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            Movimiento = mov;
            CuentaNombre = _context.CuentasFinancieras
                .FirstOrDefault(c => c.Id == mov.CuentaId)?.Nombre ?? "—";

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "MovimientosAnular", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para anular movimientos";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            if (string.IsNullOrWhiteSpace(Motivo))
            {
                ModelState.AddModelError("Motivo", "Debes indicar el motivo");
            }

            var mov = _context.MovimientosFinancieros.FirstOrDefault(m => m.Id == id);
            if (mov == null || mov.Estado != "Activo")
            {
                TempData["Error"] = "Movimiento no válido";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            if (!MovimientoAnulable(mov, out var motivoBloqueo))
            {
                TempData["Error"] = motivoBloqueo;
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            // ===== VALIDACIÓN: período contable cerrado =====
            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, mov.Fecha);
            if (!periodoOk)
            {
                TempData["Error"] = $"No puedes anular este movimiento porque pertenece a un período cerrado. {periodoError}";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            if (!ModelState.IsValid)
            {
                Movimiento = mov;
                CuentaNombre = _context.CuentasFinancieras
                    .FirstOrDefault(c => c.Id == mov.CuentaId)?.Nombre ?? "—";
                return Page();
            }

            // Primero se marca como anulado y DESPUÉS se revierte el saldo
            // (SaldoHelper.Revertir no hace nada con un movimiento que sigue Activo).
            using var tx = _context.Database.BeginTransaction();

            // Reclamo atómico: si dos personas (o dos clics) anulan a la vez, solo una lo logra
            // y el saldo de la cuenta no se revierte dos veces.
            if (_context.Database.IsRelational())
            {
                var filas = _context.MovimientosFinancieros.Where(m => m.Id == id && m.Estado == "Activo")
                    .ExecuteUpdate(u => u.SetProperty(m => m.Estado, "Anulado"));
                if (filas == 0)
                {
                    tx.Rollback();
                    _context.ChangeTracker.Clear();
                    TempData["Error"] = "Este movimiento ya fue anulado";
                    return RedirectToPage("/Finanzas/Movimientos/Index");
                }
                _context.Entry(mov).Reload();
            }

            mov.Estado = "Anulado";
            mov.MotivoAnulacion = Motivo;
            mov.FechaAnulacion = DateTime.Now;
            mov.UsuarioAnuloId = currentUser?.Id;

            SaldoHelper.Revertir(_context, mov);

            // Un gasto de caja chica aprobado queda anulado junto con su movimiento
            if (mov.Origen == "CajaChica" && mov.OrigenId.HasValue)
            {
                var gasto = _context.GastosCajaChica.FirstOrDefault(g => g.Id == mov.OrigenId.Value && g.MovimientoFinancieroId == mov.Id);
                if (gasto != null)
                {
                    gasto.Estado = "Anulado";
                    gasto.MotivoRechazo = $"Movimiento {mov.Numero} anulado: {Motivo}";
                    gasto.FechaResolucion = DateTime.Now;
                }
            }

            _context.SaveChanges();
            tx.Commit();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Anular movimiento financiero",
                $"Anuló el movimiento {mov.Numero} por L. {mov.Monto:N2}. Motivo: {Motivo}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Movimiento {mov.Numero} anulado";
            return RedirectToPage("/Finanzas/Movimientos/Index");
        }
    }
}