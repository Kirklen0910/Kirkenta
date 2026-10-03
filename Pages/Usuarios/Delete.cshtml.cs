using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Usuarios
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Usuario Usuario { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Delete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar usuarios";
                return RedirectToPage("/Usuarios/Index");
            }

            var user = _context.Usuarios.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToPage("/Usuarios/Index");
            }

            if (user.Username == User.Identity?.Name)
            {
                TempData["Error"] = "No puedes eliminar tu propio usuario";
                return RedirectToPage("/Usuarios/Index");
            }

            if (user.Rol == "Admin")
            {
                var adminCount = _context.Usuarios.Count(u => u.Rol == "Admin" && u.Activo);
                if (adminCount <= 1)
                {
                    TempData["Error"] = "No puedes eliminar al último administrador del sistema";
                    return RedirectToPage("/Usuarios/Index");
                }
            }

            Usuario = user;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Delete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar usuarios";
                return RedirectToPage("/Usuarios/Index");
            }

            var user = _context.Usuarios.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToPage("/Usuarios/Index");
            }

            if (user.Username == User.Identity?.Name)
            {
                TempData["Error"] = "No puedes eliminar tu propio usuario";
                return RedirectToPage("/Usuarios/Index");
            }

            if (user.Rol == "Admin")
            {
                var adminCount = _context.Usuarios.Count(u => u.Rol == "Admin" && u.Activo);
                if (adminCount <= 1)
                {
                    TempData["Error"] = "No puedes eliminar al último administrador del sistema";
                    return RedirectToPage("/Usuarios/Index");
                }
            }

            var username = user.Username;

            if (currentUser != null)
            {
                ActividadHelper.Registrar(
                    _context,
                    currentUser.Id,
                    "Eliminar usuario",
                    $"Eliminó al usuario '{username}'",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            _context.Usuarios.Remove(user);
            _context.SaveChanges();

            TempData["Success"] = $"Usuario '{username}' eliminado correctamente";
            return RedirectToPage("/Usuarios/Index");
        }
    }
}