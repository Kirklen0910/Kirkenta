using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.UnidadesMedida
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
            [StringLength(50)]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "La abreviatura es obligatoria")]
            [StringLength(10)]
            public string Abreviatura { get; set; } = string.Empty;

            [StringLength(200)]
            public string? Descripcion { get; set; }

            public bool Activa { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/UnidadesMedida/Index");
            }
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

            if (_context.UnidadesMedida.Any(u => u.Nombre.ToLower() == Input.Nombre.ToLower()))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe una unidad con este nombre");
                return Page();
            }

            var unidad = new UnidadMedida
            {
                Nombre = Input.Nombre,
                Abreviatura = Input.Abreviatura,
                Descripcion = Input.Descripcion,
                Activa = Input.Activa,
                FechaCreacion = DateTime.Now
            };

            _context.UnidadesMedida.Add(unidad);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear unidad de medida",
                $"Creó la unidad '{unidad.Nombre} ({unidad.Abreviatura})'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Unidad '{unidad.Nombre}' creada correctamente";
            return RedirectToPage("/UnidadesMedida/Index");
        }
    }
}