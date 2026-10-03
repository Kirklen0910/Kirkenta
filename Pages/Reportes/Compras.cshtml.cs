using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Kirkenta.Pages.Reportes
{
    public class ComprasModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ComprasModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public DateTime Desde { get; set; } = DateTime.Now.AddDays(-30);
        public DateTime Hasta { get; set; } = DateTime.Now;
        public int? FiltroProveedorId { get; set; }
        public string FiltroProducto { get; set; } = "";

        public List<Proveedor> Proveedores { get; set; } = new();

        public decimal TotalComprado { get; set; }
        public int CantidadOrdenes { get; set; }
        public int ProductosComprados { get; set; }
        public int ProveedoresActivos { get; set; }
        public decimal PromedioOrden { get; set; }

        public List<DetalleProveedorItem> DetallePorProveedor { get; set; } = new();
        public List<DetalleProductoItem> DetallePorProducto { get; set; } = new();

        public string ComprasPorDiaJson { get; set; } = "{}";
        public string TopProveedoresJson { get; set; } = "{}";
        public string TopProductosJson { get; set; } = "{}";

        public class DetalleProveedorItem
        {
            public string NombreProveedor { get; set; } = "";
            public int CantidadOrdenes { get; set; }
            public decimal TotalComprado { get; set; }
            public decimal SaldoPendiente { get; set; }
            public DateTime UltimaCompra { get; set; }
        }

        public class DetalleProductoItem
        {
            public string NombreProducto { get; set; } = "";
            public string SKU { get; set; } = "";
            public decimal CantidadComprada { get; set; }
            public decimal PrecioMinimo { get; set; }
            public decimal PrecioMaximo { get; set; }
            public decimal PrecioPromedio { get; set; }
            public decimal UltimoPrecio { get; set; }
            public decimal TotalInvertido { get; set; }
        }

        public void OnGet(DateTime? desde, DateTime? hasta, int? proveedorId, string? producto)
        {
            if (desde.HasValue) Desde = desde.Value;
            if (hasta.HasValue) Hasta = hasta.Value;
            FiltroProveedorId = proveedorId;
            FiltroProducto = producto ?? "";

            var desdeFull = Desde.Date;
            var hastaFull = Hasta.Date.AddDays(1).AddSeconds(-1);

            Proveedores = _context.Proveedores.OrderBy(p => p.Nombre).ToList();
            var productos = _context.Productos.ToList();

            // === Filtrar órdenes ===
            var ordenesQuery = _context.OrdenesCompra
                .Where(o => o.Fecha >= desdeFull && o.Fecha <= hastaFull && o.Estado != "Cancelada");

            if (FiltroProveedorId.HasValue)
            {
                ordenesQuery = ordenesQuery.Where(o => o.ProveedorId == FiltroProveedorId.Value);
            }

            var ordenes = ordenesQuery.OrderBy(o => o.Fecha).ToList();
            var ordenIds = ordenes.Select(o => o.Id).ToList();

            // === Detalles ===
            var detallesQuery = _context.DetalleOrdenesCompra.Where(d => ordenIds.Contains(d.OrdenCompraId));

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
            CantidadOrdenes = ordenes.Count;
            TotalComprado = ordenes.Sum(o => o.Total);
            ProductosComprados = (int)detalles.Sum(d => d.Cantidad);
            ProveedoresActivos = ordenes.Select(o => o.ProveedorId).Distinct().Count();
            PromedioOrden = CantidadOrdenes > 0 ? TotalComprado / CantidadOrdenes : 0;

            // === Detalle por proveedor ===
            DetallePorProveedor = ordenes
                .GroupBy(o => o.ProveedorId)
                .Select(g => new DetalleProveedorItem
                {
                    NombreProveedor = Proveedores.FirstOrDefault(p => p.Id == g.Key)?.Nombre ?? "—",
                    CantidadOrdenes = g.Count(),
                    TotalComprado = g.Sum(o => o.Total),
                    SaldoPendiente = g.Sum(o => o.Saldo),
                    UltimaCompra = g.Max(o => o.Fecha)
                })
                .OrderByDescending(x => x.TotalComprado)
                .ToList();

            // === Detalle por producto ===
            DetallePorProducto = detalles
                .GroupBy(d => d.ProductoId)
                .Select(g => new DetalleProductoItem
                {
                    NombreProducto = productos.FirstOrDefault(p => p.Id == g.Key)?.Nombre ?? "—",
                    SKU = productos.FirstOrDefault(p => p.Id == g.Key)?.SKU ?? "",
                    CantidadComprada = g.Sum(d => d.Cantidad),
                    PrecioMinimo = g.Min(d => d.PrecioUnitario),
                    PrecioMaximo = g.Max(d => d.PrecioUnitario),
                    PrecioPromedio = g.Average(d => d.PrecioUnitario),
                    UltimoPrecio = g.OrderByDescending(d => d.Id).First().PrecioUnitario,
                    TotalInvertido = g.Sum(d => d.Total)
                })
                .OrderByDescending(x => x.TotalInvertido)
                .ToList();

            // === Gráfico por día ===
            var comprasPorDia = ordenes
                .GroupBy(o => o.Fecha.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Fecha = g.Key.ToString("dd/MM"),
                    Total = g.Sum(o => o.Total)
                })
                .ToList();

            ComprasPorDiaJson = JsonSerializer.Serialize(new
            {
                labels = comprasPorDia.Select(x => x.Fecha).ToArray(),
                data = comprasPorDia.Select(x => x.Total).ToArray()
            });

            // === Top proveedores ===
            var topProv = DetallePorProveedor.Take(10).ToList();
            TopProveedoresJson = JsonSerializer.Serialize(new
            {
                labels = topProv.Select(x => x.NombreProveedor).ToArray(),
                data = topProv.Select(x => x.TotalComprado).ToArray()
            });

            // === Top productos ===
            var topProd = DetallePorProducto.Take(10).ToList();
            TopProductosJson = JsonSerializer.Serialize(new
            {
                labels = topProd.Select(x => x.NombreProducto).ToArray(),
                data = topProd.Select(x => x.CantidadComprada).ToArray()
            });
        }
    }
}