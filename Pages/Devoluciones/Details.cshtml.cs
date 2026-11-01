using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Devoluciones
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Devolucion Devolucion { get; set; } = new();
        public Cliente? Cliente { get; set; }
        public Venta? Venta { get; set; }
        public Factura? Factura { get; set; }
        public ConfiguracionEmpresa? Empresa { get; set; }
        public List<(DetalleDevolucion det, string producto)> Items { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Devoluciones", "ver"))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var dev = _context.Devoluciones.FirstOrDefault(d => d.Id == id);
            if (dev == null)
            {
                TempData["Error"] = "Devolución no encontrada";
                return RedirectToPage("/Devoluciones/Index");
            }

            Devolucion = dev;
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault();
            Cliente = _context.Clientes.FirstOrDefault(c => c.Id == dev.ClienteId);
            Venta = dev.VentaId.HasValue ? _context.Ventas.FirstOrDefault(v => v.Id == dev.VentaId.Value) : null;
            Factura = dev.FacturaId.HasValue ? _context.Facturas.FirstOrDefault(f => f.Id == dev.FacturaId.Value) : null;

            var detalles = _context.DetalleDevoluciones.Where(d => d.DevolucionId == id).ToList();
            var productos = _context.Productos.ToDictionary(p => p.Id, p => p.Nombre);
            Items = detalles.Select(d => (d, productos.TryGetValue(d.ProductoId, out var n) ? n : $"Producto {d.ProductoId}")).ToList();

            return Page();
        }
    }
}
