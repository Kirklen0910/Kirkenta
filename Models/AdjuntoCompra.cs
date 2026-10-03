namespace Kirkenta.Models
{
    public class AdjuntoCompra
    {
        public int Id { get; set; }
        public int? OrdenCompraId { get; set; }
        public int? PagoProveedorId { get; set; }
        public string TipoDocumento { get; set; } = "Factura";
        public string? NombreArchivo { get; set; }
        public string? RutaArchivo { get; set; }
        public string? TipoArchivo { get; set; }
        public int TamanoKB { get; set; }
        public DateTime FechaSubida { get; set; } = DateTime.Now;
        public int? UsuarioId { get; set; }
    }
}