using System.Collections.Concurrent;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Logger sencillo a archivo: logs/kirkenta-AAAAMMDD.log (uno por día).
    /// Solo registra Warning o superior para no crecer sin control.
    /// </summary>
    public sealed class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _carpeta;
        private readonly LogLevel _minimo;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
        private static readonly object Candado = new();

        public FileLoggerProvider(string carpeta, LogLevel minimo = LogLevel.Warning)
        {
            _carpeta = carpeta;
            _minimo = minimo;
            try { Directory.CreateDirectory(_carpeta); } catch { /* sin permisos: se ignora */ }
        }

        public ILogger CreateLogger(string categoryName)
            => _loggers.GetOrAdd(categoryName, n => new FileLogger(n, _carpeta, _minimo));

        public void Dispose() { _loggers.Clear(); }

        private sealed class FileLogger : ILogger
        {
            private readonly string _categoria;
            private readonly string _carpeta;
            private readonly LogLevel _minimo;

            public FileLogger(string categoria, string carpeta, LogLevel minimo)
            {
                _categoria = categoria;
                _carpeta = carpeta;
                _minimo = minimo;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= _minimo;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;

                try
                {
                    var linea = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {_categoria}: {formatter(state, exception)}";
                    if (exception != null) linea += Environment.NewLine + exception;

                    var archivo = Path.Combine(_carpeta, $"kirkenta-{DateTime.Now:yyyyMMdd}.log");
                    lock (Candado)
                    {
                        File.AppendAllText(archivo, linea + Environment.NewLine);
                    }
                }
                catch
                {
                    // el log nunca debe tumbar la aplicación
                }
            }
        }
    }
}
