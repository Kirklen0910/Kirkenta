using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Impuestos
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

            [Range(0, 100, ErrorMessage = "Debe estar entre 0 y 100")]
            public decimal Porcentaje { get; set; }

            [StringLength(200)]
            public string? Descripcion { get; set; }

            public bool EsPredeterminado { get; set; }
            public bool Activo { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Impuestos/Index");
            }

            var impuesto = _context.Impuestos.FirstOrDefault(i => i.Id == id);
            if (impuesto == null)
            {
                TempData["Error"] = "Impuesto no encontrado";
                return RedirectToPage("/Impuestos/Index");
            }

            Input = new InputModel
            {
                Id = impuesto.Id,
                Nombre = impuesto.Nombre,
                Porcentaje = impuesto.Porcentaje,
                Descripcion = impuesto.Descripcion,
                EsPredeterminado = impuesto.EsPredeterminado,
                Activo = impuesto.Activo
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
                return RedirectToPage("/Impuestos/Index");
            }

            if (!ModelState.IsValid) return Page();

            var impuesto = _context.Impuestos.FirstOrDefault(i => i.Id == Input.Id);
            if (impuesto == null)
            {
                TempData["Error"] = "Impuesto no encontrado";
                return RedirectToPage("/Impuestos/Index");
            }

            if (_context.Impuestos.Any(i => i.Nombre.ToLower() == Input.Nombre.ToLower() && i.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe un impuesto con este nombre");
                return Page();
            }

            // Si es predeterminado, quitar el anterior
            if (Input.EsPredeterminado && !impuesto.EsPredeterminado)
            {
                var anteriores = _context.Impuestos.Where(i => i.EsPredeterminado && i.Id != Input.Id).ToList();
                foreach (var i in anteriores) i.EsPredeterminado = false;
            }

            impuesto.Nombre = Input.Nombre;
            impuesto.Porcentaje = Input.Porcentaje;
            impuesto.Descripcion = Input.Descripcion;
            impuesto.EsPredeterminado = Input.EsPredeterminado;
            impuesto.Activo = Input.Activo;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar impuesto",
                $"Editó el impuesto '{impuesto.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Impuesto '{impuesto.Nombre}' actualizado";
            return RedirectToPage("/Impuestos/Index");
        }
    }
}