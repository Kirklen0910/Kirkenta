using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Ventas
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Venta Venta { get; set; } = new();
        public Cliente? Cliente { get; set; }
        public List<DetalleVenta> Items { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public List<Pago> Pagos { get; set; } = new();
        public List<MetodoPago> MetodosPago { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var venta = _context.Ventas.FirstOrDefault(v => v.Id == id);
            if (venta == null)
            {
                TempData["Error"] = "Venta no encontrada";
                return RedirectToPage("/Ventas/Index");
            }

            Venta = venta;
            Cliente = venta.ClienteId.HasValue
                ? _context.Clientes.FirstOrDefault(c => c.Id == venta.ClienteId.Value)
                : null;
            Items = _context.DetalleVentas.Where(d => d.VentaId == id).ToList();
            Productos = _context.Productos.ToList();
            Pagos = _context.Pagos.Where(p => p.VentaId == id).ToList();
            MetodosPago = _context.MetodosPago.ToList();

            return Page();
        }
    }
}