using System.Text;
using Kirkenta.Helpers.Import;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Kirkenta.Tests
{
    public class ImportacionArchivosDificilesTests
    {
        private static ImportWizardHelper.ResultadoLectura Subir(string nombre, byte[] bytes) =>
            ImportWizardHelper.ProcesarArchivo(new FormFile(new MemoryStream(bytes), 0, bytes.Length, "Archivo", nombre));

        private static byte[] Ansi(string s)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252).GetBytes(s);
        }

        [Fact]
        public void CsvDeExcelEnEspanol_Ansi_ConservaEnesTildesYEuro()
        {
            var r = Subir("productos.csv", Ansi("Nombre;Descripción;Precio\r\nNiño Jesús;Caja “premium” – 5 €;12,50\r\n"));
            Assert.True(r.Ok, r.Error);
            Assert.Equal("Descripción", r.Headers[1]);
            Assert.Equal("Niño Jesús", r.Rows[0]["Nombre"]);
            Assert.Equal("Caja “premium” – 5 €", r.Rows[0]["Descripción"]);
        }

        [Fact]
        public void CsvConTituloEncimaDeLaTabla_EncuentraElEncabezado()
        {
            var csv = "Reporte de productos\r\nGenerado el 01/11/2026\r\n\r\nCodigo,Nombre,Precio\r\nA1,Martillo,12.50\r\nA2,Taladro,95.00\r\n";
            var r = Subir("reporte.csv", Encoding.UTF8.GetBytes(csv));
            Assert.True(r.Ok, r.Error);
            Assert.Equal(new[] { "Codigo", "Nombre", "Precio" }, r.Headers);
            Assert.Equal(2, r.Rows.Count);
        }

        [Fact]
        public void ColumnaConComaDecimal_ElPuntoEsDeMiles()
        {
            var r = Subir("precios.csv", Encoding.UTF8.GetBytes("Codigo;Precio\nA1;1.234\nA2;12,50\nA3;1.250,75\n"));
            Assert.True(r.Ok, r.Error);
            Assert.Equal(1234m, ParserHelper.ParsearDecimal(r.Rows[0]["Precio"]));
            Assert.Equal(12.50m, ParserHelper.ParsearDecimal(r.Rows[1]["Precio"]));
            Assert.Equal(1250.75m, ParserHelper.ParsearDecimal(r.Rows[2]["Precio"]));
        }

        [Fact]
        public void Codigos_NoSeTocanAunqueParezcanNumeros()
        {
            var r = Subir("codigos.csv", Encoding.UTF8.GetBytes("Codigo,Nombre\n001.234,Uno\n002.500,Dos\n"));
            Assert.True(r.Ok, r.Error);
            Assert.Equal("001.234", r.Rows[0]["Codigo"]);
        }

        [Fact]
        public void ArchivoGrande_SinTopeDeFilas_YSeGuardaFueraDeLaSesion()
        {
            var sb = new StringBuilder("Nombre,Precio\n");
            for (int i = 0; i < 20000; i++) sb.Append("Producto ").Append(i).Append(",").Append(i % 100).Append(".50\n");
            var r = Subir("grande.csv", Encoding.UTF8.GetBytes(sb.ToString()));
            Assert.True(r.Ok, r.Error);
            Assert.Equal(20000, r.Rows.Count);

            var sesion = new SesionPrueba();
            var cfg = new ImportWizardConfig { SessionKey = "Prueba" };
            ImportWizardHelper.GuardarEnSesion(sesion, cfg, r.Headers, r.Rows);
            Assert.True(sesion.GetString(cfg.SessionRowsKey)!.Length < 100);      // en la sesión solo queda la referencia
            var leido = ImportWizardHelper.RecuperarDeSesion(sesion, cfg);
            Assert.NotNull(leido);
            Assert.Equal(20000, leido!.Value.Rows.Count);
            Assert.Equal("Producto 19999", leido.Value.Rows[^1]["Nombre"]);

            ImportWizardHelper.LimpiarSesion(sesion, cfg);
            Assert.Null(ImportWizardHelper.RecuperarDeSesion(sesion, cfg));
        }

        [Fact]
        public void ExcelViejoXls_YaNoSeRechaza()
        {
            var r = Subir("viejo.xls", new byte[] { 1, 2, 3 });
            Assert.False(r.Ok);
            Assert.DoesNotContain("no es compatible", r.Error);           // se intenta leer; si está dañado lo dice
        }

        private class SesionPrueba : ISession
        {
            private readonly Dictionary<string, byte[]> _d = new();
            public bool IsAvailable => true;
            public string Id => "prueba";
            public IEnumerable<string> Keys => _d.Keys;
            public void Clear() => _d.Clear();
            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void Remove(string key) => _d.Remove(key);
            public void Set(string key, byte[] value) => _d[key] = value;
            public bool TryGetValue(string key, out byte[] value) => _d.TryGetValue(key, out value!);
        }
    }
}
