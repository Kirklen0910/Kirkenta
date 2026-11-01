using System.Globalization;
using System.Text;
using Kirkenta.Services;

namespace Kirkenta.Helpers
{
    /// <summary>Constructor de comandos ESC/POS (impresoras térmicas de tickets). No depende de la base de datos.</summary>
    public sealed class EscPosBuilder
    {
        private readonly List<byte> _b = new();
        private readonly Encoding _enc;
        public int Columnas { get; }

        public EscPosBuilder(int columnas = 42)
        {
            Columnas = Math.Clamp(columnas, 24, 64);
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                _enc = Encoding.GetEncoding(858); // CP858: acentos y ñ del español
            }
            catch { _enc = Encoding.ASCII; }
            Inicializar();
        }

        private void Cmd(params byte[] bytes) => _b.AddRange(bytes);

        /// <summary>ESC @ (reiniciar) y ESC t 19 (tabla de caracteres CP858).</summary>
        public EscPosBuilder Inicializar() { Cmd(0x1B, 0x40); Cmd(0x1B, 0x74, 19); return this; }

        /// <summary>0 = izquierda, 1 = centro, 2 = derecha.</summary>
        public EscPosBuilder Alinear(int a) { Cmd(0x1B, 0x61, (byte)Math.Clamp(a, 0, 2)); return this; }
        public EscPosBuilder Negrita(bool on) { Cmd(0x1B, 0x45, (byte)(on ? 1 : 0)); return this; }
        /// <summary>Doble alto y ancho (el texto ocupa la mitad de columnas).</summary>
        public EscPosBuilder Grande(bool on) { Cmd(0x1D, 0x21, (byte)(on ? 0x11 : 0x00)); return this; }

        public EscPosBuilder Texto(string? s)
        {
            if (!string.IsNullOrEmpty(s)) _b.AddRange(_enc.GetBytes(Limpiar(s)));
            return this;
        }

        public EscPosBuilder Linea(string? s = "") { Texto(s); _b.Add(0x0A); return this; }

        public EscPosBuilder Separador(char c = '-') => Linea(new string(c, Columnas));

        /// <summary>Texto a la izquierda y a la derecha de la misma línea (descripción … monto).</summary>
        public EscPosBuilder Fila(string izq, string der)
        {
            izq ??= ""; der ??= "";
            var libre = Columnas - der.Length - 1;
            if (libre < 1) { Linea(izq); Alinear(2); Linea(der); Alinear(0); return this; }
            if (izq.Length > libre) izq = izq[..libre];
            return Linea(izq.PadRight(libre) + " " + der);
        }

        public EscPosBuilder Avance(int lineas = 1) { Cmd(0x1B, 0x64, (byte)Math.Clamp(lineas, 0, 10)); return this; }

        /// <summary>Avanza el papel y hace el corte parcial.</summary>
        public EscPosBuilder Cortar() { Cmd(0x1D, 0x56, 0x42, 0x03); return this; }

        /// <summary>Pulso para abrir la gaveta de dinero conectada a la impresora (pin 2).</summary>
        public EscPosBuilder Gaveta() { Cmd(0x1B, 0x70, 0x00, 0x19, 0xFA); return this; }

        public byte[] ToArray() => _b.ToArray();

