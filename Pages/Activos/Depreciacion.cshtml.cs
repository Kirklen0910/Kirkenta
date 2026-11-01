using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Activos
{
    public class DepreciacionModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DepreciacionModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class Linea
        {
            public ActivoFijo Activo { get; set; } = new();
            public decimal Monto { get; set; }
            public bool YaDepreciado { get; set; }
        }

        [BindProperty(SupportsGet = true)] public int? Anio { get; set; }
        [BindProperty(SupportsGet = true)] public int? Mes { get; set; }

        public List<Linea> Lineas { get; set; } = new();
        public decimal TotalPendiente { get; set; }
        public bool PuedeEjecutar { get; set; }
        public List<(int Anio, int Mes, decimal Total, int Activos)> Historial { get; set; } = new();

        private void Normalizar()
        {
            var hoy = DateTime.Today;
            Anio ??= hoy.Year;
            Mes ??= hoy.Month;
            if (Mes < 1 || Mes > 12) Mes = hoy.Month;
            if (Anio < 2000 || Anio > hoy.Year + 1) Anio = hoy.Year;
        }

        private static decimal CalcularMonto(ActivoFijo a) => DepreciacionHelper.CalcularMonto(a);

        private void Cargar()
        {
            Normalizar();
            var finMes = new DateTime(Anio!.Value, Mes!.Value, 1).AddMonths(1).AddDays(-1);
            var yaIds = _context.DepreciacionesActivo
                .Where(d => d.Anio == Anio && d.Mes == Mes)
                .Select(d => d.ActivoFijoId).ToList();

            var activos = _context.ActivosFijos
                .Where(a => a.Estado == "Activo" && a.FechaAdquisicion <= finMes)
                .OrderBy(a => a.Codigo).ToList();

            Lineas = activos.Select(a => new Linea
            {
                Activo = a,
                YaDepreciado = yaIds.Contains(a.Id),
                Monto = yaIds.Contains(a.Id) ? 0 : CalcularMonto(a)
            }).ToList();
            TotalPendiente = Lineas.Sum(l => l.Monto);

            Historial = _context.DepreciacionesActivo
                .GroupBy(d => new { d.Anio, d.Mes })
                .Select(g => new { g.Key.Anio, g.Key.Mes, Total = g.Sum(x => x.Monto), Cant = g.Count() })
                .OrderByDescending(x => x.Anio).ThenByDescending(x => x.Mes)
                .Take(12).ToList()
                .Select(x => (Anio: x.Anio, Mes: x.Mes, Total: x.Total, Activos: x.Cant)).ToList();

            PuedeEjecutar = PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Activos", "Depreciacion", "crear");
        }

        public IActionResult OnGet()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Activos", "Depreciacion", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver la depreciación";
                return RedirectToPage("/Index");
            }
            Cargar();
            return Page();
        }

        public IActionResult OnPostEjecutar()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Activos", "Depreciacion", "crear"))
            {
                TempData["Error"] = "No tienes permiso para ejecutar la depreciación";
                return RedirectToPage();
            }

            Normalizar();
            var hoy = DateTime.Today;
            if (new DateTime(Anio!.Value, Mes!.Value, 1) > new DateTime(hoy.Year, hoy.Month, 1))
            {
                TempData["Error"] = "No se puede depreciar un mes futuro";
                return RedirectToPage(new { Anio, Mes });
            }

            Cargar();
            var pendientes = Lineas.Where(l => !l.YaDepreciado && l.Monto > 0).ToList();
            if (pendientes.Count == 0)
            {
                TempData["Error"] = "No hay activos pendientes de depreciar en ese período";
                return RedirectToPage(new { Anio, Mes });
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var fechaMov = new DateTime(Anio.Value, Mes.Value, 1).AddMonths(1).AddDays(-1);

            using var tx = _context.Database.BeginTransaction();
            try
            {
                foreach (var l in pendientes)
                {
                    var activo = _context.ActivosFijos.First(a => a.Id == l.Activo.Id);
                    activo.DepreciacionAcumulada += l.Monto;
                    _context.DepreciacionesActivo.Add(new DepreciacionActivo
                    {
                        ActivoFijoId = activo.Id,
                        Anio = Anio.Value,
                        Mes = Mes.Value,
                        Monto = l.Monto,
                        Fecha = DateTime.Now,
                        UsuarioId = usuario?.Id
                    });
                }
                _context.SaveChanges();

                // Gasto contable (no sale dinero): queda en Estado de Resultados como Depreciaciones (6.2.08)
                var total = pendientes.Sum(l => l.Monto);
                var cuentaId = MovimientoAutomaticoHelper.ResolverCuentaCajaActiva(_context);
                if (cuentaId.HasValue)
                {
                    var cat = MovimientoManualHelper.ObtenerOCrearCategoria(_context, "Egreso", "Depreciación de activos fijos", "6.2.08", "#6b7280");
                    var (mov, error) = MovimientoManualHelper.Crear(_context, "Egreso", fechaMov, cuentaId.Value, null, cat.Id,
                        total, $"Depreciación activos {Mes:D2}/{Anio}", $"DEP-{Anio}-{Mes:D2}", "Depreciacion", null,
                        usuario?.Id, soloContable: true);
                    if (mov == null)
                    {
                        tx.Rollback();
                        TempData["Error"] = $"No se pudo registrar la depreciación: {error}";
                        return RedirectToPage(new { Anio, Mes });
                    }
                }

                tx.Commit();

                ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Depreciación mensual",
                    $"Depreció {pendientes.Count} activo(s) de {Mes:D2}/{Anio} por L. {total:N2}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
                TempData["Success"] = $"Depreciación {Mes:D2}/{Anio} aplicada a {pendientes.Count} activo(s) por L. {total:N2}";
            }
            catch (Exception ex)
            {
                tx.Rollback();
                _context.ChangeTracker.Clear();
                TempData["Error"] = $"Error al depreciar: {ex.Message}";
            }
            return RedirectToPage(new { Anio, Mes });
        }
    }
}
