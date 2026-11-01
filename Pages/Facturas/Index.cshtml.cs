using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Facturas
{
    public class IndexModel : PageModel
    {
        private const int TamanoPagina = 50;

        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Estado { get; set; }

        [BindProperty(SupportsGet = true, Name = "p")]
        public int Pagina { get; set; } = 1;

        public List<Factura> Facturas { get; set; } = new();
        public List<Cliente> Clientes { get; set; } = new();
        public PagerInfo Pager { get; set; } = new();
        public Dictionary<int, string> VentaNumeros { get; set; } = new();

        public IActionResult OnGet()
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Facturas", "ver")))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var query = _context.Facturas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var t = Q.Trim();
                var idsCliente = _context.Clientes.Where(c => c.Nombre.Contains(t)).Select(c => c.Id);
                var idsVenta = _context.Ventas.Where(v => v.Numero.Contains(t)).Select(v => v.Id);
                query = query.Where(f => f.Numero.Contains(t) || idsCliente.Contains(f.ClienteId)
                    || (f.VentaId != null && idsVenta.Contains(f.VentaId.Value)));
            }
            if (!string.IsNullOrWhiteSpace(Estado)) query = query.Where(f => f.Estado == Estado);

            Pager = new PagerInfo(Pagina, TamanoPagina, query.Count());
            Facturas = query.OrderByDescending(f => f.Fecha).Skip(Pager.Skip).Take(TamanoPagina).ToList();

            var ids = Facturas.Select(f => f.ClienteId).Distinct().ToList();
            Clientes = _context.Clientes.Where(c => ids.Contains(c.Id)).ToList();
            var vids = Facturas.Where(f => f.VentaId != null).Select(f => f.VentaId!.Value).Distinct().ToList();
            VentaNumeros = _context.Ventas.Where(v => vids.Contains(v.Id)).Select(v => new { v.Id, v.Numero }).ToDictionary(v => v.Id, v => v.Numero);

            return Page();
        }
    }
}
