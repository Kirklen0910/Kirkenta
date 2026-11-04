using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class InformeDetalleVentasTests
    {
        [Fact]
        public void CadaVentaVaEnLaSeccionDeSuMetodo_ConSuFacturaYTotalesQueCuadran()
        {
            using var ctx = TestDb.Crear();
            var efectivo = new MetodoPago { Nombre = "Efectivo", Tipo = "Efectivo", Activo = true };
            var tarjeta = new MetodoPago { Nombre = "Tarjeta", Tipo = "Tarjeta", Activo = true };
            ctx.MetodosPago.AddRange(efectivo, tarjeta);
            var cli = new Cliente { Nombre = "Ana Pérez" };
            ctx.Clientes.Add(cli);
            ctx.SaveChanges();
            var hoy = DateTime.Now;

            var v1 = new Venta { Numero = "VEN-1", Fecha = hoy, ClienteId = cli.Id, Subtotal = 100m, Descuento = 10m, Impuestos = 13.5m, Total = 103.5m, Estado = "Completada" };
            var v2 = new Venta { Numero = "VEN-2", Fecha = hoy, ClienteId = cli.Id, Subtotal = 200m, Impuestos = 30m, Total = 230m, Estado = "Completada" };
            var v3 = new Venta { Numero = "VEN-3", Fecha = hoy, ClienteId = cli.Id, Subtotal = 50m, Impuestos = 7.5m, Total = 57.5m, Estado = "Completada", EsCredito = true, SaldoPendiente = 57.5m };
            ctx.Ventas.AddRange(v1, v2, v3);
            ctx.SaveChanges();
            ctx.Facturas.Add(new Factura { Numero = "000-001-01-00000012", VentaId = v1.Id, ClienteId = cli.Id, Total = 103.5m, Estado = "Pagada" });
            ctx.Pagos.AddRange(
                new Pago { VentaId = v1.Id, ClienteId = cli.Id, Fecha = hoy, Monto = 103.5m, MetodoPagoId = efectivo.Id },
                new Pago { VentaId = v2.Id, ClienteId = cli.Id, Fecha = hoy, Monto = 115m, MetodoPagoId = efectivo.Id },
                new Pago { VentaId = v2.Id, ClienteId = cli.Id, Fecha = hoy, Monto = 115m, MetodoPagoId = tarjeta.Id, Referencia = "POS 4411" });
            ctx.SaveChanges();

            var inf = InformeMetodosPagoHelper.Generar(ctx, hoy.Date, hoy.Date.AddDays(1));
            Assert.Equal(new[] { "Efectivo", "Tarjeta", "Crédito (por cobrar)" }, inf.Secciones.Select(s => s.Nombre));

            var ef = inf.Secciones[0];
            var fila1 = ef.Filas.Single(f => f.Venta == "VEN-1");
            Assert.Equal("000-001-01-00000012", fila1.Factura);
            Assert.Equal("Ana Pérez", fila1.Cliente);
            Assert.Equal((100m, 10m, 13.5m, 103.5m), (fila1.Subtotal, fila1.Descuento, fila1.Isv, fila1.Total));

            // VEN-2 pagada mitad y mitad: a cada sección le toca su parte
            var mitad = ef.Filas.Single(f => f.Venta == "VEN-2");
            Assert.Equal((100m, 15m, 115m, 230m), (mitad.Subtotal, mitad.Isv, mitad.Total, mitad.TotalDocumento!.Value));
            Assert.Equal("POS 4411", inf.Secciones[1].Filas.Single().Referencia);
            Assert.Equal(inf.TotalCobrado, inf.Secciones.Where(s => !s.EsCredito).Sum(s => s.Total));

            var cred = inf.Secciones[2];
            Assert.True(cred.EsCredito);
            Assert.Equal(57.5m, cred.Total);
        }
    }
}
