using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.PlanCuentas
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<PlanCuenta> Cuentas { get; set; } = new();

        // KPIs
        public int TotalCuentas { get; set; }
        public int TotalActivas { get; set; }
        public int TotalMovimiento { get; set; }
        public int TotalSinPadre { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "PlanCuentas", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Index");
            }

            PuedeCrear = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "PlanCuentas", "crear");
            PuedeEditar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "PlanCuentas", "editar");
            PuedeEliminar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "PlanCuentas", "eliminar");

            Cuentas = _context.PlanCuentas
                .OrderBy(c => c.Codigo)
                .ToList();

            TotalCuentas = Cuentas.Count;
            TotalActivas = Cuentas.Count(c => c.Activa);
            TotalMovimiento = Cuentas.Count(c => c.EsMovimiento);
            TotalSinPadre = Cuentas.Count(c => c.CodigoPadre == null);

            return Page();
        }
    }
}