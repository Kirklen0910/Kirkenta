using Kirkenta.Helpers;
using Xunit;

namespace Kirkenta.Tests
{
    public class WhatsAppHelperTests
    {
        [Theory]
        [InlineData("+504 9999-9999", "50499999999")]
        [InlineData("+504 99999999", "50499999999")]
        [InlineData("9999-9999", "50499999999")]
        [InlineData("504 9999 9999", "50499999999")]
        [InlineData("+1 305 555 1234", "13055551234")]
        public void Normalizar_acepta_formatos_validos(string entrada, string esperado)
            => Assert.Equal(esperado, WhatsAppHelper.Normalizar(entrada));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abc")]
        [InlineData("+504 999")]
        [InlineData("123")]
        public void Normalizar_rechaza_invalidos(string? entrada)
            => Assert.Null(WhatsAppHelper.Normalizar(entrada));

        [Fact]
        public void Enlace_codifica_el_mensaje()
        {
            var url = WhatsAppHelper.Enlace("+504 9999-9999", "Hola & gracias ñ");
            Assert.StartsWith("https://wa.me/50499999999?text=", url);
            Assert.DoesNotContain(" ", url);
            Assert.DoesNotContain("&", url!.Substring(url.IndexOf("text=")));
        }

        [Fact]
        public void Enlace_sin_telefono_devuelve_null()
            => Assert.Null(WhatsAppHelper.Enlace(null, "hola"));

        [Fact]
        public void Enlace_sin_mensaje_solo_abre_el_chat()
            => Assert.Equal("https://wa.me/50499999999", WhatsAppHelper.Enlace("+504 9999-9999", null));

        [Fact]
        public void Mensajes_incluyen_los_datos_clave()
        {
            var f = WhatsAppHelper.MensajeFactura("Mi Empresa", "Ana", "F-001", 1150m, 500m, new DateTime(2026, 11, 30));
            Assert.Contains("Ana", f); Assert.Contains("*Mi Empresa*", f); Assert.Contains("\n", f); Assert.Contains("F-001", f); Assert.Contains("1,150.00", f);
            Assert.Contains("500.00", f); Assert.Contains("30/11/2026", f);

            var pagada = WhatsAppHelper.MensajeFactura("Mi Empresa", "Ana", "F-001", 1150m, 0m, null);
            Assert.DoesNotContain("Saldo pendiente", pagada);

            var c = WhatsAppHelper.MensajeCobranza("Mi Empresa", "Ana", 800m, 300m);
            Assert.Contains("800.00", c); Assert.Contains("300.00", c); Assert.Contains("Saldo vencido", c);
            Assert.DoesNotContain("Saldo vencido", WhatsAppHelper.MensajeCobranza("Mi Empresa", "Ana", 800m, 0m));
        }
    }
}
