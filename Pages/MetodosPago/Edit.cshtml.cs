using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.MetodosPago
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
            [StringLength(80)]
            public string Nombre { get; set; } = string.Empty;

            [Required]
            public string Tipo { get; set; } = "Efectivo";

            public bool RequiereReferencia { get; set; }
            public bool Activo { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/MetodosPago/Index");
            }

            var metodo = _context.MetodosPago.FirstOrDefault(m => m.Id == id);
            if (metodo == null)
            {
                TempData["Error"] = "Método no encontrado";
                return RedirectToPage("/MetodosPago/Index");
            }

            Input = new InputModel
            {
                Id = metodo.Id,
                Nombre = metodo.Nombre,
                Tipo = metodo.Tipo,
                RequiereReferencia = metodo.RequiereReferencia,
                Activo = metodo.Activo
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
                return RedirectToPage("/MetodosPago/Index");
            }

            if (!ModelState.IsValid) return Page();

            var metodo = _context.MetodosPago.FirstOrDefault(m => m.Id == Input.Id);
            if (metodo == null)
            {
                TempData["Error"] = "Método no encontrado";
                return RedirectToPage("/MetodosPago/Index");
            }

            if (_context.MetodosPago.Any(m => m.Nombre.ToLower() == Input.Nombre.ToLower() && m.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe un método con este nombre");
                return Page();
            }

            metodo.Nombre = Input.Nombre;
            metodo.Tipo = Input.Tipo;
            metodo.RequiereReferencia = Input.RequiereReferencia;
            metodo.Activo = Input.Activo;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar método de pago",
                $"Editó el método '{metodo.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Método '{metodo.Nombre}' actualizado";
            return RedirectToPage("/MetodosPago/Index");
        }
    }
}