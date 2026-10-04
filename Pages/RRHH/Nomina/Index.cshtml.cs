using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.RRHH.Nomina
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<NominaItem> Nominas { get; set; } = new();

        public int TotalCalculadas { get; set; }
        public int TotalAprobadas { get; set; }
        public int TotalPagadas { get; set; }
        public decimal NominaPendiente { get; set; }
        public decimal NominaPagadaMes { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeAprobar { get; set; }
        public bool PuedePagar { get; set; }

        public class NominaItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public string Tipo { get; set; } = "";
            public string PeriodoDescripcion { get; set; } = "";
            public DateTime FechaInicio { get; set; }
            public DateTime FechaFin { get; set; }
            public DateTime FechaPago { get; set; }
            public string Estado { get; set; } = "";
            public int CantidadEmpleados { get; set; }
            public decimal TotalNeto { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "Nomina", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Index");
            }

            PuedeCrear = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "NominaCreate", "crear");
            PuedeAprobar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "NominaAprobar", "editar");
            PuedePagar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "NominaPagar", "editar");

            var lista = _context.Nominas
                .AsNoTracking()
                .OrderByDescending(n => n.FechaCreacion)
                .ToList();

            Nominas = lista.Select(n => new NominaItem
            {
                Id = n.Id,
                Numero = n.Numero,
                Tipo = n.Tipo,
                PeriodoDescripcion = n.PeriodoDescripcion,
                FechaInicio = n.FechaInicio,
                FechaFin = n.FechaFin,
                FechaPago = n.FechaPago,
                Estado = n.Estado,
                CantidadEmpleados = n.CantidadEmpleados,
                TotalNeto = n.TotalNeto
            }).ToList();

            TotalCalculadas = lista.Count(n => n.Estado == "Calculada");
            TotalAprobadas = lista.Count(n => n.Estado == "Aprobada");
            TotalPagadas = lista.Count(n => n.Estado == "Pagada");
            NominaPendiente = lista.Where(n => n.Estado == "Calculada" || n.Estado == "Aprobada").Sum(n => n.TotalNeto);

            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            NominaPagadaMes = lista.Where(n => n.Estado == "Pagada" && n.FechaPagoReal.HasValue && n.FechaPagoReal.Value >= inicioMes).Sum(n => n.TotalNeto);

            return Page();
        }
    }
}