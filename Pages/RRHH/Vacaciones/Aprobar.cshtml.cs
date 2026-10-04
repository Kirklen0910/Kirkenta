using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Vacaciones
{
    public class AprobarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AprobarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public VacacionEmpleado Vacacion { get; set; } = new();
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
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "VacacionesAprobar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Vacaciones/Index");
            }

            var vacacion = _context.VacacionesEmpleado.FirstOrDefault(v => v.Id == id);
            if (vacacion == null)
            {
                TempData["Error"] = "Vacación no encontrada";
                return RedirectToPage("/RRHH/Vacaciones/Index");
            }

            if (vacacion.Estado != "Solicitado")
            {
                TempData["Error"] = "Esta solicitud ya fue procesada";
                return RedirectToPage("/RRHH/Vacaciones/Index");
            }

            Vacacion = vacacion;
            Empleado = _context.Empleados.FirstOrDefault(e => e.Id == vacacion.EmpleadoId) ?? new Empleado();

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "VacacionesAprobar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Vacaciones/Index");
            }

            var vacacion = _context.VacacionesEmpleado.FirstOrDefault(v => v.Id == id);
            if (vacacion == null || vacacion.Estado != "Solicitado")
            {
                TempData["Error"] = "La vacación no se puede procesar";
                return RedirectToPage("/RRHH/Vacaciones/Index");
            }

            Vacacion = vacacion;
            Empleado = _context.Empleados.FirstOrDefault(e => e.Id == vacacion.EmpleadoId) ?? new Empleado();

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
                var (ok, error) = VacacionHelper.Aprobar(_context, id, currentUser!.Id);
                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, error ?? "Error al aprobar");
                    return Page();
                }
            }
            else
            {
                var (ok, error) = VacacionHelper.Rechazar(_context, id, currentUser!.Id, Motivo!);
                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, error ?? "Error al rechazar");
                    return Page();
                }
            }

            ActividadHelper.Registrar(
                _context,
                currentUser.Id,
                Accion == "aprobar" ? "Aprobar vacaciones" : "Rechazar vacaciones",
                $"{Accion} solicitud {vacacion.Numero} de {Empleado.Nombres} {Empleado.Apellidos}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Solicitud {vacacion.Numero} {(Accion == "aprobar" ? "aprobada" : "rechazada")}";
            return RedirectToPage("/RRHH/Vacaciones/Index");
        }
    }
}