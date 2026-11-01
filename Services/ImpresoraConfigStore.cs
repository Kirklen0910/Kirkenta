using System.Text.Json;

namespace Kirkenta.Services
{
    /// <summary>Configuración de la impresora de tickets (térmica ESC/POS) y de la gaveta de dinero.</summary>
    public class ImpresoraConfig
    {
        /// <summary>"ninguna" | "red" (impresora con IP, puerto 9100) | "windows" (instalada en Windows, por nombre).</summary>
        public string Modo { get; set; } = "ninguna";
        public string Host { get; set; } = "";
        public int Puerto { get; set; } = 9100;
        /// <summary>Nombre exacto de la impresora en Windows (Configuración → Impresoras).</summary>
        public string Impresora { get; set; } = "";
        /// <summary>Caracteres por línea: 32 (58 mm), 42 o 48 (80 mm).</summary>
        public int Columnas { get; set; } = 42;
        public bool AbrirGaveta { get; set; }
        /// <summary>"ninguno" | "venta" (al cobrar en el POS) | "factura" (al emitir la factura, con CAI).</summary>
        public string Auto { get; set; } = "ninguno";
        public int Copias { get; set; } = 1;
        public string Pie { get; set; } = "¡Gracias por su compra!";
    }

    /// <summary>Guarda la configuración de la impresora en App_Data/impresora.json.</summary>
    public class ImpresoraConfigStore
    {
        private readonly string _ruta;
        private readonly object _lock = new();
        private ImpresoraConfig? _cache;

        public ImpresoraConfigStore(IWebHostEnvironment env)
        {
            _ruta = Path.Combine(env.ContentRootPath, "App_Data", "impresora.json");
        }

        public ImpresoraConfig Obtener()
        {
            lock (_lock)
            {
                if (_cache == null)
                {
                    try { _cache = File.Exists(_ruta) ? JsonSerializer.Deserialize<ImpresoraConfig>(File.ReadAllText(_ruta)) : null; }
                    catch { _cache = null; }
                    _cache = Normalizar(_cache ?? new ImpresoraConfig());
                }
                return Copia(_cache);
            }
        }

        public void Guardar(ImpresoraConfig c)
        {
            lock (_lock)
            {
                var n = Normalizar(c);
                Directory.CreateDirectory(Path.GetDirectoryName(_ruta)!);
                File.WriteAllText(_ruta, JsonSerializer.Serialize(n, new JsonSerializerOptions { WriteIndented = true }));
                _cache = n;
            }
        }

        private static ImpresoraConfig Copia(ImpresoraConfig c) => new()
        {
            Modo = c.Modo, Host = c.Host, Puerto = c.Puerto, Impresora = c.Impresora, Columnas = c.Columnas,
            AbrirGaveta = c.AbrirGaveta, Auto = c.Auto, Copias = c.Copias, Pie = c.Pie
        };

        /// <summary>Deja valores válidos (nunca lanza): modo y auto conocidos, columnas y copias en rango.</summary>
        public static ImpresoraConfig Normalizar(ImpresoraConfig c)
        {
            c.Modo = c.Modo is "red" or "windows" ? c.Modo : "ninguna";
            c.Auto = c.Auto is "venta" or "factura" ? c.Auto : "ninguno";
            c.Host = (c.Host ?? "").Trim();
            c.Impresora = (c.Impresora ?? "").Trim();
            c.Pie = (c.Pie ?? "").Trim();
            if (c.Pie.Length > 120) c.Pie = c.Pie[..120];
            if (c.Puerto < 1 || c.Puerto > 65535) c.Puerto = 9100;
            c.Columnas = c.Columnas <= 0 ? 42 : Math.Clamp(c.Columnas, 24, 64);
            c.Copias = Math.Clamp(c.Copias, 1, 3);
            return c;
        }

        /// <summary>Texto de error si la configuración no sirve para imprimir; null si está bien.</summary>
        public static string? Validar(ImpresoraConfig c)
        {
            if (c.Modo == "red" && string.IsNullOrWhiteSpace(c.Host))
                return "Escribe la dirección IP de la impresora (por ejemplo 192.168.1.50).";
            if (c.Modo == "windows" && string.IsNullOrWhiteSpace(c.Impresora))
                return "Escribe el nombre de la impresora tal como aparece en Windows.";
            return null;
        }
    }
}
