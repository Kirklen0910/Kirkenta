using System.Globalization;
using System.Text;
using ExcelDataReader;
using Microsoft.Data.Sqlite;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>
    /// Lectores de bases de datos de archivo de sistemas viejos: dBase/FoxPro (.dbf), SQLite (.db/.sqlite) y Excel 97-2003 (.xls).
    /// Solo leen, nunca ejecutan ni modifican nada.
    /// </summary>
    public static class MigracionBasesDatos
    {
        private static readonly byte[] CabeceraSqlite = Encoding.ASCII.GetBytes("SQLite format 3\0");

        public static bool EsSqlite(byte[] bytes) =>
            bytes.Length >= CabeceraSqlite.Length && bytes.AsSpan(0, CabeceraSqlite.Length).SequenceEqual(CabeceraSqlite);

        // ---------------- DBF (dBase III/IV, FoxPro, Clipper) ----------------
        public static void LeerDbf(string nombreTabla, Stream stream, ResultadoMigracionLectura r)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var b = ms.ToArray();
            if (b.Length < 33) { r.Avisos.Add($"«{nombreTabla}.dbf» está vacío o dañado."); return; }

            long registros = BitConverter.ToUInt32(b, 4);
            int cabecera = BitConverter.ToUInt16(b, 8);
            int largoReg = BitConverter.ToUInt16(b, 10);
            if (cabecera < 33 || largoReg < 2 || cabecera >= b.Length) { r.Avisos.Add($"«{nombreTabla}.dbf» no tiene un formato dBase válido."); return; }

            Encoding enc = Encoding.Latin1;
            if (b[29] == 0x02 || b[29] == 0x01 || b[29] == 0xC8)
            {
                try
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    enc = Encoding.GetEncoding(b[29] == 0x02 ? 850 : b[29] == 0x01 ? 437 : 1250);
                }
                catch { enc = Encoding.Latin1; }
            }

            var campos = new List<(string Nombre, char Tipo, int Pos, int Largo, int Dec)>();
            int pos = 1;
            for (int o = 32; o + 32 <= cabecera && b[o] != 0x0D; o += 32)
            {
                var nombre = enc.GetString(b, o, 11).Split('\0')[0].Trim();
                char tipo = (char)b[o + 11];
                int largo = b[o + 16], dec = b[o + 17];
                if (nombre.Length == 0) nombre = "campo" + (campos.Count + 1);
                campos.Add((nombre, tipo, pos, largo, dec));
                pos += largo;
            }
            if (campos.Count == 0) { r.Avisos.Add($"«{nombreTabla}.dbf» no tiene columnas."); return; }

            var headers = ImportFileHeaders(campos.Select(c => c.Nombre));
            var filas = new List<Dictionary<string, string>>();
            for (long i = 0; i < registros && filas.Count < MigracionLector.MaxFilasPorTabla; i++)
            {
                long ini = cabecera + i * largoReg;
                if (ini + largoReg > b.Length) break;
                if (b[ini] == (byte)'*') continue; // registro borrado
                var fila = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int k = 0; k < campos.Count; k++)
                {
                    var c = campos[k];
                    fila[headers[k]] = ValorDbf(b, (int)ini + c.Pos, c, enc);
                }
                if (fila.Values.Any(v => v.Length > 0)) filas.Add(fila);
            }
            r.Agregar(nombreTabla, headers, filas);
        }

        private static string ValorDbf(byte[] b, int ini, (string Nombre, char Tipo, int Pos, int Largo, int Dec) c, Encoding enc)
        {
            try
            {
                switch (c.Tipo)
                {
                    case 'I' when c.Largo == 4: return BitConverter.ToInt32(b, ini).ToString(CultureInfo.InvariantCulture);
                    case 'Y' when c.Largo == 8: return (BitConverter.ToInt64(b, ini) / 10000m).ToString(CultureInfo.InvariantCulture);
                    case 'B' when c.Largo == 8: return BitConverter.ToDouble(b, ini).ToString("0.################", CultureInfo.InvariantCulture);
                    case 'T' when c.Largo == 8:
                        {
                            int dia = BitConverter.ToInt32(b, ini), ms = BitConverter.ToInt32(b, ini + 4);
                            if (dia <= 0) return "";
                            var f = new DateTime(1, 1, 1).AddDays(dia - 1721426).AddMilliseconds(ms);
                            return f.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        }
                    case 'M': case 'G': case 'P': return ""; // memo / binarios: viven en otro archivo
                }
                var t = enc.GetString(b, ini, c.Largo).Replace("\0", "").Trim();
                if (c.Tipo == 'D' && t.Length == 8 && t.All(char.IsDigit))
                    return $"{t[..4]}-{t.Substring(4, 2)}-{t.Substring(6, 2)}";
                if (c.Tipo == 'L') return t is "T" or "t" or "Y" or "y" ? "true" : t is "F" or "f" or "N" or "n" ? "false" : "";
                return t;
            }
            catch { return ""; }
        }

        // ---------------- SQLite ----------------
        public static void LeerSqlite(byte[] bytes, ResultadoMigracionLectura r)
        {
            var temporal = Path.Combine(Path.GetTempPath(), "kk-mig-" + Guid.NewGuid().ToString("N") + ".db");
            File.WriteAllBytes(temporal, bytes);
            try
            {
                using (var cn = new SqliteConnection($"Data Source={temporal};Mode=ReadOnly;Pooling=False"))
                {
                    cn.Open();
                    var tablas = new List<string>();
                    using (var cmd = cn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
                        using var rd = cmd.ExecuteReader();
                        while (rd.Read()) tablas.Add(rd.GetString(0));
                    }
                    foreach (var t in tablas)
                    {
                        using var cmd = cn.CreateCommand();
                        cmd.CommandText = $"SELECT * FROM \"{t.Replace("\"", "\"\"")}\" LIMIT {MigracionLector.MaxFilasPorTabla}";
                        using var rd = cmd.ExecuteReader();
                        var nombres = new List<string>();
                        for (int i = 0; i < rd.FieldCount; i++) nombres.Add(rd.GetName(i));
                        var headers = ImportFileHeaders(nombres);
                        var filas = new List<Dictionary<string, string>>();
                        while (rd.Read())
                        {
                            var fila = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for (int i = 0; i < headers.Count; i++)
                            {
                                if (rd.IsDBNull(i)) { fila[headers[i]] = ""; continue; }
                                var v = rd.GetValue(i);
                                fila[headers[i]] = v switch
                                {
                                    byte[] => "",
                                    double d => d.ToString("0.################", CultureInfo.InvariantCulture),
                                    string s => s.Trim(),
                                    _ => Convert.ToString(v, CultureInfo.InvariantCulture) ?? ""
                                };
                            }
                            if (fila.Values.Any(x => x.Length > 0)) filas.Add(fila);
                        }
                        r.Agregar(t, headers, filas);
                    }
                    if (tablas.Count == 0) r.Avisos.Add("La base SQLite no tiene tablas.");
                }
                SqliteConnection.ClearAllPools();
            }
            finally { try { File.Delete(temporal); } catch { } }
        }

        // ---------------- Excel 97-2003 (.xls) ----------------
        public static void LeerXls(Stream stream, ResultadoMigracionLectura r)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            do
            {
                var filasCrudas = new List<string[]>();
                while (reader.Read() && filasCrudas.Count <= MigracionLector.MaxFilasPorTabla)
                {
                    var f = new string[reader.FieldCount];
                    for (int i = 0; i < f.Length; i++) f[i] = TextoXls(reader.GetValue(i));
                    filasCrudas.Add(f);
                }
                // Encabezado = primera fila con algún texto
                int h = filasCrudas.FindIndex(f => f.Any(x => x.Length > 0));
                if (h < 0 || h >= filasCrudas.Count - 1) continue;
                var cols = Enumerable.Range(0, filasCrudas[h].Length).Where(i => filasCrudas[h][i].Length > 0).ToList();
                var headers = ImportFileHeaders(cols.Select(i => filasCrudas[h][i]));
                var filas = new List<Dictionary<string, string>>();
                for (int k = h + 1; k < filasCrudas.Count; k++)
                {
                    var fila = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int j = 0; j < cols.Count; j++)
                        fila[headers[j]] = cols[j] < filasCrudas[k].Length ? filasCrudas[k][cols[j]] : "";
                    if (fila.Values.Any(x => x.Length > 0)) filas.Add(fila);
                }
                r.Agregar(reader.Name, headers, filas);
            } while (reader.NextResult());
        }

        private static string TextoXls(object? v) => v switch
        {
            null => "",
            DateTime d => d.TimeOfDay == TimeSpan.Zero ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            double n => n.ToString("0.################", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => (Convert.ToString(v, CultureInfo.InvariantCulture) ?? "").Trim()
        };

        private static List<string> ImportFileHeaders(IEnumerable<string> nombres)
            => Kirkenta.Helpers.Import.ImportFile.HacerHeadersUnicos(nombres.ToList());
    }
}
