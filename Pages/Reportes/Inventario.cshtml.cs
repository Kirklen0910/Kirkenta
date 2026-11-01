using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace Kirkenta.Pages.Reportes
{
    /// <summary>Valorización de inventario (a costo promedio y a precio de venta).</summary>
    public class InventarioModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public InventarioModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int? CategoriaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SoloBajoMinimo { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IncluirInactivos { get; set; }

        public List<Categoria> Categorias { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public Dictionary<int, string> NombresCategoria { get; set; } = new();
        public decimal ValorCosto { get; set; }
        public decimal ValorVenta { get; set; }

        private bool Cargar()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Reportes", "Inventario", "ver")) return false;

            Categorias = _context.Categorias.OrderBy(c => c.Nombre).ToList();
            NombresCategoria = Categorias.ToDictionary(c => c.Id, c => c.Nombre);

            var q = _context.Productos.AsQueryable();
            if (!IncluirInactivos) q = q.Where(p => p.Activo);
            if (CategoriaId.HasValue) q = q.Where(p => p.CategoriaId == CategoriaId.Value);
            if (SoloBajoMinimo) q = q.Where(p => p.Stock <= p.StockMinimo);

            Productos = q.OrderBy(p => p.Nombre).ToList();
            ValorCosto = Productos.Sum(p => Math.Max(p.Stock, 0) * p.PrecioCompra);
            ValorVenta = Productos.Sum(p => Math.Max(p.Stock, 0) * p.PrecioVenta);
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
            var filas = new List<string?[]> { new[] { "SKU", "Producto", "Categoría", "Stock", "Mínimo", "Costo", "Valor a costo", "Precio venta", "Valor a venta" } };
            filas.AddRange(Productos.Select(p => new string?[]
            {
                p.SKU, p.Nombre,
                p.CategoriaId.HasValue && NombresCategoria.TryGetValue(p.CategoriaId.Value, out var c) ? c : "",
                p.Stock.ToString("0.####", CultureInfo.InvariantCulture), p.StockMinimo.ToString("0.####", CultureInfo.InvariantCulture),
                F(p.PrecioCompra), F(Math.Max(p.Stock, 0) * p.PrecioCompra), F(p.PrecioVenta), F(Math.Max(p.Stock, 0) * p.PrecioVenta)
            }));
            filas.Add(new string?[] { "", "TOTAL", "", "", "", "", F(ValorCosto), "", F(ValorVenta) });
            return CsvExportHelper.Crear($"valorizacion-inventario-{DateTime.Today:yyyyMMdd}.csv", filas);
        }
    }
}
