using System.Globalization;
using System.Text.RegularExpressions;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>Formato que se detectó en una columna (fechas o números).</summary>
    public class FormatoColumna
    {
        public string Tabla { get; set; } = "";
        public string Columna { get; set; } = "";
        public bool EsFecha { get; set; }
        /// <summary>Fechas: «dmy», «mdy», «ymd». Números: «.» o «,» como decimal.</summary>
        public string Formato { get; set; } = "";
        /// <summary>Ningún valor permite saber el orden (todos los días y meses son ≤ 12).</summary>
        public bool Ambiguo { get; set; }
        public string Ejemplo { get; set; } = "";
    }

    /// <summary>
    /// Lee TODOS los valores de cada columna de fecha o de número antes de migrar y decide su formato
    /// para la columna entera: «03/04/2024» se lee igual en todas las filas, según lo que digan las demás
    /// («25/04/2024» prueba que es día/mes). Luego deja los valores en un formato sin ambigüedad.
    /// </summary>
    public static class MigracionFormatos
    {
        public static readonly HashSet<string> CamposFecha = new() { "Fecha", "Vencimiento", "FechaNacimiento", "FechaIngreso", "FechaBaja" };

        public static readonly HashSet<string> CamposNumero = new()
        {
            "Total", "Subtotal", "Descuento", "Impuestos", "Saldo", "Cantidad", "Precio", "TotalLinea", "Costo", "StockResultante",
            "PrecioCompra", "PrecioVenta", "PrecioMayorista", "Stock", "StockMinimo", "Porcentaje", "Monto", "LimiteCredito",
            "DiasCredito", "Salario", "Debe", "Haber", "Ingreso", "Egreso", "Vacaciones"
        };

        private static readonly Regex RxFecha = new(@"^(\d{1,4})([/\-.])(\d{1,2})\2(\d{1,4})(?:[ T](.+))?$", RegexOptions.Compiled);

        /// <summary>Columnas de fecha y número de las tablas que se van a migrar.</summary>
        private static IEnumerable<(TablaOrigen tabla, string col, bool fecha)> Columnas(List<TablaOrigen> tablas, List<PasoMigracion> plan)
        {
            var vistas = new HashSet<(int, string)>();
            foreach (var p in plan.Where(p => p.Incluir))
            {
                foreach (var (indice, mapa) in new[] { (p.TablaIndice, p.Mapa), (p.DetalleIndice ?? -1, p.MapaDetalle) })
                {
                    if (indice < 0) continue;
                    foreach (var (campo, col) in mapa)
                    {
                        bool f = CamposFecha.Contains(campo);
                        if (!f && !CamposNumero.Contains(campo)) continue;
                        if (vistas.Add((indice, col))) yield return (tablas[indice], col, f);
                    }
                }
            }
        }

        /// <summary>Formatos detectados (para mostrarlos y preguntar los ambiguos antes de migrar).</summary>
        public static List<FormatoColumna> Analizar(List<TablaOrigen> tablas, List<PasoMigracion> plan) =>
            Columnas(tablas, plan).Select(c => c.fecha ? Fecha(c.tabla, c.col) : Numero(c.tabla, c.col)).Where(x => x != null).ToList()!;

        /// <summary>
        /// Reescribe en la memoria (no en el archivo) cada valor ambiguo según el formato de su columna.
        /// formatoFechaAmbigua: cómo leer las columnas donde ningún valor lo aclara («dmy» o «mdy»).
        /// </summary>
        public static List<FormatoColumna> Normalizar(List<TablaOrigen> tablas, List<PasoMigracion> plan, string formatoFechaAmbigua = "dmy")
        {
            var res = new List<FormatoColumna>();
            foreach (var (tabla, col, fecha) in Columnas(tablas, plan).ToList())
            {
                var f = fecha ? Fecha(tabla, col) : Numero(tabla, col);
                if (f == null) continue;
                if (fecha && f.Ambiguo) f.Formato = formatoFechaAmbigua == "mdy" ? "mdy" : "dmy";
                foreach (var fila in tabla.Rows)
                {
                    if (!fila.TryGetValue(col, out var v) || string.IsNullOrWhiteSpace(v)) continue;
                    var nuevo = fecha ? FechaIso(v.Trim(), f.Formato) : NumeroClaro(v.Trim(), f.Formato);
                    if (nuevo != null) fila[col] = nuevo;
                }
                res.Add(f);
            }
            return res;
        }

        // ---------- fechas ----------
        private static FormatoColumna? Fecha(TablaOrigen t, string col)
        {
            int dmy = 0, mdy = 0, ymd = 0, iguales = 0;
            string ejemplo = "";
            foreach (var fila in t.Rows)
            {
                if (!fila.TryGetValue(col, out var v) || string.IsNullOrWhiteSpace(v)) continue;
                var m = RxFecha.Match(v.Trim());
                if (!m.Success) continue;
                if (m.Groups[1].Value.Length == 4) { ymd++; continue; }
                int a = int.Parse(m.Groups[1].Value), b = int.Parse(m.Groups[3].Value);
                if (a > 12 && b <= 12) dmy++;
                else if (b > 12 && a <= 12) mdy++;
                else if (a == b) iguales++;
                else if (ejemplo.Length == 0) ejemplo = v.Trim();
            }
            if (dmy + mdy + ymd + iguales == 0 && ejemplo.Length == 0) return null;
            var r = new FormatoColumna { Tabla = t.Nombre, Columna = col, EsFecha = true, Ejemplo = ejemplo };
            if (dmy > 0 && mdy == 0) r.Formato = "dmy";
            else if (mdy > 0 && dmy == 0) r.Formato = "mdy";
            else if (dmy > 0 && mdy > 0) r.Formato = dmy >= mdy ? "dmy" : "mdy";   // columna mezclada: manda la mayoría
            else if (ejemplo.Length > 0) { r.Formato = "dmy"; r.Ambiguo = true; }
            else r.Formato = ymd > 0 ? "ymd" : "dmy";
            return r;
        }

        /// <summary>Devuelve «yyyy-MM-ddTHH:mm:ss» (o solo la fecha); null si el valor no es una fecha con barras/guiones.</summary>
        internal static string? FechaIso(string v, string formato)
        {
            var m = RxFecha.Match(v);
            if (!m.Success) return null;
            int p1 = int.Parse(m.Groups[1].Value), p2 = int.Parse(m.Groups[3].Value), p3 = int.Parse(m.Groups[4].Value);
            int dia, mes, anio;
            if (m.Groups[1].Value.Length == 4) return null;     // año primero: ya no es ambiguo
            {
                // un valor que solo tiene sentido en el otro orden se respeta (columna mezclada)
                bool mdy = formato == "mdy";
                if (mdy && p2 <= 12 && p1 > 12) mdy = false;
                if (!mdy && p1 <= 12 && p2 > 12) mdy = true;
                (dia, mes) = mdy ? (p2, p1) : (p1, p2);
                anio = m.Groups[4].Value.Length <= 2 ? (p3 <= 49 ? 2000 + p3 : 1900 + p3) : p3;
            }
            if (mes < 1 || mes > 12 || anio < 1 || anio > 9999 || dia < 1 || dia > DateTime.DaysInMonth(anio, mes)) return null;
            var fecha = new DateTime(anio, mes, dia);
            var hora = m.Groups[5].Success ? m.Groups[5].Value.Trim() : "";
            if (hora.Length > 0 && DateTime.TryParse("2000-01-01 " + hora, CultureInfo.InvariantCulture, DateTimeStyles.None, out var h))
                return fecha.Add(h.TimeOfDay).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            return fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static string NombreFormatoFecha(string f) => f switch { "mdy" => "mes/día/año", "ymd" => "año-mes-día", _ => "día/mes/año" };

        // ---------- números ----------
        private static string Limpio(string v) => Regex.Replace(Regex.Replace(v, @"(?i)lps\.?|hnl|usd|l\.", ""), @"[^\d,.]", "").Trim('.', ',');

        private static FormatoColumna? Numero(TablaOrigen t, string col)
        {
            int punto = 0, coma = 0, ambiguos = 0;
            string ejemplo = "";
            foreach (var fila in t.Rows)
            {
                if (!fila.TryGetValue(col, out var v) || string.IsNullOrWhiteSpace(v)) continue;
                var s = Limpio(v);
                int nc = s.Count(c => c == ','), np = s.Count(c => c == '.');
                if (nc == 0 && np == 0) continue;
                if (nc > 0 && np > 0) { if (s.LastIndexOf(',') > s.LastIndexOf('.')) coma++; else punto++; continue; }
                char sep = nc > 0 ? ',' : '.';
                if (nc + np > 1) { if (sep == ',') punto++; else coma++; continue; }   // «1,234,567»: la coma es de miles
                var despues = s.Length - s.IndexOf(sep) - 1;
                if (despues != 3) { if (sep == ',') coma++; else punto++; continue; }
                ambiguos++;
                if (ejemplo.Length == 0) ejemplo = v.Trim();
            }
            if (punto + coma + ambiguos == 0) return null;
            return new FormatoColumna
            {
                Tabla = t.Nombre, Columna = col, Ejemplo = ejemplo,
                Formato = coma > punto ? "," : ".",
                Ambiguo = punto + coma == 0
            };
        }

        /// <summary>Solo reescribe los valores ambiguos («1.234» o «1,234»); el resto ya se entiende sin ayuda.</summary>
        internal static string? NumeroClaro(string v, string separadorDecimal)
        {
            var s = Limpio(v);
            int nc = s.Count(c => c == ','), np = s.Count(c => c == '.');
            if (nc + np != 1 || s.Length - s.IndexOfAny(new[] { ',', '.' }) - 1 != 3) return null;
            var sep = nc == 1 ? ',' : '.';
            var signo = v.TrimStart().StartsWith("-") || (v.Trim().StartsWith("(") && v.Trim().EndsWith(")")) ? "-" : "";
            return sep == separadorDecimal[0]
                ? signo + s.Replace(',', '.')          // era el decimal: 1,234 → 1.234
                : signo + s.Replace(",", "").Replace(".", "");   // era de miles: 1.234 → 1234
        }
    }
}
