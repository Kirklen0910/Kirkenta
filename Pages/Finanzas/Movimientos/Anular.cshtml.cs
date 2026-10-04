using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
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

            // Revertir saldo
            SaldoHelper.Revertir(_context, mov);

            mov.Estado = "Anulado";
            mov.MotivoAnulacion = Motivo;
            mov.FechaAnulacion = DateTime.Now;
            mov.UsuarioAnuloId = currentUser?.Id;

            _context.SaveChanges();

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