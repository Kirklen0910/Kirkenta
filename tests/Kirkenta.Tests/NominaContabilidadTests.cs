using Kirkenta.Data;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>
    /// Nómina de punta a punta: el gasto es el bruto más los aportes, lo retenido es deuda hasta pagarle a la entidad,
    /// y en cada paso el balance debe seguir cuadrando.
    /// </summary>
    [Collection("BD")]
    public class NominaContabilidadTests
    {
        private static (ApplicationDbContext ctx, CuentaFinanciera banco, Empleado emp, Nomina nomina, DetalleNomina det) Base(decimal salario = 40000m)
        {
            var ctx = TestDb.Crear();
            PlanCuentasHelper.Seed(ctx);
            var banco = new CuentaFinanciera { Codigo = "BCO", Nombre = "Banco", Tipo = "Banco", SaldoActual = 100000m, SaldoInicial = 100000m, Activa = true };
            ctx.CuentasFinancieras.Add(banco);
            var emp = new Empleado { Nombres = "Ana", Apellidos = "Paz", SalarioBase = salario, CotizaIHSS = true, CotizaRAP = true, AplicaISR = true, Estado = "Activo", FrecuenciaPago = "Mensual" };
            ctx.Empleados.Add(emp);
            ctx.SaveChanges();

            var nomina = new Nomina { Numero = "NOM-1", Tipo = "Mensual", PeriodoDescripcion = "x", FechaInicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), FechaFin = DateTime.Today, FechaPago = DateTime.Today, Estado = "Aprobada" };
            ctx.Nominas.Add(nomina);
            ctx.SaveChanges();
            var det = NominaHelper.CalcularDetalleEmpleado(ctx, emp, "Mensual", 30, nomina.FechaInicio, nomina.FechaFin);
            det.NominaId = nomina.Id;
            ctx.DetalleNominas.Add(det);
            ctx.SaveChanges();
            NominaHelper.RecalcularTotales(ctx, nomina);
            ctx.SaveChanges();
            return (ctx, banco, emp, nomina, det);
        }

        /// <summary>Hace lo mismo que la pantalla de pago: sale el neto y entra la parte contable.</summary>
        private static void Pagar(ApplicationDbContext ctx, CuentaFinanciera banco, Nomina nomina, DetalleNomina det)
        {
            var cat = MovimientoManualHelper.ObtenerOCrearCategoria(ctx, "Egreso", "Nómina", "6.2.01");
            var mov = new MovimientoFinanciero
            {
                Numero = "M-N", Tipo = "Egreso", Fecha = DateTime.Now, CuentaId = banco.Id, CategoriaId = cat.Id, Monto = det.SalarioNeto,
                MonedaId = 1, TipoCambio = 1, FormaPago = "Transferencia", Concepto = "Nómina", Origen = "Nomina", OrigenId = nomina.Id, Estado = "Activo"
            };
            ctx.MovimientosFinancieros.Add(mov); ctx.SaveChanges();
            SaldoHelper.Aplicar(ctx, mov); ctx.SaveChanges();
            var pago = new PagoNominaEmpleado { Numero = "P-1", NominaId = nomina.Id, EmpleadoId = det.EmpleadoId, DetalleNominaId = det.Id, Monto = det.SalarioNeto, CuentaId = banco.Id, MovimientoId = mov.Id, FechaPago = DateTime.Now, Estado = "Pagado" };
            ctx.PagosNominaEmpleado.Add(pago); ctx.SaveChanges();
            Assert.Null(RetencionesHelper.RegistrarDeNomina(ctx, pago, det, nomina.Numero, "Ana Paz", null));
        }

        private static decimal Linea(List<ContabilidadHelper.LineaReporte> l, string codigo) => l.Where(x => x.Codigo == codigo).Sum(x => x.Monto);

        [Fact]
        public void GastoEsBrutoMasAportes_YLaDeudaQuedaPorEntidad_SinTocarElBancoDeMas()
        {
            var (ctx, banco, _, nomina, det) = Base();
            using var _c = ctx;
            Assert.True(det.DeduccionISR > 0 && det.DeduccionRAP > 0 && det.AporteIHSSPatronal > 0 && det.AporteRAPPatronal > 0);

            Pagar(ctx, banco, nomina, det);

            // al banco solo le sale el neto
            Assert.Equal(100000m - det.SalarioNeto, ctx.CuentasFinancieras.Single(c => c.Id == banco.Id).SaldoActual);

            var er = ContabilidadHelper.GenerarEstadoResultados(ctx, DateTime.Today, DateTime.Today);
            Assert.Equal(det.TotalBruto, Linea(er.GastosAdministracion, "6.2.01"));
            Assert.Equal(det.AporteIHSSPatronal + det.AporteRAPPatronal, Linea(er.GastosAdministracion, "6.2.02"));

            var r = RetencionesHelper.Resumen(ctx).ToDictionary(f => f.Concepto.Clave);
            Assert.Equal(det.DeduccionISR, r["ISR"].Pendiente);
            Assert.Equal(det.DeduccionIHSS + det.AporteIHSSPatronal, r["IHSS"].Pendiente);
            Assert.Equal(det.DeduccionRAP + det.AporteRAPPatronal, r["RAP"].Pendiente);

            var b = ContabilidadHelper.GenerarBalanceGeneral(ctx, DateTime.Today);
            Assert.Equal(r["ISR"].Pendiente, Linea(b.Pasivos, "2.1.02.002"));
            Assert.Equal(r["IHSS"].Pendiente, Linea(b.Pasivos, "2.1.02.003"));
            Assert.Equal(r["RAP"].Pendiente, Linea(b.Pasivos, "2.1.02.004"));
            Assert.Equal(0m, b.Diferencia);
        }

        [Fact]
        public void PagarALaEntidad_BajaElBancoYLaDeuda_NoLaUtilidad_YAnularloLaRegresa()
        {
            var (ctx, banco, _, nomina, det) = Base();
            using var _c = ctx;
            Pagar(ctx, banco, nomina, det);
            var utilidadAntes = ContabilidadHelper.GenerarEstadoResultados(ctx, DateTime.Today, DateTime.Today).UtilidadNeta;
            var saldoAntes = ctx.CuentasFinancieras.Single(c => c.Id == banco.Id).SaldoActual;

            var (mov, error) = RetencionesHelper.Pagar(ctx, "ISR", det.DeduccionISR, banco.Id, DateTime.Today, "REC-1", null);

            Assert.Null(error);
            Assert.Equal(saldoAntes - det.DeduccionISR, ctx.CuentasFinancieras.Single(c => c.Id == banco.Id).SaldoActual);
            Assert.Equal(0m, RetencionesHelper.Resumen(ctx).Single(f => f.Concepto.Clave == "ISR").Pendiente);
            Assert.Equal(utilidadAntes, ContabilidadHelper.GenerarEstadoResultados(ctx, DateTime.Today, DateTime.Today).UtilidadNeta);
            var b = ContabilidadHelper.GenerarBalanceGeneral(ctx, DateTime.Today);
            Assert.Equal(0m, Linea(b.Pasivos, "2.1.02.002"));
            Assert.Equal(0m, b.Diferencia);

            mov!.Estado = "Anulado"; ctx.SaveChanges();
            SaldoHelper.Revertir(ctx, mov); ctx.SaveChanges();
            Assert.Equal(det.DeduccionISR, RetencionesHelper.Resumen(ctx).Single(f => f.Concepto.Clave == "ISR").Pendiente);
            Assert.Equal(0m, ContabilidadHelper.GenerarBalanceGeneral(ctx, DateTime.Today).Diferencia);
        }

        [Fact]
        public void NoSePagaMasDeLoQueSeDebe_NiSinRecibo()
        {
            var (ctx, banco, _, nomina, det) = Base();
            using var _c = ctx;
            Pagar(ctx, banco, nomina, det);

            Assert.NotNull(RetencionesHelper.Pagar(ctx, "ISR", det.DeduccionISR + 1, banco.Id, DateTime.Today, "R", null).error);
            Assert.NotNull(RetencionesHelper.Pagar(ctx, "ISR", 1, banco.Id, DateTime.Today, " ", null).error);
            Assert.NotNull(RetencionesHelper.Pagar(ctx, "ISR", 1, banco.Id, DateTime.Today.AddDays(1), "R", null).error);
        }

        [Fact]
        public void Vale_EsAnticipo_YAlDescontarseEnNominaPasaAGasto()
        {
            var (ctx, banco, emp, nomina, _) = Base();
            using var _c = ctx;
            FinanzasSeeder.Seed(ctx);
            var cat = ctx.CategoriasFinancieras.Single(c => c.Nombre == "Adelantos/Vales a empleados");

            // se entrega el vale: sale dinero, pero no es gasto
            var movVale = new MovimientoFinanciero
            {
                Numero = "M-V", Tipo = "Egreso", Fecha = DateTime.Now, CuentaId = banco.Id, CategoriaId = cat.Id, Monto = 3000m,
                MonedaId = 1, TipoCambio = 1, FormaPago = "Transferencia", Concepto = "Vale", Origen = "Vale", OrigenId = 1, Estado = "Activo"
            };
            ctx.MovimientosFinancieros.Add(movVale); ctx.SaveChanges();
            SaldoHelper.Aplicar(ctx, movVale); ctx.SaveChanges();
            ctx.ValesEmpleado.Add(new ValeEmpleado { Numero = "V-1", EmpleadoId = emp.Id, Monto = 3000m, SaldoPendiente = 3000m, MontoCuota = 1000m, Cuotas = 3, Estado = "Entregado", FechaEntrega = DateTime.Now });
            ctx.SaveChanges();

            Assert.Equal(0m, ContabilidadHelper.GenerarEstadoResultados(ctx, DateTime.Today, DateTime.Today).TotalGastos);
            var b1 = ContabilidadHelper.GenerarBalanceGeneral(ctx, DateTime.Today);
            Assert.Equal(3000m, Linea(b1.Activos, RetencionesHelper.CodigoAnticipos));
            Assert.Equal(0m, b1.Diferencia);

            // la nómina descuenta una cuota
            var det = ctx.DetalleNominas.Single(d => d.NominaId == nomina.Id);
            Assert.Null(NominaHelper.AplicarExtras(ctx, nomina, det, 0.01m, "ajuste"));
            Assert.Equal(1000m, det.DeduccionVales);
            Pagar(ctx, banco, nomina, det);

            var er = ContabilidadHelper.GenerarEstadoResultados(ctx, DateTime.Today, DateTime.Today);
            Assert.Equal(det.TotalBruto, Linea(er.GastosAdministracion, "6.2.01"));
            var b2 = ContabilidadHelper.GenerarBalanceGeneral(ctx, DateTime.Today);
            Assert.Equal(2000m, Linea(b2.Activos, RetencionesHelper.CodigoAnticipos));
            Assert.Equal(0m, b2.Diferencia);
        }

        [Fact]
        public void Extras_SubenBrutoEIsr_NoElIhss_YActualizanLaNomina()
        {
            var (ctx, _, _, nomina, det) = Base();
            using var _c = ctx;
            var (brutoAntes, isrAntes, ihssAntes, netoAntes) = (det.TotalBruto, det.DeduccionISR, det.DeduccionIHSS, det.SalarioNeto);

            Assert.Null(NominaHelper.AplicarExtras(ctx, nomina, det, 5000m, "20 horas extra"));

            Assert.Equal(brutoAntes + 5000m, det.TotalBruto);
            Assert.Equal(5000m, det.MontoHorasExtra);
            Assert.True(det.DeduccionISR > isrAntes);
            Assert.Equal(ihssAntes, det.DeduccionIHSS);
            Assert.Equal(det.TotalBruto - det.TotalDeducciones, det.SalarioNeto);
            Assert.True(det.SalarioNeto > netoAntes);
            Assert.Equal(det.TotalBruto, nomina.TotalSalariosBrutos);
            Assert.Equal(det.SalarioNeto, nomina.TotalNeto);
            Assert.Equal(5000m, nomina.TotalHorasExtra);
        }

        [Fact]
        public void Extras_ExigenMotivo_YNoSeTocaAQuienYaCobro()
        {
            var (ctx, banco, _, nomina, det) = Base();
            using var _c = ctx;
            Assert.NotNull(NominaHelper.AplicarExtras(ctx, nomina, det, 100m, " "));
            Assert.NotNull(NominaHelper.AplicarExtras(ctx, nomina, det, -1m, "x"));
            Pagar(ctx, banco, nomina, det);
            Assert.NotNull(NominaHelper.AplicarExtras(ctx, nomina, det, 100m, "tarde"));
        }

        [Fact]
        public void ExtrasEnNominaQuincenal_NoSeMultiplicanAlMes()
        {
            var (ctx, _, emp, nomina, _) = Base(30000m);
            using var _c = ctx;
            var cfg = NominaHelper.ObtenerConfiguracion(ctx, DateTime.Today.Year);
            var sin = NominaHelper.CalcularDetalleEmpleado(ctx, emp, "Quincenal", 15, nomina.FechaInicio, nomina.FechaFin);
            var con = NominaHelper.CalcularDetalleEmpleado(ctx, emp, "Quincenal", 15, nomina.FechaInicio, nomina.FechaFin, 1000m);
            // tramo del 15%: 1,000 extra pagan 150, no 300
            Assert.Equal(sin.DeduccionISR + 150m, con.DeduccionISR);
        }

        [Fact]
        public void PagoViejoSinRetenciones_SeCompletaUnaSolaVez()
        {
            var (ctx, banco, _, nomina, det) = Base();
            using var _c = ctx;
            ctx.PagosNominaEmpleado.Add(new PagoNominaEmpleado { Numero = "P-V", NominaId = nomina.Id, EmpleadoId = det.EmpleadoId, DetalleNominaId = det.Id, Monto = det.SalarioNeto, CuentaId = banco.Id, FechaPago = DateTime.Now, Estado = "Pagado" });
            ctx.SaveChanges();

            RetencionesHelper.Reparar(ctx);
            var n = ctx.MovimientosFinancieros.Count(m => m.Origen == RetencionesHelper.OrigenNomina);
            RetencionesHelper.Reparar(ctx);

            Assert.True(n > 0);
            Assert.Equal(n, ctx.MovimientosFinancieros.Count(m => m.Origen == RetencionesHelper.OrigenNomina));
        }

        // ===== Envío en facturas =====
        [Fact]
        public void Factura_MuestraElIsvDelEnvioAparte()
        {
            using var ctx = TestDb.Crear();
            ctx.Impuestos.Add(new Impuesto { Nombre = "ISV", Porcentaje = 15, Activo = true, EsPredeterminado = true });
            ctx.SaveChanges();
            var f = new Factura { Subtotal = 2525m, Impuestos = 378.75m, Total = 2903.75m, MontoEnvio = 1600m };
            var lineas = new List<DetalleFactura> { new() { Cantidad = 2, PrecioUnitario = 462.50m, Subtotal = 925m, ImpuestoPorcentaje = 15, Total = 1063.75m } };

            var d = ISVHelper.DesglosarFactura(ctx, f, lineas);

            Assert.Equal(925m, d.SubtotalProductos);
            Assert.Equal(240m, d.EnvioISV);
            Assert.Equal(378.75m, d.ISVTotal);
            Assert.Equal(2903.75m, d.Total);
        }

        [Fact]
        public void FleteDeEnvioPagado_NoBajaLaUtilidad_YBajaLoCobradoPorAplicar()
        {
            using var ctx = TestDb.Crear();
            PlanCuentasHelper.Seed(ctx);
            FinanzasSeeder.Seed(ctx);
            var caja = new CuentaFinanciera { Codigo = "C", Nombre = "Caja", Tipo = "Caja", SaldoActual = 2000m, SaldoInicial = 2000m, Activa = true };
            ctx.CuentasFinancieras.Add(caja);
            ctx.SaveChanges();
            var cat = ctx.CategoriasFinancieras.Single(c => c.Nombre == "Fletes de envíos a clientes");

            var (mov, err) = MovimientoManualHelper.Crear(ctx, "Egreso", DateTime.Now, caja.Id, null, cat.Id, 300m, "Flete", null, "Manual", null, null);

            Assert.Null(err);
            Assert.Equal(0m, ContabilidadHelper.GenerarEstadoResultados(ctx, DateTime.Today, DateTime.Today).TotalGastos);
            var b = ContabilidadHelper.GenerarBalanceGeneral(ctx, DateTime.Today);
            Assert.Equal(-300m, Linea(b.Pasivos, "2.1.99"));
            Assert.Equal(0m, b.Diferencia);
        }
    }
}
