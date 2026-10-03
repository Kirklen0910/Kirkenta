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

            Pagos = _context.PagosProveedor
                .AsNoTracking()
                .OrderByDescending(p => p.Fecha)
                .ToList();

            Proveedores = _context.Proveedores
                .AsNoTracking()
                .ToList();

            MetodosPago = _context.MetodosPago
                .AsNoTracking()
                .ToList();

            OrdenesCompra = _context.OrdenesCompra
                .AsNoTracking()
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

            var pagosMes = Pagos
                .Where(p => p.Fecha >= inicioMes && p.Fecha < finMes)
                .ToList();

            TotalPagadoMes = pagosMes.Sum(p => p.Monto);
            CantidadPagosMes = pagosMes.Count;

            TotalPorPagar = OrdenesPorPagar.Sum(o => o.Saldo);
            OrdenesPendientes = OrdenesPorPagar.Count;
            ProveedoresConDeuda = OrdenesPorPagar.Select(o => o.ProveedorId).Distinct().Count();
            TotalHistorico = Pagos.Sum(p => p.Monto);

            return Page();
        }
    }
}