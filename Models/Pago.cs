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
    }
}