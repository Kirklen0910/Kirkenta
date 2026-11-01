namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Estructura común para archivos importados (Excel, CSV, TSV, TXT o JSON).
    /// </summary>
    public class ImportFile
    {
        public string? Error { get; set; }
        public List<string> Headers { get; set; } = new();
        public List<Dictionary<string, string>> Rows { get; set; } = new();

        public bool Ok => string.IsNullOrEmpty(Error);

        /// <summary>
        /// Asegura que no haya dos encabezados iguales (sin importar mayúsculas):
        /// "Precio", "Precio" → "Precio", "Precio (2)". Los vacíos se llaman "ColumnaN".
        /// </summary>
        public static List<string> HacerHeadersUnicos(IEnumerable<string?> originales)
        {
            var resultado = new List<string>();
            var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int posicion = 0;

            foreach (var original in originales)
            {
                posicion++;
                var nombre = (original ?? "").Replace("﻿", "").Trim();
                if (nombre.Length == 0) nombre = $"Columna{posicion}";

                var candidato = nombre;
                int contador = 2;
                while (!usados.Add(candidato))
                {
                    candidato = $"{nombre} ({contador})";
                    contador++;
                }

                resultado.Add(candidato);
            }

            return resultado;
        }
    }
}
