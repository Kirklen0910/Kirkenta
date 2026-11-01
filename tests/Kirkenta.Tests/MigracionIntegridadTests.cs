using System.Text;
using Kirkenta.Helpers.Migracion;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class MigracionIntegridadTests
    {
        private static List<TablaOrigen> Leer(string json) =>
            MigracionLector.Leer("origen.json", new MemoryStream(Encoding.UTF8.GetBytes(json))).Tablas;

        private static TablaOrigen Tabla(string col, params string[] valores) => new()
        {
            Nombre = "t", Headers = new() { col },
            Rows = valores.Select(v => new Dictionary<string, string> { [col] = v }).ToList()
        };

        private static List<PasoMigracion> Plan(string campo, string col) =>
            new() { new PasoMigracion { Incluir = true, TablaIndice = 0, Destino = "@ventas", Mapa = new() { [campo] = col } } };

        // ---------- fechas ----------
        [Fact]
        public void Fechas_UnValorConDiaMayorA12_DecideToda_LaColumna()
        {
            var t = Tabla("fecha", "03/04/2024", "25/04/2024");
            var f = MigracionFormatos.Normalizar(new() { t }, Plan("Fecha", "fecha")).Single();
            Assert.Equal("dmy", f.Formato);
            Assert.False(f.Ambiguo);
            Assert.Equal("2024-04-03", t.Rows[0]["fecha"]);       // 3 de abril, no 4 de marzo
        }

        [Fact]
        public void Fechas_FormatoAmericano_SeDetecta()
        {
            var t = Tabla("fecha", "03/04/2024 14:30", "04/25/2024");
            MigracionFormatos.Normalizar(new() { t }, Plan("Fecha", "fecha"));
            Assert.Equal("2024-03-04T14:30:00", t.Rows[0]["fecha"]);   // 4 de marzo
            Assert.Equal("2024-04-25", t.Rows[1]["fecha"]);
        }

        [Fact]
        public void Fechas_Ambiguas_SeAvisanYUsanLoElegido()
        {
            var tablas = new List<TablaOrigen> { Tabla("fecha", "03/04/2024", "01/02/2024") };
            Assert.True(MigracionFormatos.Analizar(tablas, Plan("Fecha", "fecha")).Single().Ambiguo);
            MigracionFormatos.Normalizar(tablas, Plan("Fecha", "fecha"), "mdy");
            Assert.Equal("2024-03-04", tablas[0].Rows[0]["fecha"]);
        }

        // ---------- números ----------
        [Fact]
        public void Numeros_ComaDecimal_ElPuntoEsDeMiles()
        {
            var t = Tabla("total", "1.234,50", "1.234");
            MigracionFormatos.Normalizar(new() { t }, Plan("Total", "total"));
            Assert.Equal(1234.50m, Kirkenta.Helpers.Import.ParserHelper.ParsearDecimal(t.Rows[0]["total"]));
            Assert.Equal(1234m, Kirkenta.Helpers.Import.ParserHelper.ParsearDecimal(t.Rows[1]["total"]));
        }

        [Fact]
        public void Numeros_PuntoDecimal_LaComaEsDeMiles()
        {
            var t = Tabla("total", "12.50", "1,234");
            MigracionFormatos.Normalizar(new() { t }, Plan("Total", "total"));
            Assert.Equal(1234m, Kirkenta.Helpers.Import.ParserHelper.ParsearDecimal(t.Rows[1]["total"]));
        }

        // ---------- relaciones por id interno ----------
        private const string JsonIds = @"{
  ""clientes"": [ { ""id"": 1, ""codigo"": ""C-01"", ""nombre"": ""Ana Pérez"" }, { ""id"": 2, ""codigo"": ""C-02"", ""nombre"": ""Beto López"" } ],
  ""productos"": [ { ""id"": 10, ""sku"": ""P-1"", ""nombre"": ""Lápiz"", ""precio"": 5, ""costo"": 2, ""existencia"": 40 },
                   { ""id"": 11, ""sku"": ""P-2"", ""nombre"": ""Cuaderno"", ""precio"": 30, ""costo"": 18, ""existencia"": 12 } ],
  ""ventas"": [ { ""id"": 7, ""numero"": ""F-100"", ""fecha"": ""2024-01-05"", ""total"": 70, ""cliente_id"": 2 } ],
  ""detalle_ventas"": [ { ""venta_id"": 7, ""producto_id"": 11, ""cantidad"": 2, ""precio"": 30 },
                        { ""venta_id"": 7, ""producto_id"": 10, ""cantidad"": 2, ""precio"": 5 } ]
}";

        [Fact]
        public void Relaciones_PorIdInterno_UnenClienteProductoYLineas()
        {
            var ctx = TestDb.Crear();
            var tablas = Leer(JsonIds);
            var plan = MigracionAutomaticaHelper.Planificar(tablas);
            var ventas = plan.Single(p => p.Destino == "@ventas");
            Assert.True(ventas.Incluir, ventas.Motivo);
            Assert.Contains(ventas.Relaciones, r => r.Contains("cliente_id"));

            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, plan, null, true);
            Assert.True(res.Ok, res.ErrorFatal);

            var venta = ctx.Ventas.Single(v => v.Numero == "H-F-100");
            Assert.Equal("Beto López", ctx.Clientes.Single(c => c.Id == venta.ClienteId).Nombre);
            var lineas = ctx.DetalleVentas.Where(d => d.VentaId == venta.Id).ToList();
            Assert.Equal(2, lineas.Count);
            var cuaderno = ctx.Productos.Single(p => p.SKU == "P-2");
            Assert.Equal(2, lineas.Single(l => l.ProductoId == cuaderno.Id).Cantidad);
        }

        // ---------- verificación registro por registro ----------
        [Fact]
        public void Verificacion_ComparaCadaRegistroYSaleExacta()
        {
            var ctx = TestDb.Crear();
            var tablas = Leer(JsonIds);
            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true);
            Assert.True(res.Ok, res.ErrorFatal);

            var prod = res.Verificacion.Single(v => v.Tabla == "productos");
            Assert.Equal(2, prod.Revisados);
            Assert.True(prod.Exacto, string.Join(" | ", prod.Diferencias));
            Assert.Equal(1, res.Verificacion.Single(v => v.Tabla == "ventas").Revisados);
            Assert.All(res.Verificacion, v => Assert.True(v.Exacto, string.Join(" | ", v.Diferencias)));
        }

        [Fact]
        public void TextoMasLargoQueLaColumna_SeAvisaNoSeRecortaEnSilencio()
        {
            var ctx = TestDb.Crear();
            var largo = new string('x', 180);
            var tablas = Leer(@"{ ""productos"": [ { ""sku"": ""L-1"", ""nombre"": """ + largo + @""", ""precio"": 1 } ] }");
            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true);
            Assert.True(res.Ok, res.ErrorFatal);

            var v = res.Verificacion.Single();
            Assert.Equal(1, v.Recortados);
            Assert.False(v.Exacto);
            Assert.Contains(v.Diferencias, d => d.Contains(largo));          // el texto completo queda en el informe
            Assert.Equal(1, res.Conciliacion.Single().Recortados);
            Assert.Contains(res.Pasos.Single().Res!.Advertencias, a => a.Contains("recortaron"));
        }

        // ---------- avance ----------
        [Fact]
        public void Avance_SubeHastaElFinal()
        {
            var ctx = TestDb.Crear();
            var tablas = Leer(JsonIds);
            var vistos = new List<int>();
            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true,
                (p, _) => vistos.Add(p));
            Assert.True(res.Ok, res.ErrorFatal);
            Assert.True(vistos.Count > 3);
            Assert.True(vistos.Last() >= 95);
            Assert.Equal(vistos.OrderBy(x => x), vistos);       // nunca retrocede
        }

        // ---------- volcados sin límite: lectura por bloques ----------
        [Fact]
        public void VolcadoSql_PorBloques_DaLoMismoQueEntero()
        {
            var sb = new StringBuilder("CREATE TABLE `clientes` (`id` int, `nombre` varchar(50), `nota` text);\n");
            for (int i = 1; i <= 300; i++)
                sb.Append($"INSERT INTO `clientes` VALUES ({i},'Cliente {i}','línea uno;\nlínea dos'),({i + 1000},'O\\'Brien {i}','x');\n");
            sb.Append("COPY public.items (id, nombre) FROM stdin;\n1\tUno\n2\tDos\n\\.\n");
            var sql = sb.ToString();

            var entero = SqlDumpParser.Parse(sql);
            var bloques = SqlDumpParser.Parse(new StringReader(sql), null, 256);     // bloques diminutos a propósito

            Assert.Equal(entero.Select(t => (t.Nombre, t.Rows.Count)), bloques.Select(t => (t.Nombre, t.Rows.Count)));
            Assert.Equal(600, bloques.Single(t => t.Nombre == "clientes").Rows.Count);
            Assert.Equal("línea uno;\nlínea dos", bloques.Single(t => t.Nombre == "clientes").Rows[0]["nota"]);
            Assert.Equal(2, bloques.Single(t => t.Nombre == "items").Rows.Count);
        }
    }
}
