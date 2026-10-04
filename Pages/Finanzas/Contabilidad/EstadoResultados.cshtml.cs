using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.Contabilidad
{
    public class EstadoResultadosModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EstadoResultadosModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }

        public ContabilidadHelper.EstadoResultados Reporte { get; set; } = new();
        public int MovimientosSinCuenta { get; set; }
        public ConfiguracionEmpresa? Empresa { get; set; }

        public IActionResult OnGet(DateTime? desde, DateTime? hasta)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ContabilidadEstadoResultados", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Index");
            }

            // Por defecto: mes actual
            var hoy = DateTime.Today;
            Desde = desde ?? new DateTime(hoy.Year, hoy.Month, 1);
            Hasta = hasta ?? hoy;

            Reporte = ContabilidadHelper.GenerarEstadoResultados(_context, Desde, Hasta);
            MovimientosSinCuenta = ContabilidadHelper.ContarSinPlanCuenta(_context, Desde, Hasta);
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault();

            return Page();
        }
    }
}