using System.Text.Json;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Kirkenta.Services.Asistente
{
    /// <summary>
    /// Orquesta el asistente: entiende el mensaje, revisa permisos, pide datos que falten, exige confirmación
    /// y contraseña en lo sensible, ejecuta y deja bitácora. Nunca toca datos sin pasar por los helpers del sistema.
    /// </summary>
    public class AsistenteService
    {
        private const string Proposito = "Kirkenta.Asistente.v1";
        private const int MaxIntentos = 5;

        private readonly ApplicationDbContext _db;
        private readonly IDataProtectionProvider _dp;
        private readonly IMemoryCache _cache;
        private readonly AsistenteIA _ia;
        private readonly ILogger<AsistenteService> _log;

        public AsistenteService(ApplicationDbContext db, IDataProtectionProvider dp, IMemoryCache cache,
            AsistenteIA ia, ILogger<AsistenteService> log)
        {
            _db = db; _dp = dp; _cache = cache; _ia = ia; _log = log;
        }

        public bool IaActiva => _ia.Activa;

        private class TokenData
        {
            public int U { get; set; }
            public string A { get; set; } = "";
            public Dictionary<string, string> P { get; set; } = new();
            public string N { get; set; } = "";
            public long Exp { get; set; }
        }

        // ───────────────────────── Catálogo para los chips del chat ─────────────────────────
        public object Catalogo(string? username)
        {
            var u = ObtenerUsuario(username);
            if (u == null) return Array.Empty<object>();
            return AsistenteCatalogo.Todas
                .Where(a => a.Tipo != TipoAccion.Navegar && PermitidaPara(a, u))
                .OrderBy(a => a.Modulo).ThenBy(a => a.Titulo)
                .Select(a => new { a.Id, a.Modulo, a.Titulo, a.Ejemplo, a.RequierePassword, operacion = a.Tipo == TipoAccion.Operacion })
                .ToList();
        }

        // ───────────────────────── Mensaje del usuario ─────────────────────────
        public async Task<RespuestaAsistente> ProcesarAsync(string? mensaje, ContextoCliente? ctx, string? username, string? ip)
        {
            var usuario = ObtenerUsuario(username);
            if (usuario == null) return RespuestaAsistente.Error("Tu sesión no es válida. Vuelve a iniciar sesión.");
            if (usuario.Rol == "Pendiente") return RespuestaAsistente.Error("Tu cuenta todavía no tiene permisos asignados.");

            mensaje = (mensaje ?? "").Trim();
            if (mensaje.Length == 0) return RespuestaAsistente.Texto("Escribe lo que necesitas. Por ejemplo: “ventas de hoy” o “ayuda”.");
            if (mensaje.Length > 500) mensaje = mensaje[..500];

            try
            {
                var respuesta = await ResolverAsync(mensaje, ctx, usuario, ip);
                return Completar(respuesta);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error en el asistente (usuario {Usuario})", usuario.Username);
                return RespuestaAsistente.Error("Ocurrió un error al procesar tu solicitud. Intenta de nuevo o hazlo desde la pantalla correspondiente.");
            }
        }

        private async Task<RespuestaAsistente> ResolverAsync(string mensaje, ContextoCliente? ctx, Usuario usuario, string? ip)
        {
            var hoy = DateTime.Today;

            // 1) ¿Estamos esperando un dato que el asistente pidió antes?
            if (ctx?.Slot != null && ctx.Accion != null)
            {
                var pendiente = AsistenteCatalogo.Buscar(ctx.Accion);
                if (pendiente != null)
                {
                    var norm = AsistenteMotor.Normalizar(mensaje);
                    if (norm is "cancelar" or "cancela" or "salir" or "olvidalo" or "dejalo")
                        return RespuestaAsistente.Texto("Listo, lo cancelé. ¿En qué más te ayudo?");

                    var otra = AsistenteMotor.Interpretar(mensaje, AsistenteCatalogo.Todas, hoy);
                    var esNumerico = ctx.Slot is "contado" or "fondo";
                    var esNuevaPeticion = otra.Accion != null && otra.Accion.Id != pendiente.Id && otra.Puntaje >= 6
                                          && (!esNumerico || AsistenteMotor.ExtraerNumero(mensaje) == null);

                    if (!esNuevaPeticion)
                    {
                        var p = new Dictionary<string, string>(ctx.Params ?? new Dictionary<string, string>());
                        if (!RellenarSlot(ctx.Slot, mensaje, p, out var error))
                            return RespuestaAsistente.Faltante(error!, pendiente.Id, p, ctx.Slot);
                        return Ejecutar(pendiente, p, usuario, ip, hoy);
                    }
                }
            }

            // 2) Charla básica
            if (AsistenteMotor.EsSaludo(mensaje))
                return RespuestaAsistente.Texto($"¡Hola, {usuario.NombreCompleto ?? usuario.Username}! Dime qué necesitas: ventas, caja, inventario, cobranza, RRHH… o escribe “ayuda”.");
            if (AsistenteMotor.EsGracias(mensaje))
                return RespuestaAsistente.Texto("¡Con gusto! Aquí estoy si necesitas algo más.");
            var normalizado = AsistenteMotor.Normalizar(mensaje);
            var charla = AsistenteLenguaje.Charla(normalizado, usuario.NombreCompleto ?? usuario.Username);
            if (charla != null) return RespuestaAsistente.Texto(charla);
            if (AsistenteLenguaje.PideModulos(normalizado)) return MenuModulos(usuario);

            // 2.4) Seguimiento de la última consulta: "¿y ayer?", "¿y el mes pasado?", "y solo en efectivo"
            if (!string.IsNullOrEmpty(ctx?.UltimaAccion))
            {
                var seguimiento = ContinuarConsulta(mensaje, ctx!, usuario, ip, hoy);
                if (seguimiento != null) return seguimiento;
            }

            // 2.5) Solo nombró un módulo ("ventas", "qué puedo hacer en finanzas"): se muestran sus opciones
            var modulo = AsistenteMotor.ModuloMencionado(mensaje);
            if (modulo != null) return MenuModulo(modulo, usuario);

            // 3) Entender el mensaje con reglas (gratis)
            var inter = AsistenteMotor.Interpretar(mensaje, AsistenteCatalogo.Todas, hoy);
            var p2 = new Dictionary<string, string> { ["raw"] = mensaje };

            if (inter.Accion == null)
            {
                // 4) Si la empresa activó IA opcional, se le pide ayuda para interpretar
                if (_ia.Activa)
                {
                    var permitidas = AsistenteCatalogo.Todas.Where(a => a.Tipo != TipoAccion.Navegar && PermitidaPara(a, usuario)).ToList();
                    var ia = await _ia.InterpretarAsync(mensaje, permitidas, hoy);
                    if (ia != null)
                    {
                        var def = AsistenteCatalogo.Buscar(ia.Accion)!;
                        if (!string.IsNullOrWhiteSpace(ia.Texto)) p2["texto"] = AsistenteMotor.Normalizar(ia.Texto);
                        if (ia.Numero.HasValue) p2["numero"] = ia.Numero.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        if (ia.Desde.HasValue) { p2["desde"] = ia.Desde.Value.ToString("yyyy-MM-dd"); p2["hasta"] = (ia.Hasta ?? ia.Desde.Value).ToString("yyyy-MM-dd"); p2["rango"] = $"del {ia.Desde:dd/MM/yyyy} al {(ia.Hasta ?? ia.Desde):dd/MM/yyyy}"; }
                        var respIa = Ejecutar(def, p2, usuario, ip, hoy);
                        respIa.Origen = "ia";
                        return respIa;
                    }
                    var noIa = NoEntendi(usuario, mensaje);
                    noIa.Origen = "ia-sin-respuesta";
                    return noIa;
                }
                var noReglas = NoEntendi(usuario, mensaje);
                noReglas.Origen = "ia-apagada";
                return noReglas;
            }

            // Ambigüedad: dos acciones con puntaje casi igual
            if (inter.Ambigua)
            {
                var opciones = inter.Candidatas.Where(a => PermitidaPara(a, usuario) || a.Tipo == TipoAccion.Operacion)
                    .Select(a => new OpcionAsistente { Texto = a.Titulo, Envio = a.Ejemplo }).ToList();
                if (opciones.Count > 1)
                    return RespuestaAsistente.Elegir("Puedo entenderlo de varias formas. ¿Cuál quieres?", opciones);
            }

            if (inter.Texto != null) p2["texto"] = inter.Texto;
            if (inter.Numero.HasValue) p2["numero"] = inter.Numero.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (inter.Rango.HasValue)
            {
                p2["desde"] = inter.Rango.Value.desde.ToString("yyyy-MM-dd");
                p2["hasta"] = inter.Rango.Value.hasta.ToString("yyyy-MM-dd");
                p2["rango"] = inter.Rango.Value.etiqueta;
            }
            return Ejecutar(inter.Accion, p2, usuario, ip, hoy);
        }

        private RespuestaAsistente Ejecutar(AccionDef def, Dictionary<string, string> p, Usuario usuario, string? ip, DateTime hoy)
        {
            if (!PermitidaPara(def, usuario))
                return RespuestaAsistente.Error($"No tienes permiso para esto ({def.Modulo} › {def.Titulo}). Pídele acceso a un administrador.");

            var c = Contexto(p, usuario, ip, hoy);

            if (def.Tipo == TipoAccion.Operacion)
            {
                var prep = def.Preparar!(c);
                if (prep.Tipo == "confirmar") prep.Token = Proteger(usuario.Id, def.Id, c.P);
                return prep;
            }

            if (def.RequierePassword)
            {
                return new RespuestaAsistente
                {
                    Tipo = "confirmar",
                    Titulo = "Información protegida",
                    Mensaje = $"“{def.Titulo}” es información sensible. Confirma con tu contraseña para verla.",
                    RequierePassword = true,
                    Resumen = new List<string> { def.Titulo + (def.UsaFechas ? " · " + c.RangoTexto : "") },
                    Token = Proteger(usuario.Id, def.Id, c.P)
                };
            }

            var resp = def.Ejecutar!(c);
            if (def.Tipo == TipoAccion.Consulta && resp.Tipo is "tabla" or "texto")
                resp.Seguimiento = new ContextoCliente { UltimaAccion = def.Id, UltimaParams = new Dictionary<string, string>(c.P) };
            return resp;
        }

        /// <summary>
        /// Si el mensaje no pide nada nuevo pero trae otra fecha ("¿y ayer?") o empieza con "y"/"solo",
        /// repite la última consulta con ese cambio. Devuelve null si no es un seguimiento.
        /// </summary>
        private RespuestaAsistente? ContinuarConsulta(string mensaje, ContextoCliente ctx, Usuario usuario, string? ip, DateTime hoy)
        {
            var def = AsistenteCatalogo.Buscar(ctx.UltimaAccion!);
            if (def == null || def.Tipo != TipoAccion.Consulta) return null;

            var toks = AsistenteMotor.Tokens(AsistenteMotor.Normalizar(mensaje));
            if (toks.Length == 0 || toks.Length > 7) return null;

            var inter = AsistenteMotor.Interpretar(mensaje, AsistenteCatalogo.Todas, hoy);
            if (inter.Accion != null) return null; // pidió otra cosa: se procesa normal

            var p = new Dictionary<string, string>(ctx.UltimaParams ?? new Dictionary<string, string>());
            var conecta = toks[0] is "y" or "ahora" or "solo" or "tambien" or "pero";
            var cambio = false;

            if (inter.Rango.HasValue && def.UsaFechas)
            {
                p["desde"] = inter.Rango.Value.desde.ToString("yyyy-MM-dd");
                p["hasta"] = inter.Rango.Value.hasta.ToString("yyyy-MM-dd");
                p["rango"] = inter.Rango.Value.etiqueta;
                cambio = true;
            }
            else if (conecta && toks.Length >= 2)
            {
                var resto = string.Join(' ', toks.Skip(1));
                p["raw"] = ((p.GetValueOrDefault("raw") ?? "") + " " + resto).Trim();
                p["texto"] = resto;
                cambio = true;
            }

            return cambio ? Ejecutar(def, p, usuario, ip, hoy) : null;
        }

        // ───────────────────────── Confirmación con contraseña ─────────────────────────
        public RespuestaAsistente Confirmar(string? token, string? password, string? autorizador, string? username, string? ip)
        {
            var usuario = ObtenerUsuario(username);
            if (usuario == null) return RespuestaAsistente.Error("Tu sesión no es válida. Vuelve a iniciar sesión.");

            TokenData? td;
            try
            {
                var json = _dp.CreateProtector(Proposito).Unprotect(token ?? "");
                td = JsonSerializer.Deserialize<TokenData>(json);
            }
            catch { return RespuestaAsistente.Error("La confirmación no es válida o venció. Pídeme la acción de nuevo."); }

            if (td == null || td.U != usuario.Id) return RespuestaAsistente.Error("Esta confirmación pertenece a otro usuario.");
            if (DateTime.UtcNow.Ticks > td.Exp) return RespuestaAsistente.Error("La confirmación venció (5 minutos). Pídeme la acción de nuevo.");
            if (_cache.TryGetValue("asist-" + td.N, out _)) return RespuestaAsistente.Error("Esta acción ya se ejecutó.");

            var def = AsistenteCatalogo.Buscar(td.A);
            if (def == null) return RespuestaAsistente.Error("La acción ya no existe.");
            if (string.IsNullOrEmpty(password)) return RespuestaAsistente.Error("Escribe la contraseña para confirmar.");

            // ¿Quién autoriza? Por defecto el mismo usuario; opcionalmente otro con permiso (ej. el administrador).
            Usuario? autor = usuario;
            var otro = !string.IsNullOrWhiteSpace(autorizador) && !string.Equals(autorizador.Trim(), usuario.Username, StringComparison.OrdinalIgnoreCase);
            if (otro)
            {
                var nombreAut = autorizador!.Trim();
                autor = _db.Usuarios.FirstOrDefault(u => u.Username == nombreAut && u.Activo);
            }
            if (autor == null) return RespuestaAsistente.Error("Usuario o contraseña del autorizador incorrectos.");

            if (!VerificarPassword(autor, password, out var errorPass))
            {
                ActividadHelper.Registrar(_db, usuario.Id, "Asistente: contraseña incorrecta",
                    $"Confirmación de “{def.Titulo}” rechazada" + (otro ? $" (autorizador {autor.Username})" : ""), ip);
                return RespuestaAsistente.Error(otro && errorPass == "Contraseña incorrecta." ? "Usuario o contraseña del autorizador incorrectos." : errorPass);
            }

            if (!PermitidaPara(def, autor))
                return RespuestaAsistente.Error($"{autor.Username} no tiene permiso para autorizar “{def.Titulo}”.");
            if (!PermitidaPara(def, usuario))
                return RespuestaAsistente.Error("Ya no tienes permiso para esta acción.");

            _cache.Set("asist-" + td.N, true, TimeSpan.FromMinutes(10));

            try
            {
                var c = Contexto(td.P, usuario, ip, DateTime.Today);
                c.Autorizo = autor.Username;
                c.AutorizoId = autor.Id;

                var resp = def.Ejecutar!(c);
                if (def.Tipo == TipoAccion.Consulta)
                    ActividadHelper.Registrar(_db, usuario.Id, "Asistente: consulta protegida", $"Consultó “{def.Titulo}”. Autorizó: {autor.Username}", ip);
                return Completar(resp);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error ejecutando acción del asistente {Accion}", def.Id);
                return RespuestaAsistente.Error("No se pudo completar la acción. Revisa el resultado en la pantalla correspondiente antes de reintentar.");
            }
        }

        // ───────────────────────── Utilidades ─────────────────────────
        private Usuario? ObtenerUsuario(string? username)
            => string.IsNullOrWhiteSpace(username) ? null : _db.Usuarios.FirstOrDefault(u => u.Username == username && u.Activo);

        private bool PermitidaPara(AccionDef a, Usuario u)
        {
            if (a.PermModulo == null) return true;
            var sub = a.PermSub ?? "Index";
            if (a.Tipo == TipoAccion.Operacion || !string.Equals(a.PermAccion, "ver", StringComparison.OrdinalIgnoreCase))
                return PermisoHelper.UsuarioTienePermiso(_db, u.Username, a.PermModulo, sub, a.PermAccion);
            return PermisoHelper.UsuarioTieneAcceso(_db, u.Username, a.PermModulo, sub);
        }

        private ContextoAccion Contexto(Dictionary<string, string> p, Usuario usuario, string? ip, DateTime hoy)
        {
            var c = new ContextoAccion { Db = _db, Usuario = usuario, P = p, Hoy = hoy, Ip = ip, Desde = hoy, Hasta = hoy, RangoTexto = "hoy" };
            if (p.TryGetValue("desde", out var d) && DateTime.TryParse(d, out var dd)) c.Desde = dd.Date;
            if (p.TryGetValue("hasta", out var h) && DateTime.TryParse(h, out var hh)) c.Hasta = hh.Date;
            if (p.TryGetValue("rango", out var r) && !string.IsNullOrWhiteSpace(r)) c.RangoTexto = r;
            return c;
        }

        private string Proteger(int usuarioId, string accion, Dictionary<string, string> p)
        {
            var td = new TokenData
            {
                U = usuarioId, A = accion, P = new Dictionary<string, string>(p),
                N = Guid.NewGuid().ToString("N"), Exp = DateTime.UtcNow.AddMinutes(5).Ticks
            };
            return _dp.CreateProtector(Proposito).Protect(JsonSerializer.Serialize(td));
        }

        private static bool RellenarSlot(string slot, string mensaje, Dictionary<string, string> p, out string? error)
        {
            error = null;
            if (slot is "contado" or "fondo")
            {
                var n = AsistenteMotor.Normalizar(mensaje);
                decimal? v = AsistenteMotor.ExtraerNumero(mensaje);
                if (v == null && n is "no" or "nada" or "ninguno" or "ninguna" or "cero") v = 0;
                if (v == null || v < 0) { error = "Escribe solo el monto, por ejemplo 1500 o 1,250.50."; return false; }
                p[slot] = v.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
            var texto = mensaje.Trim();
            if (texto.Length > 300) texto = texto[..300];
            p[slot] = slot == "texto" ? AsistenteMotor.Normalizar(texto) : texto;
            return true;
        }

        private bool VerificarPassword(Usuario u, string password, out string error)
        {
            error = "";
            if (u.BloqueadoHasta.HasValue && u.BloqueadoHasta.Value > DateTime.Now)
            {
                error = "Usuario bloqueado temporalmente por intentos fallidos. Intenta más tarde.";
                return false;
            }
            bool ok;
            try { ok = BCrypt.Net.BCrypt.Verify(password, u.PasswordHash); } catch { ok = false; }
            if (ok)
            {
                if (u.IntentosFallidos > 0 || u.BloqueadoHasta != null)
                {
                    u.IntentosFallidos = 0; u.BloqueadoHasta = null; _db.SaveChanges();
                }
                return true;
            }
            u.IntentosFallidos++;
            if (u.IntentosFallidos >= MaxIntentos) { u.BloqueadoHasta = DateTime.Now.AddMinutes(10); u.IntentosFallidos = 0; }
            _db.SaveChanges();
            error = "Contraseña incorrecta.";
            return false;
        }

        private static readonly (string modulo, string permiso, string palabra, string icono)[] Modulos =
        {
            ("Ventas", "Ventas", "ventas", "🛒"), ("Inventario", "Inventario", "inventario", "📦"), ("Compras", "Compras", "compras", "🧾"),
            ("Finanzas", "Finanzas", "finanzas", "💰"), ("RRHH", "RRHH", "rrhh", "👥"), ("Logística", "Logistica", "logistica", "🚚"),
            ("Seguridad", "Usuarios", "seguridad", "🔐")
        };

        /// <summary>Opciones de un módulo: lo que puedo consultar o hacer ahí y enlaces a sus pantallas.</summary>
        private RespuestaAsistente MenuModulo(string modulo, Usuario u)
        {
            var acciones = AsistenteCatalogo.Todas
                .Where(a => a.Tipo != TipoAccion.Navegar && a.Modulo == modulo && a.Id != "ayuda" && PermitidaPara(a, u))
                .OrderBy(a => a.Titulo).ToList();
            var perm = Modulos.FirstOrDefault(m => m.modulo == modulo).permiso ?? modulo;
            var pantallas = AsistenteCatalogo.Todas
                .Where(a => a.Tipo == TipoAccion.Navegar && a.PermModulo == perm && !string.IsNullOrEmpty(a.Url) && PermitidaPara(a, u))
                .Take(10).ToList();

            if (acciones.Count == 0 && pantallas.Count == 0)
                return RespuestaAsistente.Texto($"Tu rol no tiene acceso a opciones de {modulo}.");

            var r = RespuestaAsistente.Elegir(
                $"En {modulo} puedo ayudarte con esto. Toca una opción o escríbeme lo que necesitas (por ejemplo: “{acciones.FirstOrDefault()?.Ejemplo ?? "ayuda"}”).",
                acciones.Select(a => new OpcionAsistente { Texto = a.Titulo, Envio = a.Ejemplo }));
            r.Titulo = modulo;
            if (pantallas.Count > 0)
                r.Enlaces = pantallas.Select(a => new EnlaceAsistente { Texto = a.Titulo.Replace("Abrir ", ""), Url = a.Url! }).ToList();
            return r;
        }

        /// <summary>Menú compacto de módulos (el navegador lo recorta según el tamaño de pantalla).</summary>
        private RespuestaAsistente MenuModulos(Usuario u)
        {
            var opciones = new List<OpcionAsistente>();
            foreach (var m in Modulos)
            {
                var tiene = AsistenteCatalogo.Todas.Any(a => a.Tipo != TipoAccion.Navegar && a.Modulo == m.modulo && a.Id != "ayuda" && PermitidaPara(a, u));
                if (tiene) opciones.Add(new OpcionAsistente { Texto = $"{m.icono} {m.modulo}", Envio = m.palabra });
            }
            opciones.Add(new OpcionAsistente { Texto = "cómo vamos hoy", Envio = "cómo vamos hoy" });
            opciones.Add(new OpcionAsistente { Texto = "ayuda (ver todo)", Envio = "ayuda" });
            return RespuestaAsistente.Elegir("Elige un módulo para ver qué puedo hacer ahí, o dime una palabra como “productos”, “clientes”, “empleados”, “proveedores” o “envíos”:", opciones);
        }

        /// <summary>No entendió: sugiere hasta 3 acciones parecidas; si no hay ninguna, muestra los módulos.</summary>
        private RespuestaAsistente NoEntendi(Usuario u, string? mensaje = null)
        {
            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                var traducido = AsistenteLenguaje.Traducir(AsistenteMotor.Normalizar(mensaje));
                var candidatas = AsistenteCatalogo.Todas.Where(a => a.Tipo != TipoAccion.Navegar && a.Id != "ayuda" && PermitidaPara(a, u));
                var sugeridas = AsistenteLenguaje.Sugerir(traducido, candidatas, 3);
                if (sugeridas.Count > 0)
                {
                    var opciones = sugeridas.Select(a => new OpcionAsistente { Texto = a.Titulo, Envio = a.Ejemplo }).ToList();
                    opciones.Add(new OpcionAsistente { Texto = "📂 Ver módulos", Envio = "módulos" });
                    return RespuestaAsistente.Elegir("No estoy seguro de lo que necesitas. ¿Quisiste decir alguna de estas?", opciones);
                }
            }
            var menu = MenuModulos(u);
            menu.Mensaje = "No estoy seguro de qué necesitas. " + menu.Mensaje;
            return menu;
        }

        private RespuestaAsistente Completar(RespuestaAsistente r)
        {
            if (r.Tipo == "tabla" && string.IsNullOrEmpty(r.Empresa))
                r.Empresa = _db.ConfiguracionEmpresa.AsNoTracking().Select(e => e.Nombre).FirstOrDefault() ?? "Mi Empresa";
            return r;
        }
    }
}
