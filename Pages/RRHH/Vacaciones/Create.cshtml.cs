using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Vacaciones
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
        public string NumeroPreview { get; set; } = "";

        public class InputModel
        {
            [Required(ErrorMessage = "Debes seleccionar un empleado")]
            public int EmpleadoId { get; set; }

            [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
            public DateTime FechaInicio { get; set; } = DateTime.Today.AddDays(7);

            [Required(ErrorMessage = "La fecha de fin es obligatoria")]
            public DateTime FechaFin { get; set; } = DateTime.Today.AddDays(14);

            [StringLength(500)]
            public string? Motivo { get; set; }
        }

        public IActionResult OnGet(int? empleadoId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "VacacionesCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Vacaciones/Index");
            }

            CargarDatos();

            if (empleadoId.HasValue)
            {
                Input.EmpleadoId = empleadoId.Value;
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "VacacionesCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Vacaciones/Index");
            }

            CargarDatos();

            if (!ModelState.IsValid) return Page();

            var (vacacion, error) = VacacionHelper.Solicitar(
                _context,
                Input.EmpleadoId,
                Input.FechaInicio,
                Input.FechaFin,
                Input.Motivo,
                currentUser?.Id
            );

            if (error != null)
            {
                ModelState.AddModelError(string.Empty, error);
                return Page();
            }

            var empleado = _context.Empleados.FirstOrDefault(e => e.Id == Input.EmpleadoId);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Solicitar vacaciones",
                $"Solicitó {vacacion!.DiasADescontar} días de vacaciones para {empleado?.Nombres} {empleado?.Apellidos}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Solicitud {vacacion.Numero} creada. {vacacion.DiasADescontar} días pendientes de aprobación.";
            return RedirectToPage("/RRHH/Vacaciones/Index");
        }

        private void CargarDatos()
        {
            Empleados = _context.Empleados
                .Where(e => e.Estado == "Activo" || e.Estado == "Vacaciones")
                .OrderBy(e => e.Apellidos)
                .ThenBy(e => e.Nombres)
                .ToList();

            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Vacacion");
        }
    }
}