using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Devoluciones
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Devolucion> Devoluciones { get; set; } = new();
        public List<Cliente> Clientes { get; set; } = new();
        public List<Factura> Facturas { get; set; } = new();

        public void OnGet()
        {
            Devoluciones = _context.Devoluciones.OrderByDescending(d => d.Fecha).ToList();
            Clientes = _context.Clientes.ToList();
            Facturas = _context.Facturas.ToList();
        }
    }
}