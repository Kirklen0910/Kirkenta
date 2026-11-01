using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class InventarioKardexTests
    {
        [Fact]
        public void Registrar_entrada_sube_stock_y_deja_kardex()
        {
            using var ctx = TestDb.Crear();
            var p = new Producto { Nombre = "A", Stock = 10, PrecioCompra = 5m };
            ctx.Productos.Add(p);
            ctx.SaveChanges();

            var mov = InventarioHelper.Registrar(ctx, p, 4m, "Compra", "OC-1");
            ctx.SaveChanges();

            Assert.Equal(14m, p.Stock);
            Assert.Equal("Entrada", mov.Tipo);
            Assert.Equal(10m, mov.StockAnterior);
            Assert.Equal(14m, mov.StockPosterior);
            Assert.Equal(5m, mov.CostoUnitario);
        }

        [Fact]
        public void Registrar_salida_baja_stock()
        {
            using var ctx = TestDb.Crear();
            var p = new Producto { Nombre = "B", Stock = 10 };
            ctx.Productos.Add(p);
            ctx.SaveChanges();

            var mov = InventarioHelper.Registrar(ctx, p, -3m, "Venta", "V-1");

            Assert.Equal(7m, p.Stock);
            Assert.Equal("Salida", mov.Tipo);
        }

        [Fact]
        public void Ajustar_a_valor_exacto_y_sin_cambio_no_registra()
        {
            using var ctx = TestDb.Crear();
            var p = new Producto { Nombre = "C", Stock = 8 };
            ctx.Productos.Add(p);
            ctx.SaveChanges();

            Assert.Null(InventarioHelper.Ajustar(ctx, p, 8m, "Toma fisica", null));

            var mov = InventarioHelper.Ajustar(ctx, p, 5m, "Toma fisica", null);
            Assert.NotNull(mov);
            Assert.Equal(5m, p.Stock);
            Assert.Equal(-3m, mov!.Cantidad);
        }

        [Fact]
        public void Costo_promedio_ponderado()
        {
            // 10 a 5.00 + 10 a 7.00 = 6.00
            Assert.Equal(6m, InventarioHelper.CostoPromedio(10m, 5m, 10m, 7m));
        }

        [Fact]
        public void Costo_promedio_con_stock_negativo_usa_el_costo_de_entrada()
        {
            Assert.Equal(7m, InventarioHelper.CostoPromedio(-4m, 5m, 10m, 7m));
            Assert.Equal(9m, InventarioHelper.CostoPromedio(0m, 0m, 0m, 9m));
        }
    }
}
