using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Kirkenta.Pages.Reportes
{
    /// <summary>
    /// Productos más / menos vendidos del período: unidades, # de ventas, cuánto generó
    /// cada producto, descuentos, costo y utilidad. Cada producto enlaza a su historial
    /// completo (ventas, clientes, facturas) con el mismo rango de fechas.
    /// </summary>
    public class ProductosVendidosModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public ProductosVendidosModel(ApplicationDbContext context) { _context = context; }

        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }
        [BindProperty(SupportsGet = true)] public int? CategoriaId { get; set; }
        [BindProperty(SupportsGet = true)] public string? Buscar { get; set; }
        /// <summary>mas | menos | ingresos | utilidad | descuentos</summary>
        [BindProperty(SupportsGet = true)] public string? Orden { get; set; }
        [BindProperty(SupportsGet = true)] public bool IncluirSinVentas { get; set; }

        public List<SelectListItem> Categorias { get; set; } = new();
        public List<Fila> Filas { get; set; } = new();

        public decimal TotalUnidades { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal TotalUtilidad { get; set; }
        public int ProductosConVentas { get; set; }

        public class Fila
        {
            public int ProductoId { get; set; }
            public string? SKU { get; set; }
            public string Nombre { get; set; } = "";
            public string Categoria { get; set; } = "";
            public decimal Unidades { get; set; }
            public int Ventas { get; set; }
            public decimal Ingresos { get; set; }
            public decimal Descuentos { get; set; }
            public decimal Costo { get; set; }
            public decimal Utilidad { get; set; }
            public decimal? MargenPct { get; set; }
            public decimal Devueltas { get; set; }
            public DateTime? UltimaVenta { get; set; }
            public decimal PorcentajeIngresos { get; set; }
        }

        private async Task<bool> CargarAsync()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Reportes", "Rentabilidad", "ver"))
                return false;

            var hasta = (Hasta ?? DateTime.Today).Date;
            var desde = (Desde ?? hasta.AddDays(-30)).Date;
            if (desde > hasta) (desde, hasta) = (hasta, desde);
            Desde = desde; Hasta = hasta;
            var hastaExcl = hasta.AddDays(1);
            if (string.IsNullOrEmpty(Orden)) Orden = "mas";

            var cats = await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync();
            Categorias = cats.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Nombre }).ToList();
            var catNombre = cats.ToDictionary(c => c.Id, c => c.Nombre);

            var det = await (from d in _context.DetalleVentas.AsNoTracking()
                             join v in _context.Ventas.AsNoTracking() on d.VentaId equals v.Id
                             where v.Fecha >= desde && v.Fecha < hastaExcl
                                   && v.Estado != "Anulada" && v.Estado != "Cancelada"
                             select new { d.ProductoId, d.VentaId, v.Fecha, d.Cantidad, d.Subtotal, d.Descuento, d.CostoTotalLinea })
                .ToListAsync();

            var vendido = det.GroupBy(x => x.ProductoId).ToDictionary(g => g.Key, g => new
            {
                Unidades = g.Sum(x => x.Cantidad),
                Ventas = g.Select(x => x.VentaId).Distinct().Count(),
                Ingresos = g.Sum(x => x.Subtotal),
                Descuentos = g.Sum(x => x.Descuento),
                Costo = g.Sum(x => x.CostoTotalLinea),
                Ultima = g.Max(x => x.Fecha)
            });

            var devueltas = await (from dd in _context.DetalleDevoluciones.AsNoTracking()
                                   join dev in _context.Devoluciones.AsNoTracking() on dd.DevolucionId equals dev.Id
                                   where dev.Fecha >= desde && dev.Fecha < hastaExcl && dev.Estado == "Aplicada"
                                   group dd by dd.ProductoId into g
                                   select new { ProductoId = g.Key, Cant = g.Sum(x => x.Cantidad) })
                .ToDictionaryAsync(x => x.ProductoId, x => x.Cant);

            var pq = _context.Productos.AsNoTracking().AsQueryable();
            if (CategoriaId.HasValue) pq = pq.Where(p => p.CategoriaId == CategoriaId);
            if (!string.IsNullOrWhiteSpace(Buscar))
            {
                var t = Buscar.Trim();
                pq = pq.Where(p => p.Nombre.Contains(t) || (p.SKU != null && p.SKU.Contains(t)));
            }
            var productos = await pq.Select(p => new { p.Id, p.SKU, p.Nombre, p.CategoriaId }).ToListAsync();

            var filas = new List<Fila>();
            foreach (var p in productos)
            {
                vendido.TryGetValue(p.Id, out var v);
                if (v == null && !IncluirSinVentas) continue;
                var f = new Fila
                {
                    ProductoId = p.Id, SKU = p.SKU, Nombre = p.Nombre,
                    Categoria = p.CategoriaId.HasValue && catNombre.TryGetValue(p.CategoriaId.Value, out var cn) ? cn : "—",
                    Unidades = v?.Unidades ?? 0, Ventas = v?.Ventas ?? 0, Ingresos = v?.Ingresos ?? 0,
                    Descuentos = v?.Descuentos ?? 0, Costo = v?.Costo ?? 0, UltimaVenta = v?.Ultima,
                    Devueltas = devueltas.TryGetValue(p.Id, out var dv) ? dv : 0
                };
                f.Utilidad = f.Ingresos - f.Costo;
                f.MargenPct = f.Ingresos > 0 && f.Costo > 0 ? f.Utilidad / f.Ingresos * 100 : (decimal?)null;
                filas.Add(f);
            }

            ProductosConVentas = filas.Count(f => f.Ventas > 0);
            TotalUnidades = filas.Sum(f => f.Unidades);
            TotalIngresos = filas.Sum(f => f.Ingresos);
            TotalDescuentos = filas.Sum(f => f.Descuentos);
            TotalUtilidad = filas.Sum(f => f.Utilidad);
            foreach (var f in filas) f.PorcentajeIngresos = TotalIngresos > 0 ? f.Ingresos / TotalIngresos * 100 : 0;

            Filas = Orden switch
            {
                "menos" => filas.OrderBy(f => f.Unidades).ThenBy(f => f.Nombre).ToList(),
                "ingresos" => filas.OrderByDescending(f => f.Ingresos).ThenBy(f => f.Nombre).ToList(),
                "utilidad" => filas.OrderByDescending(f => f.Utilidad).ThenBy(f => f.Nombre).ToList(),
                "descuentos" => filas.OrderByDescending(f => f.Descuentos).ThenBy(f => f.Nombre).ToList(),
                _ => filas.OrderByDescending(f => f.Unidades).ThenBy(f => f.Nombre).ToList()
            };
            return true;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!await CargarAsync())
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnGetCsvAsync()
        {
            if (!await CargarAsync()) return RedirectToPage("/Index");
            string F(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);
            var filas = new List<string?[]>
            {
                new[] { "SKU", "Producto", "Categoría", "Unidades", "# Ventas", "Ingresos (sin ISV)", "Descuentos", "Costo", "Utilidad", "Margen %", "Devueltas", "Última venta", "% de ingresos" }
            };
            filas.AddRange(Filas.Select(f => new string?[]
            {
                f.SKU, f.Nombre, f.Categoria,
                f.Unidades.ToString("0.####", CultureInfo.InvariantCulture), f.Ventas.ToString(),
                F(f.Ingresos), F(f.Descuentos), F(f.Costo), F(f.Utilidad),
                f.MargenPct.HasValue ? F(f.MargenPct.Value) : "",
                f.Devueltas.ToString("0.####", CultureInfo.InvariantCulture),
                f.UltimaVenta?.ToString("yyyy-MM-dd") ?? "", F(f.PorcentajeIngresos)
            }));
            return CsvExportHelper.Crear($"productos-vendidos-{Desde:yyyyMMdd}-{Hasta:yyyyMMdd}.csv", filas);
        }
    }
}
