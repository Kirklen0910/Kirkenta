using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Kirkenta.Pages.Auth
{
    public class LogoutModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LogoutModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out var userId))
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                ActividadHelper.Registrar(
                    _context,
                    userId,
                    "Cierre de sesión",
                    "Salió del sistema",
                    ip);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Auth/Login");
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("/Auth/Login");
        }
    }
}