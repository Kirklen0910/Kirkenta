using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Nomina
{
    public class AprobarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AprobarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Models.Nomina Nomina { get; set; } = new();

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
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "NominaAprobar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Nomina/Index");
            }

            var nomina = _context.Nominas.FirstOrDefault(n => n.Id == id);
            if (nomina == null || nomina.Estado != "Calculada")
            {
                TempData["Error"] = "Nómina no encontrada o ya procesada";
                return RedirectToPage("/RRHH/Nomina/Index");
            }

            Nomina = nomina;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "NominaAprobar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Nomina/Index");
            }

            var nomina = _context.Nominas.FirstOrDefault(n => n.Id == id);
            if (nomina == null || nomina.Estado != "Calculada")
            {
                TempData["Error"] = "La nómina no se puede procesar";
                return RedirectToPage("/RRHH/Nomina/Index");
            }

            Nomina = nomina;

            if (Accion != "aprobar" && Accion != "rechazar")
            {
                ModelState.AddModelError("Accion", "Selecciona una acción válida");
                return Page();
            }

            if (Accion == "aprobar")
            {
                nomina.Estado = "Aprobada";
                nomina.UsuarioApruebaId = currentUser?.Id;
                nomina.FechaAprobacion = DateTime.Now;
            }
            else
            {
                nomina.Estado = "Anulada";
                nomina.Notas = (nomina.Notas ?? "") + $"\n[Rechazada {DateTime.Now:dd/MM HH:mm}] {Motivo}";
            }

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                Accion == "aprobar" ? "Aprobar nómina" : "Rechazar nómina",
                $"{Accion} nómina {nomina.Numero} por L. {nomina.TotalNeto:N2}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Nómina {nomina.Numero} {(Accion == "aprobar" ? "aprobada" : "rechazada")}";
            return RedirectToPage("/RRHH/Nomina/Details", new { id = nomina.Id });
        }
    }
}