        /// <summary>Quita caracteres de control (un nombre de producto no debe poder inyectar comandos).</summary>
        public static string Limpiar(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s) sb.Append(char.IsControl(ch) ? ' ' : ch);
            return sb.ToString();
        }

        /// <summary>Parte un texto en líneas de máximo <paramref name="ancho"/> caracteres, cortando en espacios.</summary>
        public static List<string> Envolver(string? texto, int ancho)
        {
            var res = new List<string>();
            if (string.IsNullOrWhiteSpace(texto)) return res;
            ancho = Math.Max(8, ancho);
            var actual = new StringBuilder();
            foreach (var palabra in Limpiar(texto).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var p = palabra;
                while (p.Length > ancho)
                {
                    if (actual.Length > 0) { res.Add(actual.ToString()); actual.Clear(); }
                    res.Add(p[..ancho]); p = p[ancho..];
                }
                if (actual.Length == 0) actual.Append(p);
                else if (actual.Length + 1 + p.Length <= ancho) actual.Append(' ').Append(p);
                else { res.Add(actual.ToString()); actual.Clear().Append(p); }
            }
            if (actual.Length > 0) res.Add(actual.ToString());
            return res;
        }
    }

    public record TicketLinea(string Descripcion, decimal Cantidad, decimal Precio, decimal Total);

    /// <summary>Todo lo que lleva un ticket, ya armado (para poder probarlo sin base de datos).</summary>
    public class TicketDatos
    {
        public string Empresa { get; set; } = "";
        public List<string> Cabecera { get; set; } = new();
        public string Titulo { get; set; } = "";
        public List<string> Datos { get; set; } = new();
        public List<TicketLinea> Lineas { get; set; } = new();
        public List<(string Etiqueta, string Valor)> Totales { get; set; } = new();
        public string TotalEtiqueta { get; set; } = "TOTAL";
        public string TotalValor { get; set; } = "";
        public List<string> Pie { get; set; } = new();
    }

    public static class TicketTermico
    {
        public static string M(decimal v) => v.ToString("N2", CultureInfo.InvariantCulture);

        /// <summary>Arma los bytes ESC/POS del ticket según la configuración (columnas, copias, gaveta).</summary>
        public static byte[] Construir(TicketDatos d, ImpresoraConfig cfg)
        {
            var b = new EscPosBuilder(cfg.Columnas);
            var cols = b.Columnas;
            var salida = new List<byte>();

            for (int copia = 0; copia < Math.Max(1, cfg.Copias); copia++)
            {
                b = new EscPosBuilder(cfg.Columnas);
                // La gaveta se abre una sola vez, antes de la primera copia
                if (copia == 0 && cfg.AbrirGaveta) b.Gaveta();

                b.Alinear(1).Negrita(true).Linea(d.Empresa).Negrita(false);
                foreach (var l in d.Cabecera) foreach (var w in EscPosBuilder.Envolver(l, cols)) b.Linea(w);
                b.Alinear(0).Separador();

                if (!string.IsNullOrWhiteSpace(d.Titulo)) b.Negrita(true).Linea(d.Titulo).Negrita(false);
                foreach (var l in d.Datos) foreach (var w in EscPosBuilder.Envolver(l, cols)) b.Linea(w);
                b.Separador();

                foreach (var li in d.Lineas)
                {
                    var desc = EscPosBuilder.Envolver(li.Descripcion, cols);
                    foreach (var w in desc) b.Linea(w);
                    b.Fila($"{li.Cantidad.ToString("0.##", CultureInfo.InvariantCulture)} x {M(li.Precio)}", M(li.Total));
                }
                b.Separador();

                foreach (var (etiqueta, valor) in d.Totales) b.Fila(etiqueta, valor);
                b.Negrita(true).Fila(d.TotalEtiqueta, d.TotalValor).Negrita(false);
                b.Separador();

                b.Alinear(1);
                foreach (var l in d.Pie) foreach (var w in EscPosBuilder.Envolver(l, cols)) b.Linea(w);
                b.Alinear(0).Avance(3).Cortar();
                salida.AddRange(b.ToArray());
            }
            return salida.ToArray();
        }

        /// <summary>Ticket corto para comprobar la conexión con la impresora.</summary>
        public static byte[] Prueba(ImpresoraConfig cfg)
        {
            var d = new TicketDatos
            {
                Empresa = "Kirkenta",
                Titulo = "PRUEBA DE IMPRESION",
                Datos = new List<string> { "Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), "Acentos: á é í ó ú ñ Ñ ü" },
                Lineas = new List<TicketLinea> { new("Producto de ejemplo", 2, 50m, 100m) },
                Totales = new List<(string, string)> { ("Subtotal", M(100m)) },
                TotalValor = M(100m),
                Pie = new List<string> { "Si lees esto, la impresora funciona." }
            };
            return Construir(d, cfg);
        }
    }
}
