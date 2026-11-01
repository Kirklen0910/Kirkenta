using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>
    /// Estado de la migración que está corriendo en este hilo: textos que hubo que recortar y avance del paso.
    /// La migración corre de punta a punta en un solo hilo, así que no se mezcla con otras solicitudes.
    /// </summary>
    public static class MigracionCorrida
    {
        [ThreadStatic] private static Dictionary<string, string>? _recortes;
        [ThreadStatic] private static Action<double>? _avance;
        [ThreadStatic] private static int _ultimo;

        internal static void Iniciar(Action<double>? avance)
        {
            _recortes = new Dictionary<string, string>();
            _avance = avance;
            _ultimo = -1;
        }

        internal static void Terminar() { _recortes = null; _avance = null; }

        /// <summary>Recortes del paso actual (campo → texto completo) y los vacía.</summary>
        internal static List<(string campo, string texto)> TomarRecortes()
        {
            var l = _recortes?.Select(x => (x.Value, x.Key.Substring(x.Key.IndexOf('\u0001') + 1))).ToList() ?? new();
            _recortes?.Clear();
            return l;
        }

        internal static void Recorte(string campo, string original, int max)
        {
            if (_recortes == null) return;
            _recortes.TryAdd(campo + "\u0001" + original, $"{campo} (máx. {max})");
        }

        [ThreadStatic] private static int _fila;

        internal static void NuevoPaso() { _fila = 0; _ultimo = -1; }

        /// <summary>Una fila más procesada de las n del paso actual.</summary>
        public static void Fila(int n) => Avance(++_fila, n);

        /// <summary>Avance dentro del paso actual (fila i de n).</summary>
        public static void Avance(int i, int n)
        {
            if (_avance == null || n <= 0) return;
            var milesimas = (int)(Math.Min(1.0, i / (double)n) * 1000);
            if (milesimas == _ultimo) return;
            _ultimo = milesimas;
            _avance(milesimas / 1000.0);
        }

        /// <summary>
        /// Red de seguridad antes de guardar: un texto más largo que su columna haría fallar toda la migración
        /// en MySQL; se recorta y queda anotado para el informe.
        /// </summary>
        internal static void AjustarTextos(DbContext ctx)
        {
            if (_recortes == null) return;
            foreach (var e in ctx.ChangeTracker.Entries())
            {
                if (e.State != EntityState.Added && e.State != EntityState.Modified) continue;
                foreach (var p in e.Properties)
                {
                    if (p.Metadata.ClrType != typeof(string) || p.CurrentValue is not string s) continue;
                    var max = p.Metadata.GetMaxLength();
                    if (max == null || s.Length <= max) continue;
                    Recorte($"{e.Metadata.ClrType.Name}.{p.Metadata.Name}", s, max.Value);
                    p.CurrentValue = s.Substring(0, max.Value);
                }
            }
        }
    }
}
