using Kirkenta.Data;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>Apertura → ventas → cierre con depósito al banco → nueva apertura: todo tiene que cuadrar.</summary>
    [Collection("BD")]
    public class CajaCicloTests
    {
        private static CuentaFinanciera Cuenta(ApplicationDbContext ctx, string codigo, string tipo, decimal saldo)
        {
            var c = new CuentaFinanciera { Codigo = codigo, Nombre = codigo, Tipo = tipo, Subtipo = tipo == "Caja" ? "General" : null, SaldoActual = saldo, Activa = true };
            ctx.CuentasFinancieras.Add(c);
            ctx.SaveChanges();
            return c;
        }

        private static void VentaEfectivo(ApplicationDbContext ctx, int cajaId, decimal monto, DateTime? fecha = null)
        {
            var m = new MovimientoFinanciero
            {
                Numero = "V-" + Guid.NewGuid().ToString("N")[..6], Tipo = "Ingreso", Fecha = fecha ?? DateTime.Now, CuentaId = cajaId,
                Monto = monto, MonedaId = 1, TipoCambio = 1, FormaPago = "Efectivo", Concepto = "Venta", Origen = "Manual", Estado = "Activo"
            };
            ctx.MovimientosFinancieros.Add(m);
            ctx.SaveChanges();
            SaldoHelper.Aplicar(ctx, m);
            ctx.SaveChanges();
        }

        private static decimal Saldo(ApplicationDbContext ctx, int id) => ctx.CuentasFinancieras.Single(c => c.Id == id).SaldoActual;

        [Fact]
        public void CierreConDeposito_LaCajaQuedaConElFondoYElBancoConElDeposito()
        {
            using var ctx = TestDb.Crear();
            var usuario = TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            var caja = Cuenta(ctx, "CAJA", "Caja", 0m);
            var banco = Cuenta(ctx, "BANCO", "Banco", 5000m);

            // 1) Abre con L. 2,000 sacados del banco: es una transferencia, no una ganancia
            var (ap, err) = AperturaHelper.Abrir(ctx, caja.Id, 2000m, null, usuario.Id, banco.Id);
            Assert.Null(err);
            Assert.Equal(2000m, Saldo(ctx, caja.Id));
            Assert.Equal(3000m, Saldo(ctx, banco.Id));
            Assert.DoesNotContain(ctx.MovimientosFinancieros, m => m.Tipo == "Ingreso" && m.Concepto!.Contains("Sobrante"));

            // 2) Vende L. 18,000 en efectivo: el cierre espera L. 20,000
            VentaEfectivo(ctx, caja.Id, 18000m);
            var calc = CierreHelper.Calcular(ctx, caja.Id, DateTime.Today);
            Assert.Null(calc.Error);
            Assert.Equal(20000m, calc.EfectivoEsperado);
            Assert.Equal(Saldo(ctx, caja.Id), calc.EfectivoEsperado);       // el saldo de la caja y el arqueo son el mismo número

            // 3) Cierra: deposita L. 18,000 (boleta 123) y deja L. 2,000 de fondo
            var (cierre, errC) = CierreHelper.Registrar(ctx, caja.Id, DateTime.Today, 20000m, null, 20m, usuario.Id);
            Assert.Null(errC);
            var (ok, errD) = DistribucionHelper.RegistrarDistribuciones(ctx, cierre.Id, caja.Id, 20000m, new List<DistribucionInput>
            {
                new() { Tipo = "RetiroBanco", Monto = 18000m, CuentaDestinoId = banco.Id, Referencia = "Boleta 123" },
                new() { Tipo = "FondoCaja", Monto = 2000m }
            }, usuario.Id);
            Assert.True(ok, errD);
            Assert.Equal(2000m, Saldo(ctx, caja.Id));
            Assert.Equal(21000m, Saldo(ctx, banco.Id));
            var deposito = ctx.MovimientosFinancieros.Single(m => m.Tipo == "Transferencia" && m.CuentaDestinoId == banco.Id);
            Assert.Equal("Boleta 123", deposito.Referencia);

            // 4) Al día siguiente abre con el fondo que quedó: no se registra ninguna diferencia
            Thread.Sleep(1100);
            var antes = ctx.MovimientosFinancieros.Count();
            var (ap2, err2) = AperturaHelper.Abrir(ctx, caja.Id, 2000m, null, usuario.Id);
            Assert.Null(err2);
            Assert.Equal(antes, ctx.MovimientosFinancieros.Count());
            Assert.Equal(2000m, CierreHelper.Calcular(ctx, caja.Id, DateTime.Today).EfectivoEsperado);
        }

        [Fact]
        public void AbrirConMenosDeLoQueDiceElSistema_QuedaComoFaltanteYCuadra()
        {
            using var ctx = TestDb.Crear();
            var usuario = TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            var caja = Cuenta(ctx, "CAJA", "Caja", 0m);
            VentaEfectivo(ctx, caja.Id, 500m, DateTime.Now.AddMinutes(-5));       // el sistema dice que hay 500

            var (ap, err) = AperturaHelper.Abrir(ctx, caja.Id, 450m, null, usuario.Id);
            Assert.Null(err);
            Assert.Equal(450m, Saldo(ctx, caja.Id));
            var faltante = ctx.MovimientosFinancieros.Single(m => m.Origen == "AperturaCaja");
            Assert.Equal("Egreso", faltante.Tipo);
            Assert.Equal(50m, faltante.Monto);
            // la diferencia es de antes de abrir: no entra al arqueo del turno
            Assert.Equal(450m, CierreHelper.Calcular(ctx, caja.Id, DateTime.Today).EfectivoEsperado);
        }

        [Fact]
        public void CierreALasSeis_ElDepositoSeRegistraAlDiaSiguienteConSuBoleta()
        {
            using var ctx = TestDb.Crear();
            var usuario = TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            var caja = Cuenta(ctx, "CAJA", "Caja", 0m);
            var banco = Cuenta(ctx, "BANCO", "Banco", 0m);
            var (ap, _) = AperturaHelper.Abrir(ctx, caja.Id, 0m, null, usuario.Id);
            VentaEfectivo(ctx, caja.Id, 20000m);

            // 6 pm: el banco cerró. 18,000 quedan «por depositar» y 2,000 de fondo
            var (cierre, errC) = CierreHelper.Registrar(ctx, caja.Id, DateTime.Today, 20000m, null, 20m, usuario.Id);
            Assert.Null(errC);
            var (ok, errD) = DistribucionHelper.RegistrarDistribuciones(ctx, cierre.Id, caja.Id, 20000m, new List<DistribucionInput>
            {
                new() { Tipo = EfectivoPorDepositarHelper.Tipo, Monto = 18000m },
                new() { Tipo = "FondoCaja", Monto = 2000m }
            }, usuario.Id);
            Assert.True(ok, errD);
            var transito = EfectivoPorDepositarHelper.Cuenta(ctx);
            Assert.Equal(2000m, Saldo(ctx, caja.Id));          // la próxima apertura solo ve el fondo
            Assert.Equal(18000m, Saldo(ctx, transito.Id));     // el efectivo sigue siendo del negocio
            Assert.Equal(0m, Saldo(ctx, banco.Id));            // pero todavía no está en el banco
            var pend = Assert.Single(EfectivoPorDepositarHelper.Pendientes(ctx));
            Assert.Equal(18000m, pend.Monto);

            // sin boleta no se puede
            Assert.NotNull(EfectivoPorDepositarHelper.RegistrarDeposito(ctx, pend.DistribucionId, banco.Id, DateTime.Today, "", usuario.Id).error);

            // al día siguiente: se deposita con la boleta
            var (mov, err) = EfectivoPorDepositarHelper.RegistrarDeposito(ctx, pend.DistribucionId, banco.Id, DateTime.Today, "Boleta 98765", usuario.Id);
            Assert.Null(err);
            Assert.Equal(0m, Saldo(ctx, transito.Id));
            Assert.Equal(18000m, Saldo(ctx, banco.Id));
            Assert.Equal("Boleta 98765", mov!.Referencia);
            Assert.Empty(EfectivoPorDepositarHelper.Pendientes(ctx));
            Assert.NotNull(EfectivoPorDepositarHelper.RegistrarDeposito(ctx, pend.DistribucionId, banco.Id, DateTime.Today, "otra", usuario.Id).error);  // no se deposita dos veces
            Assert.Equal("Boleta 98765", ctx.DistribucionesCierre.Single(d => d.Id == pend.DistribucionId).Referencia);
        }
    }
}
