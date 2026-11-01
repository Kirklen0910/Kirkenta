using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Verificación de permisos por rol.
    ///
    /// CACHE EN MEMORIA:
    /// Antes, cada página hacía 2-3 consultas a la base de datos solo para saber si el
    /// usuario podía entrar. Ahora los permisos de cada rol y los datos básicos del
    /// usuario (rol y si está activo) se guardan en memoria:
    ///   - Permisos de un rol: hasta 5 minutos.
    ///   - Datos del usuario: hasta 60 segundos.
    /// El cache se invalida SOLO y de inmediato cuando se guarda cualquier cambio en
    /// Roles, Permisos o en el rol/activo/username de un Usuario
    /// (ver ApplicationDbContext.SaveChanges), así que los cambios hechos desde
    /// "Usuarios > Roles" se aplican al instante. Los tiempos son solo una red de seguridad.
    ///
    /// Todos los métodos públicos conservan la misma firma que antes.
    /// </summary>
    public static class PermisoHelper
    {
        private static readonly TimeSpan DuracionCacheRol = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DuracionCacheUsuario = TimeSpan.FromSeconds(60);
        private const int MaxUsuariosEnCache = 5000;

        // Se incrementa cada vez que se invalida el cache. Una entrada cargada con una
        // versión anterior se considera vencida (evita guardar datos viejos por una carrera).
        private static long _version;

        private sealed record Flags(bool Ver, bool Crear, bool Editar, bool Eliminar)
        {
            public bool Alguno => Ver || Crear || Editar || Eliminar;
        }

        private sealed record FilaPermiso(string Modulo, string? Submodulo, Flags Flags);

        private sealed class RolEnCache
        {
            public bool Existe { get; set; }
            public List<FilaPermiso> Filas { get; set; } = new();
            public Dictionary<string, Flags> PorClave { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            public long Version { get; set; }
            public DateTime ExpiraUtc { get; set; }
        }

        private sealed record UsuarioEnCache(bool Existe, string Rol, bool Activo, string Sello, long Version, DateTime ExpiraUtc);

        private static readonly ConcurrentDictionary<string, RolEnCache> _roles =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, UsuarioEnCache> _usuarios =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Vacía el cache de permisos y de usuarios. Se llama automáticamente desde
        /// ApplicationDbContext al guardar cambios de seguridad; también se puede
        /// llamar a mano si hiciera falta.
        /// </summary>
        public static void InvalidarCache()
        {
            Interlocked.Increment(ref _version);
            _roles.Clear();
            _usuarios.Clear();
        }

        /// <summary>
        /// Verifica si un rol tiene un permiso específico sobre un módulo/submódulo.
        /// </summary>
        /// <param name="accion">"ver" | "crear" | "editar" | "eliminar"</param>
        public static bool TienePermiso(
            ApplicationDbContext context,
            string rolNombre,
            string modulo,
            string? submodulo,
            string accion)
        {
            // Admin siempre tiene todos los permisos
            if (rolNombre == "Admin")
                return true;

            if (string.IsNullOrEmpty(rolNombre))
                return false;

            var rol = RolDesdeCache(context, rolNombre);
            if (!rol.Existe)
                return false;

            if (!rol.PorClave.TryGetValue(Clave(modulo, submodulo), out var permiso))
                return false;

            return accion.ToLowerInvariant() switch
            {
                "ver" => permiso.Ver,
                "crear" => permiso.Crear,
                "editar" => permiso.Editar,
                "eliminar" => permiso.Eliminar,
                _ => false
            };
        }

        /// <summary>
        /// Igual que TienePermiso, pero a partir del nombre de usuario (User.Identity?.Name).
        /// Busca el rol actual del usuario. Si el usuario no existe,
        /// se trata como "Pendiente" (sin permisos). Pensado para proteger páginas y handlers.
        /// </summary>
        public static bool UsuarioTienePermiso(
            ApplicationDbContext context,
            string? username,
            string modulo,
            string? submodulo,
            string accion)
        {
            var rolNombre = ObtenerRol(context, username);
            return TienePermiso(context, rolNombre, modulo, submodulo, accion);
        }

        /// <summary>
        /// Verifica si el usuario tiene ALGÚN permiso (ver, crear, editar o eliminar) sobre
        /// un submódulo. Es la misma regla que usa el menú lateral, y sirve para proteger
        /// páginas de consulta (listados, detalles, reportes). Admin siempre tiene acceso.
        /// </summary>
        public static bool UsuarioTieneAcceso(
            ApplicationDbContext context,
            string? username,
            string modulo,
            string submodulo)
        {
            var rolNombre = ObtenerRol(context, username);
            if (rolNombre == "Admin")
                return true;

            if (string.IsNullOrEmpty(rolNombre))
                return false;

            var rol = RolDesdeCache(context, rolNombre);
            if (!rol.Existe)
                return false;

            return rol.Filas.Any(f =>
                string.Equals(f.Modulo, modulo, StringComparison.OrdinalIgnoreCase)
                && string.Equals(f.Submodulo, submodulo, StringComparison.OrdinalIgnoreCase)
                && f.Flags.Alguno);
        }

        /// <summary>
        /// Indica si el usuario existe y está activo. Se usa al validar la cookie de
        /// sesión para que un usuario desactivado pierda el acceso de inmediato
        /// (antes conservaba la sesión hasta 8 horas).
        /// </summary>
        public static bool UsuarioActivo(ApplicationDbContext context, string? username)
        {
            if (string.IsNullOrEmpty(username))
                return false;

            var usuario = UsuarioDesdeCache(context, username);
            return usuario.Existe && usuario.Activo;
        }

        /// <summary>
        /// Indica si el sello de la cookie coincide con el de la contraseña actual del usuario.
        /// Si la contraseña cambió después de iniciar esa sesión, devuelve false y la sesión se cierra.
        /// </summary>
        public static bool SesionVigente(ApplicationDbContext context, string? username, string? sello)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(sello))
                return false;

            var usuario = UsuarioDesdeCache(context, username);
            if (!usuario.Existe || usuario.Sello.Length == 0)
                return false;

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(usuario.Sello),
                Encoding.UTF8.GetBytes(sello));
        }

        /// <summary>
        /// Devuelve la lista de módulos que el rol puede ver en el menú.
        /// Un módulo aparece si el rol tiene "Ver" en alguno de sus submódulos
        /// (o algún permiso en las páginas de acción, ver CuentaParaMenu).
        /// </summary>
        public static List<string> ModulosVisibles(ApplicationDbContext context, string rolNombre)
        {
            // Admin ve todos los módulos definidos en ModulosERP
            if (rolNombre == "Admin")
            {
                return ModulosERP.Modulos.Keys.ToList();
            }

            if (string.IsNullOrEmpty(rolNombre))
                return new List<string>();

            var rol = RolDesdeCache(context, rolNombre);
            if (!rol.Existe)
                return new List<string>();

            return rol.Filas
                .Where(f => f.Submodulo != null && CuentaParaMenu(f))
                .Select(f => f.Modulo)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Devuelve la lista de submódulos que el rol puede ver en el menú para un módulo.
        /// Un submódulo aparece si el rol tiene "Ver" sobre él (o algún permiso, si es una
        /// página de acción). Lista vacía = sin submódulos (Admin se resuelve aparte en el layout).
        /// </summary>
        public static List<string> SubmodulosVisibles(
            ApplicationDbContext context,
            string rolNombre,
            string modulo)
        {
            if (rolNombre == "Admin")
            {
                return new List<string>(); // Lista vacía = mostrar todo
            }

            if (string.IsNullOrEmpty(rolNombre))
                return new List<string>();

            var rol = RolDesdeCache(context, rolNombre);
            if (!rol.Existe)
                return new List<string>();

            return rol.Filas
                .Where(f => f.Submodulo != null
                    && string.Equals(f.Modulo, modulo, StringComparison.OrdinalIgnoreCase)
                    && CuentaParaMenu(f))
                .Select(f => f.Submodulo!)
                .ToList();
        }

        // ============================================================
        // MENÚ: un enlace aparece solo si el rol tiene "Ver" en ese submódulo.
        // (Antes aparecía con CUALQUIER permiso, y quien solo tenía "Crear" veía
        // enlaces a pantallas que luego lo rechazaban.)
        // Excepción: estas filas no son listados sino acciones o páginas propias del
        // usuario, así que cuentan con cualquier permiso marcado.
        // ============================================================
        private static readonly HashSet<string> FilasDeAccion = new(StringComparer.OrdinalIgnoreCase)
        {
            "Ventas|Create",            // Punto de venta
            "Usuarios|Create",          // Nuevo usuario
            "Usuarios|Perfil",          // Mi perfil
            "Usuarios|CambiarPassword", // Cambiar contraseña
            "Usuarios|Actividad"        // Mi actividad
        };

        private static bool CuentaParaMenu(FilaPermiso fila)
        {
            if (fila.Flags.Ver) return true;
            return fila.Submodulo != null
                && FilasDeAccion.Contains(fila.Modulo + "|" + fila.Submodulo)
                && fila.Flags.Alguno;
        }

        // ============================================================
        // INTERNOS
        // ============================================================

        private static string Clave(string modulo, string? submodulo)
        {
            return modulo + "|" + (submodulo ?? string.Empty);
        }

        private static string ObtenerRol(ApplicationDbContext context, string? username)
        {
            if (string.IsNullOrEmpty(username))
                return "Pendiente";

            var usuario = UsuarioDesdeCache(context, username);
            return usuario.Existe ? usuario.Rol : "Pendiente";
        }

        private static RolEnCache RolDesdeCache(ApplicationDbContext context, string rolNombre)
        {
            var version = Interlocked.Read(ref _version);

            if (_roles.TryGetValue(rolNombre, out var enCache)
                && enCache.Version == version
                && enCache.ExpiraUtc > DateTime.UtcNow)
            {
                return enCache;
            }

            var rol = context.Roles
                .AsNoTracking()
                .FirstOrDefault(r => r.Nombre == rolNombre);

            var nuevo = new RolEnCache
            {
                Existe = rol != null,
                Version = version,
                ExpiraUtc = DateTime.UtcNow + DuracionCacheRol
            };

            if (rol != null)
            {
                var permisos = context.Permisos
                    .AsNoTracking()
                    .Where(p => p.RolId == rol.Id)
                    .OrderBy(p => p.Id)
                    .ToList();

                foreach (var p in permisos)
                {
                    var flags = new Flags(p.PuedeVer, p.PuedeCrear, p.PuedeEditar, p.PuedeEliminar);
                    nuevo.Filas.Add(new FilaPermiso(p.Modulo, p.Submodulo, flags));
                    nuevo.PorClave.TryAdd(Clave(p.Modulo, p.Submodulo), flags);
                }
            }

            _roles[rolNombre] = nuevo;
            return nuevo;
        }

        private static UsuarioEnCache UsuarioDesdeCache(ApplicationDbContext context, string username)
        {
            var version = Interlocked.Read(ref _version);

            if (_usuarios.TryGetValue(username, out var enCache)
                && enCache.Version == version
                && enCache.ExpiraUtc > DateTime.UtcNow)
            {
                return enCache;
            }

            var datos = context.Usuarios
                .AsNoTracking()
                .Where(u => u.Username == username)
                .Select(u => new { u.Rol, u.Activo, u.PasswordHash })
                .FirstOrDefault();

            var nuevo = new UsuarioEnCache(
                Existe: datos != null,
                Rol: datos?.Rol ?? "Pendiente",
                Activo: datos?.Activo ?? false,
                Sello: datos != null ? SesionHelper.CalcularSello(datos.PasswordHash) : "",
                Version: version,
                ExpiraUtc: DateTime.UtcNow + DuracionCacheUsuario);

            // Los nombres vienen de la cookie de un usuario autenticado, así que el tamaño
            // está acotado; este tope es solo una protección extra.
            if (_usuarios.Count >= MaxUsuariosEnCache)
                _usuarios.Clear();

            _usuarios[username] = nuevo;
            return nuevo;
        }
    }
}
