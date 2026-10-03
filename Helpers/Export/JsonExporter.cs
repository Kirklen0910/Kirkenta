using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Kirkenta.Helpers.Export
{
    public static class JsonExporter
    {
        public static byte[] Export<T>(IEnumerable<T> items, bool indentado = true)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = indentado,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(items, options);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}