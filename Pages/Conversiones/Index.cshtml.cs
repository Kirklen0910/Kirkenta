using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Conversiones
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        public List<ConversionInventario> Conversiones { get; set; } = new();
        public Dictionary<int, string> Nombres { get; set; } = new();
        public bool PuedeCrear { get; set; }
        public bool PuedeAprobar { get; set; }

        private Usuario? UsuarioActual() =>
            _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);

        private bool Tiene(Usuario? u, string accion)
        {
            var rol = u?.Rol ?? "Pendiente";
            return rol == "Admin" || PermisoHelper.TienePermiso(_context, rol, "Inventario", "Bajas", accion);
        }

        public IActionResult OnGet()
        {
            var u = UsuarioActual();
            if (!Tiene(u, "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver las conversiones";
                return RedirectToPage("/Index");
            }
            PuedeCrear = Tiene(u, "crear");
            PuedeAprobar = Tiene(u, "editar");
            Cargar();
            return Page();
        }

        public IActionResult OnPostAprobar(int id)
        {
            var u = UsuarioActual();
            if (!Tiene(u, "editar"))
            {
                TempData["Error"] = "No tienes permiso para aprobar conversiones";
                return RedirectToPage();
            }
            var c = _context.ConversionesInventario.FirstOrDefault(x => x.Id == id);
            if (c == null) { TempData["Error"] = "Conversión no encontrada"; return RedirectToPage(); }

            try
            {
                using var tx = _context.Database.BeginTransaction();
                var error = ConversionInventarioHelper.Aprobar(_context, c, u!.Id);
                if (error != null)
                {
                    tx.Rollback();
                    _context.ChangeTracker.Clear();
                    TempData["Error"] = error;
                    return RedirectToPage();
                }
                _context.SaveChanges();
                tx.Commit();
            }
            catch (DbUpdateConcurrencyException)
            {
                _context.ChangeTracker.Clear();
                TempData["Error"] = "El inventario cambió mientras se aprobaba. Intenta de nuevo.";
                return RedirectToPage();
            }

            ActividadHelper.Registrar(_context, u.Id, "Aprobar conversión",
                $"Aprobó conversión {c.Numero}: {c.CantidadOrigen} → {c.CantidadDestino} (costo trasladado L. {c.CostoTotal:N2})",
                HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Conversión {c.Numero} aprobada. El inventario se movió en ambos productos y el costo se trasladó.";
            return RedirectToPage();
        }

        public IActionResult OnPostRechazar(int id, string? motivoRechazo)
        {
            var u = UsuarioActual();
            if (!Tiene(u, "editar"))
            {
                TempData["Error"] = "No tienes permiso para rechazar conversiones";
                return RedirectToPage();
            }
            if (string.IsNullOrWhiteSpace(motivoRechazo))
            {
                TempData["Error"] = "Escribe el motivo del rechazo";
                return RedirectToPage();
            }
            var c = _context.ConversionesInventario.FirstOrDefault(x => x.Id == id);
            if (c == null || c.Estado != "Pendiente") { TempData["Error"] = "La conversión ya fue procesada o no existe"; return RedirectToPage(); }

            c.Estado = "Rechazada";
            c.MotivoRechazo = motivoRechazo.Trim();
            c.UsuarioApruebaId = u!.Id;
            c.FechaAprobacion = DateTime.Now;
            _context.SaveChanges();

            ActividadHelper.Registrar(_context, u.Id, "Rechazar conversión",
                $"Rechazó conversión {c.Numero}: {c.MotivoRechazo}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Conversión {c.Numero} rechazada.";
            return RedirectToPage();
        }

        private void Cargar()
        {
            var q = _context.ConversionesInventario.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(Estado)) q = q.Where(c => c.Estado == Estado);
            Pager = new PagerInfo(Pagina, 50, q.Count());
            Conversiones = q.OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id)
                .Skip(Pager.Skip).Take(Pager.PageSize).ToList();

            var ids = Conversiones.Select(c => c.ProductoOrigenId)
                .Concat(Conversiones.Where(c => c.ProductoDestinoId.HasValue).Select(c => c.ProductoDestinoId!.Value))
                .Distinct().ToList();
            Nombres = _context.Productos.AsNoTracking().Where(p => ids.Contains(p.Id))
                .ToDictionary(p => p.Id, p => p.Nombre);
        }
    }
}
