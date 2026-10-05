using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Logistica.Zonas
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

        public int EnviosAsociados { get; set; }

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(100)]
            public string Nombre { get; set; } = "";

            [StringLength(300)]
            public string? Descripcion { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
            public decimal PrecioSugerido { get; set; } = 0;

            [StringLength(20)]
            public string Color { get; set; } = "#4f46e5";

            public bool Activa { get; set; } = true;

            public int Orden { get; set; } = 0;
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "ZonasEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar zonas";
                return RedirectToPage("/Logistica/Zonas/Index");
            }

            var zona = _context.ZonasEnvio.FirstOrDefault(z => z.Id == id);
            if (zona == null)
            {
                TempData["Error"] = "Zona no encontrada";
                return RedirectToPage("/Logistica/Zonas/Index");
            }

            Input = new InputModel
            {
                Id = zona.Id,
                Nombre = zona.Nombre,
                Descripcion = zona.Descripcion,
                PrecioSugerido = zona.PrecioSugerido,
                Color = zona.Color,
                Activa = zona.Activa,
                Orden = zona.Orden
            };

            EnviosAsociados = _context.Envios.Count(e => e.ZonaId == id);

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "ZonasEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar zonas";
                return RedirectToPage("/Logistica/Zonas/Index");
            }

            if (!ModelState.IsValid) return Page();

            var zona = _context.ZonasEnvio.FirstOrDefault(z => z.Id == Input.Id);
            if (zona == null)
            {
                TempData["Error"] = "Zona no encontrada";
                return RedirectToPage("/Logistica/Zonas/Index");
            }

            // Validar nombre único (excluyendo la propia)
            if (_context.ZonasEnvio.Any(z => z.Nombre.ToLower() == Input.Nombre.ToLower() && z.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe otra zona con este nombre");
                EnviosAsociados = _context.Envios.Count(e => e.ZonaId == Input.Id);
                return Page();
            }

            zona.Nombre = Input.Nombre;
            zona.Descripcion = Input.Descripcion;
            zona.PrecioSugerido = Input.PrecioSugerido;
            zona.Color = Input.Color;
            zona.Activa = Input.Activa;
            zona.Orden = Input.Orden;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar zona de envío",
                $"Editó la zona '{zona.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Zona '{zona.Nombre}' actualizada correctamente";
            return RedirectToPage("/Logistica/Zonas/Index");
        }
    }
}