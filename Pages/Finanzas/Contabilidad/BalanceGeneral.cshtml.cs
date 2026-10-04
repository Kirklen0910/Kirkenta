using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.Contabilidad
{
    public class BalanceGeneralModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BalanceGeneralModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public DateTime FechaCorte { get; set; }

        public ContabilidadHelper.BalanceGeneral Reporte { get; set; } = new();
        public ConfiguracionEmpresa? Empresa { get; set; }

        public IActionResult OnGet(DateTime? fecha)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ContabilidadBalanceGeneral", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Index");
            }

            FechaCorte = fecha ?? DateTime.Today;

            Reporte = ContabilidadHelper.GenerarBalanceGeneral(_context, FechaCorte);
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault();

            return Page();
        }
    }
}