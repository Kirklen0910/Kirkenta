using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Finanzas.Conciliacion
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ConciliacionItem> Conciliaciones { get; set; } = new();

        public int TotalAbiertas { get; set; }
        public int TotalEnRevision { get; set; }
        public int TotalConciliadas { get; set; }
        public int TotalCanceladas { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeCerrar { get; set; }

        public class ConciliacionItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public string CuentaNombre { get; set; } = "";
            public string Banco { get; set; } = "";
            public DateTime FechaInicio { get; set; }
            public DateTime FechaFin { get; set; }
            public decimal SaldoBanco { get; set; }
            public decimal SaldoSistema { get; set; }
            public decimal Diferencia { get; set; }
            public string Estado { get; set; } = "";
            public int TotalMatcheadas { get; set; }
            public int TotalNoMatcheadas { get; set; }
            public DateTime FechaCreacion { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "Conciliacion", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Index");
            }

            PuedeCrear = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCreate", "crear");
            PuedeCerrar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCerrar", "editar");

            var cuentasDict = _context.CuentasFinancieras
                .AsNoTracking()
                .ToDictionary(c => c.Id, c => new { c.Nombre, c.Banco });

            var lista = _context.ConciliacionesBancarias                .AsNoTracking()
                .OrderByDescending(c => c.FechaCreacion)
                .ToList();

            Conciliaciones = lista.Select(c => new ConciliacionItem
            {
                Id = c.Id,
                Numero = c.Numero,
                CuentaNombre = cuentasDict.GetValueOrDefault(c.CuentaId)?.Nombre ?? "—",
                Banco = cuentasDict.GetValueOrDefault(c.CuentaId)?.Banco ?? "",
                FechaInicio = c.FechaInicio,
                FechaFin = c.FechaFin,
                SaldoBanco = c.SaldoBanco,
                SaldoSistema = c.SaldoSistema,
                Diferencia = c.Diferencia,
                Estado = c.Estado,
                TotalMatcheadas = c.TotalMatcheadas,
                TotalNoMatcheadas = c.TotalNoMatcheadas,
                FechaCreacion = c.FechaCreacion
            }).ToList();

            TotalAbiertas = lista.Count(c => c.Estado == "Abierta");
            TotalEnRevision = lista.Count(c => c.Estado == "EnRevision");
            TotalConciliadas = lista.Count(c => c.Estado == "Conciliada");
            TotalCanceladas = lista.Count(c => c.Estado == "Cancelada");

            return Page();
        }
    }
}