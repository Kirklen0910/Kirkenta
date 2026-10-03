using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Kirkenta.Pages.Usuarios
{
    public class ActividadModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ActividadModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Usuario Usuario { get; set; } = new();
        public List<ActividadUsuario> Actividades { get; set; } = new();
        public string RolColor { get; set; } = "#6b7280";

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin")
            {
                var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(currentUserIdStr, out var currentUserId) || currentUserId != id)
                {
                    TempData["Error"] = "No tienes permiso para ver esta actividad";
                    return RedirectToPage("/Index");
                }
            }

            var user = _context.Usuarios.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToPage("/Usuarios/Index");
            }

            Usuario = user;
            RolColor = _context.Roles.FirstOrDefault(r => r.Nombre == user.Rol)?.Color ?? "#6b7280";

            Actividades = _context.Actividades
                .Where(a => a.UsuarioId == id)
                .OrderByDescending(a => a.Fecha)
                .Take(200)
                .ToList();

            return Page();
        }
    }
}