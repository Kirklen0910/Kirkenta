using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class AnulacionFacturaTests
    {
        [Fact]
        public void Anular_FacturaSinVenta_DevuelveStock_YMarcaAnulada()
        {
            using var ctx = TestDb.Crear();
            var prod = new Producto { Nombre = "Producto test", Stock = 3, PrecioCompra = 10, PrecioVenta = 20, Activo = true };
            ctx.Productos.Add(prod);
            ctx.SaveChanges();

            var cot = new Cotizacion { Numero = "COT-1", ClienteId = 1, Estado = "Convertida" };
            ctx.Cotizaciones.Add(cot);
            var fac = new Factura { Numero = "F-1", ClienteId = 1, Total = 40, Saldo = 40, Estado = "Emitida" };
            ctx.Facturas.Add(fac);
            ctx.SaveChanges();
            cot.FacturaId = fac.Id;
            ctx.DetalleFacturas.Add(new DetalleFactura { FacturaId = fac.Id, ProductoId = prod.Id, Cantidad = 2, PrecioUnitario = 20, Subtotal = 40, Total = 40 });
            ctx.SaveChanges();

            var (ok, error) = AnulacionFacturaHelper.Anular(ctx, fac.Id, "Error de captura", usuarioId: 1);

            Assert.True(ok, error);
            Assert.Equal("Anulada", ctx.Facturas.First(f => f.Id == fac.Id).Estado);
            Assert.Equal(5, ctx.Productos.First(p => p.Id == prod.Id).Stock);
            var c = ctx.Cotizaciones.First(x => x.Id == cot.Id);
            Assert.Equal("Aceptada", c.Estado);
            Assert.Null(c.FacturaId);
            Assert.True(ctx.MovimientosInventario.Any(m => m.ProductoId == prod.Id && m.Origen == "AnulacionFactura"));
        }

        [Fact]
        public void Anular_RequiereMotivo()
        {
            using var ctx = TestDb.Crear();
            var fac = new Factura { Numero = "F-2", ClienteId = 1, Estado = "Emitida" };
            ctx.Facturas.Add(fac);
            ctx.SaveChanges();

            var (ok, error) = AnulacionFacturaHelper.Anular(ctx, fac.Id, "  ", 1);
            Assert.False(ok);
            Assert.Contains("motivo", error!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Anular_FacturaDeUnaVenta_SeRechaza()
        {
            using var ctx = TestDb.Crear();
            var fac = new Factura { Numero = "F-3", ClienteId = 1, Estado = "Emitida", VentaId = 99 };
            ctx.Facturas.Add(fac);
            ctx.SaveChanges();

            var (ok, error) = AnulacionFacturaHelper.Anular(ctx, fac.Id, "x", 1);
            Assert.False(ok);
            Assert.Contains("venta", error!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Anular_DosVeces_SegundaSeRechaza()
        {
            using var ctx = TestDb.Crear();
            var fac = new Factura { Numero = "F-4", ClienteId = 1, Estado = "Emitida" };
            ctx.Facturas.Add(fac);
            ctx.SaveChanges();

            Assert.True(AnulacionFacturaHelper.Anular(ctx, fac.Id, "motivo", 1).ok);
            var (ok2, _) = AnulacionFacturaHelper.Anular(ctx, fac.Id, "motivo", 1);
            Assert.False(ok2);
        }
    }
}
