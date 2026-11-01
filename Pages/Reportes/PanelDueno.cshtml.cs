using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kirkenta.Pages.Reportes
{
    /// <summary>
    /// Panel del dueño: cómo va el mes frente al anterior, cuánto se gana, qué se mueve y qué está parado.
    /// Solo lectura; los detalles están en Reportes (Rentabilidad, Ventas, Cuentas por cobrar).
    /// </summary>
    public class PanelDuenoModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public PanelDuenoModel(ApplicationDbContext context) => _context = context;

        public class Fila { public string Nombre { get; set; } = ""; public decimal Valor { get; set; } public decimal Extra { get; set; } }

        public decimal VentasMes { get; set; }
        public decimal VentasMesAnteriorMismoPeriodo { get; set; }
        public decimal VariacionPct { get; set; }
        public int TransaccionesMes { get; set; }
        public decimal TicketPromedio { get; set; }
        public decimal IngresosLineas { get; set; }
        public decimal CostoLineas { get; set; }
        public decimal UtilidadBruta => IngresosLineas - CostoLineas;
        public decimal MargenPct => IngresosLineas > 0 ? Math.Round(UtilidadBruta / IngresosLineas * 100, 1) : 0;
        public decimal PorCobrar { get; set; }
        public decimal PorCobrarVencido { get; set; }
        public decimal ValorInventario { get; set; }
        public decimal CapitalParado { get; set; }
        public int ProductosParados { get; set; }
        public List<Fila> TopUtilidad { get; set; } = new();
        public List<Fila> TopClientes { get; set; } = new();
        public List<Fila> Parados { get; set; } = new();
        public string GraficoJson { get; set; } = "{}";
        public string MesActualTexto { get; set; } = "";

        public IActionResult OnGet()
        {
            var u = _context.Usuarios.AsNoTracking().FirstOrDefault(x => x.Username == User.Identity!.Name);
            var rol = u?.Rol ?? "Pendiente";
            if (rol != "Admin" && !PermisoHelper.TienePermiso(_context, rol, "Reportes", "PanelDueno", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver este panel.";
                return RedirectToPage("/Index");
            }

            var hoy = DateTime.Today;
            var inicio = new DateTime(hoy.Year, hoy.Month, 1);
            var fin = inicio.AddMonths(1);
            var inicioAnt = inicio.AddMonths(-1);
            var diaCorte = Math.Min(hoy.Day, DateTime.DaysInMonth(inicioAnt.Year, inicioAnt.Month));
            var hastaAnt = inicioAnt.AddDays(diaCorte); // mismo avance del mes pasado (exclusivo)
            MesActualTexto = inicio.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-HN"));

            var ventasMes = _context.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= inicio && v.Fecha < fin && v.Estado == "Completada")
                .Select(v => new { v.Fecha, v.Total, v.ClienteId }).ToList();
            var ventasAnt = _context.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= inicioAnt && v.Fecha < hastaAnt && v.Estado == "Completada")
                .Select(v => new { v.Fecha, v.Total }).ToList();

            VentasMes = ventasMes.Sum(v => v.Total);
            VentasMesAnteriorMismoPeriodo = ventasAnt.Sum(v => v.Total);
            TransaccionesMes = ventasMes.Count;
            TicketPromedio = TransaccionesMes > 0 ? Math.Round(VentasMes / TransaccionesMes, 2) : 0;
            VariacionPct = VentasMesAnteriorMismoPeriodo > 0
                ? Math.Round((VentasMes - VentasMesAnteriorMismoPeriodo) / VentasMesAnteriorMismoPeriodo * 100, 1)
                : 0;

            // Gráfico: acumulado diario de este mes vs el mes pasado
            var labels = new List<string>(); var actual = new List<decimal?>(); var anterior = new List<decimal?>();
            decimal accA = 0, accP = 0;
            var diasMes = DateTime.DaysInMonth(inicio.Year, inicio.Month);
            var diasAntMes = DateTime.DaysInMonth(inicioAnt.Year, inicioAnt.Month);
            var ventasAntCompleto = _context.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= inicioAnt && v.Fecha < inicio && v.Estado == "Completada")
                .Select(v => new { v.Fecha, v.Total }).ToList();
            for (int d = 1; d <= diasMes; d++)
            {
                labels.Add(d.ToString());
                if (d <= hoy.Day)
                {
                    accA += ventasMes.Where(v => v.Fecha.Day == d).Sum(v => v.Total);
                    actual.Add(accA);
                }
                else actual.Add(null);
                if (d <= diasAntMes)
                {
                    accP += ventasAntCompleto.Where(v => v.Fecha.Day == d).Sum(v => v.Total);
                    anterior.Add(accP);
                }
                else anterior.Add(null);
            }
            GraficoJson = JsonSerializer.Serialize(new { labels, actual, anterior });

            // Utilidad por producto (líneas de venta del mes)
            var lineas = _context.DetalleVentas.AsNoTracking()
                .Join(_context.Ventas.AsNoTracking().Where(v => v.Fecha >= inicio && v.Fecha < fin && v.Estado == "Completada"),
                      d => d.VentaId, v => v.Id, (d, v) => new { d.ProductoId, d.Descripcion, d.Subtotal, d.CostoTotalLinea })
                .ToList();
            IngresosLineas = lineas.Sum(l => l.Subtotal);
            CostoLineas = lineas.Sum(l => l.CostoTotalLinea);

            var nombres = _context.Productos.AsNoTracking().Select(p => new { p.Id, p.Nombre }).ToList().ToDictionary(p => p.Id, p => p.Nombre);
            TopUtilidad = lineas.GroupBy(l => l.ProductoId)
                .Select(g => new Fila
                {
                    Nombre = nombres.GetValueOrDefault(g.Key, g.First().Descripcion ?? "Producto"),
                    Valor = g.Sum(x => x.Subtotal - x.CostoTotalLinea),
                    Extra = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(f => f.Valor).Take(8).ToList();

            // Mejores clientes del mes
            var idsCli = ventasMes.Where(v => v.ClienteId != null).Select(v => v.ClienteId!.Value).Distinct().ToList();
            var cli = _context.Clientes.AsNoTracking().Where(c => idsCli.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Nombre);
            TopClientes = ventasMes.Where(v => v.ClienteId != null)
                .GroupBy(v => v.ClienteId!.Value)
                .Select(g => new Fila { Nombre = cli.GetValueOrDefault(g.Key, "Cliente"), Valor = g.Sum(x => x.Total), Extra = g.Count() })
                .OrderByDescending(f => f.Valor).Take(8).ToList();

            // Cuentas por cobrar
            var cxc = _context.Ventas.AsNoTracking()
                .Where(v => v.EsCredito && v.SaldoPendiente > 0 && v.Estado != "Anulada")
                .Select(v => new { v.SaldoPendiente, v.Fecha, v.FechaVencimientoCredito }).ToList();
            PorCobrar = cxc.Sum(v => v.SaldoPendiente);
            PorCobrarVencido = cxc.Where(v => (v.FechaVencimientoCredito ?? v.Fecha).Date < hoy).Sum(v => v.SaldoPendiente);

            // Inventario: valor total y capital parado (sin ventas en 60 días)
            var desde60 = hoy.AddDays(-60);
            var vendidos60 = _context.DetalleVentas.AsNoTracking()
                .Join(_context.Ventas.AsNoTracking().Where(v => v.Fecha >= desde60 && v.Estado != "Anulada"),
                      d => d.VentaId, v => v.Id, (d, v) => d.ProductoId)
                .Distinct().ToList();
            var set60 = new HashSet<int>(vendidos60);
            var prods = _context.Productos.AsNoTracking().Where(p => p.Activo && p.Stock > 0).ToList();
            ValorInventario = prods.Sum(p => p.Stock * p.PrecioCompra);
            var parados = prods.Where(p => !set60.Contains(p.Id)).ToList();
            ProductosParados = parados.Count;
            CapitalParado = parados.Sum(p => p.Stock * p.PrecioCompra);
            Parados = parados.OrderByDescending(p => p.Stock * p.PrecioCompra).Take(8)
                .Select(p => new Fila { Nombre = p.Nombre, Valor = p.Stock * p.PrecioCompra, Extra = p.Stock }).ToList();

            return Page();
        }
    }
}
