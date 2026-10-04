using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.RRHH.Expedientes
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ExpedienteItem> Expedientes { get; set; } = new();

        public int TotalLlamados { get; set; }
        public int TotalAmonestaciones { get; set; }
        public int TotalSuspensiones { get; set; }
        public int TotalMeritos { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeEliminar { get; set; }

        public class ExpedienteItem
        {
            public int Id { get; set; }
            public int EmpleadoId { get; set; }
            public string EmpleadoCodigo { get; set; } = "";
            public string EmpleadoNombre { get; set; } = "";
            public string? FotoPath { get; set; }
            public string Tipo { get; set; } = "";
            public string Titulo { get; set; } = "";
            public string Descripcion { get; set; } = "";
            public string? Gravedad { get; set; }
            public DateTime Fecha { get; set; }
            public int? DiasSuspension { get; set; }
            public bool EmpleadoFirmo { get; set; }
            public string? NombreRegistro { get; set; }
        }

        public IActionResult OnGet(int? empleadoId, string? tipo)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "Expedientes", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Index");
            }

            PuedeCrear = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ExpedientesCreate", "crear");
            PuedeEliminar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ExpedientesDelete", "eliminar");

            var empleadosDict = _context.Empleados
                .AsNoTracking()
                .ToDictionary(e => e.Id, e => new { e.Codigo, Nombre = $"{e.Nombres} {e.Apellidos}", e.FotoPath });

            var query = _context.ExpedientesEmpleado.AsNoTracking().AsQueryable();

            if (empleadoId.HasValue)
                query = query.Where(e => e.EmpleadoId == empleadoId.Value);

            if (!string.IsNullOrWhiteSpace(tipo))
                query = query.Where(e => e.Tipo == tipo);

            var lista = query.OrderByDescending(e => e.Fecha).ToList();

            Expedientes = lista.Select(e =>
            {
                var emp = empleadosDict.GetValueOrDefault(e.EmpleadoId);
                return new ExpedienteItem
                {
                    Id = e.Id,
                    EmpleadoId = e.EmpleadoId,
                    EmpleadoCodigo = emp?.Codigo ?? "—",
                    EmpleadoNombre = emp?.Nombre ?? "—",
                    FotoPath = emp?.FotoPath,
                    Tipo = e.Tipo,
                    Titulo = e.Titulo,
                    Descripcion = e.Descripcion,
                    Gravedad = e.Gravedad,
                    Fecha = e.Fecha,
                    DiasSuspension = e.DiasSuspension,
                    EmpleadoFirmo = e.EmpleadoFirmo,
                    NombreRegistro = e.NombreRegistro
                };
            }).ToList();

            TotalLlamados = lista.Count(e => e.Tipo == "LlamadoAtencion" || e.Tipo == "AmonestacionVerbal" || e.Tipo == "AmonestacionEscrita");
            TotalAmonestaciones = lista.Count(e => e.Tipo == "AmonestacionVerbal" || e.Tipo == "AmonestacionEscrita");
            TotalSuspensiones = lista.Count(e => e.Tipo == "Suspension");
            TotalMeritos = lista.Count(e => e.Tipo == "NotaMerito" || e.Tipo == "Reconocimiento");

            return Page();
        }

        public IActionResult OnPostEliminar(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ExpedientesDelete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Expedientes/Index");
            }

            var expediente = _context.ExpedientesEmpleado.FirstOrDefault(e => e.Id == id);
            if (expediente == null)
            {
                TempData["Error"] = "Expediente no encontrado";
                return RedirectToPage("/RRHH/Expedientes/Index");
            }

            _context.ExpedientesEmpleado.Remove(expediente);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar expediente empleado",
                $"Eliminó el expediente '{expediente.Titulo}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Expediente eliminado";
            return RedirectToPage("/RRHH/Expedientes/Index");
        }
    }
}