using Kirkenta.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Kirkenta.Pages.Reportes
{
    public class VentasModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public VentasModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public DateTime Desde { get; set; } = DateTime.Now.AddDays(-30);
        public DateTime Hasta { get; set; } = DateTime.Now;
        public string FiltroCliente { get; set; } = "";
        public string FiltroProducto { get; set; } = "";

        public decimal TotalVendido { get; set; }
        public int CantidadVentas { get; set; }
        public decimal TicketPromedio { get; set; }
        public int ProductosVendidos { get; set; }
        public int ClientesUnicos { get; set; }

        public List<TopProducto> TopProductos { get; set; } = new();
        public List<TopProducto> MenosProductos { get; set; } = new();
        public List<TopCliente> TopClientes { get; set; } = new();

        public string VentasPorDiaJson { get; set; } = "{}";
        public string TopProductosJson { get; set; } = "{}";
        public string TopClientesJson { get; set; } = "{}";

        public class TopProducto
        {
            public string Nombre { get; set; } = "";
            public decimal Cantidad { get; set; }
            public decimal Total { get; set; }
        }

        public class TopCliente
        {
            public string Nombre { get; set; } = "";
            public int Ventas { get; set; }
            public decimal Total { get; set; }
        }

        public void OnGet(DateTime? desde, DateTime? hasta, string? cliente, string? producto)
        {
            if (desde.HasValue) Desde = desde.Value;
            if (hasta.HasValue) Hasta = hasta.Value;
            FiltroCliente = cliente ?? "";
            FiltroProducto = producto ?? "";

            var desdeFull = Desde.Date;
            var hastaFull = Hasta.Date.AddDays(1).AddSeconds(-1);

            // Cargar catálogos
            var productos = _context.Productos.ToList();
            var clientes = _context.Clientes.ToList();

            // === Filtrar ventas ===
            var ventasQuery = _context.Ventas
                .Where(v => v.Fecha >= desdeFull && v.Fecha <= hastaFull && v.Estado == "Completada");

            // Filtro por cliente
            if (!string.IsNullOrWhiteSpace(FiltroCliente))
            {
                var cli = FiltroCliente.ToLower();
                var clienteIds = clientes
                    .Where(c => (c.Nombre ?? "").ToLower().Contains(cli) || (c.RTN ?? "").ToLower().Contains(cli))
                    .Select(c => c.Id)
                    .ToList();

                ventasQuery = ventasQuery.Where(v => v.ClienteId.HasValue && clienteIds.Contains(v.ClienteId.Value));
            }

            var ventas = ventasQuery.OrderBy(v => v.Fecha).ToList();
            var ventaIds = ventas.Select(v => v.Id).ToList();

            // === Detalles ===
            var detallesQuery = _context.DetalleVentas.Where(d => ventaIds.Contains(d.VentaId));

            // Filtro por producto
            if (!string.IsNullOrWhiteSpace(FiltroProducto))
            {
                var prodFiltro = FiltroProducto.ToLower();
                var prodIds = productos
                    .Where(p => (p.Nombre ?? "").ToLower().Contains(prodFiltro) || (p.SKU ?? "").ToLower().Contains(prodFiltro))
                    .Select(p => p.Id)
                    .ToList();

                detallesQuery = detallesQuery.Where(d => prodIds.Contains(d.ProductoId));
            }

            var detalles = detallesQuery.ToList();

            // === KPIs ===
            CantidadVentas = ventas.Count;
            TotalVendido = ventas.Sum(v => v.Total);
            TicketPromedio = CantidadVentas > 0 ? TotalVendido / CantidadVentas : 0;
            ProductosVendidos = (int)detalles.Sum(d => d.Cantidad);
            ClientesUnicos = ventas.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value).Distinct().Count();

            // === Top productos ===
            var productosAgrupados = detalles
                .GroupBy(d => d.ProductoId)
                .Select(g => new TopProducto
                {
                    Nombre = productos.FirstOrDefault(p => p.Id == g.Key)?.Nombre ?? "—",
                    Cantidad = g.Sum(d => d.Cantidad),
                    Total = g.Sum(d => d.Total)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            TopProductos = productosAgrupados.Take(10).ToList();
            MenosProductos = productosAgrupados.OrderBy(x => x.Total).Take(10).ToList();

            // === Top clientes ===
            TopClientes = ventas
                .Where(v => v.ClienteId.HasValue)
                .GroupBy(v => v.ClienteId!.Value)
                .Select(g => new TopCliente
                {
                    Nombre = clientes.FirstOrDefault(c => c.Id == g.Key)?.Nombre ?? "—",
                    Ventas = g.Count(),
                    Total = g.Sum(v => v.Total)
                })
                .OrderByDescending(x => x.Total)
                .Take(10)
                .ToList();

            // === Ventas por día ===
            var ventasPorDia = ventas
                .GroupBy(v => v.Fecha.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Fecha = g.Key.ToString("dd/MM"),
                    Total = g.Sum(v => v.Total)
                })
                .ToList();

            VentasPorDiaJson = JsonSerializer.Serialize(new
            {
                labels = ventasPorDia.Select(x => x.Fecha).ToArray(),
                data = ventasPorDia.Select(x => x.Total).ToArray()
            });

            TopProductosJson = JsonSerializer.Serialize(new
            {
                labels = TopProductos.Select(x => x.Nombre).ToArray(),
                data = TopProductos.Select(x => x.Cantidad).ToArray()
            });

            TopClientesJson = JsonSerializer.Serialize(new
            {
                labels = TopClientes.Select(x => x.Nombre).ToArray(),
                data = TopClientes.Select(x => x.Total).ToArray()
            });
        }
    }
}