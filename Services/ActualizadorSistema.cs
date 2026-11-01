using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kirkenta.Helpers;

namespace Kirkenta.Services
{
    public record InfoActualizacion(string Version, string Archivo, string Sha256, long Bytes, string Notas, string Fecha, string UrlArchivo);

    public record ResultadoActualizacion(bool Ok, string Mensaje, InfoActualizacion? Disponible = null);

    /// <summary>
    /// Busca versiones nuevas en GitHub Releases, descarga el instalador, verifica su firma digital y su SHA-256
    /// y lo ejecuta en silencio. Nunca instala algo que no este firmado con la clave privada de quien publica.
    ///
    /// Cada release de GitHub lleva 2 archivos: Kirkenta-Setup-X.Y.Z.exe y actualizacion.json
    /// (version, archivo, sha256, bytes, fecha, notas, firma). La firma cubre "version|archivo|sha256".
    /// </summary>
    public class ActualizadorSistema
    {
        public const string NombreManifiesto = "actualizacion.json";
        private static readonly Regex ReVersion = new(@"^\d{1,4}\.\d{1,4}\.\d{1,4}$", RegexOptions.Compiled);
        private static readonly Regex ReArchivo = new(@"^Kirkenta-Setup-\d{1,4}\.\d{1,4}\.\d{1,4}\.exe$", RegexOptions.Compiled);
        private static readonly Regex ReSha = new(@"^[0-9a-fA-F]{64}$", RegexOptions.Compiled);
        private const long MaximoInstalador = 3L * 1024 * 1024 * 1024;

        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ActualizadorSistema> _log;

        public ActualizadorSistema(IHttpClientFactory http, IConfiguration config, IWebHostEnvironment env, ILogger<ActualizadorSistema> log)
        {
            _http = http; _config = config; _env = env; _log = log;
        }

        // ----- Estado compartido (para el aviso del menu) -----
        private static volatile InfoActualizacion? _ultimaDisponible;
        public static InfoActualizacion? UltimaDisponible => _ultimaDisponible;
        public static DateTime? UltimaRevision { get; private set; }

        public static Version VersionActual
        {
            get
            {
                var asm = Assembly.GetEntryAssembly() ?? typeof(ActualizadorSistema).Assembly;
                var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrWhiteSpace(info))
                {
                    var limpio = info.Split('+', '-')[0];
                    if (Version.TryParse(limpio, out var v)) return Normalizar(v);
                }
                return Normalizar(asm.GetName().Version ?? new Version(1, 0, 0));
            }
        }

        private static Version Normalizar(Version v) => new(Math.Max(v.Major, 0), Math.Max(v.Minor, 0), Math.Max(v.Build, 0));

        public string Repositorio
        {
            get
            {
                var r = _config["Actualizaciones:Repositorio"];
                if (string.IsNullOrWhiteSpace(r)) r = ConfigActualizaciones.Repositorio;
                return (r ?? "").Trim();
            }
        }

        public bool Configurado => EsRepositorioValido(Repositorio) && !string.IsNullOrWhiteSpace(ConfigActualizaciones.ClavePublicaXml);

        public static bool EsRepositorioValido(string? repo)
            => !string.IsNullOrWhiteSpace(repo) && Regex.IsMatch(repo, @"^[A-Za-z0-9][A-Za-z0-9._-]{0,99}/[A-Za-z0-9._-]{1,100}$");

        /// <summary>Carpeta donde se guardan los instaladores descargados. Solo existe en una instalacion del cliente.</summary>
        public string? CarpetaDescargas
        {
            get
            {
                var raiz = Directory.GetParent(_env.ContentRootPath.TrimEnd('\\', '/'))?.FullName;
                if (raiz == null || !Directory.Exists(Path.Combine(raiz, "mantenimiento"))) return null;
                return Path.Combine(raiz, "actualizaciones");
            }
        }

        // ================= Reglas puras (con pruebas) =================

