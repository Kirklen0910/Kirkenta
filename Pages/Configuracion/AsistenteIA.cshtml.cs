using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Services.Asistente;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Configuracion
{
    /// <summary>Configuración de la IA opcional del asistente (Ollama local o proveedor compatible con OpenAI). Solo Admin.</summary>
    public class AsistenteIAModel : PageModel
    {
        private readonly AsistenteIAConfigStore _store;
        private readonly AsistenteIA _ia;
        private readonly ApplicationDbContext _db;

        public AsistenteIAModel(AsistenteIAConfigStore store, AsistenteIA ia, ApplicationDbContext db)
        {
            _store = store; _ia = ia; _db = db;
        }

        [BindProperty] public bool Habilitada { get; set; }
        [BindProperty] public string Endpoint { get; set; } = "";
        [BindProperty] public string Modelo { get; set; } = "";
        /// <summary>Vacío = conservar la clave guardada.</summary>
        [BindProperty] public string? ApiKey { get; set; }
        [BindProperty] public bool QuitarClave { get; set; }

        public bool TieneClave { get; set; }
        /// <summary>Estado realmente guardado (no lo que hay escrito en el formulario).</summary>
        public bool ActivaGuardada { get; set; }
        public string ModeloGuardado { get; set; } = "";
        public string? Diagnostico { get; set; }
        public string? Resultado { get; set; }
        public bool ResultadoOk { get; set; }
        public List<string> ModelosDisponibles { get; set; } = new();

        private async Task<Models.Usuario?> UsuarioActualAsync()
        {
            var nombre = User.Identity?.Name?.Trim();
            if (string.IsNullOrEmpty(nombre)) return null;
            return await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Username == nombre);
        }

        private async Task<bool> EsAdminAsync()
        {
            var u = await UsuarioActualAsync();
            return u != null && PermisoHelper.UsuarioTieneAcceso(_db, u.Username, "Configuracion", "AsistenteIA");
        }

        private IActionResult SinPermiso()
        {
            TempData["Error"] = "Solo un administrador puede configurar la IA del asistente.";
            return Redirect("/");
        }

        public override void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context)
        {
            var g = _store.Obtener();
            ActivaGuardada = g.Habilitada && !string.IsNullOrWhiteSpace(g.Url) && !string.IsNullOrWhiteSpace(g.Modelo);
            ModeloGuardado = g.Modelo;
            Diagnostico = _store.UltimoDiagnostico == null ? null : $"{_store.UltimoDiagnosticoFecha:dd/MM HH:mm} — {_store.UltimoDiagnostico}";
            base.OnPageHandlerExecuting(context);
        }

        private void Cargar()
        {
            var c = _store.Obtener();
            Habilitada = c.Habilitada; Endpoint = c.Url; Modelo = c.Modelo; TieneClave = !string.IsNullOrEmpty(c.ApiKey);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!await EsAdminAsync()) return SinPermiso();
            Cargar();
            return Page();
        }

        private AsistenteIAConfig ArmarConfig()
        {
            var actual = _store.Obtener();
            var clave = QuitarClave ? "" : (string.IsNullOrWhiteSpace(ApiKey) ? actual.ApiKey : ApiKey!.Trim());
            return new AsistenteIAConfig { Habilitada = Habilitada, Url = (Endpoint ?? "").Trim(), Modelo = (Modelo ?? "").Trim(), ApiKey = clave };
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            var u = await UsuarioActualAsync();
            if (u == null || !PermisoHelper.UsuarioTieneAcceso(_db, u.Username, "Configuracion", "AsistenteIA")) return SinPermiso();

            var cfg = ArmarConfig();
            if (cfg.Habilitada && (string.IsNullOrWhiteSpace(cfg.Url) || string.IsNullOrWhiteSpace(cfg.Modelo)))
            {
                Resultado = "Para activar la IA indica la dirección (URL) y el modelo.";
                ResultadoOk = false; TieneClave = !string.IsNullOrEmpty(_store.Obtener().ApiKey);
                return Page();
            }
            if (cfg.Habilitada && string.IsNullOrWhiteSpace(cfg.ApiKey)
                && Uri.TryCreate(cfg.Url, UriKind.Absolute, out var uriClave)
                && uriClave.Host != "localhost" && uriClave.Host != "127.0.0.1" && uriClave.Host != "::1" && uriClave.Host != "[::1]")
            {
                Resultado = "Falta la clave de API: este proveedor la exige. Después de “Probar conexión” el campo queda vacío, así que pégala otra vez antes de pulsar Guardar.";
                ResultadoOk = false; TieneClave = !string.IsNullOrEmpty(_store.Obtener().ApiKey);
                return Page();
            }
            if (cfg.Url.Length > 0 && !(Uri.TryCreate(cfg.Url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https")))
            {
                Resultado = "La dirección debe empezar con http:// o https://";
                ResultadoOk = false; TieneClave = !string.IsNullOrEmpty(_store.Obtener().ApiKey);
                return Page();
            }

            _store.Guardar(cfg);
            ActividadHelper.Registrar(_db, u.Id, "Asistente: configuración de IA",
                cfg.Habilitada ? $"IA activada (modelo {cfg.Modelo})" : "IA desactivada", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = cfg.Habilitada ? "Guardado. La IA del asistente quedó activa." : "Guardado. La IA quedó apagada; el asistente sigue con reglas.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostModelosAsync()
        {
            if (!await EsAdminAsync()) return SinPermiso();
            var (ok, msg, ids) = await _ia.ListarModelosAsync(ArmarConfig());
            Resultado = msg; ResultadoOk = ok; ModelosDisponibles = ids;
            TieneClave = !string.IsNullOrEmpty(_store.Obtener().ApiKey);
            return Page();
        }

        public async Task<IActionResult> OnPostProbarAsync()
        {
            if (!await EsAdminAsync()) return SinPermiso();
            var (ok, msg) = await _ia.ProbarAsync(ArmarConfig());
            Resultado = msg; ResultadoOk = ok;
            TieneClave = !string.IsNullOrEmpty(_store.Obtener().ApiKey);
            return Page();
        }
    }
}
