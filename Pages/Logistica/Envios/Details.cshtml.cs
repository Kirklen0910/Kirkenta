using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Logistica;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Logistica.Envios
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Envio Envio { get; set; } = new();
        public Ruta? Ruta { get; set; }
        public Repartidor? Repartidor { get; set; }
        public ZonaEnvio? Zona { get; set; }
        public Venta? VentaOrigen { get; set; }
        public Factura? FacturaOrigen { get; set; }
        public Pedido? PedidoOrigen { get; set; }
        public Cotizacion? CotizacionOrigen { get; set; }
        public ConfiguracionEmpresa Empresa { get; set; } = new();

        public List<RutaHistorial> Historial { get; set; } = new();

        // ===== Alertas / semáforo =====
        public string Semaforo { get; set; } = "Verde";
        public int? DiasDiferencia { get; set; }
        public string EtiquetaSemaforo { get; set; } = "";

        // ===== Permisos =====
        public bool PuedeEditar { get; set; }
        public bool PuedeCrearRuta { get; set; }

        // ===== Regla de negocio =====
        /// <summary>
        /// Indica si el envío está listo para procesarse (tiene ruta asignada).
        /// Si no tiene ruta, NO se puede marcar entregado ni fallido.
        /// </summary>
        public bool PuedeProcesar { get; set; }

        // ===== Inputs de acciones =====
        [BindProperty]
        public string? NombreRecibio { get; set; }

        [BindProperty]
        public string? NotasAccion { get; set; }

        [BindProperty]
        public string? MotivoFallo { get; set; }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Envios", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver este envío";
                return RedirectToPage("/Logistica/Envios/Index");
            }

            PuedeEditar = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Envios", "editar");
            PuedeCrearRuta = currentRol == "Admin" ||
                PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RutasCreate", "crear");

            var envio = _context.Envios.FirstOrDefault(e => e.Id == id);
            if (envio == null)
            {
                TempData["Error"] = "Envío no encontrado";
                return RedirectToPage("/Logistica/Envios/Index");
            }

            CargarDatos(envio);

            // ===== REGLA DE NEGOCIO =====
            // Solo se puede marcar entregado/fallido si el envío tiene ruta asignada.
            // El envío sin ruta debe planificarse primero.
            PuedeProcesar = envio.RutaId.HasValue;

            return Page();
        }

        // ============================================================
        // ACCIÓN: MARCAR ENTREGADO
        // ============================================================
        public IActionResult OnPostMarcarEntregado(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Envios", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Logistica/Envios/Index");
            }

            var envio = _context.Envios.FirstOrDefault(e => e.Id == id);
            if (envio == null)
            {
                TempData["Error"] = "Envío no encontrado";
                return RedirectToPage("/Logistica/Envios/Index");
            }

            // ===== VALIDACIÓN: debe tener ruta =====
            if (!envio.RutaId.HasValue)
            {
                TempData["Error"] = "Este envío debe asignarse a una ruta y despacharse antes de poder marcarse como entregado.";
                return RedirectToPage("/Logistica/Envios/Details", new { id });
            }

            if (string.IsNullOrWhiteSpace(NombreRecibio))
            {
                TempData["Error"] = "Debes indicar el nombre de quien recibe";
                return RedirectToPage("/Logistica/Envios/Details", new { id });
            }

            var (ok, error) = EnvioHelper.MarcarEntregado(
                _context, id, NombreRecibio, null, null,
                NotasAccion, currentUser?.Id);

            if (!ok)
            {
                TempData["Error"] = error;
                return RedirectToPage("/Logistica/Envios/Details", new { id });
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Marcar envío entregado",
                $"Marcó el envío {envio.Numero} como entregado a {NombreRecibio}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Envío {envio.Numero} marcado como entregado";
            return RedirectToPage("/Logistica/Envios/Details", new { id });
        }

        // ============================================================
        // ACCIÓN: MARCAR FALLIDO
        // ============================================================
        public IActionResult OnPostMarcarFallido(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Envios", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Logistica/Envios/Index");
            }

            var envio = _context.Envios.FirstOrDefault(e => e.Id == id);
            if (envio == null)
            {
                TempData["Error"] = "Envío no encontrado";
                return RedirectToPage("/Logistica/Envios/Index");
            }

            // ===== VALIDACIÓN: debe tener ruta =====
            if (!envio.RutaId.HasValue)
            {
                TempData["Error"] = "Este envío debe asignarse a una ruta y despacharse antes de poder marcarse como fallido.";
                return RedirectToPage("/Logistica/Envios/Details", new { id });
            }

            if (string.IsNullOrWhiteSpace(MotivoFallo))
            {
                TempData["Error"] = "Debes indicar el motivo del fallo";
                return RedirectToPage("/Logistica/Envios/Details", new { id });
            }

            var (ok, error) = EnvioHelper.MarcarFallido(_context, id, MotivoFallo, currentUser?.Id);

            if (!ok)
            {
                TempData["Error"] = error;
                return RedirectToPage("/Logistica/Envios/Details", new { id });
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Marcar envío fallido",
                $"Marcó el envío {envio.Numero} como fallido. Motivo: {MotivoFallo}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Envío {envio.Numero} marcado como fallido";
            return RedirectToPage("/Logistica/Envios/Details", new { id });
        }

        // ============================================================
        // ACCIÓN: REAGENDAR
        // ============================================================
        public IActionResult OnPostReagendar(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Envios", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Logistica/Envios/Index");
            }

            var envio = _context.Envios.FirstOrDefault(e => e.Id == id);
            if (envio == null)
            {
                TempData["Error"] = "Envío no encontrado";
                return RedirectToPage("/Logistica/Envios/Index");
            }

            // El reagendar sí se permite sin ruta (porque el envío ya estaba en una ruta y falló)
            // El propio EnvioHelper.Reagendar limpia la ruta anterior
            var (ok, error) = EnvioHelper.Reagendar(_context, id, NotasAccion, currentUser?.Id);

            if (!ok)
            {
                TempData["Error"] = error;
                return RedirectToPage("/Logistica/Envios/Details", new { id });
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Reagendar envío",
                $"Reagendó el envío {envio.Numero}. Notas: {NotasAccion ?? "—"}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Envío {envio.Numero} reagendado. Ahora está en estado Pendiente.";
            return RedirectToPage("/Logistica/Envios/Details", new { id });
        }

        // ============================================================
        // CARGA DE DATOS
        // ============================================================
        private void CargarDatos(Envio envio)
        {
            Envio = envio;
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault()
                ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };

            if (envio.RutaId.HasValue)
            {
                Ruta = _context.Rutas.FirstOrDefault(r => r.Id == envio.RutaId.Value);
            }

            if (envio.RepartidorId.HasValue)
            {
                Repartidor = _context.Repartidores.FirstOrDefault(r => r.Id == envio.RepartidorId.Value);
            }

            if (envio.ZonaId.HasValue)
            {
                Zona = _context.ZonasEnvio.FirstOrDefault(z => z.Id == envio.ZonaId.Value);
            }

            if (envio.VentaId.HasValue)
            {
                VentaOrigen = _context.Ventas.FirstOrDefault(v => v.Id == envio.VentaId.Value);
            }

            if (envio.FacturaId.HasValue)
            {
                FacturaOrigen = _context.Facturas.FirstOrDefault(f => f.Id == envio.FacturaId.Value);
            }

            if (envio.PedidoId.HasValue)
            {
                PedidoOrigen = _context.Pedidos.FirstOrDefault(p => p.Id == envio.PedidoId.Value);
            }

            if (envio.CotizacionId.HasValue)
            {
                CotizacionOrigen = _context.Cotizaciones.FirstOrDefault(c => c.Id == envio.CotizacionId.Value);
            }

            // Historial de eventos
            if (envio.RutaId.HasValue)
            {
                Historial = _context.RutasHistorial
                    .Where(h => h.EnvioId == envio.Id)
                    .OrderByDescending(h => h.Fecha)
                    .ToList();
            }

            // Semaforo
            Semaforo = AlertaEnvioHelper.CalcularSemaforo(envio);
            DiasDiferencia = AlertaEnvioHelper.DiasDiferencia(envio);
            EtiquetaSemaforo = AlertaEnvioHelper.EtiquetaSemaforo(Semaforo);
        }
    }
}