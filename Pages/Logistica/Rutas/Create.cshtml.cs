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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string NumeroPreview { get; set; } = "";

        // ===== Catálogos =====
        public List<Repartidor> Repartidores { get; set; } = new();
        public List<ZonaEnvio> Zonas { get; set; } = new();

        // ===== Envíos disponibles para agrupar =====
        public List<EnvioDisponibleItem> EnviosDisponibles { get; set; } = new();

        // ===== Si viene pre-seleccionado desde otra página =====
        public int? EnvioIdPreseleccionado { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Debes seleccionar al menos un envío")]
            public List<int> EnviosSeleccionados { get; set; } = new();

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
            public string? ContactoTelefono { get; set; }
            public DateTime? FechaEntregaEstimada { get; set; }
            public DateTime FechaCreacion { get; set; }
            public decimal Monto { get; set; }
            public string Semaforo { get; set; } = "Verde";
            public int? DiasDiferencia { get; set; }
        }

        public IActionResult OnGet(int? envioId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RutasCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear rutas";
                return RedirectToPage("/Logistica/Rutas/Index");
            }

            if (envioId.HasValue)
            {
                EnvioIdPreseleccionado = envioId.Value;
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RutasCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear rutas";
                return RedirectToPage("/Logistica/Rutas/Index");
            }

            CargarDatos();

            if (Input.EnviosSeleccionados == null || Input.EnviosSeleccionados.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Debes seleccionar al menos un envío para la ruta");
                return Page();
            }

            // Si hay repartidor, cargarlo
            string? repartidorNombre = Input.RepartidorNombre;
            string? vehiculo = Input.Vehiculo;
            string? placa = Input.Placa;

            if (Input.RepartidorId.HasValue)
            {
                var rep = _context.Repartidores.FirstOrDefault(r => r.Id == Input.RepartidorId.Value);
                if (rep == null)
                {
                    ModelState.AddModelError("Input.RepartidorId", "Repartidor no encontrado");
                    return Page();
                }

                repartidorNombre = rep.Nombre;
                vehiculo ??= rep.Vehiculo;
                placa ??= rep.Placa;
            }

            if (!ModelState.IsValid) return Page();

            var (ruta, error) = RutaHelper.Crear(
                _context,
                Input.EnviosSeleccionados,
                Input.RepartidorId,
                repartidorNombre,
                vehiculo,
                placa,
                Input.ZonaId,
                Input.Descripcion,
                Input.FechaEntregaEstimada,
                Input.Notas,
                currentUser?.Id
            );

            if (error != null || ruta == null)
            {
                ModelState.AddModelError(string.Empty, error ?? "Error al crear la ruta");
                return Page();
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear ruta",
                $"Creó la ruta {ruta.Numero} con {Input.EnviosSeleccionados.Count} parada(s)",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Ruta {ruta.Numero} creada con {Input.EnviosSeleccionados.Count} parada(s). Lista para despachar.";
            return RedirectToPage("/Logistica/Rutas/Details", new { id = ruta.Id });
        }

        private void CargarDatos()
        {
            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Ruta");

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

            // Envíos disponibles: Pendientes sin ruta asignada
            var hoy = DateTime.Today;

            var envios = _context.Envios
                .AsNoTracking()
                .Where(e => e.Estado == "Pendiente" && e.RutaId == null)
                .OrderBy(e => e.FechaEntregaEstimada)
                .ThenBy(e => e.Id)
                .ToList();

            EnviosDisponibles = envios.Select(e => new EnvioDisponibleItem
            {
                Id = e.Id,
                Numero = e.Numero,
                ClienteNombre = e.ClienteNombre,
                DireccionEntrega = e.DireccionEntrega,
                Ciudad = e.Ciudad,
                ZonaId = e.ZonaId,
                ZonaNombre = e.ZonaNombre,
                ContactoTelefono = e.ContactoTelefono,
                FechaEntregaEstimada = e.FechaEntregaEstimada,
                FechaCreacion = e.FechaCreacion,
                Monto = e.Monto,
                Semaforo = AlertaEnvioHelper.CalcularSemaforo(e),
                DiasDiferencia = AlertaEnvioHelper.DiasDiferencia(e)
            }).ToList();
        }
    }
}