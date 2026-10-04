using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.RRHH.Empleados
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<EmpleadoItem> Empleados { get; set; } = new();
        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }

        public int TotalActivos { get; set; }
        public int TotalInactivos { get; set; }
        public int TotalVacaciones { get; set; }
        public int TotalBajas { get; set; }

        public class EmpleadoItem
        {
            public int Id { get; set; }
            public string Codigo { get; set; } = "";
            public string NombreCompleto { get; set; } = "";
            public string Cedula { get; set; } = "";
            public string? FotoPath { get; set; }
            public string? PuestoNombre { get; set; }
            public string? Departamento { get; set; }
            public string? Telefono { get; set; }
            public string? Email { get; set; }
            public DateTime FechaIngreso { get; set; }
            public int AniosAntiguedad { get; set; }
            public string Estado { get; set; } = "Activo";
            public decimal SalarioBase { get; set; }
            public decimal DiasVacacionesDisponibles { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "Empleados", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Index");
            }

            PuedeCrear = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "EmpleadosCreate", "crear");
            PuedeEditar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "EmpleadosEdit", "editar");
            PuedeEliminar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "EmpleadosDelete", "eliminar");

            var hoy = DateTime.Today;

            var lista = _context.Empleados
                .AsNoTracking()
                .OrderBy(e => e.Apellidos)
                .ThenBy(e => e.Nombres)
                .ToList();

            Empleados = lista.Select(e => new EmpleadoItem
            {
                Id = e.Id,
                Codigo = e.Codigo,
                NombreCompleto = $"{e.Nombres} {e.Apellidos}",
                Cedula = e.Cedula,
                FotoPath = e.FotoPath,
                PuestoNombre = e.PuestoNombre,
                Departamento = e.Departamento,
                Telefono = e.Telefono,
                Email = e.Email,
                FechaIngreso = e.FechaIngreso,
                AniosAntiguedad = CalcularAnios(e.FechaIngreso, hoy),
                Estado = e.Estado,
                SalarioBase = e.SalarioBase,
                DiasVacacionesDisponibles = e.DiasVacacionesDisponibles
            }).ToList();

            TotalActivos = lista.Count(e => e.Estado == "Activo");
            TotalInactivos = lista.Count(e => e.Estado == "Inactivo" || e.Estado == "Suspendido");
            TotalVacaciones = lista.Count(e => e.Estado == "Vacaciones");
            TotalBajas = lista.Count(e => e.Estado == "Baja");

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