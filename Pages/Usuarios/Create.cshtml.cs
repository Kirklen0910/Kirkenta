using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Usuarios
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<Rol> Roles { get; set; } = new();

        public class InputModel
        {
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

            [Required(ErrorMessage = "La contraseña es obligatoria")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Confirma la contraseña")]
            [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
            public string ConfirmPassword { get; set; } = string.Empty;

            public bool Activo { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Create", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear usuarios";
                return RedirectToPage("/Usuarios/Index");
            }

            Roles = _context.Roles.Where(r => currentRol == "Admin" || r.Nombre != "Admin").OrderBy(r => r.Nombre).ToList();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Create", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear usuarios";
                return RedirectToPage("/Usuarios/Index");
            }

            Roles = _context.Roles.Where(r => currentRol == "Admin" || r.Nombre != "Admin").OrderBy(r => r.Nombre).ToList();

            if (!ModelState.IsValid)
                return Page();

            var errorClave = PasswordPolicy.Validar(Input.Password, Input.Username);
            if (errorClave != null)
            {
                ModelState.AddModelError("Input.Password", errorClave);
                return Page();
            }

            // Seguridad: el rol debe existir y solo un Admin puede crear otros Admin
            if (Input.Rol != "Pendiente" && !_context.Roles.Any(r => r.Nombre == Input.Rol))
            {
                ModelState.AddModelError("Input.Rol", "El rol seleccionado no existe");
                return Page();
            }

            if (Input.Rol == "Admin" && currentRol != "Admin")
            {
                ModelState.AddModelError("Input.Rol", "Solo un administrador puede crear usuarios con rol Admin");
                return Page();
            }

            if (_context.Usuarios.Any(u => u.Username == Input.Username))
            {
                ModelState.AddModelError("Input.Username", "Este usuario ya existe");
                return Page();
            }

            if (_context.Usuarios.Any(u => u.Email == Input.Email))
            {
                ModelState.AddModelError("Input.Email", "Este email ya está registrado");
                return Page();
            }

            var user = new Usuario
            {
                Username = Input.Username,
                Email = Input.Email,
                NombreCompleto = Input.NombreCompleto,
                Telefono = Input.Telefono,
                Rol = Input.Rol,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
                Activo = Input.Activo,
                FechaCreacion = DateTime.Now
            };

            _context.Usuarios.Add(user);
            _context.SaveChanges();

            if (currentUser != null)
            {
                ActividadHelper.Registrar(
                    _context,
                    currentUser.Id,
                    "Crear usuario",
                    $"Creó al usuario '{user.Username}' con rol '{user.Rol}'",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            TempData["Success"] = $"Usuario '{user.Username}' creado correctamente";
            return RedirectToPage("/Usuarios/Index");
        }
    }
}