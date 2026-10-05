using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Logistica.Zonas
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ZonaItem> Zonas { get; set; } = new();

        public int TotalActivas { get; set; }
        public int TotalInactivas { get; set; }
        public int TotalEnviosAsociados { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }

        public class ZonaItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";
            public string? Descripcion { get; set; }
            public decimal PrecioSugerido { get; set; }
            public string Color { get; set; } = "#6b7280";
            public bool Activa { get; set; }
            public int Orden { get; set; }
            public DateTime FechaCreacion { get; set; }
            public int EnviosCount { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Zonas", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver zonas de envío";
                return RedirectToPage("/Logistica/Index");
            }

            PuedeCrear = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "ZonasCreate", "crear");
            PuedeEditar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "ZonasEdit", "editar");
            PuedeEliminar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "ZonasDelete", "eliminar");

            // Conteo de envíos por zona (una sola query)
            var enviosPorZona = _context.Envios
                .AsNoTracking()
                .Where(e => e.ZonaId != null)
                .GroupBy(e => e.ZonaId!.Value)
                .Select(g => new { ZonaId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.ZonaId, x => x.Count);

            var zonas = _context.ZonasEnvio
                .AsNoTracking()
                .OrderBy(z => z.Orden)
                .ThenBy(z => z.Nombre)
                .ToList();

            Zonas = zonas.Select(z => new ZonaItem
            {
                Id = z.Id,
                Nombre = z.Nombre,
                Descripcion = z.Descripcion,
                PrecioSugerido = z.PrecioSugerido,
                Color = z.Color,
                Activa = z.Activa,
                Orden = z.Orden,
                FechaCreacion = z.FechaCreacion,
                EnviosCount = enviosPorZona.GetValueOrDefault(z.Id, 0)
            }).ToList();

            TotalActivas = zonas.Count(z => z.Activa);
            TotalInactivas = zonas.Count(z => !z.Activa);
            TotalEnviosAsociados = enviosPorZona.Values.Sum();

            return Page();
        }
    }
}