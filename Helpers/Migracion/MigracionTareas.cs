using System.Collections.Concurrent;

namespace Kirkenta.Helpers.Migracion
{
    /// <summary>Una revisión o migración corriendo en segundo plano (la página consulta su avance).</summary>
    public class TareaMigracion
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public string Token { get; init; } = "";
        public bool Aplicar { get; init; }
        public Dictionary<int, string> Destino { get; init; } = new();
        public string FormatoFecha { get; init; } = "dmy";
        public DateTime Inicio { get; } = DateTime.Now;
        public volatile int Porcentaje;
        public volatile string Etapa = "Preparando…";
        public volatile bool Terminada;
        public ResultadoAutomatico? Resultado;

        public void Avanzar(int pct, string etapa)
        {
            if (pct > Porcentaje) Porcentaje = pct;
            Etapa = etapa;
        }
    }

    public static class MigracionTareas
    {
        private static readonly ConcurrentDictionary<string, TareaMigracion> Tareas = new();

        public static TareaMigracion? Obtener(string? id) => id != null && Tareas.TryGetValue(id, out var t) ? t : null;

        /// <summary>La que está corriendo ahora (solo se permite una a la vez: comparten la base).</summary>
        public static TareaMigracion? EnCurso() => Tareas.Values.FirstOrDefault(t => !t.Terminada);

        private static readonly object Candado = new();

        /// <summary>
        /// Corre el trabajo en un hilo propio; los errores no se pierden: quedan en el resultado.
        /// Devuelve null si ya hay otra corriendo.
        /// </summary>
        public static TareaMigracion? Iniciar(TareaMigracion t, Func<TareaMigracion, ResultadoAutomatico> trabajo)
        {
            lock (Candado)
            {
                if (EnCurso() != null) return null;
                Tareas[t.Id] = t;
            }
            foreach (var vieja in Tareas.Values.Where(x => x.Terminada && x.Inicio < DateTime.Now.AddHours(-12)).ToList())
                Tareas.TryRemove(vieja.Id, out _);
            Task.Factory.StartNew(() =>
            {
                try { t.Resultado = trabajo(t); }
                catch (Exception ex) { t.Resultado = new ResultadoAutomatico { ErrorFatal = "Se canceló todo y NO se guardó nada: " + (ex.InnerException?.Message ?? ex.Message) }; }
                finally { t.Porcentaje = 100; t.Etapa = "Listo"; t.Terminada = true; }
            }, TaskCreationOptions.LongRunning);
            return t;
        }
    }
}
