using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>
    /// «Así se ve tu negocio en Kirkenta»: los números del negocio calculados sobre lo que dejó la migración
    /// (en la revisión también, porque se calcula antes de deshacerla). Es lo que el dueño reconoce al instante.
    /// </summary>
    public class ResumenNegocio
    {
        public record Mes(DateTime Inicio, decimal Total, int Facturas, decimal Isv);
        public record Fila(string Nombre, decimal Monto, string? Detalle = null);

        public int Facturas { get; set; }
        public decimal TotalVendido { get; set; }
        public decimal TicketPromedio => Facturas == 0 ? 0 : Math.Round(TotalVendido / Facturas, 2, MidpointRounding.AwayFromZero);
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public List<Mes> VentasPorMes { get; } = new();
        public List<Fila> TopProductos { get; } = new();
        public List<Fila> TopClientes { get; } = new();

        public decimal PorCobrar { get; set; }
        public decimal PorCobrarVencido { get; set; }
        public List<Fila> Deudores { get; } = new();
        public decimal PorPagar { get; set; }

        public int Productos { get; set; }
        public decimal InventarioCosto { get; set; }
        public decimal InventarioVenta { get; set; }
        public int SinExistencia { get; set; }
        public int Clientes { get; set; }
        public int Proveedores { get; set; }

        public bool HayDatos => Facturas > 0 || Productos > 0 || Clientes > 0 || PorCobrar > 0 || PorPagar > 0;
        public decimal MesMayor => VentasPorMes.Count == 0 ? 0 : VentasPorMes.Max(m => m.Total);
    }

    public static class MigracionResumen
    {
        /// <summary>Marcas (último id) antes de migrar: el resumen solo cuenta lo que entra en esta migración.</summary>
        public record Marcas(int Ventas, int Ordenes, int Productos, int Clientes, int Proveedores);

        public static Marcas Marcar(ApplicationDbContext ctx) => new(
            ctx.Ventas.Max(x => (int?)x.Id) ?? 0, ctx.OrdenesCompra.Max(x => (int?)x.Id) ?? 0,
            ctx.Productos.Max(x => (int?)x.Id) ?? 0, ctx.Clientes.Max(x => (int?)x.Id) ?? 0, ctx.Proveedores.Max(x => (int?)x.Id) ?? 0);

        public static ResumenNegocio Calcular(ApplicationDbContext ctx, Marcas m)
        {
            var r = new ResumenNegocio();
            var hoy = DateTime.Today;

            // ---- ventas (sin los saldos iniciales «SI-», que no son ventas del período) ----
            var ventas = ctx.Ventas.AsNoTracking()
                .Where(v => v.Id > m.Ventas && v.Estado != "Anulada" && !v.Numero.StartsWith("SI-"))
                .Select(v => new { v.Id, v.Fecha, v.Total, v.Impuestos, v.ClienteId }).ToList();
            r.Facturas = ventas.Count;
            r.TotalVendido = ventas.Sum(v => v.Total);
            if (ventas.Count > 0) { r.Desde = ventas.Min(v => v.Fecha); r.Hasta = ventas.Max(v => v.Fecha); }
            foreach (var g in ventas.GroupBy(v => new DateTime(v.Fecha.Year, v.Fecha.Month, 1)).OrderByDescending(g => g.Key).Take(12).OrderBy(g => g.Key))
                r.VentasPorMes.Add(new ResumenNegocio.Mes(g.Key, g.Sum(v => v.Total), g.Count(), g.Sum(v => v.Impuestos)));

            var ids = ventas.Select(v => v.Id).ToHashSet();
            if (ids.Count > 0)
            {
                var lineas = ctx.DetalleVentas.AsNoTracking().Where(d => d.VentaId > m.Ventas)
                    .Select(d => new { d.VentaId, d.ProductoId, d.Descripcion, d.Cantidad, d.Total }).ToList()
                    .Where(d => ids.Contains(d.VentaId)).ToList();
                var nombres = ctx.Productos.AsNoTracking().Select(p => new { p.Id, p.Nombre }).ToDictionary(p => p.Id, p => p.Nombre);
                foreach (var g in lineas.GroupBy(l => l.ProductoId).OrderByDescending(g => g.Sum(l => l.Total)).Take(8))
                {
                    var nombre = nombres.GetValueOrDefault(g.Key) ?? g.First().Descripcion ?? "Producto";
                    r.TopProductos.Add(new ResumenNegocio.Fila(nombre, g.Sum(l => l.Total), $"{g.Sum(l => l.Cantidad):#,##0.##} vendidos"));
                }
            }

            var clientes = ctx.Clientes.AsNoTracking().Select(c => new { c.Id, c.Nombre }).ToDictionary(c => c.Id, c => c.Nombre);
            foreach (var g in ventas.Where(v => v.ClienteId != null).GroupBy(v => v.ClienteId!.Value).OrderByDescending(g => g.Sum(v => v.Total)).Take(5))
                r.TopClientes.Add(new ResumenNegocio.Fila(clientes.GetValueOrDefault(g.Key) ?? "Cliente", g.Sum(v => v.Total), $"{g.Count()} compra(s)"));

            // ---- cuentas por cobrar y por pagar ----
            var credito = ctx.Ventas.AsNoTracking()
                .Where(v => v.Id > m.Ventas && v.SaldoPendiente > 0 && v.Estado != "Anulada")
                .Select(v => new { v.ClienteId, v.SaldoPendiente, Vence = v.FechaVencimientoCredito ?? v.Fecha }).ToList();
            r.PorCobrar = credito.Sum(c => c.SaldoPendiente);
            r.PorCobrarVencido = credito.Where(c => c.Vence.Date < hoy).Sum(c => c.SaldoPendiente);
            foreach (var g in credito.Where(c => c.ClienteId != null).GroupBy(c => c.ClienteId!.Value).OrderByDescending(g => g.Sum(c => c.SaldoPendiente)).Take(5))
            {
                var venc = g.Where(c => c.Vence.Date < hoy).Sum(c => c.SaldoPendiente);
                r.Deudores.Add(new ResumenNegocio.Fila(clientes.GetValueOrDefault(g.Key) ?? "Cliente", g.Sum(c => c.SaldoPendiente),
                    venc > 0 ? $"L. {venc:N2} vencido" : "al día"));
            }
            r.PorPagar = ctx.OrdenesCompra.AsNoTracking().Where(o => o.Id > m.Ordenes && o.Saldo > 0).Sum(o => (decimal?)o.Saldo) ?? 0;

            // ---- inventario y catálogos ----
            var prods = ctx.Productos.AsNoTracking().Where(p => p.Id > m.Productos)
                .Select(p => new { p.Stock, p.PrecioCompra, p.PrecioVenta }).ToList();
            r.Productos = prods.Count;
            r.InventarioCosto = prods.Sum(p => Math.Max(0, p.Stock) * p.PrecioCompra);
            r.InventarioVenta = prods.Sum(p => Math.Max(0, p.Stock) * p.PrecioVenta);
            r.SinExistencia = prods.Count(p => p.Stock <= 0);
            r.Clientes = ctx.Clientes.Count(c => c.Id > m.Clientes);
            r.Proveedores = ctx.Proveedores.Count(c => c.Id > m.Proveedores);
            return r;
        }
    }
}
