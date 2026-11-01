using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    public class ConfiguracionInicialTests
    {
        [Fact]
        public void ParsearLista_QuitaVaciosYRepetidos()
        {
            var r = ConfiguracionInicialHelper.ParsearLista("Bebidas\r\n  abarrotes , BEBIDAS;;Limpieza\n\n");
            Assert.Equal(new[] { "Bebidas", "abarrotes", "Limpieza" }, r.ToArray());
            Assert.Empty(ConfiguracionInicialHelper.ParsearLista("  "));
            Assert.Empty(ConfiguracionInicialHelper.ParsearLista(null));
        }

        [Fact]
        public void ParsearLista_LimitaLargoYCantidad()
        {
            var largo = new string('a', 300);
            Assert.Equal(100, ConfiguracionInicialHelper.ParsearLista(largo)[0].Length);
            var muchos = string.Join("\n", Enumerable.Range(1, 80).Select(i => "Cat" + i));
            Assert.Equal(50, ConfiguracionInicialHelper.ParsearLista(muchos).Count);
        }

        [Theory]
        [InlineData("Mi Empresa", "0801-1990-123456", false)]   // nombre por defecto
        [InlineData("Ferretería López", null, false)]            // sin RTN
        [InlineData("Ferretería López", "0801-1990-123456", true)]
        [InlineData("   ", "0801-1990-123456", false)]
        public void EmpresaConfigurada_PideNombrePropioYRtn(string nombre, string? rtn, bool esperado)
        {
            Assert.Equal(esperado, ConfiguracionInicialHelper.EmpresaConfigurada(new ConfiguracionEmpresa { Nombre = nombre, RTN = rtn }));
        }

        [Fact]
        public void EmpresaConfigurada_NullEsFalso()
            => Assert.False(ConfiguracionInicialHelper.EmpresaConfigurada(null));
    }
}
