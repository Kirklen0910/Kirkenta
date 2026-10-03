using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Finanzas.Cuentas
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CuentaFinanciera> Cuentas { get; set; } = new();
        public List<Moneda> Monedas { get; set; } = new();

        public decimal SaldoCajas { get; set; }
        public decimal SaldoBancos { get; set; }
        public decimal SaldoTotal { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "Cuentas", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Index");
            }

            PuedeCrear = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CuentasCreate", "crear");
            PuedeEditar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CuentasEdit", "editar");
            PuedeEliminar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CuentasDelete", "eliminar");

            Cuentas = _context.CuentasFinancieras
                .AsNoTracking()
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToList();

            Monedas = _context.Monedas.Where(m => m.Activa).ToList();

            SaldoCajas = Cuentas.Where(c => c.Tipo == "Caja").Sum(c => c.SaldoActual);
            SaldoBancos = Cuentas.Where(c => c.Tipo == "Banco").Sum(c => c.SaldoActual);
            SaldoTotal = SaldoCajas + SaldoBancos;

            return Page();
        }
    }
}