using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.RegularExpressions;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Crea (solo si faltan) las tablas de los módulos nuevos: Activos fijos, Producción,
    /// Caja chica, Toma física y Asientos contables. Usa el script de creación que genera EF
    /// a partir del modelo, pero ejecuta ÚNICAMENTE los CREATE TABLE / CREATE INDEX de estas
    /// tablas. Es idempotente y nunca toca tablas existentes. Corre al arrancar.
    /// </summary>
    public static class EsquemaModulosNuevos
    {
        public static readonly string[] Tablas =
        {
            "ActivosFijos", "DepreciacionesActivo", "MantenimientosActivo",
            "OrdenesProduccion", "DetallesOrdenProduccion", "InspeccionesCalidad",
            "GastosCajaChica", "ConteosCierreCaja", "VentasOffline", "CatalogoWebConfig", "PedidosWeb",
            "TomasFisicas", "DetallesTomaFisica",
            "AsientosContables", "DetallesAsiento"
        };

        public static void Aplicar(ApplicationDbContext db)
        {
            var conexion = db.Database.GetDbConnection();
            var abrir = conexion.State != ConnectionState.Open;
            if (abrir) conexion.Open();
            try
            {
                var existentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE()";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read()) existentes.Add(rd.GetString(0));
                }

                var faltantes = Tablas.Where(t => !existentes.Contains(t)).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (faltantes.Count == 0) return;

                var script = db.Database.GenerateCreateScript();
                var sentencias = Regex.Split(script, @";\s*\r?\n")
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();

                foreach (var sql in sentencias)
                {
                    var m = Regex.Match(sql, @"^CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?`(?<t>[^`]+)`", RegexOptions.IgnoreCase);
                    if (!m.Success)
                        m = Regex.Match(sql, @"^CREATE\s+(?:UNIQUE\s+)?INDEX\s+`[^`]+`\s+ON\s+`(?<t>[^`]+)`", RegexOptions.IgnoreCase);
                    if (!m.Success) continue;

                    if (!faltantes.Contains(m.Groups["t"].Value)) continue;

                    using var cmd = conexion.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (abrir) conexion.Close();
            }
        }
    }
}
