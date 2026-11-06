using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    public class SugerenciaCompra
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = "";
        public string? Sku { get; set; }
        public decimal Stock { get; set; }
        public decimal Minimo { get; set; }
        public decimal Vendido { get; set; }
        public decimal DemandaDiaria { get; set; }
        /// <summary>Días que alcanza el stock actual al ritmo de venta (null = sin ventas).</summary>
        public decimal? DiasRestantes { get; set; }
        public decimal EnCamino { get; set; }
        public decimal Sugerido { get; set; }
        public int? ProveedorId { get; set; }
        public string Proveedor { get; set; } = "Sin proveedor anterior";
        public decimal CostoUnitario { get; set; }
        public decimal CostoTotal => Math.Round(Sugerido * CostoUnitario, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Propone cuánto pedir de cada producto según lo que realmente se vende:
    ///   sugerido = demanda diaria × días de cobertura + stock mínimo − stock − lo que ya viene en camino.
    /// El proveedor y el costo salen de la última orden de compra donde apareció el producto.
    /// </summary>
    public static class SugerenciaComprasHelper
    {
        public static decimal Calcular(decimal demandaDiaria, int diasCobertura, decimal minimo, decimal stock, decimal enCamino)
        {
            var necesidad = demandaDiaria * diasCobertura + minimo - stock - enCamino;
            return necesidad <= 0 ? 0 : Math.Ceiling(necesidad);
        }

        public static List<SugerenciaCompra> Generar(ApplicationDbContext db, int diasHistorial = 30, int diasCobertura = 30)
        {
            diasHistorial = Math.Clamp(diasHistorial, 7, 365);
            diasCobertura = Math.Clamp(diasCobertura, 7, 180);
            var desde = DateTime.Today.AddDays(-diasHistorial);

            // Unidades vendidas por producto en el período (sin ventas anuladas)
            var vendidos = db.DetalleVentas.AsNoTracking()
                .Join(db.Ventas.AsNoTracking().Where(v => v.Fecha >= desde && v.Estado != "Anulada"),
                      d => d.VentaId, v => v.Id, (d, v) => new { d.ProductoId, d.Cantidad })
                .GroupBy(x => x.ProductoId)
                .Select(g => new { ProductoId = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .ToList()
                .ToDictionary(x => x.ProductoId, x => x.Cantidad);

            // Lo que ya está pedido y todavía no llegó
            var estadosAbiertos = new List<string> { "Enviada", "RecibidaParcial" };
            var enCamino = db.DetalleOrdenesCompra.AsNoTracking()
                .Join(db.OrdenesCompra.AsNoTracking().Where(o => estadosAbiertos.Contains(o.Estado)),
                      d => d.OrdenCompraId, o => o.Id, (d, o) => new { d.ProductoId, Pendiente = d.Cantidad - d.CantidadRecibida })
                .GroupBy(x => x.ProductoId)
                .Select(g => new { ProductoId = g.Key, Pendiente = g.Sum(x => x.Pendiente) })
                .ToList()
                .ToDictionary(x => x.ProductoId, x => Math.Max(0, x.Pendiente));

            var productos = db.Productos.AsNoTracking().Where(p => p.Activo).ToList();
            var lista = new List<SugerenciaCompra>();
            foreach (var p in productos)
            {
                vendidos.TryGetValue(p.Id, out var vend);
                enCamino.TryGetValue(p.Id, out var camino);
                var demanda = vend / diasHistorial;
                var sugerido = Calcular(demanda, diasCobertura, p.StockMinimo, p.Stock, camino);
                if (sugerido <= 0) continue;

                lista.Add(new SugerenciaCompra
                {
                    ProductoId = p.Id,
                    Nombre = p.Nombre,
                    Sku = p.SKU,
                    Stock = p.Stock,
                    Minimo = p.StockMinimo,
                    Vendido = vend,
                    DemandaDiaria = Math.Round(demanda, 2, MidpointRounding.AwayFromZero),
                    DiasRestantes = demanda > 0 ? Math.Round(Math.Max(0, p.Stock) / demanda, 1) : null,
                    EnCamino = camino,
                    Sugerido = sugerido,
                    CostoUnitario = p.PrecioCompra
                });
            }
            if (lista.Count == 0) return lista;

            // Proveedor y costo de la última compra de cada producto sugerido
            var ids = lista.Select(s => s.ProductoId).ToList();
            var ultimas = db.DetalleOrdenesCompra.AsNoTracking()
                .Where(d => ids.Contains(d.ProductoId))
                .Join(db.OrdenesCompra.AsNoTracking(), d => d.OrdenCompraId, o => o.Id,
                      (d, o) => new { d.ProductoId, o.ProveedorId, o.Fecha, d.PrecioUnitario })
                .ToList()
                .GroupBy(x => x.ProductoId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Fecha).First());

            var provIds = ultimas.Values.Select(u => u.ProveedorId).Distinct().ToList();
            var proveedores = db.Proveedores.AsNoTracking().Where(p => provIds.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Nombre);

            foreach (var s in lista)
            {
                if (!ultimas.TryGetValue(s.ProductoId, out var u)) continue;
                s.ProveedorId = u.ProveedorId;
                s.Proveedor = proveedores.GetValueOrDefault(u.ProveedorId, "Sin proveedor anterior");
                if (u.PrecioUnitario > 0) s.CostoUnitario = u.PrecioUnitario;
            }

            return lista
                .OrderBy(s => s.Proveedor == "Sin proveedor anterior" ? 1 : 0)
                .ThenBy(s => s.Proveedor)
                .ThenBy(s => s.DiasRestantes ?? decimal.MaxValue)
                .ToList();
        }
    }
}
