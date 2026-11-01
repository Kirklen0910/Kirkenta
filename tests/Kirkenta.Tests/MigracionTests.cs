using System.IO.Compression;
using System.Text;
using Kirkenta.Helpers.Migracion;
using Xunit;

namespace Kirkenta.Tests
{
    public class MigracionTests
    {
        private static MemoryStream Bytes(string s) => new(Encoding.UTF8.GetBytes(s));

        [Fact]
        public void SqlMysqldump_LeeColumnasYFilas_ConComillasYNulos()
        {
            var sql = @"
CREATE TABLE `tbl_clientes` (
  `id` int(11) NOT NULL,
  `nombre` varchar(100),
  `limite` decimal(10,2),
  PRIMARY KEY (`id`)
) ENGINE=InnoDB;
INSERT INTO `tbl_clientes` VALUES (1,'Juan O\'Brien',500.50),(2,'María, S.A.',NULL);
INSERT INTO `tbl_clientes` (`id`,`nombre`) VALUES (3,'Pedro ''el'' grande');";

            var tablas = SqlDumpParser.Parse(sql);
            var t = Assert.Single(tablas);
            Assert.Equal("tbl_clientes", t.Nombre);
            Assert.Equal(3, t.Rows.Count);
            Assert.Equal(new[] { "id", "nombre", "limite" }, t.Headers);
            Assert.Equal("Juan O'Brien", t.Rows[0]["nombre"]);
            Assert.Equal("500.50", t.Rows[0]["limite"]);
            Assert.Equal("María, S.A.", t.Rows[1]["nombre"]);
            Assert.Equal("", t.Rows[1]["limite"]);
            Assert.Equal("Pedro 'el' grande", t.Rows[2]["nombre"]);
        }

        [Fact]
        public void SqlPostgresCopy_LeeFilas()
        {
            var sql = "COPY public.productos (codigo, nombre, precio) FROM stdin;\nA1\tTornillo\t2.5\nA2\t\\N\t3\n\\.\n";
            var t = Assert.Single(SqlDumpParser.Parse(sql));
            Assert.Equal("productos", t.Nombre);
            Assert.Equal(2, t.Rows.Count);
            Assert.Equal("Tornillo", t.Rows[0]["nombre"]);
            Assert.Equal("", t.Rows[1]["nombre"]);
        }

        [Fact]
        public void Json_ObjetoConVariasTablas()
        {
            var json = "{\"clientes\":[{\"nombre\":\"Ana\",\"rtn\":\"123\"}],\"productos\":[{\"sku\":\"A\",\"precio\":10}]}";
            var r = MigracionLector.Leer("backup.json", Bytes(json));
            Assert.Equal(2, r.Tablas.Count);
            Assert.Contains(r.Tablas, t => t.Nombre == "clientes" && t.Rows[0]["nombre"] == "Ana");
            Assert.Contains(r.Tablas, t => t.Nombre == "productos" && t.Rows[0]["precio"] == "10");
        }

        [Fact]
        public void Json_FormatoPhpMyAdmin()
        {
            var json = "[{\"type\":\"header\"},{\"type\":\"table\",\"name\":\"proveedores\",\"data\":[{\"nombre\":\"Casa X\"}]}]";
            var r = MigracionLector.Leer("export.json", Bytes(json));
            var t = Assert.Single(r.Tablas);
            Assert.Equal("proveedores", t.Nombre);
        }

        [Fact]
        public void Zip_RespaldoKirkenta_LeeJsonlPorTabla()
        {
            var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                void Entrada(string n, string c)
                {
                    using var w = new StreamWriter(zip.CreateEntry(n).Open(), new UTF8Encoding(false));
                    w.Write(c);
                }
                Entrada("manifest.json", "{\"Version\":1}");
                Entrada("data/Clientes.jsonl", "{\"Nombre\":\"A\",\"Telefono\":\"1\"}\n{\"Nombre\":\"B\",\"Telefono\":\"2\"}\n");
                Entrada("archivos/wwwroot/uploads/x.png", "xx");
            }
            ms.Position = 0;
            var r = MigracionLector.Leer("respaldo.zip", ms);
            var t = Assert.Single(r.Tablas);
            Assert.Equal("Clientes", t.Nombre);
            Assert.Equal(2, t.Rows.Count);
        }

        [Fact]
        public void Analizador_DetectaEntidadPorColumnas_AunqueLaTablaTengaOtroNombre()
        {
            var cli = new TablaOrigen
            {
                Nombre = "tbl_x1",
                Headers = new() { "Cliente", "RTN", "Telefono", "LimiteCredito", "DiasCredito" },
                Rows = new() { new() { ["Cliente"] = "A", ["RTN"] = "1", ["Telefono"] = "2", ["LimiteCredito"] = "100", ["DiasCredito"] = "30" } }
            };
            var prod = new TablaOrigen
            {
                Nombre = "maestro",
                Headers = new() { "Codigo", "Descripcion", "Costo", "Precio", "Existencia" },
                Rows = new() { new() { ["Codigo"] = "A", ["Descripcion"] = "Clavo", ["Costo"] = "1", ["Precio"] = "2", ["Existencia"] = "5" } }
            };
            var d = MigracionAnalizador.Analizar(new[] { cli, prod });
            Assert.Equal("Clientes", d[0].Entidad?.Key);
            Assert.Equal("Productos", d[1].Entidad?.Key);
        }

        [Fact]
        public void Analizador_NombreDeTablaAyudaYTablaDeVentasNoSeFuerza()
        {
            var prov = new TablaOrigen
            {
                Nombre = "Proveedores",
                Headers = new() { "Nombre", "Contacto", "Telefono" },
                Rows = new() { new() { ["Nombre"] = "X", ["Contacto"] = "Y", ["Telefono"] = "1" } }
            };
            var ventas = new TablaOrigen
            {
                Nombre = "facturas",
                Headers = new() { "Numero", "Fecha", "Total" },
                Rows = new() { new() { ["Numero"] = "1", ["Fecha"] = "2020-01-01", ["Total"] = "5" } }
            };
            var d = MigracionAnalizador.Analizar(new[] { prov, ventas });
            Assert.Equal("Proveedores", d[0].Entidad?.Key);
            Assert.Equal("Alta", d[0].Confianza);
            Assert.Null(d[1].Entidad);
            Assert.NotNull(d[1].PosibleFuturo);
            Assert.Equal("ventas", d[1].TipoHistorial);
        }

        [Fact]
        public void Almacen_GuardaYCarga()
        {
            var t = new List<TablaOrigen> { new() { Nombre = "a", Headers = new() { "x" }, Rows = new() { new() { ["x"] = "1" } } } };
            var token = MigracionAlmacen.Guardar(t);
            var cargado = MigracionAlmacen.Cargar(token);
            Assert.NotNull(cargado);
            Assert.Equal("1", cargado![0].Rows[0]["x"]);
            MigracionAlmacen.Eliminar(token);
            Assert.Null(MigracionAlmacen.Cargar(token));
            Assert.Null(MigracionAlmacen.Cargar("../../etc/passwd"));
        }
    }
}
