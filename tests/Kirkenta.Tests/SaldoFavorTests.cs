using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class SaldoFavorTests
    {
        private static (ApplicationDbContext ctx, Venta venta, DetalleVenta det, Usuario user, Cliente cliente) Preparar(bool conCliente)
        {
            var ctx = TestDb.Crear();
            var user = TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            ctx.CuentasFinancieras.Add(new CuentaFinanciera { Codigo = "C1", Nombre = "Caja", Tipo = "Caja", Subtipo = "POS", SaldoActual = 100m, Activa = true });
            var cliente = new Cliente { Nombre = "Cliente Uno" };
            ctx.Clientes.Add(cliente);
            var prod = new Producto { Nombre = "Cuaderno", Stock = 8, PrecioVenta = 50m, PrecioCompra = 30m };
            ctx.Productos.Add(prod);
            ctx.SaveChanges();

            var venta = new Venta
            {
                Numero = "VEN-1", Subtotal = 100m, Total = 100m, Estado = "Completada",
                CostoRegistrado = true, CostoTotalVendido = 60m, MovimientoRegistrado = true,
                ClienteId = conCliente ? cliente.Id : null
            };
            ctx.Ventas.Add(venta);
            ctx.SaveChanges();
            var det = new DetalleVenta
            {
                VentaId = venta.Id, ProductoId = prod.Id, Cantidad = 2, PrecioUnitario = 50m,
                Subtotal = 100m, Total = 100m, CostoUnitarioSnapshot = 30m, CostoTotalLinea = 60m
            };
            ctx.DetalleVentas.Add(det);
            ctx.SaveChanges();
            return (ctx, venta, det, user, cliente);
        }

        [Fact]
        public void Devolucion_como_saldo_a_favor_acredita_al_cliente_y_no_reembolsa()
        {
            var (ctx, venta, det, user, cliente) = Preparar(true);
            using var _ = ctx;

            var (dev, error) = DevolucionVentaHelper.Crear(ctx, new DevolucionVentaHelper.Solicitud
            {
                VentaId = venta.Id, Motivo = "Producto equivocado", UsuarioId = user.Id,
                FormaReembolso = "Saldo a favor",
                Items = new() { new() { DetalleVentaId = det.Id, Cantidad = 1 } }
            });

            Assert.Null(error);
            Assert.Equal(50m, dev!.MontoSaldoFavor);
            Assert.Equal(0m, dev.MontoReembolsado);
            Assert.Equal(50m, SaldoFavorHelper.Saldo(ctx, cliente.Id));
        }

        [Fact]
        public void Saldo_a_favor_exige_cliente_identificado()
        {
            var (ctx, venta, det, user, _) = Preparar(false);
            using var _ = ctx;

            var (dev, error) = DevolucionVentaHelper.Crear(ctx, new DevolucionVentaHelper.Solicitud
            {
                VentaId = venta.Id, Motivo = "Cambio", UsuarioId = user.Id,
                FormaReembolso = "Saldo a favor",
                Items = new() { new() { DetalleVentaId = det.Id, Cantidad = 1 } }
            });

            Assert.Null(dev);
            Assert.Contains("cliente identificado", error);
        }

        [Fact]
        public void Usar_descuenta_del_saldo()
        {
            var (ctx, venta, _, _, cliente) = Preparar(true);
            using var __ = ctx;

            SaldoFavorHelper.Acreditar(ctx, cliente.Id, 80m, "Prueba", null, venta.Id, null);
            SaldoFavorHelper.Usar(ctx, cliente.Id, 30m, "Venta", venta.Id, null);
            ctx.SaveChanges();

            Assert.Equal(50m, SaldoFavorHelper.Saldo(ctx, cliente.Id));
        }

        [Fact]
        public void Anular_venta_devuelve_el_saldo_a_favor_usado()
        {
            var (ctx, venta, _, user, cliente) = Preparar(true);
            using var __ = ctx;
            venta.SaldoFavorAplicado = 20m;
            ctx.SaveChanges();

            var (ok, error) = AnulacionVentaHelper.Anular(ctx, venta.Id, "Error de captura", user.Id);

            Assert.True(ok, error);
            Assert.Equal(20m, SaldoFavorHelper.Saldo(ctx, cliente.Id));
        }

        [Fact]
        public void Metodo_de_pago_saldo_a_favor_queda_inactivo_y_se_crea_una_sola_vez()
        {
            using var ctx = TestDb.Crear();
            var a = SaldoFavorHelper.AsegurarMetodoPago(ctx);
            var b = SaldoFavorHelper.AsegurarMetodoPago(ctx);
            Assert.Equal(a.Id, b.Id);
            Assert.False(a.Activo);
            Assert.Equal(1, ctx.MetodosPago.Count(m => m.Nombre == SaldoFavorHelper.NombreMetodo));
        }
    }
}
