using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Categorias
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(100)]
            public string Nombre { get; set; } = string.Empty;

            [StringLength(300)]
            public string? Descripcion { get; set; }

            public string Color { get; set; } = "#4f46e5";
            public bool Activa { get; set; }
        }

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

            Input = new InputModel
            {
                Id = cat.Id,
                Nombre = cat.Nombre,
                Descripcion = cat.Descripcion,
                Color = cat.Color,
                Activa = cat.Activa
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Categorias/Index");
            }

            if (!ModelState.IsValid) return Page();

            var cat = _context.Categorias.FirstOrDefault(c => c.Id == Input.Id);
            if (cat == null)
            {
                TempData["Error"] = "Categoría no encontrada";
                return RedirectToPage("/Categorias/Index");
            }

            if (_context.Categorias.Any(c => c.Nombre.ToLower() == Input.Nombre.ToLower() && c.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe una categoría con este nombre");
                return Page();
            }

            cat.Nombre = Input.Nombre;
            cat.Descripcion = Input.Descripcion;
            cat.Color = Input.Color;
            cat.Activa = Input.Activa;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar categoría",
                $"Editó la categoría '{cat.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Categoría '{cat.Nombre}' actualizada";
            return RedirectToPage("/Categorias/Index");
        }
    }
}