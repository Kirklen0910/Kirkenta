using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Services
{
    public class ManifiestoRespaldo
    {
        public int Version { get; set; } = 1;
        public string Sistema { get; set; } = "Kirkenta";
        public DateTime Fecha { get; set; }
        public string? UltimaMigracion { get; set; }
        public Dictionary<string, long> Tablas { get; set; } = new();
        public int Archivos { get; set; }
        public long FilasTotales { get; set; }
    }

    public class ResultadoRespaldo
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public string? Ruta { get; set; }
        public long Bytes { get; set; }
        public List<string> Avisos { get; } = new();
    }

    /// <summary>
    /// Respaldo y restauración completa de los DATOS del cliente (todas las tablas + archivos subidos) en un solo .zip.
    /// Es independiente de la versión de MySQL: guarda cada tabla como JSON (una fila por línea) y sus tipos.
    /// Para restaurar en un sistema en blanco basta con la app corriendo (crea el esquema si la BD está vacía).
    /// </summary>
    public class RespaldoSistemaService
    {
        private static readonly Regex NombreSeguro = new("^[A-Za-z0-9_]{1,128}$", RegexOptions.Compiled);
        private static readonly string[] CarpetasArchivos = { "wwwroot/uploads", "wwwroot/images", "App_Data" };
        private const string Prefijo = "kirkenta-datos_";
        private static readonly SemaphoreSlim _candado = new(1, 1);

        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<RespaldoSistemaService> _log;

        public RespaldoSistemaService(ApplicationDbContext db, IWebHostEnvironment env, ILogger<RespaldoSistemaService> log)
        {
            _db = db; _env = env; _log = log;
        }

        public static string CarpetaRespaldos(IWebHostEnvironment env)
        {
            var c = RespaldoEstado.Buscar() ?? Path.Combine(env.ContentRootPath, "respaldos");
            Directory.CreateDirectory(c);
            return c;
        }

        public string Carpeta => CarpetaRespaldos(_env);

        public List<FileInfo> Listar() =>
            new DirectoryInfo(Carpeta).GetFiles(Prefijo + "*.zip").OrderByDescending(f => f.LastWriteTime).ToList();

        /// <summary>Módulos que se pueden restaurar por separado (nombre → tablas).</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Grupos = new Dictionary<string, string[]>
        {
            ["Clientes"] = new[] { "Clientes" },
            ["Productos e inventario"] = new[] { "Productos", "Categorias", "UnidadesMedida", "MovimientosInventario", "BajasInventario", "TomasFisicas", "DetallesTomaFisica" },
            ["Ventas y facturación"] = new[] { "Cotizaciones", "DetalleCotizaciones", "Pedidos", "DetallePedidos", "Ventas", "DetalleVentas", "VentasOffline", "PedidosWeb", "CatalogoWebConfig", "Facturas", "DetalleFacturas", "Pagos", "Devoluciones", "DetalleDevoluciones" },
            ["Compras y proveedores"] = new[] { "Proveedores", "OrdenesCompra", "DetalleOrdenesCompra", "PagosProveedor", "AdjuntosCompras", "DevolucionesProveedor", "DetalleDevolucionesProveedor" },
            ["Finanzas y contabilidad"] = new[] { "CuentasFinancieras", "CategoriasFinancieras", "MovimientosFinancieros", "CierresCaja", "AperturasCaja", "DistribucionesCierre", "AdjuntosCierre", "ConteosCierreCaja", "CierresContables", "ConciliacionesBancarias", "ConciliacionesDetalle", "PlanCuentas", "AsientosContables", "DetallesAsiento", "GastosCajaChica", "Monedas" },
            ["RRHH y nómina"] = new[] { "Empleados", "TiposDocumentoEmpleado", "AdjuntosEmpleado", "ExpedientesEmpleado", "VacacionesEmpleado", "PermisosEmpleado", "ValesEmpleado", "ValesDescuentos", "Nominas", "DetalleNominas", "Feriados", "AlertasPersonalizadas", "ConfiguracionDeducciones", "TramosISR", "PagosNominaEmpleado" },
            ["Logística y reparto"] = new[] { "ZonasEnvio", "Repartidores", "Envios", "Rutas", "RutasHistorial" },
            ["Activos fijos y producción"] = new[] { "ActivosFijos", "DepreciacionesActivo", "MantenimientosActivo", "OrdenesProduccion", "DetallesOrdenProduccion", "InspeccionesCalidad" },
            ["Usuarios y configuración"] = new[] { "Usuarios", "Roles", "Permisos", "SeriesDocumentos", "Impuestos", "MetodosPago", "ConfiguracionEmpresa" },
        };

        /// <summary>Convierte los nombres de módulos elegidos en el conjunto de tablas. Ignora nombres desconocidos.</summary>
        public static HashSet<string> TablasDeGrupos(IEnumerable<string> modulos)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in modulos)
                if (Grupos.TryGetValue(m, out var t)) foreach (var x in t) set.Add(x);
            return set;
        }

        // ---------- Conversión de valores (pública para pruebas) ----------

        public static object? ValorAJson(object? v)
        {
            if (v == null || v is DBNull) return null;
            return v switch
            {
                byte[] b => Convert.ToBase64String(b),
                DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture),
                DateTimeOffset o => o.ToString("o", CultureInfo.InvariantCulture),
                TimeSpan t => t.ToString("c", CultureInfo.InvariantCulture),
                decimal m => m.ToString(CultureInfo.InvariantCulture),
                Guid g => g.ToString(),
                float f => (double)f,
                ulong u => u.ToString(CultureInfo.InvariantCulture),
                bool bo => bo,
                string s => s,
                _ => v.GetType().IsPrimitive ? v : v.ToString()
            };
        }

        public static object? JsonAValor(JsonElement e, string tipo)
        {
            if (e.ValueKind == JsonValueKind.Null || e.ValueKind == JsonValueKind.Undefined) return DBNull.Value;
            switch (tipo)
            {
                case "System.Byte[]": return Convert.FromBase64String(e.GetString() ?? "");
                case "System.DateTime": return DateTime.Parse(e.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.None);
                case "System.DateTimeOffset": return DateTimeOffset.Parse(e.GetString()!, CultureInfo.InvariantCulture);
                case "System.TimeSpan": return TimeSpan.Parse(e.GetString()!, CultureInfo.InvariantCulture);
                case "System.Decimal": return decimal.Parse(e.GetString()!, CultureInfo.InvariantCulture);
                case "System.Guid": return Guid.Parse(e.GetString()!);
                case "System.UInt64": return ulong.Parse(e.ValueKind == JsonValueKind.String ? e.GetString()! : e.GetRawText(), CultureInfo.InvariantCulture);
                case "System.Boolean": return e.ValueKind == JsonValueKind.True || (e.ValueKind == JsonValueKind.Number && e.GetInt64() != 0);
                case "System.String": return e.GetString();
                case "System.Int64": return e.GetInt64();
                case "System.Int32": return e.GetInt32();
                case "System.Int16": return e.GetInt16();
                case "System.SByte": return e.GetSByte();
                case "System.Byte": return e.GetByte();
                case "System.UInt32": return e.GetUInt32();
                case "System.UInt16": return e.GetUInt16();
                case "System.Double": return e.GetDouble();
                case "System.Single": return (float)e.GetDouble();
                default: return e.ValueKind == JsonValueKind.String ? e.GetString() : e.GetRawText();
            }
        }

        // ---------- Utilidades de BD ----------

        private async Task<DbConnection> AbrirAsync()
        {
            await _db.Database.OpenConnectionAsync();
            return _db.Database.GetDbConnection();
        }

        private static async Task<List<string>> TablasAsync(DbConnection cn)
        {
            var lista = new List<string>();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME";
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) { var n = r.GetString(0); if (NombreSeguro.IsMatch(n)) lista.Add(n); }
            return lista;
        }

        private static async Task<string?> UltimaMigracionAsync(DbConnection cn)
        {
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1";
                var o = await cmd.ExecuteScalarAsync();
                return o as string;
            }
            catch { return null; }
        }

        // ---------- Crear respaldo ----------

        public async Task<ResultadoRespaldo> CrearAsync(string etiqueta = "")
        {
            await _candado.WaitAsync();
            var res = new ResultadoRespaldo();
            string? temporal = null;
            try
            {
                var carpeta = Carpeta;
                var nombre = $"{Prefijo}{DateTime.Now:yyyyMMdd_HHmmss}{(etiqueta.Length > 0 ? "_" + Regex.Replace(etiqueta, "[^A-Za-z0-9-]", "") : "")}.zip";
                var destino = Path.Combine(carpeta, nombre);
                temporal = destino + ".tmp";

                var cn = await AbrirAsync();
                var manifiesto = new ManifiestoRespaldo { Fecha = DateTime.Now, UltimaMigracion = await UltimaMigracionAsync(cn) };
                var tablas = (await TablasAsync(cn)).Where(t => !t.Equals("__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase)).ToList();

                using (var fs = new FileStream(temporal, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
                {
                    foreach (var t in tablas)
                    {
                        long filas = 0;
                        var entrada = zip.CreateEntry($"data/{t}.jsonl", CompressionLevel.Optimal);
                        using var es = entrada.Open();
                        using var sw = new StreamWriter(es, new UTF8Encoding(false));
                        using var cmd = cn.CreateCommand();
                        cmd.CommandText = $"SELECT * FROM `{t}`";
                        cmd.CommandTimeout = 0;
                        using var rd = await cmd.ExecuteReaderAsync();
                        var cols = new List<object>();
                        for (int i = 0; i < rd.FieldCount; i++)
                            cols.Add(new { n = rd.GetName(i), t = rd.GetFieldType(i).FullName });
                        await sw.WriteLineAsync(JsonSerializer.Serialize(new { cols }));
                        var fila = new object?[rd.FieldCount];
                        while (await rd.ReadAsync())
                        {
                            for (int i = 0; i < rd.FieldCount; i++)
                                fila[i] = rd.IsDBNull(i) ? null : ValorAJson(rd.GetValue(i));
                            await sw.WriteLineAsync(JsonSerializer.Serialize(fila));
                            filas++;
                        }
                        manifiesto.Tablas[t] = filas;
                        manifiesto.FilasTotales += filas;
                    }

                    foreach (var rel in CarpetasArchivos)
                    {
                        var abs = Path.Combine(_env.ContentRootPath, rel.Replace('/', Path.DirectorySeparatorChar));
                        if (!Directory.Exists(abs)) continue;
                        foreach (var f in Directory.EnumerateFiles(abs, "*", SearchOption.AllDirectories))
                        {
                            // La clave de primer acceso (texto plano) nunca debe viajar dentro de un respaldo
                            if (string.Equals(Path.GetFileName(f), "primer-acceso.txt", StringComparison.OrdinalIgnoreCase)) continue;
                            try
                            {
                                var relArch = Path.GetRelativePath(_env.ContentRootPath, f).Replace('\\', '/');
                                zip.CreateEntryFromFile(f, "archivos/" + relArch, CompressionLevel.Optimal);
                                manifiesto.Archivos++;
                            }
                            catch (IOException ex) { res.Avisos.Add($"No se pudo copiar {Path.GetFileName(f)}: {ex.Message}"); }
                        }
                    }

                    var m = zip.CreateEntry("manifest.json");
                    using var ms = new StreamWriter(m.Open(), new UTF8Encoding(false));
                    await ms.WriteAsync(JsonSerializer.Serialize(manifiesto, new JsonSerializerOptions { WriteIndented = true }));
                }

                // Verificar que el zip se pueda leer y que traiga todas las tablas
                using (var zv = ZipFile.OpenRead(temporal))
                {
                    var man = LeerManifiesto(zv);
                    if (man == null || man.Tablas.Count != manifiesto.Tablas.Count) throw new InvalidDataException("El respaldo creado no pasó la verificación.");
                    foreach (var t in manifiesto.Tablas.Keys)
                        if (zv.GetEntry($"data/{t}.jsonl") == null) throw new InvalidDataException($"Falta la tabla {t} en el respaldo.");
                }

                // Cifrado opcional (Seguridad:ClaveRespaldos): el .zip queda ilegible sin la clave
                if (RespaldoCifrado.Activo)
                {
                    var cifrado = destino + ".cif";
                    try { RespaldoCifrado.CifrarArchivo(temporal, cifrado); }
                    catch { try { File.Delete(cifrado); } catch { } throw; }
                    try { File.Delete(temporal); } catch { }
                    temporal = cifrado;
                }

                if (File.Exists(destino)) File.Delete(destino);
                File.Move(temporal, destino);
                temporal = null;
                var info = new FileInfo(destino);
                res.Ok = true; res.Ruta = destino; res.Bytes = info.Length;
                RespaldoEstado.LimpiarError(carpeta);
                res.Mensaje = $"Respaldo creado: {manifiesto.Tablas.Count} tablas, {manifiesto.FilasTotales:N0} registros y {manifiesto.Archivos} archivos ({info.Length / 1024.0 / 1024.0:N1} MB)." + (RespaldoCifrado.Activo ? " Cifrado con la clave de respaldos." : "");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error creando respaldo del sistema");
                res.Ok = false; res.Mensaje = "No se pudo crear el respaldo: " + ex.Message;
                try { RespaldoEstado.RegistrarError(Carpeta, ex.Message); } catch { }
            }
            finally
            {
                if (temporal != null) { try { File.Delete(temporal); } catch { } }
                try { await _db.Database.CloseConnectionAsync(); } catch { }
                _candado.Release();
            }
            return res;
        }

        /// <summary>Borra los respaldos automáticos viejos (conserva los últimos N). Nunca toca los "previo-restauracion".</summary>
        public void AplicarRetencion(int conservar)
        {
            if (conservar < 1) return;
            foreach (var f in Listar().Where(f => !f.Name.Contains("previo")).Skip(conservar))
            {
                try { f.Delete(); } catch { }
            }
        }

        private static ManifiestoRespaldo? LeerManifiesto(ZipArchive zip)
        {
            var e = zip.GetEntry("manifest.json");
            if (e == null) return null;
            using var s = e.Open();
            try { return JsonSerializer.Deserialize<ManifiestoRespaldo>(s, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
            catch { return null; }
        }

        /// <summary>Lee el manifiesto de un .zip sin restaurar (para mostrar qué trae antes de confirmar).</summary>
        public static ManifiestoRespaldo? Inspeccionar(string ruta)
        {
            try { using var z = ZipFile.OpenRead(ruta); return LeerManifiesto(z); } catch { return null; }
        }

        // ---------- Restaurar ----------

        /// <param name="soloTablas">null = restaura todo; si no, solo esas tablas (restauración por módulo).</param>
        /// <param name="archivos">Restaurar también los archivos subidos (imágenes, adjuntos).</param>
        public async Task<ResultadoRespaldo> RestaurarAsync(string rutaZip, ISet<string>? soloTablas = null, bool archivos = true)
        {
            var res = new ResultadoRespaldo();
            ZipArchive? zip = null;
            string? descifrado = null;
            try
            {
                // Si el respaldo está cifrado se descifra a un archivo temporal (se borra al terminar)
                if (RespaldoCifrado.EstaCifrado(rutaZip))
                {
                    descifrado = RespaldoCifrado.DescifrarATemporal(rutaZip);
                    rutaZip = descifrado;
                }
                zip = ZipFile.OpenRead(rutaZip);
                var man = LeerManifiesto(zip);
                if (man == null || man.Sistema != "Kirkenta") { res.Mensaje = "El archivo no es un respaldo válido de Kirkenta."; return res; }

                // 1) Esquema: si la BD está vacía, crearlo con las migraciones de esta versión.
                var cn0 = await AbrirAsync();
                var existentes0 = await TablasAsync(cn0);
                if (!existentes0.Any(t => !t.Equals("__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase)))
                {
                    try { await _db.Database.MigrateAsync(); res.Avisos.Add("La base estaba vacía: se creó el esquema con las migraciones."); }
                    catch (Exception ex) { res.Mensaje = "La base de datos está vacía y no se pudo crear el esquema: " + ex.Message + " Ejecuta primero 'dotnet ef database update' y vuelve a intentar."; return res; }
                }
                await _db.Database.CloseConnectionAsync();

                // 2) Versión: el respaldo no puede ser de una versión más nueva que este programa.
                var cn = await AbrirAsync();
                var actual = await UltimaMigracionAsync(cn);
                if (man.UltimaMigracion != null && actual != null && string.CompareOrdinal(man.UltimaMigracion, actual) > 0)
                {
                    res.Mensaje = $"Este respaldo es de una versión más nueva del sistema ({man.UltimaMigracion}). Actualiza el sistema antes de restaurar.";
                    return res;
                }
                if (man.UltimaMigracion != null && actual != null && man.UltimaMigracion != actual)
                    res.Avisos.Add("El respaldo es de una versión anterior; las columnas nuevas quedan con su valor por defecto.");

                // 3) Respaldo de seguridad de lo que hay ahora (por si algo sale mal).
                await _db.Database.CloseConnectionAsync();
                var previo = await CrearAsync("previo-restauracion");
                if (!previo.Ok) { res.Mensaje = "No se restauró nada porque no se pudo crear el respaldo de seguridad previo: " + previo.Mensaje; return res; }
                res.Avisos.Add("Antes de restaurar se guardó una copia de lo anterior: " + Path.GetFileName(previo.Ruta));

                // 4) Restaurar tablas en una sola transacción.
                cn = await AbrirAsync();
                var tablasDestino = await TablasAsync(cn);
                var setDestino = new HashSet<string>(tablasDestino, StringComparer.OrdinalIgnoreCase);
                var cargadas = new Dictionary<string, long>();

                using (var tx = await cn.BeginTransactionAsync())
                {
                    try
                    {
                        await EjecutarAsync(cn, tx, "SET FOREIGN_KEY_CHECKS = 0");
                        foreach (var kv in man.Tablas)
                        {
                            var t = kv.Key;
                            if (soloTablas != null && !soloTablas.Contains(t)) continue;
                            if (!NombreSeguro.IsMatch(t) || !setDestino.Contains(t)) { res.Avisos.Add($"Tabla {t} no existe en este sistema: se omitió."); continue; }
                            var entrada = zip.GetEntry($"data/{t}.jsonl");
                            if (entrada == null) throw new InvalidDataException($"Falta la tabla {t} dentro del respaldo.");
                            cargadas[t] = await RestaurarTablaAsync(cn, tx, t, entrada);
                        }
                        await EjecutarAsync(cn, tx, "SET FOREIGN_KEY_CHECKS = 1");

                        // Verificación de conteos antes de confirmar
                        foreach (var kv in cargadas)
                        {
                            using var c = cn.CreateCommand(); c.Transaction = tx;
                            c.CommandText = $"SELECT COUNT(*) FROM `{kv.Key}`";
                            var n = Convert.ToInt64(await c.ExecuteScalarAsync());
                            if (n != man.Tablas[kv.Key]) throw new InvalidDataException($"La tabla {kv.Key} quedó con {n} registros y el respaldo tenía {man.Tablas[kv.Key]}.");
                        }
                        await tx.CommitAsync();
                    }
                    catch
                    {
                        try { await tx.RollbackAsync(); } catch { }
                        try { await EjecutarAsync(cn, null, "SET FOREIGN_KEY_CHECKS = 1"); } catch { }
                        throw;
                    }
                }

                // Los ajustes manuales de actualización se vuelven a evaluar (son idempotentes) en el próximo arranque.
                try { await EjecutarAsync(cn, null, "DELETE FROM `__KirkentaEsquema`"); } catch { /* la tabla puede no existir aún */ }
                if (ActualizacionBD.PasosManuales.Count > 0)
                    res.Avisos.Add("Reinicia el sistema para que se reapliquen los ajustes de actualización sobre los datos restaurados.");

                // 5) Archivos
                int copiados = 0;
                foreach (var e in zip.Entries.Where(x => archivos && x.FullName.StartsWith("archivos/", StringComparison.Ordinal) && x.Name.Length > 0))
                {
                    var rel = e.FullName.Substring("archivos/".Length);
                    var destino = Path.GetFullPath(Path.Combine(_env.ContentRootPath, rel));
                    var raiz = Path.GetFullPath(_env.ContentRootPath) + Path.DirectorySeparatorChar;
                    if (!destino.StartsWith(raiz, StringComparison.OrdinalIgnoreCase)) continue; // anti zip-slip
                    if (!ArchivoPermitidoAlRestaurar(rel)) { res.Avisos.Add($"Se omitió {Path.GetFileName(rel)} (tipo de archivo no permitido)."); continue; }
                    if (!CarpetasArchivos.Any(c => rel.Replace('\\', '/').StartsWith(c + "/", StringComparison.OrdinalIgnoreCase))) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
                    e.ExtractToFile(destino, overwrite: true);
                    copiados++;
                }

                var filas = cargadas.Values.Sum();
                res.Ok = true;
                res.Mensaje = $"{(soloTablas == null ? "Restauración completa" : "Restauración parcial")}: {cargadas.Count} tablas, {filas:N0} registros y {copiados} archivos. Cierra sesión y vuelve a entrar.";
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error restaurando respaldo");
                res.Ok = false;
                res.Mensaje = "La restauración falló y NO se cambió nada (se revirtió): " + ex.Message;
            }
            finally
            {
                zip?.Dispose();
                if (descifrado != null) { try { File.Delete(descifrado); } catch { } }
                try { await _db.Database.CloseConnectionAsync(); } catch { }
            }
            return res;
        }

        // Un respaldo manipulado no debe poder colar programas ni páginas ejecutables dentro de la carpeta del sistema.
        private static readonly string[] ExtensionesProhibidasAlRestaurar =
            { ".exe", ".dll", ".bat", ".cmd", ".ps1", ".vbs", ".msi", ".com", ".scr", ".js", ".mjs", ".html", ".htm", ".aspx", ".cshtml", ".php", ".jar" };

        public static bool ArchivoPermitidoAlRestaurar(string rutaRelativa)
        {
            var rel = (rutaRelativa ?? "").Replace('\\', '/');
            var nombre = Path.GetFileName(rel);
            if (string.Equals(nombre, "primer-acceso.txt", StringComparison.OrdinalIgnoreCase)) return false;

            var ext = Path.GetExtension(nombre).ToLowerInvariant();
            if (ExtensionesProhibidasAlRestaurar.Contains(ext)) return false;

            // Un .svg subido a /uploads se abriría directo en el navegador y podría ejecutar scripts
            if (ext == ".svg" && rel.StartsWith("wwwroot/uploads/", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private static async Task EjecutarAsync(DbConnection cn, DbTransaction? tx, string sql)
        {
            using var c = cn.CreateCommand(); c.Transaction = tx; c.CommandText = sql;
            await c.ExecuteNonQueryAsync();
        }

        /// <summary>Valor neutro para llenar una columna obligatoria que un respaldo viejo no trae. null = no se pudo elegir.</summary>
        public static object? ValorNeutro(string dataType, string columnType)
        {
            switch ((dataType ?? "").ToLowerInvariant())
            {
                case "int": case "bigint": case "smallint": case "mediumint": case "tinyint": case "decimal": case "numeric": case "double": case "float": case "bit":
                    return 0;
                case "varchar": case "text": case "tinytext": case "mediumtext": case "longtext":
                    return "";
                case "char":
                    return (columnType ?? "").StartsWith("char(36)", StringComparison.OrdinalIgnoreCase) ? "00000000-0000-0000-0000-000000000000" : "";
                case "datetime": case "timestamp": return DateTime.Now;
                case "date": return DateTime.Today;
                case "time": return TimeSpan.Zero;
                case "json": return "{}";
                case "blob": case "tinyblob": case "mediumblob": case "longblob": return Array.Empty<byte>();
                default: return null;
            }
        }

        private static async Task<long> RestaurarTablaAsync(DbConnection cn, DbTransaction tx, string tabla, ZipArchiveEntry entrada)
        {
            // Columnas reales de la tabla destino
            var destinoCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Columnas obligatorias (NOT NULL, sin DEFAULT, no autonuméricas) del destino: si el respaldo es viejo y no las trae,
            // se llenan con un valor neutro para que la restauración no falle.
            var obligatorias = new List<(string Nombre, object Valor)>();
            using (var c = cn.CreateCommand())
            {
                c.Transaction = tx;
                c.CommandText = "SELECT COLUMN_NAME, IS_NULLABLE, COLUMN_DEFAULT, EXTRA, DATA_TYPE, COLUMN_TYPE FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t";
                var p = c.CreateParameter(); p.ParameterName = "@t"; p.Value = tabla; c.Parameters.Add(p);
                using var r = await c.ExecuteReaderAsync();
                var candidatas = new List<(string, object)>();
                while (await r.ReadAsync())
                {
                    var nombreCol = r.GetString(0);
                    destinoCols.Add(nombreCol);
                    var noNulo = string.Equals(r.GetString(1), "NO", StringComparison.OrdinalIgnoreCase);
                    var sinDefault = r.IsDBNull(2);
                    var extra = r.IsDBNull(3) ? "" : r.GetString(3);
                    if (!noNulo || !sinDefault) continue;
                    if (extra.Contains("auto_increment", StringComparison.OrdinalIgnoreCase) || extra.Contains("GENERATED", StringComparison.OrdinalIgnoreCase)) continue;
                    var v = ValorNeutro(r.GetString(4), r.GetString(5));
                    if (v != null && NombreSeguro.IsMatch(nombreCol)) candidatas.Add((nombreCol, v));
                }
                obligatorias.AddRange(candidatas);
            }

            await EjecutarAsync(cn, tx, $"DELETE FROM `{tabla}`");

            using var sr = new StreamReader(entrada.Open(), new UTF8Encoding(false));
            var cabecera = await sr.ReadLineAsync();
            if (cabecera == null) return 0;
            using var doc = JsonDocument.Parse(cabecera);
            var nombres = new List<string>(); var tipos = new List<string>();
            foreach (var col in doc.RootElement.GetProperty("cols").EnumerateArray())
            {
                nombres.Add(col.GetProperty("n").GetString() ?? ""); tipos.Add(col.GetProperty("t").GetString() ?? "");
            }
            var usar = Enumerable.Range(0, nombres.Count).Where(i => NombreSeguro.IsMatch(nombres[i]) && destinoCols.Contains(nombres[i])).ToList();
            if (usar.Count == 0) return 0;

            var enRespaldo = new HashSet<string>(nombres, StringComparer.OrdinalIgnoreCase);
            var extras = obligatorias.Where(o => !enRespaldo.Contains(o.Nombre)).ToList();

            var lista = string.Join(",", usar.Select(i => $"`{nombres[i]}`").Concat(extras.Select(e => $"`{e.Nombre}`")));
            int porLote = Math.Max(1, 1500 / (usar.Count + extras.Count));
            var lote = new List<object?[]>();
            long total = 0;

            async Task VaciarLoteAsync()
            {
                if (lote.Count == 0) return;
                var sb = new StringBuilder($"INSERT INTO `{tabla}` ({lista}) VALUES ");
                using var cmd = cn.CreateCommand(); cmd.Transaction = tx; cmd.CommandTimeout = 0;
                int k = 0;
                for (int f = 0; f < lote.Count; f++)
                {
                    if (f > 0) sb.Append(',');
                    sb.Append('(');
                    for (int j = 0; j < usar.Count + extras.Count; j++)
                    {
                        if (j > 0) sb.Append(',');
                        var nombreP = "@p" + k++;
                        sb.Append(nombreP);
                        var p = cmd.CreateParameter(); p.ParameterName = nombreP;
                        p.Value = j < usar.Count ? (lote[f][j] ?? DBNull.Value) : extras[j - usar.Count].Valor;
                        cmd.Parameters.Add(p);
                    }
                    sb.Append(')');
                }
                cmd.CommandText = sb.ToString();
                await cmd.ExecuteNonQueryAsync();
                total += lote.Count;
                lote.Clear();
            }

            string? linea;
            while ((linea = await sr.ReadLineAsync()) != null)
            {
                if (linea.Length == 0) continue;
                var vals = JsonSerializer.Deserialize<JsonElement[]>(linea)!;
                var fila = new object?[usar.Count];
                for (int j = 0; j < usar.Count; j++) fila[j] = JsonAValor(vals[usar[j]], tipos[usar[j]]);
                lote.Add(fila);
                if (lote.Count >= porLote) await VaciarLoteAsync();
            }
            await VaciarLoteAsync();
            return total;
        }
    }
}