        public static string TextoFirmado(string version, string archivo, string sha256)
            => $"KIRKENTA-UPDATE|{version}|{archivo}|{sha256.ToLowerInvariant()}";

        public static bool VerificarFirma(string clavePublicaXml, string texto, string firmaBase64)
        {
            if (string.IsNullOrWhiteSpace(clavePublicaXml) || string.IsNullOrWhiteSpace(firmaBase64)) return false;
            try
            {
                using var rsa = RSA.Create();
                rsa.FromXmlString(clavePublicaXml);
                return rsa.VerifyData(Encoding.UTF8.GetBytes(texto), Convert.FromBase64String(firmaBase64), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch { return false; }
        }

        public static bool EsMasNueva(string version, Version actual)
            => ReVersion.IsMatch(version) && Version.TryParse(version, out var v) && v > actual;

        /// <summary>Solo se descarga desde GitHub por HTTPS.</summary>
        public static bool UrlPermitida(string? url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var u) || u.Scheme != Uri.UriSchemeHttps) return false;
            var h = u.Host.ToLowerInvariant();
            return h == "github.com" || h == "api.github.com" || h.EndsWith(".githubusercontent.com") || h == "githubusercontent.com";
        }

        /// <summary>Valida un manifiesto ya leido. Devuelve null y un mensaje si algo no cuadra.</summary>
        public static InfoActualizacion? ValidarManifiesto(JsonElement m, string clavePublicaXml, Func<string, string?> urlDeAsset, out string error)
        {
            error = "";
            string Texto(string campo) => m.TryGetProperty(campo, out var p) && p.ValueKind == JsonValueKind.String ? (p.GetString() ?? "") : "";
            var version = Texto("version"); var archivo = Texto("archivo"); var sha = Texto("sha256"); var firma = Texto("firma");
            if (!ReVersion.IsMatch(version)) { error = "El manifiesto de la actualización no trae una versión válida."; return null; }
            if (!ReArchivo.IsMatch(archivo) || archivo != $"Kirkenta-Setup-{version}.exe") { error = "El nombre del instalador no corresponde a la versión."; return null; }
            if (!ReSha.IsMatch(sha)) { error = "El manifiesto no trae un SHA-256 válido."; return null; }
            if (!VerificarFirma(clavePublicaXml, TextoFirmado(version, archivo, sha), firma))
            { error = "La firma de la actualización NO es válida. Por seguridad no se instalará."; return null; }
            var url = urlDeAsset(archivo);
            if (!UrlPermitida(url)) { error = "El instalador no está en el release o su dirección no es de GitHub."; return null; }
            long bytes = m.TryGetProperty("bytes", out var b) && b.ValueKind == JsonValueKind.Number && b.TryGetInt64(out var bb) ? bb : 0;
            var notas = Texto("notas"); if (notas.Length > 4000) notas = notas[..4000];
            return new InfoActualizacion(version, archivo, sha.ToLowerInvariant(), bytes, notas, Texto("fecha"), url!);
        }

        // ================= Busqueda =================

        private HttpClient Cliente(TimeSpan timeout)
        {
            var c = _http.CreateClient();
            c.Timeout = timeout;
            c.DefaultRequestHeaders.UserAgent.ParseAdd("Kirkenta-Actualizador/1.0");
            c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return c;
        }

