using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Permisos
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
            [Required]
            public int EmpleadoId { get; set; }

            [Required]
            public string Tipo { get; set; } = "Personal";

            [Required]
            public DateTime FechaInicio { get; set; } = DateTime.Today;

            [Required]
            public DateTime FechaFin { get; set; } = DateTime.Today;

            public bool ConGoceSueldo { get; set; } = true;

            [Required]
            [StringLength(500)]
            public string Motivo { get; set; } = "";

            [StringLength(500)]
            public string? Notas { get; set; }
        }

        public IActionResult OnGet(int? empleadoId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "PermisosCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Permisos/Index");
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
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "PermisosCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Permisos/Index");
            }

            CargarDatos();

            if (Input.FechaFin < Input.FechaInicio)
            {
                ModelState.AddModelError("Input.FechaFin", "La fecha fin no puede ser anterior al inicio");
                return Page();
            }

            var tiposValidos = new[] { "Personal", "Enfermedad", "Duelo", "Maternidad", "Paternidad", "Estudio", "CitaMedica", "Otro" };
            if (!tiposValidos.Contains(Input.Tipo))
                ModelState.AddModelError("Input.Tipo", "Tipo de permiso no válido");

            if ((Input.FechaFin.Date - Input.FechaInicio.Date).Days > 90)
                ModelState.AddModelError("Input.FechaFin", "Un permiso no puede exceder 90 días");

            var emp0 = _context.Empleados.FirstOrDefault(e => e.Id == Input.EmpleadoId);
            if (emp0 == null || emp0.Estado == "Baja")
                ModelState.AddModelError("Input.EmpleadoId", "Empleado no válido o dado de baja");

            if (ModelState.IsValid)
            {
                var ini = Input.FechaInicio.Date;
                var fin = Input.FechaFin.Date;
                var solapa = _context.PermisosEmpleado.Any(p => p.EmpleadoId == Input.EmpleadoId
                    && (p.Estado == "Solicitado" || p.Estado == "Aprobado")
                    && p.FechaInicio.Date <= fin && p.FechaFin.Date >= ini);
                if (solapa)
                    ModelState.AddModelError(string.Empty, "El empleado ya tiene un permiso solicitado/aprobado que se cruza con esas fechas");
            }

            if (!ModelState.IsValid) return Page();

            var dias = (Input.FechaFin.Date - Input.FechaInicio.Date).Days + 1;
            var numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "PermisoEmpleado");

            var permiso = new PermisoEmpleado
            {
                Numero = numero,
                EmpleadoId = Input.EmpleadoId,
                Tipo = Input.Tipo,
                Estado = "Solicitado",
                FechaInicio = Input.FechaInicio,
                FechaFin = Input.FechaFin,
                DiasSolicitados = dias,
                ConGoceSueldo = Input.ConGoceSueldo,
                FechaSolicitud = DateTime.Now,
                UsuarioSolicitaId = currentUser?.Id,
                Motivo = Input.Motivo,
                Notas = Input.Notas,
                EmpresaId = EmpresaActual.Id
            };

            _context.PermisosEmpleado.Add(permiso);
            _context.SaveChanges();

            var emp = _context.Empleados.FirstOrDefault(e => e.Id == Input.EmpleadoId);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Solicitar permiso",
                $"Solicitó permiso de {dias} días para {emp?.Nombres} {emp?.Apellidos}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Permiso {numero} creado. {dias} días pendientes de aprobación.";
            return RedirectToPage("/RRHH/Permisos/Index");
        }

        private void CargarDatos()
        {
            Empleados = _context.Empleados
                .Where(e => e.Estado == "Activo" || e.Estado == "Vacaciones")
                .OrderBy(e => e.Apellidos)
                .ToList();

            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "PermisoEmpleado");
        }
    }
}