using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Logistica;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace Kirkenta.Pages.Logistica.Reportes
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== Filtros =====
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int? FiltroZonaId { get; set; }
        public int? FiltroRepartidorId { get; set; }

        // ===== Catálogos para filtros =====
        public List<ZonaEnvio> Zonas { get; set; } = new();
        public List<Repartidor> Repartidores { get; set; } = new();

        // ===== KPIs generales =====
        public int TotalEnvios { get; set; }
        public int TotalEntregados { get; set; }
        public int TotalFallidos { get; set; }
        public int TotalPendientes { get; set; }
        public int TotalEnRuta { get; set; }
        public int TotalCancelados { get; set; }

        public decimal TasaExito { get; set; }
        public decimal TasaFallo { get; set; }

        public decimal MontoTotalEnvios { get; set; }
        public decimal MontoTotalEntregados { get; set; }
        public int CantidadEnviosGratis { get; set; }

        public decimal TiempoPromedioEntregaDias { get; set; }
        public decimal TiempoPromedioEntregaHoras { get; set; }

        // ===== Listas para tablas =====
        public List<RepartidorReporte> ReportePorRepartidor { get; set; } = new();
        public List<ZonaReporte> ReportePorZona { get; set; } = new();
        public List<RutaTopReporte> TopRutas { get; set; } = new();

        // ===== JSON para gráficos =====
        public string EnviosPorZonaJson { get; set; } = "{}";
        public string TasaExitoPorRepartidorJson { get; set; } = "{}";
        public string TendenciaEntregasJson { get; set; } = "{}";

        // ===== Permisos =====
        public bool PuedeVer { get; set; }

        // ============================================================
        // DTOs
        // ============================================================
        public class RepartidorReporte
        {
            public int RepartidorId { get; set; }
            public string Nombre { get; set; } = "";
            public string? Vehiculo { get; set; }
            public int TotalEnvios { get; set; }
            public int Entregados { get; set; }
            public int Fallidos { get; set; }
            public int Pendientes { get; set; }
            public decimal TasaExito { get; set; }
            public decimal TiempoPromedioDias { get; set; }
            public decimal MontoTotal { get; set; }
        }

        public class ZonaReporte
        {
            public int ZonaId { get; set; }
            public string Nombre { get; set; } = "";
            public string? Color { get; set; }
            public int TotalEnvios { get; set; }
            public int Entregados { get; set; }
            public int Fallidos { get; set; }
            public decimal TasaExito { get; set; }
            public decimal MontoTotal { get; set; }
        }

        public class RutaTopReporte
        {
            public int RutaId { get; set; }
            public string Numero { get; set; } = "";
            public string RepartidorNombre { get; set; } = "";
            public DateTime Fecha { get; set; }
            public int TotalParadas { get; set; }
            public int Entregadas { get; set; }
            public int Fallidas { get; set; }
            public decimal Progreso { get; set; }
            public decimal MontoTotal { get; set; }
        }

        // ============================================================
        // ONGET
        // ============================================================
        public IActionResult OnGet(DateTime? desde, DateTime? hasta, int? zonaId, int? repartidorId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Reportes", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver los reportes de logística";
                return RedirectToPage("/Logistica/Index");
            }

            PuedeVer = true;

            // ===== Rango de fechas por defecto: mes actual =====
            var hoy = DateTime.Today;
            Desde = (desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
            Hasta = (hasta ?? hoy).Date;

            if (Hasta < Desde)
            {
                Hasta = Desde;
            }

            FiltroZonaId = zonaId;
            FiltroRepartidorId = repartidorId;

            // ===== Catálogos =====
            Zonas = _context.ZonasEnvio
                .AsNoTracking()
                .Where(z => z.Activa)
                .OrderBy(z => z.Orden).ThenBy(z => z.Nombre)
                .ToList();

            Repartidores = _context.Repartidores
                .AsNoTracking()
                .Where(r => r.Activo)
                .OrderBy(r => r.Nombre)
                .ToList();

            // ===== Query base de envíos =====
            var desdeFull = Desde;
            var hastaFull = Hasta.AddDays(1);

            var queryEnvios = _context.Envios
                .AsNoTracking()
                .Where(e => e.FechaCreacion >= desdeFull && e.FechaCreacion < hastaFull);

            if (FiltroZonaId.HasValue)
            {
                queryEnvios = queryEnvios.Where(e => e.ZonaId == FiltroZonaId.Value);
            }

            if (FiltroRepartidorId.HasValue)
            {
                queryEnvios = queryEnvios.Where(e => e.RepartidorId == FiltroRepartidorId.Value);
            }

            var envios = queryEnvios.ToList();

            // ===== KPIs generales =====
            TotalEnvios = envios.Count;
            TotalEntregados = envios.Count(e => e.Estado == "Entregado");
            TotalFallidos = envios.Count(e => e.Estado == "Fallido");
            TotalPendientes = envios.Count(e => e.Estado == "Pendiente");
            TotalEnRuta = envios.Count(e => e.Estado == "EnRuta");
            TotalCancelados = envios.Count(e => e.Estado == "Cancelado");

            // Tasa de éxito sobre los resueltos (Entregado + Fallido)
            var resueltos = TotalEntregados + TotalFallidos;
            TasaExito = resueltos > 0
                ? Math.Round((decimal)TotalEntregados / resueltos * 100, 2)
                : 0;
            TasaFallo = resueltos > 0
                ? Math.Round((decimal)TotalFallidos / resueltos * 100, 2)
                : 0;

            // Montos
            MontoTotalEnvios = envios.Sum(e => e.Monto);
            MontoTotalEntregados = envios
                .Where(e => e.Estado == "Entregado")
                .Sum(e => e.Monto);
            CantidadEnviosGratis = envios.Count(e => e.EsGratis || e.Monto <= 0);

            // Tiempo promedio de entrega (solo entregados con ambas fechas)
            var entregadosConFecha = envios
                .Where(e => e.Estado == "Entregado"
                         && e.FechaEntregaReal.HasValue
                         && e.FechaSalida.HasValue)
                .ToList();

            if (entregadosConFecha.Count > 0)
            {
                var totalHoras = entregadosConFecha
                    .Sum(e => (e.FechaEntregaReal!.Value - e.FechaSalida!.Value).TotalHours);
                TiempoPromedioEntregaHoras = Math.Round((decimal)totalHoras / entregadosConFecha.Count, 2);
                TiempoPromedioEntregaDias = Math.Round(TiempoPromedioEntregaHoras / 24m, 2);
            }

            // ===== Reporte por repartidor =====
            ReportePorRepartidor = GenerarReportePorRepartidor(envios, Repartidores);

            // ===== Reporte por zona =====
            ReportePorZona = GenerarReportePorZona(envios, Zonas);

            // ===== Top rutas =====
            TopRutas = GenerarTopRutas(desdeFull, hastaFull);

            // ===== JSON para gráficos =====
            EnviosPorZonaJson = JsonSerializer.Serialize(new
            {
                labels = ReportePorZona
                    .Where(z => z.TotalEnvios > 0)
                    .OrderByDescending(z => z.TotalEnvios)
                    .Select(z => z.Nombre)
                    .ToArray(),
                data = ReportePorZona
                    .Where(z => z.TotalEnvios > 0)
                    .OrderByDescending(z => z.TotalEnvios)
                    .Select(z => z.TotalEnvios)
                    .ToArray(),
                colors = ReportePorZona
                    .Where(z => z.TotalEnvios > 0)
                    .OrderByDescending(z => z.TotalEnvios)
                    .Select(z => z.Color ?? "#6b7280")
                    .ToArray()
            });

            // Tasa de éxito por repartidor (solo los que tienen envíos resueltos)
            var repartidoresConResueltos = ReportePorRepartidor
                .Where(r => (r.Entregados + r.Fallidos) > 0)
                .OrderByDescending(r => r.Entregados + r.Fallidos)
                .Take(10)
                .ToList();

            TasaExitoPorRepartidorJson = JsonSerializer.Serialize(new
            {
                labels = repartidoresConResueltos.Select(r => r.Nombre).ToArray(),
                entregados = repartidoresConResueltos.Select(r => r.Entregados).ToArray(),
                fallidos = repartidoresConResueltos.Select(r => r.Fallidos).ToArray()
            });

            // Tendencia de entregas por día
            var entregasPorDia = envios
                .Where(e => e.Estado == "Entregado" && e.FechaEntregaReal.HasValue)
                .GroupBy(e => e.FechaEntregaReal!.Value.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Fecha = g.Key.ToString("dd/MM"),
                    Entregados = g.Count(),
                    Monto = g.Sum(e => e.Monto)
                })
                .ToList();

            TendenciaEntregasJson = JsonSerializer.Serialize(new
            {
                labels = entregasPorDia.Select(x => x.Fecha).ToArray(),
                entregados = entregasPorDia.Select(x => x.Entregados).ToArray(),
                monto = entregasPorDia.Select(x => x.Monto).ToArray()
            });

            return Page();
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private static List<RepartidorReporte> GenerarReportePorRepartidor(
            List<Envio> envios,
            List<Repartidor> repartidores)
        {
            var repartidoresDict = repartidores.ToDictionary(r => r.Id, r => r);

            var grupos = envios
                .Where(e => e.RepartidorId.HasValue)
                .GroupBy(e => e.RepartidorId!.Value)
                .ToList();

            var resultado = new List<RepartidorReporte>();

            foreach (var g in grupos)
            {
                var rep = repartidoresDict.GetValueOrDefault(g.Key);
                var entregados = g.Count(e => e.Estado == "Entregado");
                var fallidos = g.Count(e => e.Estado == "Fallido");
                var pendientes = g.Count(e => e.Estado == "Pendiente" || e.Estado == "EnRuta");
                var resueltos = entregados + fallidos;

                // Tiempo promedio
                var conTiempo = g
                    .Where(e => e.Estado == "Entregado"
                             && e.FechaEntregaReal.HasValue
                             && e.FechaSalida.HasValue)
                    .ToList();

                decimal tiempoPromedio = 0;
                if (conTiempo.Count > 0)
                {
                    var totalHoras = conTiempo
                        .Sum(e => (e.FechaEntregaReal!.Value - e.FechaSalida!.Value).TotalHours);
                    tiempoPromedio = Math.Round((decimal)totalHoras / conTiempo.Count / 24m, 2);
                }

                resultado.Add(new RepartidorReporte
                {
                    RepartidorId = g.Key,
                    Nombre = rep?.Nombre ?? "—",
                    Vehiculo = rep?.Vehiculo,
                    TotalEnvios = g.Count(),
                    Entregados = entregados,
                    Fallidos = fallidos,
                    Pendientes = pendientes,
                    TasaExito = resueltos > 0
                        ? Math.Round((decimal)entregados / resueltos * 100, 2)
                        : 0,
                    TiempoPromedioDias = tiempoPromedio,
                    MontoTotal = g.Sum(e => e.Monto)
                });
            }

            return resultado
                .OrderByDescending(r => r.TotalEnvios)
                .ThenByDescending(r => r.TasaExito)
                .ToList();
        }

        private static List<ZonaReporte> GenerarReportePorZona(
            List<Envio> envios,
            List<ZonaEnvio> zonas)
        {
            var zonasDict = zonas.ToDictionary(z => z.Id, z => z);

            var grupos = envios
                .Where(e => e.ZonaId.HasValue)
                .GroupBy(e => e.ZonaId!.Value)
                .ToList();

            var resultado = new List<ZonaReporte>();

            foreach (var g in grupos)
            {
                var zona = zonasDict.GetValueOrDefault(g.Key);
                var entregados = g.Count(e => e.Estado == "Entregado");
                var fallidos = g.Count(e => e.Estado == "Fallido");
                var resueltos = entregados + fallidos;

                resultado.Add(new ZonaReporte
                {
                    ZonaId = g.Key,
                    Nombre = zona?.Nombre ?? "—",
                    Color = zona?.Color ?? "#6b7280",
                    TotalEnvios = g.Count(),
                    Entregados = entregados,
                    Fallidos = fallidos,
                    TasaExito = resueltos > 0
                        ? Math.Round((decimal)entregados / resueltos * 100, 2)
                        : 0,
                    MontoTotal = g.Sum(e => e.Monto)
                });
            }

            return resultado
                .OrderByDescending(z => z.TotalEnvios)
                .ToList();
        }

        private List<RutaTopReporte> GenerarTopRutas(DateTime desde, DateTime hasta)
        {
            var rutas = _context.Rutas
                .AsNoTracking()
                .Where(r => r.Fecha >= desde && r.Fecha < hasta)
                .OrderByDescending(r => r.TotalParadas)
                .ThenByDescending(r => r.Fecha)
                .Take(10)
                .ToList();

            if (rutas.Count == 0) return new List<RutaTopReporte>();

            var rutaIds = rutas.Select(r => r.Id).ToList();

            // Contar paradas resueltas por ruta
            var enviosPorRuta = _context.Envios
                .AsNoTracking()
                .Where(e => e.RutaId.HasValue && rutaIds.Contains(e.RutaId.Value))
                .ToList()
                .GroupBy(e => e.RutaId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            return rutas.Select(r =>
            {
                var paradas = enviosPorRuta.GetValueOrDefault(r.Id) ?? new List<Envio>();
                var entregadas = paradas.Count(e => e.Estado == "Entregado");
                var fallidas = paradas.Count(e => e.Estado == "Fallido");
                var total = paradas.Count;
                var resueltas = entregadas + fallidas;

                return new RutaTopReporte
                {
                    RutaId = r.Id,
                    Numero = r.Numero,
                    RepartidorNombre = r.RepartidorNombre,
                    Fecha = r.Fecha,
                    TotalParadas = total,
                    Entregadas = entregadas,
                    Fallidas = fallidas,
                    Progreso = total > 0
                        ? Math.Round((decimal)resueltas / total * 100, 1)
                        : 0,
                    MontoTotal = r.MontoTotalEnvios
                };
            })
            .OrderByDescending(r => r.Entregadas)
            .ToList();
        }
    }
}