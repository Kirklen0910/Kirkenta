using Kirkenta.Helpers;
using Xunit;

namespace Kirkenta.Tests
{
    public class CodigoBarrasTests
    {
        [Fact]
        public void Patrones_TodosSuman11Modulos_YSonUnicos()
        {
            Assert.Equal(107, CodigoBarrasHelper.Patrones.Length);
            for (int i = 0; i < 106; i++)
            {
                Assert.Equal(6, CodigoBarrasHelper.Patrones[i].Length);
                Assert.Equal(11, CodigoBarrasHelper.Patrones[i].Sum(c => c - '0'));
            }
            Assert.Equal(13, CodigoBarrasHelper.Patrones[106].Sum(c => c - '0'));
            Assert.Equal(107, CodigoBarrasHelper.Patrones.Distinct().Count());
        }

        /// <summary>Decodifica los módulos como lo haría un lector y devuelve el texto original.</summary>
        private static string Decodificar(string modulos)
        {
            string Bits(int v)
            {
                var sb = new System.Text.StringBuilder(); bool barra = true;
                foreach (var ch in CodigoBarrasHelper.Patrones[v]) { sb.Append(barra ? '1' : '0', ch - '0'); barra = !barra; }
                return sb.ToString();
            }
            var tabla = Enumerable.Range(0, 106).ToDictionary(Bits, v => v);
            var simbolos = new List<int>(); int pos = 0;
            while (pos < modulos.Length)
            {
                if (modulos.Length - pos == 13) { Assert.Equal(Bits(106), modulos.Substring(pos, 13)); simbolos.Add(106); pos += 13; break; }
                simbolos.Add(tabla[modulos.Substring(pos, 11)]); pos += 11;
            }
            Assert.Equal(modulos.Length, pos);
            long suma = simbolos[0];
            for (int k = 1; k < simbolos.Count - 2; k++) suma += (long)k * simbolos[k];
            Assert.Equal((int)(suma % 103), simbolos[^2]);

            var modo = simbolos[0] == 105 ? 'C' : 'B'; var texto = new System.Text.StringBuilder();
            foreach (var v in simbolos.Skip(1).Take(simbolos.Count - 3))
            {
                if (v == 99) { modo = 'C'; continue; }
                if (modo == 'C') texto.Append(v.ToString("00")); else texto.Append((char)(v + 32));
            }
            return texto.ToString();
        }

        [Theory]
        [InlineData("7501234567890")]   // 13 dígitos (impar): B + C
        [InlineData("4006381333931")]
        [InlineData("200000000012")]    // par: todo en C
        [InlineData("123")]
        [InlineData("ABC-123")]
        [InlineData("Hola Mundo!")]
        [InlineData("a1b2")]
        [InlineData("0001")]
        public void Codificar_YDecodificar_DevuelveElMismoTexto(string texto)
        {
            Assert.Equal(texto, Decodificar(CodigoBarrasHelper.Modulos(texto)));
        }

        [Fact]
        public void Svg_TieneBarras_YEscapaElTexto()
        {
            var svg = CodigoBarrasHelper.Svg("AB<1>");
            Assert.Contains("<path", svg);
            Assert.Contains("AB&lt;1&gt;", svg);
            Assert.DoesNotContain("AB<1>", svg);
        }

        [Theory]
        [InlineData("")]
        [InlineData("ñandú")]
        public void NoCodificable_Rechaza(string texto)
        {
            Assert.False(CodigoBarrasHelper.EsCodificable(texto));
            Assert.Throws<ArgumentException>(() => CodigoBarrasHelper.Simbolos(texto));
        }

        [Fact]
        public void Ean13_DigitoVerificador()
        {
            Assert.Equal(1, CodigoBarrasHelper.DigitoEan13("400638133393"));
            Assert.True(CodigoBarrasHelper.Ean13Valido("4006381333931"));
            Assert.False(CodigoBarrasHelper.Ean13Valido("4006381333932"));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(250)]
        [InlineData(99999)]
        public void CodigoInterno_EsEan13ValidoYUnico(int id)
        {
            var c = CodigoBarrasHelper.CodigoInterno(id);
            Assert.Equal(13, c.Length);
            Assert.StartsWith("2", c);
            Assert.True(CodigoBarrasHelper.Ean13Valido(c));
            Assert.NotEqual(c, CodigoBarrasHelper.CodigoInterno(id + 1));
        }

        [Fact]
        public void ParsearItems_UneRepetidosIgnoraBasuraYLimita()
        {
            var r = EtiquetasHelper.ParsearItems("5:3, 7, x:2, 5:2, -1:4, 9:0, 8:abc");
            Assert.Equal(new[] { (5, 5), (7, 1) }, r.ToArray());
            var lim = EtiquetasHelper.ParsearItems("1:800,2:800", 1000);
            Assert.Equal(1000, lim.Sum(x => x.Cantidad));
            Assert.Empty(EtiquetasHelper.ParsearItems(null));
        }

        [Fact]
        public void EnPaginas_YFormatos()
        {
            var pag = EtiquetasHelper.EnPaginas(Enumerable.Range(1, 50), 21);
            Assert.Equal(new[] { 21, 21, 8 }, pag.Select(p => p.Count).ToArray());
            Assert.Equal(21, EtiquetasHelper.Formato("hoja21").PorPagina);
            Assert.True(EtiquetasHelper.Formato("50x25").EsRollo);
            Assert.Equal("50x25", EtiquetasHelper.Formato("no-existe").Clave);
            Assert.Equal("EAN", EtiquetasHelper.CodigoDe(null, " EAN "));
            Assert.Equal("123", EtiquetasHelper.CodigoDe("123", "SKU"));
            Assert.Null(EtiquetasHelper.CodigoDe(" ", null));
        }
    }
}
