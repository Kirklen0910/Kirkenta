using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
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

        private const int TamanoPagina = 50;

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Estado { get; set; }

        [BindProperty(SupportsGet = true, Name = "p")]
        public int Pagina { get; set; } = 1;

        public List<Pedido> Pedidos { get; set; } = new();
        public List<Cliente> Clientes { get; set; } = new();
        public PagerInfo Pager { get; set; } = new();

        public IActionResult OnGet()
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Pedidos", "ver")))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var query = _context.Pedidos.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var t = Q.Trim();
                var idsCliente = _context.Clientes.Where(c => c.Nombre.Contains(t)).Select(c => c.Id);
                query = query.Where(p => p.Numero.Contains(t) || idsCliente.Contains(p.ClienteId));
            }
            if (!string.IsNullOrWhiteSpace(Estado)) query = query.Where(p => p.Estado == Estado);

            Pager = new PagerInfo(Pagina, TamanoPagina, query.Count());
            Pedidos = query.OrderByDescending(p => p.Fecha).Skip(Pager.Skip).Take(TamanoPagina).ToList();

            var ids = Pedidos.Select(p => p.ClienteId).Distinct().ToList();
            Clientes = _context.Clientes.Where(c => ids.Contains(c.Id)).ToList();

            return Page();
        }
    }
}