using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Productos
{
    public class KardexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public KardexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int? ProductoId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? Desde { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? Hasta { get; set; }

        public List<Producto> Productos { get; set; } = new();
        public Producto? Producto { get; set; }
        public List<MovimientoInventario> Movimientos { get; set; } = new();
        public Dictionary<int, string> Usuarios { get; set; } = new();
        public bool PuedeAjustar { get; set; }

        public decimal TotalEntradas { get; set; }
        public decimal TotalSalidas { get; set; }
        public decimal ValorInventario { get; set; }

        public IActionResult OnGet()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "Productos", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver el kardex";
                return RedirectToPage("/Index");
            }

            PuedeAjustar = PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "Productos", "editar");
            Productos = _context.Productos.OrderBy(p => p.Nombre).ToList();
            ValorInventario = Productos.Sum(p => Math.Max(p.Stock, 0) * p.PrecioCompra);

            if (ProductoId.HasValue)
            {
                Producto = Productos.FirstOrDefault(p => p.Id == ProductoId.Value);
                if (Producto != null)
                {
                    var q = _context.MovimientosInventario.Where(m => m.ProductoId == Producto.Id);
                    if (Desde.HasValue) q = q.Where(m => m.Fecha >= Desde.Value.Date);
                    if (Hasta.HasValue) q = q.Where(m => m.Fecha < Hasta.Value.Date.AddDays(1));

                    Movimientos = q.OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id).Take(500).ToList();
                    TotalEntradas = Movimientos.Where(m => m.Cantidad > 0).Sum(m => m.Cantidad);
                    TotalSalidas = Movimientos.Where(m => m.Cantidad < 0).Sum(m => -m.Cantidad);

                    var ids = Movimientos.Where(m => m.UsuarioId.HasValue).Select(m => m.UsuarioId!.Value).Distinct().ToList();
                    Usuarios = _context.Usuarios.Where(u => ids.Contains(u.Id))
                        .ToDictionary(u => u.Id, u => u.Username);
                }
            }

            return Page();
        }
    }
}
