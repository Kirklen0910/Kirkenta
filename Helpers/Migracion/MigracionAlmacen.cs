using System.Text.Json;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>
    /// Guarda temporalmente (en disco, 24 h) las tablas leídas del archivo del sistema viejo,
    /// para que el usuario pueda ir enviando módulo por módulo sin volver a subir el archivo.
    /// </summary>
    public static class MigracionAlmacen
    {
        public const string SessionToken = "MigracionToken";
        public const int FilasPorLote = 5000; // = máximo del asistente de importación

        private static string Carpeta()
        {
            var c = Path.Combine(Path.GetTempPath(), "kirkenta-migracion");
            Directory.CreateDirectory(c);
            return c;
        }

        private static bool TokenValido(string? token) => Guid.TryParseExact(token, "N", out _);

        private static string Ruta(string token) => Path.Combine(Carpeta(), token + ".json");

        public static string Guardar(List<TablaOrigen> tablas)
        {
            LimpiarViejos();
            var token = Guid.NewGuid().ToString("N");
            using (var fs = File.Create(Ruta(token))) JsonSerializer.Serialize(fs, tablas);
            return token;
        }

        public static List<TablaOrigen>? Cargar(string? token)
        {
            if (!TokenValido(token)) return null;
            var ruta = Ruta(token!);
            if (!File.Exists(ruta)) return null;
            try { using var fs = File.OpenRead(ruta); return JsonSerializer.Deserialize<List<TablaOrigen>>(fs); }
            catch { return null; }
        }

        public static void Eliminar(string? token)
        {
            if (!TokenValido(token)) return;
            try { File.Delete(Ruta(token!)); } catch { /* ignorar */ }
        }

        private static void LimpiarViejos()
        {
            try
            {
                foreach (var f in Directory.GetFiles(Carpeta(), "*.json"))
                    if (File.GetLastWriteTimeUtc(f) < DateTime.UtcNow.AddHours(-24)) File.Delete(f);
            }
            catch { /* ignorar */ }
        }

        public static int TotalLotes(int filas) => Math.Max(1, (int)Math.Ceiling(filas / (double)FilasPorLote));
    }
}
