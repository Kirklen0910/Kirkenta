using System.ComponentModel.DataAnnotations;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Produccion
{
    public class CalidadModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CalidadModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class NuevaInspeccion
        {
            public int? OrdenProduccionId { get; set; }
            [Range(1, int.MaxValue, ErrorMessage = "Selecciona el producto")]
            public int ProductoId { get; set; }
            [Range(0.0001, 999999999, ErrorMessage = "La cantidad inspeccionada debe ser mayor a 0")]
            public decimal CantidadInspeccionada { get; set; }
            [Range(0, 999999999, ErrorMessage = "La cantidad defectuosa no es válida")]
            public decimal CantidadDefectuosa { get; set; }
            [Required] public string Resultado { get; set; } = "Aprobado";
            [StringLength(500)] public string? Observaciones { get; set; }
            [StringLength(100)] public string? Inspector { get; set; }
        }

        [BindProperty(SupportsGet = true)] public int? OrdenId { get; set; }
        [BindProperty(SupportsGet = true)] public string? Resultado { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        [BindProperty] public NuevaInspeccion Nueva { get; set; } = new();

        public List<InspeccionCalidad> Inspecciones { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public List<OrdenProduccion> Ordenes { get; set; } = new();
        public Dictionary<int, string> NombresProducto { get; set; } = new();
        public Dictionary<int, string> NumerosOrden { get; set; } = new();
        public bool PuedeCrear { get; set; }

        public int TotalInspecciones { get; set; }
        public decimal PorcentajeDefectos { get; set; }
        public int Rechazadas { get; set; }

        private bool Permiso(string accion) =>
            PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Produccion", "Calidad", accion);

        private void Cargar()
        {
            PuedeCrear = Permiso("crear");
            Productos = _context.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToList();
            Ordenes = _context.OrdenesProduccion.Where(o => o.Estado != "Cancelada")
                .OrderByDescending(o => o.Fecha).ThenByDescending(o => o.Id).Take(100).ToList();
            NombresProducto = _context.Productos.ToDictionary(p => p.Id, p => p.Nombre);
            NumerosOrden = _context.OrdenesProduccion.ToDictionary(o => o.Id, o => o.Numero);

            var query = _context.InspeccionesCalidad.AsQueryable();
            if (OrdenId.HasValue) query = query.Where(i => i.OrdenProduccionId == OrdenId.Value);
            if (!string.IsNullOrWhiteSpace(Resultado)) query = query.Where(i => i.Resultado == Resultado);

            Pager = new PagerInfo(Pagina, 50, query.Count());
            Inspecciones = query.OrderByDescending(i => i.Fecha).ThenByDescending(i => i.Id)
                .Skip(Pager.Skip).Take(Pager.PageSize).ToList();

            var todas = _context.InspeccionesCalidad;
            TotalInspecciones = todas.Count();
            var insp = todas.Sum(i => (decimal?)i.CantidadInspeccionada) ?? 0;
            var def = todas.Sum(i => (decimal?)i.CantidadDefectuosa) ?? 0;
            PorcentajeDefectos = insp > 0 ? Math.Round(def / insp * 100m, 2, MidpointRounding.AwayFromZero) : 0;
            Rechazadas = todas.Count(i => i.Resultado == "Rechazado");
        }

        public IActionResult OnGet()
        {
            if (!Permiso("ver")) { TempData["Error"] = "No tienes permiso para ver calidad"; return RedirectToPage("/Index"); }
            if (OrdenId.HasValue)
            {
                Nueva.OrdenProduccionId = OrdenId;
                var o = _context.OrdenesProduccion.FirstOrDefault(x => x.Id == OrdenId.Value);
                if (o != null) { Nueva.ProductoId = o.ProductoId; Nueva.CantidadInspeccionada = o.CantidadProducida > 0 ? o.CantidadProducida : o.Cantidad; }
            }
            Cargar();
            return Page();
        }

        public IActionResult OnPostRegistrar()
        {
            if (!Permiso("crear")) { TempData["Error"] = "No tienes permiso para registrar inspecciones"; return RedirectToPage(); }

            if (Nueva.CantidadDefectuosa > Nueva.CantidadInspeccionada)
                ModelState.AddModelError("Nueva.CantidadDefectuosa", "Las defectuosas no pueden superar a las inspeccionadas");
            if (Nueva.Resultado != "Aprobado" && Nueva.Resultado != "Condicional" && Nueva.Resultado != "Rechazado")
                ModelState.AddModelError("Nueva.Resultado", "Resultado no válido");

            if (!ModelState.IsValid)
            {
                Cargar();
                return Page();
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            _context.InspeccionesCalidad.Add(new InspeccionCalidad
            {
                Fecha = DateTime.Now,
                OrdenProduccionId = Nueva.OrdenProduccionId > 0 ? Nueva.OrdenProduccionId : null,
                ProductoId = Nueva.ProductoId,
                CantidadInspeccionada = Nueva.CantidadInspeccionada,
                CantidadDefectuosa = Nueva.CantidadDefectuosa,
                Resultado = Nueva.Resultado,
                Observaciones = Nueva.Observaciones,
                Inspector = string.IsNullOrWhiteSpace(Nueva.Inspector) ? usuario?.Username : Nueva.Inspector,
                UsuarioId = usuario?.Id
            });
            _context.SaveChanges();

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Inspección de calidad",
                $"Registró inspección ({Nueva.Resultado})", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = "Inspección registrada";
            return RedirectToPage();
        }
    }
}
