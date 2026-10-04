using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Expedientes
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
            [Required(ErrorMessage = "Debes seleccionar un empleado")]
            public int EmpleadoId { get; set; }

            [Required(ErrorMessage = "El tipo es obligatorio")]
            public string Tipo { get; set; } = "LlamadoAtencion";

            [Required(ErrorMessage = "El título es obligatorio")]
            [StringLength(200)]
            public string Titulo { get; set; } = "";

            [Required(ErrorMessage = "La descripción es obligatoria")]
            public string Descripcion { get; set; } = "";

            public string? Gravedad { get; set; }

            public DateTime Fecha { get; set; } = DateTime.Now;

            public DateTime? FechaInicioSuspension { get; set; }
            public DateTime? FechaFinSuspension { get; set; }
            public int? DiasSuspension { get; set; }

            [StringLength(300)]
            public string? Testigos { get; set; }

            public bool EmpleadoFirmo { get; set; }

            [StringLength(500)]
            public string? Notas { get; set; }
        }

        public IActionResult OnGet(int? empleadoId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ExpedientesCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Expedientes/Index");
            }

            CargarDatos();

            if (empleadoId.HasValue)
                Input.EmpleadoId = empleadoId.Value;

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ExpedientesCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Expedientes/Index");
            }

            CargarDatos();

            // Validar suspensión
            if (Input.Tipo == "Suspension")
            {
                if (!Input.FechaInicioSuspension.HasValue || !Input.FechaFinSuspension.HasValue)
                {
                    ModelState.AddModelError(string.Empty, "Las suspensiones requieren fecha de inicio y fin");
                }
                else if (Input.FechaFinSuspension < Input.FechaInicioSuspension)
                {
                    ModelState.AddModelError(string.Empty, "La fecha fin no puede ser anterior al inicio");
                }
            }

            if (!ModelState.IsValid) return Page();

            // Calcular días de suspensión
            if (Input.Tipo == "Suspension" && Input.FechaInicioSuspension.HasValue && Input.FechaFinSuspension.HasValue)
            {
                Input.DiasSuspension = (Input.FechaFinSuspension.Value.Date - Input.FechaInicioSuspension.Value.Date).Days + 1;
            }

            var expediente = new ExpedienteEmpleado
            {
                EmpleadoId = Input.EmpleadoId,
                Tipo = Input.Tipo,
                Titulo = Input.Titulo,
                Descripcion = Input.Descripcion,
                Gravedad = Input.Gravedad,
                Fecha = Input.Fecha,
                FechaInicioSuspension = Input.FechaInicioSuspension,
                FechaFinSuspension = Input.FechaFinSuspension,
                DiasSuspension = Input.DiasSuspension,
                UsuarioRegistroId = currentUser?.Id,
                NombreRegistro = currentUser?.Username,
                Testigos = Input.Testigos,
                EmpleadoFirmo = Input.EmpleadoFirmo,
                FechaFirma = Input.EmpleadoFirmo ? DateTime.Now : null,
                Notas = Input.Notas,
                FechaCreacion = DateTime.Now
            };

            _context.ExpedientesEmpleado.Add(expediente);
            _context.SaveChanges();

            var emp = _context.Empleados.FirstOrDefault(e => e.Id == Input.EmpleadoId);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear expediente",
                $"Registró '{expediente.Titulo}' ({expediente.Tipo}) a {emp?.Nombres} {emp?.Apellidos}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Expediente creado correctamente";
            return RedirectToPage("/RRHH/Empleados/Details", new { id = Input.EmpleadoId });
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