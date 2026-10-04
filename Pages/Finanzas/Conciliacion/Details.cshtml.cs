using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Conciliacion
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public ConciliacionBancaria Conciliacion { get; set; } = new();
        public CuentaFinanciera Cuenta { get; set; } = new();
        public List<ConciliacionDetalle> LineasSistema { get; set; } = new();
        public List<ConciliacionDetalle> LineasBanco { get; set; } = new();

        public string? UsuarioCierra { get; set; }
        public string? UsuarioCreo { get; set; }

        public bool PuedeCerrar { get; set; }
        public bool PuedeEditar { get; set; }

        [BindProperty]
        public InputBanco NuevoBanco { get; set; } = new();

        public class InputBanco
        {
            [Required(ErrorMessage = "La fecha es obligatoria")]
            public DateTime Fecha { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "La descripción es obligatoria")]
            [StringLength(300)]
            public string Descripcion { get; set; } = "";

            [StringLength(100)]
            public string? Referencia { get; set; }

            [Required(ErrorMessage = "El monto es obligatorio")]
            public decimal Monto { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "Conciliacion", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Conciliacion/Index");
            }

            if (!CargarConciliacion(id)) return RedirectToPage("/Finanzas/Conciliacion/Index");

            PuedeCerrar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCerrar", "editar");
            PuedeEditar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCreate", "editar");

            return Page();
        }

        public IActionResult OnPostAgregarBanco(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCreate", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            var conciliacion = _context.ConciliacionesBancarias.FirstOrDefault(c => c.Id == id);
            if (conciliacion == null)
            {
                TempData["Error"] = "Conciliación no encontrada";
                return RedirectToPage("/Finanzas/Conciliacion/Index");
            }

            if (conciliacion.Estado == "Conciliada" || conciliacion.Estado == "Cancelada")
            {
                TempData["Error"] = "La conciliación ya está cerrada o cancelada";
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            if (!ModelState.IsValid)
            {
                if (!CargarConciliacion(id)) return RedirectToPage("/Finanzas/Conciliacion/Index");
                PuedeCerrar = currentRol == "Admin" ||
                    PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCerrar", "editar");
                PuedeEditar = currentRol == "Admin" ||
                    PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCreate", "editar");
                return Page();
            }

            var detalle = new ConciliacionDetalle
            {
                ConciliacionBancariaId = id,
                Origen = "Banco",
                Fecha = NuevoBanco.Fecha,
                Descripcion = NuevoBanco.Descripcion,
                Referencia = NuevoBanco.Referencia,
                Monto = NuevoBanco.Monto,
                Matcheada = false,
                FechaCreacion = DateTime.Now
            };

            _context.ConciliacionesDetalle.Add(detalle);
            _context.SaveChanges();

            ConciliacionHelper.RecalcularTotales(_context, conciliacion);

            // Intentar matching automático con la nueva línea
            var matches = ConciliacionHelper.MatchingAutomatico(_context, id);
            if (matches > 0)
            {
                TempData["Success"] = $"Línea agregada. Se matchearon {matches} línea(s) automáticamente.";
            }
            else
            {
                TempData["Success"] = "Línea del banco agregada";
            }

            return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
        }

        public IActionResult OnPostEliminarBanco(int id, int detalleId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCreate", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            var detalle = _context.ConciliacionesDetalle
                .FirstOrDefault(d => d.Id == detalleId && d.ConciliacionBancariaId == id);

            if (detalle == null)
            {
                TempData["Error"] = "Detalle no encontrado";
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            if (detalle.Matcheada)
            {
                TempData["Error"] = "No puedes eliminar una línea que ya está matcheada. Primero deshaz el match.";
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            _context.ConciliacionesDetalle.Remove(detalle);
            _context.SaveChanges();

            var conciliacion = _context.ConciliacionesBancarias.FirstOrDefault(c => c.Id == id);
            if (conciliacion != null)
            {
                ConciliacionHelper.RecalcularTotales(_context, conciliacion);
            }

            TempData["Success"] = "Línea eliminada";
            return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
        }

        public IActionResult OnPostMatchingAuto(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCreate", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            var matches = ConciliacionHelper.MatchingAutomatico(_context, id);

            var conciliacion = _context.ConciliacionesBancarias.FirstOrDefault(c => c.Id == id);
            if (conciliacion != null)
            {
                ConciliacionHelper.RecalcularTotales(_context, conciliacion);
            }

            TempData["Success"] = $"Matching automático ejecutado. {matches} línea(s) matcheadas.";
            return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
        }

        public IActionResult OnPostMatchingManual(int id, int detalleSistemaId, int detalleBancoId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCreate", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            var (ok, error) = ConciliacionHelper.MatchingManual(_context, detalleSistemaId, detalleBancoId);

            if (!ok)
            {
                TempData["Error"] = error;
            }
            else
            {
                var conciliacion = _context.ConciliacionesBancarias.FirstOrDefault(c => c.Id == id);
                if (conciliacion != null)
                {
                    ConciliacionHelper.RecalcularTotales(_context, conciliacion);
                }
                TempData["Success"] = "Líneas matcheadas manualmente";
            }

            return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
        }

        public IActionResult OnPostDeshacerMatch(int id, int detalleId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCreate", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            var (ok, error) = ConciliacionHelper.DeshacerMatch(_context, detalleId);

            if (!ok)
            {
                TempData["Error"] = error;
            }
            else
            {
                var conciliacion = _context.ConciliacionesBancarias.FirstOrDefault(c => c.Id == id);
                if (conciliacion != null)
                {
                    ConciliacionHelper.RecalcularTotales(_context, conciliacion);
                }
                TempData["Success"] = "Match deshecho";
            }

            return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
        }

        public IActionResult OnPostRecargarSistema(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCreate", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            var conciliacion = _context.ConciliacionesBancarias.FirstOrDefault(c => c.Id == id);
            if (conciliacion == null)
            {
                TempData["Error"] = "Conciliación no encontrada";
                return RedirectToPage("/Finanzas/Conciliacion/Index");
            }

            if (conciliacion.Estado == "Conciliada" || conciliacion.Estado == "Cancelada")
            {
                TempData["Error"] = "La conciliación ya está cerrada o cancelada";
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            ConciliacionHelper.CargarLineasSistema(_context, conciliacion);
            ConciliacionHelper.RecalcularTotales(_context, conciliacion);

            TempData["Success"] = "Líneas del sistema recargadas";
            return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
        }

        public IActionResult OnPostCerrar(int id, string? notasCierre)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCerrar", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            var (ok, error) = ConciliacionHelper.Cerrar(_context, id, currentUser!.Id, notasCierre);

            if (!ok)
            {
                TempData["Error"] = error;
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            ActividadHelper.Registrar(
                _context,
                currentUser.Id,
                "Cerrar conciliación bancaria",
                $"Cerró la conciliación #{id}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Conciliación cerrada correctamente";
            return RedirectToPage("/Finanzas/Conciliacion/Index");
        }

        public IActionResult OnPostCancelar(int id, string motivo)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCerrar", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            var (ok, error) = ConciliacionHelper.Cancelar(_context, id, motivo ?? "Sin motivo");

            if (!ok)
            {
                TempData["Error"] = error;
                return RedirectToPage("/Finanzas/Conciliacion/Details", new { id });
            }

            TempData["Success"] = "Conciliación cancelada";
            return RedirectToPage("/Finanzas/Conciliacion/Index");
        }

        private bool CargarConciliacion(int id)
        {
            var conciliacion = _context.ConciliacionesBancarias.FirstOrDefault(c => c.Id == id);
            if (conciliacion == null)
            {
                TempData["Error"] = "Conciliación no encontrada";
                return false;
            }

            Conciliacion = conciliacion;
            Cuenta = _context.CuentasFinancieras.FirstOrDefault(c => c.Id == conciliacion.CuentaId) ?? new CuentaFinanciera();

            var detalles = _context.ConciliacionesDetalle
                .Where(d => d.ConciliacionBancariaId == id)
                .OrderBy(d => d.Fecha)
                .ToList();

            LineasSistema = detalles.Where(d => d.Origen == "Sistema").ToList();
            LineasBanco = detalles.Where(d => d.Origen == "Banco").ToList();

            UsuarioCreo = conciliacion.UsuarioCreoId.HasValue
                ? _context.Usuarios.FirstOrDefault(u => u.Id == conciliacion.UsuarioCreoId.Value)?.Username
                : null;

            UsuarioCierra = conciliacion.UsuarioCierraId.HasValue
                ? _context.Usuarios.FirstOrDefault(u => u.Id == conciliacion.UsuarioCierraId.Value)?.Username
                : null;

            return true;
        }
    }
}