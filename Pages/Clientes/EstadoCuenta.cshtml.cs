using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Clientes
{
    public class EstadoCuentaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EstadoCuentaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Cliente Cliente { get; set; } = new();
        public ConfiguracionEmpresa Empresa { get; set; } = new();

        public class FilaVenta
        {
            public Venta Venta { get; set; } = new();
            public string? FacturaNumero { get; set; }
            public decimal Pagado => Venta.Total - Venta.SaldoPendiente;
            public DateTime Vencimiento => Venta.FechaVencimientoCredito ?? Venta.Fecha;
            public int DiasVencido => Venta.SaldoPendiente > 0 ? Math.Max(0, (DateTime.Today - Vencimiento.Date).Days) : 0;
        }

        public List<FilaVenta> Ventas { get; set; } = new();
        public List<Pago> Pagos { get; set; } = new();
        public List<Devolucion> Devoluciones { get; set; } = new();
        public decimal TotalCredito { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public decimal SaldoVencido { get; set; }
        public decimal CreditoDisponible { get; set; }

        public IActionResult OnGet(int id)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Clientes", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver clientes";
                return RedirectToPage("/Index");
            }

            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);
            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado";
                return RedirectToPage("/Clientes/Index");
            }

            Cliente = cliente;
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault() ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };

            var ventas = _context.Ventas
                .Where(v => v.ClienteId == id && v.EsCredito && v.Estado != "Anulada")
                .OrderByDescending(v => v.Fecha).ToList();
            var ventaIds = ventas.Select(v => v.Id).ToList();
            var facturas = _context.Facturas.Where(f => f.VentaId != null && ventaIds.Contains(f.VentaId.Value))
                .ToDictionary(f => f.VentaId!.Value, f => f.Numero);

            Ventas = ventas.Select(v => new FilaVenta
            {
                Venta = v,
                FacturaNumero = facturas.TryGetValue(v.Id, out var n) ? n : null
            }).ToList();

            Pagos = _context.Pagos.Where(p => p.ClienteId == id).OrderByDescending(p => p.Fecha).Take(100).ToList();
            Devoluciones = _context.Devoluciones.Where(d => d.ClienteId == id && d.Estado == "Aplicada")
                .OrderByDescending(d => d.Fecha).Take(100).ToList();

            TotalCredito = ventas.Sum(v => v.Total);
            SaldoPendiente = ventas.Sum(v => v.SaldoPendiente);
            TotalPagado = TotalCredito - SaldoPendiente;
            SaldoVencido = Ventas.Where(f => f.DiasVencido > 0).Sum(f => f.Venta.SaldoPendiente);
            CreditoDisponible = cliente.LimiteCredito > 0 ? Math.Max(0, cliente.LimiteCredito - SaldoPendiente) : 0;

            return Page();
        }
    }
}
