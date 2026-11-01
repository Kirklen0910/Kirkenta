using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Productos
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Producto Producto { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Productos/Index");
            }

            var producto = _context.Productos.FirstOrDefault(p => p.Id == id);
            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado";
                return RedirectToPage("/Productos/Index");
            }

            if (TieneDependencias(id))
            {
                TempData["Error"] = "No se puede eliminar el producto: tiene ventas, facturas, cotizaciones, pedidos, órdenes de compra o bajas asociadas. Desactívalo mejor.";
                return RedirectToPage("/Productos/Index");
            }

            Producto = producto;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Productos/Index");
            }

            var producto = _context.Productos.FirstOrDefault(p => p.Id == id);
            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado";
                return RedirectToPage("/Productos/Index");
            }

            if (TieneDependencias(id))
            {
                TempData["Error"] = "No se puede eliminar el producto: tiene movimientos o documentos asociados. Desactívalo (Editar → Activo) para que no aparezca en ventas.";
                return RedirectToPage("/Productos/Index");
            }

            var nombre = producto.Nombre;
            _context.Productos.Remove(producto);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar producto",
                $"Eliminó el producto '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Producto '{nombre}' eliminado";
            return RedirectToPage("/Productos/Index");
        }

        private bool TieneDependencias(int productoId)
        {
            if (_context.DetalleVentas.Any(d => d.ProductoId == productoId)) return true;
            if (_context.DetalleFacturas.Any(d => d.ProductoId == productoId)) return true;
            if (_context.DetalleCotizaciones.Any(d => d.ProductoId == productoId)) return true;
            if (_context.DetallePedidos.Any(d => d.ProductoId == productoId)) return true;
            if (_context.DetalleOrdenesCompra.Any(d => d.ProductoId == productoId)) return true;
            if (_context.DetalleDevoluciones.Any(d => d.ProductoId == productoId)) return true;
            if (_context.DetalleDevolucionesProveedor.Any(d => d.ProductoId == productoId)) return true;
            if (_context.BajasInventario.Any(b => b.ProductoId == productoId)) return true;
            // Kardex, producción, calidad y tomas físicas también guardan el historial del producto
            if (_context.MovimientosInventario.Any(m => m.ProductoId == productoId)) return true;
            if (_context.OrdenesProduccion.Any(o => o.ProductoId == productoId)) return true;
            if (_context.DetallesOrdenProduccion.Any(d => d.ProductoId == productoId)) return true;
            if (_context.InspeccionesCalidad.Any(i => i.ProductoId == productoId)) return true;
            if (_context.DetallesTomaFisica.Any(d => d.ProductoId == productoId)) return true;
            return false;
        }
    }
}