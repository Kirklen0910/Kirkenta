using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.RRHH.Alertas
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<AlertaItem> Alertas { get; set; } = new();
        public int TotalActivas { get; set; }
        public int TotalUrgentes { get; set; }
        public int TotalCompletadas { get; set; }

        public bool PuedeCrear { get; set; }

        public class AlertaItem
        {
            public int Id { get; set; }
            public string Titulo { get; set; } = "";
            public string? Descripcion { get; set; }
            public string Prioridad { get; set; } = "Info";
            public string? EmpleadoNombre { get; set; }
            public DateTime FechaAlerta { get; set; }
            public DateTime? FechaVigenciaHasta { get; set; }
            public bool Completada { get; set; }
            public DateTime? FechaCompletada { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "Alertas", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Index");
            }

            PuedeCrear = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "AlertasCreate", "crear");

            var empleadosDict = _context.Empleados.AsNoTracking().ToDictionary(e => e.Id, e => $"{e.Nombres} {e.Apellidos}");

            var lista = _context.AlertasPersonalizadas
                .AsNoTracking()
                .OrderByDescending(a => a.Completada)
                .ThenByDescending(a => a.FechaAlerta)
                .ToList();

            Alertas = lista.Select(a => new AlertaItem
            {
                Id = a.Id,
                Titulo = a.Titulo,
                Descripcion = a.Descripcion,
                Prioridad = a.Prioridad,
                EmpleadoNombre = a.EmpleadoId.HasValue ? empleadosDict.GetValueOrDefault(a.EmpleadoId.Value) : null,
                FechaAlerta = a.FechaAlerta,
                FechaVigenciaHasta = a.FechaVigenciaHasta,
                Completada = a.Completada,
                FechaCompletada = a.FechaCompletada
            }).ToList();

            TotalActivas = lista.Count(a => !a.Completada);
            TotalUrgentes = lista.Count(a => !a.Completada && a.Prioridad == "Urgente");
            TotalCompletadas = lista.Count(a => a.Completada);

            return Page();
        }

        public IActionResult OnPostCompletar(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "AlertasCreate", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Alertas/Index");
            }

            var alerta = _context.AlertasPersonalizadas.FirstOrDefault(a => a.Id == id);
            if (alerta == null)
            {
                TempData["Error"] = "Alerta no encontrada";
                return RedirectToPage("/RRHH/Alertas/Index");
            }

            alerta.Completada = true;
            alerta.FechaCompletada = DateTime.Now;
            _context.SaveChanges();

            TempData["Success"] = "Alerta marcada como completada";
            return RedirectToPage("/RRHH/Alertas/Index");
        }

        public IActionResult OnPostEliminar(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "AlertasCreate", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Alertas/Index");
            }

            var alerta = _context.AlertasPersonalizadas.FirstOrDefault(a => a.Id == id);
            if (alerta == null)
            {
                TempData["Error"] = "Alerta no encontrada";
                return RedirectToPage("/RRHH/Alertas/Index");
            }

            _context.AlertasPersonalizadas.Remove(alerta);
            _context.SaveChanges();

            TempData["Success"] = "Alerta eliminada";
            return RedirectToPage("/RRHH/Alertas/Index");
        }
    }
}