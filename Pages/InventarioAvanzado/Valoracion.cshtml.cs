using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.InventarioAvanzado
{
    public class ValoracionModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ValoracionModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class FilaCategoria
        {
            public string Nombre { get; set; } = "";
            public int Productos { get; set; }
            public decimal Unidades { get; set; }
            public decimal ValorCosto { get; set; }
            public decimal ValorVenta { get; set; }
        }

        [BindProperty(SupportsGet = true)] public string? Vista { get; set; }

        public List<FilaCategoria> PorCategoria { get; set; } = new();
        public List<Producto> BajoMinimo { get; set; } = new();
        public List<Producto> SinMovimiento { get; set; } = new();
        public decimal ValorCostoTotal { get; set; }
        public decimal ValorVentaTotal { get; set; }
        public decimal UtilidadPotencial => ValorVentaTotal - ValorCostoTotal;
        public int TotalProductos { get; set; }
        public decimal TotalUnidades { get; set; }
        public int StockNegativo { get; set; }

        public IActionResult OnGet()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "Valoracion", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver la valoración";
                return RedirectToPage("/Index");
            }

            var productos = _context.Productos.Where(p => p.Activo).ToList();
            var categorias = _context.Categorias.ToDictionary(c => c.Id, c => c.Nombre);

            TotalProductos = productos.Count;
            TotalUnidades = productos.Where(p => p.Stock > 0).Sum(p => p.Stock);
            ValorCostoTotal = productos.Where(p => p.Stock > 0).Sum(p => Math.Round(p.Stock * p.PrecioCompra, 2, MidpointRounding.AwayFromZero));
            ValorVentaTotal = productos.Where(p => p.Stock > 0).Sum(p => Math.Round(p.Stock * p.PrecioVenta, 2, MidpointRounding.AwayFromZero));
            StockNegativo = productos.Count(p => p.Stock < 0);

            PorCategoria = productos
                .GroupBy(p => p.CategoriaId.HasValue && categorias.ContainsKey(p.CategoriaId.Value)
                    ? categorias[p.CategoriaId.Value] : "Sin categoría")
                .Select(g => new FilaCategoria
                {
                    Nombre = g.Key,
                    Productos = g.Count(),
                    Unidades = g.Where(p => p.Stock > 0).Sum(p => p.Stock),
                    ValorCosto = g.Where(p => p.Stock > 0).Sum(p => Math.Round(p.Stock * p.PrecioCompra, 2, MidpointRounding.AwayFromZero)),
                    ValorVenta = g.Where(p => p.Stock > 0).Sum(p => Math.Round(p.Stock * p.PrecioVenta, 2, MidpointRounding.AwayFromZero))
                })
                .OrderByDescending(f => f.ValorCosto).ToList();

            BajoMinimo = productos.Where(p => p.StockMinimo > 0 && p.Stock <= p.StockMinimo)
                .OrderBy(p => p.Stock - p.StockMinimo).Take(100).ToList();

            // Productos con stock que no se han movido en 90 días (capital inmovilizado)
            var limite = DateTime.Now.AddDays(-90);
            var conMovimiento = _context.MovimientosInventario.Where(m => m.Fecha >= limite)
                .Select(m => m.ProductoId).Distinct().ToList();
            SinMovimiento = productos.Where(p => p.Stock > 0 && !conMovimiento.Contains(p.Id))
                .OrderByDescending(p => p.Stock * p.PrecioCompra).Take(50).ToList();

            return Page();
        }
    }
}
