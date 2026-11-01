using System.Text;

namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Lee archivos de texto delimitados: CSV (coma o punto y coma), TSV (tabulador) o TXT.
    /// - Detecta el separador automáticamente (, ; tab |).
    /// - Detecta la codificación (UTF-8 / UTF-16 con BOM; si no es UTF-8 válido usa Windows-1252/Latin1,
    ///   que es lo que genera Excel en español con "CSV (delimitado por comas)").
    /// - Soporta celdas entre comillas con saltos de línea y comillas dobles escapadas.
    /// </summary>
    public static class CsvImporter
    {
        public static ImportFile Read(Stream stream)
        {
            var result = new ImportFile();

            string contenido;
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                contenido = Decodificar(ms.ToArray());
            }

            contenido = contenido.Replace("﻿", "");

            if (string.IsNullOrWhiteSpace(contenido))
            {
                result.Error = "El archivo está vacío";
                return result;
            }

            // Excel a veces agrega una primera línea "sep=;"
            if (contenido.StartsWith("sep=", StringComparison.OrdinalIgnoreCase))
            {
                int fin = contenido.IndexOf('\n');
                contenido = fin >= 0 ? contenido[(fin + 1)..] : "";
            }

            char separador = DetectarSeparador(contenido);
            var registros = ParseRegistros(contenido, separador)
                .Where(r => r.Any(c => !string.IsNullOrWhiteSpace(c)))
                .ToList();

            if (registros.Count < 2)
            {
                result.Error = "El archivo debe tener al menos un encabezado y una fila de datos";
                return result;
            }

            var headers = ImportFile.HacerHeadersUnicos(registros[0]);
            result.Headers = headers;

            for (int i = 1; i < registros.Count; i++)
            {
                var valores = registros[i];
                var rowDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (int c = 0; c < headers.Count; c++)
                {
                    rowDict[headers[c]] = c < valores.Count ? valores[c].Trim() : "";
                }

                result.Rows.Add(rowDict);
            }

            return result;
        }

        public static string Decodificar(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                // No es UTF-8 válido: casi seguro viene de Excel en Windows (ANSI)
                return Encoding.Latin1.GetString(bytes);
            }
        }

        /// <summary>
        /// Cuenta cada separador candidato en la primera línea (fuera de comillas)
        /// y elige el más frecuente.
        /// </summary>
        private static char DetectarSeparador(string contenido)
        {
            var candidatos = new[] { ',', ';', '\t', '|' };
            var conteos = candidatos.ToDictionary(c => c, _ => 0);
            bool enComillas = false;

            foreach (var ch in contenido)
            {
                if (ch == '"') { enComillas = !enComillas; continue; }
                if (!enComillas && (ch == '\n' || ch == '\r'))
                {
                    if (conteos.Values.Any(v => v > 0)) break; // ya tenemos la línea de encabezados
                    continue;
                }
                if (!enComillas && conteos.ContainsKey(ch)) conteos[ch]++;
            }

            var mejor = conteos.OrderByDescending(x => x.Value).First();
            return mejor.Value == 0 ? ',' : mejor.Key;
        }

        private static List<List<string>> ParseRegistros(string texto, char separador)
        {
            var registros = new List<List<string>>();
            var fila = new List<string>();
            var celda = new StringBuilder();
            bool enComillas = false;

            void TerminarFila()
            {
                fila.Add(celda.ToString());
                celda.Clear();
                registros.Add(fila);
                fila = new List<string>();
            }

            for (int i = 0; i < texto.Length; i++)
            {
                char c = texto[i];

                if (enComillas)
                {
                    if (c == '"')
                    {
                        if (i + 1 < texto.Length && texto[i + 1] == '"')
                        {
                            celda.Append('"');
                            i++;
                        }
                        else
                        {
                            enComillas = false;
                        }
                    }
                    else
                    {
                        celda.Append(c);
                    }
                    continue;
                }

                if (c == '"')
                {
                    enComillas = true;
                }
                else if (c == separador)
                {
                    fila.Add(celda.ToString());
                    celda.Clear();
                }
                else if (c == '\r')
                {
                    if (i + 1 < texto.Length && texto[i + 1] == '\n') i++;
                    TerminarFila();
                }
                else if (c == '\n')
                {
                    TerminarFila();
                }
                else
                {
                    celda.Append(c);
                }
            }

            if (celda.Length > 0 || fila.Count > 0)
                TerminarFila();

            return registros;
        }
    }
}
