using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    public class DepreciacionTests
    {
        private static ActivoFijo Activo(decimal costo, decimal residual, int meses, decimal acumulada = 0) => new()
        {
            CostoAdquisicion = costo,
            ValorResidual = residual,
            VidaUtilMeses = meses,
            DepreciacionAcumulada = acumulada
        };

        [Fact]
        public void Cuota_mensual_es_costo_menos_residual_entre_meses()
        {
            var a = Activo(12000m, 0m, 60);
            Assert.Equal(200m, a.DepreciacionMensual);
            Assert.Equal(200m, DepreciacionHelper.CalcularMonto(a));
        }

        [Fact]
        public void Ultima_cuota_no_pasa_del_valor_residual()
        {
            var a = Activo(1000m, 100m, 3, acumulada: 800m); // cuota 300, solo faltan 100
            Assert.Equal(100m, DepreciacionHelper.CalcularMonto(a));
        }

        [Fact]
        public void Activo_totalmente_depreciado_da_cero()
        {
            var a = Activo(1000m, 100m, 3, acumulada: 900m);
            Assert.Equal(0m, DepreciacionHelper.CalcularMonto(a));
        }

        [Fact]
        public void Vida_util_cero_no_divide_entre_cero()
        {
            var a = Activo(1000m, 0m, 0);
            Assert.Equal(0m, DepreciacionHelper.CalcularMonto(a));
        }

        [Fact]
        public void Valor_en_libros_resta_la_acumulada()
        {
            var a = Activo(1000m, 0m, 10, acumulada: 250m);
            Assert.Equal(750m, a.ValorEnLibros);
        }
    }
}
