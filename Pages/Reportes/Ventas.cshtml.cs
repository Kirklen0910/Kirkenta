using Kirkenta.Helpers;
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

        public IActionResult OnGet(DateTime? desde, DateTime? hasta, string? cliente, string? producto)
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Reportes", "Ventas", "ver")))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            if (desde.HasValue) Desde = desde.Value;
            if (hasta.HasValue) Hasta = hasta.Value;
            FiltroCliente = cliente ?? "";
            FiltroProducto = producto ?? "";

            var desdeFull = Desde.Date;
            var hastaFull = Hasta.Date.AddDays(1).AddSeconds(-1);

            // Cargar catálogos

            // === Filtrar ventas ===
            var ventasQuery = _context.Ventas
                .Where(v => v.Fecha >= desdeFull && v.Fecha <= hastaFull && v.Estado == "Completada");

            // Filtro por cliente
            if (!string.IsNullOrWhiteSpace(FiltroCliente))
            {
                var cli = FiltroCliente.ToLower();
                var clienteIds = _context.Clientes
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
                var prodIds = _context.Productos
                    .Where(p => (p.Nombre ?? "").ToLower().Contains(prodFiltro) || (p.SKU ?? "").ToLower().Contains(prodFiltro))
                    .Select(p => p.Id)
                    .ToList();

                detallesQuery = detallesQuery.Where(d => prodIds.Contains(d.ProductoId));
            }

            var detalles = detallesQuery.ToList();

            // Con filtro de producto, cada venta cuenta solo por las líneas de ese producto y las ventas
            // que no lo tienen salen del reporte: así total, ticket, clientes y gráfica responden al filtro.
            var montoPorVenta = !string.IsNullOrWhiteSpace(FiltroProducto)
                ? detalles.GroupBy(d => d.VentaId).ToDictionary(g => g.Key, g => g.Sum(d => d.Total))
                : ventas.ToDictionary(v => v.Id, v => v.Total);
            ventas = ventas.Where(v => montoPorVenta.ContainsKey(v.Id)).ToList();
            decimal Monto(Models.Venta v) => montoPorVenta[v.Id];

            // === KPIs ===
            CantidadVentas = ventas.Count;
            TotalVendido = ventas.Sum(Monto);
            TicketPromedio = CantidadVentas > 0 ? Math.Round(TotalVendido / CantidadVentas, 2, MidpointRounding.AwayFromZero) : 0;
            ProductosVendidos = (int)detalles.Sum(d => d.Cantidad);
            ClientesUnicos = ventas.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value).Distinct().Count();

            var idsProductosUsados = detalles.Select(d => d.ProductoId).Distinct().ToList();
            var idsClientesUsados = ventas.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value).Distinct().ToList();
            var nombresProductos = _context.Productos
                .Where(p => idsProductosUsados.Contains(p.Id))
                .ToDictionary(p => p.Id, p => p.Nombre);
            var nombresClientes = _context.Clientes
                .Where(c => idsClientesUsados.Contains(c.Id))
                .ToDictionary(c => c.Id, c => c.Nombre);

            // === Top productos ===
            var productosAgrupados = detalles
                .GroupBy(d => d.ProductoId)
                .Select(g => new TopProducto
                {
                    Nombre = (nombresProductos.TryGetValue(g.Key, out var np) ? np : null) ?? "—",
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
                    Nombre = (nombresClientes.TryGetValue(g.Key, out var nc) ? nc : null) ?? "—",
                    Ventas = g.Count(),
                    Total = g.Sum(Monto)
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
                    Total = g.Sum(Monto)
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

            return Page();
        }
    }
}