using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Categorias
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

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(100)]
            public string Nombre { get; set; } = string.Empty;

            [StringLength(300)]
            public string? Descripcion { get; set; }

            public string Color { get; set; } = "#4f46e5";
            public bool Activa { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Categorias/Index");
            }
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

            if (_context.Categorias.Any(c => c.Nombre.ToLower() == Input.Nombre.ToLower()))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe una categoría con este nombre");
                return Page();
            }

            var categoria = new Categoria
            {
                Nombre = Input.Nombre,
                Descripcion = Input.Descripcion,
                Color = Input.Color,
                Activa = Input.Activa,
                FechaCreacion = DateTime.Now
            };

            _context.Categorias.Add(categoria);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear categoría",
                $"Creó la categoría '{categoria.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Categoría '{categoria.Nombre}' creada correctamente";
            return RedirectToPage("/Categorias/Index");
        }
    }
}