using System.Text.RegularExpressions;

namespace Kirkenta.Services.Asistente
{
    /// <summary>
    /// Capa de lenguaje natural del asistente (100% offline y gratis): convierte la forma de hablar de la gente
    /// ("plata", "al día", "qué se está acabando", "quién me debe") a las frases canónicas que entiende el catálogo,
    /// responde charla corta y sugiere las acciones más cercanas cuando no entiende.
    /// </summary>
    public static class AsistenteLenguaje
    {
        private static readonly (Regex Patron, string Reemplazo)[] Reglas = Construir();

        private static (Regex, string)[] Construir()
        {
            // El texto ya llega normalizado: minúsculas, sin acentos ni signos.
            // Las reglas se aplican en orden; las que empiezan con ^ y terminan con $ reemplazan el mensaje completo.
            var origen = new (string Patron, string Reemplazo)[]
            {
                // Preguntas generales
                (@"^(mis )?(pendientes|pendiente|que tengo por hacer|que hay por hacer|que me falta|que falta por hacer|tareas|por hacer)$", "que tengo pendiente"),
                (@"^(al dia|como andamos|como estamos|como andan las cosas|como nos fue|como nos va|como va todo|como anda todo|que tal el negocio|que tal las ventas|como va el dia|como va la cosa|como ando|como vamos)( hoy)?$", "como vamos"),
                (@"^(que puedes hacer|que sabes hacer|para que sirves|que haces|como funcionas|como te uso|que opciones hay)$", "ayuda"),

                // Dinero
                (@"\b(plata|pisto|lana|billete|billetes|cash|lempiras disponibles|lempira disponible)\b", "dinero"),
                (@"^(cuanto |cuanta |cuantos )?(dinero)( hay| tenemos| tengo| llevamos| queda| quedan| disponible)?( en total| ahorita| ahora| hoy)?$", "cuanto dinero tenemos"),

                // Ventas e ingresos
                (@"\bcuanto (llevamos|llevo|hemos vendido|se vendio|vendimos|entro|entraron)( hoy)?\b", "ventas hoy"),
                (@"\b(que|cuanto) (se )?(vendio|vendimos)\b", "ventas"),
                (@"\bcuanto (gane|ganamos|hemos ganado|me quedo|nos quedo|saque|sacamos)\b", "cuanto ganamos"),
                (@"\bbalance\b", "resultado"),

                // Inventario
                (@"\bque (se )?(esta )?(acabo|acabando|acaba|falta|faltan|hace falta|nos falta|toca reponer|toca comprar)\b", "stock bajo"),
                (@"\b(faltantes|me falta mercaderia|mercaderia por agotar)\b", "stock bajo"),
                (@"\b(tienen|tienes)\b", "tenemos"),

                // Cobranza y deudas
                (@"\bquienes? (me |nos )?(deben|debe|estan debiendo|estan atrasad\w+|no (han )?pagad\w+|siguen debiendo)\b", "quien debe"),
                (@"\b(a quien le debo|a quien le debemos|a quien debemos|que debemos|que debo(?! comprar| pedir| reponer| surtir))\b", "cuentas por pagar"),

                // Caja
                (@"\b(cierre del dia|cerrar el dia|cierro el dia|cerrar turno|cierre de turno|terminar el dia)\b", "cerrar caja"),
            };
            return origen.Select(x => (new Regex(x.Patron, RegexOptions.CultureInvariant | RegexOptions.Compiled), x.Reemplazo)).ToArray();
        }

        /// <summary>Recibe el texto ya normalizado y devuelve la versión con las palabras coloquiales traducidas.</summary>
        public static string Traducir(string normalizado)
        {
            var s = normalizado ?? "";
            foreach (var (patron, reemplazo) in Reglas)
                s = patron.Replace(s, reemplazo);
            return Regex.Replace(s, @"\s+", " ").Trim();
        }

        /// <summary>Respuesta a charla corta (cómo estás, quién eres, adiós). null si no es charla.</summary>
        public static string? Charla(string normalizado, string nombre)
        {
            if (Regex.IsMatch(normalizado, @"^(como estas|como esta|como te va|que tal estas|como andas|todo bien|como amaneciste)( hoy)?$"))
                return $"¡Todo bien, {nombre}! Listo para ayudarte. Pídeme ventas, caja, cobranza, inventario… o escribe “pendientes”.";
            if (Regex.IsMatch(normalizado, @"^(quien eres|que eres|como te llamas|eres una ia|eres humano|eres un robot)$"))
                return "Soy el asistente de Kirkenta: respondo consultas y hago operaciones del sistema con tus mismos permisos. Escribe “ayuda” para ver qué puedo hacer.";
            if (Regex.IsMatch(normalizado, @"^(adios|chao|chau|hasta luego|nos vemos|bye|hasta manana)$"))
                return "¡Hasta luego! Aquí estaré cuando me necesites.";
            return null;
        }

        /// <summary>True si el usuario pide ver todos los módulos.</summary>
        public static bool PideModulos(string normalizado)
            => Regex.IsMatch(normalizado, @"^(menu|modulos|ver modulos|todos los modulos|lista de modulos|opciones)$");

        /// <summary>
        /// Acciones más cercanas al mensaje (por palabras en común con sus frases, título y descripción).
        /// Sirve para "¿Quisiste decir…?" cuando las reglas no entienden, sin saturar con todo el menú.
        /// </summary>
        public static List<AccionDef> Sugerir(string normalizado, IEnumerable<AccionDef> acciones, int max)
        {
            var tokens = AsistenteMotor.Tokens(normalizado)
                .Where(t => t.Length >= 4 && !AsistenteMotor.EsRelleno(t) && !Regex.IsMatch(t, @"^[\d/.,-]+$"))
                .Distinct().ToList();
            if (tokens.Count == 0) return new List<AccionDef>();

            var res = new List<(AccionDef Accion, int Puntos)>();
            foreach (var a in acciones)
            {
                var palabras = new HashSet<string>(StringComparer.Ordinal);
                var texto = string.Join(' ', a.Frases.Select(f => f.Replace("*", ""))) + " " + AsistenteMotor.Normalizar(a.Titulo) + " " + AsistenteMotor.Normalizar(a.Descripcion);
                foreach (var w in AsistenteMotor.Tokens(AsistenteMotor.Normalizar(texto)))
                    if (w.Length >= 4) palabras.Add(w);

                var puntos = 0;
                foreach (var t in tokens)
                {
                    if (palabras.Any(w => w == t
                                          || (t.Length >= 5 && w.StartsWith(t, StringComparison.Ordinal))
                                          || (w.Length >= 5 && t.StartsWith(w, StringComparison.Ordinal))))
                        puntos++;
                }
                if (puntos > 0) res.Add((a, puntos));
            }
            return res.OrderByDescending(x => x.Puntos).ThenBy(x => x.Accion.Titulo).Take(max).Select(x => x.Accion).ToList();
        }
    }
}
