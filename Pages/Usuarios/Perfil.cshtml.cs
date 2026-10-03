using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Kirkenta.Pages.Usuarios
{
    public class PerfilModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PerfilModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Usuario Usuario { get; set; } = new();
        public string RolColor { get; set; } = "#6b7280";

        public class InputModel
        {
            [Required]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "El email es obligatorio")]
            [EmailAddress(ErrorMessage = "Email inválido")]
            public string Email { get; set; } = string.Empty;

            [StringLength(100)]
            public string? NombreCompleto { get; set; }

            [StringLength(20)]
            public string? Telefono { get; set; }
        }

        private Usuario? GetCurrentUser()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId)) return null;
            return _context.Usuarios.FirstOrDefault(u => u.Id == userId);
        }

        public IActionResult OnGet()
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToPage("/Auth/Login");

            Usuario = user;
            RolColor = _context.Roles.FirstOrDefault(r => r.Nombre == user.Rol)?.Color ?? "#6b7280";

            Input = new InputModel
            {
                Username = user.Username,
                Email = user.Email,
                NombreCompleto = user.NombreCompleto,
                Telefono = user.Telefono
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToPage("/Auth/Login");

            Usuario = user;
            RolColor = _context.Roles.FirstOrDefault(r => r.Nombre == user.Rol)?.Color ?? "#6b7280";

            if (!ModelState.IsValid) return Page();

            if (_context.Usuarios.Any(u => u.Email == Input.Email && u.Id != user.Id))
            {
                ModelState.AddModelError("Input.Email", "Este email ya está en uso");
                return Page();
            }

            user.Email = Input.Email;
            user.NombreCompleto = Input.NombreCompleto;
            user.Telefono = Input.Telefono;
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                user.Id,
                "Editar perfil",
                "Actualizó su información personal",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Perfil actualizado correctamente";
            return RedirectToPage("/Usuarios/Perfil");
        }
    }
}