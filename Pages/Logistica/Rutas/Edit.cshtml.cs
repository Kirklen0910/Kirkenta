using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Logistica;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Logistica.Rutas
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string Numero { get; set; } = "";

        // ===== Catálogos =====
        public List<Repartidor> Repartidores { get; set; } = new();
        public List<ZonaEnvio> Zonas { get; set; } = new();

        // ===== Envíos ya en la ruta (paradas actuales) =====
        public List<EnvioEnRutaItem> EnviosEnRuta { get; set; } = new();

        // ===== Envíos pendientes disponibles para agregar =====
        public List<EnvioDisponibleItem> EnviosDisponibles { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            public int? RepartidorId { get; set; }

            [StringLength(150)]
            public string? RepartidorNombre { get; set; }

            [StringLength(100)]
            public string? Vehiculo { get; set; }

            [StringLength(20)]
            public string? Placa { get; set; }

            public int? ZonaId { get; set; }

            [StringLength(300)]
            public string? Descripcion { get; set; }

            public DateTime? FechaEntregaEstimada { get; set; }

            [StringLength(500)]
            public string? Notas { get; set; }

            // Envíos seleccionados actualmente (ya en la ruta o por agregar)
            public List<int> EnviosSeleccionados { get; set; } = new();

            // IDs de envíos que se deben quitar
            public List<int> EnviosAQuitar { get; set; } = new();
        }

        public class EnvioEnRutaItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public int? OrdenParada { get; set; }
            public string ClienteNombre { get; set; } = "";
            public string DireccionEntrega { get; set; } = "";
            public string? Ciudad { get; set; }
            public string? ZonaNombre { get; set; }
            public string Estado { get; set; } = "";
            public DateTime? FechaEntregaEstimada { get; set; }
            public decimal Monto { get; set; }
            public string Semaforo { get; set; } = "Verde";
        }

        public class EnvioDisponibleItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public string ClienteNombre { get; set; } = "";
            public string DireccionEntrega { get; set; } = "";
            public string? Ciudad { get; set; }
            public int? ZonaId { get; set; }
            public string? ZonaNombre { get; set; }
            public DateTime? FechaEntregaEstimada { get; set; }
            public decimal Monto { get; set; }
            public string Semaforo { get; set; } = "Verde";
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RutasEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar rutas";
                return RedirectToPage("/Logistica/Rutas/Index");
            }

            var ruta = _context.Rutas.FirstOrDefault(r => r.Id == id);
            if (ruta == null)
            {
                TempData["Error"] = "Ruta no encontrada";
                return RedirectToPage("/Logistica/Rutas/Index");
            }

            if (ruta.Estado != "Borrador")
            {
                TempData["Error"] = "Solo se pueden editar rutas en estado Borrador";
                return RedirectToPage("/Logistica/Rutas/Details", new { id });
            }

            Numero = ruta.Numero;

            Input = new InputModel
            {
                Id = ruta.Id,
                RepartidorId = ruta.RepartidorId,
                RepartidorNombre = ruta.RepartidorNombre,
                Vehiculo = ruta.Vehiculo,
                Placa = ruta.Placa,
                ZonaId = ruta.ZonaId,
                Descripcion = ruta.Descripcion,
                Notas = ruta.Notas
            };

            CargarDatos(ruta);
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RutasEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar rutas";
                return RedirectToPage("/Logistica/Rutas/Index");
            }

            var ruta = _context.Rutas.FirstOrDefault(r => r.Id == Input.Id);
            if (ruta == null)
            {
                TempData["Error"] = "Ruta no encontrada";
                return RedirectToPage("/Logistica/Rutas/Index");
            }

            if (ruta.Estado != "Borrador")
            {
                TempData["Error"] = "Solo se pueden editar rutas en Borrador";
                return RedirectToPage("/Logistica/Rutas/Details", new { id = ruta.Id });
            }

            Numero = ruta.Numero;
            CargarDatos(ruta);

            // Validar que la ruta quede con al menos 1 parada
            var enviosActuales = _context.Envios
                .Where(e => e.RutaId == ruta.Id)
                .Select(e => e.Id)
                .ToList();

            var aQuitar = Input.EnviosAQuitar ?? new List<int>();
            var seleccionados = Input.EnviosSeleccionados ?? new List<int>();

            var quedan = enviosActuales.Where(id => !aQuitar.Contains(id)).ToList();
            var totalFinal = quedan.Count + seleccionados.Count;

            if (totalFinal == 0)
            {
                ModelState.AddModelError(string.Empty, "La ruta debe tener al menos una parada. Si quieres eliminar todas, cancela la ruta.");
                return Page();
            }

            if (!ModelState.IsValid) return Page();

            // Resolver repartidor
            string? repartidorNombre = Input.RepartidorNombre;
            string? vehiculo = Input.Vehiculo;
            string? placa = Input.Placa;

            if (Input.RepartidorId.HasValue)
            {
                var rep = _context.Repartidores.FirstOrDefault(r => r.Id == Input.RepartidorId.Value);
                if (rep != null)
                {
                    repartidorNombre = rep.Nombre;
                    vehiculo ??= rep.Vehiculo;
                    placa ??= rep.Placa;
                }
            }

            // Actualizar datos de la ruta
            ruta.RepartidorId = Input.RepartidorId;
            ruta.RepartidorNombre = repartidorNombre ?? "";
            ruta.Vehiculo = vehiculo;
            ruta.Placa = placa;
            ruta.ZonaId = Input.ZonaId;
            ruta.ZonaNombre = Input.ZonaId.HasValue
                ? _context.ZonasEnvio.FirstOrDefault(z => z.Id == Input.ZonaId.Value)?.Nombre
                : null;
            ruta.Descripcion = Input.Descripcion;
            ruta.Notas = Input.Notas;

            // Quitar envíos marcados
            foreach (var envioId in aQuitar)
            {
                var envio = _context.Envios.FirstOrDefault(e => e.Id == envioId && e.RutaId == ruta.Id);
                if (envio != null)
                {
                    envio.RutaId = null;
                    envio.OrdenParada = null;
                }
            }

            // ===== FIX EF Core 9: evitar DefaultIfEmpty en query =====
            var ordenesActuales = _context.Envios
                .Where(e => e.RutaId == ruta.Id)
                .Select(e => e.OrdenParada ?? 0)
                .ToList();

            var maxOrden = ordenesActuales.Any() ? ordenesActuales.Max() : 0;

            // Agregar nuevos envíos
            foreach (var envioId in seleccionados)
            {
                var envio = _context.Envios.FirstOrDefault(e => e.Id == envioId && e.RutaId == null && e.Estado == "Pendiente");
                if (envio != null)
                {
                    maxOrden++;
                    envio.RutaId = ruta.Id;
                    envio.OrdenParada = maxOrden;

                    if (Input.FechaEntregaEstimada.HasValue)
                    {
                        envio.FechaEntregaEstimada = Input.FechaEntregaEstimada;
                    }
                }
            }

            _context.SaveChanges();

            // Recalcular totales
            RutaHelper.RecalcularTotales(_context, ruta.Id);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar ruta",
                $"Editó la ruta {ruta.Numero}. Paradas finales: {totalFinal}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Ruta {ruta.Numero} actualizada. Ahora tiene {totalFinal} parada(s).";
            return RedirectToPage("/Logistica/Rutas/Details", new { id = ruta.Id });
        }

        private void CargarDatos(Ruta ruta)
        {
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

            // Envíos actualmente en la ruta
            var enviosEnRuta = _context.Envios
                .AsNoTracking()
                .Where(e => e.RutaId == ruta.Id)
                .OrderBy(e => e.OrdenParada)
                .ThenBy(e => e.Id)
                .ToList();

            EnviosEnRuta = enviosEnRuta.Select(e => new EnvioEnRutaItem
            {
                Id = e.Id,
                Numero = e.Numero,
                OrdenParada = e.OrdenParada,
                ClienteNombre = e.ClienteNombre,
                DireccionEntrega = e.DireccionEntrega,
                Ciudad = e.Ciudad,
                ZonaNombre = e.ZonaNombre,
                Estado = e.Estado,
                FechaEntregaEstimada = e.FechaEntregaEstimada,
                Monto = e.Monto,
                Semaforo = AlertaEnvioHelper.CalcularSemaforo(e)
            }).ToList();

            // Envíos pendientes disponibles para agregar
            var enviosDisponibles = _context.Envios
                .AsNoTracking()
                .Where(e => e.Estado == "Pendiente" && e.RutaId == null)
                .OrderBy(e => e.FechaEntregaEstimada)
                .ThenBy(e => e.Id)
                .ToList();

            EnviosDisponibles = enviosDisponibles.Select(e => new EnvioDisponibleItem
            {
                Id = e.Id,
                Numero = e.Numero,
                ClienteNombre = e.ClienteNombre,
                DireccionEntrega = e.DireccionEntrega,
                Ciudad = e.Ciudad,
                ZonaId = e.ZonaId,
                ZonaNombre = e.ZonaNombre,
                FechaEntregaEstimada = e.FechaEntregaEstimada,
                Monto = e.Monto,
                Semaforo = AlertaEnvioHelper.CalcularSemaforo(e)
            }).ToList();
        }
    }
}