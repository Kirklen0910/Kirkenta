using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class ConversionInventarioTests
    {
        [Fact]
        public void Aprobar_producto_nuevo_traslada_costo_sin_perdida()
        {
            using var ctx = TestDb.Crear();
            var lance = new Producto { Nombre = "Lance tubo", Stock = 10, PrecioCompra = 200m };
            ctx.Productos.Add(lance);
            var conv = new ConversionInventario
            {
                Numero = "CNV-1", ProductoOrigenId = 0, CantidadOrigen = 1, CantidadDestino = 20,
                NuevoProductoNombre = "Tubo por pie", NuevoProductoUnidad = "Pie", NuevoProductoPrecioVenta = 15m
            };
            ctx.SaveChanges();
            conv.ProductoOrigenId = lance.Id;
            ctx.ConversionesInventario.Add(conv);
            ctx.SaveChanges();

            var error = ConversionInventarioHelper.Aprobar(ctx, conv, 1);
            ctx.SaveChanges();

            Assert.Null(error);
            Assert.Equal(9m, lance.Stock);
            var pie = ctx.Productos.First(p => p.Nombre == "Tubo por pie");
            Assert.Equal(20m, pie.Stock);
            Assert.Equal(10m, pie.PrecioCompra);       // 200 / 20
            Assert.Equal(15m, pie.PrecioVenta);
            Assert.Equal(200m, conv.CostoTotal);
            Assert.Equal("Aprobada", conv.Estado);
            // Valor del inventario no cambia: sale 200, entra 20 x 10
            Assert.Equal(2, ctx.MovimientosInventario.Count(m => m.Origen == "Conversión"));
        }

        [Fact]
        public void Aprobar_producto_existente_promedia_costo()
        {
            using var ctx = TestDb.Crear();
            var a = new Producto { Nombre = "Bolsa", Stock = 5, PrecioCompra = 940m };
            var b = new Producto { Nombre = "Cemento libra", Stock = 100, PrecioCompra = 12m };
            ctx.Productos.AddRange(a, b);
            ctx.SaveChanges();
            var conv = new ConversionInventario { Numero = "CNV-2", ProductoOrigenId = a.Id, ProductoDestinoId = b.Id, CantidadOrigen = 1, CantidadDestino = 94 };
            ctx.ConversionesInventario.Add(conv);
            ctx.SaveChanges();

            Assert.Null(ConversionInventarioHelper.Aprobar(ctx, conv, 1));
            Assert.Equal(4m, a.Stock);
            Assert.Equal(194m, b.Stock);
            // (100*12 + 94*10) / 194
            Assert.Equal(Math.Round((1200m + 940m) / 194m, 4), b.PrecioCompra);
        }

        [Fact]
        public void No_aprueba_sin_stock_ni_dos_veces()
        {
            using var ctx = TestDb.Crear();
            var a = new Producto { Nombre = "A", Stock = 1, PrecioCompra = 10m };
            var b = new Producto { Nombre = "B", Stock = 0, PrecioCompra = 1m };
            ctx.Productos.AddRange(a, b);
            ctx.SaveChanges();
            var conv = new ConversionInventario { Numero = "CNV-3", ProductoOrigenId = a.Id, ProductoDestinoId = b.Id, CantidadOrigen = 5, CantidadDestino = 5 };
            ctx.ConversionesInventario.Add(conv);
            ctx.SaveChanges();

            Assert.Contains("Stock insuficiente", ConversionInventarioHelper.Aprobar(ctx, conv, 1));
            conv.CantidadOrigen = 1;
            Assert.Null(ConversionInventarioHelper.Aprobar(ctx, conv, 1));
            Assert.NotNull(ConversionInventarioHelper.Aprobar(ctx, conv, 1));
            Assert.Equal(0m, a.Stock);
        }
    }
}
