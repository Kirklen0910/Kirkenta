using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
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

        private const int TamanoPagina = 50;

        [BindProperty(SupportsGet = true, Name = "p")]
        public int Pagina { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        public PagerInfo Pager { get; set; } = new();

        public List<Devolucion> Devoluciones { get; set; } = new();
        public List<Cliente> Clientes { get; set; } = new();
        public List<Factura> Facturas { get; set; } = new();
        public List<Venta> Ventas { get; set; } = new();
        public bool PuedeCrear { get; set; }

        public IActionResult OnGet()
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Devoluciones", "ver")))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var query = _context.Devoluciones.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var t = Q.Trim();
                var cli = _context.Clientes.Where(c => c.Nombre.Contains(t)).Select(c => c.Id);
                var ven = _context.Ventas.Where(v => v.Numero.Contains(t)).Select(v => v.Id);
                var fac = _context.Facturas.Where(f => f.Numero.Contains(t)).Select(f => f.Id);
                query = query.Where(d => d.Numero.Contains(t) || cli.Contains(d.ClienteId)
                    || (d.VentaId != null && ven.Contains(d.VentaId.Value))
                    || (d.FacturaId != null && fac.Contains(d.FacturaId.Value)));
            }
            Pager = new PagerInfo(Pagina, TamanoPagina, query.Count());
            Devoluciones = query.OrderByDescending(d => d.Fecha)
                .Skip(Pager.Skip).Take(TamanoPagina).ToList();

            // Solo los clientes, facturas y ventas de las devoluciones de esta página
            var idsCliente = Devoluciones.Select(d => d.ClienteId).Distinct().ToList();
            var idsFactura = Devoluciones.Where(d => d.FacturaId.HasValue).Select(d => d.FacturaId!.Value).Distinct().ToList();
            var idsVenta = Devoluciones.Where(d => d.VentaId.HasValue).Select(d => d.VentaId!.Value).Distinct().ToList();
            Clientes = _context.Clientes.Where(c => idsCliente.Contains(c.Id)).ToList();
            Facturas = _context.Facturas.Where(f => idsFactura.Contains(f.Id)).ToList();
            Ventas = _context.Ventas.Where(v => idsVenta.Contains(v.Id)).ToList();
            PuedeCrear = PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Devoluciones", "crear");

            return Page();
        }
    }
}