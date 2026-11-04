using System.Text.RegularExpressions;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Pasa a español claro los mensajes de validación que .NET genera en inglés cuando un campo
    /// no trae su propio mensaje («The Nombre field is required.» → «Nombre» es obligatorio.).
    /// </summary>
    public static class MensajesValidacion
    {
        private static readonly (Regex rx, Func<GroupCollection, string> es)[] Reglas =
        {
            (new(@"^The (.+) field is required\.?$"), g => $"«{Campo(g[1].Value)}» es obligatorio."),
            (new(@"^The value '(.*)' is not valid for (.+?)\.?$"), g => $"«{g[1].Value}» no es un valor válido para «{Campo(g[2].Value)}»."),
            (new(@"^The value '(.*)' is invalid\.?$"), g => $"«{g[1].Value}» no es un valor válido."),
            (new(@"^The field (.+) must be a number\.?$"), g => $"«{Campo(g[1].Value)}» debe ser un número."),
            (new(@"^The field (.+) must be between (.+) and (.+?)\.?$"), g => $"«{Campo(g[1].Value)}» debe estar entre {g[2].Value} y {g[3].Value}."),
            (new(@"^The field (.+) must be a string with a minimum length of (\d+) and a maximum length of (\d+)\.?$"), g => $"«{Campo(g[1].Value)}» debe tener entre {g[2].Value} y {g[3].Value} caracteres."),
            (new(@"^The field (.+) must be a string (?:or array type )?with a maximum length of '?(\d+)'?\.?$"), g => $"«{Campo(g[1].Value)}» admite como máximo {g[2].Value} caracteres."),
            (new(@"^The (.+) field is not a valid e-mail address\.?$"), g => $"«{Campo(g[1].Value)}» no es un correo válido."),
            (new(@"^The (.+) field is not a valid phone number\.?$"), g => $"«{Campo(g[1].Value)}» no es un teléfono válido."),
            (new(@"^A value for the '(.+)' parameter or property was not provided\.?$"), g => $"Falta «{Campo(g[1].Value)}»."),
            (new(@"^A value is required\.?$"), _ => "Falta un dato obligatorio."),
            (new(@"^The supplied value is invalid for (.+?)\.?$"), g => $"El valor de «{Campo(g[1].Value)}» no es válido."),
        };

        public static string Traducir(string? mensaje, string? clave)
        {
            if (string.IsNullOrWhiteSpace(mensaje))
                return clave == null ? "" : $"Revisa «{Campo(clave)}».";
            var m = mensaje.Trim();
            // campo que llegó vacío («The value '' is invalid.»): lo que falta es el dato
            if (clave != null && (m.StartsWith("The value '' ") || m == "A value is required."))
                return $"Falta «{Campo(clave)}».";
            foreach (var (rx, es) in Reglas)
            {
                var r = rx.Match(m);
                if (r.Success) return es(r.Groups);
            }
            return m;
        }

        /// <summary>«Input.PrecioVenta» → «Precio Venta».</summary>
        internal static string Campo(string clave)
        {
            var c = clave.Contains('.') ? clave[(clave.LastIndexOf('.') + 1)..] : clave;
            c = Regex.Replace(c, @"\[\d+\]$", "");
            if (c.Length > 2 && c.EndsWith("Id")) c = c[..^2];              // ProveedorId → Proveedor
            return Regex.Replace(c, "(?<=[a-záéíóúñ])(?=[A-ZÁÉÍÓÚÑ])", " ");
        }
    }
}
