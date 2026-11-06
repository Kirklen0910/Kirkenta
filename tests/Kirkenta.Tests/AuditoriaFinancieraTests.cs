using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>Errores encontrados en la revisión financiera: cada prueba repite el caso que estaba mal.</summary>
    [Collection("BD")]
    public class AuditoriaFinancieraTests
    {
        private static CuentaFinanciera Cuenta(ApplicationDbContext ctx, string codigo, string tipo, decimal saldo)
        {
            var c = new CuentaFinanciera { Codigo = codigo, Nombre = codigo, Tipo = tipo, Subtipo = tipo == "Caja" ? "General" : null, SaldoActual = saldo, Activa = true };
            ctx.CuentasFinancieras.Add(c);
            ctx.SaveChanges();
            return c;
        }

        private static MovimientoFinanciero Mov(ApplicationDbContext ctx, int cuentaId, string tipo, decimal monto, string? forma, DateTime? fecha = null, int? destino = null)
        {
            var m = new MovimientoFinanciero
            {
                Numero = "M-" + Guid.NewGuid().ToString("N")[..6], Tipo = tipo, Fecha = fecha ?? DateTime.Now, CuentaId = cuentaId, CuentaDestinoId = destino,
                Monto = monto, MonedaId = 1, TipoCambio = 1, FormaPago = forma, Concepto = "Prueba", Origen = "Manual", Estado = "Activo"
            };
            ctx.MovimientosFinancieros.Add(m);
            ctx.SaveChanges();
            SaldoHelper.Aplicar(ctx, m);
            ctx.SaveChanges();
            return m;
        }

        private static decimal Saldo(ApplicationDbContext ctx, int id) => ctx.CuentasFinancieras.Single(c => c.Id == id).SaldoActual;

        // ===== Factura 000-001-01-00001006: gravado impreso 16,035.72 cuando el real era 18,040.19 =====
        [Fact]
        public void RecuadroFiscal_NoRestaDosVecesElDescuentoDeLasLineas()
        {
            using var ctx = TestDb.Crear();
            var factura = new Factura { Subtotal = 20044.66m, Descuento = 2004.47m, Impuestos = 2706.03m, Total = 20746.22m };
            var lineas = new List<DetalleFactura>
            {
                new() { Cantidad = 1, PrecioUnitario = 20044.66m, Descuento = 2004.47m, Subtotal = 18040.19m, ImpuestoPorcentaje = 15, Total = 20746.22m }
            };

            var d = ISVHelper.DesglosarFactura(ctx, factura, lineas);

            Assert.Equal(18040.19m, d.BaseGravableTotal);
            Assert.Equal(2706.03m, d.ISVTotal);
            Assert.Equal(20746.22m, d.Total);
        }

        [Fact]
        public void RecuadroFiscal_RespetaElDescuentoGeneralEnMontoQueNoEstaEnLasLineas()
        {
            using var ctx = TestDb.Crear();
            var factura = new Factura { Subtotal = 1000m, Descuento = 100m, Impuestos = 135m, Total = 1035m };
            var lineas = new List<DetalleFactura>
            {
                new() { Cantidad = 10, PrecioUnitario = 100m, Descuento = 0, Subtotal = 1000m, ImpuestoPorcentaje = 15, Total = 1150m }
            };

            var d = ISVHelper.DesglosarFactura(ctx, factura, lineas);

            Assert.Equal(900m, d.BaseGravableTotal);
            Assert.Equal(135m, d.ISVTotal);
        }

        // ===== Redondeo: el servidor y la pantalla deben dar el mismo centavo =====
        [Fact]
        public void Isv_UsaRedondeoComercial()
        {
            using var ctx = TestDb.Crear();
            ctx.Impuestos.Add(new Impuesto { Nombre = "ISV", Porcentaje = 15, Activo = true, EsPredeterminado = true });
            ctx.SaveChanges();
            // 0.30 × 15% = 0.045: el redondeo bancario daba 0.04 en el servidor y la pantalla mostraba 0.05
            var par = new List<ItemParaISV> { new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 0.30m } };
            Assert.Equal(0.05m, ISVHelper.Calcular(ctx, par).ISVTotal);
        }

        // ===== Arqueo =====
        [Fact]
        public void Arqueo_CuentaEgresosManualesDeHoyYTransferenciasDeLaCaja()
        {
            using var ctx = TestDb.Crear();
            var u = TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            var caja = Cuenta(ctx, "CAJA", "Caja", 1000m);
            var banco = Cuenta(ctx, "BANCO", "Banco", 0m);
            AperturaHelper.Abrir(ctx, caja.Id, 1000m, null, u.Id);

            // egreso sin forma de pago: en una caja es efectivo
            Mov(ctx, caja.Id, "Egreso", 100m, null);
            // transferencia al banco con rótulo «Transferencia»: los billetes salieron igual
            Mov(ctx, caja.Id, "Transferencia", 200m, "Transferencia", destino: banco.Id);

            Assert.Equal(700m, CierreHelper.Calcular(ctx, caja.Id, DateTime.Today).EfectivoEsperado);
        }

        [Fact]
        public void FechaDeHoyEscritaComoDia_LlevaLaHoraActual_ParaEntrarAlTurno()
        {
            var ahora = FormaPagoHelper.FechaReal(DateTime.Today);
            Assert.Equal(DateTime.Today, ahora.Date);
            Assert.True(ahora > DateTime.Today);
            var ayer = DateTime.Today.AddDays(-1);
            Assert.Equal(ayer, FormaPagoHelper.FechaReal(ayer));          // un día pasado se queda como lo escribieron
        }

        [Fact]
        public void Cierre_LasTarjetasPasanAlBanco_YLaSiguienteAperturaNoTieneFaltante()
        {
            using var ctx = TestDb.Crear();
            var u = TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            var caja = Cuenta(ctx, "CAJA", "Caja", 500m);
            var banco = Cuenta(ctx, "BANCO", "Banco", 0m);
            AperturaHelper.Abrir(ctx, caja.Id, 500m, null, u.Id);

            Mov(ctx, caja.Id, "Ingreso", 300m, "Efectivo");
            Mov(ctx, caja.Id, "Ingreso", 1000m, "Tarjeta");
            Mov(ctx, caja.Id, "Ingreso", 250m, "Transferencia");

            var (cierre, err) = CierreHelper.Registrar(ctx, caja.Id, DateTime.Today, 800m, null, 0m, u.Id);
            Assert.Null(err);
            Assert.Equal("Cuadrado", cierre.Resultado);

            Assert.Equal(800m, Saldo(ctx, caja.Id));       // solo los billetes
            Assert.Equal(1250m, Saldo(ctx, banco.Id));     // tarjetas y transferencias

            var (ap, errA) = AperturaHelper.Abrir(ctx, caja.Id, 800m, null, u.Id);
            Assert.Null(errA);
            Assert.DoesNotContain(ctx.MovimientosFinancieros, m => m.Concepto.Contains("Faltante"));
        }

        [Fact]
        public void Cierre_SinBanco_ElNoEfectivoVaALaCuentaPuente()
        {
            using var ctx = TestDb.Crear();
            var u = TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            var caja = Cuenta(ctx, "CAJA", "Caja", 0m);
            AperturaHelper.Abrir(ctx, caja.Id, 0m, null, u.Id);
            Mov(ctx, caja.Id, "Ingreso", 400m, "Tarjeta");

            var (_, err) = CierreHelper.Registrar(ctx, caja.Id, DateTime.Today, 0m, null, 0m, u.Id);

            Assert.Null(err);
            Assert.Equal(0m, Saldo(ctx, caja.Id));
            Assert.Equal(400m, ctx.CuentasFinancieras.Single(c => c.Codigo == EfectivoPorDepositarHelper.CodigoCuenta).SaldoActual);
        }

        [Fact]
        public void Cierre_ReembolsoPorTarjetaMayorAlCobrado_RegresaDelBanco()
        {
            using var ctx = TestDb.Crear();
            var u = TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            var caja = Cuenta(ctx, "CAJA", "Caja", 100m);
            var banco = Cuenta(ctx, "BANCO", "Banco", 500m);
            AperturaHelper.Abrir(ctx, caja.Id, 100m, null, u.Id);
            Mov(ctx, caja.Id, "Egreso", 60m, "Tarjeta");

            CierreHelper.Registrar(ctx, caja.Id, DateTime.Today, 100m, null, 0m, u.Id);

            Assert.Equal(100m, Saldo(ctx, caja.Id));
            Assert.Equal(440m, Saldo(ctx, banco.Id));
        }

        [Fact]
        public void Cierre_DentroDeLaTolerancia_IgualRegistraElCentavo()
        {
            using var ctx = TestDb.Crear();
            var u = TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            var caja = Cuenta(ctx, "CAJA", "Caja", 100m);
            AperturaHelper.Abrir(ctx, caja.Id, 100m, null, u.Id);

            var (cierre, _) = CierreHelper.Registrar(ctx, caja.Id, DateTime.Today, 99.50m, null, 20m, u.Id);

            Assert.Equal("Cuadrado", cierre.Resultado);    // se ve cuadrado...
            Assert.Equal(99.50m, Saldo(ctx, caja.Id));     // ...pero el saldo es lo contado
        }

        [Fact]
        public void Cierre_FaltanteMayorAlSaldo_SeRegistraIgual()
        {
            using var ctx = TestDb.Crear();
            var u = TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            var caja = Cuenta(ctx, "CAJA", "Caja", 100m);
            AperturaHelper.Abrir(ctx, caja.Id, 100m, null, u.Id);
            Mov(ctx, caja.Id, "Ingreso", 300m, "Efectivo");

            var (cierre, err) = CierreHelper.Registrar(ctx, caja.Id, DateTime.Today, 50m, null, 0m, u.Id);

            Assert.Null(err);
            Assert.NotNull(cierre.MovimientoAjusteId);
            Assert.Equal(50m, Saldo(ctx, caja.Id));
        }

        // ===== Formas de pago =====
        [Theory]
        [InlineData("Ingreso", "Efectivo", true)]
        [InlineData("Egreso", null, true)]
        [InlineData("Transferencia", "Transferencia", true)]
        [InlineData("Ingreso", "Tarjeta", false)]
        [InlineData("Ingreso", "Saldo a favor", false)]
        public void EsEfectivo(string tipo, string? forma, bool esperado)
            => Assert.Equal(esperado, FormaPagoHelper.EsEfectivo(new MovimientoFinanciero { Tipo = tipo, FormaPago = forma }));

        [Fact]
        public void MovimientoSoloContable_NuncaEsEfectivo()
            => Assert.False(FormaPagoHelper.EsEfectivo(new MovimientoFinanciero { Tipo = "Egreso", FormaPago = "Efectivo", Notas = SaldoHelper.MarcaSoloContable }));

        [Fact]
        public void MetodoTipoEfectivo_SeRotulaEfectivo_SeLlameComoSeLlame()
        {
            Assert.Equal("Efectivo", FormaPagoHelper.Rotulo(new MetodoPago { Nombre = "Contado", Tipo = "Efectivo" }));
            Assert.Equal("Tarjeta BAC", FormaPagoHelper.Rotulo(new MetodoPago { Nombre = "Tarjeta BAC", Tipo = "Tarjeta" }));
        }

        // ===== Consumidor final =====
        [Fact]
        public void ConsumidorFinal_NoEsElClienteUno_NiSeDuplica()
        {
            using var ctx = TestDb.Crear();
            ctx.Clientes.Add(new Cliente { Id = 1, Nombre = "Carlos Cardenas" });
            ctx.SaveChanges();

            var id = ClienteGenericoHelper.Id(ctx);

            Assert.NotEqual(1, id);
            Assert.Equal(ClienteGenericoHelper.Nombre, ctx.Clientes.Single(c => c.Id == id).Nombre);
            Assert.Equal(id, ClienteGenericoHelper.Id(ctx));
            Assert.Equal(2, ctx.Clientes.Count());
        }

        // ===== Segunda revisión: contabilidad y reportes =====
        [Fact]
        public void IngresosCobrados_NoIncluyenElCambioEntregado()
        {
            using var ctx = TestDb.Crear();
            var plan = new PlanCuenta { Codigo = "4.1.01", Nombre = "Ventas", Tipo = "Ingreso", Naturaleza = "Acreedora", Nivel = 3, EsMovimiento = true, Activa = true };
            ctx.PlanCuentas.Add(plan); ctx.SaveChanges();
            var catIngreso = new CategoriaFinanciera { Nombre = "Ventas al contado", Tipo = "Ingreso", Activa = true, PlanCuentaId = plan.Id };
            var catCambio = new CategoriaFinanciera { Nombre = "Cambio entregado", Tipo = "Egreso", Activa = true, PlanCuentaId = plan.Id };
            ctx.AddRange(catIngreso, catCambio); ctx.SaveChanges();
            var caja = Cuenta(ctx, "CAJA", "Caja", 0m);
            var ing = Mov(ctx, caja.Id, "Ingreso", 500m, "Efectivo"); ing.CategoriaId = catIngreso.Id;
            var cam = Mov(ctx, caja.Id, "Egreso", 400m, "Efectivo"); cam.CategoriaId = catCambio.Id;
            ctx.SaveChanges();

            var er = ContabilidadHelper.GenerarEstadoResultados(ctx, DateTime.Today, DateTime.Today);

            Assert.Equal(100m, er.IngresosCobrados);
        }

        [Fact]
        public void Balance_LaDepreciacionBajaLosActivos_YSigueCuadrando()
        {
            using var ctx = TestDb.Crear();
            var plan = new PlanCuenta { Codigo = "6.2.08", Nombre = "Depreciaciones", Tipo = "Gasto", Naturaleza = "Deudora", Nivel = 3, EsMovimiento = true, Activa = true };
            ctx.PlanCuentas.Add(plan); ctx.SaveChanges();
            var cat = new CategoriaFinanciera { Nombre = "Depreciación de activos fijos", Tipo = "Egreso", Activa = true, PlanCuentaId = plan.Id };
            ctx.CategoriasFinancieras.Add(cat); ctx.SaveChanges();
            var caja = Cuenta(ctx, "CAJA", "Caja", 0m);

            var (mov, err) = MovimientoManualHelper.Crear(ctx, "Egreso", DateTime.Now, caja.Id, null, cat.Id, 250m, "Depreciación", null, "Depreciacion", null, null, soloContable: true);
            Assert.Null(err);
            Assert.Equal(0m, Saldo(ctx, caja.Id));         // no sale dinero

            var b = ContabilidadHelper.GenerarBalanceGeneral(ctx, DateTime.Today);

            Assert.Equal(-250m, b.Activos.Single(l => l.Codigo == "1.2.02").Monto);
            Assert.Equal(0m, b.Diferencia);
        }

        [Fact]
        public void Balance_CuentaInactivaConDinero_SigueEnLosActivos()
        {
            using var ctx = TestDb.Crear();
            var c = Cuenta(ctx, "VIEJA", "Caja", 75m);
            c.Activa = false; ctx.SaveChanges();

            Assert.Contains(ContabilidadHelper.GenerarBalanceGeneral(ctx, DateTime.Today).Activos, l => l.Monto == 75m);
        }

        [Fact]
        public void Reportes_LoDevueltoSeMideSinIsv_YElCostoSoloSiRegresoAlInventario()
        {
            using var ctx = TestDb.Crear();
            var venta = new Venta { Numero = "V-1", Total = 115m, Subtotal = 100m, Impuestos = 15m, Estado = "Completada", Fecha = DateTime.Now };
            ctx.Ventas.Add(venta); ctx.SaveChanges();
            var det = new DetalleVenta { VentaId = venta.Id, ProductoId = 7, Cantidad = 2, PrecioUnitario = 50m, Subtotal = 100m, Total = 115m };
            ctx.DetalleVentas.Add(det); ctx.SaveChanges();
            var d1 = new Devolucion { Numero = "D-1", VentaId = venta.Id, ClienteId = 1, Fecha = DateTime.Now, Total = 57.5m, Estado = "Aplicada", ReingresaInventario = true };
            var d2 = new Devolucion { Numero = "D-2", VentaId = venta.Id, ClienteId = 1, Fecha = DateTime.Now, Total = 57.5m, Estado = "Aplicada", ReingresaInventario = false };
            ctx.Devoluciones.AddRange(d1, d2); ctx.SaveChanges();
            ctx.DetalleDevoluciones.AddRange(
                new DetalleDevolucion { DevolucionId = d1.Id, ProductoId = 7, DetalleVentaId = det.Id, Cantidad = 1, PrecioUnitario = 57.5m, CostoUnitario = 30m },
                new DetalleDevolucion { DevolucionId = d2.Id, ProductoId = 7, DetalleVentaId = det.Id, Cantidad = 1, PrecioUnitario = 57.5m, CostoUnitario = 30m });
            ctx.SaveChanges();

            var r = DevolucionVentaHelper.DevueltoSinIsv(ctx, DateTime.Today, DateTime.Today.AddDays(1));

            Assert.Equal(100m, r.Sum(x => x.Base));   // no 115
            Assert.Equal(30m, r.Sum(x => x.Costo));   // solo la que regresó
        }

        [Fact]
        public void Vale_LaUltimaCuotaLimpiaElSaldo()
        {
            Assert.Equal(33.34m, Kirkenta.Helpers.RRHH.NominaHelper.CuotaVale(new ValeEmpleado { MontoCuota = 33.33m, SaldoPendiente = 33.34m }));
            Assert.Equal(33.33m, Kirkenta.Helpers.RRHH.NominaHelper.CuotaVale(new ValeEmpleado { MontoCuota = 33.33m, SaldoPendiente = 66.67m }));
        }

        // ===== Devoluciones parciales =====
        [Fact]
        public void TresDevolucionesParciales_DevuelvenExactamenteLoVendido()
        {
            using var ctx = TestDb.Crear();
            var u = TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            Cuenta(ctx, "CAJA", "Caja", 100m);
            ctx.CategoriasFinancieras.Add(new CategoriaFinanciera { Nombre = "Devoluciones en ventas", Tipo = "Egreso", Activa = true });
            var cliente = new Cliente { Nombre = "Ana" };
            var prod = new Producto { Nombre = "P", Stock = 0 };
            ctx.AddRange(cliente, prod);
            ctx.SaveChanges();
            var venta = new Venta { Numero = "V-1", ClienteId = cliente.Id, Total = 10m, Subtotal = 10m, Estado = "Completada", Fecha = DateTime.Now, UsuarioCreoId = u.Id };
            ctx.Ventas.Add(venta); ctx.SaveChanges();
            var det = new DetalleVenta { VentaId = venta.Id, ProductoId = prod.Id, Cantidad = 3, PrecioUnitario = 3.3333m, Subtotal = 10m, Total = 10m };
            ctx.DetalleVentas.Add(det); ctx.SaveChanges();

            decimal devuelto = 0;
            for (var i = 0; i < 3; i++)
            {
                var (dev, err) = DevolucionVentaHelper.Crear(ctx, new DevolucionVentaHelper.Solicitud
                {
                    VentaId = venta.Id, Motivo = "x", UsuarioId = u.Id, FormaReembolso = "Efectivo",
                    Items = new() { new DevolucionVentaHelper.ItemSolicitud { DetalleVentaId = det.Id, Cantidad = 1 } }
                });
                Assert.Null(err);
                devuelto += dev!.Total;
            }

            Assert.Equal(10m, devuelto);
        }
    }
}
