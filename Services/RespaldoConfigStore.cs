using System.Text.Json;

namespace Kirkenta.Services
{
    /// <summary>Opciones del respaldo automático (se editan desde Configuración → Respaldos del sistema).</summary>
    public class RespaldoConfig
    {
        public bool Automatico { get; set; } = true;
        /// <summary>Hora del respaldo diario, "HH:mm".</summary>
        public string Hora { get; set; } = "02:00";
        /// <summary>Cuántos respaldos conservar.</summary>
        public int Conservar { get; set; } = 14;
        /// <summary>Carpeta de otro disco, USB o unidad de red donde se copia cada respaldo (opcional).</summary>
        public string? CopiaExterna { get; set; }
    }

    /// <summary>Resultado de la última copia a la carpeta externa (para mostrarlo en pantalla).</summary>
    public class RespaldoCopiaEstado
    {
        public DateTime Fecha { get; set; }
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
    }

    /// <summary>
    /// Guarda las opciones del respaldo automático en App_Data/respaldo-config.json.
    /// Si el archivo no existe se usan, como respaldo, las claves Respaldo:* de la configuración (appsettings).
    /// </summary>
    public class RespaldoConfigStore
    {
        private readonly string _ruta;
        private readonly string _rutaEstado;
        private readonly IConfiguration _cfg;
        private readonly object _lock = new();

        public RespaldoConfigStore(IWebHostEnvironment env, IConfiguration cfg)
        {
            var dir = Path.Combine(env.ContentRootPath, "App_Data");
            _ruta = Path.Combine(dir, "respaldo-config.json");
            _rutaEstado = Path.Combine(dir, "respaldo-copia-estado.json");
            _cfg = cfg;
        }

        public RespaldoConfig Obtener()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_ruta))
                    {
                        var c = JsonSerializer.Deserialize<RespaldoConfig>(File.ReadAllText(_ruta));
                        if (c != null) return Normalizar(c);
                    }
                }
                catch { /* archivo dañado: se usa la configuración base */ }

                return Normalizar(new RespaldoConfig
                {
                    Automatico = !string.Equals(_cfg["Respaldo:Automatico"], "false", StringComparison.OrdinalIgnoreCase),
                    Hora = _cfg["Respaldo:Hora"] ?? "02:00",
                    Conservar = int.TryParse(_cfg["Respaldo:Conservar"], out var n) ? n : 14,
                    CopiaExterna = _cfg["Respaldo:CopiaExterna"]
                });
            }
        }

        private static RespaldoConfig Normalizar(RespaldoConfig c)
        {
            if (!TimeSpan.TryParse(c.Hora, out _)) c.Hora = "02:00";
            if (c.Conservar < 1) c.Conservar = 14;
            if (c.Conservar > 365) c.Conservar = 365;
            c.CopiaExterna = string.IsNullOrWhiteSpace(c.CopiaExterna) ? null : c.CopiaExterna.Trim();
            return c;
        }

        /// <summary>Texto de error si la configuración no sirve; null si está bien.</summary>
        public static string? Validar(RespaldoConfig c)
        {
            if (!TimeSpan.TryParse(c.Hora, out var h) || h < TimeSpan.Zero || h >= TimeSpan.FromHours(24))
                return "La hora del respaldo no es válida (usa el formato 02:00).";
            if (c.Conservar < 1 || c.Conservar > 365)
                return "Conservar debe estar entre 1 y 365 respaldos.";
            if (!string.IsNullOrWhiteSpace(c.CopiaExterna) && !Path.IsPathRooted(c.CopiaExterna.Trim()))
                return "La carpeta de copia externa debe ser una ruta completa, por ejemplo E:\\Respaldos o \\\\servidor\\respaldos.";
            return null;
        }

        public void Guardar(RespaldoConfig c)
        {
            lock (_lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_ruta)!);
                File.WriteAllText(_ruta, JsonSerializer.Serialize(Normalizar(c), new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        public RespaldoCopiaEstado? UltimaCopia()
        {
            lock (_lock)
            {
                try { return File.Exists(_rutaEstado) ? JsonSerializer.Deserialize<RespaldoCopiaEstado>(File.ReadAllText(_rutaEstado)) : null; }
                catch { return null; }
            }
        }

        public void RegistrarCopia(bool ok, string mensaje)
        {
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_rutaEstado)!);
                    File.WriteAllText(_rutaEstado, JsonSerializer.Serialize(new RespaldoCopiaEstado { Fecha = DateTime.Now, Ok = ok, Mensaje = mensaje }));
                }
                catch { /* el estado es informativo; nunca debe romper el respaldo */ }
            }
        }

        /// <summary>
        /// Copia un respaldo a la carpeta externa y deja allí solo los últimos <paramref name="conservar"/> respaldos.
        /// Devuelve (ok, mensaje). Nunca lanza: un USB desconectado no debe tumbar el respaldo principal.
        /// </summary>
        public static (bool Ok, string Mensaje) CopiarAExterna(string rutaZip, string carpeta, int conservar)
        {
            try
            {
                Directory.CreateDirectory(carpeta);
                var destino = Path.Combine(carpeta, Path.GetFileName(rutaZip));
                File.Copy(rutaZip, destino, true);
                var viejos = new DirectoryInfo(carpeta).GetFiles("kirkenta-datos_*.zip")
                    .Where(f => !f.Name.Contains("previo"))
                    .OrderByDescending(f => f.LastWriteTime).Skip(Math.Max(1, conservar));
                foreach (var f in viejos) { try { f.Delete(); } catch { } }
                return (true, $"Copiado a {carpeta}");
            }
            catch (Exception ex)
            {
                return (false, $"No se pudo copiar a {carpeta}: {ex.Message}");
            }
        }

        /// <summary>Prueba que la carpeta externa existe (o se puede crear) y que se puede escribir en ella.</summary>
        public static (bool Ok, string Mensaje) ProbarCarpeta(string carpeta)
        {
            try
            {
                Directory.CreateDirectory(carpeta);
                var prueba = Path.Combine(carpeta, ".prueba-kirkenta-" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(prueba, "ok");
                File.Delete(prueba);
                return (true, $"La carpeta {carpeta} está lista: se puede escribir en ella.");
            }
            catch (Exception ex)
            {
                return (false, $"No se puede escribir en {carpeta}: {ex.Message}");
            }
        }
    }
}
