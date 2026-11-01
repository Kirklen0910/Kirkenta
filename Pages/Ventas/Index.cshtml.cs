using Kirkenta.Helpers;
using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Ventas
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
        public DateTime? Desde { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? Hasta { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Estado { get; set; }

        [BindProperty(SupportsGet = true, Name = "p")]
        public int Pagina { get; set; } = 1;

        public List<Venta> Ventas { get; set; } = new();
        public List<Cliente> Clientes { get; set; } = new();
        public PagerInfo Pager { get; set; } = new();
        /// <summary>Factura vigente de cada venta (id y número).</summary>
        public Dictionary<int, (int Id, string Numero)> FacturasPorVenta { get; set; } = new();

        public IActionResult OnGet()
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Index", "ver")))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var query = _context.Ventas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var t = Q.Trim();
                var idsCliente = _context.Clientes.Where(c => c.Nombre.Contains(t)).Select(c => c.Id);
                var idsVentaFactura = _context.Facturas.Where(f => f.VentaId != null && f.Numero.Contains(t)).Select(f => f.VentaId!.Value);
                query = query.Where(v => v.Numero.Contains(t) ||
                                         idsVentaFactura.Contains(v.Id) ||
                                         (v.ClienteId != null && idsCliente.Contains(v.ClienteId.Value)));
            }
            if (Desde.HasValue) query = query.Where(v => v.Fecha >= Desde.Value.Date);
            if (Hasta.HasValue) query = query.Where(v => v.Fecha < Hasta.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(Estado)) query = query.Where(v => v.Estado == Estado);

            Pager = new PagerInfo(Pagina, TamanoPagina, query.Count());
            Ventas = query.OrderByDescending(v => v.Fecha).Skip(Pager.Skip).Take(TamanoPagina).ToList();

            var ids = Ventas.Where(v => v.ClienteId != null).Select(v => v.ClienteId!.Value).Distinct().ToList();
            Clientes = _context.Clientes.Where(c => ids.Contains(c.Id)).ToList();

            var ventaIds = Ventas.Select(v => v.Id).ToList();
            FacturasPorVenta = _context.Facturas.Where(f => f.VentaId != null && ventaIds.Contains(f.VentaId.Value) && f.Estado != "Anulada")
                .Select(f => new { f.Id, f.Numero, VentaId = f.VentaId!.Value }).ToList()
                .GroupBy(f => f.VentaId).ToDictionary(g => g.Key, g => { var f = g.OrderByDescending(x => x.Id).First(); return (f.Id, f.Numero); });

            return Page();
        }
    }
}
