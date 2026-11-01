using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Kirkenta.Helpers.Export
{
    public static class JsonExporter
    {
        private static JsonSerializerOptions Opciones(bool indentado) => new()
        {
            WriteIndented = indentado,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Exporta las entidades tal cual (todas sus propiedades). Solo para usos internos:
        /// para el usuario final usar la sobrecarga con columnas.
        /// </summary>
        public static byte[] Export<T>(IEnumerable<T> items, bool indentado = true)
        {
            var json = JsonSerializer.Serialize(items, Opciones(indentado));
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Exporta con las MISMAS columnas que el Excel/CSV (mismos encabezados como claves),
        /// así el archivo se puede volver a importar tal cual y no expone campos internos.
        /// Sin filas devuelve un solo objeto con las claves vacías (sirve de plantilla).
        /// </summary>
        public static byte[] Export<T>(IEnumerable<T> items, List<ExportColumn<T>> columns, bool indentado = true)
        {
            var filas = new List<Dictionary<string, object?>>();

            foreach (var item in items)
            {
                var fila = new Dictionary<string, object?>();
                foreach (var col in columns)
                    fila[col.Header] = col.Value(item);
                filas.Add(fila);
            }

            if (filas.Count == 0)
            {
                var plantilla = new Dictionary<string, object?>();
                foreach (var col in columns) plantilla[col.Header] = "";
                filas.Add(plantilla);
            }

            var json = JsonSerializer.Serialize(filas, Opciones(indentado));
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
