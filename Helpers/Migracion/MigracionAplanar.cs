using System.Globalization;
using System.Text.RegularExpressions;
using static Kirkenta.Helpers.Migracion.MigracionHistorialHelper;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>
    /// Adapta una base relacional (todo por ids) a tablas que se entienden solas, antes de analizarlas:
    /// <list type="bullet">
    /// <item>«categoria_id = 2» → se agrega la columna «categoria» con el nombre («Pinturas»).</item>
    /// <item>Tabla aparte con el número de factura de cada venta → la venta recibe su «numero».</item>
    /// <item>Existencias por sucursal o bodega → se suman en el producto («existencia»).</item>
    /// </list>
    /// No borra nada del origen: solo agrega columnas. Se puede correr varias veces (no repite).
    /// </summary>
    public static class MigracionAplanar
    {
        private static readonly HashSet<string> ColumnasNumero = new() { "numero", "numerofactura", "factura", "folio", "documento", "nodocumento", "numerodocumento", "correlativo", "nofactura" };
        private static readonly HashSet<string> ColumnasStock = new() { "stock", "existencia", "existencias", "cantidad", "disponible", "onhand", "saldo", "stockactual" };
        private static readonly HashSet<string> ColumnasNombre = new() { "nombre", "name", "descripcion", "description", "titulo", "razonsocial", "detalle" };
        private static readonly string[] RaicesDocumento = { "venta", "factura", "compra", "orden", "cotizacion", "pedido", "documento" };
        private static readonly string[] RaicesProducto = { "producto", "articulo", "item", "product" };

        public static void Aplicar(List<TablaOrigen> tablas)
        {
            Numeracion(tablas);
            Existencias(tablas);
            Costos(tablas);
            Nombres(tablas);
            Pagos(tablas);
        }

        // ---------- utilidades ----------
        /// <summary>«categoria_id», «idCategoria» → «categoria»; null si la columna no es un id de otra tabla.</summary>
        internal static string? Raiz(string header)
        {
            var n = Norm(header);
            if (n.Length > 3 && n.EndsWith("id")) return n.Substring(0, n.Length - 2);
            if (n.Length > 3 && n.StartsWith("id")) return n.Substring(2);
            return null;
        }

        private static string Singular(string n) =>
            n.EndsWith("es") && n.Length > 4 && !n.EndsWith("ces") && Regex.IsMatch(n, "(r|l|n|d|s|z)es$") ? n.Substring(0, n.Length - 2)
            : n.EndsWith("ces") ? n.Substring(0, n.Length - 3) + "z"
            : n.EndsWith("s") ? n.Substring(0, n.Length - 1) : n;

        /// <summary>La tabla a la que apunta una raíz («categoria» → «categorias»).</summary>
        private static TablaOrigen? Padre(List<TablaOrigen> tablas, string raiz) =>
            tablas.FirstOrDefault(t => Norm(t.Nombre) == raiz || Singular(Norm(t.Nombre)) == raiz || Norm(t.Nombre) == raiz + "s" || Norm(t.Nombre) == raiz + "es");

        internal static string? ColumnaId(TablaOrigen t)
        {
            var sing = Singular(Norm(t.Nombre));
            return t.Headers.FirstOrDefault(h => Norm(h) == "id")
                ?? t.Headers.FirstOrDefault(h => Norm(h) == sing + "id" || Norm(h) == "id" + sing);
        }

        private static readonly Regex RxNumero = new(@"^-?\d+([.,]\d+)?$", RegexOptions.Compiled);

        private static bool EsNumerica(TablaOrigen t, string col)
        {
            int total = 0, num = 0;
            foreach (var f in t.Rows)
            {
                if (!f.TryGetValue(col, out var v) || string.IsNullOrWhiteSpace(v)) continue;
                total++;
                if (RxNumero.IsMatch(v.Trim())) num++;
            }
            return total > 0 && num >= total * 0.9;
        }

        /// <summary>id → valor de otra columna de la misma tabla.</summary>
        private static Dictionary<string, string> Mapa(TablaOrigen t, string colId, string colValor)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in t.Rows)
                if (f.TryGetValue(colId, out var id) && !string.IsNullOrWhiteSpace(id) && f.TryGetValue(colValor, out var v) && !string.IsNullOrWhiteSpace(v))
                    d.TryAdd(id.Trim(), v.Trim());
            return d;
        }

        private static void AgregarColumna(TablaOrigen t, string nombre, string colId, Dictionary<string, string> valores)
        {
            t.Headers.Add(nombre);
            foreach (var f in t.Rows)
                f[nombre] = f.TryGetValue(colId, out var id) && id != null && valores.TryGetValue(id.Trim(), out var v) ? v : "";
        }

        private static void Anotar(TablaOrigen t, string nota)
        {
            if (t.Nota == null) t.Nota = nota;
            else if (!t.Nota.Contains(nota)) t.Nota += " " + nota;
        }

        /// <summary>Columnas que apuntan a otra tabla del archivo: (columna, tabla padre).</summary>
        private static IEnumerable<(string col, string raiz, TablaOrigen padre)> Referencias(List<TablaOrigen> tablas, TablaOrigen t)
        {
            foreach (var h in t.Headers.ToList())
            {
                var r = Raiz(h);
                if (r == null) continue;
                var p = Padre(tablas, r);
                if (p != null && !ReferenceEquals(p, t) && ColumnaId(p) != null) yield return (h, r, p);
            }
        }

        // ---------- número del documento guardado en otra tabla (facturas → ventas) ----------
        private static void Numeracion(List<TablaOrigen> tablas)
        {
            foreach (var e in tablas)
            {
                var colNum = e.Headers.FirstOrDefault(h => ColumnasNumero.Contains(Norm(h)));
                if (colNum == null) continue;
                var refs = Referencias(tablas, e).Where(x => RaicesDocumento.Any(x.raiz.StartsWith)).ToList();
                if (refs.Count != 1) continue;
                var (fk, _, doc) = refs[0];
                if (doc.Headers.Any(h => ColumnasNumero.Contains(Norm(h)))) continue;      // ya trae su número
                var valores = e.Rows.Select(f => f.GetValueOrDefault(fk)?.Trim()).Where(v => !string.IsNullOrEmpty(v)).ToList();
                if (valores.Count == 0 || valores.Distinct(StringComparer.OrdinalIgnoreCase).Count() != valores.Count) continue;   // debe ser uno a uno

                AgregarColumna(doc, "numero", ColumnaId(doc)!, Mapa(e, fk, colNum));
                Anotar(doc, $"Número tomado de «{e.Nombre}».");
                if (e.Headers.Count <= 4) { e.Unida = true; Anotar(e, $"Su número se copió a «{doc.Nombre}»."); }
            }
        }

        // ---------- existencias por sucursal / bodega → producto ----------
        private static void Existencias(List<TablaOrigen> tablas)
        {
            foreach (var s in tablas)
            {
                // movimientos, kardex o líneas no son existencias (sumarlos daría entradas + salidas)
                var nom = Norm(s.Nombre);
                if (s.Unida || new[] { "mov", "kardex", "detalle", "linea", "historial" }.Any(nom.Contains)
                    || s.Headers.Any(h => Norm(h).Contains("fecha") || Norm(h).Contains("date") || Norm(h) == "tipo")) continue;
                var colStock = s.Headers.FirstOrDefault(h => ColumnasStock.Contains(Norm(h)));
                if (colStock == null) continue;
                var refs = Referencias(tablas, s).ToList();
                if (refs.Any(x => RaicesDocumento.Any(x.raiz.StartsWith))) continue;      // líneas de un documento, no existencias
                var prod = refs.FirstOrDefault(x => RaicesProducto.Any(x.raiz.StartsWith));
                if (prod.padre == null || prod.padre.Headers.Any(h => ColumnasStock.Contains(Norm(h)))) continue;

                var suma = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                foreach (var f in s.Rows)
                {
                    var id = f.GetValueOrDefault(prod.col)?.Trim();
                    var q = Kirkenta.Helpers.Import.ParserHelper.ParsearDecimal(f.GetValueOrDefault(colStock));
                    if (string.IsNullOrEmpty(id) || q == null) continue;
                    suma[id] = suma.GetValueOrDefault(id) + q.Value;
                }
                AgregarColumna(prod.padre, "existencia", ColumnaId(prod.padre)!,
                    suma.ToDictionary(x => x.Key, x => x.Value.ToString(CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase));
                var porLugar = refs.Count > 1 ? $" (suma de todas las {refs.First(x => !ReferenceEquals(x.padre, prod.padre)).padre.Nombre})" : "";
                Anotar(prod.padre, $"Existencia tomada de «{s.Nombre}»{porLugar}.");
                s.Unida = true;
                Anotar(s, $"Sus existencias se sumaron en «{prod.padre.Nombre}»{porLugar}.");
                foreach (var lugar in refs.Where(x => !ReferenceEquals(x.padre, prod.padre)))
                    Anotar(lugar.padre, $"Kirkenta suma la existencia de todas en cada producto (ver «{s.Nombre}»).");
            }
        }

        // ---------- costo del producto: el de su última compra ----------
        private static readonly HashSet<string> ColumnasCosto = new() { "costo", "costounitario", "preciocompra", "preciocosto", "cost", "ultimocosto" };

        private static void Costos(List<TablaOrigen> tablas)
        {
            foreach (var l in tablas)
            {
                var colCosto = l.Headers.FirstOrDefault(h => ColumnasCosto.Contains(Norm(h)));
                if (colCosto == null) continue;
                var refs = Referencias(tablas, l).ToList();
                var prod = refs.FirstOrDefault(x => RaicesProducto.Any(x.raiz.StartsWith));
                if (prod.padre == null || !refs.Any(x => x.raiz.StartsWith("compra") || x.raiz.StartsWith("orden"))) continue;
                if (prod.padre.Headers.Any(h => ColumnasCosto.Contains(Norm(h)))) continue;     // el producto ya trae su costo

                // las líneas vienen en el orden en que se registraron: gana la última
                var ultimo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var f in l.Rows)
                    if (f.GetValueOrDefault(prod.col)?.Trim() is { Length: > 0 } id && f.GetValueOrDefault(colCosto)?.Trim() is { Length: > 0 } c)
                        ultimo[id] = c;
                if (ultimo.Count == 0) continue;
                AgregarColumna(prod.padre, "costo", ColumnaId(prod.padre)!, ultimo);
                Anotar(prod.padre, $"Costo tomado de su última compra («{l.Nombre}»).");
            }
        }

        // ---------- pagos de documentos: las ventas migradas entran como pagadas ----------
        private static void Pagos(List<TablaOrigen> tablas)
        {
            foreach (var t in tablas.Where(t => t.Nota == null))
            {
                var n = Norm(t.Nombre);
                if (!new[] { "pago", "abono", "cobro", "recibo" }.Any(n.Contains)) continue;
                var doc = Referencias(tablas, t).FirstOrDefault(x => RaicesDocumento.Any(x.raiz.StartsWith));
                if (doc.padre == null) continue;
                Anotar(t, $"Pagos de «{doc.padre.Nombre}»: las ventas históricas entran ya pagadas (si alguna quedó debiendo, migra su saldo como «Saldos por cobrar»).");
            }
        }

        // ---------- catálogos chicos por id (categoría, marca, cliente…) → su nombre ----------
        private static void Nombres(List<TablaOrigen> tablas)
        {
            foreach (var t in tablas.Where(t => !t.Unida))
            foreach (var (col, raiz, padre) in Referencias(tablas, t).ToList())
            {
                var colId = ColumnaId(padre)!;
                // solo tablas de nombres: id + textos (si tiene precios, fechas o más ids, no es un catálogo de nombres)
                var otras = padre.Headers.Where(h => h != colId).ToList();
                if (otras.Count == 0 || otras.Count > 4 || otras.Any(h => Raiz(h) != null || EsNumerica(padre, h))) continue;
                var colNombre = otras.FirstOrDefault(h => ColumnasNombre.Contains(Norm(h))) ?? otras[0];

                var nueva = Regex.Replace(col, @"(?i)^id[_\s-]?|[_\s-]?id$", "");
                if (nueva.Length == 0 || t.Headers.Any(h => Norm(h) == Norm(nueva))) continue;
                AgregarColumna(t, nueva, col, Mapa(padre, colId, colNombre));
                Anotar(padre, $"Sus nombres se copiaron a «{t.Nombre}».");
            }
        }
    }
}
