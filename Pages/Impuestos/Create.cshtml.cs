using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Impuestos
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

            [Range(0, 100, ErrorMessage = "Debe estar entre 0 y 100")]
            public decimal Porcentaje { get; set; }

            [StringLength(200)]
            public string? Descripcion { get; set; }

            public bool EsPredeterminado { get; set; }
            public bool Activo { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Impuestos/Index");
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
                return RedirectToPage("/Impuestos/Index");
            }

            if (!ModelState.IsValid) return Page();

            if (_context.Impuestos.Any(i => i.Nombre.ToLower() == Input.Nombre.ToLower()))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe un impuesto con este nombre");
                return Page();
            }

            // Si es predeterminado, quitar el anterior
            if (Input.EsPredeterminado)
            {
                var anteriores = _context.Impuestos.Where(i => i.EsPredeterminado).ToList();
                foreach (var i in anteriores) i.EsPredeterminado = false;
            }

            var impuesto = new Impuesto
            {
                Nombre = Input.Nombre,
                Porcentaje = Input.Porcentaje,
                Descripcion = Input.Descripcion,
                EsPredeterminado = Input.EsPredeterminado,
                Activo = Input.Activo
            };

            _context.Impuestos.Add(impuesto);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear impuesto",
                $"Creó el impuesto '{impuesto.Nombre}' ({impuesto.Porcentaje}%)",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Impuesto '{impuesto.Nombre}' creado correctamente";
            return RedirectToPage("/Impuestos/Index");
        }
    }
}