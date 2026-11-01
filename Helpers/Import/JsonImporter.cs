using System.Text.Json;

namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Lee un archivo JSON: una lista de objetos [{...},{...}] o un objeto que contenga
    /// una lista, por ejemplo { "data": [ ... ] }. Compatible con los archivos que
    /// genera la opción Exportar → JSON del sistema.
    /// </summary>
    public static class JsonImporter
    {
        public static ImportFile Read(Stream stream)
        {
            var result = new ImportFile();

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(stream, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
            }
            catch (JsonException)
            {
                result.Error = "El archivo JSON no es válido";
                return result;
            }

            using (doc)
            {
                var lista = EncontrarLista(doc.RootElement);
                if (lista == null)
                {
                    result.Error = "El JSON debe ser una lista de objetos, por ejemplo [ { \"Nombre\": \"...\" } ]";
                    return result;
                }

                var nombres = new List<string>();
                var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var filasCrudas = new List<Dictionary<string, string>>();

                foreach (var item in lista.Value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;

                    var fila = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in item.EnumerateObject())
                    {
                        if (vistos.Add(prop.Name)) nombres.Add(prop.Name);
                        fila[prop.Name] = ValorComoTexto(prop.Value);
                    }
                    filasCrudas.Add(fila);
                }

                if (nombres.Count == 0)
                {
                    result.Error = "El JSON no contiene objetos con datos";
                    return result;
                }

                result.Headers = nombres;
                foreach (var cruda in filasCrudas)
                {
                    var fila = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var nombre in nombres)
                        fila[nombre] = cruda.TryGetValue(nombre, out var v) ? v : "";

                    if (fila.Values.Any(v => v.Length > 0))
                        result.Rows.Add(fila);
                }
            }

            return result;
        }

        private static JsonElement? EncontrarLista(JsonElement raiz)
        {
            if (raiz.ValueKind == JsonValueKind.Array) return raiz;

            if (raiz.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in raiz.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array) return prop.Value;
                }
            }

            return null;
        }

        private static string ValorComoTexto(JsonElement valor)
        {
            return valor.ValueKind switch
            {
                JsonValueKind.String => (valor.GetString() ?? "").Trim(),
                JsonValueKind.Number => valor.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "",
                JsonValueKind.Undefined => "",
                _ => valor.GetRawText()
            };
        }
    }
}
