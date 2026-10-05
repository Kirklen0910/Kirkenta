using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Logistica.Zonas
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

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "ZonasCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear zonas";
                return RedirectToPage("/Logistica/Zonas/Index");
            }

            // Sugerir orden siguiente
            var maxOrden = _context.ZonasEnvio
                .Select(z => (int?)z.Orden)
                .Max() ?? 0;

            Input.Orden = maxOrden + 1;

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "ZonasCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear zonas";
                return RedirectToPage("/Logistica/Zonas/Index");
            }

            if (!ModelState.IsValid) return Page();

            // Validar nombre único
            if (_context.ZonasEnvio.Any(z => z.Nombre.ToLower() == Input.Nombre.ToLower()))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe una zona con este nombre");
                return Page();
            }

            var zona = new ZonaEnvio
            {
                Nombre = Input.Nombre,
                Descripcion = Input.Descripcion,
                PrecioSugerido = Input.PrecioSugerido,
                Color = Input.Color,
                Activa = Input.Activa,
                Orden = Input.Orden,
                FechaCreacion = DateTime.Now,
                EmpresaId = 1
            };

            _context.ZonasEnvio.Add(zona);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear zona de envío",
                $"Creó la zona '{zona.Nombre}' con precio sugerido L. {zona.PrecioSugerido:N2}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Zona '{zona.Nombre}' creada correctamente";
            return RedirectToPage("/Logistica/Zonas/Index");
        }
    }
}