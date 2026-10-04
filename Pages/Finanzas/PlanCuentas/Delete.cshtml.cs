using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.PlanCuentas
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public PlanCuenta Cuenta { get; set; } = new();

        public bool TieneHijos { get; set; }
        public int CantidadHijos { get; set; }
        public bool TieneCategorias { get; set; }
        public int CantidadCategorias { get; set; }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "PlanCuentas", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/PlanCuentas/Index");
            }

            var cuenta = _context.PlanCuentas.FirstOrDefault(c => c.Id == id);
            if (cuenta == null)
            {
                TempData["Error"] = "Cuenta no encontrada";
                return RedirectToPage("/Finanzas/PlanCuentas/Index");
            }

            Cuenta = cuenta;

            CantidadHijos = _context.PlanCuentas.Count(c => c.CodigoPadre == cuenta.Codigo);
            TieneHijos = CantidadHijos > 0;

            CantidadCategorias = _context.CategoriasFinancieras.Count(c => c.PlanCuentaId == cuenta.Id);
            TieneCategorias = CantidadCategorias > 0;

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "PlanCuentas", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/PlanCuentas/Index");
            }

            var cuenta = _context.PlanCuentas.FirstOrDefault(c => c.Id == id);
            if (cuenta == null)
            {
                TempData["Error"] = "Cuenta no encontrada";
                return RedirectToPage("/Finanzas/PlanCuentas/Index");
            }

            // Validar que no tenga hijos
            if (_context.PlanCuentas.Any(c => c.CodigoPadre == cuenta.Codigo))
            {
                TempData["Error"] = "No se puede eliminar: la cuenta tiene subcuentas asociadas. Elimínalas primero o desactívala.";
                return RedirectToPage("/Finanzas/PlanCuentas/Index");
            }

            // Validar que no tenga categorías financieras asociadas
            if (_context.CategoriasFinancieras.Any(c => c.PlanCuentaId == cuenta.Id))
            {
                TempData["Error"] = "No se puede eliminar: la cuenta está asociada a categorías financieras. Desactívala en su lugar.";
                return RedirectToPage("/Finanzas/PlanCuentas/Index");
            }

            var codigo = cuenta.Codigo;
            var nombre = cuenta.Nombre;

            _context.PlanCuentas.Remove(cuenta);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar cuenta contable",
                $"Eliminó la cuenta '{codigo} - {nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cuenta '{codigo} - {nombre}' eliminada";
            return RedirectToPage("/Finanzas/PlanCuentas/Index");
        }
    }
}