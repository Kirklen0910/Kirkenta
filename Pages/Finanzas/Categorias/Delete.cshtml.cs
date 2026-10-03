using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.Categorias
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public CategoriaFinanciera Categoria { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CategoriasDelete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Categorias/Index");
            }

            var c = _context.CategoriasFinancieras.FirstOrDefault(x => x.Id == id);
            if (c == null)
            {
                TempData["Error"] = "Categoría no encontrada";
                return RedirectToPage("/Finanzas/Categorias/Index");
            }

            if (c.EsSistema)
            {
                TempData["Error"] = "No se pueden eliminar categorías del sistema";
                return RedirectToPage("/Finanzas/Categorias/Index");
            }

            if (_context.MovimientosFinancieros.Any(m => m.CategoriaId == id))
            {
                TempData["Error"] = "No se puede eliminar: la categoría tiene movimientos asociados. Desactívala en su lugar.";
                return RedirectToPage("/Finanzas/Categorias/Index");
            }

            Categoria = c;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CategoriasDelete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Categorias/Index");
            }

            var c = _context.CategoriasFinancieras.FirstOrDefault(x => x.Id == id);
            if (c == null)
            {
                TempData["Error"] = "Categoría no encontrada";
                return RedirectToPage("/Finanzas/Categorias/Index");
            }

            if (c.EsSistema)
            {
                TempData["Error"] = "No se pueden eliminar categorías del sistema";
                return RedirectToPage("/Finanzas/Categorias/Index");
            }

            if (_context.MovimientosFinancieros.Any(m => m.CategoriaId == id))
            {
                TempData["Error"] = "No se puede eliminar: tiene movimientos asociados";
                return RedirectToPage("/Finanzas/Categorias/Index");
            }

            var nombre = c.Nombre;
            _context.CategoriasFinancieras.Remove(c);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar categoría financiera",
                $"Eliminó la categoría '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Categoría '{nombre}' eliminada";
            return RedirectToPage("/Finanzas/Categorias/Index");
        }
    }
}