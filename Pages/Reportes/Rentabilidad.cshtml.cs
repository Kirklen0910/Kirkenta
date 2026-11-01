using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace Kirkenta.Pages.Reportes
{
    /// <summary>
    /// Rentabilidad y rotación por producto en un período: unidades, ventas, costo,
    /// utilidad, margen, última venta y capital inmovilizado en stock.
    /// Sirve para detectar productos que no se mueven y productos con margen bajo.
    /// </summary>
    public class RentabilidadModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RentabilidadModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? Desde { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? Hasta { get; set; }

        /// <summary>"" = todos | "SinVentas" | "MargenBajo"</summary>
        [BindProperty(SupportsGet = true)]
        public string? Filtro { get; set; }

        /// <summary>Margen mínimo aceptable (%) para el filtro MargenBajo.</summary>
        [BindProperty(SupportsGet = true)]
        public decimal? MargenMin { get; set; }

        public List<Fila> Filas { get; set; } = new();

        public decimal TotalVentas { get; set; }
        public decimal TotalUtilidad { get; set; }
        public decimal MargenGlobalPct { get; set; }
        public int ProductosSinVentas { get; set; }
        public decimal CapitalSinMovimiento { get; set; }

        public class Fila
        {
            public int ProductoId { get; set; }
            public string? SKU { get; set; }
            public string Nombre { get; set; } = "";
            public decimal Stock { get; set; }
            public decimal CostoActual { get; set; }
            public decimal Unidades { get; set; }
            public decimal Ventas { get; set; }
            public decimal Costo { get; set; }
            public decimal Utilidad { get; set; }
            public decimal? MargenPct { get; set; }
            /// <summary>Ventas de líneas con costo conocido (base del margen).</summary>
            public decimal BaseMargen { get; set; }
            public DateTime? UltimaVenta { get; set; }
            public decimal ValorStock { get; set; }
        }

        private bool Cargar()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Reportes", "Rentabilidad", "ver"))
                return false;

            var hasta = (Hasta ?? DateTime.Today).Date;
            var desde = (Desde ?? hasta.AddDays(-90)).Date;
            if (desde > hasta) (desde, hasta) = (hasta, desde);
            Desde = desde;
            Hasta = hasta;
            var hastaExclusivo = hasta.AddDays(1);
            var margenMin = Math.Clamp(MargenMin ?? 15m, -100m, 100m);
            MargenMin = margenMin;

            // Ventas del período agrupadas por producto (sin anuladas/canceladas)
            var vendido = (from d in _context.DetalleVentas
                           join v in _context.Ventas on d.VentaId equals v.Id
                           where v.Fecha >= desde && v.Fecha < hastaExclusivo
                                 && v.Estado != "Anulada" && v.Estado != "Cancelada"
                           group d by d.ProductoId into g
                           select new
                           {
                               ProductoId = g.Key,
                               Unidades = g.Sum(x => x.Cantidad),
                               Monto = g.Sum(x => x.Subtotal),
                               Costo = g.Sum(x => x.CostoTotalLinea),
                               // Solo las líneas con costo conocido entran al cálculo del margen
                               MontoConCosto = g.Sum(x => x.CostoTotalLinea > 0 ? x.Subtotal : 0m)
                           })
                .ToDictionary(x => x.ProductoId);

            // Devoluciones aplicadas del período, para dejar las cifras netas
            var devuelto = (from dd in _context.DetalleDevoluciones
                            join dev in _context.Devoluciones on dd.DevolucionId equals dev.Id
                            where dev.Fecha >= desde && dev.Fecha < hastaExclusivo && dev.Estado == "Aplicada"
                            group dd by dd.ProductoId into g
                            select new
                            {
                                ProductoId = g.Key,
                                Unidades = g.Sum(x => x.Cantidad),
                                Monto = g.Sum(x => x.Cantidad * x.PrecioUnitario),
                                Costo = g.Sum(x => x.Cantidad * x.CostoUnitario)
                            })
                .ToDictionary(x => x.ProductoId);

            // Última venta (histórica) por producto
            var ultimas = (from d in _context.DetalleVentas
                           join v in _context.Ventas on d.VentaId equals v.Id
                           where v.Estado != "Anulada" && v.Estado != "Cancelada"
                           group v by d.ProductoId into g
                           select new { ProductoId = g.Key, Fecha = g.Max(x => x.Fecha) })
                .ToDictionary(x => x.ProductoId, x => x.Fecha);

            var productos = _context.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToList();

            var filas = new List<Fila>();
            foreach (var p in productos)
            {
                vendido.TryGetValue(p.Id, out var v);
                devuelto.TryGetValue(p.Id, out var dv);

                var unidades = (v?.Unidades ?? 0) - (dv?.Unidades ?? 0);
                var ventas = (v?.Monto ?? 0) - (dv?.Monto ?? 0);
                var costo = (v?.Costo ?? 0) - (dv?.Costo ?? 0);
                var montoConCosto = (v?.MontoConCosto ?? 0) - (dv != null && (v?.Costo ?? 0) > 0 ? dv.Monto : 0);

                var f = new Fila
                {
                    ProductoId = p.Id,
                    SKU = p.SKU,
                    Nombre = p.Nombre,
                    Stock = p.Stock,
                    CostoActual = p.PrecioCompra,
                    Unidades = unidades,
                    Ventas = ventas,
                    Costo = costo,
                    UltimaVenta = ultimas.TryGetValue(p.Id, out var uv) ? uv : (DateTime?)null,
                    ValorStock = Math.Max(p.Stock, 0) * p.PrecioCompra
                };

                if (montoConCosto > 0)
                {
                    f.BaseMargen = montoConCosto;
                    f.Utilidad = montoConCosto - costo;
                    f.MargenPct = f.Utilidad / montoConCosto * 100;
                }
                else
                {
                    f.Utilidad = 0;
                }
                filas.Add(f);
            }

            // Totales del período (antes de filtrar la lista)
            var conVentas = filas.Where(f => f.Unidades > 0).ToList();
            TotalVentas = conVentas.Sum(f => f.Ventas);
            TotalUtilidad = filas.Where(f => f.MargenPct.HasValue).Sum(f => f.Utilidad);
            var baseMargen = filas.Sum(f => f.BaseMargen);
            MargenGlobalPct = baseMargen != 0 ? TotalUtilidad / baseMargen * 100 : 0;

            var sinVentas = filas.Where(f => f.Unidades <= 0).ToList();
            ProductosSinVentas = sinVentas.Count;
            CapitalSinMovimiento = sinVentas.Sum(f => f.ValorStock);

            switch (Filtro)
            {
                case "SinVentas":
                    Filas = sinVentas.OrderByDescending(f => f.ValorStock).ThenBy(f => f.Nombre).ToList();
                    break;
                case "MargenBajo":
                    Filas = filas.Where(f => f.Unidades > 0 && f.MargenPct.HasValue && f.MargenPct.Value < margenMin)
                        .OrderBy(f => f.MargenPct).ToList();
                    break;
                default:
                    Filtro = null;
                    Filas = filas.OrderByDescending(f => f.Utilidad).ThenBy(f => f.Nombre).ToList();
                    break;
            }

            return true;
        }

        public IActionResult OnGet()
        {
            if (!Cargar())
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public IActionResult OnGetCsv()
        {
            if (!Cargar()) return RedirectToPage("/Index");

            string F(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);
            var filas = new List<string?[]>
            {
                new[] { "SKU", "Producto", "Stock", "Unidades vendidas", "Ventas", "Costo", "Utilidad", "Margen %", "Última venta", "Valor en stock (costo)" }
            };
            filas.AddRange(Filas.Select(f => new string?[]
            {
                f.SKU, f.Nombre,
                f.Stock.ToString("0.####", CultureInfo.InvariantCulture),
                f.Unidades.ToString("0.####", CultureInfo.InvariantCulture),
                F(f.Ventas), F(f.Costo), F(f.Utilidad),
                f.MargenPct.HasValue ? F(f.MargenPct.Value) : "",
                f.UltimaVenta?.ToString("yyyy-MM-dd") ?? "",
                F(f.ValorStock)
            }));
            return CsvExportHelper.Crear($"rentabilidad-productos-{DateTime.Today:yyyyMMdd}.csv", filas);
        }
    }
}
