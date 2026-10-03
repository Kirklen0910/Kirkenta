using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Usuarios
{
    public class RolesDeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RolesDeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Rol Rol { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            // 🔒 Solo Admin
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var rol = _context.Roles.FirstOrDefault(r => r.Id == id);
            if (rol == null)
            {
                TempData["Error"] = "Rol no encontrado";
                return RedirectToPage("/Usuarios/Roles");
            }

            if (rol.EsSistema)
            {
                TempData["Error"] = "No se pueden eliminar roles del sistema";
                return RedirectToPage("/Usuarios/Roles");
            }

            if (_context.Usuarios.Any(u => u.Rol == rol.Nombre))
            {
                TempData["Error"] = "No se puede eliminar un rol que tiene usuarios asignados";
                return RedirectToPage("/Usuarios/Roles");
            }

            Rol = rol;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var rol = _context.Roles.FirstOrDefault(r => r.Id == id);
            if (rol == null)
            {
                TempData["Error"] = "Rol no encontrado";
                return RedirectToPage("/Usuarios/Roles");
            }

            if (rol.EsSistema)
            {
                TempData["Error"] = "No se pueden eliminar roles del sistema";
                return RedirectToPage("/Usuarios/Roles");
            }

            if (_context.Usuarios.Any(u => u.Rol == rol.Nombre))
            {
                TempData["Error"] = "No se puede eliminar un rol que tiene usuarios asignados";
                return RedirectToPage("/Usuarios/Roles");
            }

            var nombre = rol.Nombre;
            _context.Roles.Remove(rol);
            _context.SaveChanges();

            TempData["Success"] = $"Rol '{nombre}' eliminado correctamente";
            return RedirectToPage("/Usuarios/Roles");
        }
    }
}