using System.Reflection;
using Kirkenta.Helpers.Export;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>Lo que se exporta (o la plantilla) se vuelve a importar completo y con los mismos valores.</summary>
    public class ExportarPlantillaTests
    {
        private static ImportWizardConfig Config(Type pagina) =>
            (ImportWizardConfig)pagina.GetMethod("BuildConfig", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null)!;

        // columnas informativas que el sistema llena solo
        private static bool EsDelSistema(string h) => h.StartsWith("Fecha ");

        public static IEnumerable<object[]> Modulos() => new[]
        {
            new object[] { "Productos" }, new object[] { "Clientes" }, new object[] { "Proveedores" },
            new object[] { "Categorias" }, new object[] { "Impuestos" }, new object[] { "UnidadesMedida" }
        };

        private static (List<string> export, Dictionary<string, string[]> alias, List<string> plantilla) Datos(string m) => m switch
        {
            "Productos" => (ExportColumns.Productos(new(), new()).Select(c => c.Header).ToList(),
                Kirkenta.Pages.Productos.ImportMapModel.AliasCampos, Kirkenta.Pages.Productos.ImportMapModel.CamposDef.Select(c => c.Label).ToList()),
            "Clientes" => De(ExportColumns.Clientes().Select(c => c.Header), typeof(Kirkenta.Pages.Clientes.ImportMapModel)),
            "Proveedores" => De(ExportColumns.Proveedores().Select(c => c.Header), typeof(Kirkenta.Pages.Proveedores.ImportMapModel)),
            "Categorias" => De(ExportColumns.Categorias().Select(c => c.Header), typeof(Kirkenta.Pages.Categorias.ImportMapModel)),
            "Impuestos" => De(ExportColumns.Impuestos().Select(c => c.Header), typeof(Kirkenta.Pages.Impuestos.ImportMapModel)),
            _ => De(ExportColumns.UnidadesMedida().Select(c => c.Header), typeof(Kirkenta.Pages.UnidadesMedida.ImportMapModel)),
        };

        private static (List<string>, Dictionary<string, string[]>, List<string>) De(IEnumerable<string> export, Type pagina)
        {
            var c = Config(pagina);
            return (export.ToList(), c.AliasCampos, c.Campos.Select(x => x.Label).ToList());
        }

        [Theory, MemberData(nameof(Modulos))]
        public void Exportacion_SeReimportaSinPerderColumnas(string modulo)
        {
            var (export, alias, _) = Datos(modulo);
            var mapa = ColumnMapper.Detectar(export, alias);
            Assert.Empty(export.Where(h => !EsDelSistema(h) && !mapa.Values.Contains(h)));   // ninguna columna se pierde
            Assert.Empty(mapa.Where(x => x.Value == null).Select(x => x.Key));               // todo campo importable se exporta
        }

        [Theory, MemberData(nameof(Modulos))]
        public void Plantilla_SeReconoceCompleta(string modulo)
        {
            var (_, alias, plantilla) = Datos(modulo);
            var mapa = ColumnMapper.Detectar(plantilla, alias);
            Assert.Empty(mapa.Where(x => x.Value == null).Select(x => x.Key));
            Assert.Equal(plantilla.Count, mapa.Values.Distinct().Count(v => v != null));
        }

        private static readonly Proveedor Dificil = new()
        {
            Codigo = "0007", Nombre = "Ferretería «El Martillo», S. de R.L.", RTN = "08019012345678",
            Telefono = "+504 9999-0000", Direccion = "Col. Kennedy, bloque 3\nfrente al parque", LimiteCredito = 1234567.89m,
            DiasCredito = 30, Notas = "Paga \"puntual\"; descuento 5 %", Activo = false, CondicionPago = "Credito"
        };

        [Theory]
        [InlineData("csv")]
        [InlineData("excel")]
        [InlineData("json")]
        public void Exportar_YReimportar_ConservaCadaValor(string formato)
        {
            var cols = ExportColumns.Proveedores();
            var lista = new List<Proveedor> { Dificil };
            var archivo = formato switch
            {
                "csv" => CsvImporter.Read(new MemoryStream(CsvExporter.Export(lista, cols))),
                "json" => JsonImporter.Read(new MemoryStream(JsonExporter.Export(lista, cols))),
                _ => ExcelImporter.Read(new MemoryStream(ExcelExporter.Export(lista, cols, "Proveedores", "Listado de proveedores")))
            };
            Assert.True(archivo.Ok, archivo.Error);
            var fila = Assert.Single(archivo.Rows);
            var alias = Config(typeof(Kirkenta.Pages.Proveedores.ImportMapModel)).AliasCampos;
            var mapa = ColumnMapper.Detectar(archivo.Headers, alias);
            string V(string campo) => fila[mapa[campo]!];

            Assert.Equal("0007", V("Codigo"));                                   // cero a la izquierda
            Assert.Equal(Dificil.Nombre, V("Nombre"));
            Assert.Equal("08019012345678", V("RTN"));
            Assert.Equal(Dificil.Direccion, V("Direccion").Replace("\r\n", "\n"));
            Assert.Equal(Dificil.Notas, V("Notas"));
            Assert.Equal(1234567.89m, ParserHelper.ParsearDecimal(V("LimiteCredito")));
            Assert.Equal(30, ParserHelper.ParsearInt(V("DiasCredito")));
            Assert.False(ParserHelper.ParsearBool(V("Activo")));
        }
    }
}
