using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class NotaCreditoFiscalTests
    {
        private static ConfiguracionEmpresa Cfg(bool activa, string ini = "000-001-03-00000001", string fin = "000-001-03-00000002", int diasLimite = 30)
            => new()
            {
                Nombre = "Test",
                FacturacionFiscalActiva = activa,
                NotaCreditoCAI = "AAAAAA-BBBBBB-CCCCCC-DDDDDD",
                NotaCreditoRangoInicial = ini,
                NotaCreditoRangoFinal = fin,
                NotaCreditoFechaLimite = DateTime.Today.AddDays(diasLimite)
            };

        [Fact]
        public void FiscalInactiva_UsaNumeracionInterna()
        {
            using var ctx = TestDb.Crear();
            ctx.ConfiguracionEmpresa.Add(Cfg(activa: false));
            ctx.SaveChanges();

            var r = NotaCreditoFiscalHelper.Reservar(ctx);
            Assert.False(r.Activa);
        }

        [Fact]
        public void SinRangoPropio_UsaNumeracionInterna()
        {
            using var ctx = TestDb.Crear();
            ctx.ConfiguracionEmpresa.Add(new ConfiguracionEmpresa { Nombre = "Test", FacturacionFiscalActiva = true });
            ctx.SaveChanges();

            Assert.False(NotaCreditoFiscalHelper.Reservar(ctx).Activa);
        }

        [Fact]
        public void Reservar_AsignaNumerosConsecutivos_YSeAgotaElRango()
        {
            using var ctx = TestDb.Crear();
            ctx.ConfiguracionEmpresa.Add(Cfg(activa: true));
            ctx.SaveChanges();

            var r1 = NotaCreditoFiscalHelper.Reservar(ctx);
            var r2 = NotaCreditoFiscalHelper.Reservar(ctx);
            var r3 = NotaCreditoFiscalHelper.Reservar(ctx);

            Assert.True(r1.Ok);
            Assert.Equal("000-001-03-00000001", r1.Numero);
            Assert.Equal("000-001-03-00000002", r2.Numero);
            Assert.False(r3.Ok);
            Assert.Contains("agot", r3.Error!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CaiVencido_BloqueaLaEmision()
        {
            using var ctx = TestDb.Crear();
            ctx.ConfiguracionEmpresa.Add(Cfg(activa: true, diasLimite: -1));
            ctx.SaveChanges();

            var r = NotaCreditoFiscalHelper.Reservar(ctx);
            Assert.True(r.Activa);
            Assert.False(r.Ok);
            Assert.Contains("venci", r.Error!, StringComparison.OrdinalIgnoreCase);
        }
    }
}
