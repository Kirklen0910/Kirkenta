namespace Kirkenta.Models
{
    public class PagoProveedor
    {
        public int Id { get; set; }
        public string? Numero { get; set; }
        public int ProveedorId { get; set; }
        public int? OrdenCompraId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Monto { get; set; }
        public int MetodoPagoId { get; set; }
        public string? Referencia { get; set; }
        public int MonedaId { get; set; } = 1;
        public decimal TipoCambio { get; set; } = 1;
        public string? Notas { get; set; }
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}