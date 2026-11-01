using Kirkenta.Data;
using Kirkenta.Models;
using Kirkenta.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>
    /// Pruebas de las validaciones del POS que rechazan la venta ANTES de tocar datos.
    /// (El registro completo con movimientos financieros se prueba a mano en el sistema.)
    /// </summary>
    [Collection("BD")]
    public class VentaServiceValidacionTests
    {
        private static (ApplicationDbContext ctx, VentaService servicio, Producto producto) Preparar(bool conCaja = true)
        {
            var ctx = TestDb.Crear();
            TestDb.AgregarUsuario(ctx, "cajero", "Admin");

            if (conCaja)
                ctx.AperturasCaja.Add(new AperturaCaja { Numero = "AP-1", CuentaId = 1, Activa = true });

            ctx.MetodosPago.Add(new MetodoPago { Nombre = "Efectivo", Tipo = "Efectivo" });

            var producto = new Producto { Nombre = "Cuaderno", PrecioVenta = 50m, PrecioCompra = 30m, Stock = 10, Activo = true };
            ctx.Productos.Add(producto);
            ctx.SaveChanges();

            return (ctx, new VentaService(ctx, NullLogger<VentaService>.Instance), producto);
        }

        private static VentaRequest Solicitud(int productoId, decimal precio, decimal cantidad = 1, decimal descuento = 0) => new()
        {
            MetodoPagoId = 1,
            Monto = 1000m,
            Items = new List<ItemVentaRequest>
            {
                new() { ProductoId = productoId, Cantidad = cantidad, PrecioUnitario = precio, Descuento = descuento }
            }
        };

        [Fact]
        public async Task Sin_caja_abierta_rechaza_la_venta()
        {
            var (ctx, servicio, prod) = Preparar(conCaja: false);
            using var _ = ctx;

            var r = await servicio.RegistrarVentaAsync(Solicitud(prod.Id, 50m), "cajero", null);

            Assert.False(r.Exito);
            Assert.Contains("caja", r.Error!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Usuario_inexistente_es_sesion_no_valida()
        {
            var (ctx, servicio, prod) = Preparar();
            using var _ = ctx;

            var r = await servicio.RegistrarVentaAsync(Solicitud(prod.Id, 50m), "fantasma", null);

            Assert.False(r.Exito);
            Assert.Contains("Sesión", r.Error!);
        }

        [Fact]
        public async Task Carrito_vacio_se_rechaza()
        {
            var (ctx, servicio, _) = Preparar();
            using var _c = ctx;

            var r = await servicio.RegistrarVentaAsync(new VentaRequest { MetodoPagoId = 1 }, "cajero", null);

            Assert.False(r.Exito);
        }

        [Fact]
        public async Task Precio_distinto_al_del_servidor_se_rechaza_y_no_guarda_nada()
        {
            var (ctx, servicio, prod) = Preparar();
            using var _ = ctx;

            // El producto cuesta L. 50, pero el navegador manda L. 1
            var r = await servicio.RegistrarVentaAsync(Solicitud(prod.Id, 1m), "cajero", null);

            Assert.False(r.Exito);
            Assert.Contains("precio", r.Error!, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(ctx.Ventas);
            Assert.Equal(10, ctx.Productos.Single().Stock);
        }

        [Fact]
        public async Task Producto_inactivo_no_se_puede_vender()
        {
            var (ctx, servicio, prod) = Preparar();
            using var _ = ctx;
            prod.Activo = false;
            ctx.SaveChanges();

            var r = await servicio.RegistrarVentaAsync(Solicitud(prod.Id, 50m), "cajero", null);

            Assert.False(r.Exito);
            Assert.Contains("inactivo", r.Error!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Stock_insuficiente_se_rechaza()
        {
            var (ctx, servicio, prod) = Preparar();
            using var _ = ctx;

            var r = await servicio.RegistrarVentaAsync(Solicitud(prod.Id, 50m, cantidad: 11), "cajero", null);

            Assert.False(r.Exito);
            Assert.Contains("Stock insuficiente", r.Error!);
        }

        [Fact]
        public async Task Mismo_producto_en_dos_lineas_suma_cantidades_para_validar_stock()
        {
            var (ctx, servicio, prod) = Preparar();
            using var _ = ctx;

            var req = Solicitud(prod.Id, 50m, cantidad: 6);
            req.Items.Add(new ItemVentaRequest { ProductoId = prod.Id, Cantidad = 6, PrecioUnitario = 50m });

            var r = await servicio.RegistrarVentaAsync(req, "cajero", null);

            Assert.False(r.Exito);
            Assert.Contains("Stock insuficiente", r.Error!);
        }

        [Fact]
        public async Task Descuento_de_linea_mayor_al_subtotal_se_rechaza()
        {
            var (ctx, servicio, prod) = Preparar();
            using var _ = ctx;

            var r = await servicio.RegistrarVentaAsync(Solicitud(prod.Id, 50m, cantidad: 1, descuento: 80m), "cajero", null);

            Assert.False(r.Exito);
            Assert.Contains("descuento", r.Error!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Descuento_general_mayor_al_total_se_rechaza()
        {
            var (ctx, servicio, prod) = Preparar();
            using var _ = ctx;

            var req = Solicitud(prod.Id, 50m);
            req.Descuento = 500m;

            var r = await servicio.RegistrarVentaAsync(req, "cajero", null);

            Assert.False(r.Exito);
            Assert.Contains("descuento", r.Error!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Cotizacion_no_aceptada_se_rechaza()
        {
            var (ctx, servicio, prod) = Preparar();
            using var _ = ctx;
            ctx.Cotizaciones.Add(new Cotizacion { Numero = "COT-1", ClienteId = 1, Estado = "Borrador" });
            ctx.SaveChanges();

            var req = Solicitud(prod.Id, 50m);
            req.CotizacionId = ctx.Cotizaciones.Single().Id;

            var r = await servicio.RegistrarVentaAsync(req, "cajero", null);

            Assert.False(r.Exito);
            Assert.Contains("aceptada", r.Error!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Cotizacion_inexistente_se_rechaza()
        {
            var (ctx, servicio, prod) = Preparar();
            using var _ = ctx;

            var req = Solicitud(prod.Id, 50m);
            req.CotizacionId = 9999;

            var r = await servicio.RegistrarVentaAsync(req, "cajero", null);

            Assert.False(r.Exito);
        }

        [Fact]
        public async Task Precio_de_cotizacion_aceptada_distinto_al_de_lista_pasa_la_validacion_de_precio()
        {
            var (ctx, servicio, prod) = Preparar();
            using var _ = ctx;
            ctx.Cotizaciones.Add(new Cotizacion { Numero = "COT-2", ClienteId = 1, Estado = "Aceptada" });
            ctx.SaveChanges();
            var cotId = ctx.Cotizaciones.Single().Id;
            ctx.DetalleCotizaciones.Add(new DetalleCotizacion { CotizacionId = cotId, ProductoId = prod.Id, Cantidad = 1, PrecioUnitario = 40m });
            ctx.SaveChanges();

            // Precio acordado en la cotización (40) en vez del de lista (50)
            var req = Solicitud(prod.Id, 40m);
            req.CotizacionId = cotId;

            var r = await servicio.RegistrarVentaAsync(req, "cajero", null);

            // Puede fallar más adelante (la BD en memoria no tiene cuentas financieras), pero NO por el precio
            if (!r.Exito)
                Assert.DoesNotContain("precio", r.Error ?? "", StringComparison.OrdinalIgnoreCase);
        }
    }
}
