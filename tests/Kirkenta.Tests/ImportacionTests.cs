using System.Text;
using ClosedXML.Excel;
using Kirkenta.Helpers.Export;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>
    /// Fidelidad de importar/exportar: lo que se exporta se puede volver a importar igual,
    /// y las columnas que el usuario elige son las que se leen.
    /// </summary>
    public class ImportacionTests
    {
        // ===== ParserHelper =====

        [Theory]
        [InlineData("1234.50", 1234.50)]
        [InlineData("1,234.50", 1234.50)]
        [InlineData("1.234,50", 1234.50)]
        [InlineData("12,5", 12.5)]
        [InlineData("12,50", 12.5)]
        [InlineData("1,234", 1234)]
        [InlineData("0,500", 0.5)]
        [InlineData("1,234,567", 1234567)]
        [InlineData("1.234.567", 1234567)]
        [InlineData("L. 1,250.00", 1250)]
        [InlineData("Lps 99.99", 99.99)]
        [InlineData("$ 10", 10)]
        [InlineData("15%", 15)]
        [InlineData("(100)", -100)]
        [InlineData("-5.25", -5.25)]
        [InlineData("  42  ", 42)]
        public void ParsearDecimal_FormatosReales(string entrada, double esperado)
        {
            Assert.Equal((decimal)esperado, ParserHelper.ParsearDecimal(entrada));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abc")]
        [InlineData("N/A")]
        [InlineData("--")]
        public void ParsearDecimal_NoNumerico_DevuelveNull(string entrada)
        {
            Assert.Null(ParserHelper.ParsearDecimal(entrada));
        }

        [Theory]
        [InlineData("30", 30)]
        [InlineData("30.0", 30)]
        [InlineData("30 días", 30)]
        [InlineData("1,000", 1000)]
        public void ParsearInt_Valido(string entrada, int esperado)
        {
            Assert.Equal(esperado, ParserHelper.ParsearInt(entrada));
        }

        [Theory]
        [InlineData("30.5")]
        [InlineData("abc")]
        [InlineData("99999999999")]
        public void ParsearInt_Invalido_DevuelveNull(string entrada)
        {
            Assert.Null(ParserHelper.ParsearInt(entrada));
        }

        [Theory]
        [InlineData("Sí", true)]
        [InlineData("SI", true)]
        [InlineData("1", true)]
        [InlineData("1.0", true)]
        [InlineData("true", true)]
        [InlineData("Activo", true)]
        [InlineData("No", false)]
        [InlineData("0", false)]
        [InlineData("0.0", false)]
        [InlineData("false", false)]
        [InlineData("Inactivo", false)]
        public void ParsearBool_Reconoce(string entrada, bool esperado)
        {
            Assert.Equal(esperado, ParserHelper.ParsearBool(entrada));
        }

        [Theory]
        [InlineData("")]
        [InlineData("quizás")]
        [InlineData("2")]
        public void ParsearBool_NoReconocido_DevuelveNull(string entrada)
        {
            Assert.Null(ParserHelper.ParsearBool(entrada));
        }

        [Fact]
        public void ImportHelper_AvisaValoresIlegibles()
        {
            var r = new ImportResult();
            Assert.Null(ImportHelper.Decimal(r, 5, "Precio", "abc"));
            Assert.Null(ImportHelper.Booleano(r, 5, "Activo", "tal vez"));
            Assert.Equal("123", ImportHelper.Texto(r, 5, "Código", "12345", 3));
            Assert.Equal(3, r.Advertencias.Count);
            Assert.All(r.Advertencias, a => Assert.Equal(5, a.NumeroFila));
        }

        // ===== ColumnMapper / ImportWizardConfig =====

        private static ImportWizardConfig ConfigProveedores() => new()
        {
            Campos = new List<CampoMapeo>
            {
                new() { Key = "Nombre", Label = "Nombre", Requerido = true },
                new() { Key = "RazonSocial", Label = "Razón social" },
                new() { Key = "CodigoBarras", Label = "Código de barras" },
                new() { Key = "Telefono", Label = "Teléfono principal" },
                new() { Key = "TelefonoContacto", Label = "Teléfono del contacto" },
                new() { Key = "Activo", Label = "Activo (Sí/No)" },
            },
            AliasCampos = new Dictionary<string, string[]>
            {
                { "Nombre", new[] { "nombre", "razon", "razonsocial" } },
                { "RazonSocial", new[] { "razonsocial" } },
                { "CodigoBarras", new[] { "codigobarras", "barcode" } },
                { "Telefono", new[] { "telefono", "tel" } },
                { "TelefonoContacto", new[] { "telefonocontacto" } },
                { "Activo", new[] { "activo" } },
            }
        };

        [Fact]
        public void Detectar_UsaLaEtiquetaVisibleDelCampo()
        {
            var cfg = ConfigProveedores();
            var headers = new List<string> { "Nombre", "Código de barras", "Teléfono principal", "Teléfono del contacto", "Activo" };

            var mapa = ColumnMapper.Detectar(headers, cfg.AliasCampos);

            Assert.Equal("Código de barras", mapa["CodigoBarras"]);
            Assert.Equal("Teléfono principal", mapa["Telefono"]);
            Assert.Equal("Teléfono del contacto", mapa["TelefonoContacto"]);
            Assert.Equal("Activo", mapa["Activo"]);
        }

        [Fact]
        public void Detectar_RazonSocialAntesDeNombre_NoSeCruzan()
        {
            var cfg = ConfigProveedores();
            var headers = new List<string> { "Razón social", "Nombre" };

            var mapa = ColumnMapper.Detectar(headers, cfg.AliasCampos);

            Assert.Equal("Nombre", mapa["Nombre"]);
            Assert.Equal("Razón social", mapa["RazonSocial"]);
        }

        [Fact]
        public void Detectar_CadaColumnaSeAsignaSoloUnaVez()
        {
            var cfg = ConfigProveedores();
            var mapa = ColumnMapper.Detectar(new List<string> { "Teléfono" }, cfg.AliasCampos);
            Assert.Single(mapa.Values.Where(v => v != null));
        }

        [Fact]
        public void EtiquetaLimpia_QuitaParentesis()
        {
            Assert.Equal("Activo", ImportWizardConfig.EtiquetaLimpia("Activo (Sí/No)"));
            Assert.Equal("Porcentaje", ImportWizardConfig.EtiquetaLimpia("Porcentaje (%)"));
        }

        [Fact]
        public void Plantilla_TraeLasEtiquetasComoEncabezados_ConBom()
        {
            var bytes = ConfigProveedores().PlantillaCsv();
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());

            var archivo = CsvImporter.Read(new MemoryStream(
                bytes.Concat(Encoding.UTF8.GetBytes("X,Y,Z,1,2,3\r\n")).ToArray()));
            Assert.True(archivo.Ok);
            Assert.Equal(new[] { "Nombre", "Razón social", "Código de barras", "Teléfono principal", "Teléfono del contacto", "Activo" },
                archivo.Headers.ToArray());
        }

        [Fact]
        public void PlantillaXlsx_TraeUnaColumnaPorCampo()
        {
            var bytes = ConfigProveedores().PlantillaXlsx();
            var archivo = ExcelImporter.Read(new MemoryStream(bytes));

            Assert.True(archivo.Ok, archivo.Error);
            Assert.Equal(new[] { "Nombre", "Razón social", "Código de barras", "Teléfono principal", "Teléfono del contacto", "Activo" },
                archivo.Headers.ToArray());
            Assert.Empty(archivo.Rows);
        }

        [Fact]
        public void Csv_Exportado_LlevaLineaSepParaExcelEnEspanol()
        {
            var bytes = CsvExporter.Export(new[] { ClienteDificil() }, ExportColumns.Clientes());
            var texto = Encoding.UTF8.GetString(bytes);
            Assert.StartsWith("\uFEFFsep=,\r\n", texto);
        }

        // ===== CSV: exportar -> importar =====

        private static Cliente ClienteDificil() => new()
        {
            Codigo = "CLI-0001",
            Nombre = "Comercial \"La Esquina\", S. de R.L.",
            RTN = "0801-1990-123456",
            Direccion = "Col. Kennedy, bloque 5, casa 12",
            Telefono = "+504 9999-0000",
            Notas = "Línea 1\nLínea 2",
            LimiteCredito = 12345.5m,
            DiasCredito = 30,
            Activo = true
        };

        [Fact]
        public void Csv_ExportarImportar_ConservaComasComillasYSaltos()
        {
            var bytes = CsvExporter.Export(new[] { ClienteDificil() }, ExportColumns.Clientes());
            var archivo = CsvImporter.Read(new MemoryStream(bytes));

            Assert.True(archivo.Ok, archivo.Error);
            Assert.Single(archivo.Rows);
            var fila = archivo.Rows[0];
            Assert.Equal("Comercial \"La Esquina\", S. de R.L.", fila["Nombre"]);
            Assert.Equal("Col. Kennedy, bloque 5, casa 12", fila["Dirección"]);
            Assert.Equal("Línea 1\nLínea 2", fila["Notas"].Replace("\r\n", "\n"));
            Assert.Equal("12345.50", fila["Límite crédito"]);
            Assert.Equal(12345.5m, ParserHelper.ParsearDecimal(fila["Límite crédito"]));
            Assert.Equal(30, ParserHelper.ParsearInt(fila["Días crédito"]));
            Assert.True(ParserHelper.ParsearBool(fila["Activo"]));
        }

        [Fact]
        public void Csv_PuntoYComaYTabulador_SeDetectan()
        {
            var pc = CsvImporter.Read(new MemoryStream(Encoding.UTF8.GetBytes("Nombre;Precio\r\nA;1,5\r\n")));
            Assert.Equal(new[] { "Nombre", "Precio" }, pc.Headers.ToArray());
            Assert.Equal(1.5m, ParserHelper.ParsearDecimal(pc.Rows[0]["Precio"]));

            var tab = CsvImporter.Read(new MemoryStream(Encoding.UTF8.GetBytes("Nombre\tPrecio\r\nA\t2.5\r\n")));
            Assert.Equal(2.5m, ParserHelper.ParsearDecimal(tab.Rows[0]["Precio"]));
        }

        // ===== Excel: exportar -> importar =====

        [Fact]
        public void Excel_ExportarImportar_ConservaCeroIzquierdaYNumeros()
        {
            var producto = new Producto
            {
                SKU = "00123",
                Nombre = "Tornillo 1/4\"",
                CodigoBarras = "7501234567890",
                PrecioCompra = 10.25m,
                PrecioVenta = 1234.5m,
                Stock = 7.5m,
                UnidadMedida = "Unidad",
                Activo = false
            };
            var cols = ExportColumns.Productos(new Dictionary<int, string>(), new Dictionary<int, string>());

            var bytes = ExcelExporter.Export(new[] { producto }, cols, "Productos", "Listado de productos");
            var archivo = ExcelImporter.Read(new MemoryStream(bytes));

            Assert.True(archivo.Ok, archivo.Error);
            Assert.Single(archivo.Rows);
            var fila = archivo.Rows[0];
            Assert.Equal("00123", fila["SKU"]);
            Assert.Equal("7501234567890", fila["Código de barras"]);
            Assert.Equal("Tornillo 1/4\"", fila["Nombre"]);
            Assert.Equal(1234.5m, ParserHelper.ParsearDecimal(fila["Precio venta"]));
            Assert.Equal(7.5m, ParserHelper.ParsearDecimal(fila["Stock"]));
            Assert.False(ParserHelper.ParsearBool(fila["Activo"]));
        }

        [Fact]
        public void Excel_CeldasNumericas_SeLeenSinNotacionCientificaNiCultura()
        {
            using var ms = new MemoryStream();
            using (var wb = new XLWorkbook())
            {
                var ws = wb.AddWorksheet("Datos");
                ws.Cell(1, 1).Value = "Codigo";
                ws.Cell(1, 2).Value = "Impuesto";
                ws.Cell(1, 3).Value = "Precio";
                ws.Cell(1, 4).Value = "Fecha";
                ws.Cell(1, 5).Value = "Activo";

                ws.Cell(2, 1).Value = 123;
                ws.Cell(2, 1).Style.NumberFormat.Format = "00000";
                ws.Cell(2, 2).Value = 0.15;
                ws.Cell(2, 2).Style.NumberFormat.Format = "0%";
                ws.Cell(2, 3).Value = 1234.5;
                ws.Cell(2, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(2, 4).Value = new DateTime(2026, 10, 28);
                ws.Cell(2, 5).Value = true;

                ws.Cell(3, 1).Value = 7501234567890d;
                ws.Cell(3, 3).Value = 0.1;
                wb.SaveAs(ms);
            }
            ms.Position = 0;

            var archivo = ExcelImporter.Read(ms);

            Assert.True(archivo.Ok, archivo.Error);
            Assert.Equal(2, archivo.Rows.Count);
            Assert.Equal("00123", archivo.Rows[0]["Codigo"]);
            Assert.Equal("15", archivo.Rows[0]["Impuesto"]);
            Assert.Equal("1234.5", archivo.Rows[0]["Precio"]);
            Assert.Equal("2026-10-28", archivo.Rows[0]["Fecha"]);
            Assert.Equal("true", archivo.Rows[0]["Activo"]);
            Assert.Equal("7501234567890", archivo.Rows[1]["Codigo"]);
            Assert.Equal("0.1", archivo.Rows[1]["Precio"]);
        }

        [Fact]
        public void Excel_TituloEncimaDeLaTabla_SeDetectaElEncabezado()
        {
            var cols = ExportColumns.Clientes();
            var bytes = ExcelExporter.Export(new[] { ClienteDificil() }, cols, "Clientes", "Listado de clientes");
            var archivo = ExcelImporter.Read(new MemoryStream(bytes));

            Assert.Contains("Nombre", archivo.Headers);
            Assert.Contains("Razón social", archivo.Headers);
            Assert.Single(archivo.Rows);
        }

        // ===== JSON: exportar -> importar =====

        [Fact]
        public void Json_ExportarImportar_UsaLosMismosEncabezados()
        {
            var cols = ExportColumns.Clientes();
            var bytes = JsonExporter.Export(new[] { ClienteDificil() }, cols);
            var texto = Encoding.UTF8.GetString(bytes);

            // No filtra campos internos
            Assert.DoesNotContain("EmpresaId", texto);
            Assert.DoesNotContain("UsuarioCreoId", texto);

            var archivo = JsonImporter.Read(new MemoryStream(bytes));
            Assert.True(archivo.Ok, archivo.Error);
            Assert.Equal("CLI-0001", archivo.Rows[0]["Código"]);
            Assert.Equal(12345.5m, ParserHelper.ParsearDecimal(archivo.Rows[0]["Límite crédito"]));
            Assert.True(ParserHelper.ParsearBool(archivo.Rows[0]["Activo"]));
        }

        // ===== Lo exportado se auto-mapea 1 a 1 =====

        [Fact]
        public void ExportadoDeProductos_SeAutoMapeaCompleto()
        {
            var cols = ExportColumns.Productos(new Dictionary<int, string>(), new Dictionary<int, string>());
            var headers = cols.Select(c => c.Header).ToList();

            var cfg = new ImportWizardConfig
            {
                Campos = new List<CampoMapeo>
                {
                    new() { Key = "SKU", Label = "SKU" },
                    new() { Key = "Nombre", Label = "Nombre" },
                    new() { Key = "Descripcion", Label = "Descripción" },
                    new() { Key = "CodigoBarras", Label = "Código de barras" },
                    new() { Key = "Categoria", Label = "Categoría" },
                    new() { Key = "Impuesto", Label = "Impuesto" },
                    new() { Key = "UnidadMedida", Label = "Unidad de medida" },
                    new() { Key = "PrecioCompra", Label = "Precio compra" },
                    new() { Key = "PrecioVenta", Label = "Precio venta" },
                    new() { Key = "PrecioMayorista", Label = "Precio mayorista" },
                    new() { Key = "Stock", Label = "Stock" },
                    new() { Key = "StockMinimo", Label = "Stock mínimo" },
                    new() { Key = "Activo", Label = "Activo (Sí/No)" },
                },
                AliasCampos = new Dictionary<string, string[]>
                {
                    { "SKU", new[] { "sku", "codigo", "code" } },
                    { "CodigoBarras", new[] { "codigobarras", "barcode" } },
                    { "Nombre", new[] { "nombre", "producto" } },
                    { "Descripcion", new[] { "descripcion" } },
                    { "Categoria", new[] { "categoria" } },
                    { "Impuesto", new[] { "impuesto" } },
                    { "UnidadMedida", new[] { "unidadmedida", "unidad" } },
                    { "PrecioCompra", new[] { "preciocompra", "costo" } },
                    { "PrecioVenta", new[] { "precioventa", "precio" } },
                    { "PrecioMayorista", new[] { "preciomayorista" } },
                    { "Stock", new[] { "stock", "existencia" } },
                    { "StockMinimo", new[] { "stockminimo", "minimo" } },
                    { "Activo", new[] { "activo" } },
                }
            };

            var mapa = ColumnMapper.Detectar(headers, cfg.AliasCampos);

            Assert.Equal("SKU", mapa["SKU"]);
            Assert.Equal("Código de barras", mapa["CodigoBarras"]);
            Assert.Equal("Unidad de medida", mapa["UnidadMedida"]);
            Assert.Equal("Precio compra", mapa["PrecioCompra"]);
            Assert.Equal("Precio venta", mapa["PrecioVenta"]);
            Assert.Equal("Precio mayorista", mapa["PrecioMayorista"]);
            Assert.Equal("Stock", mapa["Stock"]);
            Assert.Equal("Stock mínimo", mapa["StockMinimo"]);
            Assert.All(mapa, par => Assert.NotNull(par.Value));
        }
    }
}
