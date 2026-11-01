using Kirkenta.Helpers;
using Xunit;

namespace Kirkenta.Tests
{
    public class ActualizacionBDTests
    {
        [Theory]
        [InlineData("int NOT NULL", "int NOT NULL DEFAULT 0")]
        [InlineData("decimal(18,2) NOT NULL", "decimal(18,2) NOT NULL DEFAULT 0")]
        [InlineData("tinyint(1) NOT NULL", "tinyint(1) NOT NULL DEFAULT 0")]
        [InlineData("bit NOT NULL", "bit NOT NULL DEFAULT b'0'")]
        [InlineData("varchar(100) CHARACTER SET utf8mb4 NOT NULL", "varchar(100) CHARACTER SET utf8mb4 NOT NULL DEFAULT ''")]
        [InlineData("char(36) NOT NULL", "char(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'")]
        [InlineData("longtext NOT NULL", "longtext NOT NULL DEFAULT ('')")]
        [InlineData("datetime(6) NOT NULL", "datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)")]
        [InlineData("date NOT NULL", "date NOT NULL DEFAULT (CURRENT_DATE)")]
        [InlineData("int NULL", "int NULL")]
        [InlineData("varchar(10) NOT NULL DEFAULT 'X'", "varchar(10) NOT NULL DEFAULT 'X'")]
        public void ConDefaultSeguro_AgregaDefaultSoloSiHaceFalta(string entrada, string esperado)
            => Assert.Equal(esperado, ActualizacionBD.ConDefaultSeguro(entrada));

        [Fact]
        public void ConDefaultSeguro_TipoDesconocido_DevuelveNull()
        {
            Assert.Null(ActualizacionBD.ConDefaultSeguro("enum('a','b') NOT NULL"));
            Assert.Null(ActualizacionBD.ConDefaultSeguro(""));
        }

        [Fact]
        public void ExtraerDefinicionesColumnas_LeeColumnasYSaltaConstraints()
        {
            var sql = "CREATE TABLE `Clientes` (\r\n" +
                      "    `Id` int NOT NULL AUTO_INCREMENT,\r\n" +
                      "    `Nombre` varchar(100) CHARACTER SET utf8mb4 NOT NULL,\r\n" +
                      "    `Limite` decimal(18,2) NULL,\r\n" +
                      "    CONSTRAINT `PK_Clientes` PRIMARY KEY (`Id`)\r\n" +
                      ") CHARACTER SET=utf8mb4";
            var d = ActualizacionBD.ExtraerDefinicionesColumnas(sql);
            Assert.Equal(3, d.Count);
            Assert.Equal("int NOT NULL AUTO_INCREMENT", d["Id"]);
            Assert.Equal("varchar(100) CHARACTER SET utf8mb4 NOT NULL", d["Nombre"]);
            Assert.Equal("decimal(18,2) NULL", d["Limite"]);
        }

        [Theory]
        [InlineData("ALTER TABLE `Clientes` ADD COLUMN `X` int NULL", true)]
        [InlineData("CREATE TABLE `Nueva` (`Id` int)", true)]
        [InlineData("CREATE INDEX `IX_A` ON `Clientes` (`X`)", true)]
        [InlineData("INSERT IGNORE INTO `Metodos` (`Nombre`) VALUES ('Efectivo')", true)]
        [InlineData("UPDATE `Clientes` SET `Estado`='Activo' WHERE `Estado`=''", true)]
        [InlineData("UPDATE `Clientes` SET `Estado`='Activo'", false)]
        [InlineData("DROP TABLE `Clientes`", false)]
        [InlineData("ALTER TABLE `Clientes` DROP COLUMN `X`", false)]
        [InlineData("ALTER TABLE `Clientes` MODIFY COLUMN `X` int", false)]
        [InlineData("ALTER TABLE `Clientes` RENAME COLUMN `X` TO `Y`", false)]
        [InlineData("DELETE FROM `Clientes` WHERE 1=1", false)]
        [InlineData("TRUNCATE TABLE `Clientes`", false)]
        [InlineData("UPDATE `A` SET `x`=1 WHERE 1=1; DROP TABLE `A`", false)]
        [InlineData("", false)]
        public void EsSqlAditivo_AceptaSoloLoSeguro(string sql, bool esperado)
            => Assert.Equal(esperado, ActualizacionBD.EsSqlAditivo(sql));

        [Fact]
        public void PasosManuales_TienenIdUnicoYSqlAditivo()
        {
            var ids = ActualizacionBD.PasosManuales.Select(p => p.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            foreach (var p in ActualizacionBD.PasosManuales)
            {
                Assert.False(string.IsNullOrWhiteSpace(p.Id));
                Assert.True(p.Id.Length <= 120);
                Assert.NotEmpty(p.Sql);
                Assert.All(p.Sql, s => Assert.True(ActualizacionBD.EsSqlAditivo(s), "No aditivo en paso " + p.Id + ": " + s));
            }
        }

        [Fact]
        public void ValorNeutro_RespaldoViejoSinColumnaObligatoria()
        {
            Assert.Equal(0, Kirkenta.Services.RespaldoSistemaService.ValorNeutro("int", "int"));
            Assert.Equal("", Kirkenta.Services.RespaldoSistemaService.ValorNeutro("varchar", "varchar(50)"));
            Assert.Equal("00000000-0000-0000-0000-000000000000", Kirkenta.Services.RespaldoSistemaService.ValorNeutro("char", "char(36)"));
            Assert.IsType<DateTime>(Kirkenta.Services.RespaldoSistemaService.ValorNeutro("datetime", "datetime(6)"));
            Assert.Null(Kirkenta.Services.RespaldoSistemaService.ValorNeutro("enum", "enum('a','b')"));
        }
    }
}
