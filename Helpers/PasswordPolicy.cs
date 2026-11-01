namespace Kirkenta.Helpers
{
    /// <summary>Reglas minimas para contrasenas NUEVAS (las existentes no se tocan).</summary>
    public static class PasswordPolicy
    {
        public const int LargoMinimo = 8;

        private static readonly HashSet<string> Comunes = new(StringComparer.OrdinalIgnoreCase)
        {
            "12345678", "123456789", "1234567890", "password", "password1", "contrasena", "contrasena1",
            "qwertyui", "qwerty123", "abc12345", "admin123", "admin1234", "kirkenta1", "kirkenta123",
            "11111111", "00000000", "iloveyou1", "letmein123", "welcome123"
        };

        /// <summary>Devuelve el mensaje de error, o null si la contrasena cumple.</summary>
        public static string? Validar(string? password, string? username = null)
        {
            if (string.IsNullOrEmpty(password)) return "La contraseña es obligatoria";
            if (password.Length < LargoMinimo) return $"La contraseña debe tener al menos {LargoMinimo} caracteres";
            if (password.Length > 100) return "La contraseña es demasiado larga (máximo 100)";
            if (!password.Any(char.IsLetter)) return "La contraseña debe incluir al menos una letra";
            if (!password.Any(char.IsDigit)) return "La contraseña debe incluir al menos un número";
            if (Comunes.Contains(password)) return "Esa contraseña es demasiado común, elige otra";
            if (!string.IsNullOrWhiteSpace(username) &&
                password.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase))
                return "La contraseña no puede ser igual al nombre de usuario";
            return null;
        }
    }
}
