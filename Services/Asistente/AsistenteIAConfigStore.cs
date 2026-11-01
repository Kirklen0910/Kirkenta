using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Kirkenta.Services.Asistente
{
    public class AsistenteIAConfig
    {
        public bool Habilitada { get; set; }
        public string Url { get; set; } = "";
        public string Modelo { get; set; } = "";
        public string ApiKey { get; set; } = "";
    }

    /// <summary>
    /// Guarda la configuración de la IA opcional del asistente en App_Data/asistente-ia.json.
    /// La clave de API se guarda protegida (Data Protection), nunca en texto plano.
    /// Si el archivo no existe se usan, como respaldo, las claves Asistente:IA:* de la configuración.
    /// </summary>
    public class AsistenteIAConfigStore
    {
        private readonly string _ruta;
        private readonly IDataProtector _prot;
        private readonly IConfiguration _cfg;
        private readonly object _lock = new();
        private AsistenteIAConfig? _cache;

        private class Archivo
        {
            public bool Habilitada { get; set; }
            public string? Url { get; set; }
            public string? Modelo { get; set; }
            public string? ApiKeyProtegida { get; set; }
        }

        public AsistenteIAConfigStore(IWebHostEnvironment env, IDataProtectionProvider dp, IConfiguration cfg)
        {
            _ruta = Path.Combine(env.ContentRootPath, "App_Data", "asistente-ia.json");
            _prot = dp.CreateProtector("Kirkenta.Asistente.IA.v1");
            _cfg = cfg;
        }

        /// <summary>Resultado del último intento de interpretar un mensaje con IA (para diagnóstico en pantalla).</summary>
        public volatile string? UltimoDiagnostico;
        public DateTime? UltimoDiagnosticoFecha;
        public void Diagnostico(string texto) { UltimoDiagnostico = texto; UltimoDiagnosticoFecha = DateTime.Now; }

        public AsistenteIAConfig Obtener()
        {
            lock (_lock)
            {
                if (_cache != null) return Copia(_cache);
                _cache = Leer();
                return Copia(_cache);
            }
        }

        private static AsistenteIAConfig Copia(AsistenteIAConfig c) => new()
        { Habilitada = c.Habilitada, Url = c.Url, Modelo = c.Modelo, ApiKey = c.ApiKey };

        private AsistenteIAConfig Leer()
        {
            try
            {
                if (File.Exists(_ruta))
                {
                    var a = JsonSerializer.Deserialize<Archivo>(File.ReadAllText(_ruta));
                    if (a != null)
                    {
                        var key = "";
                        if (!string.IsNullOrEmpty(a.ApiKeyProtegida))
                        {
                            try { key = _prot.Unprotect(a.ApiKeyProtegida); } catch { key = ""; }
                        }
                        return new AsistenteIAConfig { Habilitada = a.Habilitada, Url = a.Url ?? "", Modelo = a.Modelo ?? "", ApiKey = key };
                    }
                }
            }
            catch { /* archivo dañado: se usa la configuración base */ }

            var url = _cfg["Asistente:IA:Url"] ?? "";
            var modelo = _cfg["Asistente:IA:Modelo"] ?? "";
            return new AsistenteIAConfig
            {
                Habilitada = !string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(modelo),
                Url = url, Modelo = modelo, ApiKey = _cfg["Asistente:IA:ApiKey"] ?? ""
            };
        }

        public void Guardar(AsistenteIAConfig c)
        {
            lock (_lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_ruta)!);
                var a = new Archivo
                {
                    Habilitada = c.Habilitada,
                    Url = c.Url?.Trim(),
                    Modelo = c.Modelo?.Trim(),
                    ApiKeyProtegida = string.IsNullOrEmpty(c.ApiKey) ? null : _prot.Protect(c.ApiKey)
                };
                File.WriteAllText(_ruta, JsonSerializer.Serialize(a, new JsonSerializerOptions { WriteIndented = true }));
                _cache = new AsistenteIAConfig { Habilitada = c.Habilitada, Url = a.Url ?? "", Modelo = a.Modelo ?? "", ApiKey = c.ApiKey ?? "" };
            }
        }
    }
}
