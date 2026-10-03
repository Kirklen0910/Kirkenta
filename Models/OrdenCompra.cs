namespace Kirkenta.Models
{
    public class OrdenCompra
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int ProveedorId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public DateTime? FechaEntregaEstimada { get; set; }
        public DateTime? FechaRecepcion { get; set; }
        public string Estado { get; set; } = "Borrador";
        public decimal Subtotal { get; set; } = 0;
        public decimal Descuento { get; set; } = 0;
        public decimal Impuestos { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public decimal Saldo { get; set; } = 0;
        public int MonedaId { get; set; } = 1;
        public decimal TipoCambio { get; set; } = 1;
        public string? Notas { get; set; }
        public int? UsuarioCreoId { get; set; }
        public int? UsuarioRecibioId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }

    public class DetalleOrdenCompra
    {
        public int Id { get; set; }
        public int OrdenCompraId { get; set; }
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CantidadRecibida { get; set; } = 0;
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; } = 0;
        public decimal ImpuestoPorcentaje { get; set; } = 0;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
    }
}