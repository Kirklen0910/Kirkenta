using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kirkenta.Pages.Finanzas
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // KPIs
        public decimal SaldoTotalCajas { get; set; }
        public decimal SaldoTotalBancos { get; set; }
        public decimal SaldoTotal { get; set; }

        public decimal IngresosMes { get; set; }
        public decimal EgresosMes { get; set; }
        public decimal BalanceMes { get; set; }

        public decimal ValesPendientes { get; set; }
        public int CantidadValesPendientes { get; set; }

        public int CantidadCuentas { get; set; }

        // Listas
        public List<CuentaResumen> Cuentas { get; set; } = new();
        public List<MovimientoItem> UltimosMovimientos { get; set; } = new();

        public string FlujoMensualJson { get; set; } = "{}";
        public string CategoriasEgresosJson { get; set; } = "{}";

        public class CuentaResumen
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";
            public string Tipo { get; set; } = "";
            public string? Subtipo { get; set; }
            public string? Banco { get; set; }
            public decimal SaldoActual { get; set; }
        }

        public class MovimientoItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public string Tipo { get; set; } = "";
            public string Concepto { get; set; } = "";
            public decimal Monto { get; set; }
            public string CuentaNombre { get; set; } = "";
            public string? CategoriaNombre { get; set; }
            public string? CategoriaColor { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "Index", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver Finanzas";
                return RedirectToPage("/Index");
            }

            // Cuentas
            var cuentas = _context.CuentasFinancieras
                .AsNoTracking()
                .Where(c => c.Activa)
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToList();

            CantidadCuentas = cuentas.Count;
            SaldoTotalCajas = cuentas.Where(c => c.Tipo == "Caja").Sum(c => c.SaldoActual);
            SaldoTotalBancos = cuentas.Where(c => c.Tipo == "Banco").Sum(c => c.SaldoActual);
            SaldoTotal = SaldoTotalCajas + SaldoTotalBancos;

            Cuentas = cuentas.Select(c => new CuentaResumen
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Tipo = c.Tipo,
                Subtipo = c.Subtipo,
                Banco = c.Banco,
                SaldoActual = c.SaldoActual
            }).ToList();

            // Movimientos del mes
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            var movimientosMes = _context.MovimientosFinancieros
                .AsNoTracking()
                .Where(m => m.Fecha >= inicioMes && m.Fecha < finMes && m.Estado == "Activo")
                .ToList();

            IngresosMes = movimientosMes.Where(m => m.Tipo == "Ingreso").Sum(m => m.Monto);
            EgresosMes = movimientosMes.Where(m => m.Tipo == "Egreso").Sum(m => m.Monto);
            BalanceMes = IngresosMes - EgresosMes;

            // Vales pendientes
            var valesPendientes = _context.ValesEmpleado
                .AsNoTracking()
                .Where(v => v.Estado == "Entregado" && v.SaldoPendiente > 0)
                .ToList();

            ValesPendientes = valesPendientes.Sum(v => v.SaldoPendiente);
            CantidadValesPendientes = valesPendientes.Count;

            // Últimos 10 movimientos
            var cuentasDict = cuentas.ToDictionary(c => c.Id, c => c.Nombre);
            var categoriasDict = _context.CategoriasFinancieras
                .AsNoTracking()
                .ToDictionary(c => c.Id, c => new { c.Nombre, c.Color });

            UltimosMovimientos = _context.MovimientosFinancieros
                .AsNoTracking()
                .Where(m => m.Estado == "Activo")
                .OrderByDescending(m => m.Fecha)
                .ThenByDescending(m => m.Id)
                .Take(10)
                .ToList()
                .Select(m => new MovimientoItem
                {
                    Id = m.Id,
                    Numero = m.Numero,
                    Fecha = m.Fecha,
                    Tipo = m.Tipo,
                    Concepto = m.Concepto,
                    Monto = m.Monto,
                    CuentaNombre = cuentasDict.GetValueOrDefault(m.CuentaId, "—"),
                    CategoriaNombre = m.CategoriaId.HasValue
                        ? categoriasDict.GetValueOrDefault(m.CategoriaId.Value)?.Nombre
                        : null,
                    CategoriaColor = m.CategoriaId.HasValue
                        ? categoriasDict.GetValueOrDefault(m.CategoriaId.Value)?.Color
                        : null
                })
                .ToList();

            // Gráfico: últimos 6 meses (ingresos vs egresos)
            var inicioRango = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-5);
            var movimientosRango = _context.MovimientosFinancieros
                .AsNoTracking()
                .Where(m => m.Fecha >= inicioRango && m.Fecha < finMes && m.Estado == "Activo")
                .Select(m => new { m.Fecha, m.Tipo, m.Monto })
                .ToList();

            var labels = new List<string>();
            var ingresos = new List<decimal>();
            var egresos = new List<decimal>();

            for (int i = 5; i >= 0; i--)
            {
                var fecha = hoy.AddMonths(-i);
                var ini = new DateTime(fecha.Year, fecha.Month, 1);
                var fin = ini.AddMonths(1);

                labels.Add(ini.ToString("MMM yyyy"));
                ingresos.Add(movimientosRango
                    .Where(m => m.Fecha >= ini && m.Fecha < fin && m.Tipo == "Ingreso")
                    .Sum(m => m.Monto));
                egresos.Add(movimientosRango
                    .Where(m => m.Fecha >= ini && m.Fecha < fin && m.Tipo == "Egreso")
                    .Sum(m => m.Monto));
            }

            FlujoMensualJson = JsonSerializer.Serialize(new
            {
                labels = labels.ToArray(),
                ingresos = ingresos.ToArray(),
                egresos = egresos.ToArray()
            });

            // Gráfico: top categorías de egresos del mes
            var categoriasEgresos = movimientosMes
                .Where(m => m.Tipo == "Egreso" && m.CategoriaId.HasValue)
                .GroupBy(m => m.CategoriaId!.Value)
                .Select(g => new
                {
                    Nombre = categoriasDict.GetValueOrDefault(g.Key)?.Nombre ?? "—",
                    Color = categoriasDict.GetValueOrDefault(g.Key)?.Color ?? "#6b7280",
                    Total = g.Sum(m => m.Monto)
                })
                .OrderByDescending(x => x.Total)
                .Take(8)
                .ToList();

            CategoriasEgresosJson = JsonSerializer.Serialize(new
            {
                labels = categoriasEgresos.Select(x => x.Nombre).ToArray(),
                data = categoriasEgresos.Select(x => x.Total).ToArray(),
                colors = categoriasEgresos.Select(x => x.Color).ToArray()
            });

            return Page();
        }
    }
}