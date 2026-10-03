namespace Kirkenta.Models
{
    public class DevolucionProveedor
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int OrdenCompraId { get; set; }
        public int ProveedorId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string? Motivo { get; set; }
        public decimal Total { get; set; } = 0;
        public string Estado { get; set; } = "Aplicada";
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }

    public class DetalleDevolucionProveedor
    {
        public int Id { get; set; }
        public int DevolucionId { get; set; }
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }
    }
}