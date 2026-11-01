using Microsoft.AspNetCore.Mvc;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Productos
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoriaId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Stock { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Estado { get; set; }

        [BindProperty(SupportsGet = true, Name = "p")]
        public int Pagina { get; set; } = 1;

        public PagerInfo Pager { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public List<Categoria> Categorias { get; set; } = new();
        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }
        public bool PuedeExportar { get; set; }
        public bool PuedeImportar { get; set; }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver productos";
                return RedirectToPage("/Index");
            }


            PuedeCrear = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "crear");
            PuedeEditar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "editar");
            PuedeEliminar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "eliminar");
            PuedeExportar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "ProductosExport", "ver");
            PuedeImportar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "ProductosImport", "crear");

            var query = _context.Productos.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                query = query.Where(p => p.Nombre.Contains(s) ||
                                         (p.SKU != null && p.SKU.Contains(s)) ||
                                         (p.CodigoBarras != null && p.CodigoBarras.Contains(s)));
            }
            if (CategoriaId.HasValue) query = query.Where(p => p.CategoriaId == CategoriaId);
            if (Stock == "agotado") query = query.Where(p => p.Stock <= 0);
            else if (Stock == "bajo") query = query.Where(p => p.Stock > 0 && p.Stock <= p.StockMinimo);
            else if (Stock == "disponible") query = query.Where(p => p.Stock > p.StockMinimo);
            if (Estado == "activo") query = query.Where(p => p.Activo);
            else if (Estado == "inactivo") query = query.Where(p => !p.Activo);

            Pager = new PagerInfo(Pagina, 50, query.Count());
            Productos = query.OrderBy(p => p.Nombre).Skip(Pager.Skip).Take(Pager.PageSize).ToList();
            Categorias = _context.Categorias.OrderBy(c => c.Nombre).ToList();

            return Page();
        }
    }
}