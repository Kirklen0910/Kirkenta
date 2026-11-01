using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Clientes
{
    /// <summary>
    /// Ficha del cliente: datos, resumen de compras, productos que más compra, ventas, facturas y
    /// devoluciones recientes. Todo enlazado (venta, factura, producto) para navegar sin perderse.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Cliente Cliente { get; set; } = new();
        public bool PuedeVerProductos { get; set; }

        public int CantidadVentas { get; set; }
        public decimal TotalComprado { get; set; }
        public decimal TicketPromedio { get; set; }
        public decimal SaldoPendiente { get; set; }
        public decimal SaldoFavor { get; set; }
        public List<SaldoFavorMovimiento> MovimientosSaldo { get; set; } = new();
        public DateTime? UltimaCompra { get; set; }

        public List<FilaVenta> Ventas { get; set; } = new();
        public List<FilaFactura> Facturas { get; set; } = new();
        public List<FilaProducto> TopProductos { get; set; } = new();
        public List<Devolucion> Devoluciones { get; set; } = new();

        public class FilaVenta
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public decimal Total { get; set; }
            public decimal Saldo { get; set; }
            public string Estado { get; set; } = "";
            public int? FacturaId { get; set; }
            public string? FacturaNumero { get; set; }
        }

        public class FilaFactura
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public decimal Total { get; set; }
            public decimal Saldo { get; set; }
            public string Estado { get; set; } = "";
        }

        public class FilaProducto
        {
            public int ProductoId { get; set; }
            public string Nombre { get; set; } = "";
            public decimal Cantidad { get; set; }
            public decimal Monto { get; set; }
            public DateTime Ultima { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var usuario = User.Identity?.Name;
            if (!PermisoHelper.UsuarioTienePermiso(_context, usuario, "Ventas", "Clientes", "ver"))
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
            PuedeVerProductos = PermisoHelper.UsuarioTienePermiso(_context, usuario, "Inventario", "Productos", "ver");

            var ventasVigentes = _context.Ventas
                .Where(v => v.ClienteId == id && v.Estado != "Anulada" && v.Estado != "Cancelada")
                .OrderByDescending(v => v.Fecha)
                .ToList();

            CantidadVentas = ventasVigentes.Count;
            TotalComprado = ventasVigentes.Sum(v => v.Total);
            TicketPromedio = CantidadVentas > 0 ? TotalComprado / CantidadVentas : 0;
            SaldoPendiente = ventasVigentes.Where(v => v.EsCredito).Sum(v => v.SaldoPendiente);
            UltimaCompra = ventasVigentes.FirstOrDefault()?.Fecha;

            SaldoFavor = SaldoFavorHelper.Saldo(_context, id);
            MovimientosSaldo = _context.SaldoFavorMovimientos.Where(m => m.ClienteId == id)
                .OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id).Take(15).ToList();

            var recientes = ventasVigentes.Take(20).ToList();
            var recientesIds = recientes.Select(v => v.Id).ToList();
            var facturaPorVenta = _context.Facturas
                .Where(f => f.VentaId != null && recientesIds.Contains(f.VentaId.Value))
                .Select(f => new { f.Id, f.Numero, f.Estado, VentaId = f.VentaId!.Value })
                .ToList()
                .GroupBy(f => f.VentaId)
                .ToDictionary(g => g.Key, g => g.OrderBy(f => f.Estado == "Anulada").ThenByDescending(f => f.Id).First());

            Ventas = recientes.Select(v =>
            {
                facturaPorVenta.TryGetValue(v.Id, out var fac);
                return new FilaVenta
                {
                    Id = v.Id,
                    Numero = v.Numero,
                    Fecha = v.Fecha,
                    Total = v.Total,
                    Saldo = v.EsCredito ? v.SaldoPendiente : 0,
                    Estado = v.Estado,
                    FacturaId = fac?.Id,
                    FacturaNumero = fac?.Numero
                };
            }).ToList();

            Facturas = _context.Facturas
                .Where(f => f.ClienteId == id)
                .OrderByDescending(f => f.Fecha).ThenByDescending(f => f.Id)
                .Take(20)
                .Select(f => new FilaFactura
                {
                    Id = f.Id, Numero = f.Numero, Fecha = f.Fecha, Total = f.Total, Saldo = f.Saldo, Estado = f.Estado
                })
                .ToList();

            var lineas = (from d in _context.DetalleVentas
                          join v in _context.Ventas on d.VentaId equals v.Id
                          where v.ClienteId == id && v.Estado != "Anulada" && v.Estado != "Cancelada"
                          select new { d.ProductoId, d.Cantidad, d.Total, v.Fecha })
                         .ToList();

            var top = lineas.GroupBy(x => x.ProductoId)
                .Select(g => new { ProductoId = g.Key, Cantidad = g.Sum(x => x.Cantidad), Monto = g.Sum(x => x.Total), Ultima = g.Max(x => x.Fecha) })
                .OrderByDescending(x => x.Monto)
                .Take(15)
                .ToList();
            var topIds = top.Select(t => t.ProductoId).ToList();
            var nombres = _context.Productos.Where(p => topIds.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Nombre);
            TopProductos = top.Select(t => new FilaProducto
            {
                ProductoId = t.ProductoId,
                Nombre = nombres.TryGetValue(t.ProductoId, out var n) ? n : "(producto eliminado)",
                Cantidad = t.Cantidad,
                Monto = t.Monto,
                Ultima = t.Ultima
            }).ToList();

            Devoluciones = _context.Devoluciones
                .Where(d => d.ClienteId == id)
                .OrderByDescending(d => d.Fecha)
                .Take(10)
                .ToList();

            return Page();
        }
    }
}
