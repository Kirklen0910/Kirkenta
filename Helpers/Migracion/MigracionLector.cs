using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Kirkenta.Helpers.Import;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>
    /// Lee un archivo del sistema viejo y devuelve TODAS sus tablas: JSON (varios formatos),
    /// JSONL, .zip (incluye los respaldos de Kirkenta), Excel (todas las hojas), CSV/TSV y
    /// volcados SQL (mysqldump / pg_dump / SQL Server "INSERT").
    /// </summary>
    public static class MigracionLector
    {
        public const long MaxEntradaBytes = 200L * 1024 * 1024;
        public const int MaxFilasPorTabla = 300_000;
        private const int MaxEntradasZip = 500;

        public static ResultadoMigracionLectura Leer(string nombreArchivo, Stream stream)
        {
            var r = new ResultadoMigracionLectura();
            try { LeerInterno(nombreArchivo, stream, r, 0); }
            catch (Exception ex)
            {
                r.Avisos.Add($"No se pudo leer «{Path.GetFileName(nombreArchivo)}»: {ex.Message}");
            }
            return r;
        }

        private static void LeerInterno(string nombre, Stream stream, ResultadoMigracionLectura r, int nivel)
        {
            var ext = Path.GetExtension(nombre).ToLowerInvariant();
            var baseNombre = Path.GetFileNameWithoutExtension(nombre);

            switch (ext)
            {
                case ".zip":
                    if (nivel > 0) { r.Avisos.Add($"«{nombre}»: no se leen .zip dentro de otro .zip."); return; }
                    LeerZip(stream, r);
                    break;
                case ".json":
                    LeerJson(baseNombre, stream, r);
                    break;
                case ".jsonl":
                case ".ndjson":
                    LeerJsonLineas(baseNombre, stream, r);
                    break;
                case ".xlsx":
                    LeerExcel(stream, r);
                    break;
                case ".csv":
                case ".tsv":
                case ".txt":
                    var csv = CsvImporter.Read(stream);
                    if (csv.Ok) r.Agregar(baseNombre, csv.Headers, csv.Rows);
                    else r.Avisos.Add($"«{nombre}»: {csv.Error}");
                    break;
                case ".sql":
                    LeerSql(stream, r);
                    break;
                case ".xls":
                    MigracionBasesDatos.LeerXls(stream, r);
                    break;
                case ".dbf":
                    MigracionBasesDatos.LeerDbf(baseNombre, stream, r);
                    break;
                case ".db":
                case ".sqlite":
                case ".sqlite3":
                case ".db3":
                    {
                        using var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        var bytes = ms.ToArray();
                        if (MigracionBasesDatos.EsSqlite(bytes)) MigracionBasesDatos.LeerSqlite(bytes, r);
                        else r.Avisos.Add($"«{nombre}»: no es una base SQLite válida.");
                        break;
                    }
                case ".mdb":
                case ".accdb":
                    r.Avisos.Add($"«{nombre}»: las bases de Access no se pueden leer directamente. Ábrela en Access y exporta cada tabla a Excel (.xlsx) o CSV, o a .sql, y súbelas (puedes meterlas todas en un .zip).");
                    break;
                case ".bak":
                case ".mdf":
                case ".ldf":
                case ".bacpac":
                    r.Avisos.Add($"«{nombre}»: los respaldos binarios de SQL Server no se pueden leer sin restaurarlos. Restáuralo en SQL Server y genera el script de solo datos (Generate Scripts → Types of data to script: Data only) y súbelo como .sql.");
                    break;
                default:
                    r.Avisos.Add($"«{nombre}»: formato {(ext.Length == 0 ? "desconocido" : ext)} no soportado " +
                                 "(usa .json, .zip, .xlsx, .xls, .csv, .sql, .dbf o SQLite).");
                    break;
            }
        }

        /// <summary>Lee todo el flujo detectando la codificación (UTF-8, UTF-16 o ANSI/Latin-1 de sistemas viejos).</summary>
        private static string LeerTexto(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return CsvImporter.Decodificar(ms.ToArray());
        }

        // ---------- ZIP ----------
        private static void LeerZip(Stream stream, ResultadoMigracionLectura r)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            if (zip.Entries.Count > MaxEntradasZip)
            {
                r.Avisos.Add($"El .zip tiene {zip.Entries.Count} archivos; se leerán solo los primeros {MaxEntradasZip}.");
            }

            foreach (var e in zip.Entries.Take(MaxEntradasZip))
            {
                if (string.IsNullOrEmpty(e.Name)) continue; // carpeta
                var ruta = e.FullName.Replace('\\', '/');
                if (ruta.StartsWith("archivos/", StringComparison.OrdinalIgnoreCase)) continue; // imágenes del respaldo Kirkenta
                if (ruta.StartsWith("__MACOSX", StringComparison.OrdinalIgnoreCase)) continue;
                if (e.Name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase)) continue;

                var ext = Path.GetExtension(e.Name).ToLowerInvariant();
                if (ext is not (".json" or ".jsonl" or ".ndjson" or ".csv" or ".tsv" or ".txt" or ".xlsx" or ".xls" or ".sql"
                    or ".dbf" or ".db" or ".sqlite" or ".sqlite3" or ".db3" or ".mdb" or ".accdb" or ".bak" or ".mdf")) continue;

                if (e.Length > MaxEntradaBytes)
                {
                    r.Avisos.Add($"«{e.Name}» pesa demasiado (>{MaxEntradaBytes / 1024 / 1024} MB) y se omitió.");
                    continue;
                }

                using var origen = e.Open();
                var ms = new MemoryStream();
                origen.CopyTo(ms);
                ms.Position = 0;
                try { LeerInterno(e.Name, ms, r, 1); }
                catch (Exception ex) { r.Avisos.Add($"«{e.Name}»: {ex.Message}"); }
            }
        }

        // ---------- JSON ----------
        private static void LeerJson(string baseNombre, Stream stream, ResultadoMigracionLectura r)
        {
            using var doc = JsonDocument.Parse(LeerTexto(stream), new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
            int antes = r.Tablas.Count;
            ExtraerTablas(doc.RootElement, baseNombre, r, 0);
            if (r.Tablas.Count == antes)
                r.Avisos.Add($"«{baseNombre}.json»: no se encontraron listas de registros.");
        }

        private static void ExtraerTablas(JsonElement el, string nombre, ResultadoMigracionLectura r, int profundidad)
        {
            if (profundidad > 4) return;

            if (el.ValueKind == JsonValueKind.Array)
            {
                var objetos = el.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object).ToList();
                if (objetos.Count == 0) return;

                // Formato phpMyAdmin: [ {"type":"table","name":"x","data":[...]} ]
                bool esPhpMyAdmin = objetos.Any(o => o.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String
                    && t.GetString() == "table" && o.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array);
                if (esPhpMyAdmin)
                {
                    foreach (var o in objetos)
                    {
                        if (o.TryGetProperty("type", out var t) && t.GetString() == "table"
                            && o.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String
                            && o.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array)
                            ExtraerTablas(d, n.GetString() ?? nombre, r, profundidad + 1);
                    }
                    return;
                }

                ConstruirTabla(nombre, objetos, r);
            }
            else if (el.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in el.EnumerateObject())
                {
                    if (p.Value.ValueKind == JsonValueKind.Array || p.Value.ValueKind == JsonValueKind.Object)
                        ExtraerTablas(p.Value, p.Name, r, profundidad + 1);
                }
            }
        }

        private static void LeerJsonLineas(string baseNombre, Stream stream, ResultadoMigracionLectura r)
        {
            var objetos = new List<JsonElement>();
            var docs = new List<JsonDocument>();
            try
            {
                using var sr = new StringReader(LeerTexto(stream));
                string? linea;
                while ((linea = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(linea)) continue;
                    if (objetos.Count >= MaxFilasPorTabla) { r.Avisos.Add($"«{baseNombre}»: se leyeron solo las primeras {MaxFilasPorTabla:N0} filas."); break; }
                    JsonDocument d;
                    try { d = JsonDocument.Parse(linea); } catch (JsonException) { continue; }
                    docs.Add(d);
                    if (d.RootElement.ValueKind == JsonValueKind.Object) objetos.Add(d.RootElement);
                }
                ConstruirTabla(baseNombre, objetos, r);
            }
            finally { foreach (var d in docs) d.Dispose(); }
        }

        private static void ConstruirTabla(string nombre, List<JsonElement> objetos, ResultadoMigracionLectura r)
        {
            var headers = new List<string>();
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var o in objetos)
                foreach (var p in o.EnumerateObject())
                    if (vistos.Add(p.Name)) headers.Add(p.Name);

            var filas = new List<Dictionary<string, string>>(objetos.Count);
            foreach (var o in objetos)
            {
                var f = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var h in headers) f[h] = "";
                foreach (var p in o.EnumerateObject()) f[p.Name] = ValorTexto(p.Value);
                if (f.Values.Any(v => v.Length > 0)) filas.Add(f);
            }
            r.Agregar(nombre, headers, filas);
        }

        private static string ValorTexto(JsonElement v) => v.ValueKind switch
        {
            JsonValueKind.String => (v.GetString() ?? "").Trim(),
            JsonValueKind.Number => v.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null or JsonValueKind.Undefined => "",
            _ => v.GetRawText()
        };

        // ---------- Excel (todas las hojas) ----------
        private static void LeerExcel(Stream stream, ResultadoMigracionLectura r)
        {
            using var wb = new XLWorkbook(stream);
            foreach (var hoja in wb.Worksheets)
            {
                var rango = hoja.RangeUsed();
                if (rango == null) continue;

                int c1 = rango.FirstColumn().ColumnNumber(), c2 = rango.LastColumn().ColumnNumber();
                int f1 = rango.FirstRow().RowNumber(), f2 = rango.LastRow().RowNumber();

                // Encabezado = primera fila con algún texto
                int filaH = f1;
                for (; filaH <= f2; filaH++)
                {
                    bool algo = false;
                    for (int c = c1; c <= c2 && !algo; c++) algo = ExcelImporter.TextoCelda(hoja.Cell(filaH, c)).Length > 0;
                    if (algo) break;
                }
                if (filaH >= f2) continue;

                var columnas = new List<int>();
                var nombres = new List<string>();
                for (int c = c1; c <= c2; c++)
                {
                    var t = ExcelImporter.TextoCelda(hoja.Cell(filaH, c));
                    if (t.Length == 0) continue;
                    columnas.Add(c); nombres.Add(t);
                }
                var headers = ImportFile.HacerHeadersUnicos(nombres);

                var filas = new List<Dictionary<string, string>>();
                for (int fr = filaH + 1; fr <= f2 && filas.Count < MaxFilasPorTabla; fr++)
                {
                    var fila = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int k = 0; k < columnas.Count; k++)
                        fila[headers[k]] = ExcelImporter.TextoCelda(hoja.Cell(fr, columnas[k]));
                    if (fila.Values.Any(v => v.Length > 0)) filas.Add(fila);
                }
                r.Agregar(hoja.Name, headers, filas);
            }
        }

        // ---------- SQL ----------
        private static void LeerSql(Stream stream, ResultadoMigracionLectura r)
        {
            var texto = LeerTexto(stream);
            int antes = r.Tablas.Count;
            foreach (var t in SqlDumpParser.Parse(texto, r.Avisos))
                r.Agregar(t.Nombre, t.Headers, t.Rows);
            if (r.Tablas.Count == antes)
                r.Avisos.Add("El archivo .sql no contiene sentencias INSERT/COPY con datos.");
        }
    }
}
