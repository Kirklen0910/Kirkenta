using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Helpers
{
    /// <summary>Formato de documentos de Honduras: RTN (14 dígitos) e Identidad (13 dígitos).</summary>
    public static class DocumentoHonduras
    {
        public const int LargoRtn = 14;
        public const int LargoIdentidad = 13;

        public static string SoloDigitos(string? valor)
            => new string((valor ?? "").Where(char.IsDigit).ToArray());

        /// <summary>0801-1990-123456. Si no tiene exactamente 14 dígitos devuelve el valor tal cual (la validación lo reporta).</summary>
        public static string? FormatearRtn(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var d = SoloDigitos(valor);
            return d.Length == LargoRtn ? $"{d[..4]}-{d[4..8]}-{d[8..]}" : valor.Trim();
        }

        /// <summary>0801-1990-12345. Si no tiene exactamente 13 dígitos devuelve el valor tal cual (la validación lo reporta).</summary>
        public static string? FormatearIdentidad(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var d = SoloDigitos(valor);
            return d.Length == LargoIdentidad ? $"{d[..4]}-{d[4..8]}-{d[8..]}" : valor.Trim();
        }

        internal static bool Valido(string? valor, int largo)
        {
            if (string.IsNullOrWhiteSpace(valor)) return true; // campo opcional
            if (valor.Any(c => !(char.IsDigit(c) || c == '-' || c == ' '))) return false;
            return SoloDigitos(valor).Length == largo;
        }
    }

    /// <summary>[Rtn]: opcional; si se escribe debe tener exactamente 14 dígitos.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class RtnAttribute : ValidationAttribute
    {
        public RtnAttribute() : base("El RTN debe tener exactamente 14 dígitos (formato 0000-0000-000000)") { }
        public override bool IsValid(object? value) => DocumentoHonduras.Valido(value as string, DocumentoHonduras.LargoRtn);
    }

    /// <summary>[Identidad]: si se escribe debe tener exactamente 13 dígitos (formato 0000-0000-00000).</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class IdentidadAttribute : ValidationAttribute
    {
        public IdentidadAttribute() : base("La identidad debe tener exactamente 13 dígitos (formato 0000-0000-00000)") { }
        public override bool IsValid(object? value) => DocumentoHonduras.Valido(value as string, DocumentoHonduras.LargoIdentidad);
    }
}
