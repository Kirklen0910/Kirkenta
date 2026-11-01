using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Agrega CuentasFinancieras.RowVersion (token de concurrencia) si falta. Idempotente.
    /// Sin esto, dos ventas simultáneas en la misma caja podían pisar el saldo y "perder" un movimiento.
    /// </summary>
    public static class EsquemaConcurrenciaCuentas
    {
        public static void Aplicar(ApplicationDbContext db)
        {
            var conexion = db.Database.GetDbConnection();
            var abrir = conexion.State != ConnectionState.Open;
            if (abrir) conexion.Open();
            try
            {
                using var existe = conexion.CreateCommand();
                existe.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CuentasFinancieras'";
                if (Convert.ToInt32(existe.ExecuteScalar()) == 0) return;

                using var col = conexion.CreateCommand();
                col.CommandText = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CuentasFinancieras' AND COLUMN_NAME = 'RowVersion'";
                if (Convert.ToInt32(col.ExecuteScalar()) > 0) return;

                using var alter = conexion.CreateCommand();
                alter.CommandText = "ALTER TABLE `CuentasFinancieras` ADD COLUMN `RowVersion` timestamp(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)";
                alter.ExecuteNonQuery();
            }
            finally
            {
                if (abrir) conexion.Close();
            }
        }
    }
}
