using System.Globalization;
using System.Text;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Generador de códigos de barras Code 128 (el que leen todas las pistolas y cámaras) en SVG,
    /// y utilidades para códigos internos EAN-13. No depende de librerías externas.
    /// </summary>
    public static class CodigoBarrasHelper
    {
        // Anchos de barra/espacio (6 elementos = 11 módulos) de los símbolos 0..105 y el de parada (106, 7 elementos = 13 módulos).
        public static readonly string[] Patrones =
        {
            "212222","222122","222221","121223","121322","131222","122213","122312","132212","221213",
            "221312","231212","112232","122132","122231","113222","123122","123221","223211","221132",
            "221231","213212","223112","312131","311222","321122","321221","312212","322112","322211",
            "212123","212321","232121","111323","131123","131321","112313","132113","132311","211313",
            "231113","231311","112133","112331","132131","113123","113321","133121","313121","211331",
            "231131","213113","213311","213131","311123","311321","331121","312113","312311","332111",
            "314111","221411","431111","111224","111422","121124","121421","141122","141221","112214",
            "112412","122114","122411","142112","142211","241211","221114","413111","241112","134111",
            "111242","121142","121241","114212","124112","124211","411212","421112","421211","212141",
            "214121","412121","111143","111341","131141","114113","114311","411113","411311","113141",
            "114131","311141","411131","211412","211214","211232","2331112"
        };

        private const int StartB = 104, StartC = 105, CodeC = 99, Stop = 106;

        /// <summary>Un texto se puede codificar si todos sus caracteres son ASCII imprimible (32–126).</summary>
        public static bool EsCodificable(string? texto)
            => !string.IsNullOrEmpty(texto) && texto.Length <= 40 && texto.All(c => c >= 32 && c <= 126);

        /// <summary>Valores de los símbolos (inicio, datos, dígito de control y parada) para el texto dado.</summary>
        public static List<int> Simbolos(string texto)
        {
            if (!EsCodificable(texto)) throw new ArgumentException("Texto no válido para Code 128.", nameof(texto));

            var s = new List<int>();
            bool todoDigitos = texto.All(char.IsAsciiDigit);

            if (todoDigitos && texto.Length >= 4)
            {
                int i = 0;
                if (texto.Length % 2 == 0) s.Add(StartC);
                else
                {
                    // Cantidad impar: el primer dígito va en B y el resto, en pares, en C.
                    s.Add(StartB); s.Add(texto[0] - 32); s.Add(CodeC); i = 1;
                }
                for (; i < texto.Length; i += 2)
                    s.Add((texto[i] - '0') * 10 + (texto[i + 1] - '0'));
            }
            else
            {
                s.Add(StartB);
                foreach (var c in texto) s.Add(c - 32);
            }

            long suma = s[0];
            for (int k = 1; k < s.Count; k++) suma += (long)k * s[k];
            s.Add((int)(suma % 103));
            s.Add(Stop);
            return s;
        }

        /// <summary>Secuencia de módulos: '1' barra, '0' espacio.</summary>
        public static string Modulos(string texto)
        {
            var sb = new StringBuilder();
            foreach (var sim in Simbolos(texto))
            {
                bool barra = true;
                foreach (var ch in Patrones[sim])
                {
                    sb.Append(barra ? '1' : '0', ch - '0');
                    barra = !barra;
                }
            }
            return sb.ToString();
        }

        /// <summary>SVG del código (sin texto debajo). Se estira al ancho disponible; el alto lo da el contenedor.</summary>
        public static string Svg(string texto, int alto = 50)
        {
            var mod = Modulos(texto);
            const int zonaSilencio = 10;
            int ancho = mod.Length + zonaSilencio * 2;
            var d = new StringBuilder();
            int i = 0;
            while (i < mod.Length)
            {
                if (mod[i] == '1')
                {
                    int j = i;
                    while (j < mod.Length && mod[j] == '1') j++;
                    d.Append(CultureInfo.InvariantCulture, $"M{i + zonaSilencio} 0h{j - i}v{alto}h-{j - i}z");
                    i = j;
                }
                else i++;
            }
            return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {ancho} {alto}\" preserveAspectRatio=\"none\" shape-rendering=\"crispEdges\" role=\"img\" aria-label=\"Código de barras {System.Net.WebUtility.HtmlEncode(texto)}\"><path d=\"{d}\" fill=\"#000\"/></svg>";
        }

        // ---------- EAN-13 interno ----------

        /// <summary>Dígito verificador EAN-13 para los 12 primeros dígitos.</summary>
        public static int DigitoEan13(string doce)
        {
            if (doce == null || doce.Length != 12 || !doce.All(char.IsAsciiDigit)) throw new ArgumentException("Se esperan 12 dígitos.", nameof(doce));
            int suma = 0;
            for (int i = 0; i < 12; i++) suma += (doce[i] - '0') * (i % 2 == 0 ? 1 : 3);
            return (10 - suma % 10) % 10;
        }

        public static bool Ean13Valido(string? codigo)
            => codigo != null && codigo.Length == 13 && codigo.All(char.IsAsciiDigit) && DigitoEan13(codigo[..12]) == codigo[12] - '0';

        /// <summary>
        /// Código interno de tienda para un producto: empieza con 2 (rango reservado de GS1 para uso interno),
        /// luego el Id del producto con 11 dígitos y el dígito verificador. Es único por producto.
        /// </summary>
        public static string CodigoInterno(int productoId)
        {
            if (productoId < 0) throw new ArgumentOutOfRangeException(nameof(productoId));
            var doce = "2" + productoId.ToString("D11", CultureInfo.InvariantCulture);
            return doce + DigitoEan13(doce);
        }
    }
}
