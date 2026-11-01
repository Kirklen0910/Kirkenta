using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Helpers.Logistica;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 🪵 Log a archivo (carpeta logs/, un archivo por día, solo advertencias y errores)
builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(builder.Environment.ContentRootPath, "logs")));

// ============================================================
// ⚙️ CONFIGURACIÓN (appsettings.json / user-secrets / variables de entorno)
// ============================================================
var permitirRegistroPublico = builder.Configuration.GetValue("Seguridad:PermitirRegistroPublico", false);
var cookieSoloHttps = builder.Configuration.GetValue("Seguridad:CookieSoloHttps", true);
var ejecutarReparaciones = builder.Configuration.GetValue("Mantenimiento:EjecutarReparacionesAlArrancar", true);
var activarCsp = builder.Configuration.GetValue("Seguridad:ActivarCSP", true);

// Clave para cifrar los respaldos (opcional). Vacía = respaldos sin cifrar. Ver Helpers/RespaldoCifrado.cs
RespaldoCifrado.Configurar(builder.Configuration["Seguridad:ClaveRespaldos"]);
// true solo si el sistema está detrás de un proxy propio (el Caddy del perfil https de Docker): así se ve la IP real del cliente
var confiarEnProxy = builder.Configuration.GetValue("Seguridad:ConfiarEnProxy", false);

// La cookie viaja solo por HTTPS en producción (salvo que se desactive porque se sirve por HTTP en una red interna).
var politicaCookieSegura = (builder.Environment.IsDevelopment() || !cookieSoloHttps)
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

// No anunciar el servidor ("Server: Kestrel") ni aceptar peticiones desmedidas
builder.WebHost.ConfigureKestrel(opciones =>
{
    opciones.AddServerHeader = false;
    opciones.Limits.MaxRequestLineSize = 8 * 1024;
    opciones.Limits.MaxRequestHeadersTotalSize = 32 * 1024;
    opciones.Limits.MaxRequestHeaderCount = 60;
    opciones.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);   // frena ataques tipo "slowloris"
    opciones.Limits.MinRequestBodyDataRate = new Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate(bytesPerSecond: 240, gracePeriod: TimeSpan.FromSeconds(30));
});

// Redirección HTTP -> HTTPS: si el instalador activó HTTPS propio, indica su puerto (Seguridad:PuertoHttps)
builder.Services.AddHttpsRedirection(opciones =>
{
    var puertoHttps = builder.Configuration.GetValue<int?>("Seguridad:PuertoHttps");
    if (puertoHttps.HasValue) opciones.HttpsPort = puertoHttps.Value;
});

// HSTS: el navegador recuerda que este sitio solo se usa por HTTPS (solo tiene efecto cuando se entra por HTTPS)
builder.Services.AddHsts(opciones =>
{
    opciones.MaxAge = TimeSpan.FromDays(365);
});

// Token anti-CSRF: su cookie no es accesible desde JavaScript y solo viaja en peticiones del mismo sitio
builder.Services.AddAntiforgery(opciones =>
{
    opciones.Cookie.Name = "KirkentaAF";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SameSite = SameSiteMode.Strict;
    opciones.Cookie.SecurePolicy = politicaCookieSegura;
});

static string IpCliente(HttpContext contexto) => contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

// true si la conexión viene de esta misma máquina o de la red privada (el proxy Caddy del perfil https de Docker)
static bool EsConexionDeConfianza(System.Net.IPAddress? ip)
{
    if (ip == null) return false;
    if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
    if (System.Net.IPAddress.IsLoopback(ip)) return true;
    if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
    var b = ip.GetAddressBytes();
    return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
}

