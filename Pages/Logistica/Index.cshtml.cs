using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Logistica;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Logistica
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== KPIs principales =====
        public int TotalEnvios { get; set; }
        public int EnviosPendientes { get; set; }
        public int EnviosEnRuta { get; set; }
        public int EnviosEntregados { get; set; }
        public int EnviosFallidos { get; set; }

        public int TotalRutas { get; set; }
        public int RutasBorrador { get; set; }
        public int RutasEnReparto { get; set; }
        public int RutasCompletadas { get; set; }

        public int TotalRepartidores { get; set; }
        public int RepartidoresActivos { get; set; }
        public int TotalZonas { get; set; }

        // ===== Alertas =====
        public AlertaEnvioHelper.ResumenAlertas Alertas { get; set; } = new();

        public List<EnvioAlertaItem> EnviosAtrasados { get; set; } = new();
        public List<EnvioAlertaItem> EnviosVencenHoy { get; set; } = new();
        public List<RutaAlertaItem> RutasAtrasadas { get; set; } = new();
        public List<EnvioAlertaItem> EnviosEntregadosHoy { get; set; } = new();
        public List<EnvioAlertaItem> EnviosFallidosLista { get; set; } = new();

        // ===== Accesos rápidos =====
        public List<EnvioRecienteItem> UltimosEnvios { get; set; } = new();
        public List<RutaRecienteItem> UltimasRutas { get; set; } = new();

        // ===== Permisos =====
        public bool PuedeCrearRuta { get; set; }
        public bool PuedeVerEnvios { get; set; }
        public bool PuedeVerRutas { get; set; }
        public bool PuedeVerReportes { get; set; }

        public class EnvioAlertaItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public string ClienteNombre { get; set; } = "";
            public string DireccionEntrega { get; set; } = "";
            public string? ZonaNombre { get; set; }
            public string Estado { get; set; } = "";
            public DateTime? FechaEntregaEstimada { get; set; }
            public DateTime? FechaEntregaReal { get; set; }
            public string? RepartidorNombre { get; set; }
            public int? DiasDiferencia { get; set; }
            public string Semaforo { get; set; } = "Verde";
        }

        public class RutaAlertaItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public string RepartidorNombre { get; set; } = "";
            public string? Vehiculo { get; set; }
            public string Estado { get; set; } = "";
            public DateTime? FechaSalida { get; set; }
            public int TotalParadas { get; set; }
            public int ParadasEntregadas { get; set; }
            public int ParadasFallidas { get; set; }
            public int ParadasPendientes { get; set; }
            public int ParadasAtrasadas { get; set; }
        }

        public class EnvioRecienteItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public string ClienteNombre { get; set; } = "";
            public string Estado { get; set; } = "";
            public DateTime FechaCreacion { get; set; }
            public string? ZonaNombre { get; set; }
        }

        public class RutaRecienteItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public string RepartidorNombre { get; set; } = "";
            public string Estado { get; set; } = "";
            public DateTime Fecha { get; set; }
            public int TotalParadas { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Index", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver Logística";
                return RedirectToPage("/Index");
            }

            PuedeCrearRuta = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RutasCreate", "crear");
            PuedeVerEnvios = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Envios", "ver");
            PuedeVerRutas = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Rutas", "ver");
            PuedeVerReportes = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Reportes", "ver");

            // ===== KPIs generales =====
            TotalEnvios = _context.Envios.AsNoTracking().Count();
            EnviosPendientes = _context.Envios.AsNoTracking().Count(e => e.Estado == "Pendiente");
            EnviosEnRuta = _context.Envios.AsNoTracking().Count(e => e.Estado == "EnRuta");
            EnviosEntregados = _context.Envios.AsNoTracking().Count(e => e.Estado == "Entregado");
            EnviosFallidos = _context.Envios.AsNoTracking().Count(e => e.Estado == "Fallido");

            TotalRutas = _context.Rutas.AsNoTracking().Count();
            RutasBorrador = _context.Rutas.AsNoTracking().Count(r => r.Estado == "Borrador");
            RutasEnReparto = _context.Rutas.AsNoTracking().Count(r => r.Estado == "EnReparto");
            RutasCompletadas = _context.Rutas.AsNoTracking().Count(r => r.Estado == "Completada");

            TotalRepartidores = _context.Repartidores.AsNoTracking().Count();
            RepartidoresActivos = _context.Repartidores.AsNoTracking().Count(r => r.Activo);
            TotalZonas = _context.ZonasEnvio.AsNoTracking().Count(z => z.Activa);

            // ===== Alertas (calculadas en tiempo real) =====
            Alertas = AlertaEnvioHelper.ObtenerResumenAlertas(_context);

            // Envíos atrasados
            var enviosAtrasados = AlertaEnvioHelper.ObtenerEnviosAtrasados(_context);
            EnviosAtrasados = MapEnvios(enviosAtrasados);

            // Envíos que vencen hoy
            var enviosHoy = AlertaEnvioHelper.ObtenerEnviosVencenHoy(_context);
            EnviosVencenHoy = MapEnvios(enviosHoy);

            // Envíos entregados hoy
            var enviosEntregadosHoy = AlertaEnvioHelper.ObtenerEnviosEntregadosHoy(_context);
            EnviosEntregadosHoy = MapEnvios(enviosEntregadosHoy);

            // Envíos fallidos
            var enviosFallidos = AlertaEnvioHelper.ObtenerEnviosFallidos(_context);
            EnviosFallidosLista = MapEnvios(enviosFallidos);

            // Rutas atrasadas
            var rutasAtrasadas = AlertaEnvioHelper.ObtenerRutasAtrasadas(_context);
            RutasAtrasadas = MapRutas(rutasAtrasadas);

            // ===== Últimos movimientos =====
            UltimosEnvios = _context.Envios
                .AsNoTracking()
                .OrderByDescending(e => e.FechaCreacion)
                .Take(5)
                .Select(e => new EnvioRecienteItem
                {
                    Id = e.Id,
                    Numero = e.Numero,
                    ClienteNombre = e.ClienteNombre,
                    Estado = e.Estado,
                    FechaCreacion = e.FechaCreacion,
                    ZonaNombre = e.ZonaNombre
                })
                .ToList();

            UltimasRutas = _context.Rutas
                .AsNoTracking()
                .OrderByDescending(r => r.FechaCreacion)
                .Take(5)
                .Select(r => new RutaRecienteItem
                {
                    Id = r.Id,
                    Numero = r.Numero,
                    RepartidorNombre = r.RepartidorNombre,
                    Estado = r.Estado,
                    Fecha = r.Fecha,
                    TotalParadas = r.TotalParadas
                })
                .ToList();

            return Page();
        }

        // ============================================================
        // HELPERS DE MAPEO
        // ============================================================

        private List<EnvioAlertaItem> MapEnvios(List<Envio> envios)
        {
            return envios.Select(e => new EnvioAlertaItem
            {
                Id = e.Id,
                Numero = e.Numero,
                ClienteNombre = e.ClienteNombre,
                DireccionEntrega = e.DireccionEntrega,
                ZonaNombre = e.ZonaNombre,
                Estado = e.Estado,
                FechaEntregaEstimada = e.FechaEntregaEstimada,
                FechaEntregaReal = e.FechaEntregaReal,
                RepartidorNombre = e.RepartidorNombre,
                DiasDiferencia = AlertaEnvioHelper.DiasDiferencia(e),
                Semaforo = AlertaEnvioHelper.CalcularSemaforo(e)
            }).ToList();
        }

        private List<RutaAlertaItem> MapRutas(List<Ruta> rutas)
        {
            if (rutas.Count == 0) return new List<RutaAlertaItem>();

            var hoy = DateTime.Today;
            var rutaIds = rutas.Select(r => r.Id).ToList();

            // Traer todas las paradas de todas las rutas de una vez
            var todosEnvios = _context.Envios
                .AsNoTracking()
                .Where(e => e.RutaId.HasValue && rutaIds.Contains(e.RutaId.Value))
                .ToList();

            return rutas.Select(r =>
            {
                var paradas = todosEnvios.Where(e => e.RutaId == r.Id).ToList();

                return new RutaAlertaItem
                {
                    Id = r.Id,
                    Numero = r.Numero,
                    RepartidorNombre = r.RepartidorNombre,
                    Vehiculo = r.Vehiculo,
                    Estado = r.Estado,
                    FechaSalida = r.FechaSalida,
                    TotalParadas = paradas.Count,
                    ParadasEntregadas = paradas.Count(e => e.Estado == "Entregado"),
                    ParadasFallidas = paradas.Count(e => e.Estado == "Fallido"),
                    ParadasPendientes = paradas.Count(e => e.Estado == "Pendiente" || e.Estado == "EnRuta"),
                    ParadasAtrasadas = paradas.Count(e =>
                        (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                        && e.FechaEntregaEstimada.HasValue
                        && e.FechaEntregaEstimada.Value.Date < hoy)
                };
            }).ToList();
        }
    }
}