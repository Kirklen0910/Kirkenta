using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Productos
{
    /// <summary>Elige qué productos y cuántas etiquetas de código de barras imprimir.</summary>
    public class EtiquetasModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EtiquetasModel(ApplicationDbContext context) { _context = context; }

        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public int? CategoriaId { get; set; }
        [BindProperty(SupportsGet = true)] public string? Solo { get; set; }

        public List<Producto> Productos { get; set; } = new();
        public List<Categoria> Categorias { get; set; } = new();
        public int SinCodigo { get; set; }
        public bool Recortado { get; set; }
        public bool PuedeEditar { get; set; }

        private (Usuario? Usuario, string Rol) Actual()
        {
            var u = _context.Usuarios.AsNoTracking().FirstOrDefault(x => x.Username == User.Identity!.Name);
            return (u, u?.Rol ?? "Pendiente");
        }

        public IActionResult OnGet()
        {
            var (_, rol) = Actual();
            if (!PermisoHelper.TienePermiso(_context, rol, "Inventario", "Productos", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver productos";
                return RedirectToPage("/Index");
            }
            PuedeEditar = rol == "Admin" || PermisoHelper.TienePermiso(_context, rol, "Inventario", "Productos", "editar");

            var query = _context.Productos.AsNoTracking().Where(p => p.Activo);
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                query = query.Where(p => p.Nombre.Contains(s) || (p.SKU != null && p.SKU.Contains(s)) || (p.CodigoBarras != null && p.CodigoBarras.Contains(s)));
            }
            if (CategoriaId.HasValue) query = query.Where(p => p.CategoriaId == CategoriaId);
            if (Solo == "sincodigo") query = query.Where(p => p.CodigoBarras == null || p.CodigoBarras == "");

            const int limite = 300;
            var lista = query.OrderBy(p => p.Nombre).Take(limite + 1).ToList();
            Recortado = lista.Count > limite;
            Productos = lista.Take(limite).ToList();
            Categorias = _context.Categorias.AsNoTracking().OrderBy(c => c.Nombre).ToList();
            SinCodigo = _context.Productos.Count(p => p.Activo && (p.CodigoBarras == null || p.CodigoBarras == ""));
            return Page();
        }

        /// <summary>Le pone un código interno único (EAN-13 que empieza con 2) a los productos activos que no tienen código de barras.</summary>
        public IActionResult OnPostAsignar()
        {
            var (u, rol) = Actual();
            if (u == null || !(rol == "Admin" || PermisoHelper.TienePermiso(_context, rol, "Inventario", "Productos", "editar")))
            {
                TempData["Error"] = "No tienes permiso para editar productos";
                return RedirectToPage();
            }

            var sin = _context.Productos.Where(p => p.Activo && (p.CodigoBarras == null || p.CodigoBarras == "")).ToList();
            if (sin.Count == 0) { TempData["Success"] = "Todos los productos ya tienen código de barras."; return RedirectToPage(); }

            var existentes = new HashSet<string>(_context.Productos.Where(p => p.CodigoBarras != null && p.CodigoBarras != "").Select(p => p.CodigoBarras!));
            int asignados = 0;
            foreach (var p in sin)
            {
                var codigo = CodigoBarrasHelper.CodigoInterno(p.Id);
                if (existentes.Contains(codigo)) continue;
                p.CodigoBarras = codigo;
                existentes.Add(codigo);
                asignados++;
            }
            _context.SaveChanges();

            ActividadHelper.Registrar(_context, u.Id, "Códigos de barras internos", $"Se asignó código interno a {asignados} producto(s) sin código",
                HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Listo: {asignados} producto(s) recibieron un código de barras interno.";
            return RedirectToPage();
        }
    }
}
