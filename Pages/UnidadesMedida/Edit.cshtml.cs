using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.UnidadesMedida
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
            [StringLength(50)]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "La abreviatura es obligatoria")]
            [StringLength(10)]
            public string Abreviatura { get; set; } = string.Empty;

            [StringLength(200)]
            public string? Descripcion { get; set; }

            public bool Activa { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            var unidad = _context.UnidadesMedida.FirstOrDefault(u => u.Id == id);
            if (unidad == null)
            {
                TempData["Error"] = "Unidad no encontrada";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            Input = new InputModel
            {
                Id = unidad.Id,
                Nombre = unidad.Nombre,
                Abreviatura = unidad.Abreviatura,
                Descripcion = unidad.Descripcion,
                Activa = unidad.Activa
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            if (!ModelState.IsValid) return Page();

            var unidad = _context.UnidadesMedida.FirstOrDefault(u => u.Id == Input.Id);
            if (unidad == null)
            {
                TempData["Error"] = "Unidad no encontrada";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            if (_context.UnidadesMedida.Any(u => u.Nombre.ToLower() == Input.Nombre.ToLower() && u.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe una unidad con este nombre");
                return Page();
            }

            unidad.Nombre = Input.Nombre;
            unidad.Abreviatura = Input.Abreviatura;
            unidad.Descripcion = Input.Descripcion;
            unidad.Activa = Input.Activa;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar unidad de medida",
                $"Editó la unidad '{unidad.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Unidad '{unidad.Nombre}' actualizada";
            return RedirectToPage("/UnidadesMedida/Index");
        }
    }
}