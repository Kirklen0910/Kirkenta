using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class AuditoriaTests
    {
        private static int NuevoProducto(Kirkenta.Data.ApplicationDbContext ctx, bool activo = true, decimal stock = 10)
        {
            var p = new Producto { Nombre = "P", Stock = stock, PrecioCompra = 5, PrecioVenta = 10, Activo = activo };
            ctx.Productos.Add(p);
            ctx.SaveChanges();
            return p.Id;
        }

        private static string? Validar(Kirkenta.Data.ApplicationDbContext ctx, int prod, decimal cant, decimal precio, decimal desc, decimal imp)
            => LineasDocumentoHelper.Validar(ctx, new List<LineasDocumentoHelper.Linea>
            {
                new(prod, cant, precio, desc, imp)
            });

        [Fact]
        public void Lineas_Validas_NoDanError()
        {
            using var ctx = TestDb.Crear();
            var id = NuevoProducto(ctx);
            Assert.Null(Validar(ctx, id, 2, 10, 5, 15));
        }

        [Fact]
        public void Lineas_SinLineas_DanError()
        {
            using var ctx = TestDb.Crear();
            Assert.NotNull(LineasDocumentoHelper.Validar(ctx, new List<LineasDocumentoHelper.Linea>()));
        }

        [Theory]
        [InlineData(0, 10, 0, 15)]     // cantidad cero
        [InlineData(-1, 10, 0, 15)]    // cantidad negativa
        [InlineData(1, -10, 0, 15)]    // precio negativo
        [InlineData(1, 10, -1, 15)]    // descuento negativo
        [InlineData(1, 10, 11, 15)]    // descuento mayor al subtotal
        [InlineData(1, 10, 0, 101)]    // impuesto > 100
        [InlineData(1, 10, 0, -1)]     // impuesto negativo
        public void Lineas_Invalidas_DanError(decimal cant, decimal precio, decimal desc, decimal imp)
        {
            using var ctx = TestDb.Crear();
            var id = NuevoProducto(ctx);
            Assert.NotNull(Validar(ctx, id, cant, precio, desc, imp));
        }

        [Fact]
        public void Lineas_ProductoInactivoOInexistente_DaError()
        {
            using var ctx = TestDb.Crear();
            var inactivo = NuevoProducto(ctx, activo: false);
            Assert.NotNull(Validar(ctx, inactivo, 1, 10, 0, 0));
            Assert.NotNull(Validar(ctx, 9999, 1, 10, 0, 0));
        }

        [Fact]
        public void Inventario_Ajustar_DejaMovimientoEnKardex()
        {
            using var ctx = TestDb.Crear();
            var id = NuevoProducto(ctx, stock: 10);
            var prod = ctx.Productos.First(p => p.Id == id);

            var mov = InventarioHelper.Ajustar(ctx, prod, 7, "AjusteManual", usuarioId: 1, notas: "conteo");
            ctx.SaveChanges();

            Assert.NotNull(mov);
            Assert.Equal(7, ctx.Productos.First(p => p.Id == id).Stock);
            var m = ctx.MovimientosInventario.Single(x => x.ProductoId == id);
            Assert.Equal(-3, m.Cantidad);
            Assert.Equal(10, m.StockAnterior);
            Assert.Equal(7, m.StockPosterior);
            Assert.Equal("AjusteManual", m.Origen);
        }

        [Fact]
        public void Inventario_Ajustar_SinCambio_NoCreaMovimiento()
        {
            using var ctx = TestDb.Crear();
            var id = NuevoProducto(ctx, stock: 4);
            var prod = ctx.Productos.First(p => p.Id == id);
            Assert.Null(InventarioHelper.Ajustar(ctx, prod, 4, "AjusteManual", 1));
        }

        [Fact]
        public void NumeroDocumento_SinSerie_GeneraConsecutivos()
        {
            using var ctx = TestDb.Crear();
            var a = NumeroDocumentoHelper.GenerarSiguiente(ctx, "Cotizacion");
            Assert.StartsWith("COT-", a);
        }

        [Fact]
        public void NumeroDocumento_ConSerie_Incrementa()
        {
            using var ctx = TestDb.Crear();
            ctx.SeriesDocumentos.Add(new SerieDocumento
            {
                Tipo = "Pedido", Nombre = "Principal", Prefijo = "PED", Separador = "-",
                LongitudNumero = 4, SiguienteNumero = 1, EsPredeterminada = true, Activa = true
            });
            ctx.SaveChanges();

            var n1 = NumeroDocumentoHelper.GenerarSiguiente(ctx, "Pedido");
            var n2 = NumeroDocumentoHelper.GenerarSiguiente(ctx, "Pedido");

            Assert.Equal("PED-0001", n1);
            Assert.Equal("PED-0002", n2);
        }
    }
}
