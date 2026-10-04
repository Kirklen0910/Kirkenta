using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Vales
{
    public class AprobarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AprobarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public ValeEmpleado Vale { get; set; } = new();
        public Empleado Empleado { get; set; } = new();

        [BindProperty]
        [Required(ErrorMessage = "Debes seleccionar una acción")]
        public string Accion { get; set; } = "";

        [BindProperty]
        [StringLength(500)]
        public string? Motivo { get; set; }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ValesAprobar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Vales/Index");
            }

            var vale = _context.ValesEmpleado.FirstOrDefault(v => v.Id == id);
            if (vale == null)
            {
                TempData["Error"] = "Vale no encontrado";
                return RedirectToPage("/RRHH/Vales/Index");
            }

            if (vale.Estado != "Solicitado" && vale.Estado != "AprobadoGerente")
            {
                TempData["Error"] = "Este vale ya fue procesado";
                return RedirectToPage("/RRHH/Vales/Index");
            }

            Vale = vale;
            Empleado = _context.Empleados.FirstOrDefault(e => e.Id == vale.EmpleadoId) ?? new Empleado();

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ValesAprobar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Vales/Index");
            }

            var vale = _context.ValesEmpleado.FirstOrDefault(v => v.Id == id);
            if (vale == null || (vale.Estado != "Solicitado" && vale.Estado != "AprobadoGerente"))
            {
                TempData["Error"] = "El vale no se puede procesar";
                return RedirectToPage("/RRHH/Vales/Index");
            }

            if (Accion != "aprobar" && Accion != "rechazar")
            {
                ModelState.AddModelError("Accion", "Selecciona una acción válida");
                Vale = vale;
                Empleado = _context.Empleados.FirstOrDefault(e => e.Id == vale.EmpleadoId) ?? new Empleado();
                return Page();
            }

            if (Accion == "rechazar" && string.IsNullOrWhiteSpace(Motivo))
            {
                ModelState.AddModelError("Motivo", "Debes indicar el motivo del rechazo");
                Vale = vale;
                Empleado = _context.Empleados.FirstOrDefault(e => e.Id == vale.EmpleadoId) ?? new Empleado();
                return Page();
            }

            if (Accion == "aprobar")
            {
                // Flujo de doble aprobación
                if (vale.Estado == "Solicitado")
                {
                    vale.Estado = "AprobadoGerente";
                    vale.AprobadoPorGerenteId = currentUser?.Id;
                    vale.FechaAprobacionGerente = DateTime.Now;
                }
                else if (vale.Estado == "AprobadoGerente")
                {
                    vale.Estado = "AprobadoRRHH";
                    vale.AprobadoPorRRHHId = currentUser?.Id;
                    vale.FechaAprobacionRRHH = DateTime.Now;
                }
            }
            else
            {
                vale.Estado = "Rechazado";
                vale.MotivoRechazo = Motivo;
            }

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                Accion == "aprobar" ? "Aprobar vale" : "Rechazar vale",
                $"{Accion} vale {vale.Numero} por L. {vale.Monto:N2}" + (Accion == "rechazar" ? $". Motivo: {Motivo}" : ""),
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Vale {vale.Numero} {(Accion == "aprobar" ? $"aprobado ({vale.Estado})" : "rechazado")}";
            return RedirectToPage("/RRHH/Vales/Index");
        }
    }
}