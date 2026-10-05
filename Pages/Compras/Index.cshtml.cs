using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kirkenta.Pages.Compras
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // KPIs
        public decimal ComprasMes { get; set; }
        public decimal ComprasMesAnterior { get; set; }
        public int OrdenesActivas { get; set; }
        public decimal CuentasPorPagar { get; set; }
        public int OrdenesPendientesPago { get; set; }
        public int ProveedoresActivos { get; set; }
        public int TotalProveedores { get; set; }

        // Paneles
        public List<TopProveedorItem> TopProveedoresMes { get; set; } = new();
        public List<UltimaOrdenItem> UltimasOrdenes { get; set; } = new();

        public string Compras6MesesJson { get; set; } = "{}";

        public class TopProveedorItem
        {
            public string Nombre { get; set; } = "";
            public int CantidadOrdenes { get; set; }
            public decimal Total { get; set; }
        }

        public class UltimaOrdenItem
        {
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public decimal Total { get; set; }
            public string ProveedorNombre { get; set; } = "";
            public string Estado { get; set; } = "";
        }

        public void OnGet()
        {
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1);
            var inicioMesAnterior = inicioMes.AddMonths(-1);

            // ⚠️ FIX EF Core 9 en .NET 10:
            // Usar HashSet<string> en lugar de string[].
            // El array de string rompe el ExpressionTreeFuncletizer.
            var estadosValidos = new HashSet<string>
            {
                "Enviada", "RecibidaParcial", "Recibida", "Pagada"
            };

            // === KPIs ===
            var ordenesMes = _context.OrdenesCompra
                .AsNoTracking()
                .Where(o => o.Fecha >= inicioMes && o.Fecha < finMes && estadosValidos.Contains(o.Estado))
                .ToList();

            var ordenesMesAnterior = _context.OrdenesCompra
                .AsNoTracking()
                .Where(o => o.Fecha >= inicioMesAnterior && o.Fecha < inicioMes && estadosValidos.Contains(o.Estado))
                .ToList();

            ComprasMes = ordenesMes.Sum(o => o.Total);
            ComprasMesAnterior = ordenesMesAnterior.Sum(o => o.Total);

            // Órdenes activas = todavía no recibidas completas ni canceladas
            OrdenesActivas = _context.OrdenesCompra
                .AsNoTracking()
                .Count(o => o.Estado == "Borrador"
                         || o.Estado == "Enviada"
                         || o.Estado == "RecibidaParcial");

            var ordenesPorPagar = _context.OrdenesCompra
                .AsNoTracking()
                .Where(o => (o.Estado == "Recibida" || o.Estado == "RecibidaParcial") && o.Saldo > 0)
                .ToList();

            CuentasPorPagar = ordenesPorPagar.Sum(o => o.Saldo);
            OrdenesPendientesPago = ordenesPorPagar.Count;

            ProveedoresActivos = _context.Proveedores.AsNoTracking().Count(p => p.Activo);
            TotalProveedores = _context.Proveedores.AsNoTracking().Count();

            // === Top proveedores del mes ===
            var proveedoresDict = _context.Proveedores
                .AsNoTracking()
                .ToDictionary(p => p.Id, p => p.Nombre);

            TopProveedoresMes = ordenesMes
                .GroupBy(o => o.ProveedorId)
                .Select(g => new TopProveedorItem
                {
                    Nombre = proveedoresDict.GetValueOrDefault(g.Key, "—"),
                    CantidadOrdenes = g.Count(),
                    Total = g.Sum(o => o.Total)
                })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToList();

            // === Últimas 5 órdenes ===
            UltimasOrdenes = _context.OrdenesCompra
                .AsNoTracking()
                .OrderByDescending(o => o.Fecha)
                .Take(5)
                .Select(o => new UltimaOrdenItem
                {
                    Numero = o.Numero,
                    Fecha = o.Fecha,
                    Total = o.Total,
                    ProveedorNombre = proveedoresDict.GetValueOrDefault(o.ProveedorId, "—"),
                    Estado = o.Estado
                })
                .ToList();

            // === Gráfico: últimos 6 meses ===
            var inicioRango = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-5);

            var ordenesRango = _context.OrdenesCompra
                .AsNoTracking()
                .Where(o => o.Fecha >= inicioRango && o.Fecha < finMes && estadosValidos.Contains(o.Estado))
                .Select(o => new { o.Fecha, o.Total })
                .ToList();

            var meses = new List<(string Label, decimal Total)>();
            for (int i = 5; i >= 0; i--)
            {
                var fecha = hoy.AddMonths(-i);
                var inicio = new DateTime(fecha.Year, fecha.Month, 1);
                var fin = inicio.AddMonths(1);

                var total = ordenesRango
                    .Where(o => o.Fecha >= inicio && o.Fecha < fin)
                    .Sum(o => o.Total);

                meses.Add((inicio.ToString("MMM yyyy"), total));
            }

            Compras6MesesJson = JsonSerializer.Serialize(new
            {
                labels = meses.Select(x => x.Label).ToArray(),
                data = meses.Select(x => x.Total).ToArray()
            });
        }
    }
}