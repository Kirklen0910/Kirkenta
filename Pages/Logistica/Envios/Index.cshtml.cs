using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Logistica;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Logistica.Envios
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== Datos =====
        public List<EnvioItem> Envios { get; set; } = new();
        public List<Ruta> RutasDisponibles { get; set; } = new();
        public List<Repartidor> RepartidoresDisponibles { get; set; } = new();

        // ===== Filtros activos =====
        public string? FiltroEstado { get; set; }
        public string? FiltroAlerta { get; set; }
        public string? FiltroBusqueda { get; set; }
        public int? FiltroZonaId { get; set; }
        public DateTime? FiltroDesde { get; set; }
        public DateTime? FiltroHasta { get; set; }

        // ===== KPIs =====
        public int TotalPendientes { get; set; }
        public int TotalEnRuta { get; set; }
        public int TotalEntregados { get; set; }
        public int TotalFallidos { get; set; }
        public int TotalAtrasados { get; set; }
        public int TotalVencenHoy { get; set; }

        // ===== Catálogos para filtros =====
        public List<ZonaEnvio> Zonas { get; set; } = new();

        // ===== Permisos =====
        public bool PuedeCrearRuta { get; set; }
        public bool PuedeEditar { get; set; }

        public class EnvioItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public string ClienteNombre { get; set; } = "";
            public string DireccionEntrega { get; set; } = "";
            public string? Referencia { get; set; }
            public string ContactoNombre { get; set; } = "";
            public string ContactoTelefono { get; set; } = "";
            public string? Ciudad { get; set; }
            public int? ZonaId { get; set; }
            public string? ZonaNombre { get; set; }
            public string Estado { get; set; } = "";
            public DateTime? FechaEntregaEstimada { get; set; }
            public DateTime? FechaEntregaReal { get; set; }
            public DateTime FechaCreacion { get; set; }
            public int? RutaId { get; set; }
            public string? RutaNumero { get; set; }
            public int? OrdenParada { get; set; }
            public string? RepartidorNombre { get; set; }
            public int? VentaId { get; set; }
            public int? FacturaId { get; set; }
            public int? PedidoId { get; set; }
            public int? CotizacionId { get; set; }
            public decimal Monto { get; set; }
            public string Semaforo { get; set; } = "Verde";
            public int? DiasDiferencia { get; set; }
        }

        public IActionResult OnGet(string? filtro, string? estado, string? busqueda, int? zonaId, DateTime? desde, DateTime? hasta)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Envios", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver envíos";
                return RedirectToPage("/Logistica/Index");
            }

            PuedeCrearRuta = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RutasCreate", "crear");
            PuedeEditar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Envios", "editar");

            // Filtros
            FiltroEstado = estado;
            FiltroAlerta = filtro;
            FiltroBusqueda = busqueda;
            FiltroZonaId = zonaId;
            FiltroDesde = desde;
            FiltroHasta = hasta;

            Zonas = _context.ZonasEnvio
                .AsNoTracking()
                .Where(z => z.Activa)
                .OrderBy(z => z.Orden).ThenBy(z => z.Nombre)
                .ToList();

            // Query base
            var query = _context.Envios.AsNoTracking().AsQueryable();

            // Filtro por estado
            if (!string.IsNullOrWhiteSpace(estado))
            {
                query = query.Where(e => e.Estado == estado);
            }

            // Filtros de fecha (por fecha de creación del envío)
            if (desde.HasValue)
            {
                query = query.Where(e => e.FechaCreacion >= desde.Value.Date);
            }
            if (hasta.HasValue)
            {
                var hastaFin = hasta.Value.Date.AddDays(1);
                query = query.Where(e => e.FechaCreacion < hastaFin);
            }

            // Filtro por zona
            if (zonaId.HasValue)
            {
                query = query.Where(e => e.ZonaId == zonaId.Value);
            }

            // Búsqueda por texto
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var b = busqueda.Trim().ToLower();
                query = query.Where(e =>
                    e.Numero.ToLower().Contains(b)
                    || e.ClienteNombre.ToLower().Contains(b)
                    || e.DireccionEntrega.ToLower().Contains(b)
                    || e.ContactoNombre.ToLower().Contains(b)
                    || e.ContactoTelefono.ToLower().Contains(b));
            }

            // Filtro de alerta (semaforo)
            var hoy = DateTime.Today;
            if (filtro == "atrasados")
            {
                query = query.Where(e => (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                                      && e.FechaEntregaEstimada.HasValue
                                      && e.FechaEntregaEstimada.Value.Date < hoy);
            }
            else if (filtro == "vencenHoy")
            {
                query = query.Where(e => (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                                      && e.FechaEntregaEstimada.HasValue
                                      && e.FechaEntregaEstimada.Value.Date == hoy);
            }
            else if (filtro == "pendientes")
            {
                query = query.Where(e => e.Estado == "Pendiente");
            }
            else if (filtro == "enruta")
            {
                query = query.Where(e => e.Estado == "EnRuta");
            }
            else if (filtro == "entregados")
            {
                query = query.Where(e => e.Estado == "Entregado");
            }
            else if (filtro == "fallidos")
            {
                query = query.Where(e => e.Estado == "Fallido");
            }
            else if (filtro == "sinRuta")
            {
                query = query.Where(e => e.RutaId == null && e.Estado == "Pendiente");
            }

            var envios = query
                .OrderByDescending(e => e.FechaCreacion)
                .ToList();

            // Cargar rutas para lookup
            var rutaIds = envios.Where(e => e.RutaId.HasValue).Select(e => e.RutaId!.Value).Distinct().ToList();
            var rutasDict = rutaIds.Count > 0
                ? _context.Rutas.AsNoTracking()
                    .Where(r => rutaIds.Contains(r.Id))
                    .ToDictionary(r => r.Id, r => r.Numero)
                : new Dictionary<int, string>();

            // Mapear items
            Envios = envios.Select(e => new EnvioItem
            {
                Id = e.Id,
                Numero = e.Numero,
                ClienteNombre = e.ClienteNombre,
                DireccionEntrega = e.DireccionEntrega,
                Referencia = e.Referencia,
                ContactoNombre = e.ContactoNombre,
                ContactoTelefono = e.ContactoTelefono,
                Ciudad = e.Ciudad,
                ZonaId = e.ZonaId,
                ZonaNombre = e.ZonaNombre,
                Estado = e.Estado,
                FechaEntregaEstimada = e.FechaEntregaEstimada,
                FechaEntregaReal = e.FechaEntregaReal,
                FechaCreacion = e.FechaCreacion,
                RutaId = e.RutaId,
                RutaNumero = e.RutaId.HasValue ? rutasDict.GetValueOrDefault(e.RutaId.Value) : null,
                OrdenParada = e.OrdenParada,
                RepartidorNombre = e.RepartidorNombre,
                VentaId = e.VentaId,
                FacturaId = e.FacturaId,
                PedidoId = e.PedidoId,
                CotizacionId = e.CotizacionId,
                Monto = e.Monto,
                Semaforo = AlertaEnvioHelper.CalcularSemaforo(e),
                DiasDiferencia = AlertaEnvioHelper.DiasDiferencia(e)
            }).ToList();

            // KPIs (calculados sobre el universo completo, no sobre el filtro)
            var todos = _context.Envios.AsNoTracking().ToList();
            TotalPendientes = todos.Count(e => e.Estado == "Pendiente");
            TotalEnRuta = todos.Count(e => e.Estado == "EnRuta");
            TotalEntregados = todos.Count(e => e.Estado == "Entregado");
            TotalFallidos = todos.Count(e => e.Estado == "Fallido");

            TotalAtrasados = todos.Count(e =>
                (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                && e.FechaEntregaEstimada.HasValue
                && e.FechaEntregaEstimada.Value.Date < hoy);

            TotalVencenHoy = todos.Count(e =>
                (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                && e.FechaEntregaEstimada.HasValue
                && e.FechaEntregaEstimada.Value.Date == hoy);

            return Page();
        }
    }
}