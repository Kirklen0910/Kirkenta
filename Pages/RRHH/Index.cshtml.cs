using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.RRHH
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // KPIs
        public int TotalEmpleados { get; set; }
        public int EmpleadosActivos { get; set; }
        public int EmpleadosVacaciones { get; set; }
        public int EmpleadosBajaMes { get; set; }
        public int EmpleadosNuevosMes { get; set; }
        public decimal NominaMensual { get; set; }
        public decimal ValesPendientes { get; set; }
        public int CantidadValesPendientes { get; set; }

        // Listas
        public List<Cumpleaniero> Cumpleanieros { get; set; } = new();
        public List<Aniversario> Aniversarios { get; set; } = new();
        public List<FeriadoProximo> FeriadosProximos { get; set; } = new();
        public List<VacacionProxima> VacacionesProximas { get; set; } = new();
        public List<AlertaDashboard> Alertas { get; set; } = new();
        public List<EmpleadoReciente> EmpleadosRecientes { get; set; } = new();

        public class Cumpleaniero
        {
            public int Id { get; set; }
            public string NombreCompleto { get; set; } = "";
            public DateTime FechaNacimiento { get; set; }
            public int Dias { get; set; }
        }

        public class Aniversario
        {
            public int Id { get; set; }
            public string NombreCompleto { get; set; } = "";
            public DateTime FechaIngreso { get; set; }
            public int Anios { get; set; }
            public int Dias { get; set; }
        }

        public class FeriadoProximo
        {
            public string Nombre { get; set; } = "";
            public DateTime Fecha { get; set; }
            public int Dias { get; set; }
            public string Tipo { get; set; } = "";
        }

        public class VacacionProxima
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public string Empleado { get; set; } = "";
            public DateTime FechaInicio { get; set; }
            public DateTime FechaFin { get; set; }
            public decimal Dias { get; set; }
        }

        public class AlertaDashboard
        {
            public int Id { get; set; }
            public string Titulo { get; set; } = "";
            public string? Descripcion { get; set; }
            public string Prioridad { get; set; } = "Info";
            public string? EmpleadoNombre { get; set; }
        }

        public class EmpleadoReciente
        {
            public int Id { get; set; }
            public string Codigo { get; set; } = "";
            public string NombreCompleto { get; set; } = "";
            public string? FotoPath { get; set; }
            public string? PuestoNombre { get; set; }
            public DateTime FechaIngreso { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "Index", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver RRHH";
                return RedirectToPage("/Index");
            }

            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            // ===== KPIs =====
            var empleados = _context.Empleados.AsNoTracking().ToList();
            TotalEmpleados = empleados.Count;
            EmpleadosActivos = empleados.Count(e => e.Estado == "Activo");
            EmpleadosVacaciones = empleados.Count(e => e.Estado == "Vacaciones");
            EmpleadosNuevosMes = empleados.Count(e => e.FechaIngreso >= inicioMes && e.FechaIngreso < finMes);
            EmpleadosBajaMes = empleados.Count(e => e.FechaBaja.HasValue && e.FechaBaja.Value >= inicioMes && e.FechaBaja.Value < finMes);

            NominaMensual = empleados.Where(e => e.Estado == "Activo" || e.Estado == "Vacaciones").Sum(e => e.SalarioBase);

            var vales = _context.ValesEmpleado
                .AsNoTracking()
                .Where(v => v.SaldoPendiente > 0 &&
                            (v.Estado == "Entregado" || v.Estado == "AprobadoRRHH"))
                .ToList();

            ValesPendientes = vales.Sum(v => v.SaldoPendiente);
            CantidadValesPendientes = vales.Count;

            // ===== CUMPLEAÑEROS DEL MES =====
            Cumpleanieros = empleados
                .Where(e => e.FechaNacimiento.HasValue &&
                            e.Estado != "Baja")
                .Where(e =>
                {
                    var fn = e.FechaNacimiento!.Value;
                    var cumpleEsteAnio = new DateTime(hoy.Year, fn.Month, fn.Day);
                    return cumpleEsteAnio.Month == hoy.Month;
                })
                .Select(e => new Cumpleaniero
                {
                    Id = e.Id,
                    NombreCompleto = $"{e.Nombres} {e.Apellidos}",
                    FechaNacimiento = e.FechaNacimiento!.Value,
                    Dias = e.FechaNacimiento!.Value.Day - hoy.Day
                })
                .OrderBy(x => x.FechaNacimiento.Day)
                .ToList();

            // ===== ANIVERSARIOS DEL MES =====
            Aniversarios = empleados
                .Where(e => e.Estado != "Baja" && e.FechaIngreso.Month == hoy.Month)
                .Select(e => new Aniversario
                {
                    Id = e.Id,
                    NombreCompleto = $"{e.Nombres} {e.Apellidos}",
                    FechaIngreso = e.FechaIngreso,
                    Anios = hoy.Year - e.FechaIngreso.Year,
                    Dias = e.FechaIngreso.Day - hoy.Day
                })
                .Where(x => x.Anios >= 1)
                .OrderBy(x => x.FechaIngreso.Day)
                .ToList();

            // ===== FERIADOS PRÓXIMOS (30 días) =====
            var config = _context.ConfiguracionEmpresa.AsNoTracking().FirstOrDefault();
            var diasAlertaFeriados = config?.RHAlertaFeriadosDias ?? 7;
            var paisCodigo = config?.PaisCodigo ?? "HN";

            var feriadosProx = _context.Feriados
                .AsNoTracking()
                .Where(f => f.Activo && f.PaisCodigo == paisCodigo && f.Fecha >= hoy)
                .OrderBy(f => f.Fecha)
                .Take(5)
                .ToList();

            FeriadosProximos = feriadosProx
                .Where(f => (f.Fecha.Date - hoy).Days <= 30)
                .Select(f => new FeriadoProximo
                {
                    Nombre = f.Nombre,
                    Fecha = f.Fecha,
                    Dias = (f.Fecha.Date - hoy).Days,
                    Tipo = f.Tipo
                })
                .ToList();

            // ===== VACACIONES PRÓXIMAS =====
            var empleadosDict = empleados.ToDictionary(e => e.Id, e => $"{e.Nombres} {e.Apellidos}");

            VacacionesProximas = _context.VacacionesEmpleado
                .AsNoTracking()
                .Where(v => v.Estado == "Aprobado" && v.FechaInicio >= hoy && v.FechaInicio <= hoy.AddDays(15))
                .OrderBy(v => v.FechaInicio)
                .Take(5)
                .ToList()
                .Select(v => new VacacionProxima
                {
                    Id = v.Id,
                    Numero = v.Numero,
                    Empleado = empleadosDict.GetValueOrDefault(v.EmpleadoId, "—"),
                    FechaInicio = v.FechaInicio,
                    FechaFin = v.FechaFin,
                    Dias = v.DiasADescontar
                })
                .ToList();

            // ===== ALERTAS PERSONALIZADAS =====
            Alertas = _context.AlertasPersonalizadas
                .AsNoTracking()
                .Where(a => !a.Completada && a.MostrarEnDashboard)
                .Where(a => !a.FechaVigenciaHasta.HasValue || a.FechaVigenciaHasta.Value >= hoy)
                .OrderByDescending(a => a.Prioridad == "Urgente")
                .ThenByDescending(a => a.FechaAlerta)
                .Take(10)
                .ToList()
                .Select(a => new AlertaDashboard
                {
                    Id = a.Id,
                    Titulo = a.Titulo,
                    Descripcion = a.Descripcion,
                    Prioridad = a.Prioridad,
                    EmpleadoNombre = a.EmpleadoId.HasValue
                        ? empleadosDict.GetValueOrDefault(a.EmpleadoId.Value)
                        : null
                })
                .ToList();

            // ===== EMPLEADOS RECIENTES =====
            EmpleadosRecientes = empleados
                .Where(e => e.Estado != "Baja")
                .OrderByDescending(e => e.FechaIngreso)
                .Take(5)
                .Select(e => new EmpleadoReciente
                {
                    Id = e.Id,
                    Codigo = e.Codigo,
                    NombreCompleto = $"{e.Nombres} {e.Apellidos}",
                    FotoPath = e.FotoPath,
                    PuestoNombre = e.PuestoNombre,
                    FechaIngreso = e.FechaIngreso
                })
                .ToList();

            return Page();
        }
    }
}