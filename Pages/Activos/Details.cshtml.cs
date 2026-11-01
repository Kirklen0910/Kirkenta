using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Activos
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public ActivoFijo Activo { get; set; } = new();
        public List<DepreciacionActivo> Depreciaciones { get; set; } = new();
        public List<MantenimientoActivo> Mantenimientos { get; set; } = new();
        public bool PuedeEditar { get; set; }
        public bool PuedeDarBaja { get; set; }
        public int MesesDepreciados { get; set; }

        [BindProperty] public string? MotivoBaja { get; set; }

        private bool Cargar(int id)
        {
            var a = _context.ActivosFijos.FirstOrDefault(x => x.Id == id);
            if (a == null) return false;
            Activo = a;
            Depreciaciones = _context.DepreciacionesActivo.Where(d => d.ActivoFijoId == id)
                .OrderByDescending(d => d.Anio).ThenByDescending(d => d.Mes).ToList();
            Mantenimientos = _context.MantenimientosActivo.Where(m => m.ActivoFijoId == id)
                .OrderByDescending(m => m.Fecha).ToList();
            MesesDepreciados = Depreciaciones.Count;
            var u = User.Identity?.Name;
            PuedeEditar = PermisoHelper.UsuarioTienePermiso(_context, u, "Activos", "Index", "editar");
            PuedeDarBaja = PermisoHelper.UsuarioTienePermiso(_context, u, "Activos", "Index", "eliminar");
            return true;
        }

        public IActionResult OnGet(int id)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Activos", "Index", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver los activos";
                return RedirectToPage("/Index");
            }
            if (!Cargar(id)) { TempData["Error"] = "Activo no encontrado"; return RedirectToPage("/Activos/Index"); }
            return Page();
        }

        public IActionResult OnPostBaja(int id)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Activos", "Index", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para dar de baja activos";
                return RedirectToPage(new { id });
            }
            var a = _context.ActivosFijos.FirstOrDefault(x => x.Id == id);
            if (a == null) { TempData["Error"] = "Activo no encontrado"; return RedirectToPage("/Activos/Index"); }
            if (a.Estado != "Activo") { TempData["Error"] = "El activo ya está dado de baja"; return RedirectToPage(new { id }); }
            if (string.IsNullOrWhiteSpace(MotivoBaja))
            {
                TempData["Error"] = "Debes indicar el motivo de la baja";
                return RedirectToPage(new { id });
            }

            a.Estado = "Baja";
            a.FechaBaja = DateTime.Now;
            a.MotivoBaja = MotivoBaja.Trim();
            _context.SaveChanges();

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Baja de activo fijo",
                $"Dio de baja el activo {a.Codigo} - {a.Nombre}: {a.MotivoBaja}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Activo {a.Codigo} dado de baja";
            return RedirectToPage(new { id });
        }
    }
}
