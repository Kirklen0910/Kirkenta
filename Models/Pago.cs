namespace Kirkenta.Models
{
    public class Pago
    {
        public int Id { get; set; }
        public int? FacturaId { get; set; }
        public int? VentaId { get; set; }
        public int ClienteId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Monto { get; set; }
        public int MetodoPagoId { get; set; }
        public string? Referencia { get; set; }
        public string? Notas { get; set; }
        public int? UsuarioCreoId { get; set; }

        // ===== EFECTIVO =====
        /// <summary>
        /// Monto que el cliente entregó físicamente. Solo aplica cuando el método
        /// de pago es Efectivo. Null para pagos con tarjeta / transferencia / etc.
        /// </summary>
        public decimal? MontoRecibido { get; set; }

        /// <summary>
        /// Vuelto entregado al cliente. Solo aplica cuando el método de pago es Efectivo
        /// y el monto recibido supera al total. Null para pagos con tarjeta / transferencia.
        /// </summary>
        public decimal? Cambio { get; set; }
    }
}