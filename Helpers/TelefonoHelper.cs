using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Helpers
{
    /// <summary>Catálogo de países con código de área y largo del número telefónico.</summary>
    public record PaisTelefono(string Iso, string Nombre, string Codigo, int[] Largos);

    public static class TelefonoHelper
    {
        public const string PaisPorDefecto = "HN";

        public static readonly IReadOnlyList<PaisTelefono> Paises = new List<PaisTelefono>
        {
            new("HN", "Honduras", "504", new[] { 8 }),
            new("GT", "Guatemala", "502", new[] { 8 }),
            new("SV", "El Salvador", "503", new[] { 8 }),
            new("NI", "Nicaragua", "505", new[] { 8 }),
            new("CR", "Costa Rica", "506", new[] { 8 }),
            new("PA", "Panamá", "507", new[] { 7, 8 }),
            new("BZ", "Belice", "501", new[] { 7 }),
            new("MX", "México", "52", new[] { 10 }),
            new("US", "Estados Unidos", "1", new[] { 10 }),
            new("CA", "Canadá", "1", new[] { 10 }),
            new("DO", "Rep. Dominicana", "1", new[] { 10 }),
            new("PR", "Puerto Rico", "1", new[] { 10 }),
            new("CU", "Cuba", "53", new[] { 8 }),
            new("CO", "Colombia", "57", new[] { 10 }),
            new("VE", "Venezuela", "58", new[] { 10 }),
            new("EC", "Ecuador", "593", new[] { 9 }),
            new("PE", "Perú", "51", new[] { 9 }),
            new("BO", "Bolivia", "591", new[] { 8 }),
            new("CL", "Chile", "56", new[] { 9 }),
            new("AR", "Argentina", "54", new[] { 10 }),
            new("UY", "Uruguay", "598", new[] { 8 }),
            new("PY", "Paraguay", "595", new[] { 9 }),
            new("BR", "Brasil", "55", new[] { 10, 11 }),
            new("ES", "España", "34", new[] { 9 }),
            new("FR", "Francia", "33", new[] { 9 }),
            new("GB", "Reino Unido", "44", new[] { 10 }),
            new("DE", "Alemania", "49", new[] { 10, 11 }),
            new("IT", "Italia", "39", new[] { 9, 10 }),
            new("CN", "China", "86", new[] { 11 }),
        };

        /// <summary>
        /// Valida un teléfono con formato "+CÓDIGO número" (ej. "+504 9999-9999").
        /// Vacío o nulo es válido (el campo es opcional; el "requerido" lo decide cada formulario).
        /// </summary>
        public static bool EsValido(string? valor, out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(valor)) return true;

            var s = valor.Trim();
            if (!s.StartsWith('+'))
            {
                error = "Selecciona el país y escribe el número (ej. +504 9999-9999)";
                return false;
            }

            if (s.Any(c => !(char.IsDigit(c) || c == '+' || c == ' ' || c == '-')))
            {
                error = "El teléfono solo puede tener números y guiones";
                return false;
            }

            var digitos = new string(s.Where(char.IsDigit).ToArray());

            foreach (var p in Paises.OrderByDescending(x => x.Codigo.Length))
            {
                if (!digitos.StartsWith(p.Codigo)) continue;
                var nacional = digitos.Length - p.Codigo.Length;
                if (p.Largos.Contains(nacional)) return true;
            }

            var candidato = Paises.OrderByDescending(x => x.Codigo.Length).FirstOrDefault(x => digitos.StartsWith(x.Codigo));
            error = candidato == null
                ? "Código de país no reconocido"
                : $"El número de {candidato.Nombre} debe tener {string.Join(" o ", candidato.Largos)} dígitos (+{candidato.Codigo})";
            return false;
        }
    }

    /// <summary>[Telefono]: valida el formato internacional con el largo exacto según el país.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class TelefonoAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (TelefonoHelper.EsValido(value as string, out var error)) return ValidationResult.Success;
            return new ValidationResult(error, new[] { validationContext.MemberName ?? "" });
        }
    }
}
