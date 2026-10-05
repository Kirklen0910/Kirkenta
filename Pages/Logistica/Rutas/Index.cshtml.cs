using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Logistica;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Logistica.Rutas
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== Datos =====
        public List<RutaItem> Rutas { get; set; } = new();

        // ===== Filtros activos =====
        public string? FiltroEstado { get; set; }
        public string? FiltroAlerta { get; set; }
        public string? FiltroBusqueda { get; set; }
        public int? FiltroRepartidorId { get; set; }
        public int? FiltroZonaId { get; set; }
        public DateTime? FiltroDesde { get; set; }
        public DateTime? FiltroHasta { get; set; }

        // ===== KPIs =====
        public int TotalBorrador { get; set; }
        public int TotalEnReparto { get; set; }
        public int TotalCompletadas { get; set; }
        public int TotalCanceladas { get; set; }
        public int TotalAtrasadas { get; set; }

        public int TotalParadasActivas { get; set; }
        public int TotalParadasPendientes { get; set; }
        public int TotalParadasEntregadas { get; set; }
        public int TotalParadasFallidas { get; set; }

        // ===== Catálogos para filtros =====
        public List<Repartidor> Repartidores { get; set; } = new();
        public List<ZonaEnvio> Zonas { get; set; } = new();

        // ===== Permisos =====
        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }

        public class RutaItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public string Estado { get; set; } = "";
            public int? RepartidorId { get; set; }
            public string RepartidorNombre { get; set; } = "";
            public string? Vehiculo { get; set; }
            public string? Placa { get; set; }
            public int? ZonaId { get; set; }
            public string? ZonaNombre { get; set; }
            public DateTime? FechaSalida { get; set; }
            public DateTime? FechaRegreso { get; set; }
            public int TotalParadas { get; set; }
            public int ParadasEntregadas { get; set; }
            public int ParadasFallidas { get; set; }
            public int ParadasPendientes { get; set; }
            public int ParadasAtrasadas { get; set; }
            public decimal MontoTotalEnvios { get; set; }
            public string? TrackingCode { get; set; }
            public bool EsAtrasada { get; set; }
            public int Progreso => TotalParadas > 0
                ? (int)Math.Round((double)(ParadasEntregadas + ParadasFallidas) / TotalParadas * 100)
                : 0;
        }

        public IActionResult OnGet(string? filtro, string? estado, string? busqueda, int? repartidorId, int? zonaId, DateTime? desde, DateTime? hasta)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Rutas", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver rutas";
                return RedirectToPage("/Logistica/Index");
            }

            PuedeCrear = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RutasCreate", "crear");
            PuedeEditar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RutasEdit", "editar");

            // Filtros
            FiltroEstado = estado;
            FiltroAlerta = filtro;
            FiltroBusqueda = busqueda;
            FiltroRepartidorId = repartidorId;
            FiltroZonaId = zonaId;
            FiltroDesde = desde;
            FiltroHasta = hasta;

            Repartidores = _context.Repartidores
                .AsNoTracking()
                .Where(r => r.Activo)
                .OrderBy(r => r.Nombre)
                .ToList();

            Zonas = _context.ZonasEnvio
                .AsNoTracking()
                .Where(z => z.Activa)
                .OrderBy(z => z.Orden).ThenBy(z => z.Nombre)
                .ToList();

            // Query base
            var query = _context.Rutas.AsNoTracking().AsQueryable();

            // Filtro por estado
            if (!string.IsNullOrWhiteSpace(estado))
            {
                query = query.Where(r => r.Estado == estado);
            }

            // Filtro por fecha
            if (desde.HasValue)
            {
                query = query.Where(r => r.Fecha >= desde.Value.Date);
            }
            if (hasta.HasValue)
            {
                var hastaFin = hasta.Value.Date.AddDays(1);
                query = query.Where(r => r.Fecha < hastaFin);
            }

            // Filtro por repartidor
            if (repartidorId.HasValue)
            {
                query = query.Where(r => r.RepartidorId == repartidorId.Value);
            }

            // Filtro por zona
            if (zonaId.HasValue)
            {
                query = query.Where(r => r.ZonaId == zonaId.Value);
            }

            // Búsqueda por texto
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var b = busqueda.Trim().ToLower();
                query = query.Where(r =>
                    r.Numero.ToLower().Contains(b)
                    || r.RepartidorNombre.ToLower().Contains(b)
                    || (r.TrackingCode != null && r.TrackingCode.ToLower().Contains(b))
                    || (r.Descripcion != null && r.Descripcion.ToLower().Contains(b)));
            }

            // Filtro de alerta
            var hoy = DateTime.Today;
            if (filtro == "borrador")
            {
                query = query.Where(r => r.Estado == "Borrador");
            }
            else if (filtro == "enreparto")
            {
                query = query.Where(r => r.Estado == "EnReparto");
            }
            else if (filtro == "completadas")
            {
                query = query.Where(r => r.Estado == "Completada");
            }
            else if (filtro == "atrasadas")
            {
                // Rutas en reparto con paradas vencidas
                var rutasActivasIds = _context.Rutas
                    .AsNoTracking()
                    .Where(r => r.Estado == "EnReparto")
                    .Select(r => r.Id)
                    .ToList();

                var idsAtrasadas = _context.Envios
                    .AsNoTracking()
                    .Where(e => e.RutaId.HasValue
                             && rutasActivasIds.Contains(e.RutaId.Value)
                             && (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                             && e.FechaEntregaEstimada.HasValue
                             && e.FechaEntregaEstimada.Value.Date < hoy)
                    .Select(e => e.RutaId!.Value)
                    .Distinct()
                    .ToList();

                query = query.Where(r => idsAtrasadas.Contains(r.Id));
            }

            var rutas = query
                .OrderByDescending(r => r.Fecha)
                .ThenByDescending(r => r.Id)
                .ToList();

            var rutaIds = rutas.Select(r => r.Id).ToList();

            // Traer todas las paradas de todas las rutas de una vez
            var todasLasParadas = rutaIds.Count > 0
                ? _context.Envios
                    .AsNoTracking()
                    .Where(e => e.RutaId.HasValue && rutaIds.Contains(e.RutaId.Value))
                    .ToList()
                : new List<Envio>();

            // Mapear items
            Rutas = rutas.Select(r =>
            {
                var paradas = todasLasParadas.Where(e => e.RutaId == r.Id).ToList();

                return new RutaItem
                {
                    Id = r.Id,
                    Numero = r.Numero,
                    Fecha = r.Fecha,
                    Estado = r.Estado,
                    RepartidorId = r.RepartidorId,
                    RepartidorNombre = r.RepartidorNombre,
                    Vehiculo = r.Vehiculo,
                    Placa = r.Placa,
                    ZonaId = r.ZonaId,
                    ZonaNombre = r.ZonaNombre,
                    FechaSalida = r.FechaSalida,
                    FechaRegreso = r.FechaRegreso,
                    TotalParadas = paradas.Count,
                    ParadasEntregadas = paradas.Count(e => e.Estado == "Entregado"),
                    ParadasFallidas = paradas.Count(e => e.Estado == "Fallido"),
                    ParadasPendientes = paradas.Count(e => e.Estado == "Pendiente" || e.Estado == "EnRuta"),
                    ParadasAtrasadas = paradas.Count(e =>
                        (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                        && e.FechaEntregaEstimada.HasValue
                        && e.FechaEntregaEstimada.Value.Date < hoy),
                    MontoTotalEnvios = r.MontoTotalEnvios,
                    TrackingCode = r.TrackingCode,
                    EsAtrasada = r.Estado == "EnReparto" && paradas.Any(e =>
                        (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                        && e.FechaEntregaEstimada.HasValue
                        && e.FechaEntregaEstimada.Value.Date < hoy)
                };
            }).ToList();

            // ===== KPIs (universo completo) =====
            var todas = _context.Rutas.AsNoTracking().ToList();
            TotalBorrador = todas.Count(r => r.Estado == "Borrador");
            TotalEnReparto = todas.Count(r => r.Estado == "EnReparto");
            TotalCompletadas = todas.Count(r => r.Estado == "Completada");
            TotalCanceladas = todas.Count(r => r.Estado == "Cancelada");

            // Rutas activas con paradas atrasadas
            var rutasActivasIds2 = todas
                .Where(r => r.Estado == "EnReparto")
                .Select(r => r.Id)
                .ToList();

            if (rutasActivasIds2.Count > 0)
            {
                var rutasAtrasadas = _context.Envios
                    .AsNoTracking()
                    .Where(e => e.RutaId.HasValue
                             && rutasActivasIds2.Contains(e.RutaId.Value)
                             && (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                             && e.FechaEntregaEstimada.HasValue
                             && e.FechaEntregaEstimada.Value.Date < hoy)
                    .Select(e => e.RutaId!.Value)
                    .Distinct()
                    .Count();

                TotalAtrasadas = rutasAtrasadas;
            }

            // Paradas totales en rutas activas
            var enviosActivos = _context.Envios
                .AsNoTracking()
                .Where(e => e.RutaId.HasValue && rutasActivasIds2.Contains(e.RutaId.Value))
                .ToList();

            TotalParadasActivas = enviosActivos.Count;
            TotalParadasPendientes = enviosActivos.Count(e => e.Estado == "Pendiente" || e.Estado == "EnRuta");
            TotalParadasEntregadas = enviosActivos.Count(e => e.Estado == "Entregado");
            TotalParadasFallidas = enviosActivos.Count(e => e.Estado == "Fallido");

            return Page();
        }
    }
}