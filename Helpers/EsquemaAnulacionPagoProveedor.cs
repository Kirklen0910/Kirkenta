using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Agrega las columnas de anulación de PagosProveedor si faltan (idempotente) y registra la migración
    /// 20261105120000_AnulacionPagoProveedor en __EFMigrationsHistory, para que la app no falle aunque
    /// no se haya corrido "dotnet ef database update" y que ese comando después no intente repetirla.
    /// Corre al arrancar, antes de cualquier consulta. Los pagos que ya existen quedan en Estado 'Activo'.
    /// </summary>
    public static class EsquemaAnulacionPagoProveedor
    {
        private const string MigracionId = "20261105120000_AnulacionPagoProveedor";

        private static readonly (string Tabla, string Columna, string Definicion)[] Columnas =
        {
            ("PagosProveedor", "Estado", "varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Activo'"),
            ("PagosProveedor", "MotivoAnulacion", "longtext CHARACTER SET utf8mb4 NULL"),
            ("PagosProveedor", "FechaAnulacion", "datetime(6) NULL"),
            ("PagosProveedor", "UsuarioAnuloId", "int NULL"),
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
