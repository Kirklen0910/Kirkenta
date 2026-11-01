using Kirkenta.Helpers;
using Xunit;

namespace Kirkenta.Tests
{
    public class CatalogoWebTests
    {
        [Theory]
        [InlineData(100, 15, 115.00)]
        [InlineData(10.10, 15, 11.62)]   // 11.615 → redondeo comercial hacia arriba
        [InlineData(50, 0, 50.00)]
        [InlineData(0, 15, 0.00)]
        public void PrecioConImpuesto_Redondea(double precio, double tasa, double esperado)
        {
            Assert.Equal((decimal)esperado, CatalogoWebHelper.PrecioConImpuesto((decimal)precio, (decimal)tasa));
        }

        [Theory]
        [InlineData("Ana", "9999-9999", null)]
        [InlineData("Ana", "+504 9999 9999", null)]
        [InlineData("", "99999999", "Escribe tu nombre.")]
        [InlineData("A", "99999999", "Escribe tu nombre.")]
        [InlineData("Ana", "123", "Escribe un teléfono válido para poder contactarte.")]
        [InlineData("Ana", null, "Escribe un teléfono válido para poder contactarte.")]
        public void ValidarContacto_Reglas(string? nombre, string? tel, string? error)
        {
            Assert.Equal(error, CatalogoWebHelper.ValidarContacto(nombre, tel));
        }

        [Fact]
        public void Limpiar_QuitaControlesYRecorta()
        {
            Assert.Equal("hola", CatalogoWebHelper.Limpiar("  ho\u0007la  ", 50));
            Assert.Equal("abc", CatalogoWebHelper.Limpiar("abcdef", 3));
            Assert.Equal("", CatalogoWebHelper.Limpiar(null, 10));
        }

        [Fact]
        public void MensajeWhatsApp_IncluyeLineasTotalYContacto()
        {
            var lineas = new List<CatalogoWebHelper.LineaCalculada>
            {
                new() { Id = 1, Nombre = "Café 1 lb", Cantidad = 2, Precio = 115m },
                new() { Id = 2, Nombre = "Azúcar", Cantidad = 1.5m, Precio = 20m }
            };
            var msg = CatalogoWebHelper.MensajeWhatsApp("Mi Tienda", "PW-1", lineas, 260m, "Ana", "9999-9999", "Col. Centro", null);
            Assert.Contains("PW-1", msg);
            Assert.Contains("2 × Café 1 lb — L. 230.00", msg);
            Assert.Contains("1.5 × Azúcar — L. 30.00", msg);
            Assert.Contains("*Total: L. 260.00*", msg);
            Assert.Contains("Col. Centro", msg);
            Assert.DoesNotContain("Notas", msg);
        }

        [Fact]
        public void EsquemaYRespaldo_IncluyenTablasNuevas()
        {
            foreach (var t in new[] { "VentasOffline", "PedidosWeb", "CatalogoWebConfig" })
            {
                Assert.Contains(t, Kirkenta.Helpers.EsquemaModulosNuevos.Tablas);
                Assert.Contains(t, Kirkenta.Services.RespaldoSistemaService.Grupos.Values.SelectMany(x => x));
            }
        }
    }
}
