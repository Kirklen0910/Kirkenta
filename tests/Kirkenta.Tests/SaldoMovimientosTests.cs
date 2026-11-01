using Kirkenta.Data;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class SaldoMovimientosTests
    {
        private static (ApplicationDbContext ctx, CuentaFinanciera cuenta) ConCuenta(decimal saldo)
        {
            var ctx = TestDb.Crear();
            var cuenta = new CuentaFinanciera { Codigo = "C1", Nombre = "Caja", Tipo = "Caja", SaldoInicial = saldo, SaldoActual = saldo, Activa = true };
            ctx.CuentasFinancieras.Add(cuenta);
            ctx.SaveChanges();
            return (ctx, cuenta);
        }

        private static MovimientoFinanciero Mov(int cuentaId, string tipo, decimal monto) => new()
        {
            Numero = "MOV-" + Guid.NewGuid().ToString("N").Substring(0, 6),
            Tipo = tipo,
            CuentaId = cuentaId,
            Monto = monto,
            Estado = "Activo",
            Origen = "Manual",
            Concepto = "Prueba"
        };

        [Fact]
        public void Aplicar_ingreso_suma_y_egreso_resta()
        {
            var (ctx, cuenta) = ConCuenta(100m);
            using var _ = ctx;

            SaldoHelper.Aplicar(ctx, Mov(cuenta.Id, "Ingreso", 50m));
            Assert.Equal(150m, cuenta.SaldoActual);

            SaldoHelper.Aplicar(ctx, Mov(cuenta.Id, "Egreso", 30m));
            Assert.Equal(120m, cuenta.SaldoActual);
        }

        [Fact]
        public void Revertir_no_hace_nada_si_el_movimiento_sigue_activo()
        {
            var (ctx, cuenta) = ConCuenta(100m);
            using var _ = ctx;

            var mov = Mov(cuenta.Id, "Ingreso", 50m);
            SaldoHelper.Aplicar(ctx, mov);
            SaldoHelper.Revertir(ctx, mov); // sigue Activo: no revierte

            Assert.Equal(150m, cuenta.SaldoActual);
        }

        [Fact]
        public void Anular_primero_y_revertir_despues_restaura_el_saldo()
        {
            var (ctx, cuenta) = ConCuenta(100m);
            using var _ = ctx;

            var mov = Mov(cuenta.Id, "Egreso", 40m);
            SaldoHelper.Aplicar(ctx, mov);
            Assert.Equal(60m, cuenta.SaldoActual);

            mov.Estado = "Anulado";
            SaldoHelper.Revertir(ctx, mov);
            Assert.Equal(100m, cuenta.SaldoActual);
        }

        [Fact]
        public void Transferencia_mueve_entre_cuentas_y_se_revierte()
        {
            var (ctx, origen) = ConCuenta(200m);
            using var _ = ctx;
            var destino = new CuentaFinanciera { Codigo = "B1", Nombre = "Banco", Tipo = "Banco", SaldoInicial = 0, SaldoActual = 0, Activa = true };
            ctx.CuentasFinancieras.Add(destino);
            ctx.SaveChanges();

            var mov = Mov(origen.Id, "Transferencia", 80m);
            mov.CuentaDestinoId = destino.Id;

            SaldoHelper.Aplicar(ctx, mov);
            Assert.Equal(120m, origen.SaldoActual);
            Assert.Equal(80m, destino.SaldoActual);

            mov.Estado = "Anulado";
            SaldoHelper.Revertir(ctx, mov);
            Assert.Equal(200m, origen.SaldoActual);
            Assert.Equal(0m, destino.SaldoActual);
        }

        [Fact]
        public void Movimiento_de_apertura_no_cambia_el_saldo()
        {
            var (ctx, cuenta) = ConCuenta(100m);
            using var _ = ctx;

            var mov = Mov(cuenta.Id, "Ingreso", 100m);
            mov.Origen = "Apertura";
            SaldoHelper.Aplicar(ctx, mov);

            Assert.Equal(100m, cuenta.SaldoActual);
        }
    }
}
