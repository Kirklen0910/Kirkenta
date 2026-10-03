using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.Cuentas
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public CuentaFinanciera Cuenta { get; set; } = new();
        public int MovimientosAsociados { get; set; }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CuentasDelete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar cuentas";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }

            var c = _context.CuentasFinancieras.FirstOrDefault(x => x.Id == id);
            if (c == null)
            {
                TempData["Error"] = "Cuenta no encontrada";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }

            MovimientosAsociados = _context.MovimientosFinancieros.Count(m => m.CuentaId == id || m.CuentaDestinoId == id);

            if (MovimientosAsociados > 0)
            {
                TempData["Error"] = $"No se puede eliminar: la cuenta tiene {MovimientosAsociados} movimientos. Desactívala en su lugar.";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }

            Cuenta = c;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CuentasDelete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar cuentas";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }

            var c = _context.CuentasFinancieras.FirstOrDefault(x => x.Id == id);
            if (c == null)
            {
                TempData["Error"] = "Cuenta no encontrada";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }

            if (_context.MovimientosFinancieros.Any(m => m.CuentaId == id || m.CuentaDestinoId == id))
            {
                TempData["Error"] = "No se puede eliminar: la cuenta tiene movimientos asociados";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }

            var nombre = c.Nombre;
            _context.CuentasFinancieras.Remove(c);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar cuenta financiera",
                $"Eliminó la cuenta '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cuenta '{nombre}' eliminada";
            return RedirectToPage("/Finanzas/Cuentas/Index");
        }
    }
}