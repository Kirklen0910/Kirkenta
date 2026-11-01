namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Lectura de valores de una fila de importación avisando siempre al usuario:
    /// si un valor no se puede leer o se recorta, queda como advertencia en el reporte
    /// (antes se ignoraba o se cortaba en silencio).
    /// </summary>
    public static class ImportHelper
    {
        /// <summary>Texto limpio, null si viene vacío. Si excede el máximo se recorta y se avisa.</summary>
        public static string? Texto(ImportResult r, int fila, string etiqueta, string? valor, int max)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var v = valor.Trim();
            if (v.Length <= max) return v;

            r.AgregarAdvertencia(fila, $"'{etiqueta}' excede {max} caracteres; se recortó.");
            return v.Substring(0, max);
        }

        public static decimal? Decimal(ImportResult r, int fila, string etiqueta, string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var d = ParserHelper.ParsearDecimal(valor);
            if (d == null)
                r.AgregarAdvertencia(fila, $"'{etiqueta}': el valor '{valor}' no es un número; se ignoró.");
            return d;
        }

        public static int? Entero(ImportResult r, int fila, string etiqueta, string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var d = ParserHelper.ParsearInt(valor);
            if (d == null)
                r.AgregarAdvertencia(fila, $"'{etiqueta}': el valor '{valor}' no es un número entero; se ignoró.");
            return d;
        }

        public static bool? Booleano(ImportResult r, int fila, string etiqueta, string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var b = ParserHelper.ParsearBool(valor);
            if (b == null)
                r.AgregarAdvertencia(fila, $"'{etiqueta}': el valor '{valor}' no se reconoce como Sí/No; se ignoró.");
            return b;
        }

        /// <summary>
        /// Ejecuta el guardado dentro de una transacción. Si falla, deja todo como estaba
        /// (sin cambios a medias) y lo informa como error en vez de romper la página.
        /// </summary>
        public static void GuardarSeguro(Kirkenta.Data.ApplicationDbContext context, ImportResult r, Action guardar)
        {
            using var tx = context.Database.BeginTransaction();
            try
            {
                guardar();
                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                context.ChangeTracker.Clear();
                var msg = ex.InnerException?.Message ?? ex.Message;
                r.AgregarError(0, $"No se pudo guardar y no se importó nada: {msg}");
                r.Creados = 0;
                r.Actualizados = 0;
                r.SinCambios = 0;
            }
        }
    }
}
