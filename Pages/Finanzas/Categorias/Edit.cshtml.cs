using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Categorias
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

            [Required]
            public string Tipo { get; set; } = "Egreso";

            [Required]
            [StringLength(100)]
            public string Nombre { get; set; } = string.Empty;

            [StringLength(300)]
            public string? Descripcion { get; set; }

            [StringLength(30)]
            public string? CuentaContable { get; set; }

            [StringLength(20)]
            public string Color { get; set; } = "#6b7280";

            public bool EsSistema { get; set; }
            public bool Activa { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CategoriasEdit", "editar"))
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

            Input = new InputModel
            {
                Id = c.Id,
                Tipo = c.Tipo,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                CuentaContable = c.CuentaContable,
                Color = c.Color,
                EsSistema = c.EsSistema,
                Activa = c.Activa
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CategoriasEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Categorias/Index");
            }

            if (!ModelState.IsValid) return Page();

            var c = _context.CategoriasFinancieras.FirstOrDefault(x => x.Id == Input.Id);
            if (c == null)
            {
                TempData["Error"] = "Categoría no encontrada";
                return RedirectToPage("/Finanzas/Categorias/Index");
            }

            if (_context.CategoriasFinancieras.Any(x => x.Tipo == Input.Tipo && x.Nombre == Input.Nombre && x.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe otra categoría con este nombre para este tipo");
                return Page();
            }

            c.Tipo = Input.Tipo;
            c.Nombre = Input.Nombre;
            c.Descripcion = Input.Descripcion;
            c.CuentaContable = Input.CuentaContable;
            c.Color = Input.Color;
            c.Activa = Input.Activa;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar categoría financiera",
                $"Editó la categoría '{c.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Categoría '{c.Nombre}' actualizada";
            return RedirectToPage("/Finanzas/Categorias/Index");
        }
    }
}