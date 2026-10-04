using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.RRHH.Vales
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ValeItem> Vales { get; set; } = new();

        public int TotalPendientes { get; set; }
        public int TotalAprobadosGerente { get; set; }
        public int TotalAprobadosRRHH { get; set; }
        public int TotalEntregados { get; set; }
        public decimal MontoPendiente { get; set; }
        public decimal SaldoPorDescontar { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeAprobar { get; set; }
        public bool PuedeEntregar { get; set; }

        public class ValeItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public string EmpleadoCodigo { get; set; } = "";
            public string EmpleadoNombre { get; set; } = "";
            public string? FotoPath { get; set; }
            public DateTime FechaSolicitud { get; set; }
            public DateTime? FechaEntrega { get; set; }
            public decimal Monto { get; set; }
            public string Motivo { get; set; } = "";
            public string Estado { get; set; } = "";
            public int Cuotas { get; set; }
            public decimal SaldoPendiente { get; set; }
            public decimal MontoCuota { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "Vales", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Index");
            }

            PuedeCrear = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ValesCreate", "crear");
            PuedeAprobar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ValesAprobar", "editar");
            PuedeEntregar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ValesEntregar", "editar");

            var empleadosDict = _context.Empleados
                .AsNoTracking()
                .ToDictionary(e => e.Id, e => new { e.Codigo, Nombre = $"{e.Nombres} {e.Apellidos}", e.FotoPath });

            var lista = _context.ValesEmpleado
                .AsNoTracking()
                .OrderByDescending(v => v.FechaSolicitud)
                .ToList();

            Vales = lista.Select(v =>
            {
                var emp = empleadosDict.GetValueOrDefault(v.EmpleadoId);
                return new ValeItem
                {
                    Id = v.Id,
                    Numero = v.Numero,
                    EmpleadoCodigo = emp?.Codigo ?? "—",
                    EmpleadoNombre = emp?.Nombre ?? "—",
                    FotoPath = emp?.FotoPath,
                    FechaSolicitud = v.FechaSolicitud,
                    FechaEntrega = v.FechaEntrega,
                    Monto = v.Monto,
                    Motivo = v.Motivo,
                    Estado = v.Estado,
                    Cuotas = v.Cuotas,
                    SaldoPendiente = v.SaldoPendiente,
                    MontoCuota = v.MontoCuota
                };
            }).ToList();

            TotalPendientes = lista.Count(v => v.Estado == "Solicitado");
            TotalAprobadosGerente = lista.Count(v => v.Estado == "AprobadoGerente");
            TotalAprobadosRRHH = lista.Count(v => v.Estado == "AprobadoRRHH");
            TotalEntregados = lista.Count(v => v.Estado == "Entregado" || v.Estado == "Descontado");
            MontoPendiente = lista.Where(v => v.Estado == "Solicitado" || v.Estado == "AprobadoGerente" || v.Estado == "AprobadoRRHH").Sum(v => v.Monto);
            SaldoPorDescontar = lista.Where(v => v.SaldoPendiente > 0).Sum(v => v.SaldoPendiente);

            return Page();
        }
    }
}