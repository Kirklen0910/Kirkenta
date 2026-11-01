using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class FacturacionFiscalTests
    {
        private static ApplicationDbContext ConEmpresa(bool activa, string ini = "000-001-01-00000005",
            string fin = "000-001-01-00000007", int diasLimite = 30)
        {
            var ctx = TestDb.Crear();
            ctx.ConfiguracionEmpresa.Add(new ConfiguracionEmpresa
            {
                Nombre = "Test",
                FacturacionFiscalActiva = activa,
                CAI = "AAAAAA-BBBBBB-CCCCCC-DDDDDD",
                RangoInicial = ini,
                RangoFinal = fin,
                FechaLimiteEmision = DateTime.Today.AddDays(diasLimite)
            });
            ctx.SaveChanges();
            return ctx;
        }

        [Fact]
        public void Inactiva_usa_numeracion_interna()
        {
            using var ctx = ConEmpresa(activa: false);
            var f = FacturacionFiscalHelper.Previsualizar(ctx);
            Assert.False(f.Activa);

            var factura = new Factura();
            var (numero, error, _) = FacturacionFiscalHelper.AsignarNumero(ctx, factura);
            Assert.NotNull(numero);
            Assert.Null(error);
            Assert.Null(factura.CAI);
        }

        [Fact]
        public void Activa_numera_dentro_del_rango_y_guarda_snapshot()
        {
            using var ctx = ConEmpresa(activa: true);

            var f1 = new Factura();
            var (n1, _, _) = FacturacionFiscalHelper.AsignarNumero(ctx, f1);
            ctx.Facturas.Add(f1);
            ctx.SaveChanges();

            var f2 = new Factura();
            var (n2, _, _) = FacturacionFiscalHelper.AsignarNumero(ctx, f2);

            Assert.Equal("000-001-01-00000005", n1);
            Assert.Equal("000-001-01-00000006", n2);
            Assert.Equal("AAAAAA-BBBBBB-CCCCCC-DDDDDD", f1.CAI);
            Assert.Equal("000-001-01-00000007", f1.RangoFinal);
        }

        [Fact]
        public void Rango_agotado_bloquea()
        {
            using var ctx = ConEmpresa(activa: true, ini: "000-001-01-00000001", fin: "000-001-01-00000002");
            for (int i = 0; i < 2; i++)
            {
                var f = new Factura();
                FacturacionFiscalHelper.AsignarNumero(ctx, f);
                ctx.Facturas.Add(f);
                ctx.SaveChanges();
            }

            var (numero, error, _) = FacturacionFiscalHelper.AsignarNumero(ctx, new Factura());
            Assert.Null(numero);
            Assert.Contains("agot", error!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Cai_vencido_bloquea()
        {
            using var ctx = ConEmpresa(activa: true, diasLimite: -1);
            var (numero, error, _) = FacturacionFiscalHelper.AsignarNumero(ctx, new Factura());
            Assert.Null(numero);
            Assert.Contains("venci", error!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Validar_configuracion_detecta_prefijos_distintos_y_rangos_invertidos()
        {
            var limite = DateTime.Today.AddDays(10);
            Assert.NotNull(FacturacionFiscalHelper.ValidarConfiguracion("X", "000-001-01-00000001", "000-002-01-00000010", limite));
            Assert.NotNull(FacturacionFiscalHelper.ValidarConfiguracion("X", "000-001-01-00000010", "000-001-01-00000001", limite));
            Assert.NotNull(FacturacionFiscalHelper.ValidarConfiguracion("", "000-001-01-00000001", "000-001-01-00000010", limite));
            Assert.Null(FacturacionFiscalHelper.ValidarConfiguracion("X", "000-001-01-00000001", "000-001-01-00000010", limite));
        }
    }

    [Collection("BD")]
    public class InventarioTests
    {
        [Fact]
        public void Registrar_cambia_stock_y_deja_kardex()
        {
            using var ctx = TestDb.Crear();
            var p = new Producto { Nombre = "A", Stock = 10, PrecioCompra = 5m };
            ctx.Productos.Add(p);
            ctx.SaveChanges();

            InventarioHelper.Registrar(ctx, p, -3, "Venta", "VEN-1", 1, null);
            ctx.SaveChanges();

            var mov = Assert.Single(ctx.MovimientosInventario);
            Assert.Equal(7, p.Stock);
            Assert.Equal(10, mov.StockAnterior);
            Assert.Equal(7, mov.StockPosterior);
            Assert.Equal("Salida", mov.Tipo);
        }

        [Fact]
        public void Ajustar_sin_diferencia_no_crea_movimiento()
        {
            using var ctx = TestDb.Crear();
            var p = new Producto { Nombre = "A", Stock = 10 };
            ctx.Productos.Add(p);
            ctx.SaveChanges();

            Assert.Null(InventarioHelper.Ajustar(ctx, p, 10, "AjusteManual", null));
            Assert.NotNull(InventarioHelper.Ajustar(ctx, p, 12, "AjusteManual", null));
        }

        [Fact]
        public void Costo_promedio_ponderado()
        {
            // 10 u a 100 + 10 u a 200 = 150
            Assert.Equal(150m, InventarioHelper.CostoPromedio(10, 100m, 10, 200m));
            // sin stock previo toma el costo de entrada
            Assert.Equal(80m, InventarioHelper.CostoPromedio(0, 999m, 5, 80m));
            // stock negativo no contamina el costo
            Assert.Equal(80m, InventarioHelper.CostoPromedio(-3, 999m, 5, 80m));
        }
    }

    [Collection("BD")]
    public class DevolucionAnulacionTests
    {
        private static (ApplicationDbContext ctx, Producto prod, Venta venta, DetalleVenta det, CuentaFinanciera caja, Usuario user) Preparar()
        {
            var ctx = TestDb.Crear();
            var user = TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            var caja = new CuentaFinanciera { Codigo = "C1", Nombre = "Caja", Tipo = "Caja", Subtipo = "POS", SaldoActual = 100m, Activa = true };
            ctx.CuentasFinancieras.Add(caja);

            var prod = new Producto { Nombre = "Cuaderno", Stock = 8, PrecioVenta = 50m, PrecioCompra = 30m };
            ctx.Productos.Add(prod);
            ctx.SaveChanges();

            var venta = new Venta
            {
                Numero = "VEN-1", Subtotal = 100m, Total = 100m, Estado = "Completada",
                CostoRegistrado = true, CostoTotalVendido = 60m, MovimientoRegistrado = true
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

            return (ctx, prod, venta, det, caja, user);
        }

        [Fact]
        public void Devolucion_parcial_reingresa_stock_y_reembolsa()
        {
            var (ctx, prod, venta, det, caja, user) = Preparar();
            using var _ = ctx;

            var (dev, error) = DevolucionVentaHelper.Crear(ctx, new DevolucionVentaHelper.Solicitud
            {
                VentaId = venta.Id,
                Motivo = "Defectuoso",
                UsuarioId = user.Id,
                Items = new() { new() { DetalleVentaId = det.Id, Cantidad = 1 } }
            });

            Assert.Null(error);
            Assert.NotNull(dev);
            Assert.Equal(50m, dev!.Total);
            Assert.Equal(50m, dev.MontoReembolsado);
            Assert.Equal(9, prod.Stock);
            Assert.Equal(50m, caja.SaldoActual);               // 100 - 50 de reembolso
            Assert.Contains(ctx.MovimientosInventario, m => m.Origen == "Devolucion" && m.Cantidad == 1);
            Assert.Equal(30m, dev.CostoRevertido);
        }

        [Fact]
        public void Devolucion_no_puede_superar_lo_vendido()
        {
            var (ctx, _, venta, det, _, user) = Preparar();
            using var __ = ctx;

            var (dev, error) = DevolucionVentaHelper.Crear(ctx, new DevolucionVentaHelper.Solicitud
            {
                VentaId = venta.Id, Motivo = "x", UsuarioId = user.Id,
                Items = new() { new() { DetalleVentaId = det.Id, Cantidad = 3 } }
            });

            Assert.Null(dev);
            Assert.NotNull(error);
        }

        [Fact]
        public void Segunda_devolucion_respeta_lo_ya_devuelto()
        {
            var (ctx, _, venta, det, _, user) = Preparar();
            using var __ = ctx;

            DevolucionVentaHelper.Crear(ctx, new DevolucionVentaHelper.Solicitud
            {
                VentaId = venta.Id, Motivo = "x", UsuarioId = user.Id,
                Items = new() { new() { DetalleVentaId = det.Id, Cantidad = 2 } }
            });

            var (dev2, error2) = DevolucionVentaHelper.Crear(ctx, new DevolucionVentaHelper.Solicitud
            {
                VentaId = venta.Id, Motivo = "x", UsuarioId = user.Id,
                Items = new() { new() { DetalleVentaId = det.Id, Cantidad = 1 } }
            });

            Assert.Null(dev2);
            Assert.NotNull(error2);
        }

        [Fact]
        public void Devolucion_a_credito_descuenta_saldo_sin_mover_caja()
        {
            var (ctx, _, venta, det, caja, user) = Preparar();
            using var __ = ctx;
            venta.EsCredito = true;
            venta.SaldoPendiente = 100m;
            ctx.SaveChanges();

            var (dev, error) = DevolucionVentaHelper.Crear(ctx, new DevolucionVentaHelper.Solicitud
            {
                VentaId = venta.Id, Motivo = "x", UsuarioId = user.Id,
                Items = new() { new() { DetalleVentaId = det.Id, Cantidad = 1 } }
            });

            Assert.Null(error);
            Assert.Equal(50m, dev!.MontoAplicadoSaldo);
            Assert.Equal(0m, dev.MontoReembolsado);
            Assert.Equal(50m, venta.SaldoPendiente);
            Assert.Equal(100m, caja.SaldoActual);
        }

        [Fact]
        public void Anular_venta_regresa_stock_y_revierte_finanzas()
        {
            var (ctx, prod, venta, _, caja, user) = Preparar();
            using var __ = ctx;

            // ingreso de la venta ya aplicado a caja
            var ingreso = new MovimientoFinanciero
            {
                Numero = "MOV-1", Tipo = "Ingreso", CuentaId = caja.Id, Monto = 100m,
                Origen = "Venta", OrigenId = venta.Id, Estado = "Activo", Concepto = "Venta"
            };
            ctx.MovimientosFinancieros.Add(ingreso);
            caja.SaldoActual = 200m;
            ctx.SaveChanges();

            var (ok, error) = AnulacionVentaHelper.Anular(ctx, venta.Id, "Error de captura", user.Id);

            Assert.True(ok, error);
            Assert.Equal("Anulada", venta.Estado);
            Assert.Equal(10, prod.Stock);                      // 8 + 2
            Assert.Equal("Anulado", ingreso.Estado);
            Assert.Equal(100m, caja.SaldoActual);              // 200 - 100
            Assert.Contains(ctx.MovimientosInventario, m => m.Origen == "AnulacionVenta" && m.Cantidad == 2);
        }

        [Fact]
        public void Anular_exige_motivo_y_no_permite_doble_anulacion()
        {
            var (ctx, _, venta, _, _, user) = Preparar();
            using var __ = ctx;

            Assert.False(AnulacionVentaHelper.Anular(ctx, venta.Id, "  ", user.Id).ok);
            Assert.True(AnulacionVentaHelper.Anular(ctx, venta.Id, "motivo", user.Id).ok);
            Assert.False(AnulacionVentaHelper.Anular(ctx, venta.Id, "motivo", user.Id).ok);
        }
    }
}
