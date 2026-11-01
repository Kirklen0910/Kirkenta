using System.ComponentModel.DataAnnotations;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Activos
{
    public class MantenimientoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MantenimientoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class NuevoModel
        {
            [Range(1, int.MaxValue, ErrorMessage = "Selecciona un activo")]
            public int ActivoFijoId { get; set; }
            public DateTime Fecha { get; set; } = DateTime.Today;
            [Required] public string Tipo { get; set; } = "Preventivo";
            [Required(ErrorMessage = "La descripción es obligatoria"), StringLength(300)]
            public string Descripcion { get; set; } = "";
            [StringLength(100)] public string? Proveedor { get; set; }
            public DateTime? ProximaFecha { get; set; }
        }

        [BindProperty(SupportsGet = true)] public int? ActivoId { get; set; }
        [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        [BindProperty] public NuevoModel Nuevo { get; set; } = new();

        public List<MantenimientoActivo> Registros { get; set; } = new();
        public List<ActivoFijo> ActivosLista { get; set; } = new();
        public List<CuentaFinanciera> Cuentas { get; set; } = new();
        public Dictionary<int, ActivoFijo> ActivosPorId { get; set; } = new();
        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public int Programados { get; set; }
        public int Vencidos { get; set; }
        public decimal CostoAnio { get; set; }

        private bool Permiso(string accion) =>
            PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Activos", "Mantenimiento", accion);

        private void Cargar()
        {
            PuedeCrear = Permiso("crear");
            PuedeEditar = Permiso("editar");

            ActivosLista = _context.ActivosFijos.Where(a => a.Estado == "Activo").OrderBy(a => a.Codigo).ToList();
            Cuentas = _context.CuentasFinancieras.Where(c => c.Activa).OrderBy(c => c.Nombre).ToList();

            var query = _context.MantenimientosActivo.AsQueryable();
            if (ActivoId.HasValue) query = query.Where(m => m.ActivoFijoId == ActivoId.Value);
            if (!string.IsNullOrWhiteSpace(Estado)) query = query.Where(m => m.Estado == Estado);

            Pager = new PagerInfo(Pagina, 50, query.Count());
            Registros = query.OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id)
                .Skip(Pager.Skip).Take(Pager.PageSize).ToList();

            var ids = Registros.Select(r => r.ActivoFijoId).Distinct().ToList();
            ActivosPorId = _context.ActivosFijos.Where(a => ids.Contains(a.Id)).ToDictionary(a => a.Id);

            var hoy = DateTime.Today;
            Programados = _context.MantenimientosActivo.Count(m => m.Estado == "Programado");
            Vencidos = _context.MantenimientosActivo.Count(m => m.Estado == "Programado" && m.Fecha < hoy);
            var inicioAnio = new DateTime(hoy.Year, 1, 1);
            CostoAnio = _context.MantenimientosActivo
                .Where(m => m.Estado == "Realizado" && m.Fecha >= inicioAnio)
                .Sum(m => (decimal?)m.Costo) ?? 0;
        }

        public IActionResult OnGet()
        {
            if (!Permiso("ver"))
            {
                TempData["Error"] = "No tienes permiso para ver el mantenimiento";
                return RedirectToPage("/Index");
            }
            if (ActivoId.HasValue) Nuevo.ActivoFijoId = ActivoId.Value;
            Cargar();
            return Page();
        }

        public IActionResult OnPostCrear()
        {
            if (!Permiso("crear")) { TempData["Error"] = "No tienes permiso para programar mantenimientos"; return RedirectToPage(); }

            if (!ModelState.IsValid || !_context.ActivosFijos.Any(a => a.Id == Nuevo.ActivoFijoId))
            {
                if (ModelState.IsValid) ModelState.AddModelError("Nuevo.ActivoFijoId", "Activo no válido");
                Cargar();
                return Page();
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            _context.MantenimientosActivo.Add(new MantenimientoActivo
            {
                ActivoFijoId = Nuevo.ActivoFijoId,
                Fecha = Nuevo.Fecha.Date,
                Tipo = Nuevo.Tipo == "Correctivo" ? "Correctivo" : "Preventivo",
                Descripcion = Nuevo.Descripcion.Trim(),
                Proveedor = Nuevo.Proveedor,
                ProximaFecha = Nuevo.ProximaFecha,
                Estado = "Programado",
                UsuarioId = usuario?.Id
            });
            _context.SaveChanges();
            TempData["Success"] = "Mantenimiento programado";
            return RedirectToPage();
        }

        /// <summary>Marca el mantenimiento como realizado. Si hay costo y cuenta, registra el egreso.</summary>
        public IActionResult OnPostCompletar(int id, decimal costo, int? cuentaId)
        {
            if (!Permiso("editar")) { TempData["Error"] = "No tienes permiso para editar mantenimientos"; return RedirectToPage(); }

            var m = _context.MantenimientosActivo.FirstOrDefault(x => x.Id == id);
            if (m == null) { TempData["Error"] = "Mantenimiento no encontrado"; return RedirectToPage(); }
            if (m.Estado != "Programado") { TempData["Error"] = "Solo se pueden completar mantenimientos programados"; return RedirectToPage(); }
            if (costo < 0) { TempData["Error"] = "El costo no puede ser negativo"; return RedirectToPage(); }
            if (costo > 0 && !cuentaId.HasValue) { TempData["Error"] = "Selecciona la cuenta de la que se pagó el mantenimiento"; return RedirectToPage(); }

            var activo = _context.ActivosFijos.FirstOrDefault(a => a.Id == m.ActivoFijoId);
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);

            using var tx = _context.Database.BeginTransaction();
            m.Estado = "Realizado";
            m.Costo = costo;

            if (costo > 0)
            {
                var cat = MovimientoManualHelper.ObtenerOCrearCategoria(_context, "Egreso", "Mantenimiento y reparaciones", "6.2.06", "#8b5cf6");
                var (mov, error) = MovimientoManualHelper.Crear(_context, "Egreso", DateTime.Now, cuentaId!.Value, null, cat.Id, costo,
                    $"Mantenimiento {activo?.Codigo}: {m.Descripcion}", activo?.Codigo, "MantenimientoActivo", m.Id, usuario?.Id);
                if (mov == null)
                {
                    tx.Rollback();
                    _context.ChangeTracker.Clear();
                    TempData["Error"] = $"No se pudo registrar el pago: {error}";
                    return RedirectToPage();
                }
                m.MovimientoFinancieroId = mov.Id;
            }
            _context.SaveChanges();
            tx.Commit();

            // Si indicó próxima fecha, deja programado el siguiente mantenimiento
            if (m.ProximaFecha.HasValue && m.ProximaFecha.Value.Date > DateTime.Today)
            {
                _context.MantenimientosActivo.Add(new MantenimientoActivo
                {
                    ActivoFijoId = m.ActivoFijoId,
                    Fecha = m.ProximaFecha.Value.Date,
                    Tipo = "Preventivo",
                    Descripcion = m.Descripcion,
                    Proveedor = m.Proveedor,
                    Estado = "Programado",
                    UsuarioId = usuario?.Id
                });
                _context.SaveChanges();
            }

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Mantenimiento realizado",
                $"Completó mantenimiento de {activo?.Codigo} por L. {costo:N2}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = "Mantenimiento registrado como realizado";
            return RedirectToPage();
        }

        public IActionResult OnPostCancelar(int id)
        {
            if (!Permiso("editar")) { TempData["Error"] = "No tienes permiso para editar mantenimientos"; return RedirectToPage(); }
            var m = _context.MantenimientosActivo.FirstOrDefault(x => x.Id == id);
            if (m != null && m.Estado == "Programado")
            {
                m.Estado = "Cancelado";
                _context.SaveChanges();
                TempData["Success"] = "Mantenimiento cancelado";
            }
            return RedirectToPage();
        }
    }
}
