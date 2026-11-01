using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Productos
{
    /// <summary>Hoja lista para imprimir con las etiquetas de código de barras (sin menús).</summary>
    public class EtiquetasImprimirModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EtiquetasImprimirModel(ApplicationDbContext context) { _context = context; }

        [BindProperty(SupportsGet = true)] public string? Items { get; set; }
        [BindProperty(SupportsGet = true)] public string? Tam { get; set; }
        [BindProperty(SupportsGet = true)] public bool Nombre { get; set; } = true;
        [BindProperty(SupportsGet = true)] public bool Precio { get; set; } = true;
        [BindProperty(SupportsGet = true)] public bool Sku { get; set; }
        [BindProperty(SupportsGet = true)] public bool Empresa { get; set; }

        public record Etiqueta(string Nombre, string Codigo, string Svg, decimal Precio, string? Sku);

        public FormatoEtiqueta Formato { get; set; } = EtiquetasHelper.Formatos[0];
        public List<List<Etiqueta>> Paginas { get; set; } = new();
        public List<string> Omitidos { get; set; } = new();
        public string NombreEmpresa { get; set; } = "";
        public int Total { get; set; }
        public double FuenteBase { get; set; } = 7;

        public IActionResult OnGet()
        {
            var u = _context.Usuarios.AsNoTracking().FirstOrDefault(x => x.Username == User.Identity!.Name);
            var rol = u?.Rol ?? "Pendiente";
            if (!PermisoHelper.TienePermiso(_context, rol, "Inventario", "Productos", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver productos";
                return RedirectToPage("/Index");
            }

            Formato = EtiquetasHelper.Formato(Tam);
            FuenteBase = Formato.AltoMm <= 22 ? 5.5 : Formato.AltoMm <= 30 ? 6.5 : 8;
            NombreEmpresa = _context.ConfiguracionEmpresa.AsNoTracking().Select(e => e.Nombre).FirstOrDefault() ?? "";

            var pedidos = EtiquetasHelper.ParsearItems(Items);
            var ids = pedidos.Select(p => p.Id).ToList();
            var productos = _context.Productos.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id);

            var etiquetas = new List<Etiqueta>();
            foreach (var (id, cant) in pedidos)
            {
                if (!productos.TryGetValue(id, out var p)) continue;
                var codigo = EtiquetasHelper.CodigoDe(p.CodigoBarras, p.SKU);
                if (codigo == null || !CodigoBarrasHelper.EsCodificable(codigo))
                {
                    Omitidos.Add($"{p.Nombre} (sin código de barras válido)");
                    continue;
                }
                var svg = CodigoBarrasHelper.Svg(codigo);
                for (int i = 0; i < cant; i++) etiquetas.Add(new Etiqueta(p.Nombre, codigo, svg, p.PrecioVenta, p.SKU));
            }

            Total = etiquetas.Count;
            Paginas = EtiquetasHelper.EnPaginas(etiquetas, Formato.PorPagina);
            return Page();
        }
    }
}
