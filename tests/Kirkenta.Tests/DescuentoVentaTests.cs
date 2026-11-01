using Kirkenta.Data;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Kirkenta.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>Descuento general en %, descuento por línea, productos sin descuento y envío (octubre 2026).</summary>
    [Collection("BD")]
    public class DescuentoVentaTests
    {
        private static (ApplicationDbContext ctx, VentaService svc, Producto conDesc, Producto sinDesc, MetodoPago efectivo) Preparar()
        {
            var ctx = TestDb.Crear();
            TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            var caja = new CuentaFinanciera { Codigo = "C1", Nombre = "Caja 1", Tipo = "Caja", Subtipo = "POS", SaldoActual = 0m, Activa = true };
            ctx.CuentasFinancieras.Add(caja);
            ctx.Impuestos.Add(new Impuesto { Nombre = "ISV 15%", Porcentaje = 15m, EsPredeterminado = true, Activo = true });
            ctx.CategoriasFinancieras.Add(new CategoriaFinanciera { Tipo = "Ingreso", Nombre = "Ventas al contado", Activa = true });
            var mp = new MetodoPago { Nombre = "Efectivo", Tipo = "Efectivo", Activo = true };
            ctx.MetodosPago.Add(mp);
            var a = new Producto { Nombre = "Con descuento", SKU = "A", PrecioVenta = 100m, PrecioCompra = 60m, Stock = 50m, Activo = true, PermiteDescuento = true };
            var b = new Producto { Nombre = "Sin descuento", SKU = "B", PrecioVenta = 50m, PrecioCompra = 30m, Stock = 50m, Activo = true, PermiteDescuento = false };
            ctx.Productos.AddRange(a, b);
            ctx.SaveChanges();
            ctx.AperturasCaja.Add(new AperturaCaja { Numero = "AP-1", CuentaId = caja.Id, UsuarioAbreId = 1, SaldoInicial = 0m, Activa = true });
            ctx.SaveChanges();
            var svc = new VentaService(ctx, NullLogger<VentaService>.Instance);
            return (ctx, svc, a, b, mp);
        }

        private static VentaRequest Pedido(MetodoPago mp, Producto a, Producto b, decimal pct, decimal descLineaA = 0m, decimal monto = 1000m)
            => new()
            {
                MetodoPagoId = mp.Id,
                Monto = monto,
                DescuentoGeneralPorcentaje = pct,
                Items = new List<ItemVentaRequest>
                {
                    new() { ProductoId = a.Id, Cantidad = 2m, PrecioUnitario = 100m, Descuento = descLineaA },
                    new() { ProductoId = b.Id, Cantidad = 1m, PrecioUnitario = 50m }
                }
            };

        [Fact]
        public async Task Descuento_general_solo_se_reparte_en_productos_que_admiten_descuento()
        {
            var (ctx, svc, a, b, mp) = Preparar();
            using var _ = ctx;

            var r = await svc.RegistrarVentaAsync(Pedido(mp, a, b, 10m), "cajero", null);

            Assert.True(r.Exito, r.Error);
            // A: 200 − 10% = 180; B: 50 sin descuento; ISV 15% sobre 230 = 34.50
            Assert.Equal(264.50m, r.Total);
            var dets = ctx.DetalleVentas.Where(d => d.VentaId == r.VentaId).ToList();
            Assert.Equal(20m, dets.First(d => d.ProductoId == a.Id).Descuento);
            Assert.Equal(0m, dets.First(d => d.ProductoId == b.Id).Descuento);
            Assert.Equal(180m, dets.First(d => d.ProductoId == a.Id).Subtotal);
        }

        [Fact]
        public async Task Descuento_de_linea_y_general_se_acumulan()
        {
            var (ctx, svc, a, b, mp) = Preparar();
            using var _ = ctx;

            // Línea A con 20 de descuento propio (neto 180) + 10% general (18) = 162
            var r = await svc.RegistrarVentaAsync(Pedido(mp, a, b, 10m, descLineaA: 20m), "cajero", null);

            Assert.True(r.Exito, r.Error);
            var detA = ctx.DetalleVentas.First(d => d.VentaId == r.VentaId && d.ProductoId == a.Id);
            Assert.Equal(38m, detA.Descuento);
            Assert.Equal(162m, detA.Subtotal);
            // (162 + 50) * 1.15 = 243.80
            Assert.Equal(243.80m, r.Total);
        }

        [Fact]
        public async Task Producto_sin_descuento_rechaza_descuento_propio()
        {
            var (ctx, svc, a, b, mp) = Preparar();
            using var _ = ctx;

            var req = Pedido(mp, a, b, 0m);
            req.Items[1].Descuento = 5m;
            var r = await svc.RegistrarVentaAsync(req, "cajero", null);

            Assert.False(r.Exito);
            Assert.Contains("no admite descuento", r.Error);
            Assert.Empty(ctx.Ventas);
        }

        [Fact]
        public async Task Porcentaje_fuera_de_rango_se_rechaza()
        {
            var (ctx, svc, a, b, mp) = Preparar();
            using var _ = ctx;

            var r = await svc.RegistrarVentaAsync(Pedido(mp, a, b, 120m), "cajero", null);

            Assert.False(r.Exito);
            Assert.Empty(ctx.Ventas);
        }

        [Fact]
        public async Task Envio_no_se_descuenta_paga_isv_y_no_cuenta_como_utilidad()
        {
            var (ctx, svc, a, b, mp) = Preparar();
            using var _ = ctx;

            var req = Pedido(mp, a, b, 10m);
            req.RequiereEnvio = true;
            req.MontoEnvio = 40m;
            req.EnvioDireccion = "Col. Centro";
            req.EnvioContactoNombre = "Juan";
            req.EnvioContactoTelefono = "9999-9999";
            var r = await svc.RegistrarVentaAsync(req, "cajero", null);

            Assert.True(r.Exito, r.Error);
            // (180 + 50 + 40) * 1.15 = 310.50: el envío entra completo y paga ISV
            Assert.Equal(310.50m, r.Total);

            // De lo cobrado, la base del envío (40) se excluye de la utilidad
            var envios = ContabilidadHelper.EnviosCobradosPorAnio(ctx, null, DateTime.Now.AddDays(1));
            Assert.Equal(40m, Math.Round(envios.Values.Sum(), 2));

            // El ISV cobrado (310.50 - 270 = 40.50) es del fisco: sale de ingresos y utilidad
            var isv = ContabilidadHelper.IsvVentasNetoPorAnio(ctx, null, DateTime.Now.AddDays(1));
            Assert.Equal(40.50m, Math.Round(isv.Values.Sum(), 2));
        }
    }
}
