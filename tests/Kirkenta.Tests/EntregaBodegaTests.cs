using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class EntregaBodegaTests
    {
        private static (ApplicationDbContext ctx, Venta venta, DetalleVenta det, Usuario user) Preparar()
        {
            var ctx = TestDb.Crear();
            var user = TestDb.AgregarUsuario(ctx, "bodega", "Admin");
            var prod = new Producto { Nombre = "Cemento", Stock = 100, PrecioVenta = 200m, PrecioCompra = 150m };
            ctx.Productos.Add(prod);
            ctx.SaveChanges();
            var venta = new Venta { Numero = "VEN-9", Subtotal = 20000m, Total = 20000m, Estado = "Completada" };
            ctx.Ventas.Add(venta);
            ctx.SaveChanges();
            var det = new DetalleVenta { VentaId = venta.Id, ProductoId = prod.Id, Cantidad = 100, PrecioUnitario = 200m, Subtotal = 20000m, Total = 20000m };
            ctx.DetalleVentas.Add(det);
            ctx.SaveChanges();
            return (ctx, venta, det, user);
        }

        [Fact]
        public void Entregas_parciales_bajan_el_pendiente_hasta_cero()
        {
            var (ctx, venta, det, user) = Preparar();
            using var _ = ctx;

            var (e1, err1) = EntregaBodegaHelper.Crear(ctx, venta.Id, new() { new() { DetalleVentaId = det.Id, Cantidad = 60 } }, "Juan", null, user.Id);
            Assert.Null(err1);
            Assert.StartsWith("ENT-", e1!.Numero);
            Assert.True(venta.ControlEntrega);
            Assert.Equal(40m, EntregaBodegaHelper.TotalPendiente(ctx, venta.Id));

            var (e2, err2) = EntregaBodegaHelper.Crear(ctx, venta.Id, new() { new() { DetalleVentaId = det.Id, Cantidad = 40 } }, "Juan", null, user.Id);
            Assert.Null(err2);
            Assert.NotNull(e2);
            Assert.Equal(0m, EntregaBodegaHelper.TotalPendiente(ctx, venta.Id));
        }

        [Fact]
        public void No_permite_entregar_mas_de_lo_pendiente()
        {
            var (ctx, venta, det, user) = Preparar();
            using var _ = ctx;

            EntregaBodegaHelper.Crear(ctx, venta.Id, new() { new() { DetalleVentaId = det.Id, Cantidad = 90 } }, null, null, user.Id);
            var (e, error) = EntregaBodegaHelper.Crear(ctx, venta.Id, new() { new() { DetalleVentaId = det.Id, Cantidad = 11 } }, null, null, user.Id);

            Assert.Null(e);
            Assert.Contains("solo quedan", error);
            Assert.Equal(10m, EntregaBodegaHelper.TotalPendiente(ctx, venta.Id));
        }

        [Fact]
        public void Anular_comprobante_devuelve_la_cantidad_a_pendiente()
        {
            var (ctx, venta, det, user) = Preparar();
            using var _ = ctx;

            var (e, _) = EntregaBodegaHelper.Crear(ctx, venta.Id, new() { new() { DetalleVentaId = det.Id, Cantidad = 100 } }, null, null, user.Id);
            Assert.Equal(0m, EntregaBodegaHelper.TotalPendiente(ctx, venta.Id));

            Assert.NotNull(EntregaBodegaHelper.Anular(ctx, e!.Id, "", user.Id));           // exige motivo
            Assert.Null(EntregaBodegaHelper.Anular(ctx, e.Id, "Se cargó mal", user.Id));
            Assert.Equal(100m, EntregaBodegaHelper.TotalPendiente(ctx, venta.Id));
        }

        [Fact]
        public void No_se_puede_anular_una_venta_con_entregas_vigentes()
        {
            var (ctx, venta, det, user) = Preparar();
            using var _ = ctx;

            var (e, _) = EntregaBodegaHelper.Crear(ctx, venta.Id, new() { new() { DetalleVentaId = det.Id, Cantidad = 10 } }, null, null, user.Id);
            var (ok, error) = AnulacionVentaHelper.Anular(ctx, venta.Id, "Error", user.Id);
            Assert.False(ok);
            Assert.Contains("entrega", error);

            EntregaBodegaHelper.Anular(ctx, e!.Id, "Se revierte", user.Id);
            var (ok2, error2) = AnulacionVentaHelper.Anular(ctx, venta.Id, "Error", user.Id);
            Assert.True(ok2, error2);
        }
    }
}
