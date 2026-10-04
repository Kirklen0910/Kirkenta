using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.RRHH.Permisos
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<PermisoItem> Permisos { get; set; } = new();
        public int TotalSolicitados { get; set; }
        public int TotalAprobados { get; set; }
        public int TotalTomados { get; set; }
        public decimal DiasConGoce { get; set; }
        public decimal DiasSinGoce { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeAprobar { get; set; }

        public class PermisoItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public string EmpleadoNombre { get; set; } = "";
            public string EmpleadoCodigo { get; set; } = "";
            public string? FotoPath { get; set; }
            public string Tipo { get; set; } = "";
            public DateTime FechaInicio { get; set; }
            public DateTime FechaFin { get; set; }
            public decimal DiasSolicitados { get; set; }
            public bool ConGoceSueldo { get; set; }
            public string Estado { get; set; } = "";
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "Permisos", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Index");
            }

            PuedeCrear = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "PermisosCreate", "crear");
            PuedeAprobar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "PermisosAprobar", "editar");

            var empleadosDict = _context.Empleados.AsNoTracking().ToDictionary(e => e.Id, e => new { e.Codigo, Nombre = $"{e.Nombres} {e.Apellidos}", e.FotoPath });

            var lista = _context.PermisosEmpleado
                .AsNoTracking()
                .OrderByDescending(p => p.FechaSolicitud)
                .ToList();

            Permisos = lista.Select(p =>
            {
                var emp = empleadosDict.GetValueOrDefault(p.EmpleadoId);
                return new PermisoItem
                {
                    Id = p.Id,
                    Numero = p.Numero,
                    EmpleadoNombre = emp?.Nombre ?? "—",
                    EmpleadoCodigo = emp?.Codigo ?? "—",
                    FotoPath = emp?.FotoPath,
                    Tipo = p.Tipo,
                    FechaInicio = p.FechaInicio,
                    FechaFin = p.FechaFin,
                    DiasSolicitados = p.DiasSolicitados,
                    ConGoceSueldo = p.ConGoceSueldo,
                    Estado = p.Estado
                };
            }).ToList();

            TotalSolicitados = lista.Count(p => p.Estado == "Solicitado");
            TotalAprobados = lista.Count(p => p.Estado == "Aprobado");
            TotalTomados = lista.Count(p => p.Estado == "Tomado");
            DiasConGoce = lista.Where(p => p.ConGoceSueldo).Sum(p => p.DiasSolicitados);
            DiasSinGoce = lista.Where(p => !p.ConGoceSueldo).Sum(p => p.DiasSolicitados);

            return Page();
        }
    }
}