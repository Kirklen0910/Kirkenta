using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Produccion
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class LineaVista
        {
            public Producto? Producto { get; set; }
            public DetalleOrdenProduccion Detalle { get; set; } = new();
        }

        public OrdenProduccion Orden { get; set; } = new();
        public Producto? ProductoFinal { get; set; }
        public List<LineaVista> Insumos { get; set; } = new();
        public List<InspeccionCalidad> Inspecciones { get; set; } = new();
        public bool PuedeEditar { get; set; }
        public bool InsumosSuficientes { get; set; }

        private bool Permiso(string accion) =>
            PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Produccion", "Index", accion);

        private bool Cargar(int id)
        {
            var o = _context.OrdenesProduccion.FirstOrDefault(x => x.Id == id);
            if (o == null) return false;
            Orden = o;
            ProductoFinal = _context.Productos.FirstOrDefault(p => p.Id == o.ProductoId);
            var detalles = _context.DetallesOrdenProduccion.Where(d => d.OrdenProduccionId == id).ToList();
            var ids = detalles.Select(d => d.ProductoId).ToList();
            var productos = _context.Productos.Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id);
            Insumos = detalles.Select(d => new LineaVista
            {
                Detalle = d,
                Producto = productos.TryGetValue(d.ProductoId, out var p) ? p : null
            }).ToList();
            InsumosSuficientes = Insumos.All(i => i.Producto != null && i.Producto.Stock >= i.Detalle.Cantidad);
            Inspecciones = _context.InspeccionesCalidad.Where(i => i.OrdenProduccionId == id)
                .OrderByDescending(i => i.Fecha).ToList();
            PuedeEditar = Permiso("editar");
            return true;
        }

        public IActionResult OnGet(int id)
        {
            if (!Permiso("ver")) { TempData["Error"] = "No tienes permiso para ver producción"; return RedirectToPage("/Index"); }
            if (!Cargar(id)) { TempData["Error"] = "Orden no encontrada"; return RedirectToPage("/Produccion/Index"); }
            return Page();
        }

        /// <summary>
        /// Reclamo atómico del estado: con un doble clic o dos personas a la vez, solo una continúa
        /// (si no, los insumos se descontaban dos veces o el producto terminado entraba dos veces).
        /// </summary>
        private bool Reclamar(int id, string estado)
        {
            if (!_context.Database.IsRelational()) return true;
            return _context.OrdenesProduccion.Where(o => o.Id == id && o.Estado == estado)
                .ExecuteUpdate(u => u.SetProperty(o => o.Estado, "Reclamada")) > 0;
        }

        /// <summary>Descuenta los insumos del inventario y pasa la orden a "En proceso".</summary>
        public IActionResult OnPostIniciar(int id)
        {
            if (!Permiso("editar")) { TempData["Error"] = "No tienes permiso para iniciar órdenes"; return RedirectToPage(new { id }); }

            var orden = _context.OrdenesProduccion.FirstOrDefault(o => o.Id == id);
            if (orden == null || orden.Estado != "Planificada") { TempData["Error"] = "La orden no está planificada"; return RedirectToPage(new { id }); }

            var detalles = _context.DetallesOrdenProduccion.Where(d => d.OrdenProduccionId == id).ToList();
            if (detalles.Count == 0) { TempData["Error"] = "La orden no tiene insumos"; return RedirectToPage(new { id }); }

            var ids = detalles.Select(d => d.ProductoId).ToList();
            var productos = _context.Productos.Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id);

            foreach (var d in detalles)
            {
                if (!productos.TryGetValue(d.ProductoId, out var p))
                {
                    TempData["Error"] = "Un insumo de la orden ya no existe";
                    return RedirectToPage(new { id });
                }
                if (p.Stock < d.Cantidad)
                {
                    TempData["Error"] = $"Stock insuficiente de '{p.Nombre}': hay {p.Stock:0.##} y se necesitan {d.Cantidad:0.##}";
                    return RedirectToPage(new { id });
                }
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            try
            {
                using var tx = _context.Database.BeginTransaction();
                if (!Reclamar(id, "Planificada")) { tx.Rollback(); TempData["Error"] = "La orden ya fue iniciada"; return RedirectToPage(new { id }); }
                decimal total = 0;
                foreach (var d in detalles)
                {
                    var p = productos[d.ProductoId];
                    d.CostoUnitario = p.PrecioCompra;
                    total += Math.Round(d.Cantidad * d.CostoUnitario, 2, MidpointRounding.AwayFromZero);
                    InventarioHelper.Registrar(_context, p, -d.Cantidad, "Produccion", orden.Numero, orden.Id,
                        usuario?.Id, p.PrecioCompra, $"Insumo para orden {orden.Numero}");
                }
                orden.CostoTotal = total;
                orden.Estado = "EnProceso";
                orden.FechaInicio = DateTime.Now;
                _context.SaveChanges();
                tx.Commit();
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                TempData["Error"] = $"No se pudo iniciar la orden: {ex.Message}";
                return RedirectToPage(new { id });
            }

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Iniciar producción",
                $"Inició la orden {orden.Numero}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Orden {orden.Numero} en proceso: insumos descontados del inventario";
            return RedirectToPage(new { id });
        }

        /// <summary>Ingresa el producto terminado al inventario con su costo real.</summary>
        public IActionResult OnPostTerminar(int id, decimal cantidadProducida)
        {
            if (!Permiso("editar")) { TempData["Error"] = "No tienes permiso para terminar órdenes"; return RedirectToPage(new { id }); }
            if (cantidadProducida <= 0) { TempData["Error"] = "La cantidad producida debe ser mayor a 0"; return RedirectToPage(new { id }); }

            var orden = _context.OrdenesProduccion.FirstOrDefault(o => o.Id == id);
            if (orden == null || orden.Estado != "EnProceso") { TempData["Error"] = "La orden no está en proceso"; return RedirectToPage(new { id }); }

            var producto = _context.Productos.FirstOrDefault(p => p.Id == orden.ProductoId);
            if (producto == null) { TempData["Error"] = "El producto terminado ya no existe"; return RedirectToPage(new { id }); }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            try
            {
                using var tx = _context.Database.BeginTransaction();
                if (!Reclamar(id, "EnProceso")) { tx.Rollback(); TempData["Error"] = "La orden ya fue terminada"; return RedirectToPage(new { id }); }
                var costoUnitario = Math.Round(orden.CostoTotal / cantidadProducida, 4);
                producto.PrecioCompra = InventarioHelper.CostoPromedio(producto.Stock, producto.PrecioCompra, cantidadProducida, costoUnitario);
                InventarioHelper.Registrar(_context, producto, cantidadProducida, "Produccion", orden.Numero, orden.Id,
                    usuario?.Id, costoUnitario, $"Producto terminado de la orden {orden.Numero}");

                orden.CantidadProducida = cantidadProducida;
                orden.Estado = "Terminada";
                orden.FechaFin = DateTime.Now;
                _context.SaveChanges();
                tx.Commit();
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                TempData["Error"] = $"No se pudo terminar la orden: {ex.Message}";
                return RedirectToPage(new { id });
            }

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Terminar producción",
                $"Terminó la orden {orden.Numero}: {cantidadProducida:0.##} unidades", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Orden {orden.Numero} terminada: {cantidadProducida:0.##} unidades ingresadas al inventario";
            return RedirectToPage(new { id });
        }

        /// <summary>Cancela la orden. Si ya estaba en proceso, devuelve los insumos al inventario.</summary>
        public IActionResult OnPostCancelar(int id)
        {
            if (!Permiso("editar")) { TempData["Error"] = "No tienes permiso para cancelar órdenes"; return RedirectToPage(new { id }); }

            var orden = _context.OrdenesProduccion.FirstOrDefault(o => o.Id == id);
            if (orden == null || (orden.Estado != "Planificada" && orden.Estado != "EnProceso"))
            {
                TempData["Error"] = "Solo se pueden cancelar órdenes planificadas o en proceso";
                return RedirectToPage(new { id });
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            try
            {
                using var tx = _context.Database.BeginTransaction();
                if (!Reclamar(id, orden.Estado)) { tx.Rollback(); TempData["Error"] = "La orden cambió mientras la cancelabas"; return RedirectToPage(new { id }); }
                if (orden.Estado == "EnProceso")
                {
                    var detalles = _context.DetallesOrdenProduccion.Where(d => d.OrdenProduccionId == id).ToList();
                    var ids = detalles.Select(d => d.ProductoId).ToList();
                    var productos = _context.Productos.Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id);
                    foreach (var d in detalles)
                    {
                        if (productos.TryGetValue(d.ProductoId, out var p))
                            InventarioHelper.Registrar(_context, p, d.Cantidad, "ProduccionCancelada", orden.Numero, orden.Id,
                                usuario?.Id, d.CostoUnitario, $"Devolución de insumo por cancelar {orden.Numero}");
                    }
                }
                orden.Estado = "Cancelada";
                orden.FechaFin = DateTime.Now;
                _context.SaveChanges();
                tx.Commit();
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                TempData["Error"] = $"No se pudo cancelar la orden: {ex.Message}";
                return RedirectToPage(new { id });
            }

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Cancelar producción",
                $"Canceló la orden {orden.Numero}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Orden {orden.Numero} cancelada";
            return RedirectToPage(new { id });
        }
    }
}
