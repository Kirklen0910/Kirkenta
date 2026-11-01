using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Kirkenta.Pages.Auth
{
    public class LoginModel : PageModel
    {
        // Un solo mensaje para todos los fallos (usuario inexistente, clave mala, cuenta bloqueada):
        // así un atacante no puede saber si un usuario existe ni si su cuenta está bloqueada.
        private const string MensajeCredencialesInvalidas =
            "Usuario o contraseña incorrectos. Si fallas varias veces, la cuenta se bloquea unos minutos.";

        // Hash falso: cuando el usuario no existe también se hace una verificación BCrypt,
        // así el tiempo de respuesta no delata si el usuario existe o no.
        private static readonly string HashFalso = BCrypt.Net.BCrypt.HashPassword("kirkenta-hash-falso");

        private readonly ApplicationDbContext _context;
        private readonly ILogger<LoginModel> _logger;
        private readonly int _maxIntentos;
        private readonly int _minutosBloqueo;

        public LoginModel(ApplicationDbContext context, IConfiguration configuration, ILogger<LoginModel> logger)
        {
            _context = context;
            _logger = logger;
            _maxIntentos = Math.Max(1, configuration.GetValue("Seguridad:MaxIntentosLogin", 5));
            _minutosBloqueo = Math.Max(1, configuration.GetValue("Seguridad:MinutosBloqueoLogin", 15));
            MostrarRegistro = configuration.GetValue("Seguridad:PermitirRegistroPublico", false);
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        /// <summary>Si es false, la vista oculta el enlace "Regístrate".</summary>
        public bool MostrarRegistro { get; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError(string.Empty, "Ingresa tu usuario y contraseña.");
                return Page();
            }

            // Límites de tamaño: evita enviar textos enormes para forzar al servidor a procesarlos
            if (Username.Length > 100 || Password.Length > 200)
            {
                await EsperaAntiFuerzaBrutaAsync();
                ModelState.AddModelError(string.Empty, MensajeCredencialesInvalidas);
                return Page();
            }

            var username = Username.Trim();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();
            var ahora = DateTime.Now;

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                // Verificación falsa para igualar tiempos de respuesta
                BCrypt.Net.BCrypt.Verify(Password, HashFalso);
                _logger.LogWarning("[Seguridad] Intento de inicio de sesión con un usuario que no existe, desde {Ip}", ip);
                await EsperaAntiFuerzaBrutaAsync();
                ModelState.AddModelError(string.Empty, MensajeCredencialesInvalidas);
                return Page();
            }

            // ----- Cuenta bloqueada temporalmente -----
            // Mientras dure el bloqueo no se verifica la contraseña, para que no se pueda
            // seguir probando claves.
            if (user.BloqueadoHasta.HasValue && user.BloqueadoHasta.Value > ahora)
            {
                // Se hace la misma verificación (lenta) que en un intento normal para que el tiempo de respuesta
                // no delate que la cuenta existe y está bloqueada.
                BCrypt.Net.BCrypt.Verify(Password, HashFalso);
                _logger.LogWarning("[Seguridad] Intento de inicio de sesión en una cuenta bloqueada, desde {Ip}", ip);
                await EsperaAntiFuerzaBrutaAsync();
                ModelState.AddModelError(string.Empty, MensajeCredencialesInvalidas);
                return Page();
            }

            // El bloqueo ya venció: se limpia para empezar a contar desde cero
            if (user.BloqueadoHasta.HasValue)
            {
                user.BloqueadoHasta = null;
                user.IntentosFallidos = 0;
            }

            // ----- Contraseña incorrecta -----
            if (!BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                user.IntentosFallidos++;
                var bloquear = user.IntentosFallidos >= _maxIntentos;

                if (bloquear)
                {
                    user.BloqueadoHasta = ahora.AddMinutes(_minutosBloqueo);
                }

                await _context.SaveChangesAsync();

                ActividadHelper.Registrar(
                    _context,
                    user.Id,
                    bloquear ? "Cuenta bloqueada" : "Login fallido",
                    bloquear
                        ? $"Bloqueada por {_minutosBloqueo} minutos tras {user.IntentosFallidos} intentos fallidos"
                        : $"Intento fallido #{user.IntentosFallidos}",
                    ip,
                    userAgent);

                if (bloquear)
                    _logger.LogWarning("[Seguridad] Cuenta bloqueada por {Min} minutos tras {N} intentos fallidos, desde {Ip}", _minutosBloqueo, _maxIntentos, ip);

                await EsperaAntiFuerzaBrutaAsync();
                ModelState.AddModelError(string.Empty, MensajeCredencialesInvalidas);
                return Page();
            }

            // ----- Contraseña correcta pero cuenta desactivada -----
            // Se revisa DESPUÉS de validar la contraseña: solo quien conoce la clave
            // llega a saber que la cuenta existe y está desactivada.
            if (!user.Activo)
            {
                ModelState.AddModelError(string.Empty, "Tu cuenta está desactivada. Contacta al administrador.");
                return Page();
            }

            // ✅ Login exitoso
            user.UltimoAcceso = ahora;
            user.IntentosFallidos = 0;
            user.BloqueadoHasta = null;
            await _context.SaveChangesAsync();

            // La cookie lleva un sello de la contraseña: si la clave cambia, esta sesión deja de valer
            await SesionHelper.IniciarSesionAsync(HttpContext, user);

            ActividadHelper.Registrar(
                _context,
                user.Id,
                "Inicio de sesión",
                "Ingresó al sistema",
                ip,
                userAgent);

            return RedirectToPage("/Index");
        }

        /// <summary>Pausa breve y aleatoria tras cada fallo: encarece los ataques de fuerza bruta.</summary>
        private static Task EsperaAntiFuerzaBrutaAsync() => Task.Delay(Random.Shared.Next(350, 800));
    }
}