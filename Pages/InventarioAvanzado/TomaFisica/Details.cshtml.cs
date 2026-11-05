using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TomaEntidad = Kirkenta.Models.TomaFisica;

namespace Kirkenta.Pages.InventarioAvanzado.TomaFisica
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class Fila
        {
            public DetalleTomaFisica Detalle { get; set; } = new();
            public Producto? Producto { get; set; }
            public decimal StockActual => Producto?.Stock ?? 0;
            public decimal? Diferencia => Detalle.StockContado.HasValue ? Detalle.StockContado.Value - Detalle.StockSistema : null;
        }

        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public bool SoloDiferencias { get; set; }
        [BindProperty(SupportsGet = true)] public bool SoloPendientes { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        public TomaEntidad Toma { get; set; } = new();
        public List<Fila> Filas { get; set; } = new();
        public bool PuedeEditar { get; set; }
        public int Total { get; set; }
        public int Contados { get; set; }
        public int ConDiferencia { get; set; }
        public decimal ImpactoValor { get; set; }
        public int StockCambio { get; set; }

        private bool Permiso(string accion) =>
            PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "TomaFisica", accion);

        private bool Cargar(int id)
        {
            var toma = _context.TomasFisicas.FirstOrDefault(t => t.Id == id);
            if (toma == null) return false;
            Toma = toma;
            PuedeEditar = Permiso("editar") && toma.Estado == "EnConteo";

            var todos = _context.DetallesTomaFisica.Where(d => d.TomaFisicaId == id).ToList();
            var productos = _context.Productos.ToDictionary(p => p.Id);
            Total = todos.Count;
            Contados = todos.Count(d => d.StockContado.HasValue);
            var difs = todos.Where(d => d.StockContado.HasValue && d.StockContado.Value != d.StockSistema).ToList();
            ConDiferencia = difs.Count;
            ImpactoValor = difs.Sum(d => Math.Round((d.StockContado!.Value - d.StockSistema) * d.CostoUnitario, 2));
            StockCambio = todos.Count(d => productos.TryGetValue(d.ProductoId, out var p) && p.Stock != d.StockSistema);

            IEnumerable<DetalleTomaFisica> vista = todos;
            if (SoloPendientes) vista = vista.Where(d => !d.StockContado.HasValue);
            if (SoloDiferencias) vista = vista.Where(d => d.StockContado.HasValue && d.StockContado.Value != d.StockSistema);
            var filas = vista.Select(d => new Fila { Detalle = d, Producto = productos.TryGetValue(d.ProductoId, out var p) ? p : null }).ToList();
            if (!string.IsNullOrWhiteSpace(Q))
            {
                // todas las palabras, sin importar tildes ni mayúsculas
                var palabras = TomaFisicaConteoHelper.Normalizar(Q).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                filas = filas.Where(f => f.Producto != null && palabras.All(w =>
                    TomaFisicaConteoHelper.Normalizar(f.Producto.Nombre + " " + f.Producto.SKU + " " + f.Producto.CodigoBarras).Contains(w))).ToList();
            }
            filas = filas.OrderBy(f => f.Producto?.Nombre).ToList();

            Pager = new PagerInfo(Pagina, 100, filas.Count);
            Filas = filas.Skip(Pager.Skip).Take(Pager.PageSize).ToList();
            return true;
        }

        /// <summary>Código de barras / SKU → detalle, para que el escáner ubique el producto al instante.</summary>
        public Dictionary<string, int> Codigos { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            if (!Permiso("ver")) { TempData["Error"] = "No tienes permiso para ver tomas físicas"; return RedirectToPage("/Index"); }
            if (!Cargar(id)) { TempData["Error"] = "Toma física no encontrada"; return RedirectToPage("/InventarioAvanzado/TomaFisica/Index"); }
            if (PuedeEditar) Codigos = TomaFisicaConteoHelper.IndiceCodigos(_context, id);
            return Page();
        }

        private static object Json(TomaFisicaConteoHelper.Item i) => new
        {
            id = i.DetalleId, nombre = i.Nombre, sku = i.Sku, codigo = i.Codigo, unidad = i.Unidad,
            sistema = i.Sistema, contado = i.Contado
        };

        /// <summary>Buscador del conteo: devuelve los productos de la toma que coinciden.</summary>
        public IActionResult OnGetBuscar(int id, string? q, int? detalle)
        {
            if (!Permiso("ver")) return new JsonResult(new { error = "Sin permiso" }) { StatusCode = 403 };
            if (detalle.HasValue)
            {
                var uno = TomaFisicaConteoHelper.Uno(_context, id, detalle.Value);
                return new JsonResult(new { items = uno == null ? new object[0] : new[] { Json(uno) } });
            }
            return new JsonResult(new { items = TomaFisicaConteoHelper.Buscar(_context, id, q).Select(Json) });
        }

        /// <summary>Registra el conteo de un producto sin recargar la página (escáner, buscador o tabla).</summary>
        public IActionResult OnPostContar(int id, int detalleId, string? cantidad, bool sumar)
        {
            if (!Permiso("editar")) return new JsonResult(new { error = "No tienes permiso para registrar conteos" }) { StatusCode = 403 };
            decimal? valor = null;
            var texto = cantidad?.Trim().Replace(',', '.');
            if (!string.IsNullOrEmpty(texto))
            {
                if (!decimal.TryParse(texto, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var v))
                    return new JsonResult(new { error = "Escribe una cantidad válida" }) { StatusCode = 400 };
                valor = v;
            }
            var (item, error) = TomaFisicaConteoHelper.Contar(_context, id, detalleId, valor, sumar);
            if (error != null) return new JsonResult(new { error }) { StatusCode = 400 };
            var t = TomaFisicaConteoHelper.Totales(_context, id);
            return new JsonResult(new { item = Json(item!), total = t.Total, contados = t.Contados, conDiferencia = t.ConDiferencia, impacto = t.Impacto });
        }

        /// <summary>Guarda los conteos de la página (campos vacíos no se tocan).</summary>
        public IActionResult OnPostGuardar(int id, Dictionary<int, string?> conteos)
        {
            if (!Permiso("editar")) { TempData["Error"] = "No tienes permiso para registrar conteos"; return RedirectToPage(new { id }); }
            var toma = _context.TomasFisicas.FirstOrDefault(t => t.Id == id);
            if (toma == null || toma.Estado != "EnConteo") { TempData["Error"] = "La toma ya no está en conteo"; return RedirectToPage(new { id }); }

            var detalles = _context.DetallesTomaFisica.Where(d => d.TomaFisicaId == id).ToList();
            var porId = detalles.ToDictionary(d => d.Id);
            var guardados = 0;
            var invalidos = 0;
            foreach (var kv in conteos)
            {
                if (!porId.TryGetValue(kv.Key, out var d)) continue;
                var texto = kv.Value?.Trim().Replace(',', '.');
                if (string.IsNullOrEmpty(texto)) continue;
                if (!decimal.TryParse(texto, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var valor) || valor < 0)
                {
                    invalidos++;
                    continue;
                }
                d.StockContado = valor;
                guardados++;
            }
            _context.SaveChanges();

            TempData["Success"] = $"{guardados} conteo(s) guardado(s)" + (invalidos > 0 ? $"; {invalidos} valor(es) inválido(s) ignorado(s)" : "");
            return RedirectToPage(new { id, Q, SoloDiferencias, SoloPendientes, p = Pagina });
        }

        /// <summary>Aplica las diferencias contadas al inventario (con kardex).</summary>
        public IActionResult OnPostAplicar(int id)
        {
            if (!Permiso("editar")) { TempData["Error"] = "No tienes permiso para aplicar la toma"; return RedirectToPage(new { id }); }
            var toma = _context.TomasFisicas.FirstOrDefault(t => t.Id == id);
            if (toma == null || toma.Estado != "EnConteo") { TempData["Error"] = "La toma ya no está en conteo"; return RedirectToPage(new { id }); }

            var detalles = _context.DetallesTomaFisica.Where(d => d.TomaFisicaId == id && d.StockContado != null).ToList();
            if (detalles.Count == 0) { TempData["Error"] = "No has registrado ningún conteo"; return RedirectToPage(new { id }); }

            var ids = detalles.Select(d => d.ProductoId).ToList();
            var productos = _context.Productos.Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id);
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);

            var ajustados = 0;
            try
            {
                using var tx = _context.Database.BeginTransaction();
                foreach (var d in detalles)
                {
                    if (!productos.TryGetValue(d.ProductoId, out var p)) continue;
                    var mov = InventarioHelper.Ajustar(_context, p, d.StockContado!.Value, "TomaFisica", usuario?.Id,
                        $"Toma física {toma.Numero}", toma.Numero, toma.Id);
                    if (mov != null) ajustados++;
                }
                toma.Estado = "Aplicada";
                toma.FechaAplicacion = DateTime.Now;
                _context.SaveChanges();
                tx.Commit();
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                TempData["Error"] = $"No se pudo aplicar la toma: {ex.Message}";
                return RedirectToPage(new { id });
            }

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Aplicar toma física",
                $"Aplicó la toma {toma.Numero}: {ajustados} producto(s) ajustado(s)", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Toma {toma.Numero} aplicada: {ajustados} producto(s) ajustado(s) en el inventario";
            return RedirectToPage(new { id });
        }

        public IActionResult OnPostCancelar(int id)
        {
            if (!Permiso("editar")) { TempData["Error"] = "No tienes permiso para cancelar la toma"; return RedirectToPage(new { id }); }
            var toma = _context.TomasFisicas.FirstOrDefault(t => t.Id == id);
            if (toma != null && toma.Estado == "EnConteo")
            {
                toma.Estado = "Cancelada";
                _context.SaveChanges();
                TempData["Success"] = $"Toma {toma.Numero} cancelada: el inventario no se modificó";
            }
            return RedirectToPage(new { id });
        }
    }
}
