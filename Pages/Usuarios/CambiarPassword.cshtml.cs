using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Kirkenta.Pages.Usuarios
{
    public class CambiarPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CambiarPasswordModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "La contraseña actual es obligatoria")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Confirma la nueva contraseña")]
            [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet() { }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToPage("/Auth/Login");

            var user = _context.Usuarios.FirstOrDefault(u => u.Id == userId);
            if (user == null) return RedirectToPage("/Auth/Login");

            if (!BCrypt.Net.BCrypt.Verify(Input.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("Input.CurrentPassword", "La contraseña actual es incorrecta");
                return Page();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.NewPassword);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                userId,
                "Cambiar contraseña",
                "Cambió su contraseña",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Contraseña cambiada correctamente";
            return RedirectToPage("/Usuarios/Perfil");
        }
    }
}