using System.Text;
using Kirkenta.Helpers.Migracion;
using Xunit;

namespace Kirkenta.Tests
{
    public class MigracionCodificacionTests
    {
        private static MemoryStream Latin1(string s) => new(Encoding.Latin1.GetBytes(s));

        [Fact]
        public void SqlEnLatin1_ConservaAcentos()
        {
            var r = MigracionLector.Leer("viejo.sql", Latin1("INSERT INTO `clientes` (`id`,`nombre`) VALUES (1,'José Ñandú');"));
            var t = Assert.Single(r.Tablas);
            Assert.Equal("José Ñandú", t.Rows[0]["nombre"]);
        }

        [Fact]
        public void JsonLinesEnLatin1_ConservaAcentos()
        {
            var r = MigracionLector.Leer("c.jsonl", Latin1("{\"nombre\":\"María Peña\"}"));
            var t = Assert.Single(r.Tablas);
            Assert.Equal("María Peña", t.Rows[0]["nombre"]);
        }

        [Fact]
        public void JsonEnUtf8ConBom_SigueLeyendo()
        {
            var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("[{\"nombre\":\"Ana\"}]")).ToArray();
            var r = MigracionLector.Leer("c.json", new MemoryStream(bytes));
            Assert.Single(r.Tablas);
        }
    }
}
