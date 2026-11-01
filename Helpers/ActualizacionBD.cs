using Kirkenta.Data;
using Kirkenta.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Un ajuste manual de datos para una actualización (rellenar un valor, sembrar un registro, etc.).
    /// Se ejecuta UNA sola vez por base de datos y queda anotado en la tabla __KirkentaEsquema.
    /// Debe ser ADITIVO e IDEMPOTENTE (poder correr dos veces sin dañar nada): ver EsSqlAditivo.
    /// </summary>
    public class PasoManual
    {
        public string Id { get; init; } = "";
        public string Descripcion { get; init; } = "";
        public string[] Sql { get; init; } = Array.Empty<string>();
    }

    public class ResultadoActualizacionBD
    {
        public List<string> TablasCreadas { get; } = new();
        public List<string> ColumnasAgregadas { get; } = new();
        public List<string> IndicesCreados { get; } = new();
        public List<string> PasosAplicados { get; } = new();
        public List<string> Errores { get; } = new();
        public string? RespaldoPrevio { get; set; }
        public int Cambios => TablasCreadas.Count + ColumnasAgregadas.Count + IndicesCreados.Count + PasosAplicados.Count;
    }

    /// <summary>
    /// ACTUALIZADOR AUTOMÁTICO DE BASE DE DATOS (solo aditivo, nunca pierde información).
    ///
    /// Al arrancar compara el modelo de EF contra la base real y:
    ///   1) Si hay algo pendiente y la base ya tiene datos, crea primero un respaldo "previo-actualizacion"
    ///      (si ese respaldo falla, NO toca nada y lo reintenta en el próximo arranque).
    ///   2) Crea las tablas que faltan.
    ///   3) Agrega las columnas que faltan. Si la columna es NOT NULL y no trae DEFAULT, le pone uno seguro
    ///      (0, '', fecha actual…) para que las filas existentes y los respaldos VIEJOS sigan cargando.
    ///   4) Crea los índices (no únicos) de las columnas nuevas.
    ///   5) Ejecuta los PasosManuales que aún no se hayan aplicado.
    ///
    /// JAMÁS ejecuta DROP, RENAME, MODIFY, CHANGE, TRUNCATE ni DELETE. Renombrar/borrar/cambiar tipos se
    /// hace a mano y con cuidado (ver tools\LEEME-ACTUALIZACIONES.md).
    /// </summary>
    public static class ActualizacionBD
    {
        /// <summary>
        /// Lista de ajustes manuales. AGREGAR AL FINAL, nunca editar ni borrar uno ya publicado.
        /// Ejemplo:
        ///   new PasoManual {
        ///       Id = "2026-11-01-001-estado-por-defecto",
        ///       Descripcion = "Pone Estado='Activo' a los registros viejos",
        ///       Sql = new[] { "UPDATE `Clientes` SET `Estado` = 'Activo' WHERE `Estado` = ''" }
        ///   }
        /// </summary>
        public static readonly List<PasoManual> PasosManuales = new()
        {
        };

        private static readonly Regex NombreValido = new("^[A-Za-z0-9_]{1,64}$", RegexOptions.Compiled);

        private const string SqlTablaRegistro =
            "CREATE TABLE IF NOT EXISTS `__KirkentaEsquema` (" +
            "`Id` varchar(120) NOT NULL, `Descripcion` varchar(300) NULL, `AplicadoEn` datetime(6) NOT NULL, " +
            "PRIMARY KEY (`Id`)) CHARACTER SET=utf8mb4";

        // ------------------------------------------------------------------
        // Punto de entrada
        // ------------------------------------------------------------------
        public static ResultadoActualizacionBD Aplicar(ApplicationDbContext db, RespaldoSistemaService? respaldo = null, bool respaldarAntes = true)
        {
            var res = new ResultadoActualizacionBD();
            var cn = db.Database.GetDbConnection();

            // ---- Fase 1: detectar (solo lectura, salvo la tabla de registro) ----
            bool hayDatos;
            List<(string Tabla, string Columna, bool EsPk)> colsFaltantes;
            HashSet<string> tablasFaltantes;
            List<PasoManual> pasosPendientes;

            var abrir = cn.State != ConnectionState.Open;
            if (abrir) cn.Open();
            try
            {
                Ejecutar(cn, SqlTablaRegistro);
                (tablasFaltantes, colsFaltantes) = Detectar(db, cn);
                pasosPendientes = PasosPendientes(cn);
                if (tablasFaltantes.Count == 0 && colsFaltantes.Count == 0 && pasosPendientes.Count == 0) return res;
                hayDatos = HayDatos(db, cn);
            }
            finally { if (abrir) cn.Close(); }

            // ---- Fase 2: respaldo previo (con la conexión cerrada) ----
            if (hayDatos && respaldarAntes)
            {
                if (respaldo == null)
                {
                    res.Errores.Add("Hay cambios pendientes en la base de datos pero no hay servicio de respaldo: no se aplicó nada.");
                    return res;
                }
                // Si la pantalla de actualizaciones acaba de crear el respaldo previo (hace menos de 30 min), se reutiliza:
                // asi la actualizacion no hace dos respaldos seguidos.
                string? reciente = null;
                try
                {
                    reciente = respaldo.Listar()
                        .FirstOrDefault(f => f.Name.Contains("previo-actualizacion", StringComparison.OrdinalIgnoreCase) && f.LastWriteTime > DateTime.Now.AddMinutes(-30))?.FullName;
                }
                catch { }
                if (reciente != null) res.RespaldoPrevio = reciente;
                else
                {
                    var r = respaldo.CrearAsync("previo-actualizacion").GetAwaiter().GetResult();
                    if (!r.Ok)
                    {
                        res.Errores.Add("No se pudo crear el respaldo previo a la actualización, por seguridad NO se aplicó ningún cambio (se reintenta al próximo arranque): " + r.Mensaje);
                        return res;
                    }
                    res.RespaldoPrevio = r.Ruta;
                }
            }

            // ---- Fase 3: aplicar (solo aditivo) ----
            abrir = cn.State != ConnectionState.Open;
            if (abrir) cn.Open();
            try
            {
                if (tablasFaltantes.Count > 0 || colsFaltantes.Count > 0)
                {
                    var sentencias = Regex.Split(db.Database.GenerateCreateScript(), @";\s*\r?\n")
                        .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

                    var definiciones = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var sql in sentencias)
                    {
                        var t = NombreTablaCreate(sql);
                        if (t != null) definiciones[t] = ExtraerDefinicionesColumnas(sql);
                    }

                    // 3.1 Tablas nuevas (con sus índices)
                    foreach (var sql in sentencias)
                    {
                        var t = NombreTablaCreate(sql) ?? NombreTablaIndice(sql);
                        if (t == null || !tablasFaltantes.Contains(t) || !NombreValido.IsMatch(t)) continue;
                        try
                        {
                            Ejecutar(cn, sql);
                            if (NombreTablaCreate(sql) != null) res.TablasCreadas.Add(t);
                            else res.IndicesCreados.Add(t + " (índice)");
                        }
                        catch (Exception ex) { res.Errores.Add($"No se pudo crear {t}: {ex.Message}"); }
                    }

                    // 3.2 Columnas nuevas en tablas existentes
                    var agregadasPorTabla = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var (tabla, columna, esPk) in colsFaltantes)
                    {
                        if (tablasFaltantes.Contains(tabla)) continue;
                        if (!NombreValido.IsMatch(tabla) || !NombreValido.IsMatch(columna)) { res.Errores.Add($"{tabla}.{columna}: nombre no válido, se omitió."); continue; }
                        if (!definiciones.TryGetValue(tabla, out var cols) || !cols.TryGetValue(columna, out var def))
                        { res.Errores.Add($"{tabla}.{columna}: no se encontró su definición. Requiere paso manual."); continue; }
                        if (esPk || def.Contains("AUTO_INCREMENT", StringComparison.OrdinalIgnoreCase))
                        { res.Errores.Add($"{tabla}.{columna}: es llave primaria/autonumérica. Requiere paso manual."); continue; }

                        var defSegura = ConDefaultSeguro(def);
                        if (defSegura == null)
                        { res.Errores.Add($"{tabla}.{columna}: es obligatoria y no se pudo elegir un valor por defecto seguro. Requiere paso manual."); continue; }
                        try
                        {
                            Ejecutar(cn, $"ALTER TABLE `{tabla}` ADD COLUMN `{columna}` {defSegura}");
                            res.ColumnasAgregadas.Add($"{tabla}.{columna}");
                            if (!agregadasPorTabla.TryGetValue(tabla, out var set))
                                agregadasPorTabla[tabla] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            set.Add(columna);
                        }
                        catch (Exception ex) { res.Errores.Add($"{tabla}.{columna}: {ex.Message}"); }
                    }

                    // 3.3 Índices NO únicos de las columnas nuevas (los únicos, llaves foráneas y demás son manuales)
                    foreach (var sql in sentencias)
                    {
                        var m = Regex.Match(sql, @"^CREATE\s+INDEX\s+`[^`]+`\s+ON\s+`(?<t>[^`]+)`\s*\((?<c>[^)]*)\)", RegexOptions.IgnoreCase);
                        if (!m.Success) continue;
                        var t = m.Groups["t"].Value;
                        if (!agregadasPorTabla.TryGetValue(t, out var nuevas)) continue;
                        if (!nuevas.Any(c => m.Groups["c"].Value.Contains("`" + c + "`", StringComparison.OrdinalIgnoreCase))) continue;
                        try { Ejecutar(cn, sql); res.IndicesCreados.Add(t + " (índice)"); }
                        catch { /* ya existía u otro detalle menor: no es crítico */ }
                    }
                }

                // 3.4 Ajustes manuales pendientes (se detiene en el primero que falle)
                foreach (var paso in pasosPendientes)
                {
                    try
                    {
                        foreach (var sql in paso.Sql)
                        {
                            if (!EsSqlAditivo(sql)) throw new InvalidOperationException("SQL no permitido (no es aditivo): " + sql);
                        }
                        foreach (var sql in paso.Sql) Ejecutar(cn, sql);
                        using (var cmd = cn.CreateCommand())
                        {
                            cmd.CommandText = "INSERT INTO `__KirkentaEsquema` (`Id`, `Descripcion`, `AplicadoEn`) VALUES (@i, @d, @f)";
                            Param(cmd, "@i", paso.Id); Param(cmd, "@d", Recortar(paso.Descripcion, 300)); Param(cmd, "@f", DateTime.Now);
                            cmd.ExecuteNonQuery();
                        }
                        res.PasosAplicados.Add(paso.Id);
                    }
                    catch (Exception ex)
                    {
                        res.Errores.Add($"Paso manual '{paso.Id}' falló y se detuvo la lista: {ex.Message}");
                        break;
                    }
                }
            }
            finally { if (abrir) cn.Close(); }

            return res;
        }

        // ------------------------------------------------------------------
        // Detección
        // ------------------------------------------------------------------
        private static (HashSet<string> Tablas, List<(string Tabla, string Columna, bool EsPk)> Columnas) Detectar(ApplicationDbContext db, DbConnection cn)
        {
            var existentes = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = cn.CreateCommand())
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

            var tablas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cols = new List<(string, string, bool)>();
            var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entidad in db.Model.GetEntityTypes())
            {
                var tabla = entidad.GetTableName();
                if (tabla == null) continue;

                if (!existentes.TryGetValue(tabla, out var columnas)) { tablas.Add(tabla); continue; }

                var id = StoreObjectIdentifier.Table(tabla, entidad.GetSchema());
                foreach (var prop in entidad.GetProperties())
                {
                    var col = prop.GetColumnName(id);
                    if (col == null || columnas.Contains(col)) continue;
                    if (!vistas.Add(tabla + "." + col)) continue;
                    cols.Add((tabla, col, prop.IsPrimaryKey()));
                }
            }
            return (tablas, cols);
        }

        private static List<PasoManual> PasosPendientes(DbConnection cn)
        {
            if (PasosManuales.Count == 0) return new List<PasoManual>();
            var hechos = new HashSet<string>(StringComparer.Ordinal);
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT `Id` FROM `__KirkentaEsquema`";
                using var rd = cmd.ExecuteReader();
                while (rd.Read()) hechos.Add(rd.GetString(0));
            }
            return PasosManuales.Where(p => !hechos.Contains(p.Id)).ToList();
        }

        /// <summary>true si la base ya tiene información que valga la pena respaldar (ante la duda, true).</summary>
        private static bool HayDatos(ApplicationDbContext db, DbConnection cn)
        {
            try
            {
                var tabla = db.Model.FindEntityType(typeof(Kirkenta.Models.Usuario))?.GetTableName() ?? "Usuarios";
                if (!NombreValido.IsMatch(tabla)) return true;
                using var cmd = cn.CreateCommand();
                cmd.CommandText = $"SELECT COUNT(*) FROM (SELECT 1 FROM `{tabla}` LIMIT 1) x";
                return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
            }
            catch { return true; }
        }

        // ------------------------------------------------------------------
        // Utilidades (públicas para poder probarlas)
        // ------------------------------------------------------------------
        public static string? NombreTablaCreate(string sql)
        {
            var m = Regex.Match(sql, @"^CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?`(?<t>[^`]+)`", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups["t"].Value : null;
        }

        public static string? NombreTablaIndice(string sql)
        {
            var m = Regex.Match(sql, @"^CREATE\s+(?:UNIQUE\s+)?INDEX\s+`[^`]+`\s+ON\s+`(?<t>[^`]+)`", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups["t"].Value : null;
        }

        /// <summary>De un CREATE TABLE devuelve columna → definición (tipo, NULL/NOT NULL, DEFAULT...). Ignora CONSTRAINT/KEY.</summary>
        public static Dictionary<string, string> ExtraerDefinicionesColumnas(string createTable)
        {
            var dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Regex.Matches(createTable, @"^\s*`(?<c>[^`]+)`\s+(?<d>[^\r\n]+?)\s*,?\s*$", RegexOptions.Multiline))
            {
                var d = m.Groups["d"].Value.Trim().TrimEnd(',').Trim();
                if (d.Length == 0) continue;
                dic[m.Groups["c"].Value] = d;
            }
            return dic;
        }

        /// <summary>
        /// Devuelve la definición lista para ADD COLUMN. Si es NOT NULL sin DEFAULT le agrega uno seguro por tipo.
        /// Devuelve null si no se puede elegir un valor seguro (hay que hacerlo a mano).
        /// </summary>
        public static string? ConDefaultSeguro(string definicion)
        {
            var d = (definicion ?? "").Trim();
            if (d.Length == 0) return null;
            if (!Regex.IsMatch(d, @"\bNOT\s+NULL\b", RegexOptions.IgnoreCase)) return d;      // admite NULL: sirve tal cual
            if (Regex.IsMatch(d, @"\bDEFAULT\b", RegexOptions.IgnoreCase)) return d;           // ya trae valor por defecto

            var m = Regex.Match(d, @"^(?<t>[A-Za-z]+)\s*(?:\((?<n>[^)]*)\))?", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            var tipo = m.Groups["t"].Value.ToLowerInvariant();
            var n = m.Groups["n"].Value.Trim();

            string? valor = tipo switch
            {
                "int" or "bigint" or "smallint" or "mediumint" or "tinyint" or "decimal" or "numeric" or "double" or "float" => "0",
                "bit" => "b'0'",
                "varchar" => "''",
                "char" => n == "36" ? "'00000000-0000-0000-0000-000000000000'" : "''",
                "text" or "tinytext" or "mediumtext" or "longtext" or "blob" or "tinyblob" or "mediumblob" or "longblob" => "('')",
                "json" => "(JSON_OBJECT())",
                "datetime" or "timestamp" => n.Length > 0 ? $"CURRENT_TIMESTAMP({n})" : "CURRENT_TIMESTAMP",
                "date" => "(CURRENT_DATE)",
                "time" => "'00:00:00'",
                _ => null
            };
            return valor == null ? null : $"{d} DEFAULT {valor}";
        }

        /// <summary>Un paso manual solo puede AGREGAR o RELLENAR. Rechaza todo lo destructivo.</summary>
        public static bool EsSqlAditivo(string? sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return false;
            var s = sql.Trim().TrimEnd(';').Trim();
            if (s.Contains(';') || s.Contains("--") || s.Contains("/*") || s.Contains('#')) return false;

            if (Regex.IsMatch(s, @"\b(DROP|TRUNCATE|DELETE|RENAME|CHANGE|MODIFY|REPLACE|GRANT|REVOKE|OUTFILE|DUMPFILE|LOAD_FILE|INFILE|SHUTDOWN|KILL)\b", RegexOptions.IgnoreCase))
                return false;

            if (Regex.IsMatch(s, @"^ALTER\s+TABLE\s+`?\w+`?\s+ADD\b", RegexOptions.IgnoreCase)) return true;
            if (Regex.IsMatch(s, @"^CREATE\s+(TABLE|INDEX|UNIQUE\s+INDEX)\b", RegexOptions.IgnoreCase)) return true;
            if (Regex.IsMatch(s, @"^INSERT\s+(IGNORE\s+)?INTO\b", RegexOptions.IgnoreCase)) return true;
            if (Regex.IsMatch(s, @"^UPDATE\b", RegexOptions.IgnoreCase)) return Regex.IsMatch(s, @"\bWHERE\b", RegexOptions.IgnoreCase);
            return false;
        }

        // ------------------------------------------------------------------
        private static void Ejecutar(DbConnection cn, string sql)
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 0;
            cmd.ExecuteNonQuery();
        }

        private static void Param(DbCommand cmd, string nombre, object valor)
        {
            var p = cmd.CreateParameter(); p.ParameterName = nombre; p.Value = valor; cmd.Parameters.Add(p);
        }

        private static string Recortar(string s, int max) => s.Length <= max ? s : s.Substring(0, max);
    }
}
