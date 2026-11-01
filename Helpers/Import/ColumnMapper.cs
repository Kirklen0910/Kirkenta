using System.Globalization;
using System.Text;

namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Auto-detecta el mapeo entre las columnas del archivo y los campos del sistema.
    /// - Ignora mayúsculas, acentos, espacios y símbolos.
    /// - Prioriza coincidencias exactas sobre parciales.
    /// - Cada columna del archivo se asigna como máximo a UN campo (antes una misma
    ///   columna podía quedar asignada a varios campos a la vez).
    /// </summary>
    public static class ColumnMapper
    {
        private const int PuntajeExacto = 100;
        private const int PuntajePropio = 110; // coincide con el nombre/etiqueta del propio campo
        public const string MarcaPropio = "=";
        private const int PuntajeParcialBase = 60;
        private const int LargoMinimoParcial = 4;

        /// <summary>
        /// Devuelve un diccionario: campo → header del archivo (o null si no encontró).
        /// </summary>
        public static Dictionary<string, string?> Detectar(
            List<string> headersArchivo,
            Dictionary<string, string[]> aliasPorCampo)
        {
            var mapa = new Dictionary<string, string?>();
            foreach (var campo in aliasPorCampo.Keys)
                mapa[campo] = null;

            var headersNorm = headersArchivo.Select(Normalizar).ToList();
            var candidatos = new List<(string Campo, int OrdenCampo, int Idx, int Puntaje)>();

            int ordenCampo = 0;
            foreach (var par in aliasPorCampo)
            {
                var campo = par.Key;
                // El nombre del campo (y los alias marcados con "=", que son su etiqueta visible)
                // cuentan como "propios": ganan sobre un alias de otro campo con el mismo texto.
                var propios = par.Value
                    .Where(a => a.StartsWith(MarcaPropio))
                    .Select(a => Normalizar(a.Substring(MarcaPropio.Length)))
                    .Append(Normalizar(campo))
                    .Where(a => a.Length > 0)
                    .ToHashSet();

                var alias = par.Value
                    .Select(a => a.StartsWith(MarcaPropio) ? a.Substring(MarcaPropio.Length) : a)
                    .Append(campo)
                    .Select(Normalizar)
                    .Where(a => a.Length > 0)
                    .Distinct()
                    .ToList();

                for (int i = 0; i < headersNorm.Count; i++)
                {
                    var hn = headersNorm[i];
                    if (hn.Length == 0) continue;

                    int mejor = 0;
                    foreach (var an in alias)
                    {
                        int puntaje = PuntajeCoincidencia(hn, an);
                        if (puntaje == PuntajeExacto && propios.Contains(an)) puntaje = PuntajePropio;
                        if (puntaje > mejor) mejor = puntaje;
                        if (mejor == PuntajePropio) break;
                    }

                    if (mejor > 0)
                        candidatos.Add((campo, ordenCampo, i, mejor));
                }

                ordenCampo++;
            }

            var camposAsignados = new HashSet<string>();
            var columnasUsadas = new HashSet<int>();

            foreach (var c in candidatos
                .OrderByDescending(x => x.Puntaje)
                .ThenBy(x => x.OrdenCampo)
                .ThenBy(x => x.Idx))
            {
                if (camposAsignados.Contains(c.Campo) || columnasUsadas.Contains(c.Idx)) continue;

                mapa[c.Campo] = headersArchivo[c.Idx];
                camposAsignados.Add(c.Campo);
                columnasUsadas.Add(c.Idx);
            }

            return mapa;
        }

        /// <summary>
        /// True si el header coincide exactamente (normalizado) con el nombre del campo,
        /// su etiqueta o alguno de sus alias.
        /// </summary>
        public static bool EsCoincidenciaExacta(string? header, string campoKey, string? etiqueta, string[]? alias)
        {
            var hn = Normalizar(header);
            if (hn.Length == 0) return false;

            if (hn == Normalizar(campoKey)) return true;
            if (!string.IsNullOrEmpty(etiqueta) && hn == Normalizar(etiqueta)) return true;
            return alias != null && alias.Any(a => Normalizar(a) == hn); // Normalizar ya descarta la marca "="
        }

        private static int PuntajeCoincidencia(string header, string alias)
        {
            if (header == alias) return PuntajeExacto;

            // Coincidencia parcial: solo si la parte contenida tiene un largo razonable,
            // para que "id" o "no" no se cuelen dentro de cualquier palabra.
            string corto, largo;
            if (header.Length <= alias.Length) { corto = header; largo = alias; }
            else { corto = alias; largo = header; }

            if (corto.Length < LargoMinimoParcial) return 0;
            // «categoria_id» o «idcliente» guardan un número de otra tabla: no son «categoría» ni «cliente»
            if (EsId(header) && !alias.Contains("id")) return 0;
            if (!largo.Contains(corto, StringComparison.Ordinal)) return 0;

            // Más parecidos en largo = mejor puntaje (60..70)
            return PuntajeParcialBase + (int)(10.0 * corto.Length / largo.Length);
        }

        /// <summary>
        /// Normaliza un texto: sin acentos, sin espacios, sin símbolos, minúsculas.
        /// </summary>
        private static bool EsId(string h) => h.Length > 3 && (h.EndsWith("id") || h.StartsWith("id"));

        public static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";

            var descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(descompuesto.Length);

            foreach (var c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
