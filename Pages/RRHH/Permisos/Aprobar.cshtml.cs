using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Permisos
{
    public class AprobarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AprobarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public PermisoEmpleado Permiso { get; set; } = new();
        public Empleado Empleado { get; set; } = new();

        [BindProperty]
        [Required]
        public string Accion { get; set; } = "";

        [BindProperty]
        [StringLength(500)]
        public string? Motivo { get; set; }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "PermisosAprobar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Permisos/Index");
            }

            var permiso = _context.PermisosEmpleado.FirstOrDefault(p => p.Id == id);
            if (permiso == null || permiso.Estado != "Solicitado")
            {
                TempData["Error"] = "Permiso no encontrado o ya procesado";
                return RedirectToPage("/RRHH/Permisos/Index");
            }

            Permiso = permiso;
            Empleado = _context.Empleados.FirstOrDefault(e => e.Id == permiso.EmpleadoId) ?? new Empleado();

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "PermisosAprobar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Permisos/Index");
            }

            var permiso = _context.PermisosEmpleado.FirstOrDefault(p => p.Id == id);
            if (permiso == null || permiso.Estado != "Solicitado")
            {
                TempData["Error"] = "El permiso no se puede procesar";
                return RedirectToPage("/RRHH/Permisos/Index");
            }

            Permiso = permiso;
            Empleado = _context.Empleados.FirstOrDefault(e => e.Id == permiso.EmpleadoId) ?? new Empleado();

            if (Accion != "aprobar" && Accion != "rechazar")
            {
                ModelState.AddModelError("Accion", "Selecciona una acción válida");
                return Page();
            }

            if (Accion == "rechazar" && string.IsNullOrWhiteSpace(Motivo))
            {
                ModelState.AddModelError("Motivo", "Debes indicar el motivo del rechazo");
                return Page();
            }

            if (Accion == "aprobar")
            {
                permiso.Estado = "Aprobado";
                permiso.UsuarioApruebaId = currentUser?.Id;
                permiso.FechaAprobacion = DateTime.Now;
            }
            else
            {
                permiso.Estado = "Rechazado";
                permiso.UsuarioApruebaId = currentUser?.Id;
                permiso.FechaAprobacion = DateTime.Now;
                permiso.MotivoRechazo = Motivo;
            }

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                Accion == "aprobar" ? "Aprobar permiso" : "Rechazar permiso",
                $"{Accion} permiso {permiso.Numero} de {Empleado.Nombres} {Empleado.Apellidos}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Permiso {permiso.Numero} {(Accion == "aprobar" ? "aprobado" : "rechazado")}";
            return RedirectToPage("/RRHH/Permisos/Index");
        }
    }
}