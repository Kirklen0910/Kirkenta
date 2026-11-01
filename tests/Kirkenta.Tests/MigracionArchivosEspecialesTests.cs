using System.Data.OleDb;
using Kirkenta.Helpers.Migracion;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>
    /// Lectura real de Access y de respaldos de SQL Server. Solo corren donde está el motor
    /// (Access Database Engine / SQL Server LocalDB); en otro equipo no hacen nada.
    /// </summary>
    public class MigracionArchivosEspecialesTests
    {
        [Fact]
        public void Access_LeeTodasLasTablas()
        {
            if (!OperatingSystem.IsWindows()) return;
            var tipo = Type.GetTypeFromProgID("ADOX.Catalog");
            if (tipo == null) return;
            var dir = Path.Combine(Path.GetTempPath(), "kk-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var ruta = Path.Combine(dir, "sistema_viejo.accdb");
            try
            {
                var cs = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={ruta}";
                try
                {
                    dynamic cat = Activator.CreateInstance(tipo)!;
                    cat.Create(cs);
                    cat.ActiveConnection.Close();
                }
                catch { return; }   // sin motor de Access de 64 bits

                using (var c = new OleDbConnection(cs))
                {
                    c.Open();
                    foreach (var sql in new[]
                    {
                        "CREATE TABLE Clientes (Codigo TEXT(10), Nombre TEXT(100), LimiteCredito CURRENCY, FechaAlta DATETIME)",
                        "INSERT INTO Clientes VALUES ('C1', 'Ana López', 5000, #2023-01-15#)",
                        "INSERT INTO Clientes VALUES ('C2', 'Ferretería Sol', 0, #2024-06-01#)",
                        "CREATE TABLE Productos (Codigo TEXT(10), Descripcion TEXT(100), Precio CURRENCY, Existencia DOUBLE)",
                        "INSERT INTO Productos VALUES ('P1', 'Clavo 2\"', 5.5, 100)"
                    })
                    {
                        using var cmd = new OleDbCommand(sql, c);
                        cmd.ExecuteNonQuery();
                    }
                }

                ResultadoMigracionLectura r;
                using (var f = File.OpenRead(ruta)) r = MigracionLector.Leer("sistema_viejo.accdb", f);

                Assert.True(r.Tablas.Count == 2, string.Join(" ", r.Avisos));
                var cli = r.Tablas.Single(t => t.Nombre == "Clientes");
                Assert.Equal(2, cli.Rows.Count);
                Assert.Equal("Ana López", cli.Rows[0]["Nombre"]);
                Assert.Equal("5000", cli.Rows[0]["LimiteCredito"]);
                Assert.Equal("2023-01-15", cli.Rows[0]["FechaAlta"]);

                var plan = MigracionAutomaticaHelper.Planificar(r.Tablas);
                Assert.Equal("Clientes", plan.Single(p => p.TablaNombre == "Clientes").Destino);
                Assert.Equal("Productos", plan.Single(p => p.TablaNombre == "Productos").Destino);
                Console.WriteLine("[migracion] Access leído de verdad");
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }

        [Fact]
        public void RespaldoBak_SeRestauraLeeYBorra()
        {
            if (!OperatingSystem.IsWindows()) return;
            var master = new SqlConnectionStringBuilder { DataSource = @"(localdb)\MSSQLLocalDB", InitialCatalog = "master", IntegratedSecurity = true, ConnectTimeout = 10 }.ConnectionString;
            var nombreBd = "KkTestOrigen_" + Guid.NewGuid().ToString("N")[..8];
            var dir = Path.Combine(Path.GetTempPath(), "kk-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var bak = Path.Combine(dir, "respaldo.bak");
            try
            {
                try
                {
                    using var c = new SqlConnection(master);
                    c.Open();
                    Ejecutar(c, $"CREATE DATABASE [{nombreBd}] ON (NAME = d, FILENAME = '{Path.Combine(dir, nombreBd + ".mdf")}') LOG ON (NAME = l, FILENAME = '{Path.Combine(dir, nombreBd + ".ldf")}')");
                    Ejecutar(c, $"USE [{nombreBd}]; CREATE TABLE dbo.proveedores (id int, nombre nvarchar(100), rtn nvarchar(20), contacto nvarchar(60)); " +
                                "INSERT INTO dbo.proveedores VALUES (1, N'Distribuidora Ñandú', '08019999000011', N'Pepe');" +
                                "CREATE TABLE dbo.cuentas_por_cobrar (factura nvarchar(20), cliente nvarchar(100), saldo decimal(18,2), fecha date);" +
                                "INSERT INTO dbo.cuentas_por_cobrar VALUES ('F-9', N'Ana', 1250.75, '2025-03-01');");
                    Ejecutar(c, $"USE master; BACKUP DATABASE [{nombreBd}] TO DISK = '{bak}' WITH INIT");
                    Ejecutar(c, $"ALTER DATABASE [{nombreBd}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{nombreBd}]");
                }
                catch (SqlException) { return; }   // sin LocalDB

                ResultadoMigracionLectura r;
                using (var f = File.OpenRead(bak)) r = MigracionLector.Leer("respaldo.bak", f);

                Assert.True(r.Tablas.Count == 2, string.Join(" ", r.Avisos));
                Assert.Equal("Distribuidora Ñandú", r.Tablas.Single(t => t.Nombre == "proveedores").Rows[0]["nombre"]);
                Assert.Equal("1250.75", r.Tablas.Single(t => t.Nombre == "cuentas_por_cobrar").Rows[0]["saldo"]);
                Assert.Contains(r.Avisos, a => a.Contains("se restauró temporalmente"));

                // la base temporal ya no existe
                using var c2 = new SqlConnection(master);
                c2.Open();
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name LIKE 'KirkentaMig_%'", c2);
                Assert.Equal(0, (int)cmd.ExecuteScalar()!);
                Console.WriteLine("[migracion] .bak restaurado, leído y borrado de verdad");
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }

        private static void Ejecutar(SqlConnection c, string sql)
        {
            using var cmd = new SqlCommand(sql, c) { CommandTimeout = 120 };
            cmd.ExecuteNonQuery();
        }

        [Fact]
        public void Firebird_SinServidor_DaUnMensajeClaro()
        {
            var r = MigracionLector.Leer("viejo.fdb", new MemoryStream(new byte[] { 1, 2, 3 }));
            Assert.Empty(r.Tablas);
            Assert.Contains("Firebird", Assert.Single(r.Avisos));
        }
    }
}
