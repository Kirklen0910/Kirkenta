using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Data;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Compara el modelo de EF contra las tablas/columnas reales de MySQL y devuelve lo que falta.
    /// Sirve para detectar al arrancar (y no cuando alguien abre una pantalla) que a la base le
    /// falta una migración. Solo lee, no modifica nada.
    /// </summary>
    public static class DiagnosticoEsquema
    {
        public static List<string> BuscarFaltantes(ApplicationDbContext db)
        {
            var faltantes = new List<string>();
            var conexion = db.Database.GetDbConnection();
            var abrir = conexion.State != ConnectionState.Open;
            if (abrir) conexion.Open();
            try
            {
                var existentes = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = "SELECT TABLE_NAME, COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        var t = rd.GetString(0);
                        if (!existentes.TryGetValue(t, out var set))
                            existentes[t] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        set.Add(rd.GetString(1));
                    }
                }

                foreach (var entidad in db.Model.GetEntityTypes())
                {
                    var tabla = entidad.GetTableName();
                    if (tabla == null) continue;

                    if (!existentes.TryGetValue(tabla, out var columnas))
                    {
                        faltantes.Add($"Tabla {tabla} (falta completa)");
                        continue;
                    }

                    var id = StoreObjectIdentifier.Table(tabla, entidad.GetSchema());
                    foreach (var prop in entidad.GetProperties())
                    {
                        var col = prop.GetColumnName(id);
                        if (col != null && !columnas.Contains(col))
                            faltantes.Add($"{tabla}.{col}");
                    }
                }
            }
            finally
            {
                if (abrir) conexion.Close();
            }
            return faltantes;
        }
    }
}
