using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Facturas
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Factura> Facturas { get; set; } = new();
        public List<Cliente> Clientes { get; set; } = new();

        public void OnGet()
        {
            Facturas = _context.Facturas.OrderByDescending(f => f.Fecha).ToList();
            Clientes = _context.Clientes.ToList();
        }
    }
}