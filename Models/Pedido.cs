namespace Kirkenta.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int ClienteId { get; set; }
        public int? CotizacionId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public DateTime? FechaEntrega { get; set; }
        public decimal Subtotal { get; set; } = 0;
        public decimal Descuento { get; set; } = 0;
        public decimal Impuestos { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public string Estado { get; set; } = "Pendiente";
        public string? Notas { get; set; }
        public int? FacturaId { get; set; }
        public int? UsuarioCreoId { get; set; }
    }

    public class DetallePedido
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
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