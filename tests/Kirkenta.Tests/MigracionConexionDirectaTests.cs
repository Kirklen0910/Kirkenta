using Kirkenta.Helpers.Migracion;
using Xunit;

namespace Kirkenta.Tests
{
    public class MigracionConexionDirectaTests
    {
        [Theory]
        [InlineData("MySql", null, "ventas", "`ventas`")]
        [InlineData("MySql", null, "raro`nombre", "`raro``nombre`")]
        [InlineData("SqlServer", "dbo", "Clientes", "[dbo].[Clientes]")]
        [InlineData("SqlServer", null, "a]b", "[a]]b]")]
        [InlineData("PostgreSql", "public", "productos", "\"public\".\"productos\"")]
        [InlineData("Firebird", null, "CLIENTES", "\"CLIENTES\"")]
        [InlineData("Access", null, "Mis Clientes", "[Mis Clientes]")]
        public void Citar_EscapaElNombreSegunElMotor(string motor, string? esquema, string tabla, string esperado)
            => Assert.Equal(esperado, MigracionConexionDirecta.Citar(motor, esquema, tabla));

        [Fact]
        public void ServidorInexistente_DaUnMensajeClaro()
        {
            var r = MigracionConexionDirecta.Leer(new DatosConexion { Motor = "MySql", Servidor = "127.0.0.1", Puerto = 1, BaseDatos = "x", Usuario = "u", Clave = "c" });
            Assert.Empty(r.Tablas);
            Assert.Contains("No se pudo", Assert.Single(r.Avisos));
        }

        /// <summary>
        /// Lectura real de una base MySQL (solo SELECT). Corre solo si están las variables KK_MIG_MYSQL_BD,
        /// KK_MIG_MYSQL_USUARIO y KK_MIG_MYSQL_CLAVE; si no, no hace nada.
        /// </summary>
        [Fact]
        public void MySqlReal_LeeTodasLasTablasYSeAnalizan()
        {
            var bd = Environment.GetEnvironmentVariable("KK_MIG_MYSQL_BD");
            if (string.IsNullOrEmpty(bd)) return;
            var d = new DatosConexion
            {
                Motor = "MySql", Servidor = "localhost", BaseDatos = bd,
                Usuario = Environment.GetEnvironmentVariable("KK_MIG_MYSQL_USUARIO") ?? "root",
                Clave = Environment.GetEnvironmentVariable("KK_MIG_MYSQL_CLAVE") ?? ""
            };
            var r = MigracionConexionDirecta.Leer(d);
            Assert.True(r.Tablas.Count > 0, string.Join(" ", r.Avisos));

            var plan = MigracionAutomaticaHelper.Planificar(r.Tablas);
            var destinos = plan.Where(p => p.Destino != "").Select(p => p.TablaNombre + "→" + p.Destino).ToList();
            Console.WriteLine($"{r.Tablas.Count} tablas, {r.Tablas.Sum(t => t.Rows.Count)} filas. Detectadas: " + string.Join(", ", destinos));
            Assert.Contains(plan, p => p.Destino == "Productos");
            Assert.Contains(plan, p => p.Destino == "Clientes");
        }
    }
}
