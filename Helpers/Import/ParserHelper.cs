using System.Globalization;

namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Utilidades de parseo robustas para valores de importación.
    /// Acepta múltiples formatos (con/sin comas de miles, con/sin símbolos).
    /// </summary>
    public static class ParserHelper
    {
        /// <summary>
        /// Parsea un bool. Acepta: 1/0, true/false, sí/si/no, yes/no, activo/inactivo.
        /// Devuelve null si el valor está vacío o no es reconocible.
        /// </summary>
        public static bool? ParsearBool(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var normalizado = valor.Trim().ToLowerInvariant();

            if (normalizado == "1" || normalizado == "true" || normalizado == "sí" ||
                normalizado == "si" || normalizado == "yes" || normalizado == "y" ||
                normalizado == "activo" || normalizado == "activa" || normalizado == "on")
                return true;

            if (normalizado == "0" || normalizado == "false" || normalizado == "no" ||
                normalizado == "n" || normalizado == "inactivo" || normalizado == "inactiva" ||
                normalizado == "off")
                return false;

            return null;
        }

        /// <summary>
        /// Parsea un int. Devuelve null si el valor está vacío o no es numérico.
        /// </summary>
        public static int? ParsearInt(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            return int.TryParse(valor.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;
        }

        /// <summary>
        /// Parsea un decimal. Limpia símbolos comunes: L., $, %, comas de miles.
        /// Intenta primero con InvariantCulture, luego con cultura local.
        /// </summary>
        public static decimal? ParsearDecimal(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var limpio = valor.Trim()
                .Replace("L.", "")
                .Replace("l.", "")
                .Replace("$", "")
                .Replace("%", "")
                .Trim();

            // Si tiene coma Y punto, asumimos coma = miles, punto = decimal
            if (limpio.Contains(',') && limpio.Contains('.'))
            {
                limpio = limpio.Replace(",", "");
            }
            // Si solo tiene coma, podría ser decimal (formato europeo) o miles
            // Convención: si hay 2 dígitos después de la coma, es decimal. Si hay 3, es miles.
            else if (limpio.Contains(','))
            {
                var partes = limpio.Split(',');
                if (partes.Length == 2 && partes[1].Length == 2)
                {
                    limpio = limpio.Replace(",", ".");
                }
                else
                {
                    limpio = limpio.Replace(",", "");
                }
            }

            if (decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;

            // Fallback: cultura local
            if (decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                return result;

            return null;
        }
    }
}