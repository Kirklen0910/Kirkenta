using System.Globalization;
using System.Text;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Enlaces "click to chat" de WhatsApp (https://wa.me). No requiere cuenta de Meta ni API:
    /// abre WhatsApp con el mensaje ya escrito y el usuario solo presiona enviar.
    /// </summary>
    public static class WhatsAppHelper
    {
        /// <summary>
        /// Devuelve el número en formato internacional solo con dígitos (ej. 50499999999) o null si no es utilizable.
        /// Acepta "+504 9999-9999". Si viene un número local sin "+" se asume el código del país indicado.
        /// </summary>
        public static string? Normalizar(string? telefono, string paisIso = TelefonoHelper.PaisPorDefecto)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return null;
            var s = telefono.Trim();
            var digitos = new string(s.Where(char.IsDigit).ToArray());
            if (digitos.Length == 0) return null;

            if (s.StartsWith('+'))
                return TelefonoHelper.EsValido("+" + digitos, out _) ? digitos : null;

            var pais = TelefonoHelper.Paises.FirstOrDefault(p => p.Iso == paisIso)
                       ?? TelefonoHelper.Paises.First(p => p.Iso == TelefonoHelper.PaisPorDefecto);
            if (digitos.StartsWith(pais.Codigo) && pais.Largos.Contains(digitos.Length - pais.Codigo.Length))
                return digitos;
            if (pais.Largos.Contains(digitos.Length)) return pais.Codigo + digitos;
            return null;
        }

        /// <summary>Enlace wa.me con el texto precargado, o null si el teléfono no sirve.</summary>
        public static string? Enlace(string? telefono, string? mensaje, string paisIso = TelefonoHelper.PaisPorDefecto)
        {
            var num = Normalizar(telefono, paisIso);
            if (num == null) return null;
            return string.IsNullOrWhiteSpace(mensaje)
                ? $"https://wa.me/{num}"
                : $"https://wa.me/{num}?text={Uri.EscapeDataString(mensaje)}";
        }

        private static string L(decimal v) => "L. " + v.ToString("N2", CultureInfo.InvariantCulture);
        private static string F(DateTime d) => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        private static string Nom(string? e) => string.IsNullOrWhiteSpace(e) ? "Cliente" : e.Trim();

        // Formato de WhatsApp: *negrita*, saltos de línea. Se redacta de usted y con firma de la empresa.
        private static string Abre(string? cliente, string empresa)
            => $"Estimado(a) {Nom(cliente)},\n\nReciba un cordial saludo de *{empresa.Trim()}*.\n\n";

        private static string Cierra(string empresa, string frase)
            => $"\n{frase}\n\nAtentamente,\n*{empresa.Trim()}*";

        public static string MensajeFactura(string empresa, string? cliente, string numero, decimal total, decimal saldo, DateTime? vence)
        {
            var sb = new StringBuilder(Abre(cliente, empresa));
            sb.Append("Le compartimos los datos de su factura:\n\n");
            sb.Append($"*Factura:* {numero}\n");
            sb.Append($"*Total:* {L(total)}\n");
            if (saldo > 0)
            {
                sb.Append($"*Saldo pendiente:* {L(saldo)}\n");
                if (vence.HasValue) sb.Append($"*Fecha de vencimiento:* {F(vence.Value)}\n");
            }
            sb.Append(Cierra(empresa, "Agradecemos su preferencia. Para cualquier consulta quedamos a sus órdenes."));
            return sb.ToString();
        }

        public static string MensajeCotizacion(string empresa, string? cliente, string numero, decimal total, DateTime? vence)
        {
            var sb = new StringBuilder(Abre(cliente, empresa));
            sb.Append("Le compartimos la cotización solicitada:\n\n");
            sb.Append($"*Cotización:* {numero}\n");
            sb.Append($"*Total:* {L(total)}\n");
            if (vence.HasValue) sb.Append($"*Válida hasta:* {F(vence.Value)}\n");
            sb.Append(Cierra(empresa, "Quedamos atentos a cualquier consulta o ajuste que necesite."));
            return sb.ToString();
        }

        public static string MensajePedido(string empresa, string? cliente, string numero, string estado, decimal total)
            => Abre(cliente, empresa)
             + "Le informamos sobre su pedido:\n\n"
             + $"*Pedido:* {numero}\n*Estado:* {estado}\n*Total:* {L(total)}\n"
             + Cierra(empresa, "Agradecemos su confianza. Quedamos a sus órdenes.");

        public static string MensajeEnvio(string empresa, string? contacto, string numero, string estado)
            => Abre(contacto, empresa)
             + "Le informamos sobre el estado de su envío:\n\n"
             + $"*Envío:* {numero}\n*Estado:* {estado}\n"
             + Cierra(empresa, "Si necesita coordinar la entrega, con gusto le atendemos.");

        public static string MensajeGeneral(string empresa, string? cliente)
            => Abre(cliente, empresa) + "Nos comunicamos con usted para ponernos a sus órdenes."
             + Cierra(empresa, "¿En qué podemos servirle?");

        public static string MensajeEstadoCuenta(string empresa, string? cliente, decimal saldo)
            => Abre(cliente, empresa)
             + "Le compartimos su estado de cuenta actualizado.\n\n"
             + $"*Saldo pendiente:* {L(saldo)}\n"
             + Cierra(empresa, "Agradecemos su preferencia. Para cualquier consulta quedamos a sus órdenes.");

        public static string MensajeDevolucion(string empresa, string? cliente, string numero, decimal total)
            => Abre(cliente, empresa)
             + "Le compartimos el comprobante de su devolución:\n\n"
             + $"*Nota de crédito:* {numero}\n*Total:* {L(total)}\n"
             + Cierra(empresa, "Quedamos a sus órdenes para cualquier consulta.");

        public static string MensajeOrdenCompra(string empresa, string? proveedor, string numero, decimal total, DateTime? entrega)
        {
            var sb = new StringBuilder(Abre(proveedor, empresa));
            sb.Append("Le compartimos nuestra orden de compra:\n\n");
            sb.Append($"*Orden de compra:* {numero}\n");
            sb.Append($"*Total:* {L(total)}\n");
            if (entrega.HasValue) sb.Append($"*Entrega esperada:* {F(entrega.Value)}\n");
            sb.Append(Cierra(empresa, "Le agradecemos confirmarnos la recepción y la fecha de entrega."));
            return sb.ToString();
        }

        /// <summary>Recordatorio cordial de cobranza. Si hay saldo vencido lo destaca.</summary>
        public static string MensajeCobranza(string empresa, string? cliente, decimal saldo, decimal vencido)
        {
            var sb = new StringBuilder(Abre(cliente, empresa));
            sb.Append("Le recordamos amablemente el estado de su cuenta:\n\n");
            sb.Append($"*Saldo pendiente:* {L(saldo)}\n");
            if (vencido > 0) sb.Append($"*Saldo vencido:* {L(vencido)}\n");
            sb.Append(Cierra(empresa, "Si ya realizó su pago, le agradecemos compartirnos el comprobante y hacer caso omiso de este mensaje."));
            return sb.ToString();
        }
    }
}

namespace Kirkenta.Helpers
{
    /// <summary>Datos para el parcial Shared/_WhatsAppBtn.</summary>
    public record WhatsAppBtn(string? Url, string Texto = "WhatsApp", bool Compacto = false, string Titulo = "Enviar por WhatsApp");

    /// <summary>
    /// Datos para el parcial Shared/_WhatsAppPdf: celular + "PDF por WhatsApp". DocUrl = página con kkGenerarPdf
    /// (ej. Facturas/Carta); si es null, el PDF sale de la sección [data-pdf-doc] de la página actual.
    /// </summary>
    public record WhatsAppPdf(string? Telefono, string Mensaje, string Archivo, string? DocUrl = null);
}
