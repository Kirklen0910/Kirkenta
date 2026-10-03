using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Pedidos
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Pedido Pedido { get; set; } = new();
        public Cliente Cliente { get; set; } = new();
        public List<DetallePedido> Items { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var pedido = _context.Pedidos.FirstOrDefault(p => p.Id == id);
            if (pedido == null)
            {
                TempData["Error"] = "Pedido no encontrado";
                return RedirectToPage("/Pedidos/Index");
            }

            Pedido = pedido;
            Cliente = _context.Clientes.FirstOrDefault(c => c.Id == pedido.ClienteId) ?? new Cliente();
            Items = _context.DetallePedidos.Where(d => d.PedidoId == id).ToList();
            Productos = _context.Productos.ToList();

            return Page();
        }
    }
}