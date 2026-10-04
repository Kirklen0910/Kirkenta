using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.RRHH.Reportes
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public string? DepartamentoFiltro { get; set; }
        public List<string> DepartamentosDisponibles { get; set; } = new();

        // KPIs
        public int TotalEmpleados { get; set; }
        public int TotalActivos { get; set; }
        public decimal NominaMensual { get; set; }
        public decimal SalarioPromedio { get; set; }
        public decimal AntiguedadPromedio { get; set; }
        public int TotalAltas { get; set; }
        public int TotalBajas { get; set; }
        public decimal DiasVacacionesTomados { get; set; }
        public decimal TasaRotacion { get; set; }

        // Listas
        public List<DepartamentoItem> EmpleadosPorDepartamento { get; set; } = new();
        public List<EstadoItem> EmpleadosPorEstado { get; set; } = new();
        public List<NominaMesItem> NominaPorMes { get; set; } = new();
        public List<CumpleanieroItem> Cumpleanieros { get; set; } = new();
        public List<TopAntiguedadItem> TopAntiguedad { get; set; } = new();

        public class DepartamentoItem
        {
            public string Departamento { get; set; } = "";
            public int Cantidad { get; set; }
            public decimal Porcentaje { get; set; }
            public decimal Nomina { get; set; }
            public decimal SalarioPromedio { get; set; }
        }

        public class EstadoItem
        {
            public string Estado { get; set; } = "";
            public int Cantidad { get; set; }
        }

        public class NominaMesItem
        {
            public string Mes { get; set; } = "";
            public decimal Monto { get; set; }
        }

        public class CumpleanieroItem
        {
            public string NombreCompleto { get; set; } = "";
            public DateTime FechaNacimiento { get; set; }
            public string? Departamento { get; set; }
        }

        public class TopAntiguedadItem
        {
            public string NombreCompleto { get; set; } = "";
            public string? Puesto { get; set; }
            public string? Departamento { get; set; }
            public DateTime FechaIngreso { get; set; }
            public int Anios { get; set; }
            public decimal SalarioBase { get; set; }
        }

        public IActionResult OnGet(DateTime? desde, DateTime? hasta, string? departamento)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "Reportes", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Index");
            }

            var hoy = DateTime.Today;

            Desde = desde ?? new DateTime(hoy.Year, 1, 1);
            Hasta = hasta ?? hoy;
            DepartamentoFiltro = departamento;

            // Departamentos disponibles
            DepartamentosDisponibles = _context.Empleados
                .Where(e => !string.IsNullOrEmpty(e.Departamento))
                .Select(e => e.Departamento!)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            // Empleados base (con filtro de departamento)
            var query = _context.Empleados.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(DepartamentoFiltro))
            {
                query = query.Where(e => e.Departamento == DepartamentoFiltro);
            }
            var empleados = query.ToList();

            // KPIs
            TotalEmpleados = empleados.Count;
            TotalActivos = empleados.Count(e => e.Estado == "Activo" || e.Estado == "Vacaciones");
            var empleadosActivos = empleados.Where(e => e.Estado == "Activo" || e.Estado == "Vacaciones").ToList();
            NominaMensual = empleadosActivos.Sum(e => e.SalarioBase);
            SalarioPromedio = empleadosActivos.Count > 0 ? NominaMensual / empleadosActivos.Count : 0;

            AntiguedadPromedio = empleados.Count > 0
                ? (decimal)empleados.Average(e => CalcularAnios(e.FechaIngreso, hoy))
                : 0;

            TotalAltas = empleados.Count(e => e.FechaIngreso >= Desde && e.FechaIngreso <= Hasta);
            TotalBajas = empleados.Count(e => e.FechaBaja.HasValue && e.FechaBaja.Value >= Desde && e.FechaBaja.Value <= Hasta);

            // Rotación = (Bajas / Promedio empleados) * 100
            var promedioEmpleados = Math.Max(1, TotalEmpleados);
            TasaRotacion = (decimal)TotalBajas / promedioEmpleados * 100;

            // Vacaciones tomadas en el período
            var vacaciones = _context.VacacionesEmpleado
                .AsNoTracking()
                .Where(v => v.Estado == "Tomado" || v.Estado == "Aprobado")
                .Where(v => v.FechaInicio >= Desde && v.FechaInicio <= Hasta)
                .ToList();

            if (!string.IsNullOrWhiteSpace(DepartamentoFiltro))
            {
                var empIds = empleados.Select(e => e.Id).ToHashSet();
                vacaciones = vacaciones.Where(v => empIds.Contains(v.EmpleadoId)).ToList();
            }

            DiasVacacionesTomados = vacaciones.Sum(v => v.DiasADescontar);

            // ===== Empleados por departamento =====
            var porDep = empleados
                .Where(e => !string.IsNullOrWhiteSpace(e.Departamento))
                .GroupBy(e => e.Departamento!)
                .Select(g => new DepartamentoItem
                {
                    Departamento = g.Key,
                    Cantidad = g.Count(),
                    Porcentaje = TotalEmpleados > 0 ? (decimal)g.Count() / TotalEmpleados * 100 : 0,
                    Nomina = g.Where(e => e.Estado == "Activo" || e.Estado == "Vacaciones").Sum(e => e.SalarioBase),
                    SalarioPromedio = g.Count(e => e.Estado == "Activo" || e.Estado == "Vacaciones") > 0
                        ? g.Where(e => e.Estado == "Activo" || e.Estado == "Vacaciones").Sum(e => e.SalarioBase)
                          / g.Count(e => e.Estado == "Activo" || e.Estado == "Vacaciones")
                        : 0
                })
                .OrderByDescending(d => d.Cantidad)
                .ToList();

            // Empleados sin departamento
            var sinDep = empleados.Count(e => string.IsNullOrWhiteSpace(e.Departamento));
            if (sinDep > 0)
            {
                porDep.Add(new DepartamentoItem
                {
                    Departamento = "(Sin departamento)",
                    Cantidad = sinDep,
                    Porcentaje = TotalEmpleados > 0 ? (decimal)sinDep / TotalEmpleados * 100 : 0,
                    Nomina = empleados.Where(e => string.IsNullOrWhiteSpace(e.Departamento) && (e.Estado == "Activo" || e.Estado == "Vacaciones")).Sum(e => e.SalarioBase)
                });
            }

            EmpleadosPorDepartamento = porDep;

            // ===== Empleados por estado =====
            EmpleadosPorEstado = empleados
                .GroupBy(e => e.Estado)
                .Select(g => new EstadoItem { Estado = g.Key, Cantidad = g.Count() })
                .OrderByDescending(e => e.Cantidad)
                .ToList();

            // ===== Nómina pagada por mes =====
            var nominasPagadas = _context.Nominas
                .AsNoTracking()
                .Where(n => n.Estado == "Pagada")
                .Where(n => n.FechaInicio >= Desde && n.FechaInicio <= Hasta)
                .ToList();

            NominaPorMes = nominasPagadas
                .GroupBy(n => new { n.FechaInicio.Year, n.FechaInicio.Month })
                .Select(g => new NominaMesItem
                {
                    Mes = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Monto = g.Sum(n => n.TotalNeto)
                })
                .OrderBy(n => DateTime.ParseExact(n.Mes, "MMM yyyy", System.Globalization.CultureInfo.InvariantCulture))
                .ToList();

            // ===== Cumpleaños del mes actual =====
            var mesActual = hoy.Month;
            Cumpleanieros = empleados
                .Where(e => e.FechaNacimiento.HasValue && e.FechaNacimiento.Value.Month == mesActual && e.Estado != "Baja")
                .OrderBy(e => e.FechaNacimiento!.Value.Day)
                .Select(e => new CumpleanieroItem
                {
                    NombreCompleto = $"{e.Nombres} {e.Apellidos}",
                    FechaNacimiento = e.FechaNacimiento!.Value,
                    Departamento = e.Departamento
                })
                .ToList();

            // ===== Top 10 por antigüedad =====
            TopAntiguedad = empleados
                .Where(e => e.Estado != "Baja")
                .OrderBy(e => e.FechaIngreso)
                .Take(10)
                .Select(e => new TopAntiguedadItem
                {
                    NombreCompleto = $"{e.Nombres} {e.Apellidos}",
                    Puesto = e.PuestoNombre,
                    Departamento = e.Departamento,
                    FechaIngreso = e.FechaIngreso,
                    Anios = CalcularAnios(e.FechaIngreso, hoy),
                    SalarioBase = e.SalarioBase
                })
                .ToList();

            return Page();
        }

        private static int CalcularAnios(DateTime fechaIngreso, DateTime hoy)
        {
            var anios = hoy.Year - fechaIngreso.Year;
            if (fechaIngreso.Date > hoy.AddYears(-anios).Date) anios--;
            return Math.Max(0, anios);
        }
    }
}