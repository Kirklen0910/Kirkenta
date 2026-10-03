using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Kirkenta.Pages.Usuarios
{
    public class RolesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RolesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Rol> Roles { get; set; } = new();
        public Dictionary<string, int> ConteoUsuarios { get; set; } = new();
        public Dictionary<int, int> ConteoPermisos { get; set; } = new();

        public IActionResult OnGet()
        {
            // 🔒 Solo Admin puede ver esta página
            var currentUsername = User.Identity?.Name;
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == currentUsername);

            if (currentUser == null || currentUser.Rol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            Roles = _context.Roles.OrderBy(r => r.Nombre).ToList();

            ConteoUsuarios = _context.Usuarios
                .GroupBy(u => u.Rol)
                .ToDictionary(g => g.Key, g => g.Count());

            ConteoPermisos = _context.Permisos
                .Where(p => p.PuedeVer)
                .GroupBy(p => p.RolId)
                .ToDictionary(g => g.Key, g => g.Count());

            return Page();
        }
    }
}