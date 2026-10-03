namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Auto-detecta el mapeo entre las columnas del archivo y los campos del sistema.
    /// Compara por nombre, sin importar mayúsculas, acentos ni espacios.
    /// </summary>
    public static class ColumnMapper
    {
        /// <summary>
        /// Dado un conjunto de headers del archivo y un diccionario de alias por campo,
        /// devuelve un diccionario: campo → header del archivo (o null si no encontró).
        /// </summary>
        public static Dictionary<string, string?> Detectar(
            List<string> headersArchivo,
            Dictionary<string, string[]> aliasPorCampo)
        {
            var mapa = new Dictionary<string, string?>();

            foreach (var campo in aliasPorCampo.Keys)
            {
                var aliases = aliasPorCampo[campo];
                var match = headersArchivo.FirstOrDefault(h =>
                    aliases.Any(a => Normalizar(h) == Normalizar(a)));

                // Si no hay match exacto, probar contains
                if (match == null)
                {
                    match = headersArchivo.FirstOrDefault(h =>
                        aliases.Any(a =>
                        {
                            var hn = Normalizar(h);
                            var an = Normalizar(a);
                            return hn.Contains(an) || an.Contains(hn);
                        }));
                }

                mapa[campo] = match;
            }

            return mapa;
        }

        /// <summary>
        /// Normaliza un texto: sin acentos, sin espacios, sin guiones, minúsculas.
        /// </summary>
        public static string Normalizar(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";

            var normalized = texto.Trim().ToLowerInvariant()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n")
                .Replace("ü", "u");

            normalized = new string(normalized.Where(c => char.IsLetterOrDigit(c)).ToArray());
            return normalized;
        }
    }
}