using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Categorias
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Categoria Categoria { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Categorias/Index");
            }

            var cat = _context.Categorias.FirstOrDefault(c => c.Id == id);
            if (cat == null)
            {
                TempData["Error"] = "Categoría no encontrada";
                return RedirectToPage("/Categorias/Index");
            }

            Categoria = cat;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Categorias/Index");
            }

            var cat = _context.Categorias.FirstOrDefault(c => c.Id == id);
            if (cat == null)
            {
                TempData["Error"] = "Categoría no encontrada";
                return RedirectToPage("/Categorias/Index");
            }

            // Desasignar productos
            var productos = _context.Productos.Where(p => p.CategoriaId == id).ToList();
            foreach (var p in productos)
            {
                p.CategoriaId = null;
            }

            var nombre = cat.Nombre;
            _context.Categorias.Remove(cat);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar categoría",
                $"Eliminó la categoría '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Categoría '{nombre}' eliminada";
            return RedirectToPage("/Categorias/Index");
        }
    }
}