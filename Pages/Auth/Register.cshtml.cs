using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegisterModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        public void OnGet() { }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Username) || Username.Length < 3)
            {
                ModelState.AddModelError(string.Empty, "El usuario debe tener al menos 3 caracteres");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ModelState.AddModelError(string.Empty, "El email es obligatorio");
                return Page();
            }

            if (Password != ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Las contraseñas no coinciden");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            {
                ModelState.AddModelError(string.Empty, "La contraseña debe tener al menos 6 caracteres");
                return Page();
            }

            if (_context.Usuarios.Any(u => u.Username == Username))
            {
                ModelState.AddModelError(string.Empty, "El usuario ya existe");
                return Page();
            }

            if (_context.Usuarios.Any(u => u.Email == Email))
            {
                ModelState.AddModelError(string.Empty, "El email ya está registrado");
                return Page();
            }

            var user = new Usuario
            {
                Username = Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                Rol = "Pendiente",
                Email = Email,
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            _context.Usuarios.Add(user);
            _context.SaveChanges();

            TempData["RegisterMessage"] = "Registro exitoso ✅ Espera a que un administrador te asigne un rol.";
            return RedirectToPage("/Auth/Login");
        }
    }
}