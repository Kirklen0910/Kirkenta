using Kirkenta.Helpers;
using Xunit;
using C = Kirkenta.Helpers.RedLocalHelper.Candidata;

namespace Kirkenta.Tests
{
    /// <summary>La dirección que se muestra para entrar desde el celular.</summary>
    public class RedLocalTests
    {
        // como la PC de prueba: Wi-Fi real + adaptadores de VMware y VirtualBox
        private static readonly C[] Equipo =
        {
            new("192.168.223.1", "VMware Network Adapter VMnet8", false, false),
            new("192.168.56.1", "Ethernet VirtualBox Host-Only Ethernet Adapter", false, false),
            new("192.168.1.210", "Wi-Fi Intel Wireless", true, true),
            new("169.254.10.4", "Ethernet 2", false, false),
        };

        [Fact]
        public void EligeElWifiReal_SinVirtuales()
        {
            var e = RedLocalHelper.Calcular(new[] { "http://0.0.0.0:5114" }, Equipo);
            Assert.False(e.SoloEstaComputadora);
            Assert.Equal("http://192.168.1.210:5114", Assert.Single(e.Direcciones).Url);
        }

        [Fact]
        public void ConHttps_UsaElPuertoSeguro_YNoElHttpLocal()
        {
            var e = RedLocalHelper.Calcular(new[] { "http://127.0.0.1:5114", "https://0.0.0.0:5115" }, Equipo);
            Assert.True(e.EsHttps);
            Assert.Equal("https://192.168.1.210:5115", Assert.Single(e.Direcciones).Url);
        }

        [Theory]
        [InlineData("http://localhost:5114")]
        [InlineData("http://127.0.0.1:5114")]
        public void SoloLocalhost_AvisaQueElCelularNoPuedeEntrar(string escucha)
        {
            var e = RedLocalHelper.Calcular(new[] { escucha }, Equipo);
            Assert.True(e.SoloEstaComputadora);
            Assert.Empty(e.Direcciones);
        }

        [Theory]
        [InlineData("http://*:5114")]
        [InlineData("http://+:5114")]
        [InlineData("http://[::]:5114")]
        public void Comodines_CuentanComoRed(string escucha)
        {
            var e = RedLocalHelper.Calcular(new[] { escucha }, Equipo);
            Assert.Equal("http://192.168.1.210:5114", Assert.Single(e.Direcciones).Url);
        }

        [Fact]
        public void SinPuertaDeEnlace_IgualMuestraLaRedPrivada_YDescartaPublicas()
        {
            var r = RedLocalHelper.Elegir(new[] { new C("10.0.0.5", "Ethernet", false, false), new C("8.8.8.8", "Ethernet", true, false) });
            Assert.Equal("10.0.0.5", Assert.Single(r).Ip);
        }
    }
}
