using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kirkenta.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Inicio de sesión centralizado. Cada cookie lleva un "sello" derivado de la contraseña del usuario:
    /// si la contraseña cambia (o un administrador la restablece), todas las sesiones abiertas con la clave
    /// anterior dejan de valer de inmediato (por ejemplo, una sesión robada o un equipo olvidado abierto).
    /// </summary>
    public static class SesionHelper
    {
        public const string ClaimSello = "kk_sello";

        /// <summary>Huella corta (no reversible) de la contraseña actual.</summary>
        public static string CalcularSello(string? passwordHash)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("kirkenta-sello|" + (passwordHash ?? "")));
            return Convert.ToHexString(bytes, 0, 12);
        }

        public static ClaimsPrincipal CrearPrincipal(Usuario user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Rol ?? "User"),
                new Claim(ClaimSello, CalcularSello(user.PasswordHash))
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }

        public static Task IniciarSesionAsync(HttpContext http, Usuario user)
        {
            return http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                CrearPrincipal(user),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });
        }
    }
}
