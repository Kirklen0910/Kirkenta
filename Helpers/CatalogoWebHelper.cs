using System.Globalization;
using System.Text;

namespace Kirkenta.Helpers
{
    /// <summary>Reglas del catálogo en línea: validación del contacto, precios con ISV y mensaje de WhatsApp.</summary>
    public static class CatalogoWebHelper
    {
        public const int MaxLineas = 40;
        public const decimal MaxCantidadPorLinea = 999;
        public const int MaxPedidosPorVentana = 5;
        public static readonly TimeSpan Ventana = TimeSpan.FromMinutes(10);

        public class LineaCalculada
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";
            public decimal Cantidad { get; set; }
            public decimal Precio { get; set; }
            public decimal Subtotal => Math.Round(Cantidad * Precio, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>Precio final al público: precio de venta más el ISV del producto.</summary>
        public static decimal PrecioConImpuesto(decimal precio, decimal tasaPorcentaje)
            => Math.Round(precio * (1 + tasaPorcentaje / 100m), 2, MidpointRounding.AwayFromZero);

        /// <summary>Devuelve el mensaje de error o null si el contacto es válido.</summary>
        public static string? ValidarContacto(string? nombre, string? telefono)
        {
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length < 2) return "Escribe tu nombre.";
            if (nombre.Trim().Length > 120) return "El nombre es demasiado largo.";
            var digitos = (telefono ?? "").Count(char.IsDigit);
            if (digitos < 8 || digitos > 15) return "Escribe un teléfono válido para poder contactarte.";
            return null;
        }

        public static string Limpiar(string? texto, int max)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            var t = new string(texto.Where(c => !char.IsControl(c) || c == '\n').ToArray()).Trim();
            return t.Length > max ? t.Substring(0, max) : t;
        }

        private static string L(decimal v) => "L. " + v.ToString("N2", CultureInfo.InvariantCulture);

        public static string MensajeWhatsApp(string negocio, string numero, IEnumerable<LineaCalculada> lineas,
            decimal total, string nombre, string telefono, string? direccion, string? notas)
        {
            var sb = new StringBuilder();
            sb.Append($"Hola *{negocio.Trim()}*, quiero hacer este pedido ({numero}):\n\n");
            foreach (var l in lineas)
                sb.Append($"• {l.Cantidad.ToString("0.##", CultureInfo.InvariantCulture)} × {l.Nombre} — {L(l.Subtotal)}\n");
            sb.Append($"\n*Total: {L(total)}* (ISV incluido)\n\n");
            sb.Append($"*Nombre:* {nombre}\n*Teléfono:* {telefono}\n");
            if (!string.IsNullOrWhiteSpace(direccion)) sb.Append($"*Entrega en:* {direccion}\n");
            if (!string.IsNullOrWhiteSpace(notas)) sb.Append($"*Notas:* {notas}\n");
            return sb.ToString().TrimEnd();
        }
    }
}
