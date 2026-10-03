using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Ventas
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Venta> Ventas { get; set; } = new();
        public List<Cliente> Clientes { get; set; } = new();

        public void OnGet()
        {
            Ventas = _context.Ventas.OrderByDescending(v => v.Fecha).ToList();
            Clientes = _context.Clientes.ToList();
        }
    }
}