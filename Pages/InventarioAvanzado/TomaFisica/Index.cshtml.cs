using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TomaEntidad = Kirkenta.Models.TomaFisica;

namespace Kirkenta.Pages.InventarioAvanzado.TomaFisica
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        [BindProperty] public string? Descripcion { get; set; }
        [BindProperty] public int? CategoriaId { get; set; }

        public List<TomaEntidad> Tomas { get; set; } = new();
        public List<Categoria> Categorias { get; set; } = new();
        public Dictionary<int, (int Total, int Contados)> Avance { get; set; } = new();
        public bool PuedeCrear { get; set; }

        private bool Permiso(string accion) =>
            PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "TomaFisica", accion);

        private void Cargar()
        {
            PuedeCrear = Permiso("crear");
            Categorias = _context.Categorias.Where(c => c.Activa).OrderBy(c => c.Nombre).ToList();

            var query = _context.TomasFisicas.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Estado)) query = query.Where(t => t.Estado == Estado);
            Pager = new PagerInfo(Pagina, 30, query.Count());
            Tomas = query.OrderByDescending(t => t.Fecha).ThenByDescending(t => t.Id)
                .Skip(Pager.Skip).Take(Pager.PageSize).ToList();

            var ids = Tomas.Select(t => t.Id).ToList();
            var detalles = _context.DetallesTomaFisica.Where(d => ids.Contains(d.TomaFisicaId))
                .Select(d => new { d.TomaFisicaId, Contado = d.StockContado != null }).ToList();
            Avance = detalles.GroupBy(d => d.TomaFisicaId)
                .ToDictionary(g => g.Key, g => (g.Count(), g.Count(x => x.Contado)));
        }

        public IActionResult OnGet()
        {
            if (!Permiso("ver"))
            {
                TempData["Error"] = "No tienes permiso para ver las tomas físicas";
                return RedirectToPage("/Index");
            }
            Cargar();
            return Page();
        }

        public IActionResult OnPostCrear()
        {
            if (!Permiso("crear")) { TempData["Error"] = "No tienes permiso para crear tomas físicas"; return RedirectToPage(); }

            if (_context.TomasFisicas.Any(t => t.Estado == "EnConteo"))
            {
                TempData["Error"] = "Ya hay una toma física en conteo. Aplícala o cancélala antes de iniciar otra.";
                return RedirectToPage();
            }

            var productos = _context.Productos.Where(p => p.Activo);
            if (CategoriaId.HasValue) productos = productos.Where(p => p.CategoriaId == CategoriaId.Value);
            var lista = productos.OrderBy(p => p.Nombre).ToList();
            if (lista.Count == 0) { TempData["Error"] = "No hay productos para contar con ese filtro"; return RedirectToPage(); }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            using var tx = _context.Database.BeginTransaction();
            var toma = new TomaEntidad
            {
                Numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "TomaFisica"),
                Fecha = DateTime.Now,
                Descripcion = string.IsNullOrWhiteSpace(Descripcion) ? "Toma física de inventario" : Descripcion.Trim(),
                Estado = "EnConteo",
                UsuarioId = usuario?.Id,
                CategoriaId = CategoriaId
            };
            _context.TomasFisicas.Add(toma);
            _context.SaveChanges();

            foreach (var p in lista)
            {
                _context.DetallesTomaFisica.Add(new DetalleTomaFisica
                {
                    TomaFisicaId = toma.Id,
                    ProductoId = p.Id,
                    StockSistema = p.Stock,
                    CostoUnitario = p.PrecioCompra
                });
            }
            _context.SaveChanges();
            tx.Commit();

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Iniciar toma física",
                $"Inició la toma {toma.Numero} con {lista.Count} productos", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Toma física {toma.Numero} iniciada con {lista.Count} productos";
            return RedirectToPage("/InventarioAvanzado/TomaFisica/Details", new { id = toma.Id });
        }
    }
}
