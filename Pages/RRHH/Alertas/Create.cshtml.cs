using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Alertas
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

        public List<Empleado> Empleados { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El título es obligatorio")]
            [StringLength(200)]
            public string Titulo { get; set; } = "";

            [StringLength(1000)]
            public string? Descripcion { get; set; }

            [Required]
            public string Prioridad { get; set; } = "Info";

            public int? EmpleadoId { get; set; }

            [Required]
            public DateTime FechaAlerta { get; set; } = DateTime.Today;

            public DateTime? FechaVigenciaHasta { get; set; }

            public bool MostrarEnDashboard { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "AlertasCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Alertas/Index");
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "AlertasCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Alertas/Index");
            }

            CargarDatos();

            if (Input.FechaVigenciaHasta.HasValue && Input.FechaVigenciaHasta.Value.Date < Input.FechaAlerta.Date)
            {
                ModelState.AddModelError("Input.FechaVigenciaHasta", "La fecha de vigencia no puede ser anterior a la fecha de la alerta");
            }

            if (!ModelState.IsValid) return Page();

            var alerta = new AlertaPersonalizada
            {
                Titulo = Input.Titulo,
                Descripcion = Input.Descripcion,
                Prioridad = Input.Prioridad,
                EmpleadoId = Input.EmpleadoId,
                FechaAlerta = Input.FechaAlerta,
                FechaVigenciaHasta = Input.FechaVigenciaHasta,
                MostrarEnDashboard = Input.MostrarEnDashboard,
                Completada = false,
                UsuarioCreoId = currentUser?.Id,
                EmpresaId = 1,
                FechaCreacion = DateTime.Now
            };

            _context.AlertasPersonalizadas.Add(alerta);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear alerta",
                $"Creó alerta '{alerta.Titulo}' ({alerta.Prioridad})",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Alerta creada correctamente";
            return RedirectToPage("/RRHH/Alertas/Index");
        }

        private void CargarDatos()
        {
            Empleados = _context.Empleados
                .Where(e => e.Estado != "Baja")
                .OrderBy(e => e.Apellidos)
                .ThenBy(e => e.Nombres)
                .ToList();
        }
    }
}