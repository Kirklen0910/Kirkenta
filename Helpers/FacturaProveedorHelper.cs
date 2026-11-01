using System.Globalization;
using System.Text;

namespace Kirkenta.Helpers
{
    /// <summary>Utilidades para cruzar lo que la IA lee de la foto de una factura con los productos y proveedores del sistema.</summary>
    public static class FacturaProveedorHelper
    {
        /// <summary>Minúsculas, sin tildes ni signos: "Café  Grano-1LB" → "cafe grano 1lb".</summary>
        public static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            var d = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (var c in d)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            }
            return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>Parecido entre dos textos, de 0 a 1 (palabras en común sobre palabras totales; un texto dentro del otro suma).</summary>
        public static double Similitud(string? a, string? b)
        {
            var na = Normalizar(a); var nb = Normalizar(b);
            if (na.Length == 0 || nb.Length == 0) return 0;
            if (na == nb) return 1;
            var ta = na.Split(' ').ToHashSet(); var tb = nb.Split(' ').ToHashSet();
            var comunes = ta.Intersect(tb).Count();
            var union = ta.Union(tb).Count();
            var jaccard = union == 0 ? 0 : (double)comunes / union;
            var contiene = (na.Contains(nb) || nb.Contains(na)) ? 0.85 : 0;
            return Math.Max(jaccard, contiene);
        }

        /// <summary>Mejor candidato con puntaje ≥ minimo, o null. El código exacto (SKU / barras) gana siempre.</summary>
        public static T? MejorCoincidencia<T>(IEnumerable<T> candidatos, string? codigo, string? descripcion,
            Func<T, string?> nombre, Func<T, string?> sku, Func<T, string?> barras, double minimo, out double puntaje) where T : class
        {
            puntaje = 0;
            var lista = candidatos.ToList();
            var cod = Normalizar(codigo).Replace(" ", "");
            if (cod.Length >= 3)
            {
                var exacto = lista.FirstOrDefault(c =>
                    Normalizar(sku(c)).Replace(" ", "") == cod || Normalizar(barras(c)).Replace(" ", "") == cod);
                if (exacto != null) { puntaje = 1; return exacto; }
            }
            T? mejor = null;
            foreach (var c in lista)
            {
                var s = Similitud(descripcion, nombre(c));
                if (s > puntaje) { puntaje = s; mejor = c; }
            }
            if (mejor != null && puntaje >= minimo) return mejor;
            puntaje = 0;
            return null;
        }

        /// <summary>Convierte "1,234.50", "1.234,50", "L. 85" o 85 en decimal; null si no se puede.</summary>
        public static decimal? ParseDecimal(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var t = new string(s.Where(c => char.IsDigit(c) || c == '.' || c == ',' || c == '-').ToArray());
            t = t.Trim('.', ',');   // "L. 85" deja un punto suelto al inicio
            if (t.Length == 0) return null;
            var ultimoPunto = t.LastIndexOf('.'); var ultimaComa = t.LastIndexOf(',');
            if (ultimoPunto >= 0 && ultimaComa >= 0)
            {
                // el separador decimal es el que aparece al final
                if (ultimaComa > ultimoPunto) t = t.Replace(".", "").Replace(',', '.');
                else t = t.Replace(",", "");
            }
            else if (ultimaComa >= 0)
            {
                // "12,5" decimal; "1,234" miles
                var despues = t.Length - ultimaComa - 1;
                t = despues == 3 && t.Count(c => c == ',') >= 1 && ultimaComa > 0 ? t.Replace(",", "") : t.Replace(',', '.');
            }
            return decimal.TryParse(t, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        /// <summary>Diferencia entre la suma de las líneas y el total de la factura; true si es significativa (> 1 lempira o 1%).</summary>
        public static bool TotalesDifieren(decimal sumaLineas, decimal? totalFactura, decimal? isv = null)
        {
            if (!totalFactura.HasValue || totalFactura.Value <= 0) return false;
            var objetivos = new List<decimal> { totalFactura.Value };
            if (isv.HasValue && isv.Value > 0) objetivos.Add(totalFactura.Value - isv.Value);
            return objetivos.All(o => Math.Abs(sumaLineas - o) > Math.Max(1m, o * 0.01m));
        }
    }
}