        public async Task<ResultadoActualizacion> BuscarAsync(CancellationToken ct = default)
        {
            if (!Configurado)
                return new(false, "Las actualizaciones por internet todavía no están configuradas en esta versión del sistema.");
            try
            {
                var cli = Cliente(TimeSpan.FromSeconds(30));
                using var resp = await cli.GetAsync($"https://api.github.com/repos/{Repositorio}/releases/latest", ct);
                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return Registrar(new(false, "No hay versiones publicadas todavía (o el repositorio no es público)."), null);
                if (!resp.IsSuccessStatusCode)
                    return new(false, $"GitHub respondió {(int)resp.StatusCode}. Intenta más tarde.");

                var json = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                var assets = new Dictionary<string, string>(StringComparer.Ordinal);
                if (doc.RootElement.TryGetProperty("assets", out var arr) && arr.ValueKind == JsonValueKind.Array)
                    foreach (var a in arr.EnumerateArray())
                    {
                        var nombre = a.TryGetProperty("name", out var n) ? n.GetString() : null;
                        var url = a.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                        if (nombre != null && url != null) assets[nombre] = url;
                    }

                if (!assets.TryGetValue(NombreManifiesto, out var urlManifiesto) || !UrlPermitida(urlManifiesto))
                    return Registrar(new(false, "El release más reciente no trae el archivo de firma (actualizacion.json)."), null);

                using var respM = await cli.GetAsync(urlManifiesto, ct);
                if (!respM.IsSuccessStatusCode) return new(false, "No se pudo leer la información de la actualización.");
                var bytesM = await respM.Content.ReadAsByteArrayAsync(ct);
                if (bytesM.Length > 64 * 1024) return new(false, "El manifiesto de la actualización es demasiado grande.");

                using var man = JsonDocument.Parse(bytesM);
                var info = ValidarManifiesto(man.RootElement, ConfigActualizaciones.ClavePublicaXml, nombre => assets.TryGetValue(nombre, out var u) ? u : null, out var error);
                if (info == null) return Registrar(new(false, error), null);

                if (!EsMasNueva(info.Version, VersionActual))
                    return Registrar(new(true, $"Ya tienes la última versión ({VersionActual})."), null);

                return Registrar(new(true, $"Hay una versión nueva: {info.Version}.", info), info);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "No se pudo buscar actualizaciones");
                return new(false, "No se pudo consultar GitHub. Revisa la conexión a internet.");
            }
        }

        private static ResultadoActualizacion Registrar(ResultadoActualizacion r, InfoActualizacion? info)
        {
            _ultimaDisponible = info;
            UltimaRevision = DateTime.Now;
            return r;
        }

        // ================= Descarga e instalacion =================

        /// <summary>
        /// Arranca todo el proceso en segundo plano (la pagina solo consulta el avance): descarga, verificacion,
        /// respaldo de seguridad e instalador. El avance se lee en EstadoActualizacion.
        /// </summary>
        public static bool IniciarEnSegundoPlano(IServiceScopeFactory fabrica, InfoActualizacion info, int usuarioId, string ip)
        {
            if (!EstadoActualizacion.Reiniciar(info.Version)) return false;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = fabrica.CreateScope();
                    var act = scope.ServiceProvider.GetRequiredService<ActualizadorSistema>();
                    var respaldo = scope.ServiceProvider.GetRequiredService<RespaldoSistemaService>();
                    var db = scope.ServiceProvider.GetRequiredService<Kirkenta.Data.ApplicationDbContext>();

                    var (okDesc, msgDesc, ruta) = await act.DescargarAsync(info, CancellationToken.None);
                    if (!okDesc || ruta == null) { EstadoActualizacion.Fallar(msgDesc); return; }

                    // El respaldo va al final (lo mas cerca posible de la instalacion) para que incluya los ultimos datos
                    EstadoActualizacion.Poner("respaldo", 75, "Creando respaldo de seguridad...");
                    var rr = await respaldo.CrearAsync("previo-actualizacion");
                    if (!rr.Ok) { EstadoActualizacion.Fallar("No se pudo crear el respaldo previo, por eso NO se actualizó: " + rr.Mensaje); return; }

                    EstadoActualizacion.Poner("lanzando", 88, "Iniciando el instalador...");
                    var (okLanz, msgLanz) = await act.LanzarInstaladorAsync(ruta, CancellationToken.None);
                    try { ActividadHelper.Registrar(db, usuarioId, "Actualización del sistema", $"{VersionActual} → {info.Version}: {(okLanz ? "iniciada" : msgLanz)}", ip); } catch { }
                    if (!okLanz) { EstadoActualizacion.Fallar(msgLanz); return; }
                    EstadoActualizacion.Poner("instalando", 90, msgLanz);
                }
                catch (Exception ex)
                {
                    EstadoActualizacion.Fallar("Falló la actualización: " + ex.Message);
                }
            });
            return true;
        }

        /// <summary>Descarga el instalador y comprueba su SHA-256. Devuelve la ruta del archivo listo.</summary>
        public async Task<(bool Ok, string Mensaje, string? Ruta)> DescargarAsync(InfoActualizacion info, CancellationToken ct)
        {
            if (!OperatingSystem.IsWindows()) return (false, "La instalación automática solo funciona en Windows.", null);
            var carpeta = CarpetaDescargas;
            if (carpeta == null) return (false, "Este sistema no está instalado con el instalador de Kirkenta; actualízalo manualmente.", null);
            var parcial = "";
            try
            {
                Directory.CreateDirectory(carpeta);
                var destino = Path.Combine(carpeta, info.Archivo);
                parcial = destino + ".descarga";
                try { File.Delete(parcial); } catch { }

                // Si ya estaba descargado y sigue intacto, no se baja otra vez
                EstadoActualizacion.Poner("descarga", 5, "Descargando la actualización...");
                if (File.Exists(destino) && await Sha256DeArchivoAsync(destino, ct) == info.Sha256)
                {
                    EstadoActualizacion.Poner("verificacion", 72, "Descarga lista y verificada.");
                    return (true, "Ya estaba descargado.", destino);
                }

                var cli = Cliente(TimeSpan.FromMinutes(45));
                using var resp = await cli.GetAsync(info.UrlArchivo, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!resp.IsSuccessStatusCode) return (false, $"No se pudo descargar el instalador (GitHub respondió {(int)resp.StatusCode}).", null);
                if (!UrlPermitida(resp.RequestMessage?.RequestUri?.ToString())) return (false, "La descarga fue redirigida a una dirección no permitida.", null);
                var esperado = resp.Content.Headers.ContentLength ?? info.Bytes;
                if (esperado > MaximoInstalador) return (false, "El instalador es más grande de lo permitido.", null);

                using var sha = SHA256.Create();
                long total = 0;
                var ultimoAviso = DateTime.MinValue;
                await using (var origen = await resp.Content.ReadAsStreamAsync(ct))
                await using (var salida = new FileStream(parcial, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    var buf = new byte[81920]; int n;
                    while ((n = await origen.ReadAsync(buf, ct)) > 0)
                    {
                        total += n;
                        if (total > MaximoInstalador) throw new InvalidDataException("El instalador es más grande de lo permitido.");
                        sha.TransformBlock(buf, 0, n, null, 0);
                        await salida.WriteAsync(buf.AsMemory(0, n), ct);
                        if ((DateTime.UtcNow - ultimoAviso).TotalMilliseconds > 400)
                        {
                            ultimoAviso = DateTime.UtcNow;
                            var pct = esperado > 0 ? 5 + (int)(65.0 * total / esperado) : 40;
                            EstadoActualizacion.Poner("descarga", Math.Min(pct, 70),
                                esperado > 0 ? $"Descargando... {total / 1048576} de {esperado / 1048576} MB" : $"Descargando... {total / 1048576} MB");
                        }
                    }
                    sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                }

                EstadoActualizacion.Poner("verificacion", 72, "Verificando que la descarga esté completa y sin alteraciones...");
                var hash = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
                if (hash != info.Sha256 || (info.Bytes > 0 && total != info.Bytes))
                {
                    try { File.Delete(parcial); } catch { }
                    return (false, "La descarga llegó dañada o alterada (no coincide con la firma). No se instaló nada.", null);
                }
                File.Move(parcial, destino, true);
                return (true, "Descarga verificada.", destino);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _log.LogError(ex, "Falló la descarga de la actualización");
                try { if (parcial != "") File.Delete(parcial); } catch { }
                return (false, "No se pudo descargar la actualización: " + ex.Message, null);
            }
        }

        /// <summary>
        /// Lanza el instalador fuera de Kirkenta.exe (con una tarea de Windows como SYSTEM) para que siga vivo cuando
        /// el instalador detenga el sistema. La tarea se define por XML para que arranque aunque la laptop este en bateria.
        /// Espera a que el instalador deje su marca de inicio; si no arranca, lo dice en vez de quedarse esperando.
        /// </summary>
        public async Task<(bool Ok, string Mensaje)> LanzarInstaladorAsync(string rutaInstalador, CancellationToken ct)
        {
            var carpeta = CarpetaDescargas;
            if (carpeta == null || !OperatingSystem.IsWindows()) return (false, "No se puede lanzar el instalador en este equipo.");
            try
            {
                var cmd = Path.Combine(carpeta, "actualizar.cmd");
                var log = Path.Combine(carpeta, "actualizar.log");
                var inicio = Path.Combine(carpeta, "inicio.txt");
                var fin = Path.Combine(carpeta, "fin.txt");
                var xmlTarea = Path.Combine(carpeta, "actualizar-tarea.xml");
                foreach (var f in new[] { inicio, fin, log }) { try { File.Delete(f); } catch { } }

                File.WriteAllText(cmd,
                    "@echo off\r\n" +
                    $"echo %date% %time% > \"{inicio}\"\r\n" +
                    "ping -n 3 127.0.0.1 >nul\r\n" +
                    $"\"{rutaInstalador}\" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /LOG=\"{log}\"\r\n" +
                    $"echo %errorlevel% > \"{fin}\"\r\n",
                    new UTF8Encoding(false));

                var xml =
                    "<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n" +
                    "<Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">\r\n" +
                    "  <Principals><Principal id=\"Author\"><UserId>S-1-5-18</UserId><RunLevel>HighestAvailable</RunLevel></Principal></Principals>\r\n" +
                    "  <Settings>\r\n" +
                    "    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>\r\n" +
                    "    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>\r\n" +
                    "    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>\r\n" +
                    "    <AllowHardTerminate>true</AllowHardTerminate>\r\n" +
                    "    <StartWhenAvailable>true</StartWhenAvailable>\r\n" +
                    "    <AllowStartOnDemand>true</AllowStartOnDemand>\r\n" +
                    "    <Enabled>true</Enabled>\r\n" +
                    "    <Hidden>true</Hidden>\r\n" +
                    "    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>\r\n" +
                    "    <Priority>4</Priority>\r\n" +
                    "  </Settings>\r\n" +
                    "  <Actions Context=\"Author\"><Exec><Command>" + System.Security.SecurityElement.Escape(cmd) + "</Command></Exec></Actions>\r\n" +
                    "</Task>\r\n";
                File.WriteAllText(xmlTarea, xml, Encoding.Unicode);

                const string tarea = "Kirkenta-Actualizar";
                var r1 = Ejecutar("schtasks.exe", "/Create", "/TN", tarea, "/XML", xmlTarea, "/F");
                if (r1.Codigo != 0) return (false, "No se pudo programar la instalación: " + r1.Salida);
                var r2 = Ejecutar("schtasks.exe", "/Run", "/TN", tarea);
                if (r2.Codigo != 0) return (false, "No se pudo iniciar la instalación: " + r2.Salida);

                // El instalador deja "inicio.txt" apenas arranca: si en 30 s no esta, no arranco
                for (int i = 0; i < 60; i++)
                {
                    if (File.Exists(inicio)) break;
                    await Task.Delay(500, ct);
                }
                if (!File.Exists(inicio))
                {
                    var q = Ejecutar("schtasks.exe", "/Query", "/TN", tarea, "/FO", "LIST", "/V");
                    _log.LogError("El instalador no arrancó. Estado de la tarea: {Estado}", q.Salida);
                    return (false, $"Windows no inició el instalador. Puedes terminar la actualización a mano ejecutando como administrador: {rutaInstalador}");
                }

                _log.LogWarning("Actualización lanzada: {Ruta}", rutaInstalador);
                return (true, "Instalando. El sistema se detendrá unos minutos y volverá solo.");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _log.LogError(ex, "Falló al lanzar la actualización");
                return (false, "No se pudo lanzar el instalador: " + ex.Message);
            }
        }

        private static (int Codigo, string Salida) Ejecutar(string exe, params string[] args)
        {
            var psi = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi)!;
            var salida = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit(30000);
            return (p.HasExited ? p.ExitCode : -1, salida.Trim());
        }

        private static async Task<string> Sha256DeArchivoAsync(string ruta, CancellationToken ct)
        {
            await using var fs = File.OpenRead(ruta);
            return Convert.ToHexString(await SHA256.HashDataAsync(fs, ct)).ToLowerInvariant();
        }

        /// <summary>Borra los instaladores de versiones que ya estan instaladas (o mas viejas).</summary>
        public void LimpiarDescargas()
        {
            try
            {
                var carpeta = CarpetaDescargas;
                if (carpeta == null || !Directory.Exists(carpeta)) return;
                foreach (var f in Directory.GetFiles(carpeta, "Kirkenta-Setup-*.exe*"))
                {
                    var m = Regex.Match(Path.GetFileName(f), @"Kirkenta-Setup-(\d+\.\d+\.\d+)\.exe");
                    if (m.Success && Version.TryParse(m.Groups[1].Value, out var v) && v <= VersionActual)
                        try { File.Delete(f); } catch { }
                }
            }
            catch { }
        }
    }

    /// <summary>Avance de la actualizacion en curso (lo lee la pantalla cada segundo).</summary>
    public record AvanceActualizacion(bool Activa, string Fase, int Porcentaje, string Mensaje, bool Error, string Version);

    public static class EstadoActualizacion
    {
        private static readonly object L = new();
        private static bool _activa, _error;
        private static string _fase = "", _mensaje = "", _version = "";
        private static int _pct;

        /// <summary>Empieza una actualizacion. false si ya hay otra en curso.</summary>
        public static bool Reiniciar(string version)
        {
            lock (L)
            {
                if (_activa) return false;
                _activa = true; _error = false; _fase = "descarga"; _pct = 2; _mensaje = "Preparando..."; _version = version;
                return true;
            }
        }

        public static void Poner(string fase, int porcentaje, string mensaje)
        {
            lock (L) { if (!_activa) return; _fase = fase; _pct = Math.Clamp(porcentaje, 0, 100); _mensaje = mensaje; }
        }

        public static void Fallar(string mensaje)
        {
            lock (L) { _activa = false; _error = true; _fase = "error"; _mensaje = mensaje; }
        }

        public static AvanceActualizacion Instantanea()
        {
            lock (L) return new AvanceActualizacion(_activa, _fase, _pct, _mensaje, _error, _version);
        }
    }

    /// <summary>Revisa si hay una version nueva a los 2 minutos de arrancar y luego cada 12 horas (Actualizaciones:Buscar=false lo apaga).</summary>
    public class ActualizacionesAutomaticasService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly IConfiguration _config;

        public ActualizacionesAutomaticasService(IServiceProvider sp, IConfiguration config) { _sp = sp; _config = config; }

        protected override async Task ExecuteAsync(CancellationToken stop)
        {
            if (string.Equals(_config["Actualizaciones:Buscar"], "false", StringComparison.OrdinalIgnoreCase)) return;
            try { await Task.Delay(TimeSpan.FromMinutes(2), stop); } catch (OperationCanceledException) { return; }
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var act = scope.ServiceProvider.GetRequiredService<ActualizadorSistema>();
                    act.LimpiarDescargas();
                    if (act.Configurado) await act.BuscarAsync(stop);
                }
                catch (OperationCanceledException) { return; }
                catch { /* sin internet o GitHub caido: se reintenta en la siguiente vuelta */ }
                try { await Task.Delay(TimeSpan.FromHours(12), stop); } catch (OperationCanceledException) { return; }
            }
        }
    }
}
