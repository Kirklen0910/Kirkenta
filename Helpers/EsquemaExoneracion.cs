using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Agrega las columnas de exoneración de ISV si faltan (idempotente) y registra la migración
    /// 20261025180000_ExoneracionClientes en __EFMigrationsHistory, para que la app no falle aunque
    /// no se haya corrido "dotnet ef database update" y que ese comando después no intente repetirla.
    /// Corre al arrancar, antes de cualquier consulta.
    /// </summary>
    public static class EsquemaExoneracion
    {
        private const string MigracionId = "20261025180000_ExoneracionClientes";

        private static readonly (string Tabla, string Columna, string Definicion)[] Columnas =
        {
            ("Clientes", "EsExonerado", "tinyint(1) NOT NULL DEFAULT 0"),
            ("Clientes", "ExoneradoConstancia", "varchar(50) CHARACTER SET utf8mb4 NULL"),
            ("Clientes", "ExoneradoOrdenCompraExenta", "varchar(50) CHARACTER SET utf8mb4 NULL"),
            ("Clientes", "ExoneradoRegistroSag", "varchar(50) CHARACTER SET utf8mb4 NULL"),
            ("Ventas", "EsExonerada", "tinyint(1) NOT NULL DEFAULT 0"),
            ("Facturas", "EsExonerada", "tinyint(1) NOT NULL DEFAULT 0"),
            ("Facturas", "ExoneradoConstancia", "varchar(50) CHARACTER SET utf8mb4 NULL"),
            ("Facturas", "ExoneradoOrdenCompraExenta", "varchar(50) CHARACTER SET utf8mb4 NULL"),
            ("Facturas", "ExoneradoRegistroSag", "varchar(50) CHARACTER SET utf8mb4 NULL"),
        };

        public static void Aplicar(ApplicationDbContext db)
        {
            var conexion = db.Database.GetDbConnection();
            var abrir = conexion.State != ConnectionState.Open;
            if (abrir) conexion.Open();
            try
            {
                foreach (var (tabla, columna, definicion) in Columnas)
                {
                    if (!Existe(conexion, "COLUMNS", tabla, columna))
                        Ejecutar(conexion, $"ALTER TABLE `{tabla}` ADD COLUMN `{columna}` {definicion}");
                }

                if (Existe(conexion, "TABLES", "__EFMigrationsHistory", null))
                {
                    Ejecutar(conexion,
                        $"INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('{MigracionId}', '9.0.0')");
                }
            }
            finally
            {
                if (abrir) conexion.Close();
            }
        }

        private static bool Existe(System.Data.Common.DbConnection conexion, string vista, string tabla, string? columna)
        {
            using var cmd = conexion.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM information_schema.{vista} WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tabla}'"
                + (columna != null ? $" AND COLUMN_NAME = '{columna}'" : "");
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void Ejecutar(System.Data.Common.DbConnection conexion, string sql)
        {
            using var cmd = conexion.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}
