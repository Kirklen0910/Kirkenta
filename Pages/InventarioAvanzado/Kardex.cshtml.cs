using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.InventarioAvanzado
{
    public class KardexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public KardexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)] public int? ProductoId { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        public List<Producto> Productos { get; set; } = new();
        public Producto? Producto { get; set; }
        public List<MovimientoInventario> Movimientos { get; set; } = new();
        public decimal TotalEntradas { get; set; }
        public decimal TotalSalidas { get; set; }

        public IActionResult OnGet()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "Kardex", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver el kardex";
                return RedirectToPage("/Index");
            }

            Productos = _context.Productos.OrderBy(p => p.Nombre).ToList();

            if (ProductoId.HasValue)
            {
                Producto = Productos.FirstOrDefault(p => p.Id == ProductoId.Value);
                if (Producto != null)
                {
                    var query = _context.MovimientosInventario.Where(m => m.ProductoId == Producto.Id);
                    if (Desde.HasValue) query = query.Where(m => m.Fecha >= Desde.Value.Date);
                    if (Hasta.HasValue) query = query.Where(m => m.Fecha < Hasta.Value.Date.AddDays(1));

                    TotalEntradas = query.Where(m => m.Cantidad > 0).Sum(m => (decimal?)m.Cantidad) ?? 0;
                    TotalSalidas = -(query.Where(m => m.Cantidad < 0).Sum(m => (decimal?)m.Cantidad) ?? 0);

                    Pager = new PagerInfo(Pagina, 100, query.Count());
                    Movimientos = query.OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id)
                        .Skip(Pager.Skip).Take(Pager.PageSize).ToList();
                }
            }
            return Page();
        }
    }
}
