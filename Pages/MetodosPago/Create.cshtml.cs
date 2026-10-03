using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.MetodosPago
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
            [StringLength(80)]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "El tipo es obligatorio")]
            public string Tipo { get; set; } = "Efectivo";

            public bool RequiereReferencia { get; set; }
            public bool Activo { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/MetodosPago/Index");
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
                return RedirectToPage("/MetodosPago/Index");
            }

            if (!ModelState.IsValid) return Page();

            if (_context.MetodosPago.Any(m => m.Nombre.ToLower() == Input.Nombre.ToLower()))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe un método con este nombre");
                return Page();
            }

            var metodo = new MetodoPago
            {
                Nombre = Input.Nombre,
                Tipo = Input.Tipo,
                RequiereReferencia = Input.RequiereReferencia,
                Activo = Input.Activo
            };

            _context.MetodosPago.Add(metodo);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear método de pago",
                $"Creó el método '{metodo.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Método '{metodo.Nombre}' creado";
            return RedirectToPage("/MetodosPago/Index");
        }
    }
}