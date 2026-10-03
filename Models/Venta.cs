namespace Kirkenta.Models
{
    public class Venta
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int? ClienteId { get; set; }
        public int? CotizacionId { get; set; }
        public int? PedidoId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Subtotal { get; set; } = 0;
        public decimal Descuento { get; set; } = 0;
        public decimal Impuestos { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public string Estado { get; set; } = "Completada";
        public string? Notas { get; set; }
        public int? UsuarioCreoId { get; set; }
    }

    public class DetalleVenta
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
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