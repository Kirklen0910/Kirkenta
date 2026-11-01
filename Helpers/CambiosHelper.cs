using System.Globalization;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Arma el texto "Campo: antes → después" solo con los campos que realmente cambiaron,
    /// para dejarlo en la bitácora de actividad (auditoría con valor anterior y nuevo).
    /// </summary>
    public static class CambiosHelper
    {
        public static string Describir(params (string Campo, object? Antes, object? Despues)[] campos)
        {
            var partes = new List<string>();
            foreach (var (campo, antes, despues) in campos)
            {
                var a = Formatear(antes);
                var d = Formatear(despues);
                if (a != d) partes.Add($"{campo}: {a} → {d}");
            }
            return partes.Count == 0 ? "sin cambios en los campos vigilados" : string.Join("; ", partes);
        }

        private static string Formatear(object? valor)
        {
            return valor switch
            {
                null => "(vacío)",
                bool b => b ? "Sí" : "No",
                decimal m => m.ToString("0.####", CultureInfo.InvariantCulture),
                double dbl => dbl.ToString("0.####", CultureInfo.InvariantCulture),
                DateTime f => f.ToString("yyyy-MM-dd"),
                string s => string.IsNullOrWhiteSpace(s) ? "(vacío)" : s,
                _ => Convert.ToString(valor, CultureInfo.InvariantCulture) ?? "(vacío)"
            };
        }
    }
}