// 🔌 Base de datos MySQL
// La contraseña real NO va en appsettings.json: se guarda con "dotnet user-secrets"
// (desarrollo) o con la variable de entorno ConnectionStrings__DefaultConnection (producción).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString)
    || connectionString.Contains("CAMBIAR_EN_USER_SECRETS", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "Falta la cadena de conexión real. Configúrala con: " +
        "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Server=...;Database=Kirkenta;User=...;Password=...;\" " +
        "(o con la variable de entorno ConnectionStrings__DefaultConnection).");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 📄 Razor Pages + Reglas de autorización
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Auth/Login");
    options.Conventions.AllowAnonymousToPage("/Auth/Logout");
    options.Conventions.AllowAnonymousToPage("/Logistica/Tracking/Index");
    options.Conventions.AllowAnonymousToPage("/Licencia/Index");   // pantalla de licencia (única disponible cuando el sistema está bloqueado)
    options.Conventions.AllowAnonymousToPage("/Catalogo/Index");   // catálogo en línea público (solo si el administrador lo activa)

    // El registro público solo existe si se activa en la configuración
    if (permitirRegistroPublico)
    {
        options.Conventions.AllowAnonymousToPage("/Auth/Register");
    }
});

// 🚦 Límite de peticiones: el seguimiento público de rutas es anónimo, así que se limita por IP
// para que nadie pueda probar códigos al azar (30 consultas por minuto por IP).
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
    {
        var ip = IpCliente(contexto);
        var ruta = contexto.Request.Path;

        // Seguimiento público de rutas: 30 consultas por minuto por IP
        if (ruta.StartsWithSegments("/Logistica/Tracking", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter("tracking:" + ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        }

        // Inicio de sesión: 20 intentos por minuto por IP (además del bloqueo por cuenta), contra ataques de fuerza bruta
        if (HttpMethods.IsPost(contexto.Request.Method) && ruta.StartsWithSegments("/Auth/Login", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter("login:" + ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        }

        // Catálogo público: 120 peticiones por minuto por IP
        if (ruta.StartsWithSegments("/Catalogo", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter("catalogo:" + ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        }

        // Tope general (muy holgado para el uso normal): frena robots y scripts que bombardean el servidor
        return RateLimitPartition.GetFixedWindowLimiter("general:" + ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1500,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

// 🔐 Autenticación por cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "KirkentaAuth";

        // En producción la cookie solo viaja por HTTPS (salvo que se desactive en la
        // configuración porque el sistema se sirve por HTTP en una red interna).
        options.Cookie.SecurePolicy = politicaCookieSegura;

        // Si el usuario fue desactivado o eliminado, pierde la sesión de inmediato
        // (antes la conservaba hasta 8 horas). Usa el cache de PermisoHelper, así que
        // casi nunca toca la base de datos.
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var username = context.Principal?.Identity?.Name;
                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

                // Se rechaza si el usuario ya no existe / está desactivado, o si cambió su contraseña
                // después de abrir esta sesión (la cookie lleva un sello de la clave con la que entró).
                var sello = context.Principal?.FindFirst(SesionHelper.ClaimSello)?.Value;
                if (!PermisoHelper.UsuarioActivo(db, username) || !PermisoHelper.SesionVigente(db, username, sello))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });

builder.Services.AddAuthorization();

// 📦 Sesión
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = politicaCookieSegura;
    options.Cookie.Name = "KirkentaSession";
});

// 🌐 Acceso a HttpContext
builder.Services.AddHttpContextAccessor();

// 🧾 Servicios de negocio (la lógica de las páginas vive aquí)
builder.Services.AddScoped<IVentaService, VentaService>();

// 🤖 Asistente del sistema (motor de reglas gratis; IA opcional por configuración)
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<Kirkenta.Services.Asistente.AsistenteIAConfigStore>();
builder.Services.AddScoped<Kirkenta.Services.Asistente.AsistenteIA>();
builder.Services.AddScoped<Kirkenta.Services.Asistente.AsistenteService>();

// ✉️ Resumen diario del negocio por correo (apagado hasta configurar Correo:Host y Correo:Destinatarios)
builder.Services.AddHostedService<Kirkenta.Services.ResumenDiarioService>();
builder.Services.AddSingleton<Kirkenta.Services.RespaldoConfigStore>();
builder.Services.AddSingleton<Kirkenta.Services.ImpresoraConfigStore>();
builder.Services.AddScoped<Kirkenta.Services.ImpresoraTermicaService>();
builder.Services.AddScoped<Kirkenta.Services.RespaldoSistemaService>();
builder.Services.AddSingleton<Kirkenta.Services.Licencia.LicenciaService>();   // licencias de uso (demos y vigencias), sin internet
builder.Services.AddHostedService<Kirkenta.Services.RespaldoAutomaticoService>();
// 🔄 Actualizaciones por internet (GitHub Releases, firmadas): busca sola cada 12 h y el Admin instala desde Configuración
builder.Services.AddScoped<Kirkenta.Services.ActualizadorSistema>();
builder.Services.AddHostedService<Kirkenta.Services.ActualizacionesAutomaticasService>();

var app = builder.Build();

// Arranca el vigilante de licencias (guarda la fecha más avanzada vista para detectar retrocesos del reloj)
app.Services.GetRequiredService<Kirkenta.Services.Licencia.LicenciaService>();

// ============================================================
// 🔄 TAREAS DE ARRANQUE (seeders y reparaciones)
// ============================================================
// - Cada tarea se ejecuta aislada: si una falla, se registra el error completo en el
//   log (con su stack trace) y se limpia el contexto para que no contamine a la siguiente.
// - Las reparaciones puntuales (Aperturas y ReparacionFinanzas) son idempotentes y se
//   controlan con "Mantenimiento:EjecutarReparacionesAlArrancar". Cuando ya corrieron
//   bien una vez, se puede poner en false para que el arranque sea más rápido.
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Arranque");

    var tareasConError = 0;
    var cronometroTotal = Stopwatch.StartNew();

    void EjecutarTarea(string nombre, Action accion)
    {
        var cronometro = Stopwatch.StartNew();
        try
        {
            accion();
            logger.LogInformation("[Arranque] {Tarea} OK ({Ms} ms)", nombre, cronometro.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            tareasConError++;
            logger.LogError(ex, "[Arranque] {Tarea} FALLÓ", nombre);
            db.ChangeTracker.Clear();
        }
    }

    logger.LogInformation("[Arranque] Iniciando seeders...");

    // Instalación limpia (base de datos vacía, p. ej. en Docker): crea las tablas del modelo y el administrador inicial.
    // En una base que ya tiene tablas no hace nada.
    if (builder.Configuration.GetValue("Mantenimiento:CrearEsquemaSiVacia", true))
    {
        EjecutarTarea("CrearEsquemaInicial", () =>
        {
            if (db.Database.EnsureCreated())
                logger.LogWarning("[Arranque] Base de datos nueva: se crearon las tablas del sistema.");
        });
        EjecutarTarea("AdministradorInicial", () =>
        {
            var msg = InstalacionInicialHelper.CrearAdminSiNoHayUsuarios(db, builder.Environment.ContentRootPath,
                builder.Configuration["Inicial:AdminUsuario"], builder.Configuration["Inicial:AdminClave"]);
            if (msg != null) logger.LogWarning("[Arranque] {Msg}", msg);
        });
    }

    // Token de concurrencia en saldos de cuentas (antes de cualquier consulta)
    EjecutarTarea("EsquemaConcurrenciaCuentas", () => EsquemaConcurrenciaCuentas.Aplicar(db));

    // Columnas nuevas de exoneración de ISV (antes de cualquier consulta)
    EjecutarTarea("EsquemaExoneracion", () => EsquemaExoneracion.Aplicar(db));

    // Columna "admite descuento" de los productos (antes de cualquier consulta)
    EjecutarTarea("EsquemaDescuentoProducto", () => EsquemaDescuentoProducto.Aplicar(db));

    // Tablas de Activos fijos, Producción, Caja chica, Toma física y Asientos (solo crea las que faltan)
    EjecutarTarea("EsquemaModulosNuevos", () => EsquemaModulosNuevos.Aplicar(db));

    // Índices de rendimiento (solo crea los que faltan; no toca datos)
    EjecutarTarea("EsquemaIndices", () =>
    {
        var creados = EsquemaIndices.Aplicar(db, msg => logger.LogWarning("[Arranque] {Msg}", msg));
        if (creados > 0) logger.LogInformation("[Arranque] Índices de rendimiento creados: {N}", creados);
    });

    // Actualizador automático (solo aditivo): crea tablas/columnas que falten, con respaldo previo si hay datos.
    // Se puede apagar con Mantenimiento:ActualizarEsquemaAutomatico=false. Ver tools\LEEME-ACTUALIZACIONES.md
    if (builder.Configuration.GetValue("Mantenimiento:ActualizarEsquemaAutomatico", true))
    {
        EjecutarTarea("ActualizacionBD", () =>
        {
            var respaldoSvc = scope.ServiceProvider.GetRequiredService<Kirkenta.Services.RespaldoSistemaService>();
            var rAct = ActualizacionBD.Aplicar(db, respaldoSvc, builder.Configuration.GetValue("Mantenimiento:RespaldoAntesDeActualizar", true));
            if (rAct.RespaldoPrevio != null)
                logger.LogWarning("[Arranque] Respaldo previo a la actualización: {Ruta}", rAct.RespaldoPrevio);
            foreach (var tAct in rAct.TablasCreadas) logger.LogWarning("[Arranque] ActualizacionBD: tabla creada {X}", tAct);
            foreach (var cAct in rAct.ColumnasAgregadas) logger.LogWarning("[Arranque] ActualizacionBD: columna agregada {X}", cAct);
            foreach (var pAct in rAct.PasosAplicados) logger.LogWarning("[Arranque] ActualizacionBD: paso manual aplicado {X}", pAct);
            foreach (var eAct in rAct.Errores) logger.LogError("[Arranque] ActualizacionBD: {Msg}", eAct);
        });
    }

    // Avisa en el log si a la base le falta alguna tabla/columna del modelo (migración pendiente)
    EjecutarTarea("DiagnosticoEsquema", () =>
    {
        var faltantes = DiagnosticoEsquema.BuscarFaltantes(db);
        if (faltantes.Count == 0)
            logger.LogInformation("[Arranque] Esquema de base de datos completo.");
        else
            logger.LogWarning("[Arranque] A la base de datos le faltan {N} columna(s)/tabla(s) del modelo: {Lista}. Revisa los mensajes de ActualizacionBD arriba o corre 'dotnet ef database update'.",
                faltantes.Count, string.Join(", ", faltantes));
    });

    EjecutarTarea("PermisoSeeder", () => PermisoSeeder.MigrarPermisosFaltantes(db));
    EjecutarTarea("PermisosPredeterminados", () => PermisoSeeder.AplicarPermisosPredeterminados(db));
    EjecutarTarea("PlanCuentasHelper", () => PlanCuentasHelper.Seed(db));
    EjecutarTarea("FinanzasSeeder", () => FinanzasSeeder.Seed(db));

    // Asientos de partida doble de los movimientos financieros (idempotente; solo crea los que faltan)
    EjecutarTarea("AsientosAutomaticos", () =>
    {
        var r = AsientoAutomaticoHelper.Sincronizar(db);
        logger.LogInformation("[Arranque] Asientos automáticos: {C} nuevos, {R} reemplazados, {A} anulados, {S} sin cuenta.",
            r.Creados, r.Reemplazados, r.Anulados, r.SinCuenta);
    });

    if (ejecutarReparaciones)
    {
        // Crea los movimientos de apertura de las cuentas financieras con SaldoInicial > 0
        // para que el Balance General refleje el Activo real. Idempotente.
        EjecutarTarea("AperturaCuentaHelper", () =>
        {
            var aperturasCreadas = AperturaCuentaHelper.ProcesarAperturasPendientes(db);
            logger.LogInformation("[Arranque] Aperturas creadas: {Cantidad}", aperturasCreadas);
        });

        // Corrige COGS que descontaron caja y ventas a crédito facturadas que bloqueaban
        // el cobro. Idempotente.
        EjecutarTarea("ReparacionFinanzasHelper", () =>
        {
            var (cogsCorregidos, creditosReabiertos) = ReparacionFinanzasHelper.Ejecutar(db);
            logger.LogInformation(
                "[Arranque] ReparacionFinanzas: COGS corregidos {Cogs}, créditos reabiertos {Creditos}",
                cogsCorregidos, creditosReabiertos);
        });
    }
    else
    {
        logger.LogInformation("[Arranque] Reparaciones omitidas (Mantenimiento:EjecutarReparacionesAlArrancar = false)");
    }

    EjecutarTarea("TipoDocumentoEmpleadoSeeder", () => TipoDocumentoEmpleadoSeeder.Seed(db));

    // Feriados: últimos 3 años, para que el cálculo de vacaciones de años anteriores
    // funcione. Idempotente (AgregarFeriado revisa nombre + fecha + país).
    EjecutarTarea("FeriadoSeeder (últimos 3 años)", () =>
    {
        var anioActual = DateTime.Today.Year;
        for (var anio = anioActual - 2; anio <= anioActual; anio++)
        {
            FeriadoSeeder.Seed(db, anio);
        }
    });

    EjecutarTarea("ZonaEnvioSeeder", () => ZonaEnvioSeeder.Seed(db));
    EjecutarTarea("MetodoPagoSeeder", () => MetodoPagoSeeder.Seed(db));
    EjecutarTarea("ImpuestoSeeder", () => ImpuestoSeeder.Seed(db));

    EjecutarTarea("EmpleadoHelper.ProcesarAcumulacionGlobal", () =>
    {
        var procesados = EmpleadoHelper.ProcesarAcumulacionGlobal(db);
        logger.LogInformation("[Arranque] Empleados procesados para acumulación de vacaciones: {Cantidad}", procesados);
    });

    EjecutarTarea("AdjuntoEmpleadoHelper.ActualizarVigencias", () => AdjuntoEmpleadoHelper.ActualizarVigencias(db));

    if (tareasConError > 0)
    {
        logger.LogWarning("[Arranque] Seeders finalizados en {Ms} ms con {Errores} tarea(s) con error. Revisa el log.",
            cronometroTotal.ElapsedMilliseconds, tareasConError);
    }
    else
    {
        logger.LogInformation("[Arranque] Seeders finalizados en {Ms} ms sin errores.", cronometroTotal.ElapsedMilliseconds);
    }
}

// ============================================================
// 🌐 PIPELINE HTTP
// ============================================================

// IP y protocolo reales del cliente cuando hay un proxy de confianza delante (Caddy del perfil https de Docker).
// Solo se acepta X-Forwarded-* si la conexión viene de esta máquina o de la red privada: un cliente de internet no puede falsificarlos.
if (confiarEnProxy)
{
    app.Use(async (context, next) =>
    {
        if (EsConexionDeConfianza(context.Connection.RemoteIpAddress))
        {
            var reenviada = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(reenviada))
            {
                var ultima = reenviada.Split(',')[^1].Trim();   // la que agregó nuestro proxy
                if (System.Net.IPAddress.TryParse(ultima, out var ipReal))
                    context.Connection.RemoteIpAddress = ipReal;
            }

            var protocolo = context.Request.Headers["X-Forwarded-Proto"].ToString();
            if (protocolo == "https" || protocolo == "http")
                context.Request.Scheme = protocolo;
        }
        await next();
    });
}

// Cabeceras de seguridad
// - CSP: el navegador solo carga scripts, estilos e imágenes del propio sistema (Chart.js va local en wwwroot/lib/chartjs), bloquea plugins
//   (object-src) y evita que otra página meta a Kirkenta en un marco (clickjacking) o envíe formularios a otro sitio.
//   Si alguna pantalla dejara de funcionar por esto, se puede apagar con Seguridad:ActivarCSP = false.
var politicaCsp = string.Join("; ", new[]
{
    "default-src 'self'",
    "script-src 'self' 'unsafe-inline'",
    "style-src 'self' 'unsafe-inline'",
    "img-src 'self' data: blob: https:",
    "font-src 'self' data:",
    "connect-src 'self'",
    "media-src 'self' blob:",
    "frame-src 'self' blob:",
    "worker-src 'self'",
    "manifest-src 'self'",
    "object-src 'none'",
    "base-uri 'self'",
    "form-action 'self'",
    "frame-ancestors 'self'"
});

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(self), microphone=(), geolocation=(), payment=(), usb=(), serial=(), bluetooth=(), accelerometer=(), gyroscope=(), magnetometer=()";
    headers["Cross-Origin-Opener-Policy"] = "same-origin";
    headers["X-Permitted-Cross-Domain-Policies"] = "none";
    if (activarCsp && !app.Environment.IsDevelopment()) headers["Content-Security-Policy"] = politicaCsp;   // en desarrollo se omite (la recarga en caliente usa websockets)

    // Las páginas del sistema llevan datos del negocio: el navegador no debe guardarlas en caché
    // (en una computadora compartida, el botón "atrás" tras cerrar sesión no debe mostrar información).
    context.Response.OnStarting(() =>
    {
        var tipo = context.Response.ContentType;
        if (tipo != null && tipo.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
            context.Response.Headers["Pragma"] = "no-cache";
        }
        return Task.CompletedTask;
    });

    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 🔒 Los documentos subidos (adjuntos de empleados, cierres de caja y compras) NO son públicos:
// solo se entregan a un usuario con sesión válida y con acceso al módulo correspondiente.
// En /uploads solo quedan públicos los archivos sueltos de la raíz (el logo de la empresa).
app.Use(async (context, next) =>
{
    var rutaNormal = (context.Request.Path.Value ?? "").Replace('\\', '/');
    rutaNormal = System.Text.RegularExpressions.Regex.Replace(rutaNormal, "/{2,}", "/");
    if (!rutaNormal.StartsWith("/")) rutaNormal = "/" + rutaNormal;

    const string raizUploads = "/uploads/";
    if (rutaNormal.StartsWith(raizUploads, StringComparison.OrdinalIgnoreCase)
        && rutaNormal.IndexOf('/', raizUploads.Length) > 0)   // está dentro de una subcarpeta
    {
        var autenticacion = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!autenticacion.Succeeded || autenticacion.Principal == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;   // no se revela que el archivo existe
            return;
        }
        context.User = autenticacion.Principal;

        // Además de tener sesión, debe tener acceso al módulo dueño del documento
        (string Modulo, string Sub)? requerido = null;
        if (rutaNormal.StartsWith("/uploads/empleados/", StringComparison.OrdinalIgnoreCase)) requerido = ("RRHH", "Empleados");
        else if (rutaNormal.StartsWith("/uploads/cierres/", StringComparison.OrdinalIgnoreCase)) requerido = ("Finanzas", "Cierres");
        else if (rutaNormal.StartsWith("/uploads/compras/", StringComparison.OrdinalIgnoreCase)) requerido = ("Compras", "Ordenes");

        if (requerido != null)
        {
            var dbPermisos = context.RequestServices.GetRequiredService<ApplicationDbContext>();
            if (!PermisoHelper.UsuarioTieneAcceso(dbPermisos, context.User.Identity?.Name, requerido.Value.Modulo, requerido.Value.Sub))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
        }

        context.Response.Headers["Cache-Control"] = "private, no-store";
    }

    await next();
});

app.UseStaticFiles();

// 🔑 Sin licencia válida solo queda disponible la pantalla /Licencia
app.UseMiddleware<Kirkenta.Services.Licencia.LicenciaMiddleware>();

// Si el registro público está desactivado, la página directamente no existe
if (!permitirRegistroPublico)
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/Auth/Register", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next();
    });
}

app.UseRouting();

app.UseRateLimiter();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Empresa activa de la peticion (claim EmpresaId; por defecto 1)
app.Use(async (context, next) =>
{
    Kirkenta.Helpers.EmpresaActual.Establecer(context.User);
    await next();
});

app.MapRazorPages();

app.Run();
