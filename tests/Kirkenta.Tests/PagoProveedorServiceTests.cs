using Kirkenta.Data;
using Kirkenta.Models;
using Kirkenta.Services;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class PagoProveedorServiceTests
    {
        private static (ApplicationDbContext ctx, OrdenCompra orden, MetodoPago metodo) Escenario(decimal saldo, string estado = "Recibida")
        {
            var ctx = TestDb.Crear();
            var metodo = new MetodoPago { Nombre = "Efectivo" };
            ctx.MetodosPago.Add(metodo);
            var orden = new OrdenCompra { Numero = "OC-1", ProveedorId = 1, Estado = estado, Total = saldo, Saldo = saldo };
            ctx.OrdenesCompra.Add(orden);
            ctx.SaveChanges();
            return (ctx, orden, metodo);
        }

        private static PagoProveedorService.Solicitud Pedir(OrdenCompra o, MetodoPago m, decimal monto) => new()
        {
            OrdenCompraId = o.Id,
            Monto = monto,
            MetodoPagoId = m.Id,
            RegistrarEnFinanzas = false
        };

        [Fact]
        public void Pago_parcial_baja_el_saldo_y_la_orden_sigue_abierta()
        {
            var (ctx, orden, metodo) = Escenario(1000m);
            using var _ = ctx;

            var r = PagoProveedorService.Registrar(ctx, Pedir(orden, metodo, 400m), 1);

            Assert.True(r.Ok, r.Error);
            Assert.Equal(600m, ctx.OrdenesCompra.First().Saldo);
            Assert.Equal("Recibida", ctx.OrdenesCompra.First().Estado);
            Assert.Single(ctx.PagosProveedor.ToList());
        }

        [Fact]
        public void Pago_total_deja_la_orden_en_Pagada()
        {
            var (ctx, orden, metodo) = Escenario(250m);
            using var _ = ctx;

            var r = PagoProveedorService.Registrar(ctx, Pedir(orden, metodo, 250m), 1);

            Assert.True(r.Ok, r.Error);
            var o = ctx.OrdenesCompra.First();
            Assert.Equal(0m, o.Saldo);
            Assert.Equal("Pagada", o.Estado);
        }

        [Fact]
        public void No_permite_pagar_mas_que_el_saldo()
        {
            var (ctx, orden, metodo) = Escenario(100m);
            using var _ = ctx;

            var r = PagoProveedorService.Registrar(ctx, Pedir(orden, metodo, 100.01m), 1);

            Assert.False(r.Ok);
            Assert.Equal("Input.Monto", r.Campo);
            Assert.Empty(ctx.PagosProveedor.ToList());
            Assert.Equal(100m, ctx.OrdenesCompra.First().Saldo);
        }

        [Fact]
        public void No_permite_pagar_ordenes_que_no_estan_recibidas()
        {
            var (ctx, orden, metodo) = Escenario(100m, estado: "Borrador");
            using var _ = ctx;

            var r = PagoProveedorService.Registrar(ctx, Pedir(orden, metodo, 10m), 1);

            Assert.False(r.Ok);
            Assert.Empty(ctx.PagosProveedor.ToList());
        }

        [Fact]
        public void Rechaza_monto_cero_y_metodo_inexistente()
        {
            var (ctx, orden, metodo) = Escenario(100m);
            using var _ = ctx;

            Assert.False(PagoProveedorService.Registrar(ctx, Pedir(orden, metodo, 0m), 1).Ok);

            var malo = Pedir(orden, metodo, 10m);
            malo.MetodoPagoId = 9999;
            Assert.False(PagoProveedorService.Registrar(ctx, malo, 1).Ok);
            Assert.Empty(ctx.PagosProveedor.ToList());
        }

        [Fact]
        public void Un_mes_cerrado_bloquea_el_pago()
        {
            var (ctx, orden, metodo) = Escenario(100m);
            using var _ = ctx;
            var hoy = DateTime.Today;
            Kirkenta.Helpers.Finanzas.CierreContableHelper.Cerrar(ctx, hoy.Year, hoy.Month, 1);

            var r = PagoProveedorService.Registrar(ctx, Pedir(orden, metodo, 10m), 1);

            Assert.False(r.Ok);
            Assert.Empty(ctx.PagosProveedor.ToList());
        }
    }
}
