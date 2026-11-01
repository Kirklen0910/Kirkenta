using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Kirkenta.Services.Asistente
{
    /// <summary>Resultado de intentar entender un mensaje (sin base de datos, 100% gratis y offline).</summary>
    public class Interpretacion
    {
        public AccionDef? Accion { get; set; }
        public int Puntaje { get; set; }
        /// <summary>Acciones con puntaje parecido al mejor (para preguntar "¿cuál de estas?").</summary>
        public List<AccionDef> Candidatas { get; set; } = new();
        public bool Ambigua { get; set; }
        public string? Texto { get; set; }
        public decimal? Numero { get; set; }
        public (DateTime desde, DateTime hasta, string etiqueta)? Rango { get; set; }
    }

    /// <summary>Entiende texto en español: normaliza, detecta fechas, números y la acción más probable.</summary>
    public static class AsistenteMotor
    {
        private static readonly string[] Meses =
            { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };

        private static readonly HashSet<string> Relleno = new(StringComparer.Ordinal)
        {
            "a","al","algo","ante","aqui","asi","con","contra","cual","cuales","cuanto","cuantos","cuanta","cuantas",
            "de","del","desde","dame","dime","donde","el","ella","en","entre","es","esta","estan","este","esto","favor",
            "genera","generame","genere","generar","gracias","ha","hacer","haz","hazme","hasta","hay","la","las","le","les",
            "lo","los","me","mi","mis","muestra","muestrame","mostrar","necesito","nos","o","para","pedir","por","porfa",
            "porfavor","puedes","puede","podrias","que","quiero","quisiera","se","ser","si","sin","sobre","su","sus","te",
            "tengo","tiene","tienen","todo","todos","todas","tu","un","una","uno","unos","unas","ver","voy","y","ya",
            "ayudame","ayuda","reporte","informe","lista","listado","resumen","hoy","ayer","anteayer","semana","mes","ano",
            "pasado","pasada","actual","ultimo","ultimos","ultima","ultimas","dias","dia","buenas","buenos","hola","necesitamos",
            "quieres","l","lps","lempiras","lempira","imprimir","pdf","descargar","dar","da","dale","entregame","entrega"
        };

        /// <summary>True si la palabra es relleno (artículos, "quiero", "dame"...) y no aporta significado.</summary>
        internal static bool EsRelleno(string token) => Relleno.Contains(token);

        // ───────────────────────── Normalización ─────────────────────────

        /// <summary>Minúsculas, sin acentos ni signos. "¿Cuánto vendí HOY?" → "cuanto vendi hoy".</summary>
        public static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            var d = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (var ch in d)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(ch) || ch == '/' || ch == '-' || ch == '.' || ch == ',') sb.Append(ch);
                else sb.Append(' ');
            }
            var s = sb.ToString();
            // "/" "." "," solo se conservan entre dígitos (fechas y montos); "-" solo entre letras o dígitos (P-001)
            s = Regex.Replace(s, @"(?<!\d)[./,]|[./,](?!\d)", " ");
            s = Regex.Replace(s, @"(?<![\da-z])-|-(?![\da-z])", " ");
            return Regex.Replace(s, @"\s+", " ").Trim();
        }

        public static string[] Tokens(string normalizado)
            => normalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim('.', ',', '-'))
                .Where(t => t.Length > 0).ToArray();

        // ───────────────────────── Fechas ─────────────────────────

        public static (DateTime desde, DateTime hasta, string etiqueta)? ExtraerRango(string? texto, DateTime hoy)
        {
            var n = Normalizar(texto);
            if (n.Length == 0) return null;
            hoy = hoy.Date;
            var toks = " " + n + " ";

            // Fechas explícitas dd/mm/aaaa (una o dos)
            var fechas = Regex.Matches(n, @"\b(\d{1,2})/(\d{1,2})/(\d{4})\b");
            var lista = new List<DateTime>();
            foreach (Match m in fechas)
            {
                if (DateTime.TryParse($"{m.Groups[3].Value}-{int.Parse(m.Groups[2].Value):00}-{int.Parse(m.Groups[1].Value):00}",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var f)) lista.Add(f.Date);
            }
            if (lista.Count >= 2)
            {
                var a = lista.Min(); var b = lista.Max();
                return (a, b, $"del {a:dd/MM/yyyy} al {b:dd/MM/yyyy}");
            }
            if (lista.Count == 1) return (lista[0], lista[0], $"el {lista[0]:dd/MM/yyyy}");

            var ult = Regex.Match(n, @"\bultim[oa]s? (\d{1,3}) (dias|semanas|meses)\b");
            if (ult.Success)
            {
                var k = int.Parse(ult.Groups[1].Value);
                var unidad = ult.Groups[2].Value;
                var desde = unidad == "dias" ? hoy.AddDays(-(k - 1))
                          : unidad == "semanas" ? hoy.AddDays(-(7 * k - 1))
                          : hoy.AddMonths(-k).AddDays(1);
                return (desde, hoy, $"últimos {k} {unidad}");
            }

            if (toks.Contains(" anteayer ")) { var d = hoy.AddDays(-2); return (d, d, "anteayer"); }
            if (toks.Contains(" ayer ")) { var d = hoy.AddDays(-1); return (d, d, "ayer"); }

            if (toks.Contains(" semana pasada ") || toks.Contains(" ultima semana "))
            {
                var lunes = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7)).AddDays(-7);
                return (lunes, lunes.AddDays(6), "la semana pasada");
            }
            if (toks.Contains(" esta semana ") || toks.Contains(" semana actual "))
            {
                var lunes = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7));
                return (lunes, hoy, "esta semana");
            }

            if (toks.Contains(" mes pasado ") || toks.Contains(" ultimo mes "))
            {
                var ini = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-1);
                return (ini, ini.AddMonths(1).AddDays(-1), $"{Meses[ini.Month - 1]} de {ini.Year}");
            }
            if (toks.Contains(" este mes ") || toks.Contains(" mes actual ") || toks.Contains(" del mes "))
                return (new DateTime(hoy.Year, hoy.Month, 1), hoy, "este mes");

            if (toks.Contains(" ano pasado ")) return (new DateTime(hoy.Year - 1, 1, 1), new DateTime(hoy.Year - 1, 12, 31), $"el año {hoy.Year - 1}");
            if (toks.Contains(" este ano ") || toks.Contains(" ano actual ")) return (new DateTime(hoy.Year, 1, 1), hoy, "este año");

            for (int i = 0; i < Meses.Length; i++)
            {
                if (!Regex.IsMatch(n, $@"\b{Meses[i]}\b")) continue;
                var ym = Regex.Match(n, $@"\b{Meses[i]}\b(?: de| del)? (\d{{4}})");
                var anio = ym.Success ? int.Parse(ym.Groups[1].Value) : (i + 1 > hoy.Month ? hoy.Year - 1 : hoy.Year);
                var ini = new DateTime(anio, i + 1, 1);
                var fin = ini.AddMonths(1).AddDays(-1);
                if (fin > hoy && anio == hoy.Year && i + 1 == hoy.Month) fin = hoy;
                return (ini, fin, $"{Meses[i]} de {anio}");
            }

            if (toks.Contains(" hoy ")) return (hoy, hoy, "hoy");
            return null;
        }

        // ───────────────────────── Números ─────────────────────────

        /// <summary>Primer monto o número del mensaje que no sea parte de una fecha. "L 2,350.50" → 2350.50.</summary>
        public static decimal? ExtraerNumero(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            var sinFechas = Regex.Replace(texto, @"\d{1,2}/\d{1,2}/\d{2,4}", " ");
            foreach (Match m in Regex.Matches(sinFechas, @"\d[\d,]*(?:\.\d+)?"))
            {
                var limpio = m.Value.Replace(",", "").TrimEnd('.');
                if (decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)) return d;
            }
            return null;
        }

        // ───────────────────────── Acción ─────────────────────────

        /// <param name="aproximado">true si coincide solo por tolerancia a errores de escritura (no exacta).</param>
        private static bool TokenCoincide(string fraseToken, string tokenTexto, out bool aproximado)
        {
            aproximado = false;
            if (fraseToken.EndsWith('*'))
            {
                var pref = fraseToken[..^1];
                if (tokenTexto.StartsWith(pref, StringComparison.Ordinal)) return true;
                // Tolerancia a errores de escritura ("cotisaciones", "envios fallidoz"): 1 letra de diferencia en el prefijo.
                // Solo con prefijos de 6+ letras: con 5 se confunden palabras distintas ("pendi" ≈ "vendi", "banca" ≈ "banco").
                aproximado = pref.Length >= 6 && tokenTexto.Length >= pref.Length
                             && Distancia(tokenTexto[..pref.Length], pref, 1) <= 1;
                return aproximado;
            }
            if (tokenTexto == fraseToken) return true;
            aproximado = fraseToken.Length >= 7 && tokenTexto.Length >= 6 && Math.Abs(fraseToken.Length - tokenTexto.Length) <= 1
                         && Distancia(tokenTexto, fraseToken, 1) <= 1;
            return aproximado;
        }

        /// <summary>Distancia de edición con corte: devuelve max+1 si supera el máximo.</summary>
        internal static int Distancia(string a, string b, int max)
        {
            if (Math.Abs(a.Length - b.Length) > max) return max + 1;
            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) prev[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                var min = cur[0];
                for (int j = 1; j <= b.Length; j++)
                {
                    var costo = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + costo);
                    if (cur[j] < min) min = cur[j];
                }
                if (min > max) return max + 1;
                (prev, cur) = (cur, prev);
            }
            return prev[b.Length];
        }

        private static bool FraseCoincide(string frase, string[] tokensTexto, out List<string> usados, out bool aproximado)
        {
            usados = new List<string>();
            aproximado = false;
            foreach (var ft in frase.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                // Primero una palabra que coincida exacta; solo si no hay, una aproximada (error de escritura)
                string? t = null;
                foreach (var x in tokensTexto)
                    if (TokenCoincide(ft, x, out var apr) && !apr) { t = x; break; }
                if (t == null)
                    foreach (var x in tokensTexto)
                        if (TokenCoincide(ft, x, out _)) { t = x; aproximado = true; break; }
                if (t == null) return false;
                usados.Add(t);
            }
            return true;
        }

        public static int Puntuar(AccionDef a, string[] tokensTexto, out HashSet<string> tokensUsados)
            => PuntuarDetalle(a, tokensTexto, out tokensUsados, out _);

        /// <summary>Igual que Puntuar, e indica si algún punto vino solo de una coincidencia aproximada (error de escritura).</summary>
        public static int PuntuarDetalle(AccionDef a, string[] tokensTexto, out HashSet<string> tokensUsados, out bool aproximado)
        {
            tokensUsados = new HashSet<string>(StringComparer.Ordinal);
            aproximado = false;
            var puntaje = 0;
            foreach (var f in a.Frases)
            {
                if (!FraseCoincide(f, tokensTexto, out var usados, out var apr)) continue;
                puntaje += usados.Count * 3;
                if (apr) aproximado = true;
                foreach (var u in usados) tokensUsados.Add(u);
            }
            // Una frase de navegación ("abre clientes", "llévame a nómina") es una orden explícita: pesa más
            // que una simple palabra suelta que también aparece en otras acciones.
            if (a.Tipo == TipoAccion.Navegar && puntaje > 0) puntaje += 6;
            return puntaje;
        }

        // ───────────────────────── Módulos ("ventas", "ayuda finanzas") ─────────────────────────
        private static readonly Dictionary<string, string> PalabrasModulo = new(StringComparer.Ordinal)
        {
            ["ventas"] = "Ventas", ["venta"] = "Ventas",
            ["inventario"] = "Inventario", ["inventarios"] = "Inventario", ["almacen"] = "Inventario", ["bodega"] = "Inventario",
            ["compras"] = "Compras", ["finanzas"] = "Finanzas", ["contabilidad"] = "Finanzas", ["tesoreria"] = "Finanzas",
            ["rrhh"] = "RRHH", ["logistica"] = "Logística", ["seguridad"] = "Seguridad"
        };

        private static readonly HashSet<string> RellenoModulo = new(StringComparer.Ordinal)
        {
            "modulo","modulos","opciones","opcion","menu","que","puedo","puedes","hacer","en","de","del","la","el","lo","los","las","ayuda","ayudame",
            "informacion","info","todo","todos","ver","muestrame","mostrar","dime","sobre","area","departamento","y","algo","quiero","necesito","con","para",
            "me","mi","hola","dame","abre","ir","ve","llevame","a","un","una","cosas","funciones","tengo","tienes","hay","disponibles","disponible","ahora"
        };

        /// <summary>Si el mensaje es solo el nombre de un módulo (o "qué puedo hacer en ventas") devuelve ese módulo.</summary>
        public static string? ModuloMencionado(string? mensaje)
        {
            var norm = Normalizar(mensaje).Replace("recursos humanos", "rrhh");
            var toks = Tokens(norm);
            if (toks.Length == 0) return null;
            var mods = new HashSet<string>();
            foreach (var t in toks)
            {
                if (PalabrasModulo.TryGetValue(t, out var m)) { mods.Add(m); continue; }
                if (!RellenoModulo.Contains(t)) return null;
            }
            return mods.Count == 1 ? mods.First() : null;
        }

        public const int PuntajeMinimo = 3;

        /// <summary>Elige la acción más probable entre las permitidas.</summary>
        public static Interpretacion Interpretar(string? mensaje, IEnumerable<AccionDef> acciones, DateTime hoy)
        {
            // Lenguaje coloquial ("plata", "al día", "qué se está acabando") → frases que entiende el catálogo
            var norm = AsistenteLenguaje.Traducir(Normalizar(mensaje));
            var toks = Tokens(norm);
            var res = new Interpretacion { Rango = ExtraerRango(mensaje, hoy), Numero = ExtraerNumero(mensaje) };

            var puntuadas = acciones
                .Select(a => (a, p: PuntuarDetalle(a, toks, out var usados, out var aprox), usados, aprox))
                .Where(x => x.p >= PuntajeMinimo)
                .OrderByDescending(x => x.p)
                .ToList();

            if (puntuadas.Count == 0) return res;

            // Una coincidencia exacta le gana a una que solo coincide por tolerancia a errores de escritura
            var tope = puntuadas[0].p;
            if (puntuadas.Any(x => !x.aprox && x.p >= tope - 2))
                puntuadas = puntuadas.Where(x => !(x.aprox && x.p >= tope - 2)).ToList();

            var mejor = puntuadas[0];
            res.Puntaje = mejor.p;
            res.Candidatas = puntuadas.Where(x => x.p >= mejor.p - 2).Take(4).Select(x => x.a).ToList();
            res.Ambigua = res.Candidatas.Count > 1;
            res.Accion = mejor.a;

            // Texto libre = lo que sobra (nombre de cliente, producto, número de factura...)
            var fechaTokens = new HashSet<string>(Meses);
            var sobrantes = toks.Where(t => !mejor.usados.Contains(t)
                                            && !Relleno.Contains(t)
                                            && !fechaTokens.Contains(t)
                                            && !Regex.IsMatch(t, @"^[\d/.,-]+$"))
                                .ToList();
            res.Texto = sobrantes.Count > 0 ? string.Join(' ', sobrantes) : null;
            return res;
        }

        /// <summary>Para respuestas a una pregunta del asistente ("¿cuánto contaste?" → "L. 1,200").</summary>
        public static bool EsCancelar(string? mensaje)
        {
            var n = Normalizar(mensaje);
            return n is "cancelar" or "cancela" or "no" or "salir" or "cancelalo" or "olvidalo" or "dejalo";
        }

        public static bool EsSaludo(string? mensaje)
        {
            var n = Normalizar(mensaje);
            return n is "hola" or "buenas" or "buenos dias" or "buenas tardes" or "buenas noches" or "hey" or "que tal";
        }

        public static bool EsGracias(string? mensaje)
        {
            var n = Normalizar(mensaje);
            return n.StartsWith("gracias") || n is "ok" or "listo" or "perfecto" or "excelente" or "vale";
        }
    }
}
