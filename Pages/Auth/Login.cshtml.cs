using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Kirkenta.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError(string.Empty, "Ingresa tu usuario y contraseña.");
                return Page();
            }

            var user = _context.Usuarios.FirstOrDefault(u => u.Username == Username);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
                return Page();
            }

            if (!user.Activo)
            {
                ModelState.AddModelError(string.Empty, "Tu cuenta está desactivada. Contacta al administrador.");
                return Page();
            }

            if (!BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                user.IntentosFallidos++;
                _context.SaveChanges();

                var ipFallido = HttpContext.Connection.RemoteIpAddress?.ToString();
                var uaFallido = Request.Headers["User-Agent"].ToString();

                ActividadHelper.Registrar(
                    _context,
                    user.Id,
                    "Login fallido",
                    $"Intento fallido #{user.IntentosFallidos}",
                    ipFallido,
                    uaFallido);

                ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
                return Page();
            }

            // ✅ Login exitoso
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            user.UltimoAcceso = DateTime.Now;
            user.IntentosFallidos = 0;
            _context.SaveChanges();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Rol ?? "User")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(8)
                });

            ActividadHelper.Registrar(
                _context,
                user.Id,
                "Inicio de sesión",
                "Ingresó al sistema",
                ip,
                userAgent);

            return RedirectToPage("/Index");
        }
    }
}