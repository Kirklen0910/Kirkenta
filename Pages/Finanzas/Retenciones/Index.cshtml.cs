using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.Retenciones
{
    /// <summary>Lo que la nómina le debe a la SAR, al IHSS, al RAP y al INFOP, y el registro de cada pago.</summary>
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context) => _context = context;

        public List<RetencionesHelper.FilaResumen> Resumen { get; set; } = new();
        public List<RetencionesHelper.FilaMes> PorMes { get; set; } = new();
        public List<MovimientoFinanciero> Pagos { get; set; } = new();
        public List<CuentaFinanciera> Cuentas { get; set; } = new();
        public Dictionary<int, string> NombresCuenta { get; set; } = new();
        public decimal TotalPendiente { get; set; }
        public bool PuedePagar { get; set; }

        private bool Permiso(string accion) =>
            PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Finanzas", "Retenciones", accion);

        private void Cargar()
        {
            Resumen = RetencionesHelper.Resumen(_context);
            TotalPendiente = Resumen.Sum(f => f.Pendiente);
            PorMes = RetencionesHelper.PorMes(_context);
            Pagos = RetencionesHelper.Pagos(_context);
            var cuentas = _context.CuentasFinancieras.ToList();
            NombresCuenta = cuentas.ToDictionary(c => c.Id, c => c.Nombre);
            Cuentas = cuentas.Where(c => c.Activa && (c.Tipo == "Banco" || c.Tipo == "Caja") && c.Subtipo != "CuentasPorCobrar")
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre).ToList();
            PuedePagar = Permiso("crear");
        }

        public IActionResult OnGet()
        {
            if (!Permiso("ver"))
            {
                TempData["Error"] = "No tienes permiso para ver las retenciones";
                return RedirectToPage("/Index");
            }
            Cargar();
            return Page();
        }

        public IActionResult OnPostPagar(string clave, decimal monto, int cuentaId, DateTime fecha, string? referencia)
        {
            if (!Permiso("crear"))
            {
                TempData["Error"] = "No tienes permiso para registrar pagos";
                return RedirectToPage();
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            using var tx = _context.Database.BeginTransaction();
            var (mov, error) = RetencionesHelper.Pagar(_context, clave, monto, cuentaId, fecha, referencia, usuario?.Id);
            if (mov == null)
            {
                tx.Rollback();
                _context.ChangeTracker.Clear();
                TempData["Error"] = error;
                return RedirectToPage();
            }
            tx.Commit();

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Pago de retenciones",
                $"{mov.Concepto}: L. {mov.Monto:N2}, recibo {mov.Referencia}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Pago registrado: L. {mov.Monto:N2} ({mov.Numero}).";
            return RedirectToPage();
        }
    }
}
