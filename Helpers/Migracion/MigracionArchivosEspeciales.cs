using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Data.SqlClient;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>
    /// Archivos de bases de datos que no son texto: Access (.mdb/.accdb), respaldos y archivos de SQL Server
    /// (.bak/.mdf) y bases Firebird (.fdb/.gdb). Se copian a una carpeta temporal, se abren con el motor que
    /// corresponda (solo lectura), se leen todas sus tablas y se borra todo lo temporal.
    /// </summary>
    public static class MigracionArchivosEspeciales
    {
        private static string CarpetaTemporal()
        {
            var dir = Path.Combine(Path.GetTempPath(), "Kirkenta-migracion", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static string Guardar(Stream stream, string dir, string nombre)
        {
            var ruta = Path.Combine(dir, Path.GetFileName(nombre));
            using var f = File.Create(ruta);
            stream.CopyTo(f);
            return ruta;
        }

        private static void Borrar(string dir)
        {
            try { Directory.Delete(dir, true); } catch { /* el sistema operativo la limpia después */ }
        }

        // ================= Access =================
        private static readonly string[] ProveedoresAccess = { "Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0", "Microsoft.Jet.OLEDB.4.0" };

        /// <summary>El motor de Access es código nativo: un archivo que no es Access, o dos lecturas a la vez, pueden tumbar el proceso.</summary>
        private static readonly object CandadoAccess = new();

        /// <summary>Firma de un archivo de Access: «Standard Jet DB» (.mdb) o «Standard ACE DB» (.accdb) en el byte 4.</summary>
        public static bool EsArchivoAccess(byte[] cabecera)
        {
            if (cabecera.Length < 19) return false;
            var texto = System.Text.Encoding.ASCII.GetString(cabecera, 4, 15);
            return texto == "Standard Jet DB" || texto == "Standard ACE DB";
        }

        public static void LeerAccess(string nombre, Stream stream, ResultadoMigracionLectura r)
        {
            if (!OperatingSystem.IsWindows())
            {
                r.Avisos.Add($"«{nombre}»: las bases de Access solo se pueden leer cuando Kirkenta corre en Windows. Expórtala a Excel o CSV.");
                return;
            }
            var dir = CarpetaTemporal();
            try
            {
                var ruta = Guardar(stream, dir, nombre);
                var cabecera = new byte[32];
                using (var f = File.OpenRead(ruta)) _ = f.Read(cabecera, 0, cabecera.Length);
                if (!EsArchivoAccess(cabecera))
                {
                    r.Avisos.Add($"«{nombre}»: no es una base de Access válida o está dañada. Ábrela en Access (Herramientas de base de datos → Compactar y reparar) y vuelve a subirla.");
                    return;
                }
                lock (CandadoAccess) LeerAccessArchivo(nombre, ruta, r);
            }
            finally { Borrar(dir); }
        }

        private static void LeerAccessArchivo(string nombre, string ruta, ResultadoMigracionLectura r)
        {
            if (!OperatingSystem.IsWindows()) return;
            {
                // Motor de Access de 32 bits, de otra versión o ausente: se prueban los proveedores conocidos
                string? ultimoError = null;
                foreach (var proveedor in ProveedoresAccess)
                {
                    try
                    {
                        var cs = new OleDbConnectionStringBuilder { Provider = proveedor, DataSource = ruta };
                        cs["Mode"] = "Read";
                        cs["Persist Security Info"] = false;
                        using var c = new OleDbConnection(cs.ConnectionString);
                        c.Open();
                        MigracionConexionDirecta.LeerTodo(c, "Access", r);
                        return;
                    }
                    catch (Exception ex)
                    {
                        ultimoError = ex.Message;
                        var n = MigracionHistorialHelper.Norm(ex.Message);
                        if (n.Contains("unrecognized database format") || n.Contains("formato de base de datos") || n.Contains("not a valid") || n.Contains("no es un"))
                        {
                            r.Avisos.Add($"«{nombre}»: no es una base de Access válida o está dañada. Ábrela en Access (Herramientas de base de datos → Compactar y reparar) y vuelve a subirla. ({ex.Message})");
                            return;
                        }
                        if (n.Contains("password") || n.Contains("contrasena"))
                        {
                            r.Avisos.Add($"«{nombre}»: la base de Access tiene contraseña. Quítasela en Access (Archivo → Información → Descifrar base de datos) y vuelve a subirla.");
                            return;
                        }
                    }
                }
                r.Avisos.Add($"«{nombre}»: este equipo no tiene el motor de Access. Instala gratis «Microsoft Access Database Engine 2016 Redistributable» (versión de 64 bits) desde el sitio de Microsoft y vuelve a subir el archivo. ({ultimoError})");
            }
        }

        // ================= SQL Server: .bak / .mdf =================
        /// <summary>Instancias locales donde se puede restaurar temporalmente (LocalDB primero: corre como el usuario actual).</summary>
        private static readonly string[] InstanciasSql = { @"(localdb)\MSSQLLocalDB", @".\SQLEXPRESS", "localhost" };

        private static SqlConnection? ConectarInstancia(out string? instancia, List<string> errores)
        {
            foreach (var i in InstanciasSql)
            {
                try
                {
                    var c = new SqlConnection(new SqlConnectionStringBuilder
                    {
                        DataSource = i, InitialCatalog = "master", IntegratedSecurity = true, ConnectTimeout = 8,
                        TrustServerCertificate = true, Encrypt = SqlConnectionEncryptOption.Optional
                    }.ConnectionString);
                    c.Open();
                    instancia = i;
                    return c;
                }
                catch (Exception ex) { errores.Add($"{i}: {ex.Message}"); }
            }
            instancia = null;
            return null;
        }

        public static void LeerSqlServer(string nombre, Stream stream, ResultadoMigracionLectura r)
        {
            var ext = Path.GetExtension(nombre).ToLowerInvariant();
            if (ext is ".ldf") { r.Avisos.Add($"«{nombre}»: es solo el registro de transacciones; sube el archivo .mdf o el respaldo .bak."); return; }
            if (ext is ".bacpac") { r.Avisos.Add($"«{nombre}»: los .bacpac se importan primero con SqlPackage en un SQL Server; luego usa la conexión directa."); return; }

            var errores = new List<string>();
            using var master = ConectarInstancia(out var instancia, errores);
            if (master == null)
            {
                r.Avisos.Add($"«{nombre}»: para leer un {ext} hace falta SQL Server en este equipo (sirve el gratuito «SQL Server Express» o «LocalDB»). " +
                             "Si el sistema anterior sigue funcionando, usa mejor la conexión directa a su SQL Server.");
                return;
            }

            // LocalDB corre con el usuario actual: lee la carpeta temporal. Un servicio de SQL Server puede no tener permiso:
            // en ese caso los archivos van a su propia carpeta de datos.
            var dir = CarpetaTemporal();
            var baseTemp = "KirkentaMig_" + Guid.NewGuid().ToString("N")[..12];
            bool creada = false;
            try
            {
                var ruta = Guardar(stream, dir, nombre);
                string destino = dir;
                if (!instancia!.StartsWith("(localdb)", StringComparison.OrdinalIgnoreCase))
                {
                    using var cmd = master.CreateCommand();
                    cmd.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(400))";
                    if (cmd.ExecuteScalar() is string datos && datos.Length > 0) destino = datos;
                }

                if (ext == ".bak")
                {
                    // Nombres lógicos de los archivos dentro del respaldo
                    var archivos = new List<(string logico, string tipo)>();
                    using (var cmd = master.CreateCommand())
                    {
                        cmd.CommandText = "RESTORE FILELISTONLY FROM DISK = @ruta";
                        cmd.Parameters.AddWithValue("@ruta", ruta);
                        cmd.CommandTimeout = 600;
                        using var rd = cmd.ExecuteReader();
                        while (rd.Read()) archivos.Add((rd.GetString(rd.GetOrdinal("LogicalName")), rd.GetString(rd.GetOrdinal("Type"))));
                    }
                    using (var cmd = master.CreateCommand())
                    {
                        var moves = new List<string>();
                        for (int i = 0; i < archivos.Count; i++)
                        {
                            var ext2 = archivos[i].tipo == "L" ? ".ldf" : i == 0 ? ".mdf" : $"_{i}.ndf";
                            cmd.Parameters.AddWithValue($"@l{i}", archivos[i].logico);
                            cmd.Parameters.AddWithValue($"@f{i}", Path.Combine(destino, baseTemp + ext2));
                            moves.Add($"MOVE @l{i} TO @f{i}");
                        }
                        cmd.Parameters.AddWithValue("@ruta", ruta);
                        cmd.Parameters.AddWithValue("@base", baseTemp);
                        cmd.CommandText = "RESTORE DATABASE @base FROM DISK = @ruta WITH RECOVERY, REPLACE" + (moves.Count > 0 ? ", " + string.Join(", ", moves) : "");
                        cmd.CommandTimeout = 1800;
                        cmd.ExecuteNonQuery();
                        creada = true;
                    }
                }
                else // .mdf: se adjunta y se reconstruye el registro de transacciones
                {
                    var mdf = ruta;
                    if (!destino.Equals(dir, StringComparison.OrdinalIgnoreCase))
                    {
                        mdf = Path.Combine(destino, baseTemp + ".mdf");
                        File.Copy(ruta, mdf);
                    }
                    using var cmd = master.CreateCommand();
                    // CREATE DATABASE no acepta parámetros: el nombre es generado aquí y la ruta se escapa
                    cmd.CommandText = $"CREATE DATABASE [{baseTemp}] ON (FILENAME = N'{mdf.Replace("'", "''")}') FOR ATTACH_REBUILD_LOG";
                    cmd.CommandTimeout = 600;
                    cmd.ExecuteNonQuery();
                    creada = true;
                }

                using (var cmd = master.CreateCommand()) { cmd.CommandText = $"USE [{baseTemp}]"; cmd.ExecuteNonQuery(); }
                MigracionConexionDirecta.LeerTodo(master, "SqlServer", r);
                if (r.Tablas.Count > 0) r.Avisos.Insert(0, $"«{nombre}» se restauró temporalmente en {instancia}, se leyó y se borró.");
            }
            catch (SqlException ex)
            {
                var n = MigracionHistorialHelper.Norm(ex.Message);
                r.Avisos.Add(n.Contains("version") || n.Contains("cannot be restored") || n.Contains("upgrade")
                    ? $"«{nombre}»: el respaldo es de una versión de SQL Server más nueva que la de este equipo ({instancia}). Instala una versión igual o más reciente, o usa la conexión directa. ({ex.Message})"
                    : n.Contains("access is denied") || n.Contains("operating system error 5")
                    ? $"«{nombre}»: SQL Server ({instancia}) no tiene permiso para leer el archivo. Copia el archivo a la carpeta de respaldos de SQL Server o usa LocalDB. ({ex.Message})"
                    : $"«{nombre}»: SQL Server no pudo abrir el archivo: {ex.Message}");
            }
            finally
            {
                if (creada)
                {
                    try
                    {
                        using var cmd = master.CreateCommand();
                        cmd.CommandText = $"USE master; ALTER DATABASE [{baseTemp}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{baseTemp}];";
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex) { r.Avisos.Add($"Aviso: no se pudo borrar la base temporal {baseTemp} de {instancia} ({ex.Message}). Puedes borrarla a mano."); }
                }
                Borrar(dir);
            }
        }

        // ================= Firebird =================
        public static void LeerFirebird(string nombre, Stream stream, ResultadoMigracionLectura r)
        {
            var dir = CarpetaTemporal();
            try
            {
                var ruta = Guardar(stream, dir, nombre);
                // Servidor Firebird de este equipo con el usuario por omisión; un servidor del cliente usa la conexión directa
                foreach (var (usuario, clave) in new[] { ("SYSDBA", "masterkey"), ("SYSDBA", "masterke") })
                {
                    try
                    {
                        using var c = new FbConnection(new FbConnectionStringBuilder
                        {
                            DataSource = "localhost", Port = 3050, Database = ruta, UserID = usuario, Password = clave,
                            Charset = "NONE", ConnectionTimeout = 8
                        }.ConnectionString);
                        c.Open();
                        MigracionConexionDirecta.LeerTodo(c, "Firebird", r);
                        return;
                    }
                    catch (FbException ex) when (MigracionHistorialHelper.Norm(ex.Message).Contains("password") || MigracionHistorialHelper.Norm(ex.Message).Contains("user name"))
                    {
                        continue;
                    }
                    catch (Exception ex)
                    {
                        r.Avisos.Add($"«{nombre}»: para leer un archivo de Firebird hace falta el servidor Firebird en este equipo (gratis, firebirdsql.org) " +
                                     $"o conectarse al servidor donde está la base usando la conexión directa (Firebird, con la ruta del archivo). ({ex.Message})");
                        return;
                    }
                }
                r.Avisos.Add($"«{nombre}»: Firebird rechazó el usuario SYSDBA. Usa la conexión directa (Firebird) con el usuario y la contraseña de esa base.");
            }
            finally { Borrar(dir); }
        }
    }
}
