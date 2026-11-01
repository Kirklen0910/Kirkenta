using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.Asientos
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public AsientoContable Asiento { get; set; } = new();
        public List<DetalleAsiento> Lineas { get; set; } = new();
        public Dictionary<int, PlanCuenta> Cuentas { get; set; } = new();
        public string? Creador { get; set; }
        public bool PuedeAnular { get; set; }

        private bool Cargar(int id)
        {
            var a = _context.AsientosContables.FirstOrDefault(x => x.Id == id);
            if (a == null) return false;
            Asiento = a;
            Lineas = _context.DetallesAsiento.Where(d => d.AsientoContableId == id).OrderBy(d => d.Id).ToList();
            var ids = Lineas.Select(l => l.PlanCuentaId).Distinct().ToList();
            Cuentas = _context.PlanCuentas.Where(c => ids.Contains(c.Id)).ToDictionary(c => c.Id);
            Creador = a.UsuarioId.HasValue ? _context.Usuarios.Where(u => u.Id == a.UsuarioId.Value).Select(u => u.Username).FirstOrDefault() : null;
            PuedeAnular = a.Estado == "Activo" &&
                PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Finanzas", "Asientos", "editar");
            return true;
        }

        public IActionResult OnGet(int id)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Finanzas", "Asientos", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver asientos";
                return RedirectToPage("/Index");
            }
            if (!Cargar(id)) { TempData["Error"] = "Asiento no encontrado"; return RedirectToPage("/Finanzas/Asientos/Index"); }
            return Page();
        }

        public IActionResult OnPostAnular(int id, string? motivo)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Finanzas", "Asientos", "editar"))
            {
                TempData["Error"] = "No tienes permiso para anular asientos";
                return RedirectToPage(new { id });
            }
            if (string.IsNullOrWhiteSpace(motivo)) { TempData["Error"] = "Indica el motivo de la anulación"; return RedirectToPage(new { id }); }

            var a = _context.AsientosContables.FirstOrDefault(x => x.Id == id);
            if (a == null || a.Estado != "Activo") { TempData["Error"] = "El asiento no está activo"; return RedirectToPage(new { id }); }

            var (ok, error) = CierreContableHelper.ValidarFecha(_context, a.Fecha);
            if (!ok) { TempData["Error"] = error; return RedirectToPage(new { id }); }

            a.Estado = "Anulado";
            a.MotivoAnulacion = motivo.Trim();
            _context.SaveChanges();

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Anular asiento contable",
                $"Anuló el asiento {a.Numero}: {a.MotivoAnulacion}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Asiento {a.Numero} anulado";
            return RedirectToPage(new { id });
        }
    }
}
