using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.RRHH.Empleados
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DetailsModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public Empleado Empleado { get; set; } = new();
        public List<AdjuntoEmpleado> Adjuntos { get; set; } = new();
        public List<TipoDocumentoEmpleado> TiposDocumento { get; set; } = new();
        public List<ExpedienteEmpleado> Expedientes { get; set; } = new();
        public List<VacacionEmpleado> Vacaciones { get; set; } = new();
        public List<PermisoEmpleado> Permisos { get; set; } = new();
        public List<ValeEmpleado> Vales { get; set; } = new();
        public List<DetalleNomina> Nominas { get; set; } = new();

        public int AniosAntiguedad { get; set; }
        public int MesesAntiguedad { get; set; }

        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }
        public bool PuedeSubirDocumento { get; set; }
        public bool PuedeCrearExpediente { get; set; }

        public class AdjuntoView
        {
            public int Id { get; set; }
            public string NombreArchivo { get; set; } = "";
            public string RutaArchivo { get; set; } = "";
            public string? TipoArchivo { get; set; }
            public string? Descripcion { get; set; }
            public string TipoDocumentoNombre { get; set; } = "";
            public string TipoDocumentoCategoria { get; set; } = "";
            public int TamanoKB { get; set; }
            public DateTime? FechaVencimiento { get; set; }
            public bool Vigente { get; set; }
            public DateTime FechaSubida { get; set; }
        }

        public List<AdjuntoView> AdjuntosView { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "Empleados", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            var empleado = _context.Empleados.FirstOrDefault(e => e.Id == id);
            if (empleado == null)
            {
                TempData["Error"] = "Empleado no encontrado";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            Empleado = empleado;

            AniosAntiguedad = EmpleadoHelper.CalcularAniosAntiguedad(empleado);
            MesesAntiguedad = EmpleadoHelper.CalcularMesesAntiguedad(empleado);

            // Permisos
            PuedeEditar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "EmpleadosEdit", "editar");
            PuedeEliminar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "EmpleadosDelete", "eliminar");
            PuedeSubirDocumento = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "DocumentosCreate", "crear");
            PuedeCrearExpediente = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ExpedientesCreate", "crear");

            // Cargar datos
            TiposDocumento = _context.TiposDocumentoEmpleado
                .Where(t => t.Activo)
                .OrderBy(t => t.Orden)
                .ToList();

            var tiposDict = TiposDocumento.ToDictionary(t => t.Id, t => new { t.Nombre, t.Categoria });

            Adjuntos = _context.AdjuntosEmpleado
                .Where(a => a.EmpleadoId == id)
                .OrderByDescending(a => a.FechaSubida)
                .ToList();

            AdjuntosView = Adjuntos.Select(a => new AdjuntoView
            {
                Id = a.Id,
                NombreArchivo = a.NombreArchivo,
                RutaArchivo = a.RutaArchivo,
                TipoArchivo = a.TipoArchivo,
                Descripcion = a.Descripcion,
                TipoDocumentoNombre = tiposDict.GetValueOrDefault(a.TipoDocumentoId)?.Nombre ?? "—",
                TipoDocumentoCategoria = tiposDict.GetValueOrDefault(a.TipoDocumentoId)?.Categoria ?? "—",
                TamanoKB = a.TamanoKB,
                FechaVencimiento = a.FechaVencimiento,
                Vigente = a.Vigente,
                FechaSubida = a.FechaSubida
            }).ToList();

            Expedientes = _context.ExpedientesEmpleado
                .Where(e => e.EmpleadoId == id)
                .OrderByDescending(e => e.Fecha)
                .ToList();

            Vacaciones = _context.VacacionesEmpleado
                .Where(v => v.EmpleadoId == id)
                .OrderByDescending(v => v.FechaInicio)
                .ToList();

            Permisos = _context.PermisosEmpleado
                .Where(p => p.EmpleadoId == id)
                .OrderByDescending(p => p.FechaInicio)
                .ToList();

            Vales = _context.ValesEmpleado
                .Where(v => v.EmpleadoId == id)
                .OrderByDescending(v => v.FechaSolicitud)
                .ToList();

            // Nóminas donde apareció
            Nominas = _context.DetalleNominas
                .Where(d => d.EmpleadoId == id)
                .OrderByDescending(d => d.Id)
                .Take(20)
                .ToList();

            return Page();
        }

        public IActionResult OnPostSubirDocumento(int id, int tipoDocumentoId, IFormFile archivo,
            string? descripcion, DateTime? fechaDocumento, DateTime? fechaVencimiento)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "DocumentosCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Empleados/Details", new { id });
            }

            if (archivo == null || archivo.Length == 0)
            {
                TempData["Error"] = "Debes seleccionar un archivo";
                return RedirectToPage("/RRHH/Empleados/Details", new { id });
            }

            var (adjunto, error) = AdjuntoEmpleadoHelper.Guardar(
                _context, _env, id, tipoDocumentoId, archivo, descripcion,
                fechaDocumento, fechaVencimiento, currentUser?.Id);

            if (error != null)
            {
                TempData["Error"] = error;
            }
            else
            {
                ActividadHelper.Registrar(
                    _context,
                    currentUser?.Id ?? 0,
                    "Subir documento empleado",
                    $"Subió '{adjunto!.NombreArchivo}' al empleado #{id}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["Success"] = "Documento subido correctamente";
            }

            return RedirectToPage("/RRHH/Empleados/Details", new { id });
        }

        public IActionResult OnPostEliminarDocumento(int id, int adjuntoId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "DocumentosDelete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Empleados/Details", new { id });
            }

            var adjunto = _context.AdjuntosEmpleado.FirstOrDefault(a => a.Id == adjuntoId && a.EmpleadoId == id);
            if (adjunto == null)
            {
                TempData["Error"] = "Documento no encontrado";
                return RedirectToPage("/RRHH/Empleados/Details", new { id });
            }

            AdjuntoEmpleadoHelper.Eliminar(_context, _env, adjuntoId);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar documento empleado",
                $"Eliminó '{adjunto.NombreArchivo}' del empleado #{id}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Documento eliminado";
            return RedirectToPage("/RRHH/Empleados/Details", new { id });
        }
    }
}