using System.Data;
using System.Data.Common;
using System.Globalization;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>Datos para conectarse a la base del sistema anterior. Nunca se guardan: viven solo durante la lectura.</summary>
    public class DatosConexion
    {
        /// <summary>MySql (también MariaDB), SqlServer, PostgreSql, Firebird.</summary>
        public string Motor { get; set; } = "SqlServer";
        public string Servidor { get; set; } = "";
        public int? Puerto { get; set; }
        public string BaseDatos { get; set; } = "";
        public string Usuario { get; set; } = "";
        public string Clave { get; set; } = "";
        /// <summary>SQL Server: usar la sesión de Windows en lugar de usuario y clave.</summary>
        public bool SeguridadWindows { get; set; }
    }

    /// <summary>
    /// Lee TODAS las tablas de la base de datos del sistema anterior directamente (SQL Server, MySQL/MariaDB,
    /// PostgreSQL o Firebird), sin que el cliente tenga que exportar nada. Solo hace SELECT: nunca escribe
    /// en la base del sistema anterior. El resultado entra al mismo análisis y migración que un archivo.
    /// </summary>
    public static class MigracionConexionDirecta
    {
        public static readonly string[] Motores = { "SqlServer", "MySql", "PostgreSql", "Firebird" };

        public static string NombreMotor(string motor) => motor switch
        {
            "SqlServer" => "SQL Server",
            "MySql" => "MySQL / MariaDB",
            "PostgreSql" => "PostgreSQL",
            "Firebird" => "Firebird / InterBase",
            _ => motor
        };

        public static int PuertoPorDefecto(string motor) => motor switch
        {
            "MySql" => 3306, "PostgreSql" => 5432, "Firebird" => 3050, _ => 1433
        };

        /// <summary>Arma la conexión con los "builders" de cada controlador (sin concatenar texto: nada se puede inyectar).</summary>
        public static DbConnection Crear(DatosConexion d)
        {
            var servidor = string.IsNullOrWhiteSpace(d.Servidor) ? "localhost" : d.Servidor.Trim();
            switch (d.Motor)
            {
                case "MySql":
                    return new MySqlConnection(new MySqlConnectionStringBuilder
                    {
                        Server = servidor, Port = (uint)(d.Puerto ?? 3306), Database = d.BaseDatos, UserID = d.Usuario, Password = d.Clave,
                        ConnectionTimeout = 15, DefaultCommandTimeout = 300, AllowZeroDateTime = true, ConvertZeroDateTime = true,
                        SslMode = MySqlSslMode.Preferred, AllowPublicKeyRetrieval = true
                    }.ConnectionString);
                case "PostgreSql":
                    return new NpgsqlConnection(new NpgsqlConnectionStringBuilder
                    {
                        Host = servidor, Port = d.Puerto ?? 5432, Database = d.BaseDatos, Username = d.Usuario, Password = d.Clave,
                        Timeout = 15, CommandTimeout = 300
                    }.ConnectionString);
                case "Firebird":
                    return new FbConnection(new FbConnectionStringBuilder
                    {
                        DataSource = servidor, Port = d.Puerto ?? 3050, Database = d.BaseDatos,
                        UserID = string.IsNullOrWhiteSpace(d.Usuario) ? "SYSDBA" : d.Usuario,
                        Password = string.IsNullOrEmpty(d.Clave) ? "masterkey" : d.Clave,
                        Charset = "UTF8", ConnectionTimeout = 15
                    }.ConnectionString);
                default:
                    var b = new SqlConnectionStringBuilder
                    {
                        DataSource = d.Puerto.HasValue && !servidor.Contains('\\') ? $"{servidor},{d.Puerto}" : servidor,
                        InitialCatalog = d.BaseDatos, ConnectTimeout = 15, TrustServerCertificate = true, Encrypt = SqlConnectionEncryptOption.Optional,
                        ApplicationIntent = ApplicationIntent.ReadOnly
                    };
                    if (d.SeguridadWindows) b.IntegratedSecurity = true;
                    else { b.UserID = d.Usuario; b.Password = d.Clave; }
                    return new SqlConnection(b.ConnectionString);
            }
        }

        /// <summary>Prueba la conexión y devuelve un mensaje claro si falla.</summary>
        public static string? Probar(DatosConexion d)
        {
            try
            {
                using var c = Crear(d);
                c.Open();
                return null;
            }
            catch (Exception ex) { return Explicar(d, ex); }
        }

        private static string Explicar(DatosConexion d, Exception ex)
        {
            var m = ex.Message;
            var n = MigracionHistorialHelper.Norm(m);
            if (n.Contains("login") || n.Contains("password") || n.Contains("access denied") || n.Contains("autentic") || n.Contains("authentication"))
                return $"{NombreMotor(d.Motor)} rechazó el usuario o la contraseña. ({m})";
            if (n.Contains("network") || n.Contains("server was not found") || n.Contains("unable to connect") || n.Contains("timeout") || n.Contains("refused") || n.Contains("no such host"))
                return $"No se pudo llegar al servidor «{d.Servidor}». Revisa la dirección, el puerto ({d.Puerto ?? PuertoPorDefecto(d.Motor)}) y que el firewall del servidor permita la conexión. ({m})";
            if (n.Contains("database") && (n.Contains("not exist") || n.Contains("unknown") || n.Contains("cannot open")))
                return $"El servidor respondió, pero la base «{d.BaseDatos}» no existe o el usuario no tiene permiso para leerla. ({m})";
            return $"No se pudo conectar a {NombreMotor(d.Motor)}: {m}";
        }

        /// <summary>Lee todas las tablas con datos. Tablas vacías o de sistema se omiten.</summary>
        public static ResultadoMigracionLectura Leer(DatosConexion d)
        {
            var r = new ResultadoMigracionLectura();
            DbConnection c;
            try { c = Crear(d); c.Open(); }
            catch (Exception ex) { r.Avisos.Add(Explicar(d, ex)); return r; }
            using (c)
            {
                LeerTodo(c, d.Motor, r);
            }
            return r;
        }

        /// <summary>Lee todas las tablas de una conexión ya abierta (también la usan Access y la restauración de .bak).</summary>
        public static void LeerTodo(DbConnection c, string motor, ResultadoMigracionLectura r)
        {
            List<(string? esquema, string tabla)> tablas;
            try { tablas = ListarTablas(c, motor); }
            catch (Exception ex) { r.Avisos.Add("No se pudo obtener la lista de tablas: " + ex.Message); return; }
            if (tablas.Count == 0) { r.Avisos.Add("La base no tiene tablas o el usuario no tiene permiso para verlas."); return; }

            long total = 0;
            foreach (var (esquema, tabla) in tablas)
            {
                if (total >= SqlDumpParser.MaxFilasTotales)
                {
                    r.Avisos.Add($"Se alcanzó el límite de {SqlDumpParser.MaxFilasTotales:N0} filas: las tablas restantes no se leyeron (migra por partes).");
                    break;
                }
                try
                {
                    using var cmd = c.CreateCommand();
                    cmd.CommandText = "SELECT * FROM " + Citar(motor, esquema, tabla);
                    cmd.CommandTimeout = 300;
                    using var rd = cmd.ExecuteReader();
                    var headers = new List<string>();
                    var usadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < rd.FieldCount; i++)
                    {
                        var h = rd.GetName(i);
                        if (string.IsNullOrWhiteSpace(h)) h = "Columna" + (i + 1);
                        var unico = h; int k = 2;
                        while (!usadas.Add(unico)) unico = $"{h}_{k++}";
                        headers.Add(unico);
                    }
                    var filas = new List<Dictionary<string, string>>();
                    while (rd.Read())
                    {
                        var f = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < rd.FieldCount; i++) f[headers[i]] = Texto(rd, i);
                        filas.Add(f);
                        if (++total >= SqlDumpParser.MaxFilasTotales) break;
                    }
                    var nombre = string.IsNullOrEmpty(esquema) || esquema is "dbo" or "public" ? tabla : $"{esquema}.{tabla}";
                    r.Agregar(nombre, headers, filas);
                }
                catch (Exception ex)
                {
                    r.Avisos.Add($"La tabla «{tabla}» no se pudo leer: {ex.Message}");
                }
            }
        }

        private static List<(string?, string)> ListarTablas(DbConnection c, string motor)
        {
            var res = new List<(string?, string)>();
            if (motor == "Access")
            {
                var esquema = c.GetSchema("Tables");
                foreach (DataRow row in esquema.Rows)
                    if (string.Equals(row["TABLE_TYPE"]?.ToString(), "TABLE", StringComparison.OrdinalIgnoreCase))
                        res.Add((null, row["TABLE_NAME"]!.ToString()!));
                return res;
            }
            using var cmd = c.CreateCommand();
            cmd.CommandText = motor switch
            {
                "MySql" => "SELECT NULL, TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME",
                "PostgreSql" => "SELECT table_schema, table_name FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND table_schema NOT IN ('pg_catalog', 'information_schema') ORDER BY table_schema, table_name",
                "Firebird" => "SELECT NULL, TRIM(RDB$RELATION_NAME) FROM RDB$RELATIONS WHERE COALESCE(RDB$SYSTEM_FLAG, 0) = 0 AND RDB$VIEW_BLR IS NULL ORDER BY 2",
                _ => "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME NOT IN ('sysdiagrams') ORDER BY TABLE_SCHEMA, TABLE_NAME"
            };
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) res.Add((rd.IsDBNull(0) ? null : rd.GetString(0), rd.GetString(1)));
            return res;
        }

        /// <summary>Nombre de tabla citado según el motor (los nombres vienen del catálogo de la propia base).</summary>
        public static string Citar(string motor, string? esquema, string tabla)
        {
            string Q(string s) => motor switch
            {
                "MySql" => "`" + s.Replace("`", "``") + "`",
                "SqlServer" or "Access" => "[" + s.Replace("]", "]]") + "]",
                _ => "\"" + s.Replace("\"", "\"\"") + "\""
            };
            return string.IsNullOrEmpty(esquema) ? Q(tabla) : Q(esquema) + "." + Q(tabla);
        }

        /// <summary>Valor como texto en formato neutro (fechas ISO, decimales con punto) para que el análisis lo entienda igual que un archivo.</summary>
        private static string Texto(DbDataReader rd, int i)
        {
            if (rd.IsDBNull(i)) return "";
            object v;
            try { v = rd.GetValue(i); }
            catch { return ""; }   // fechas inválidas (0000-00-00) y tipos raros: se dejan vacíos
            return v switch
            {
                DateTime f => f.TimeOfDay == TimeSpan.Zero ? f.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : f.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                DateTimeOffset fo => fo.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                bool b => b ? "true" : "false",
                byte[] => "",
                IFormattable n => n.ToString(null, CultureInfo.InvariantCulture),
                _ => v.ToString()?.Trim() ?? ""
            };
        }
    }
}
