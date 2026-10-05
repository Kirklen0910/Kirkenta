using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Logistica;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Logistica.Rutas
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== Datos de la ruta =====
        public Ruta Ruta { get; set; } = new();
        public Repartidor? Repartidor { get; set; }
        public ZonaEnvio? Zona { get; set; }
        public ConfiguracionEmpresa Empresa { get; set; } = new();

        // ===== Paradas =====
        public List<ParadaItem> Paradas { get; set; } = new();

        // ===== Historial de la ruta =====
        public List<RutaHistorial> Historial { get; set; } = new();

        // ===== KPIs =====
        public int TotalParadas { get; set; }
        public int ParadasEntregadas { get; set; }
        public int ParadasFallidas { get; set; }
        public int ParadasPendientes { get; set; }
        public int ParadasAtrasadas { get; set; }
        public int Progreso { get; set; }
        public decimal MontoTotal { get; set; }

        // ===== Alertas =====
        public bool EsAtrasada { get; set; }
        public string EtiquetaEstado { get; set; } = "";

        // ===== Permisos =====
        public bool PuedeEditar { get; set; }
        public bool PuedeDespachar { get; set; }
        public bool PuedeCerrar { get; set; }
        public bool PuedeCancelar { get; set; }

        // ===== Inputs para acciones =====
        [BindProperty]
        public string? NombreRecibio { get; set; }

        [BindProperty]
        public string? NotasAccion { get; set; }

        [BindProperty]
        public string? MotivoFallo { get; set; }

        [BindProperty]
        public string? MotivoCancelacion { get; set; }

        public class ParadaItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public int? OrdenParada { get; set; }
            public string ClienteNombre { get; set; } = "";
            public string DireccionEntrega { get; set; } = "";
            public string? Referencia { get; set; }
            public string? Ciudad { get; set; }
            public string ContactoNombre { get; set; } = "";
            public string ContactoTelefono { get; set; } = "";
            public string? ZonaNombre { get; set; }
            public string Estado { get; set; } = "";
            public DateTime? FechaEntregaEstimada { get; set; }
            public DateTime? FechaEntregaReal { get; set; }
            public string? NombreRecibio { get; set; }
            public string? MotivoFallo { get; set; }
            public decimal Monto { get; set; }
            public string Semaforo { get; set; } = "Verde";
            public int? DiasDiferencia { get; set; }
            public bool EsAtrasada { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Rutas", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver esta ruta";
                return RedirectToPage("/Logistica/Rutas/Index");
            }

            var ruta = _context.Rutas.FirstOrDefault(r => r.Id == id);
            if (ruta == null)
            {
                TempData["Error"] = "Ruta no encontrada";
                return RedirectToPage("/Logistica/Rutas/Index");
            }

            CargarPermisos(currentRol);
            CargarDatos(ruta);
            return Page();
        }

        // ============================================================
        // ACCIÓN: DESPACHAR RUTA
        // ============================================================
        public IActionResult OnPostDespachar(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RutasDespachar", "editar"))
            {
                TempData["Error"] = "No tienes permiso para despachar rutas";
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            var ruta = _context.Rutas.FirstOrDefault(r => r.Id == id);
            if (ruta == null)
            {
                TempData["Error"] = "Ruta no encontrada";
                return RedirectToPage("/Logistica/Rutas/Index");
            }

            var (ok, error) = RutaHelper.Despachar(_context, id, currentUser?.Id);

            if (!ok)
            {
                TempData["Error"] = error;
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Despachar ruta",
                $"Despachó la ruta {ruta.Numero} con {ruta.TotalParadas} parada(s)",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Ruta {ruta.Numero} despachada. Los envíos ahora están en ruta.";
            return RedirectToPage("/Logistica/Rutas/Details", new { id });
        }

        // ============================================================
        // ACCIÓN: CERRAR RUTA
        // ============================================================
        public IActionResult OnPostCerrar(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RutasCerrar", "editar"))
            {
                TempData["Error"] = "No tienes permiso para cerrar rutas";
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            var ruta = _context.Rutas.FirstOrDefault(r => r.Id == id);
            if (ruta == null)
            {
                TempData["Error"] = "Ruta no encontrada";
                return RedirectToPage("/Logistica/Rutas/Index");
            }

            var (ok, error) = RutaHelper.Cerrar(_context, id, NotasAccion, currentUser?.Id);

            if (!ok)
            {
                TempData["Error"] = error;
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Cerrar ruta",
                $"Cerró la ruta {ruta.Numero}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Ruta {ruta.Numero} cerrada correctamente";
            return RedirectToPage("/Logistica/Rutas/Details", new { id });
        }

        // ============================================================
        // ACCIÓN: CANCELAR RUTA
        // ============================================================
        public IActionResult OnPostCancelar(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RutasEdit", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para cancelar rutas";
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            var ruta = _context.Rutas.FirstOrDefault(r => r.Id == id);
            if (ruta == null)
            {
                TempData["Error"] = "Ruta no encontrada";
                return RedirectToPage("/Logistica/Rutas/Index");
            }

            if (string.IsNullOrWhiteSpace(MotivoCancelacion))
            {
                TempData["Error"] = "Debes indicar el motivo de la cancelación";
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            var (ok, error) = RutaHelper.Cancelar(_context, id, MotivoCancelacion, currentUser?.Id);

            if (!ok)
            {
                TempData["Error"] = error;
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Cancelar ruta",
                $"Canceló la ruta {ruta.Numero}. Motivo: {MotivoCancelacion}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Ruta {ruta.Numero} cancelada. Los envíos volvieron a Pendiente.";
            return RedirectToPage("/Logistica/Rutas/Index");
        }

        // ============================================================
        // ACCIÓN: MARCAR PARADA ENTREGADA
        // ============================================================
        public IActionResult OnPostMarcarEntregado(int id, int envioId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Envios", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            if (string.IsNullOrWhiteSpace(NombreRecibio))
            {
                TempData["Error"] = "Debes indicar el nombre de quien recibe";
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            var envio = _context.Envios.FirstOrDefault(e => e.Id == envioId && e.RutaId == id);
            if (envio == null)
            {
                TempData["Error"] = "Envío no encontrado en esta ruta";
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            var (ok, error) = EnvioHelper.MarcarEntregado(
                _context, envioId, NombreRecibio, null, null, NotasAccion, currentUser?.Id);

            if (!ok)
            {
                TempData["Error"] = error;
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Marcar parada entregada",
                $"Marcó el envío {envio.Numero} como entregado a {NombreRecibio} (ruta #{id})",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Envío {envio.Numero} marcado como entregado";
            return RedirectToPage("/Logistica/Rutas/Details", new { id });
        }

        // ============================================================
        // ACCIÓN: MARCAR PARADA FALLIDA
        // ============================================================
        public IActionResult OnPostMarcarFallido(int id, int envioId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "Envios", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            if (string.IsNullOrWhiteSpace(MotivoFallo))
            {
                TempData["Error"] = "Debes indicar el motivo del fallo";
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            var envio = _context.Envios.FirstOrDefault(e => e.Id == envioId && e.RutaId == id);
            if (envio == null)
            {
                TempData["Error"] = "Envío no encontrado en esta ruta";
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            var (ok, error) = EnvioHelper.MarcarFallido(_context, envioId, MotivoFallo, currentUser?.Id);

            if (!ok)
            {
                TempData["Error"] = error;
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Marcar parada fallida",
                $"Marcó el envío {envio.Numero} como fallido. Motivo: {MotivoFallo}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Envío {envio.Numero} marcado como fallido";
            return RedirectToPage("/Logistica/Rutas/Details", new { id });
        }

        // ============================================================
        // HELPERS
        // ============================================================
        private void CargarPermisos(string rol)
        {
            PuedeEditar = rol == "Admin" || PermisoHelper.TienePermiso(_context, rol, "Logistica", "RutasEdit", "editar");
            PuedeDespachar = rol == "Admin" || PermisoHelper.TienePermiso(_context, rol, "Logistica", "RutasDespachar", "editar");
            PuedeCerrar = rol == "Admin" || PermisoHelper.TienePermiso(_context, rol, "Logistica", "RutasCerrar", "editar");
            PuedeCancelar = rol == "Admin" || PermisoHelper.TienePermiso(_context, rol, "Logistica", "RutasEdit", "eliminar");
        }

        private void CargarDatos(Ruta ruta)
        {
            Ruta = ruta;
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault()
                ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };

            if (ruta.RepartidorId.HasValue)
            {
                Repartidor = _context.Repartidores.FirstOrDefault(r => r.Id == ruta.RepartidorId.Value);
            }

            if (ruta.ZonaId.HasValue)
            {
                Zona = _context.ZonasEnvio.FirstOrDefault(z => z.Id == ruta.ZonaId.Value);
            }

            // Cargar paradas ordenadas
            var hoy = DateTime.Today;

            var envios = _context.Envios
                .Where(e => e.RutaId == ruta.Id)
                .OrderBy(e => e.OrdenParada)
                .ThenBy(e => e.Id)
                .ToList();

            Paradas = envios.Select(e => new ParadaItem
            {
                Id = e.Id,
                Numero = e.Numero,
                OrdenParada = e.OrdenParada,
                ClienteNombre = e.ClienteNombre,
                DireccionEntrega = e.DireccionEntrega,
                Referencia = e.Referencia,
                Ciudad = e.Ciudad,
                ContactoNombre = e.ContactoNombre,
                ContactoTelefono = e.ContactoTelefono,
                ZonaNombre = e.ZonaNombre,
                Estado = e.Estado,
                FechaEntregaEstimada = e.FechaEntregaEstimada,
                FechaEntregaReal = e.FechaEntregaReal,
                NombreRecibio = e.NombreRecibio,
                MotivoFallo = e.MotivoFallo,
                Monto = e.Monto,
                Semaforo = AlertaEnvioHelper.CalcularSemaforo(e),
                DiasDiferencia = AlertaEnvioHelper.DiasDiferencia(e),
                EsAtrasada = (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                             && e.FechaEntregaEstimada.HasValue
                             && e.FechaEntregaEstimada.Value.Date < hoy
            }).ToList();

            // KPIs
            TotalParadas = Paradas.Count;
            ParadasEntregadas = Paradas.Count(p => p.Estado == "Entregado");
            ParadasFallidas = Paradas.Count(p => p.Estado == "Fallido");
            ParadasPendientes = Paradas.Count(p => p.Estado == "Pendiente" || p.Estado == "EnRuta");
            ParadasAtrasadas = Paradas.Count(p => p.EsAtrasada);
            MontoTotal = Paradas.Sum(p => p.Monto);

            Progreso = TotalParadas > 0
                ? (int)Math.Round((double)(ParadasEntregadas + ParadasFallidas) / TotalParadas * 100)
                : 0;

            // Alerta de atraso
            EsAtrasada = ruta.Estado == "EnReparto" && ParadasAtrasadas > 0;

            EtiquetaEstado = ruta.Estado switch
            {
                "Borrador" => "📝 Borrador",
                "EnReparto" => "🚚 En reparto",
                "Completada" => "✅ Completada",
                "Cancelada" => "❌ Cancelada",
                _ => ruta.Estado
            };

            // Historial
            Historial = _context.RutasHistorial
                .Where(h => h.RutaId == ruta.Id)
                .OrderByDescending(h => h.Fecha)
                .ToList();
        }
    }
}