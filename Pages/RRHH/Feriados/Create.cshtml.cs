using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Feriados
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
            [Required]
            [StringLength(150)]
            public string Nombre { get; set; } = "";

            [Required]
            public DateTime Fecha { get; set; } = DateTime.Today;

            [Required]
            [StringLength(3)]
            public string PaisCodigo { get; set; } = "HN";

            [Required]
            public string Tipo { get; set; } = "Nacional";

            public bool EsRecurrente { get; set; } = true;
            public bool EsMovil { get; set; } = false;

            [StringLength(300)]
            public string? Descripcion { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "FeriadosCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Feriados/Index");
            }

            var config = _context.ConfiguracionEmpresa.FirstOrDefault();
            Input.PaisCodigo = config?.PaisCodigo ?? "HN";

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "FeriadosCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Feriados/Index");
            }

            if (!ModelState.IsValid) return Page();

            var existe = _context.Feriados.Any(f => f.Nombre == Input.Nombre && f.Fecha.Date == Input.Fecha.Date && f.PaisCodigo == Input.PaisCodigo);
            if (existe)
            {
                ModelState.AddModelError(string.Empty, "Ya existe un feriado con ese nombre y fecha");
                return Page();
            }

            var feriado = new Feriado
            {
                Nombre = Input.Nombre,
                Fecha = Input.Fecha,
                PaisCodigo = Input.PaisCodigo,
                Tipo = Input.Tipo,
                EsRecurrente = Input.EsRecurrente,
                EsMovil = Input.EsMovil,
                Descripcion = Input.Descripcion,
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            _context.Feriados.Add(feriado);
            _context.SaveChanges();

            TempData["Success"] = $"Feriado '{feriado.Nombre}' creado";
            return RedirectToPage("/RRHH/Feriados/Index");
        }
    }
}