using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Sube a 4 decimales las cantidades, el stock y los costos que estaban en 2. El POS y las conversiones venden
    /// fracciones (0.125 lb) y el kardex guarda 4 decimales: con 2 en el stock, la base redondeaba cada venta y el
    /// stock se iba separando del kardex. Solo amplía la columna (no cambia ningún dato) y no vuelve a actuar
    /// cuando la columna ya tiene 4 decimales o más. Corre al arrancar, antes de cualquier consulta.
    /// </summary>
    public static class EsquemaPrecisionCantidades
    {
        private static readonly (string Tabla, string Columna)[] Columnas =
        {
            ("Productos", "Stock"), ("Productos", "StockMinimo"), ("Productos", "PrecioCompra"),
            ("DetalleVentas", "Cantidad"), ("DetalleFacturas", "Cantidad"),
            ("DetalleCotizaciones", "Cantidad"), ("DetallePedidos", "Cantidad"),
            ("DetalleDevoluciones", "Cantidad"), ("DetalleDevoluciones", "PrecioUnitario"),
            ("DetalleDevolucionesProveedor", "Cantidad"), ("DetalleDevolucionesProveedor", "PrecioUnitario"),
            ("DetalleOrdenesCompra", "Cantidad"), ("DetalleOrdenesCompra", "CantidadRecibida"), ("DetalleOrdenesCompra", "PrecioUnitario"),
            ("BajasInventario", "Cantidad"),
        };

        public static int Aplicar(ApplicationDbContext db)
        {
            var conexion = db.Database.GetDbConnection();
            var abrir = conexion.State != ConnectionState.Open;
            if (abrir) conexion.Open();
            var cambios = 0;
            try
            {
                foreach (var (tabla, columna) in Columnas)
                {
                    var info = Leer(conexion, tabla, columna);
                    if (info == null || info.Value.Escala >= 4) continue;

                    var nulo = info.Value.Nulo ? "NULL" : "NOT NULL";
                    var porDefecto = info.Value.PorDefecto != null ? $" DEFAULT {info.Value.PorDefecto}" : "";
                    // 20 dígitos: conserva los 16 enteros que tenía la columna y agrega 2 decimales
                    Ejecutar(conexion, $"ALTER TABLE `{tabla}` MODIFY COLUMN `{columna}` decimal(20,4) {nulo}{porDefecto}");
                    cambios++;
                }
            }
            finally
            {
                if (abrir) conexion.Close();
            }
            return cambios;
        }

        private static (int Escala, bool Nulo, string? PorDefecto)? Leer(DbConnection conexion, string tabla, string columna)
        {
            using var cmd = conexion.CreateCommand();
            cmd.CommandText = "SELECT NUMERIC_SCALE, IS_NULLABLE, COLUMN_DEFAULT FROM information_schema.COLUMNS " +
                              $"WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tabla}' AND COLUMN_NAME = '{columna}' AND DATA_TYPE = 'decimal'";
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            var defecto = r.IsDBNull(2) ? null : r.GetValue(2)?.ToString();
            // el valor por defecto de una columna decimal solo puede ser un número
            if (defecto != null && !decimal.TryParse(defecto, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _))
                defecto = null;
            return (Convert.ToInt32(r.GetValue(0)), r.GetString(1) == "YES", defecto);
        }

        private static void Ejecutar(DbConnection conexion, string sql)
        {
            using var cmd = conexion.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}
