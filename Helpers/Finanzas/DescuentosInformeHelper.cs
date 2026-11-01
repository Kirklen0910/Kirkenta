using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    public class LineaDescuento
    {
        public int VentaId { get; set; }
        public string Venta { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string Cliente { get; set; } = "Consumidor final";
        public int ProductoId { get; set; }
        public string Producto { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Bruto { get; set; }
        public decimal Descuento { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class DescuentosInforme
    {
        public decimal TotalDescuento { get; set; }
        public decimal VentasBrutas { get; set; }
        public int VentasConDescuento { get; set; }
        public decimal PorcentajeSobreVentas => VentasBrutas > 0 ? Math.Round(TotalDescuento / VentasBrutas * 100m, 2) : 0m;
        public List<LineaDescuento> Lineas { get; set; } = new();
    }

    /// <summary>Descuentos otorgados en un rango (para el informe del cierre de caja): qué producto, en qué venta y a qué cliente.</summary>
    public static class DescuentosInformeHelper
    {
        public static DescuentosInforme Generar(ApplicationDbContext db, DateTime desde, DateTime hastaExcl)
        {
            var inf = new DescuentosInforme();

            var lineasVenta = (from d in db.DetalleVentas.AsNoTracking()
                               join v in db.Ventas.AsNoTracking() on d.VentaId equals v.Id
                               where v.Fecha >= desde && v.Fecha < hastaExcl && v.Estado != "Anulada"
                               select new
                               {
                                   v.Id, v.Numero, v.Fecha, v.ClienteId,
                                   d.ProductoId, d.Descripcion, d.Cantidad, d.PrecioUnitario, d.Descuento
                               }).ToList();

            inf.VentasBrutas = lineasVenta.Sum(x => x.Cantidad * x.PrecioUnitario);
            var conDesc = lineasVenta.Where(x => x.Descuento > 0).ToList();
            inf.TotalDescuento = conDesc.Sum(x => x.Descuento);
            inf.VentasConDescuento = conDesc.Select(x => x.Id).Distinct().Count();
            if (conDesc.Count == 0) return inf;

            var prodIds = conDesc.Select(x => x.ProductoId).Distinct().ToList();
            var cliIds = conDesc.Where(x => x.ClienteId.HasValue).Select(x => x.ClienteId!.Value).Distinct().ToList();
            var productos = db.Productos.AsNoTracking().Where(p => prodIds.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Nombre);
            var clientes = db.Clientes.AsNoTracking().Where(c => cliIds.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Nombre);

            inf.Lineas = conDesc.OrderBy(x => x.Fecha).Select(x =>
            {
                var bruto = x.Cantidad * x.PrecioUnitario;
                return new LineaDescuento
                {
                    VentaId = x.Id, Venta = x.Numero, Fecha = x.Fecha,
                    Cliente = x.ClienteId.HasValue && clientes.TryGetValue(x.ClienteId.Value, out var c) ? c : "Consumidor final",
                    ProductoId = x.ProductoId,
                    Producto = !string.IsNullOrWhiteSpace(x.Descripcion) ? x.Descripcion! : (productos.TryGetValue(x.ProductoId, out var n) ? n : "—"),
                    Cantidad = x.Cantidad, PrecioUnitario = x.PrecioUnitario, Bruto = bruto, Descuento = x.Descuento,
                    Porcentaje = bruto > 0 ? Math.Round(x.Descuento / bruto * 100m, 2) : 0m
                };
            }).ToList();
            return inf;
        }
    }
}
