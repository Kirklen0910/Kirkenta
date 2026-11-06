using System.Globalization;
using System.Text;
using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Conteo rápido de la toma física: ubicar un producto por código (escáner o cámara) o por nombre,
    /// y registrar su cantidad fijándola o sumando a lo ya contado.
    /// </summary>
    public static class TomaFisicaConteoHelper
    {
        public record Item(int DetalleId, int ProductoId, string Nombre, string? Sku, string? Codigo, string Unidad,
                           decimal Sistema, decimal? Contado);

        public record Resumen(int Total, int Contados, int ConDiferencia, decimal Impacto);

        public static string Normalizar(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var d = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (var c in d) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString();
        }

        private static List<Item> Items(ApplicationDbContext db, int tomaId) =>
            (from d in db.DetallesTomaFisica.AsNoTracking()
             join p in db.Productos.AsNoTracking() on d.ProductoId equals p.Id
             where d.TomaFisicaId == tomaId
             select new Item(d.Id, p.Id, p.Nombre, p.SKU, p.CodigoBarras, p.UnidadMedida, d.StockSistema, d.StockContado)).ToList();

        /// <summary>Código de barras o SKU (en minúsculas) → detalle, para ubicar al instante lo que se escanea.</summary>
        public static Dictionary<string, int> IndiceCodigos(ApplicationDbContext db, int tomaId)
        {
            var r = new Dictionary<string, int>();
            var items = Items(db, tomaId);
            foreach (var i in items)
            {
                // el código de barras manda sobre el SKU si dos productos chocan
                if (!string.IsNullOrWhiteSpace(i.Codigo)) r[i.Codigo.Trim().ToLowerInvariant()] = i.DetalleId;
            }
            foreach (var i in items)
            {
                var k = i.Sku?.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(k) && !r.ContainsKey(k)) r[k] = i.DetalleId;
            }
            return r;
        }

        /// <summary>
        /// Busca en los productos de la toma: primero el código o SKU exacto, luego los que tienen
        /// todas las palabras escritas (sin importar tildes ni mayúsculas) en nombre, SKU o código.
        /// </summary>
        public static List<Item> Buscar(ApplicationDbContext db, int tomaId, string? q, int max = 8)
        {
            var texto = Normalizar(q);
            if (texto.Length == 0) return new();
            var todos = Items(db, tomaId);
            var exactos = todos.Where(i => Normalizar(i.Codigo) == texto || Normalizar(i.Sku) == texto).ToList();
            var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var parecidos = todos
                .Where(i => !exactos.Contains(i))
                .Select(i => new { i, t = Normalizar(i.Nombre + " " + i.Sku + " " + i.Codigo), n = Normalizar(i.Nombre) })
                .Where(x => palabras.All(w => x.t.Contains(w)))
                .OrderByDescending(x => x.n.StartsWith(palabras[0]))
                .ThenBy(x => x.n.Length)
                .Select(x => x.i);
            return exactos.Concat(parecidos).Take(max).ToList();
        }

        /// <summary>
        /// Mete en la toma en conteo los productos activos que no están en ella: los creados después de
        /// iniciarla (una conversión, una compra, uno nuevo) o reactivados. Entran con el stock que tienen
        /// ahora. Respeta la categoría de la toma; en tomas de antes sin ese dato, la deduce: si todos sus
        /// productos son de una categoría y quedaron fuera productos de otras que ya existían, era por categoría.
        /// Devuelve los productos agregados.
        /// </summary>
        public static List<string> AgregarNuevos(ApplicationDbContext db, int tomaId)
        {
            var toma = db.TomasFisicas.FirstOrDefault(t => t.Id == tomaId);
            if (toma == null || toma.Estado != "EnConteo") return new();

            var enToma = db.DetallesTomaFisica.Where(d => d.TomaFisicaId == tomaId).Select(d => d.ProductoId).ToHashSet();
            var activos = db.Productos.Where(p => p.Activo)
                .Select(p => new { p.Id, p.CategoriaId, p.FechaCreacion }).ToList();
            var faltan = activos.Where(p => !enToma.Contains(p.Id)).ToList();
            if (faltan.Count == 0) return new();

            var categoria = toma.CategoriaId;
            if (categoria == null)
            {
                var cats = activos.Where(p => enToma.Contains(p.Id)).Select(p => p.CategoriaId).Distinct().ToList();
                if (cats.Count == 1 && cats[0] != null
                    && faltan.Any(p => p.CategoriaId != cats[0] && p.FechaCreacion <= toma.Fecha))
                    categoria = cats[0];
            }
            if (categoria != null) faltan = faltan.Where(p => p.CategoriaId == categoria).ToList();
            if (faltan.Count == 0) return new();

            var ids = faltan.Select(p => p.Id).ToList();
            var productos = db.Productos.Where(p => ids.Contains(p.Id)).OrderBy(p => p.Nombre).ToList();
            foreach (var p in productos)
                db.DetallesTomaFisica.Add(new Models.DetalleTomaFisica
                {
                    TomaFisicaId = tomaId, ProductoId = p.Id, StockSistema = p.Stock, CostoUnitario = p.PrecioCompra
                });
            db.SaveChanges();
            return productos.Select(p => p.Nombre).ToList();
        }

        public static Item? Uno(ApplicationDbContext db, int tomaId, int detalleId) =>
            Items(db, tomaId).FirstOrDefault(i => i.DetalleId == detalleId);

        public static Resumen Totales(ApplicationDbContext db, int tomaId)
        {
            var d = db.DetallesTomaFisica.AsNoTracking().Where(x => x.TomaFisicaId == tomaId)
                .Select(x => new { x.StockSistema, x.StockContado, x.CostoUnitario }).ToList();
            var difs = d.Where(x => x.StockContado.HasValue && x.StockContado.Value != x.StockSistema).ToList();
            return new Resumen(d.Count, d.Count(x => x.StockContado.HasValue), difs.Count,
                difs.Sum(x => Math.Round((x.StockContado!.Value - x.StockSistema) * x.CostoUnitario, 2, MidpointRounding.AwayFromZero)));
        }

        /// <summary>
        /// Registra el conteo de un producto. <paramref name="sumar"/>: suma a lo ya contado (cada escaneo +1,
        /// o una caja más); si no, la cantidad queda tal cual. null en cantidad borra el conteo (vuelve a pendiente).
        /// </summary>
        public static (Item? item, string? error) Contar(ApplicationDbContext db, int tomaId, int detalleId, decimal? cantidad, bool sumar)
        {
            var toma = db.TomasFisicas.FirstOrDefault(t => t.Id == tomaId);
            if (toma == null || toma.Estado != "EnConteo") return (null, "La toma ya no está en conteo");
            var d = db.DetallesTomaFisica.FirstOrDefault(x => x.Id == detalleId && x.TomaFisicaId == tomaId);
            if (d == null) return (null, "Ese producto no está en esta toma");
            if (cantidad.HasValue && cantidad.Value < 0 && !sumar) return (null, "La cantidad no puede ser negativa");
            if (cantidad.HasValue && cantidad.Value > 999_999_999m) return (null, "La cantidad es demasiado grande");

            if (cantidad == null) d.StockContado = null;
            else if (sumar)
            {
                var nuevo = (d.StockContado ?? 0) + cantidad.Value;
                if (nuevo < 0) return (null, "Lo contado no puede quedar en negativo");
                d.StockContado = nuevo;
            }
            else d.StockContado = cantidad.Value;
            db.SaveChanges();
            return (Uno(db, tomaId, detalleId), null);
        }
    }
}
