using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.RRHH.Vacaciones
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<VacacionItem> Vacaciones { get; set; } = new();

        public int TotalSolicitadas { get; set; }
        public int TotalAprobadas { get; set; }
        public int TotalTomadas { get; set; }
        public int TotalRechazadas { get; set; }
        public decimal DiasPendientes { get; set; }
        public decimal DiasTomadosAnio { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeAprobar { get; set; }

        public class VacacionItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public string EmpleadoCodigo { get; set; } = "";
            public string EmpleadoNombre { get; set; } = "";
            public string? FotoPath { get; set; }
            public DateTime FechaInicio { get; set; }
            public DateTime FechaFin { get; set; }
            public decimal DiasSolicitados { get; set; }
            public decimal DiasADescontar { get; set; }
            public int DiasFeriados { get; set; }
            public string Estado { get; set; } = "";
            public DateTime FechaSolicitud { get; set; }
            public string? MotivoRechazo { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "Vacaciones", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Index");
            }

            PuedeCrear = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "VacacionesCreate", "crear");
            PuedeAprobar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "VacacionesAprobar", "editar");

            var empleadosDict = _context.Empleados
                .AsNoTracking()
                .ToDictionary(e => e.Id, e => new { e.Codigo, Nombre = $"{e.Nombres} {e.Apellidos}", e.FotoPath });

            var lista = _context.VacacionesEmpleado
                .AsNoTracking()
                .OrderByDescending(v => v.FechaSolicitud)
                .ToList();

            Vacaciones = lista.Select(v =>
            {
                var emp = empleadosDict.GetValueOrDefault(v.EmpleadoId);
                return new VacacionItem
                {
                    Id = v.Id,
                    Numero = v.Numero,
                    EmpleadoCodigo = emp?.Codigo ?? "—",
                    EmpleadoNombre = emp?.Nombre ?? "—",
                    FotoPath = emp?.FotoPath,
                    FechaInicio = v.FechaInicio,
                    FechaFin = v.FechaFin,
                    DiasSolicitados = v.DiasSolicitados,
                    DiasADescontar = v.DiasADescontar,
                    DiasFeriados = v.DiasFeriados,
                    Estado = v.Estado,
                    FechaSolicitud = v.FechaSolicitud,
                    MotivoRechazo = v.MotivoRechazo
                };
            }).ToList();

            TotalSolicitadas = lista.Count(v => v.Estado == "Solicitado");
            TotalAprobadas = lista.Count(v => v.Estado == "Aprobado");
            TotalTomadas = lista.Count(v => v.Estado == "Tomado");
            TotalRechazadas = lista.Count(v => v.Estado == "Rechazado");
            DiasPendientes = lista.Where(v => v.Estado == "Aprobado" || v.Estado == "Solicitado").Sum(v => v.DiasADescontar);

            var inicioAnio = new DateTime(DateTime.Today.Year, 1, 1);
            DiasTomadosAnio = lista.Where(v => v.FechaInicio >= inicioAnio && v.Estado == "Tomado").Sum(v => v.DiasADescontar);

            return Page();
        }
    }
}