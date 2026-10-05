using Kirkenta.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Logistica.Tracking
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Codigo { get; set; }

        // ===== Datos de la ruta =====
        public Models.Ruta? Ruta { get; set; }
        public string CodigoBuscado { get; set; } = "";
        public string? Error { get; set; }

        // ===== Paradas =====
        public List<ParadaPublica> Paradas { get; set; } = new();

        // ===== Historial =====
        public List<Models.RutaHistorial> Historial { get; set; } = new();

        // ===== KPIs =====
        public int TotalParadas { get; set; }
        public int ParadasEntregadas { get; set; }
        public int ParadasFallidas { get; set; }
        public int ParadasPendientes { get; set; }
        public int Progreso { get; set; }
        public DateTime? FechaEstimada { get; set; }

        public class ParadaPublica
        {
            public int Id { get; set; }
            public int? OrdenParada { get; set; }
            public string ClienteNombre { get; set; } = "";
            public string DireccionEntrega { get; set; } = "";
            public string? Ciudad { get; set; }
            public string? Referencia { get; set; }
            public string Estado { get; set; } = "";
            public DateTime? FechaEntregaEstimada { get; set; }
            public DateTime? FechaEntregaReal { get; set; }
            public string? NombreRecibio { get; set; }
            public string? MotivoFallo { get; set; }
        }

        public void OnGet()
        {
            CodigoBuscado = (Codigo ?? "").Trim();

            if (string.IsNullOrWhiteSpace(CodigoBuscado))
            {
                // Sin código no hay error, solo mostramos el buscador
                return;
            }

            // Normalizar: trim + uppercase
            var codigoNorm = CodigoBuscado.ToUpperInvariant();

            // Buscar la ruta por tracking code
            var ruta = _context.Rutas
                .AsNoTracking()
                .FirstOrDefault(r => r.TrackingCode != null && r.TrackingCode.ToUpper() == codigoNorm);

            if (ruta == null)
            {
                Error = $"El código '{CodigoBuscado}' no existe o ya expiró. Verifica que esté escrito correctamente.";
                return;
            }

            Ruta = ruta;

            // Cargar paradas
            var envios = _context.Envios
                .AsNoTracking()
                .Where(e => e.RutaId == ruta.Id)
                .OrderBy(e => e.OrdenParada)
                .ThenBy(e => e.Id)
                .ToList();

            Paradas = envios.Select(e => new ParadaPublica
            {
                Id = e.Id,
                OrdenParada = e.OrdenParada,
                ClienteNombre = e.ClienteNombre,
                DireccionEntrega = e.DireccionEntrega,
                Ciudad = e.Ciudad,
                Referencia = e.Referencia,
                Estado = e.Estado,
                FechaEntregaEstimada = e.FechaEntregaEstimada,
                FechaEntregaReal = e.FechaEntregaReal,
                NombreRecibio = e.NombreRecibio,
                MotivoFallo = e.MotivoFallo
            }).ToList();

            TotalParadas = Paradas.Count;
            ParadasEntregadas = Paradas.Count(p => p.Estado == "Entregado");
            ParadasFallidas = Paradas.Count(p => p.Estado == "Fallido");
            ParadasPendientes = Paradas.Count(p => p.Estado == "Pendiente" || p.Estado == "EnRuta");

            Progreso = TotalParadas > 0
                ? (int)Math.Round((double)(ParadasEntregadas + ParadasFallidas) / TotalParadas * 100)
                : 0;

            // Fecha estimada: si la ruta tiene fecha de salida, usamos la del primer envío o la fecha estimada de la ruta
            // Si no, usamos la fecha estimada del primer envío
            FechaEstimada = envios
                .Where(e => e.FechaEntregaEstimada.HasValue)
                .Select(e => e.FechaEntregaEstimada)
                .FirstOrDefault();

            // Historial (solo eventos relevantes, no exponemos datos sensibles)
            Historial = _context.RutasHistorial
                .AsNoTracking()
                .Where(h => h.RutaId == ruta.Id)
                .OrderByDescending(h => h.Fecha)
                .ToList();
        }
    }
}