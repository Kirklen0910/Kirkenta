using System.Globalization;
using System.Text.RegularExpressions;

namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Utilidades de parseo robustas para valores de importación.
    /// Entiende los formatos que aparecen en archivos reales: "1,234.50", "1.234,50",
    /// "12,5", "L. 1,250.00", "15%", "(100)", "30 días", etc.
    /// </summary>
    public static class ParserHelper
    {
        private static readonly HashSet<string> Verdaderos = new()
        {
            "1", "true", "t", "si", "s", "yes", "y", "activo", "activa", "on", "verdadero", "x", "habilitado", "vigente"
        };

        private static readonly HashSet<string> Falsos = new()
        {
            "0", "false", "f", "no", "n", "inactivo", "inactiva", "off", "falso", "deshabilitado", "baja"
        };

        /// <summary>
        /// Parsea un bool. Devuelve null si el valor está vacío o no es reconocible.
        /// </summary>
        public static bool? ParsearBool(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var n = ColumnMapper.Normalizar(valor); // sin acentos: "Sí" => "si"
            if (n.Length == 0) return null;

            if (Verdaderos.Contains(n)) return true;
            if (Falsos.Contains(n)) return false;

            // "1.0" / "0.0" (Excel) => normalizado "10" / "00"
            var d = ParsearDecimal(valor);
            if (d == 1m) return true;
            if (d == 0m) return false;

            return null;
        }

        /// <summary>
        /// Parsea un entero. Acepta "30", "30.0", "30 días". Devuelve null si está vacío,
        /// no es numérico, tiene decimales reales o se sale del rango.
        /// </summary>
        public static int? ParsearInt(string? valor)
        {
            var d = ParsearDecimal(valor);
            if (d == null) return null;
            if (d.Value != decimal.Truncate(d.Value)) return null;
            if (d.Value > int.MaxValue || d.Value < int.MinValue) return null;
            return (int)d.Value;
        }

        /// <summary>
        /// Parsea un decimal. Limpia monedas (L., Lps, HNL, USD, $), %, espacios y NBSP.
        /// El último separador (coma o punto) es el decimal, salvo que sea una coma/punto
        /// único seguido de exactamente 3 dígitos (miles).
        /// </summary>
        public static decimal? ParsearDecimal(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var s = valor.Trim();

            bool negativo = false;
            if (s.StartsWith("(") && s.EndsWith(")"))
            {
                negativo = true;
                s = s.Substring(1, s.Length - 2);
            }

            s = Regex.Replace(s, @"(?i)lps\.?|hnl|usd|l\.", "");
            // Solo dejamos dígitos, separadores y signo
            s = Regex.Replace(s, @"[^\d,.\-+]", "");
            if (s.Length == 0) return null;

            if (s.StartsWith("-")) { negativo = !negativo; s = s.Substring(1); }
            else if (s.StartsWith("+")) s = s.Substring(1);

            if (s.Contains('-') || s.Contains('+')) return null;
            s = s.Trim('.', ',');
            if (s.Length == 0 || !s.Any(char.IsDigit)) return null;

            int ultimaComa = s.LastIndexOf(',');
            int ultimoPunto = s.LastIndexOf('.');
            string normal;

            if (ultimaComa >= 0 && ultimoPunto >= 0)
            {
                // El que aparece al final es el decimal
                if (ultimaComa > ultimoPunto)
                    normal = s.Replace(".", "").Replace(',', '.');
                else
                    normal = s.Replace(",", "");
            }
            else if (ultimaComa >= 0)
            {
                normal = SoloUnSeparador(s, ',');
            }
            else if (ultimoPunto >= 0)
            {
                // Varios puntos => miles ("1.234.567"); un punto => decimal
                normal = s.Count(c => c == '.') > 1 ? s.Replace(".", "") : s;
            }
            else
            {
                normal = s;
            }

            if (!decimal.TryParse(normal, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var r))
                return null;

            return negativo ? -r : r;
        }

        private static string SoloUnSeparador(string s, char sep)
        {
            var partes = s.Split(sep);
            if (partes.Length > 2) return s.Replace(sep.ToString(), ""); // 1,234,567

            var entero = partes[0];
            var frac = partes[1];

            // "1,234" => miles; "12,5" / "12,50" => decimal. "0,500" => decimal (empieza en 0)
            if (frac.Length == 3 && entero != "0" && entero.Length is >= 1 and <= 3)
                return entero + frac;

            return entero + "." + frac;
        }
    }
}
