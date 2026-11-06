using System.ComponentModel.DataAnnotations;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Produccion
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class LineaInput
        {
            public int ProductoId { get; set; }
            public decimal? Cantidad { get; set; }
        }

        public class InputModel
        {
            public int Id { get; set; }
            public DateTime Fecha { get; set; } = DateTime.Today;
            [Range(1, int.MaxValue, ErrorMessage = "Selecciona el producto a fabricar")]
            public int ProductoId { get; set; }
            [Range(0.0001, 999999999, ErrorMessage = "La cantidad debe ser mayor a 0")]
            public decimal Cantidad { get; set; } = 1;
            [StringLength(500)] public string? Notas { get; set; }
            public List<LineaInput> Lineas { get; set; } = new();
        }

        [BindProperty] public InputModel Input { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public string Numero { get; set; } = "";
        public bool EsNuevo => Input.Id == 0;

        private bool Autorizado(string accion) =>
            PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Produccion", "Index", accion);

        private void CargarListas()
        {
            Productos = _context.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToList();
        }

        public IActionResult OnGet(int? id)
        {
            if (!Autorizado(id.HasValue ? "editar" : "crear"))
            {
                TempData["Error"] = "No tienes permiso para esta acción";
                return RedirectToPage("/Produccion/Index");
            }
            CargarListas();

            if (id.HasValue)
            {
                var o = _context.OrdenesProduccion.FirstOrDefault(x => x.Id == id.Value);
                if (o == null) { TempData["Error"] = "Orden no encontrada"; return RedirectToPage("/Produccion/Index"); }
                if (o.Estado != "Planificada") { TempData["Error"] = "Solo se pueden editar órdenes planificadas"; return RedirectToPage("/Produccion/Details", new { id = o.Id }); }

                Numero = o.Numero;
                Input = new InputModel
                {
                    Id = o.Id, Fecha = o.Fecha, ProductoId = o.ProductoId, Cantidad = o.Cantidad, Notas = o.Notas,
                    Lineas = _context.DetallesOrdenProduccion.Where(d => d.OrdenProduccionId == o.Id)
                        .Select(d => new LineaInput { ProductoId = d.ProductoId, Cantidad = (decimal?)d.Cantidad }).ToList()
                };
            }
            else
            {
                Numero = NumeroDocumentoHelper.PreviewSiguiente(_context, "OrdenProduccion");
            }
            if (Input.Lineas.Count == 0) Input.Lineas.Add(new LineaInput());
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!Autorizado(Input.Id == 0 ? "crear" : "editar"))
            {
                TempData["Error"] = "No tienes permiso para esta acción";
                return RedirectToPage("/Produccion/Index");
            }
            CargarListas();

            OrdenProduccion? orden = null;
            if (Input.Id != 0)
            {
                orden = _context.OrdenesProduccion.FirstOrDefault(x => x.Id == Input.Id);
                if (orden == null) { TempData["Error"] = "Orden no encontrada"; return RedirectToPage("/Produccion/Index"); }
                if (orden.Estado != "Planificada") { TempData["Error"] = "Solo se pueden editar órdenes planificadas"; return RedirectToPage("/Produccion/Index"); }
                Numero = orden.Numero;
            }
            else
            {
                Numero = NumeroDocumentoHelper.PreviewSiguiente(_context, "OrdenProduccion");
            }

            var lineas = Input.Lineas.Where(l => l.ProductoId > 0).ToList();
            if (lineas.Count == 0) ModelState.AddModelError(string.Empty, "Agrega al menos un insumo");
            if (lineas.Any(l => (l.Cantidad ?? 0) <= 0)) ModelState.AddModelError(string.Empty, "Todas las cantidades de insumos deben ser mayores a 0");
            if (lineas.GroupBy(l => l.ProductoId).Any(g => g.Count() > 1)) ModelState.AddModelError(string.Empty, "No repitas el mismo insumo en varias líneas");
            if (lineas.Any(l => l.ProductoId == Input.ProductoId)) ModelState.AddModelError(string.Empty, "El producto terminado no puede ser también un insumo");

            if (!ModelState.IsValid)
            {
                if (Input.Lineas.Count == 0) Input.Lineas.Add(new LineaInput());
                return Page();
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            using var tx = _context.Database.BeginTransaction();

            if (orden == null)
            {
                orden = new OrdenProduccion
                {
                    Numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "OrdenProduccion"),
                    UsuarioId = usuario?.Id,
                    Estado = "Planificada"
                };
                _context.OrdenesProduccion.Add(orden);
            }
            else
            {
                var viejos = _context.DetallesOrdenProduccion.Where(d => d.OrdenProduccionId == orden.Id).ToList();
                _context.DetallesOrdenProduccion.RemoveRange(viejos);
            }

            orden.Fecha = Input.Fecha.Date;
            orden.ProductoId = Input.ProductoId;
            orden.Cantidad = Input.Cantidad;
            orden.Notas = Input.Notas;
            _context.SaveChanges();

            var idsInsumos = lineas.Select(l => l.ProductoId).ToList();
            var costos = _context.Productos.Where(p => idsInsumos.Contains(p.Id))
                .ToDictionary(p => p.Id, p => p.PrecioCompra);
            decimal total = 0;
            foreach (var l in lineas)
            {
                var cantidadLinea = l.Cantidad ?? 0;
                var costo = costos.TryGetValue(l.ProductoId, out var c) ? c : 0;
                _context.DetallesOrdenProduccion.Add(new DetalleOrdenProduccion
                {
                    OrdenProduccionId = orden.Id,
                    ProductoId = l.ProductoId,
                    Cantidad = cantidadLinea,
                    CostoUnitario = costo
                });
                total += Math.Round(cantidadLinea * costo, 2, MidpointRounding.AwayFromZero);
            }
            orden.CostoTotal = total;
            _context.SaveChanges();
            tx.Commit();

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Orden de producción",
                $"Guardó la orden {orden.Numero}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Orden {orden.Numero} guardada";
            return RedirectToPage("/Produccion/Details", new { id = orden.Id });
        }
    }
}
