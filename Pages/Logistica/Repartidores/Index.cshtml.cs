using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Logistica.Repartidores
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<RepartidorItem> Repartidores { get; set; } = new();
        public List<string> VehiculosDisponibles { get; set; } = new();

        public int TotalActivos { get; set; }
        public int TotalInactivos { get; set; }
        public int TotalRutasActivas { get; set; }
        public int TotalEntregas { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }

        public class RepartidorItem
        {
            public int Id { get; set; }
            public string Codigo { get; set; } = "";
            public string Nombre { get; set; } = "";
            public string? Telefono { get; set; }
            public string? Vehiculo { get; set; }
            public string? Placa { get; set; }
            public string? Licencia { get; set; }
            public bool Activo { get; set; }
            public DateTime FechaCreacion { get; set; }
            public int RutasActivas { get; set; }
            public int TotalEntregas { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Repartidores", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver repartidores";
                return RedirectToPage("/Logistica/Index");
            }

            PuedeCrear = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RepartidoresCreate", "crear");
            PuedeEditar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RepartidoresEdit", "editar");
            PuedeEliminar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RepartidoresDelete", "eliminar");

            var repartidores = _context.Repartidores
                .AsNoTracking()
                .OrderBy(r => r.Nombre)
                .ToList();

            // ===== RUTAS ACTIVAS POR REPARTIDOR =====
            // Solo contamos rutas en estado Borrador o EnReparto
            var rutasActivasPorRepartidor = _context.Rutas
                .AsNoTracking()
                .Where(r => r.RepartidorId != null
                         && (r.Estado == "Borrador" || r.Estado == "EnReparto"))
                .GroupBy(r => r.RepartidorId!.Value)
                .Select(g => new { RepartidorId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.RepartidorId, x => x.Count);

            // ===== ENTREGAS COMPLETADAS POR REPARTIDOR =====
            // Contamos envíos Entregados asignados a cada repartidor
            var entregasPorRepartidor = _context.Envios
                .AsNoTracking()
                .Where(e => e.RepartidorId != null && e.Estado == "Entregado")
                .GroupBy(e => e.RepartidorId!.Value)
                .Select(g => new { RepartidorId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.RepartidorId, x => x.Count);

            Repartidores = repartidores.Select(r => new RepartidorItem
            {
                Id = r.Id,
                Codigo = r.Codigo,
                Nombre = r.Nombre,
                Telefono = r.Telefono,
                Vehiculo = r.Vehiculo,
                Placa = r.Placa,
                Licencia = r.Licencia,
                Activo = r.Activo,
                FechaCreacion = r.FechaCreacion,
                RutasActivas = rutasActivasPorRepartidor.GetValueOrDefault(r.Id, 0),
                TotalEntregas = entregasPorRepartidor.GetValueOrDefault(r.Id, 0)
            }).ToList();

            TotalActivos = repartidores.Count(r => r.Activo);
            TotalInactivos = repartidores.Count(r => !r.Activo);
            TotalRutasActivas = rutasActivasPorRepartidor.Values.Sum();
            TotalEntregas = entregasPorRepartidor.Values.Sum();

            // Para el filtro de vehículos
            VehiculosDisponibles = repartidores
                .Where(r => !string.IsNullOrEmpty(r.Vehiculo))
                .Select(r => r.Vehiculo!)
                .Distinct()
                .OrderBy(v => v)
                .ToList();

            return Page();
        }
    }
}