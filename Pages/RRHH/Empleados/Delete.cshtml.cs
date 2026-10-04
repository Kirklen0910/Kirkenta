using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.RRHH.Empleados
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Empleado Empleado { get; set; } = new();

        public int TieneNominas { get; set; }
        public int TieneVales { get; set; }
        public int TieneVacaciones { get; set; }
        public int TienePermisos { get; set; }
        public int TieneDocumentos { get; set; }
        public int TieneExpedientes { get; set; }

        public bool TieneDependencias => TieneNominas > 0 || TieneVales > 0 || TieneVacaciones > 0 ||
                                          TienePermisos > 0 || TieneDocumentos > 0 || TieneExpedientes > 0;

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "EmpleadosDelete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar empleados";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            var empleado = _context.Empleados.FirstOrDefault(e => e.Id == id);
            if (empleado == null)
            {
                TempData["Error"] = "Empleado no encontrado";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            Empleado = empleado;

            // Verificar dependencias
            TieneNominas = _context.DetalleNominas.Count(d => d.EmpleadoId == id);
            TieneVales = _context.ValesEmpleado.Count(v => v.EmpleadoId == id);
            TieneVacaciones = _context.VacacionesEmpleado.Count(v => v.EmpleadoId == id);
            TienePermisos = _context.PermisosEmpleado.Count(p => p.EmpleadoId == id);
            TieneDocumentos = _context.AdjuntosEmpleado.Count(a => a.EmpleadoId == id);
            TieneExpedientes = _context.ExpedientesEmpleado.Count(e => e.EmpleadoId == id);

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "EmpleadosDelete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            var empleado = _context.Empleados.FirstOrDefault(e => e.Id == id);
            if (empleado == null)
            {
                TempData["Error"] = "Empleado no encontrado";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            // Verificar dependencias
            if (_context.DetalleNominas.Any(d => d.EmpleadoId == id))
            {
                TempData["Error"] = "No se puede eliminar: tiene nóminas asociadas. Cámbialo a estado 'Baja' en su lugar.";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            if (_context.ValesEmpleado.Any(v => v.EmpleadoId == id))
            {
                TempData["Error"] = "No se puede eliminar: tiene vales asociados. Cámbialo a estado 'Baja' en su lugar.";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            if (_context.VacacionesEmpleado.Any(v => v.EmpleadoId == id))
            {
                TempData["Error"] = "No se puede eliminar: tiene vacaciones registradas. Cámbialo a estado 'Baja'.";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            if (_context.PermisosEmpleado.Any(p => p.EmpleadoId == id))
            {
                TempData["Error"] = "No se puede eliminar: tiene permisos registrados. Cámbialo a estado 'Baja'.";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            if (_context.AdjuntosEmpleado.Any(a => a.EmpleadoId == id))
            {
                TempData["Error"] = "No se puede eliminar: tiene documentos asociados. Elimínalos primero.";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            if (_context.ExpedientesEmpleado.Any(e => e.EmpleadoId == id))
            {
                TempData["Error"] = "No se puede eliminar: tiene expedientes registrados. Cámbialo a estado 'Baja'.";
                return RedirectToPage("/RRHH/Empleados/Index");
            }

            var codigo = empleado.Codigo;
            var nombre = $"{empleado.Nombres} {empleado.Apellidos}";

            _context.Empleados.Remove(empleado);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar empleado",
                $"Eliminó al empleado {codigo} — {nombre}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Empleado {codigo} eliminado";
            return RedirectToPage("/RRHH/Empleados/Index");
        }
    }
}