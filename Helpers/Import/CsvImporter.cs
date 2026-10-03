using System.Text;

namespace Kirkenta.Helpers.Import
{
    public static class CsvImporter
    {
        public static ImportFile Read(Stream stream)
        {
            var result = new ImportFile();

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var contenido = reader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(contenido))
            {
                result.Error = "El archivo está vacío";
                return result;
            }

            var lineas = contenido.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (lineas.Count < 2)
            {
                result.Error = "El archivo debe tener al menos un encabezado y una fila de datos";
                return result;
            }

            // Detectar separador
            char separador = DetectarSeparador(lineas[0]);

            // Parsear headers
            var headers = ParseLine(lineas[0], separador);
            result.Headers = headers;

            // Parsear filas
            for (int i = 1; i < lineas.Count; i++)
            {
                var valores = ParseLine(lineas[i], separador);
                var rowDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (int c = 0; c < headers.Count; c++)
                {
                    rowDict[headers[c]] = c < valores.Count ? valores[c].Trim() : "";
                }

                result.Rows.Add(rowDict);
            }

            return result;
        }

        private static char DetectarSeparador(string lineaHeader)
        {
            var candidatos = new[] { ',', ';', '\t', '|' };
            var conteos = candidatos.Select(c => new { Sep = c, Count = lineaHeader.Count(x => x == c) }).ToList();
            return conteos.OrderByDescending(x => x.Count).First().Sep;
        }

        private static List<string> ParseLine(string linea, char separador)
        {
            var resultado = new List<string>();
            var actual = new StringBuilder();
            bool enComillas = false;

            for (int i = 0; i < linea.Length; i++)
            {
                char c = linea[i];

                if (c == '"')
                {
                    if (enComillas && i + 1 < linea.Length && linea[i + 1] == '"')
                    {
                        actual.Append('"');
                        i++;
                    }
                    else
                    {
                        enComillas = !enComillas;
                    }
                }
                else if (c == separador && !enComillas)
                {
                    resultado.Add(actual.ToString());
                    actual.Clear();
                }
                else
                {
                    actual.Append(c);
                }
            }

            resultado.Add(actual.ToString());
            return resultado;
        }
    }
}