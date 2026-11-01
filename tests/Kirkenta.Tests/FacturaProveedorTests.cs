using System.Text.Json;
using Kirkenta.Helpers;
using Kirkenta.Services.Asistente;
using Xunit;

namespace Kirkenta.Tests
{
    public class FacturaProveedorTests
    {
        private record Prod(string Nombre, string? Sku, string? Barras);

        [Theory]
        [InlineData("Café  Grano-1LB", "cafe grano 1lb")]
        [InlineData("  ÁÉÍÓÚ Ñandú ", "aeiou nandu")]
        [InlineData(null, "")]
        public void Normalizar_QuitaTildesYSignos(string? entrada, string esperado)
            => Assert.Equal(esperado, FacturaProveedorHelper.Normalizar(entrada));

        [Theory]
        [InlineData("1,234.50", 1234.50)]
        [InlineData("1.234,50", 1234.50)]
        [InlineData("L. 85", 85)]
        [InlineData("12,5", 12.5)]
        [InlineData("1,234", 1234)]
        [InlineData("-15.25", -15.25)]
        public void ParseDecimal_Formatos(string s, double esperado)
            => Assert.Equal((decimal)esperado, FacturaProveedorHelper.ParseDecimal(s));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abc")]
        public void ParseDecimal_Invalido(string? s) => Assert.Null(FacturaProveedorHelper.ParseDecimal(s));

        [Fact]
        public void Similitud_ParecidosYDistintos()
        {
            Assert.Equal(1, FacturaProveedorHelper.Similitud("Aceite Girasol 1L", "aceite girasol 1l"));
            Assert.True(FacturaProveedorHelper.Similitud("Aceite de girasol 1 litro", "Aceite girasol 1 litro") >= 0.5);
            Assert.True(FacturaProveedorHelper.Similitud("Arroz", "Detergente líquido") < 0.2);
            Assert.Equal(0, FacturaProveedorHelper.Similitud("", "Arroz"));
        }

        [Fact]
        public void MejorCoincidencia_CodigoExactoGana()
        {
            var lista = new List<Prod> { new("Arroz Blanco 5 lb", "ARZ-05", "7001"), new("Arroz Integral 5 lb", "ARZ-06", null) };
            var p = FacturaProveedorHelper.MejorCoincidencia(lista, "arz 06", "algo distinto", x => x.Nombre, x => x.Sku, x => x.Barras, 0.5, out var pt);
            Assert.Equal("Arroz Integral 5 lb", p!.Nombre);
            Assert.Equal(1, pt);
        }

        [Fact]
        public void MejorCoincidencia_PorNombreOSinResultado()
        {
            var lista = new List<Prod> { new("Arroz Blanco 5 lb", "ARZ-05", null), new("Frijol Rojo 1 lb", "FRJ-01", null) };
            var p = FacturaProveedorHelper.MejorCoincidencia(lista, null, "ARROZ BLANCO 5LB", x => x.Nombre, x => x.Sku, x => x.Barras, 0.3, out _);
            Assert.NotNull(p);
            var ninguno = FacturaProveedorHelper.MejorCoincidencia(lista, null, "Detergente", x => x.Nombre, x => x.Sku, x => x.Barras, 0.5, out var pt);
            Assert.Null(ninguno);
            Assert.Equal(0, pt);
        }

        [Theory]
        [InlineData(1150, 1150, null, false)]
        [InlineData(1000, 1150, 150, false)]   // coincide con el total sin ISV
        [InlineData(900, 1150, 150, true)]
        [InlineData(500, null, null, false)]
        public void TotalesDifieren_Reglas(int suma, int? total, int? isv, bool esperado)
            => Assert.Equal(esperado, FacturaProveedorHelper.TotalesDifieren((decimal)suma, (decimal?)total, (decimal?)isv));

        [Fact]
        public void ConvertirFactura_LeeCamposYCalculaPrecioFaltante()
        {
            var json = """
            {"proveedor":"Distribuidora XYZ","rtn":"0801-1999-123456","numero":"000-001-01-00001234","fecha":"2026-10-20",
             "subtotal":"1,000.00","isv":150,"total":1150,
             "lineas":[{"codigo":"A1","descripcion":"Aceite 1L","cantidad":10,"precio_unitario":null,"total":500},
                       {"descripcion":"Arroz 5lb","cantidad":"4","precio_unitario":"125.50","total":502}]}
            """;
            using var doc = JsonDocument.Parse(json);
            var f = AsistenteIA.ConvertirFactura(doc.RootElement);
            Assert.Null(f.Error);
            Assert.Equal("Distribuidora XYZ", f.Proveedor);
            Assert.Equal(new DateTime(2026, 10, 20), f.Fecha);
            Assert.Equal(1000m, f.Subtotal);
            Assert.Equal(2, f.Lineas.Count);
            Assert.Equal(50m, f.Lineas[0].Precio);      // 500 / 10
            Assert.Equal(125.50m, f.Lineas[1].Precio);
            Assert.Equal(4m, f.Lineas[1].Cantidad);
        }

        [Theory]
        [InlineData("https://api.anthropic.com/v1/messages", true)]
        [InlineData("HTTPS://API.ANTHROPIC.COM/v1/messages", true)]
        [InlineData("https://api.openai.com/v1/chat/completions", false)]
        [InlineData("http://localhost:11434/v1/chat/completions", false)]
        [InlineData("", false)]
        public void EsAnthropic_DetectaLaApiDeClaude(string url, bool esperado)
            => Assert.Equal(esperado, AsistenteIA.EsAnthropic(url));

        [Fact]
        public void ConvertirFactura_SinLineasEsError()
        {
            using var doc = JsonDocument.Parse("{\"proveedor\":\"X\",\"lineas\":[]}");
            Assert.NotNull(AsistenteIA.ConvertirFactura(doc.RootElement).Error);
        }
    }
}
