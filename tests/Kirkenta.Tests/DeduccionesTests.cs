using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Kirkenta.Pages.RRHH.Deducciones;
using Xunit;

namespace Kirkenta.Tests
{
    public class DeduccionesTests
    {
        private static IndexModel.TramoInput T(decimal? desde, decimal? hasta, decimal? pct, decimal? fijo = 0m)
            => new() { Desde = desde, Hasta = hasta, Porcentaje = pct, MontoFijo = fijo };

        // Ejemplos publicados para 2026: techo IHSS L. 11,903.13, 5% IHSS, RAP 1.5% sobre el excedente
        [Theory]
        [InlineData(9596.64, 479.83, 0.00)]
        [InlineData(12349.49, 595.16, 6.70)]
        [InlineData(19298.72, 595.16, 110.93)]
        [InlineData(20000.00, 595.16, 121.45)]
        public void IHSS_y_RAP_segun_ley_2026(decimal salario, decimal ihssEsperado, decimal rapEsperado)
        {
            var config = new ConfiguracionDeduccion();
            Assert.Equal(ihssEsperado, NominaHelper.CalcularIHSS(salario, config));
            Assert.Equal(rapEsperado, NominaHelper.CalcularRAP(salario, config));
        }

        // Tabla mensual 2026 (SAR 02-2026)
        [Theory]
        [InlineData(20000.00, 0.00)]
        [InlineData(22360.36, 0.00)]
        [InlineData(30000.00, 1145.95)]
        [InlineData(50000.00, 5028.63)]
        [InlineData(100000.00, 16488.39)]
        public void ISR_mensual_tabla_2026(decimal salario, decimal esperado)
        {
            var tramos = new List<TramoISR>
            {
                new() { Orden = 1, Desde = 0, Hasta = 22360.36m, Porcentaje = 0, MontoFijo = 0 },
                new() { Orden = 2, Desde = 22360.37m, Hasta = 32346.18m, Porcentaje = 15, MontoFijo = 0 },
                new() { Orden = 3, Desde = 32346.19m, Hasta = 70805.06m, Porcentaje = 20, MontoFijo = 1497.87m },
                new() { Orden = 4, Desde = 70805.07m, Hasta = null, Porcentaje = 25, MontoFijo = 9189.65m }
            };
            Assert.Equal(esperado, NominaHelper.CalcularISRMensual(salario, tramos));
        }

        [Fact]
        public void Exento_anual_ajusta_el_primer_tramo_y_recalcula_montos_fijos()
        {
            // Tabla vieja (exento 15,692.25) con el exento anual 2026 escrito: manda el exento
            var tramos = new List<IndexModel.TramoInput>
            {
                T(0, 15692.25m, 0), T(15692.26m, 32346.18m, 15, 999), T(32346.19m, 70805.06m, 20, 1), T(70805.07m, null, 25, 1)
            };
            Assert.Null(IndexModel.AjustarTramosISR(tramos, 228324.32m));
            Assert.Equal(22360.36m, tramos[0].Hasta);
            Assert.Equal(22360.37m, tramos[1].Desde);
            Assert.Equal(new decimal?[] { 0m, 0m, 1497.87m, 9189.65m }, tramos.Select(t => t.MontoFijo).ToArray());
        }

        [Fact]
        public void Exento_anual_que_no_cabe_en_la_tabla_da_error()
        {
            var tramos = new List<IndexModel.TramoInput> { T(0, 15000m, 0), T(15000.01m, 20000m, 15), T(20000.01m, null, 25) };
            Assert.NotNull(IndexModel.AjustarTramosISR(tramos, 228324.32m));
        }

        [Fact]
        public void ValidarTramos_TablaCorrecta_SeOrdenaYPasa()
        {
            var r = IndexModel.ValidarTramos(new[] { T(1000.01m, null, 15, 50), T(0, 1000, 0) }, true, out var error);
            Assert.Null(error);
            Assert.NotNull(r);
            Assert.Equal(new decimal?[] { 0m, 1000.01m }, r!.Select(x => x.Desde).ToArray());
        }

        [Fact]
        public void ValidarTramos_FilasVaciasSeIgnoran()
        {
            var r = IndexModel.ValidarTramos(new[] { T(0, null, 10), new IndexModel.TramoInput(), new IndexModel.TramoInput() }, true, out var error);
            Assert.Null(error);
            Assert.Single(r!);
        }

        [Fact]
        public void ValidarTramos_SinTramos_FallaSoloSiSeExige()
        {
            Assert.Null(IndexModel.ValidarTramos(new List<IndexModel.TramoInput>(), true, out var e1));
            Assert.NotNull(e1);
            var r = IndexModel.ValidarTramos(new List<IndexModel.TramoInput>(), false, out var e2);
            Assert.Null(e2);
            Assert.Empty(r!);
        }

        [Fact]
        public void ValidarTramos_Traslape_Falla()
        {
            Assert.Null(IndexModel.ValidarTramos(new[] { T(0, 1000, 0), T(900, null, 15) }, true, out var error));
            Assert.Contains("traslap", error);
        }

        [Fact]
        public void ValidarTramos_PrimeroNoEmpiezaEnCero_Falla()
        {
            Assert.Null(IndexModel.ValidarTramos(new[] { T(100, null, 10) }, true, out var error));
            Assert.NotNull(error);
        }

        [Fact]
        public void ValidarTramos_UltimoConHasta_Falla()
        {
            Assert.Null(IndexModel.ValidarTramos(new[] { T(0, 1000, 10) }, true, out var error));
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData(101)]
        [InlineData(-1)]
        public void ValidarTramos_PorcentajeFueraDeRango_Falla(int pct)
        {
            Assert.Null(IndexModel.ValidarTramos(new[] { T(0, null, pct) }, true, out var error));
            Assert.NotNull(error);
        }

        [Fact]
        public void CalcularISRMensual_SalarioEntreDosTramos_NoQuedaSinISR()
        {
            var tramos = new List<TramoISR>
            {
                new() { Desde = 0m, Hasta = 1000m, Porcentaje = 0m, MontoFijo = 0m },
                new() { Desde = 1000.01m, Hasta = null, Porcentaje = 10m, MontoFijo = 5m },
            };
            // 1000.005 cae en el hueco entre "hasta 1000" y "desde 1000.01": usa el tramo anterior (0%)
            Assert.Equal(0m, NominaHelper.CalcularISRMensual(1000.005m, tramos));
            // 2000 → 5 + (2000 - 1000.01) * 10% = 104.999 → 105.00
            Assert.Equal(105.00m, NominaHelper.CalcularISRMensual(2000m, tramos));
        }
    }
}
