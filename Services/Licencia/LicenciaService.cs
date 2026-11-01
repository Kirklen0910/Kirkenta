using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Services.Licencia
{
    /// <summary>Lo que el sistema recuerda de la licencia activa (se guarda firmado en varios lugares).</summary>
    public class LicenciaEstadoInterno
    {
        public string LicId { get; set; } = "";
        public DateTime ActivadaUtc { get; set; }
        /// <summary>La fecha más avanzada que el sistema ha visto en el reloj. Si el reloj queda por detrás, hubo retroceso.</summary>
        public DateTime UltimaVistaUtc { get; set; }
        public bool RelojAlterado { get; set; }
        public string? BloqueoLicId { get; set; }
    }

    public record LicenciaVista(
        bool Aplica, bool Permitido, string Motivo, string Mensaje,
        DateTime? VenceUtc, int? DiasRestantes, string? Cliente, string? Tipo,
        string CodigoMaquina, DateTime? ActivadaUtc);

    /// <summary>
    /// Licencias sin internet: firmadas (ECDSA P-256), ligadas a la computadora y con detección de retroceso del reloj.
    /// El vencimiento se mide con el reloj, pero el sistema recuerda la fecha más avanzada que vio (en 4 lugares
    /// firmados): si el reloj queda por detrás de esa fecha, la licencia se bloquea hasta que el emisor entregue otra.
    /// </summary>
    public sealed class LicenciaService : IDisposable
    {
        public static readonly TimeSpan Tolerancia = TimeSpan.FromMinutes(5);

        public sealed class Contenedor
        {
            public string? Lic { get; set; }
            public string? EstadoJson { get; set; }
            public string? Mac { get; set; }
        }

        private readonly object _lock = new();
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<LicenciaService> _log;
        private readonly string[] _rutas;
        private readonly string _clavePublica;
        private readonly byte[] _macKey;
        private readonly Timer? _timer;

        private bool _cargado;
        private bool _tablaLista;
        private LicenciaDatos? _lic;
        private string? _licTexto;
        private LicenciaEstadoInterno? _st;
        private DateTime _ultimoLatido = DateTime.MinValue;
        private DateTime _ultimoGuardado = DateTime.MinValue;

        public string CodigoMaquina { get; }
        public bool Aplica => !string.IsNullOrWhiteSpace(_clavePublica);

        public LicenciaService(IWebHostEnvironment env, IServiceScopeFactory scopes, ILogger<LicenciaService> log)
        {
            _scopes = scopes;
            _log = log;
            _clavePublica = LicenciaClavePublica.Valor;

            var appData = Path.Combine(env.ContentRootPath, "App_Data");
            CodigoMaquina = Aplica ? ObtenerCodigoMaquina(appData) : "SIN-LICENCIAS";
            _macKey = SHA256.HashData(Encoding.UTF8.GetBytes("KRK-ESTADO|" + CodigoMaquina + "|k1r7e9n3t4a"));

            var sufijo = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(env.ContentRootPath.ToLowerInvariant())))[..12].ToLowerInvariant();
            _rutas = new[]
            {
                Path.Combine(appData, "licencia.dat"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Kirkenta", "kk-" + sufijo + ".dat"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kirkenta", "kk-" + sufijo + ".dat")
            };

            if (Aplica) _timer = new Timer(_ => Latido(), null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
        }

        public void Dispose() => _timer?.Dispose();

        // ---------------------------------------------------------------- API pública

        public LicenciaVista Estado()
        {
            if (!Aplica) return Evaluar(null, null, DateTime.UtcNow, CodigoMaquina, false);
            lock (_lock)
            {
                Cargar();
                if (DateTime.UtcNow - _ultimoLatido > TimeSpan.FromSeconds(20)) LatirInterno();
                return Evaluar(_lic, _st, DateTime.UtcNow, CodigoMaquina, true);
            }
        }

        /// <summary>Activa una licencia pegada por el usuario. Devuelve (ok, mensaje).</summary>
        public (bool Ok, string Mensaje) Activar(string? texto)
        {
            if (!Aplica) return (false, "Las licencias no están activadas en esta copia del sistema.");
            lock (_lock)
            {
                Cargar();
                var lic = LicenciaFormato.Leer(texto, _clavePublica, out var error);
                if (lic == null) return (false, error);

                if (lic.Maquina != "*" && NormCodigo(lic.Maquina) != NormCodigo(CodigoMaquina))
                    return (false, $"Esta licencia es para otra computadora. El código de esta computadora es {CodigoMaquina}.");

                var ahora = DateTime.UtcNow;
                var efectivo = _st != null && _st.UltimaVistaUtc > ahora ? _st.UltimaVistaUtc : ahora;

                if (_lic != null && _st != null && _lic.Id == lic.Id)
                {
                    if (_st.RelojAlterado && _st.BloqueoLicId == lic.Id)
                        return (false, "Esta licencia quedó bloqueada porque se detectó una alteración del reloj. Solicita una licencia nueva.");
                    return (true, "Esta licencia ya estaba activa.");
                }

                if (lic.Inicio == null && lic.ActivarAntes != null && efectivo > lic.ActivarAntes.Value)
                    return (false, $"El plazo para activar esta licencia terminó el {lic.ActivarAntes.Value.ToLocalTime():dd/MM/yyyy}. Solicita otra.");

                _lic = lic;
                _licTexto = LicenciaFormato.Normalizar(texto);
                _st = new LicenciaEstadoInterno { LicId = lic.Id, ActivadaUtc = efectivo, UltimaVistaUtc = efectivo };
                _ultimoLatido = DateTime.UtcNow;
                Guardar();
                _log.LogWarning("[Licencia] Licencia activada: {Tipo} de {Cliente}, {Dias} día(s).", lic.Tipo, lic.Cliente, lic.Dias);
                return (true, lic.Dias == 0 && lic.Minutos == 0 ? "Licencia activada (sin vencimiento)." : $"Licencia activada: {Texto(Duracion(lic))} de uso.");
            }
        }

        // ---------------------------------------------------------------- Evaluación (pura, probada en tests)

        public static LicenciaVista Evaluar(LicenciaDatos? lic, LicenciaEstadoInterno? st, DateTime ahoraUtc, string codigo, bool aplica)
        {
            if (!aplica)
                return new LicenciaVista(false, true, "desactivada", "Licencias desactivadas.", null, null, null, null, codigo, null);

            if (lic == null || st == null)
                return new LicenciaVista(true, false, "sin-licencia", "Este sistema necesita una licencia para funcionar.", null, null, null, null, codigo, null);

            if (lic.Maquina != "*" && NormCodigo(lic.Maquina) != NormCodigo(codigo))
                return new LicenciaVista(true, false, "maquina", "La licencia guardada es de otra computadora.", null, null, lic.Cliente, lic.Tipo, codigo, st.ActivadaUtc);

            if (st.RelojAlterado && st.BloqueoLicId == lic.Id && Vence(lic))
                return new LicenciaVista(true, false, "reloj",
                    "Se detectó que la fecha de la computadora fue atrasada o que se borraron los registros de la licencia. El sistema quedó bloqueado: solicita una licencia nueva.",
                    null, 0, lic.Cliente, lic.Tipo, codigo, st.ActivadaUtc);

            if (!Vence(lic))
                return new LicenciaVista(true, true, "ok", "Licencia sin vencimiento.", null, null, lic.Cliente, lic.Tipo, codigo, st.ActivadaUtc);

            var inicio = lic.Inicio ?? st.ActivadaUtc;
            var vence = inicio.Add(Duracion(lic));
            var efectivo = ahoraUtc > st.UltimaVistaUtc ? ahoraUtc : st.UltimaVistaUtc;
            var restante = vence - efectivo;
            var dias = (int)Math.Ceiling(restante.TotalDays);

            if (restante <= TimeSpan.Zero)
                return new LicenciaVista(true, false, "vencida",
                    (lic.Tipo == "demo" ? "La demostración terminó" : "La licencia venció") + $" el {vence.ToLocalTime():dd/MM/yyyy}. Solicita una licencia nueva.",
                    vence, 0, lic.Cliente, lic.Tipo, codigo, st.ActivadaUtc);

            return new LicenciaVista(true, true, "ok",
                (lic.Tipo == "demo" ? "Demostración" : "Licencia") + $": quedan {Texto(restante)}.",
                vence, dias, lic.Cliente, lic.Tipo, codigo, st.ActivadaUtc);
        }

        public static bool HuboRetroceso(LicenciaDatos lic, LicenciaEstadoInterno st, DateTime ahoraUtc)
            => Vence(lic) && !st.RelojAlterado && ahoraUtc < st.UltimaVistaUtc - Tolerancia;

        /// <summary>true si la licencia tiene vencimiento (en días o en minutos).</summary>
        public static bool Vence(LicenciaDatos lic) => lic.Dias > 0 || lic.Minutos > 0;

        public static TimeSpan Duracion(LicenciaDatos lic)
            => lic.Minutos > 0 ? TimeSpan.FromMinutes(lic.Minutos) : TimeSpan.FromDays(lic.Dias);

        /// <summary>"3 día(s)", "2 hora(s) 15 minuto(s)" o "40 minuto(s)", redondeando hacia arriba.</summary>
        public static string Texto(TimeSpan t)
        {
            if (t < TimeSpan.Zero) t = TimeSpan.Zero;
            if (t >= TimeSpan.FromDays(1)) return $"{(int)Math.Ceiling(t.TotalDays)} día(s)";
            var minutos = (int)Math.Ceiling(t.TotalMinutes);
            if (minutos >= 60)
            {
                var h = minutos / 60; var m = minutos % 60;
                return m == 0 ? $"{h} hora(s)" : $"{h} hora(s) {m} minuto(s)";
            }
            return $"{minutos} minuto(s)";
        }

        public static string NormCodigo(string? s) => (s ?? "").Replace("-", "").Trim().ToUpperInvariant();

        // ---------------------------------------------------------------- Latido (guarda la fecha más avanzada vista)

        private void Latido()
        {
            try { lock (_lock) { Cargar(); LatirInterno(); } }
            catch (Exception ex) { _log.LogWarning(ex, "[Licencia] Error en el latido."); }
        }

        private void LatirInterno()
        {
            _ultimoLatido = DateTime.UtcNow;
            if (_lic == null || _st == null || _st.LicId != _lic.Id) return;

            var ahora = DateTime.UtcNow;
            var cambio = false;

            if (HuboRetroceso(_lic, _st, ahora))
            {
                _st.RelojAlterado = true;
                _st.BloqueoLicId = _lic.Id;
                cambio = true;
                _log.LogError("[Licencia] Se detectó retroceso del reloj (visto {Visto:o}, ahora {Ahora:o}). Sistema bloqueado.", _st.UltimaVistaUtc, ahora);
            }

            if (ahora > _st.UltimaVistaUtc) { _st.UltimaVistaUtc = ahora; }
            if (cambio || ahora - _ultimoGuardado >= TimeSpan.FromSeconds(55)) Guardar();
        }

        // ---------------------------------------------------------------- Carga y guardado

        private void Cargar()
        {
            if (_cargado) return;
            _cargado = true;
            if (!Aplica) return;

            var contenedores = new List<Contenedor>();
            foreach (var r in _rutas)
            {
                try { if (File.Exists(r)) { var c = JsonSerializer.Deserialize<Contenedor>(File.ReadAllText(r)); if (c != null) contenedores.Add(c); } }
                catch { /* archivo dañado: se ignora */ }
            }
            var dbTxt = LeerDb();
            if (dbTxt != null)
            {
                try { var c = JsonSerializer.Deserialize<Contenedor>(dbTxt); if (c != null) contenedores.Add(c); } catch { }
            }

            // La licencia válida más reciente
            LicenciaDatos? mejor = null; string? mejorTxt = null;
            foreach (var c in contenedores)
            {
                var d = LicenciaFormato.Leer(c.Lic, _clavePublica, out _);
                if (d != null && (mejor == null || d.Emitida > mejor.Emitida)) { mejor = d; mejorTxt = LicenciaFormato.Normalizar(c.Lic); }
            }
            _lic = mejor; _licTexto = mejorTxt;
            if (mejor == null) { _st = null; return; }

            // El estado: se combinan las copias firmadas (activación más antigua, fecha vista más avanzada)
            LicenciaEstadoInterno? st = null;
            foreach (var c in contenedores)
            {
                if (string.IsNullOrEmpty(c.EstadoJson) || string.IsNullOrEmpty(c.Mac) || !MacValido(c.EstadoJson, c.Mac)) continue;
                LicenciaEstadoInterno? e;
                try { e = JsonSerializer.Deserialize<LicenciaEstadoInterno>(c.EstadoJson); } catch { continue; }
                if (e == null || e.LicId != mejor.Id) continue;
                if (st == null) { st = e; continue; }
                if (e.ActivadaUtc < st.ActivadaUtc) st.ActivadaUtc = e.ActivadaUtc;
                if (e.UltimaVistaUtc > st.UltimaVistaUtc) st.UltimaVistaUtc = e.UltimaVistaUtc;
                if (e.RelojAlterado) { st.RelojAlterado = true; st.BloqueoLicId ??= e.BloqueoLicId; }
            }

            if (st == null)
            {
                // Hay licencia pero se perdieron todos los registros: se reinicia solo si todavía está dentro de su plazo de activación
                var ahora = DateTime.UtcNow;
                st = new LicenciaEstadoInterno { LicId = mejor.Id, ActivadaUtc = ahora, UltimaVistaUtc = ahora };
                if (mejor.Inicio == null && mejor.ActivarAntes != null && ahora > mejor.ActivarAntes.Value)
                {
                    st.RelojAlterado = true;
                    st.BloqueoLicId = mejor.Id;
                    _log.LogError("[Licencia] No hay registros de activación y el plazo de activación ya terminó: bloqueada.");
                }
            }
            _st = st;
            Guardar();
        }

        private void Guardar()
        {
            _ultimoGuardado = DateTime.UtcNow;
            if (!Aplica || _lic == null || _st == null || _licTexto == null) return;

            var estadoJson = JsonSerializer.Serialize(_st);
            var cont = new Contenedor { Lic = _licTexto, EstadoJson = estadoJson, Mac = Mac(estadoJson) };
            var txt = JsonSerializer.Serialize(cont);

            foreach (var r in _rutas)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(r)!);
                    var tmp = r + ".tmp";
                    File.WriteAllText(tmp, txt);
                    File.Move(tmp, r, true);
                }
                catch { /* un lugar sin permiso de escritura no debe tumbar al sistema */ }
            }
            EscribirDb(txt);
        }

        private string Mac(string texto) => Convert.ToHexString(HMACSHA256.HashData(_macKey, Encoding.UTF8.GetBytes(texto)));

        private bool MacValido(string texto, string mac)
        {
            try { return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Mac(texto)), Encoding.ASCII.GetBytes(mac)); }
            catch { return false; }
        }

        // ---------------------------------------------------------------- Base de datos (cuarto lugar donde se guarda)

        private void AsegurarTabla(ApplicationDbContext db)
        {
            if (_tablaLista) return;
            db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS LicenciaEstado (Clave VARCHAR(40) NOT NULL PRIMARY KEY, Valor LONGTEXT NOT NULL) CHARACTER SET utf8mb4");
            _tablaLista = true;
        }

        private string? LeerDb()
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                AsegurarTabla(db);
                return db.Database.SqlQueryRaw<string>("SELECT Valor AS `Value` FROM LicenciaEstado WHERE Clave = {0}", "contenedor").AsEnumerable().FirstOrDefault();
            }
            catch { return null; }
        }

        private void EscribirDb(string txt)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                AsegurarTabla(db);
                db.Database.ExecuteSqlRaw("INSERT INTO LicenciaEstado (Clave, Valor) VALUES ({0}, {1}) ON DUPLICATE KEY UPDATE Valor = {1}", "contenedor", txt);
            }
            catch { }
        }

        // ---------------------------------------------------------------- Código de la computadora

        public static string ObtenerCodigoMaquina(string appDataDir)
        {
            var enContenedor = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
            var raw = "";
            if (!enContenedor)
            {
                try
                {
                    if (OperatingSystem.IsWindows())
                        raw = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", "MachineGuid", null) as string ?? "";
                }
                catch { }
                if (raw == "")
                {
                    foreach (var p in new[] { "/etc/machine-id", "/var/lib/dbus/machine-id" })
                    {
                        try { if (File.Exists(p)) { raw = File.ReadAllText(p).Trim(); break; } } catch { }
                    }
                }
            }
            if (raw == "")
            {
                // En contenedor (o sin identificador de equipo) se usa un identificador de instalación que vive en el volumen de datos
                try
                {
                    Directory.CreateDirectory(appDataDir);
                    var f = Path.Combine(appDataDir, "instalacion.id");
                    if (!File.Exists(f)) File.WriteAllText(f, Guid.NewGuid().ToString("N"));
                    raw = "inst:" + File.ReadAllText(f).Trim();
                }
                catch { raw = "inst:desconocida"; }
            }
            var h = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("KRK-MAQ|" + raw)))[..16];
            return $"{h[..4]}-{h[4..8]}-{h[8..12]}-{h[12..16]}";
        }
    }

    /// <summary>Bloquea el sistema (menos la pantalla de licencia) cuando no hay licencia válida.</summary>
    public class LicenciaMiddleware
    {
        private readonly RequestDelegate _next;
        public LicenciaMiddleware(RequestDelegate next) { _next = next; }

        public async Task InvokeAsync(HttpContext ctx, LicenciaService svc)
        {
            var v = svc.Estado();
            if (v.Permitido || EsRutaLibre(ctx.Request.Path))
            {
                ctx.Items["LicenciaVista"] = v;
                await _next(ctx);
                return;
            }

            // Las peticiones que no son una página (ventas offline que se sincronizan, fetch) reciben un código claro, nunca una redirección
            if (HttpMethods.IsGet(ctx.Request.Method) && !string.Equals(ctx.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Response.Redirect("/Licencia");
                return;
            }
            ctx.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            ctx.Response.ContentType = "text/plain; charset=utf-8";
            await ctx.Response.WriteAsync("Licencia vencida o no válida. Abre /Licencia para activarla.");
        }

        private static bool EsRutaLibre(PathString p)
            => p.StartsWithSegments("/Licencia", StringComparison.OrdinalIgnoreCase)
            || p.StartsWithSegments("/offline.html", StringComparison.OrdinalIgnoreCase)
            || p.StartsWithSegments("/sw.js", StringComparison.OrdinalIgnoreCase)
            || p.StartsWithSegments("/manifest.webmanifest", StringComparison.OrdinalIgnoreCase);
    }
}
