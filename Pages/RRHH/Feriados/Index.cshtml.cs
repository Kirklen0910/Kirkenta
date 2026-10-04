using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.RRHH.Feriados
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Feriado> Feriados { get; set; } = new();
        public List<string> PaisesDisponibles { get; set; } = new();
        public string PaisEmpresa { get; set; } = "HN";

        public int TotalEsteAnio { get; set; }
        public int TotalProximos { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }

        public IActionResult OnGet(int? anio, string? pais)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "Feriados", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Index");
            }

            PuedeCrear = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "FeriadosCreate", "crear");
            PuedeEditar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "FeriadosCreate", "editar");
            PuedeEliminar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "FeriadosCreate", "eliminar");

            var config = _context.ConfiguracionEmpresa.FirstOrDefault();
            PaisEmpresa = config?.PaisCodigo ?? "HN";

            PaisesDisponibles = _context.Feriados
                .Select(f => f.PaisCodigo)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            var anioFiltro = anio ?? DateTime.Today.Year;
            var paisFiltro = pais ?? PaisEmpresa;

            Feriados = _context.Feriados
                .Where(f => f.PaisCodigo == paisFiltro && f.Fecha.Year == anioFiltro)
                .OrderBy(f => f.Fecha)
                .ToList();

            var hoy = DateTime.Today;
            TotalEsteAnio = Feriados.Count;
            TotalProximos = Feriados.Count(f => f.Fecha.Date >= hoy);

            return Page();
        }

        public IActionResult OnPostEliminar(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "FeriadosCreate", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Feriados/Index");
            }

            var feriado = _context.Feriados.FirstOrDefault(f => f.Id == id);
            if (feriado == null)
            {
                TempData["Error"] = "Feriado no encontrado";
                return RedirectToPage("/RRHH/Feriados/Index");
            }

            _context.Feriados.Remove(feriado);
            _context.SaveChanges();

            TempData["Success"] = "Feriado eliminado";
            return RedirectToPage("/RRHH/Feriados/Index");
        }
    }
}