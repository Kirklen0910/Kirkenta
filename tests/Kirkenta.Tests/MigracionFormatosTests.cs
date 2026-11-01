using System.Text;
using Kirkenta.Helpers.Migracion;
using Xunit;

namespace Kirkenta.Tests
{
    public class MigracionFormatosTests
    {
        private static byte[] CrearDbf()
        {
            // dBase III: 2 campos (NOMBRE C(10), PRECIO N(8,2)), 2 registros, el segundo borrado
            var campos = new[] { ("NOMBRE", 'C', 10, 0), ("PRECIO", 'N', 8, 2) };
            int cab = 32 + campos.Length * 32 + 1, reg = 1 + 10 + 8;
            var b = new List<byte>();
            var h = new byte[32]; h[0] = 3;
            BitConverter.GetBytes((uint)2).CopyTo(h, 4);
            BitConverter.GetBytes((ushort)cab).CopyTo(h, 8);
            BitConverter.GetBytes((ushort)reg).CopyTo(h, 10);
            b.AddRange(h);
            foreach (var (n, t, l, d) in campos)
            {
                var f = new byte[32];
                Encoding.ASCII.GetBytes(n).CopyTo(f, 0); f[11] = (byte)t; f[16] = (byte)l; f[17] = (byte)d;
                b.AddRange(f);
            }
            b.Add(0x0D);
            b.Add((byte)' '); b.AddRange(Encoding.Latin1.GetBytes("Jos\u00e9      ")); b.AddRange(Encoding.ASCII.GetBytes("   12.50"));
            b.Add((byte)'*'); b.AddRange(Encoding.ASCII.GetBytes("Borrado   ")); b.AddRange(Encoding.ASCII.GetBytes("    1.00"));
            b.Add(0x1A);
            return b.ToArray();
        }

        [Fact]
        public void Dbf_LeeColumnasYSaltaBorrados()
        {
            var r = MigracionLector.Leer("productos.dbf", new MemoryStream(CrearDbf()));
            var t = Assert.Single(r.Tablas);
            Assert.Equal("productos", t.Nombre);
            var fila = Assert.Single(t.Rows);
            Assert.Equal("Jos\u00e9", fila["NOMBRE"]);
            Assert.Equal("12.50", fila["PRECIO"]);
        }

        [Fact]
        public void Access_DaMensajeClaro()
        {
            var r = MigracionLector.Leer("viejo.mdb", new MemoryStream(new byte[] { 1, 2, 3 }));
            Assert.Empty(r.Tablas);
            Assert.Contains(r.Avisos, a => a.Contains("Access"));
        }

        [Fact]
        public void ColumnasSinNombreUtil_SeReconocenPorContenido()
        {
            var sql = new StringBuilder("INSERT INTO `t` (`c1`,`c2`,`c3`) VALUES ");
            sql.Append("(1,'Ana','ana@x.com'),(2,'Luis','luis@x.com'),(3,'Rosa','rosa@x.com'),(4,'Mia','mia@x.com');");
            var r = MigracionLector.Leer("t.sql", new MemoryStream(Encoding.UTF8.GetBytes(sql.ToString())));
            var t = Assert.Single(r.Tablas);
            var clientes = MigracionAnalizador.BuscarEntidad("Clientes")!;
            var m2 = clientes.Alias.Keys.ToDictionary(k => k, k => (string?)null);
            MigracionAnalizador.CompletarPorContenido(clientes, t, m2);
            Assert.Equal("c3", m2["Email"]);
        }
    }
}
