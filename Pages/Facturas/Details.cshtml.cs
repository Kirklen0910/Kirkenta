using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Facturas
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Factura Factura { get; set; } = new();
        public Cliente Cliente { get; set; } = new();
        public List<DetalleFactura> Items { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public List<Pago> Pagos { get; set; } = new();
        public List<MetodoPago> MetodosPago { get; set; } = new();
        public ConfiguracionEmpresa Empresa { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var factura = _context.Facturas.FirstOrDefault(f => f.Id == id);
            if (factura == null)
            {
                TempData["Error"] = "Factura no encontrada";
                return RedirectToPage("/Facturas/Index");
            }

            Factura = factura;
            Cliente = _context.Clientes.FirstOrDefault(c => c.Id == factura.ClienteId) ?? new Cliente();
            Items = _context.DetalleFacturas.Where(d => d.FacturaId == id).ToList();
            Productos = _context.Productos.ToList();
            Pagos = _context.Pagos.Where(p => p.FacturaId == id).ToList();
            MetodosPago = _context.MetodosPago.ToList();
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault() ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };

            return Page();
        }
    }
}