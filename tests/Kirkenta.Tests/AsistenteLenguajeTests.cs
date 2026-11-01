using Kirkenta.Services.Asistente;
using Xunit;

namespace Kirkenta.Tests
{
    public class AsistenteLenguajeTests
    {
        private static readonly DateTime Hoy = new(2026, 10, 25);

        private static string Accion(string frase)
            => AsistenteMotor.Interpretar(frase, AsistenteCatalogo.Todas, Hoy).Accion?.Id ?? "(ninguna)";

        [Theory]
        [InlineData("plata", "saldos_cuentas")]
        [InlineData("¿Cuánta plata hay?", "saldos_cuentas")]
        [InlineData("pendientes", "pendientes")]
        [InlineData("al día", "resumen_dia")]
        [InlineData("cómo andamos hoy", "resumen_dia")]
        [InlineData("qué se está acabando", "stock_bajo")]
        [InlineData("quién me debe", "recordatorios_cobro")]
        [InlineData("a quién le debo", "cxp")]
        [InlineData("cuánto llevamos hoy", "ventas_resumen")]
        public void Entiende_lenguaje_coloquial(string frase, string esperada)
            => Assert.Equal(esperada, Accion(frase));

        [Fact]
        public void Traducir_convierte_slang_y_no_toca_frases_normales()
        {
            Assert.Equal("cuanto dinero tenemos", AsistenteLenguaje.Traducir("plata"));
            Assert.Equal("ventas de hoy", AsistenteLenguaje.Traducir("ventas de hoy"));
            Assert.Equal("saldo de alfredo", AsistenteLenguaje.Traducir("saldo de alfredo"));
        }

        [Fact]
        public void Charla_responde_solo_a_charla()
        {
            Assert.NotNull(AsistenteLenguaje.Charla("como estas", "Carlos"));
            Assert.NotNull(AsistenteLenguaje.Charla("quien eres", "Carlos"));
            Assert.Null(AsistenteLenguaje.Charla("ventas de hoy", "Carlos"));
        }

        [Fact]
        public void Sugerir_devuelve_acciones_cercanas_sin_pasarse_del_maximo()
        {
            var s = AsistenteLenguaje.Sugerir("factura vencida", AsistenteCatalogo.Todas.Where(a => a.Tipo != TipoAccion.Navegar), 3);
            Assert.InRange(s.Count, 1, 3);
        }

        [Fact]
        public void Pide_modulos()
        {
            Assert.True(AsistenteLenguaje.PideModulos("modulos"));
            Assert.False(AsistenteLenguaje.PideModulos("ventas"));
        }
    }
}
