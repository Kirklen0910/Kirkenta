using Kirkenta.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Kirkenta.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // KPIs
        public decimal VentasHoy { get; set; }
        public decimal VentasAyer { get; set; }
        public decimal VentasMes { get; set; }
        public decimal VentasMesAnterior { get; set; }

        public int ClientesActivos { get; set; }
        public int ClientesTotal { get; set; }
        public int ProductosTotal { get; set; }
        public int ProductosStockBajo { get; set; }

        // Datos para gráficos y paneles
        public string Ventas7DiasJson { get; set; } = "{}";
        public List<TopProductoMes> TopProductosMes { get; set; } = new();
        public List<VentaReciente> UltimasVentas { get; set; } = new();

        public class TopProductoMes
        {
            public string Nombre { get; set; } = "";
            public decimal Cantidad { get; set; }
            public decimal Total { get; set; }
        }

        public class VentaReciente
        {
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public decimal Total { get; set; }
            public string ClienteNombre { get; set; } = "Consumidor final";
        }

        public void OnGet()
        {
            var hoy = DateTime.Today;
            var ayer = hoy.AddDays(-1);
            var manana = hoy.AddDays(1);

            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1);
            var inicioMesAnterior = inicioMes.AddMonths(-1);
            var finMesAnterior = inicioMes;

            // === KPIs ===
            VentasHoy = _context.Ventas
                .Where(v => v.Fecha >= hoy && v.Fecha < manana && v.Estado == "Completada")
                .Sum(v => (decimal?)v.Total) ?? 0;

            VentasAyer = _context.Ventas
                .Where(v => v.Fecha >= ayer && v.Fecha < hoy && v.Estado == "Completada")
                .Sum(v => (decimal?)v.Total) ?? 0;

            VentasMes = _context.Ventas
                .Where(v => v.Fecha >= inicioMes && v.Fecha < finMes && v.Estado == "Completada")
                .Sum(v => (decimal?)v.Total) ?? 0;

            VentasMesAnterior = _context.Ventas
                .Where(v => v.Fecha >= inicioMesAnterior && v.Fecha < finMesAnterior && v.Estado == "Completada")
                .Sum(v => (decimal?)v.Total) ?? 0;

            ClientesActivos = _context.Clientes.Count(c => c.Activo);
            ClientesTotal = _context.Clientes.Count();
            ProductosTotal = _context.Productos.Count(p => p.Activo);
            ProductosStockBajo = _context.Productos.Count(p => p.Activo && p.Stock <= p.StockMinimo);

            // === Ventas últimos 7 días ===
            var hace7Dias = hoy.AddDays(-6);
            var ventas7 = _context.Ventas
                .Where(v => v.Fecha >= hace7Dias && v.Estado == "Completada")
                .ToList();

            var porDia = new List<(string Label, decimal Total)>();
            for (int i = 0; i < 7; i++)
            {
                var dia = hace7Dias.AddDays(i);
                var totalDia = ventas7
                    .Where(v => v.Fecha.Date == dia.Date)
                    .Sum(v => v.Total);
                porDia.Add((dia.ToString("ddd dd/MM"), totalDia));
            }

            Ventas7DiasJson = JsonSerializer.Serialize(new
            {
                labels = porDia.Select(x => x.Label).ToArray(),
                data = porDia.Select(x => x.Total).ToArray()
            });

            // === Top 5 productos del mes ===
            var ventaIdsMes = _context.Ventas
                .Where(v => v.Fecha >= inicioMes && v.Fecha < finMes && v.Estado == "Completada")
                .Select(v => v.Id)
                .ToList();

            var detallesMes = _context.DetalleVentas
                .Where(d => ventaIdsMes.Contains(d.VentaId))
                .ToList();

            var productos = _context.Productos.ToList();

            TopProductosMes = detallesMes
                .GroupBy(d => d.ProductoId)
                .Select(g => new TopProductoMes
                {
                    Nombre = productos.FirstOrDefault(p => p.Id == g.Key)?.Nombre ?? "—",
                    Cantidad = g.Sum(d => d.Cantidad),
                    Total = g.Sum(d => d.Total)
                })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToList();

            // === Últimas 5 ventas ===
            var ultimas = _context.Ventas
                .Where(v => v.Estado == "Completada")
                .OrderByDescending(v => v.Fecha)
                .Take(5)
                .ToList();

            var clientes = _context.Clientes.ToList();

            UltimasVentas = ultimas.Select(v => new VentaReciente
            {
                Numero = v.Numero,
                Fecha = v.Fecha,
                Total = v.Total,
                ClienteNombre = v.ClienteId.HasValue
                    ? (clientes.FirstOrDefault(c => c.Id == v.ClienteId.Value)?.Nombre ?? "Consumidor final")
                    : "Consumidor final"
            }).ToList();
        }
    }
}