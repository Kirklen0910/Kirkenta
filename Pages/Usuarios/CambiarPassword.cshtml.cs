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
        private readonly IWebHostEnvironment _env;

        public CambiarPasswordModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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

        public async Task<IActionResult> OnPostAsync()
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

            var errorClave = PasswordPolicy.Validar(Input.NewPassword, user.Username);
            if (errorClave != null)
            {
                ModelState.AddModelError("Input.NewPassword", errorClave);
                return Page();
            }
            if (Input.NewPassword == Input.CurrentPassword)
            {
                ModelState.AddModelError("Input.NewPassword", "La nueva contraseña debe ser distinta de la actual");
                return Page();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.NewPassword);
            _context.SaveChanges();

            // Las demás sesiones abiertas con la clave anterior dejan de valer; esta se renueva con la clave nueva
            await SesionHelper.IniciarSesionAsync(HttpContext, user);

            // Si es el administrador, ya cambió la clave inicial: se borra el archivo con la clave de primer acceso
            if (user.Rol == "Admin")
            {
                try
                {
                    var primerAcceso = Path.Combine(_env.ContentRootPath, "App_Data", "primer-acceso.txt");
                    if (System.IO.File.Exists(primerAcceso)) System.IO.File.Delete(primerAcceso);
                }
                catch { /* si no se puede borrar, no impide el cambio de clave */ }
            }

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