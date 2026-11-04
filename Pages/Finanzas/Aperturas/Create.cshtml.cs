using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Aperturas
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<CuentaFinanciera> Cuentas { get; set; } = new();
        /// <summary>Cuentas de donde puede salir fondo extra para abrir (otras cajas y bancos).</summary>
        public List<CuentaFinanciera> CuentasOrigen { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Debes seleccionar una cuenta")]
            public int CuentaId { get; set; }

            [Required(ErrorMessage = "Debes ingresar el saldo inicial")]
            [Range(0, double.MaxValue, ErrorMessage = "El saldo no puede ser negativo")]
            public decimal SaldoInicial { get; set; }

            [StringLength(500)]
            public string? Notas { get; set; }

            /// <summary>Si se abre con más de lo que dice el sistema: de qué cuenta sale la diferencia (null = sobrante).</summary>
            public int? CuentaOrigenFondoId { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "AperturasCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Aperturas/Index");
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "AperturasCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Aperturas/Index");
            }

            CargarDatos();

            // ===== VALIDACIÓN: período contable cerrado =====
            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, DateTime.Today);
            if (!periodoOk)
            {
                ModelState.AddModelError(string.Empty, periodoError!);
            }

            if (!ModelState.IsValid) return Page();

            var (apertura, error) = AperturaHelper.Abrir(
                _context,
                Input.CuentaId,
                Input.SaldoInicial,
                Input.Notas,
                currentUser!.Id,
                Input.CuentaOrigenFondoId
            );

            if (error != null)
            {
                ModelState.AddModelError(string.Empty, error);
                return Page();
            }

            ActividadHelper.Registrar(
                _context,
                currentUser.Id,
                "Apertura de caja",
                $"Abrió la caja {apertura!.Numero} con saldo inicial L. {apertura.SaldoInicial:N2}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Apertura {apertura.Numero} registrada con saldo L. {apertura.SaldoInicial:N2}";
            return RedirectToPage("/Finanzas/Aperturas/Index");
        }

        private void CargarDatos()
        {
            Cuentas = _context.CuentasFinancieras
                .Where(c => c.Tipo == "Caja" && c.Activa && c.Subtipo != "CuentasPorCobrar")
                .OrderBy(c => c.Nombre)
                .ToList();
            CuentasOrigen = _context.CuentasFinancieras
                .Where(c => c.Activa && (c.Tipo == "Caja" || c.Tipo == "Banco") && c.Subtipo != "CuentasPorCobrar")
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToList();
        }
    }
}