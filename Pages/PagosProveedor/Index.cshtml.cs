using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.PagosProveedor
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Models.PagoProveedor> Pagos { get; set; } = new();
        public List<Models.OrdenCompra> OrdenesPorPagar { get; set; } = new();
        public List<Proveedor> Proveedores { get; set; } = new();
        public List<MetodoPago> MetodosPago { get; set; } = new();
        public List<Models.OrdenCompra> OrdenesCompra { get; set; } = new();

        public decimal TotalPagadoMes { get; set; }
        public int CantidadPagosMes { get; set; }
        public decimal TotalPorPagar { get; set; }
        public int OrdenesPendientes { get; set; }
        public int ProveedoresConDeuda { get; set; }
        public decimal TotalHistorico { get; set; }

        public string TabActiva { get; set; } = "pagos";

        private const int TamanoPagina = 50;

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ProveedorId { get; set; }

        [BindProperty(SupportsGet = true, Name = "p")]
        public int Pagina { get; set; } = 1;

        public PagerInfo Pager { get; set; } = new();

        public IActionResult OnGet(string? tab)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "Pagos", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver pagos a proveedores";
                return RedirectToPage("/Index");
            }

            TabActiva = tab == "porpagar" ? "porpagar" : "pagos";

            // Pagos filtrados y paginados en la base de datos (antes se cargaba todo el historial)
            var pagosQuery = _context.PagosProveedor.AsNoTracking().AsQueryable();
            if (ProveedorId.HasValue) pagosQuery = pagosQuery.Where(p => p.ProveedorId == ProveedorId.Value);
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var texto = Q.Trim();
                var idsProv = _context.Proveedores.Where(x => x.Nombre.Contains(texto)).Select(x => x.Id);
                pagosQuery = pagosQuery.Where(p => (p.Numero != null && p.Numero.Contains(texto)) || idsProv.Contains(p.ProveedorId));
            }
            Pager = new PagerInfo(Pagina, TamanoPagina, pagosQuery.Count());
            Pagos = pagosQuery.OrderByDescending(p => p.Fecha).Skip(Pager.Skip).Take(TamanoPagina).ToList();

            Proveedores = _context.Proveedores
                .AsNoTracking()
                .ToList();

            MetodosPago = _context.MetodosPago
                .AsNoTracking()
                .ToList();

            var idsOrdenes = Pagos.Where(p => p.OrdenCompraId.HasValue).Select(p => p.OrdenCompraId!.Value).Distinct().ToList();
            OrdenesCompra = _context.OrdenesCompra
                .AsNoTracking()
                .Where(o => idsOrdenes.Contains(o.Id))
                .ToList();

            // Cuentas por pagar = Recibida o RecibidaParcial con saldo > 0
            OrdenesPorPagar = _context.OrdenesCompra
                .AsNoTracking()
                .Where(o => (o.Estado == "Recibida" || o.Estado == "RecibidaParcial") && o.Saldo > 0)
                .OrderBy(o => o.Fecha)
                .ToList();

            // === KPIs ===
            var ahora = DateTime.Now;
            var inicioMes = new DateTime(ahora.Year, ahora.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            // KPIs sobre TODOS los pagos (no solo la página mostrada)
            var pagosMes = _context.PagosProveedor.AsNoTracking()
                .Where(p => p.Fecha >= inicioMes && p.Fecha < finMes);
            TotalPagadoMes = pagosMes.Sum(p => (decimal?)p.Monto) ?? 0m;
            CantidadPagosMes = pagosMes.Count();

            TotalPorPagar = OrdenesPorPagar.Sum(o => o.Saldo);
            OrdenesPendientes = OrdenesPorPagar.Count;
            ProveedoresConDeuda = OrdenesPorPagar.Select(o => o.ProveedorId).Distinct().Count();
            TotalHistorico = _context.PagosProveedor.AsNoTracking().Sum(p => (decimal?)p.Monto) ?? 0m;

            return Page();
        }
    }
}