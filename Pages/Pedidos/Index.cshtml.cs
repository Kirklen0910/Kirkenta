using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Pedidos
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Pedido> Pedidos { get; set; } = new();
        public List<Cliente> Clientes { get; set; } = new();

        public void OnGet()
        {
            Pedidos = _context.Pedidos.OrderByDescending(p => p.Fecha).ToList();
            Clientes = _context.Clientes.ToList();
        }
    }
}