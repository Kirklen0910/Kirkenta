using System.Text;
using System.Text.RegularExpressions;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>
    /// Lee un volcado SQL de texto (mysqldump, pg_dump, SQL Server "Generate scripts"...) SIN ejecutarlo:
    /// solo extrae los datos de los INSERT / COPY y los nombres de columnas de los CREATE TABLE.
    /// </summary>
    public static class SqlDumpParser
    {
        private static readonly Regex Inicio = new(
            @"\b(CREATE\s+(?:TEMPORARY\s+)?TABLE|INSERT\s+(?:IGNORE\s+)?INTO|COPY)\s+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HashSet<string> PalabrasClave = new(StringComparer.OrdinalIgnoreCase)
        {
            "PRIMARY", "KEY", "UNIQUE", "CONSTRAINT", "INDEX", "FOREIGN", "FULLTEXT", "CHECK", "SPATIAL", "PERIOD", "EXCLUDE"
        };

        /// <summary>Sin límite: el único tope es la memoria del equipo.</summary>
        public const long MaxFilasTotales = long.MaxValue;

        public static List<TablaOrigen> Parse(string sql, List<string>? avisos = null)
        {
            var e = new Estado();
            e.Procesar(sql, avisos);
            return e.Tablas();
        }

        private static readonly Regex InicioCopy = new(@"^\s*COPY\s.+\bFROM\s+stdin", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Lee un volcado de cualquier tamaño por bloques (de ~32 MB, cortando siempre al final de una sentencia),
        /// sin cargar el archivo entero en memoria como un solo texto.
        /// </summary>
        public static List<TablaOrigen> Parse(TextReader lector, List<string>? avisos = null, int bloque = 32 * 1024 * 1024)
        {
            var e = new Estado();
            var sb = new StringBuilder();
            bool enCopy = false, enCadena = false;
            string? linea;
            while ((linea = lector.ReadLine()) != null)
            {
                sb.Append(linea).Append('\n');
                if (enCopy) { if (linea.TrimEnd('\r') == "\\.") enCopy = false; continue; }
                if (!enCadena && InicioCopy.IsMatch(linea)) { enCopy = true; continue; }
                var t = linea.TrimStart();
                if (!enCadena && (t.StartsWith("--") || t.StartsWith("#"))) continue;
                enCadena = DentroDeCadena(linea, enCadena);
                if (!enCadena && sb.Length >= bloque && linea.TrimEnd().EndsWith(";"))
                {
                    e.Procesar(sb.ToString(), avisos);
                    sb.Clear();
                }
            }
            if (sb.Length > 0) e.Procesar(sb.ToString(), avisos);
            return e.Tablas();
        }

        /// <summary>¿La línea termina dentro de un texto entre comillas simples? (respeta \' y '').</summary>
        private static bool DentroDeCadena(string linea, bool dentro)
        {
            for (int i = 0; i < linea.Length; i++)
            {
                var c = linea[i];
                if (!dentro) { if (c == '\'') dentro = true; continue; }
                if (c == '\\') { i++; continue; }
                if (c == '\'')
                {
                    if (i + 1 < linea.Length && linea[i + 1] == '\'') { i++; continue; }
                    dentro = false;
                }
            }
            return dentro;
        }

        /// <summary>Lo leído hasta ahora: sigue entre bloques (columnas de los CREATE, tablas y filas).</summary>
        private class Estado
        {
            private readonly Dictionary<string, List<string>> columnasCreate = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, TablaOrigen> tablas = new(StringComparer.OrdinalIgnoreCase);
            private readonly List<string> orden = new();
            private long total;

            public List<TablaOrigen> Tablas() => orden.Select(n => tablas[n]).Where(t => t.Rows.Count > 0).ToList();

        public void Procesar(string sql, List<string>? avisos)
        {
            int pos = 0;

            while (pos < sql.Length)
            {
                var m = Inicio.Match(sql, pos);
                if (!m.Success) break;
                int i = m.Index + m.Length;
                var tipo = m.Groups[1].Value.ToUpperInvariant();

                if (tipo.StartsWith("CREATE"))
                {
                    SaltarFrase(sql, ref i, "IF NOT EXISTS");
                    var nombre = LeerIdent(sql, ref i);
                    SaltarEspacios(sql, ref i);
                    if (nombre.Length > 0 && i < sql.Length && sql[i] == '(')
                    {
                        var cuerpo = LeerParentesis(sql, ref i);
                        columnasCreate[nombre] = ColumnasDeCreate(cuerpo);
                    }
                    pos = Math.Max(i, m.Index + m.Length);
                    continue;
                }

                if (tipo.StartsWith("INSERT"))
                {
                    var nombre = LeerIdent(sql, ref i);
                    SaltarEspacios(sql, ref i);
                    List<string>? cols = null;
                    if (i < sql.Length && sql[i] == '(')
                    {
                        cols = SepararSuperior(LeerParentesis(sql, ref i)).Select(LimpiarIdent).ToList();
                        SaltarEspacios(sql, ref i);
                    }

                    int largo = Coincide(sql, i, "VALUES") ? 6 : Coincide(sql, i, "VALUE") ? 5 : 0;
                    if (nombre.Length == 0 || largo == 0) { pos = Math.Max(i, m.Index + m.Length); continue; }
                    i += largo;

                    cols ??= columnasCreate.GetValueOrDefault(nombre);
                    var tabla = ObtenerTabla(tablas, orden, nombre);

                    while (true)
                    {
                        SaltarEspacios(sql, ref i);
                        if (i >= sql.Length || sql[i] != '(') break;
                        var valores = LeerTupla(sql, ref i);
                        if (total >= MaxFilasTotales)
                        {
                            if (total == MaxFilasTotales) avisos?.Add($"El volcado es muy grande: se leyeron solo las primeras {MaxFilasTotales:N0} filas.");
                            total++;
                        }
                        else
                        {
                            AgregarFila(tabla, cols, valores);
                            total++;
                        }
                        SaltarEspacios(sql, ref i);
                        if (i < sql.Length && sql[i] == ',') { i++; continue; }
                        break;
                    }
                    pos = Math.Max(i, m.Index + m.Length);
                    continue;
                }

                // COPY tabla (cols) FROM stdin;   (PostgreSQL)
                {
                    var nombre = LeerIdent(sql, ref i);
                    SaltarEspacios(sql, ref i);
                    List<string>? cols = null;
                    if (i < sql.Length && sql[i] == '(')
                    {
                        cols = SepararSuperior(LeerParentesis(sql, ref i)).Select(LimpiarIdent).ToList();
                    }
                    int finLinea = sql.IndexOf('\n', i);
                    if (finLinea < 0) break;
                    var resto = sql.Substring(i, finLinea - i);
                    if (nombre.Length == 0 || resto.IndexOf("stdin", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        pos = finLinea + 1; continue;
                    }

                    var tabla = ObtenerTabla(tablas, orden, nombre);
                    int p = finLinea + 1;
                    while (p < sql.Length)
                    {
                        int fin = sql.IndexOf('\n', p);
                        var linea = fin < 0 ? sql.Substring(p) : sql.Substring(p, fin - p);
                        p = fin < 0 ? sql.Length : fin + 1;
                        linea = linea.TrimEnd('\r');
                        if (linea == "\\.") break;
                        if (total >= MaxFilasTotales) { total++; continue; }
                        var valores = linea.Split('\t').Select(DesescaparCopy).ToList();
                        AgregarFila(tabla, cols ?? columnasCreate.GetValueOrDefault(nombre), valores);
                        total++;
                    }
                    pos = p;
                }
            }

        }
        }


        // ---------- utilidades ----------
        private static TablaOrigen ObtenerTabla(Dictionary<string, TablaOrigen> tablas, List<string> orden, string nombre)
        {
            if (!tablas.TryGetValue(nombre, out var t))
            {
                t = new TablaOrigen { Nombre = nombre };
                tablas[nombre] = t;
                orden.Add(nombre);
            }
            return t;
        }

        private static void AgregarFila(TablaOrigen tabla, List<string>? cols, List<string> valores)
        {
            var nombres = new List<string>();
            for (int k = 0; k < valores.Count; k++)
                nombres.Add(cols != null && k < cols.Count && cols[k].Length > 0 ? cols[k] : $"Columna{k + 1}");

            foreach (var n in nombres)
                if (!tabla.Headers.Contains(n, StringComparer.OrdinalIgnoreCase)) tabla.Headers.Add(n);

            var fila = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int k = 0; k < valores.Count; k++) fila[nombres[k]] = valores[k];
            tabla.Rows.Add(fila);
        }

        private static void SaltarEspacios(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        private static bool Coincide(string s, int i, string palabra) =>
            i + palabra.Length <= s.Length &&
            string.Compare(s, i, palabra, 0, palabra.Length, StringComparison.OrdinalIgnoreCase) == 0 &&
            (i + palabra.Length == s.Length || !char.IsLetterOrDigit(s[i + palabra.Length]));

        private static void SaltarFrase(string s, ref int i, string frase)
        {
            SaltarEspacios(s, ref i);
            if (i + frase.Length <= s.Length &&
                string.Compare(s, i, frase, 0, frase.Length, StringComparison.OrdinalIgnoreCase) == 0)
                i += frase.Length;
        }

        /// <summary>Lee un identificador (con comillas/backticks/corchetes y prefijo de esquema) y devuelve la última parte.</summary>
        private static string LeerIdent(string s, ref int i)
        {
            string ultimo = "";
            while (true)
            {
                SaltarEspacios(s, ref i);
                if (i >= s.Length) break;
                char c = s[i];
                string parte;
                if (c == '`' || c == '"' || c == '[')
                {
                    char cierre = c == '[' ? ']' : c;
                    int fin = s.IndexOf(cierre, i + 1);
                    if (fin < 0) break;
                    parte = s.Substring(i + 1, fin - i - 1);
                    i = fin + 1;
                }
                else
                {
                    int ini = i;
                    while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_' || s[i] == '$')) i++;
                    if (i == ini) break;
                    parte = s.Substring(ini, i - ini);
                }
                ultimo = parte;
                if (i < s.Length && s[i] == '.') { i++; continue; }
                break;
            }
            return ultimo;
        }

        private static string LimpiarIdent(string s)
        {
            s = s.Trim();
            if (s.Length >= 2 && ((s[0] == '`' && s[^1] == '`') || (s[0] == '"' && s[^1] == '"') || (s[0] == '[' && s[^1] == ']')))
                s = s.Substring(1, s.Length - 2);
            int punto = s.LastIndexOf('.');
            if (punto >= 0) s = s.Substring(punto + 1).Trim('`', '"', '[', ']');
            return s;
        }

        /// <summary>Con i en '(' devuelve el texto interior y deja i después del ')' correspondiente.</summary>
        private static string LeerParentesis(string s, ref int i)
        {
            int ini = i + 1;
            int prof = 0;
            for (; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\'' || c == '"' || c == '`') { i = SaltarCita(s, i); continue; }
                if (c == '(') prof++;
                else if (c == ')')
                {
                    prof--;
                    if (prof == 0)
                    {
                        var interior = s.Substring(ini, i - ini);
                        i++;
                        return interior;
                    }
                }
            }
            return s.Substring(Math.Min(ini, s.Length));
        }

        /// <summary>Devuelve el índice del último carácter de la cita que empieza en i.</summary>
        private static int SaltarCita(string s, int i)
        {
            char q = s[i];
            for (int k = i + 1; k < s.Length; k++)
            {
                if (s[k] == '\\' && q == '\'') { k++; continue; }
                if (s[k] == q)
                {
                    if (k + 1 < s.Length && s[k + 1] == q) { k++; continue; }
                    return k;
                }
            }
            return s.Length - 1;
        }

        private static List<string> SepararSuperior(string s)
        {
            var partes = new List<string>();
            int prof = 0, ini = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\'' || c == '"' || c == '`') { i = SaltarCita(s, i); continue; }
                if (c == '(') prof++;
                else if (c == ')') prof--;
                else if (c == ',' && prof == 0)
                {
                    partes.Add(s.Substring(ini, i - ini));
                    ini = i + 1;
                }
            }
            partes.Add(s.Substring(ini));
            return partes;
        }

        private static List<string> ColumnasDeCreate(string cuerpo)
        {
            var cols = new List<string>();
            foreach (var parte in SepararSuperior(cuerpo))
            {
                var t = parte.Trim();
                if (t.Length == 0) continue;
                int i = 0;
                var nombre = LeerIdent(t, ref i);
                if (nombre.Length == 0) continue;
                // palabra clave sin comillas (PRIMARY KEY, UNIQUE KEY...) → no es columna
                if (t[0] != '`' && t[0] != '"' && t[0] != '[' && PalabrasClave.Contains(nombre)) continue;
                cols.Add(nombre);
            }
            return cols;
        }

        /// <summary>Lee (v1, v2, ...) y deja i después del ')'.</summary>
        private static List<string> LeerTupla(string s, ref int i)
        {
            var valores = new List<string>();
            i++; // '('
            while (i < s.Length)
            {
                SaltarEspacios(s, ref i);
                if (i >= s.Length) break;

                string valor;
                char c = s[i];
                if (c == '\'' )
                {
                    valor = LeerCadena(s, ref i);
                }
                else if ((c == 'N' || c == 'n') && i + 1 < s.Length && s[i + 1] == '\'')
                {
                    i++;
                    valor = LeerCadena(s, ref i);
                }
                else
                {
                    int ini = i, prof = 0;
                    for (; i < s.Length; i++)
                    {
                        char d = s[i];
                        if (d == '\'' || d == '"') { i = SaltarCita(s, i); continue; }
                        if (d == '(') prof++;
                        else if (d == ')') { if (prof == 0) break; prof--; }
                        else if (d == ',' && prof == 0) break;
                    }
                    valor = s.Substring(ini, i - ini).Trim();
                    if (valor.Equals("NULL", StringComparison.OrdinalIgnoreCase)) valor = "";
                    else if (valor.Length >= 2 && valor[0] == '"' && valor[^1] == '"') valor = valor.Substring(1, valor.Length - 2);
                }
                valores.Add(valor);

                SaltarEspacios(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ')') { i++; break; }
                // carácter inesperado: salir para no quedar en bucle
                if (i < s.Length) i++;
                break;
            }
            return valores;
        }

        private static string LeerCadena(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++; // comilla de apertura
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char n = s[i + 1];
                    sb.Append(n switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '0' => '\0', _ => n });
                    i += 2;
                    continue;
                }
                if (c == '\'')
                {
                    if (i + 1 < s.Length && s[i + 1] == '\'') { sb.Append('\''); i += 2; continue; }
                    i++;
                    break;
                }
                sb.Append(c);
                i++;
            }
            return sb.ToString().Replace("\0", "").Trim();
        }

        private static string DesescaparCopy(string v)
        {
            if (v == "\\N") return "";
            if (v.IndexOf('\\') < 0) return v.Trim();
            var sb = new StringBuilder();
            for (int i = 0; i < v.Length; i++)
            {
                if (v[i] == '\\' && i + 1 < v.Length)
                {
                    char n = v[++i];
                    sb.Append(n switch { 'n' => '\n', 'r' => '\r', 't' => '\t', _ => n });
                }
                else sb.Append(v[i]);
            }
            return sb.ToString().Trim();
        }
    }
}
