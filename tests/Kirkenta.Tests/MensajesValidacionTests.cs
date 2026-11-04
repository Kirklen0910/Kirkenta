using Kirkenta.Helpers;
using Xunit;

namespace Kirkenta.Tests
{
    public class MensajesValidacionTests
    {
        [Theory]
        [InlineData("The Nombre field is required.", "Input.Nombre", "«Nombre» es obligatorio.")]
        [InlineData("The value '' is invalid.", "Input.ProveedorId", "Falta «Proveedor».")]
        [InlineData("The value 'abc' is not valid for PrecioVenta.", "Input.PrecioVenta", "«abc» no es un valor válido para «Precio Venta».")]
        [InlineData("The field Stock must be a number.", "Input.Stock", "«Stock» debe ser un número.")]
        [InlineData("The field Nombre must be a string with a maximum length of 150.", "Input.Nombre", "«Nombre» admite como máximo 150 caracteres.")]
        [InlineData("Esa factura ya fue cargada en la orden OC-0007", null, "Esa factura ya fue cargada en la orden OC-0007")]
        public void PasaLosMensajesAEspanolClaro(string original, string? clave, string esperado) =>
            Assert.Equal(esperado, MensajesValidacion.Traducir(original, clave));

        [Fact]
        public void MensajeVacioDeUnCampo_DiceQueRevisarlo() =>
            Assert.Equal("Revisa «Fecha Ingreso».", MensajesValidacion.Traducir("", "Input.FechaIngreso"));
    }
}
