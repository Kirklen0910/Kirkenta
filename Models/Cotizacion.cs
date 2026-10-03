namespace Kirkenta.Models
{
    public class Cotizacion
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int ClienteId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public DateTime? FechaVencimiento { get; set; }
        public decimal Subtotal { get; set; } = 0;
        public decimal Descuento { get; set; } = 0;
        public decimal Impuestos { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public string Estado { get; set; } = "Borrador";
        public string? Notas { get; set; }
        public int Validez { get; set; } = 30;
        public int? FacturaId { get; set; }
        public DateTime? FechaConversion { get; set; }
        public int? UsuarioCreoId { get; set; }
    }

    public class DetalleCotizacion
    {
        public int Id { get; set; }
        public int CotizacionId { get; set; }
        public int ProductoId { get; set; }
        public string? Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; } = 0;
        public decimal ImpuestoPorcentaje { get; set; } = 0;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
    }
}