using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Kirkenta.Pages.Usuarios
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<Rol> Roles { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El usuario es obligatorio")]
            [StringLength(50, MinimumLength = 3)]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "El email es obligatorio")]
            [EmailAddress(ErrorMessage = "Email inválido")]
            public string Email { get; set; } = string.Empty;

            [StringLength(100)]
            public string? NombreCompleto { get; set; }

            [StringLength(20)]
            public string? Telefono { get; set; }

            [Required(ErrorMessage = "El rol es obligatorio")]
            public string Rol { get; set; } = "Pendiente";

            public bool Activo { get; set; } = true;

            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
            public string? NewPassword { get; set; }

            [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden")]
            public string? ConfirmNewPassword { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Edit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar usuarios";
                return RedirectToPage("/Usuarios/Index");
            }

            Roles = _context.Roles.OrderBy(r => r.Nombre).ToList();

            var user = _context.Usuarios.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToPage("/Usuarios/Index");
            }

            Input = new InputModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                NombreCompleto = user.NombreCompleto,
                Telefono = user.Telefono,
                Rol = user.Rol,
                Activo = user.Activo
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Edit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar usuarios";
                return RedirectToPage("/Usuarios/Index");
            }

            Roles = _context.Roles.OrderBy(r => r.Nombre).ToList();

            if (!ModelState.IsValid)
                return Page();

            var user = _context.Usuarios.FirstOrDefault(u => u.Id == Input.Id);
            if (user == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToPage("/Usuarios/Index");
            }

            if (_context.Usuarios.Any(u => u.Username == Input.Username && u.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Username", "Este usuario ya existe");
                return Page();
            }

            if (_context.Usuarios.Any(u => u.Email == Input.Email && u.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Email", "Este email ya está registrado");
                return Page();
            }

            if (user.Rol == "Admin" && Input.Rol != "Admin")
            {
                var adminCount = _context.Usuarios.Count(u => u.Rol == "Admin" && u.Activo);
                if (adminCount <= 1)
                {
                    ModelState.AddModelError(string.Empty, "No puedes quitar el rol Admin al último administrador del sistema");
                    return Page();
                }
            }

            user.Username = Input.Username;
            user.Email = Input.Email;
            user.NombreCompleto = Input.NombreCompleto;
            user.Telefono = Input.Telefono;
            user.Rol = Input.Rol;
            user.Activo = Input.Activo;

            if (!string.IsNullOrWhiteSpace(Input.NewPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.NewPassword);
            }

            try
            {
                _context.SaveChanges();

                if (currentUser != null)
                {
                    var detalle = $"Editó al usuario '{user.Username}'";
                    if (!string.IsNullOrWhiteSpace(Input.NewPassword))
                        detalle += " (incluyendo contraseña)";

                    ActividadHelper.Registrar(
                        _context,
                        currentUser.Id,
                        "Editar usuario",
                        detalle,
                        HttpContext.Connection.RemoteIpAddress?.ToString());
                }

                TempData["Success"] = $"Usuario '{user.Username}' actualizado correctamente";
                return RedirectToPage("/Usuarios/Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al guardar: {ex.Message}");
                return Page();
            }
        }
    }
}