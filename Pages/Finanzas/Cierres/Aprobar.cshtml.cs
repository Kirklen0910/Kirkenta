using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Cierres
{
    public class AprobarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AprobarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public CierreCaja Cierre { get; set; } = new();
        public string CuentaNombre { get; set; } = "";
        public string UsuarioCierra { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Debes seleccionar una acción")]
        public string Accion { get; set; } = "";  // "aprobar" | "rechazar"

        [BindProperty]
        [StringLength(500)]
        public string? Motivo { get; set; }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresAprobar", "editar"))
            {
                TempData["Error"] = "No tienes permiso para aprobar cierres";
                return RedirectToPage("/Finanzas/Cierres/Index");
            }

            var cierre = _context.CierresCaja.FirstOrDefault(c => c.Id == id);
            if (cierre == null)
            {
                TempData["Error"] = "Cierre no encontrado";
                return RedirectToPage("/Finanzas/Cierres/Index");
            }

            if (cierre.EstadoActa != "Cerrado")
            {
                TempData["Error"] = $"Este cierre ya está {cierre.EstadoActa.ToLower()}";
                return RedirectToPage("/Finanzas/Cierres/Details", new { id });
            }

            Cierre = cierre;
            CuentaNombre = _context.CuentasFinancieras
                .FirstOrDefault(c => c.Id == cierre.CuentaId)?.Nombre ?? "—";
            UsuarioCierra = _context.Usuarios
                .FirstOrDefault(u => u.Id == cierre.UsuarioCierraId)?.Username ?? "—";

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresAprobar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Cierres/Index");
            }

            var cierre = _context.CierresCaja.FirstOrDefault(c => c.Id == id);
            if (cierre == null || cierre.EstadoActa != "Cerrado")
            {
                TempData["Error"] = "El cierre no se puede aprobar";
                return RedirectToPage("/Finanzas/Cierres/Index");
            }

            if (Accion != "aprobar" && Accion != "rechazar")
            {
                ModelState.AddModelError("Accion", "Selecciona una acción válida");
                Cierre = cierre;
                return Page();
            }

            if (Accion == "rechazar" && string.IsNullOrWhiteSpace(Motivo))
            {
                ModelState.AddModelError("Motivo", "Debes indicar el motivo del rechazo");
                Cierre = cierre;
                CuentaNombre = _context.CuentasFinancieras.FirstOrDefault(c => c.Id == cierre.CuentaId)?.Nombre ?? "—";
                UsuarioCierra = _context.Usuarios.FirstOrDefault(u => u.Id == cierre.UsuarioCierraId)?.Username ?? "—";
                return Page();
            }

            cierre.EstadoActa = Accion == "aprobar" ? "Aprobado" : "Rechazado";
            cierre.UsuarioApruebaId = currentUser?.Id;
            cierre.FechaAprobacion = DateTime.Now;
            cierre.MotivoRechazo = Accion == "rechazar" ? Motivo : null;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                Accion == "aprobar" ? "Aprobar cierre de caja" : "Rechazar cierre de caja",
                $"{Accion} el cierre {cierre.Numero}" + (Accion == "rechazar" ? $". Motivo: {Motivo}" : ""),
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cierre {cierre.Numero} {(Accion == "aprobar" ? "aprobado" : "rechazado")} correctamente";
            return RedirectToPage("/Finanzas/Cierres/Details", new { id });
        }
    }
}