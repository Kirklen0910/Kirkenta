namespace Kirkenta.Models
{
    /// <summary>
    /// Comprobante de entrega en bodega: una salida física (total o parcial) de lo vendido.
    /// Se pueden emitir varios por venta hasta que lo pendiente llegue a 0.
    /// El inventario se descuenta al vender; el comprobante solo controla qué falta por entregar.
    /// </summary>
    public class EntregaBodega
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public int VentaId { get; set; }
        public int? ClienteId { get; set; }
        public string? RecibidoPor { get; set; }
        public string? Notas { get; set; }
        public int? UsuarioId { get; set; }
        public bool Anulada { get; set; }
        public string? MotivoAnulacion { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public int? UsuarioAnuloId { get; set; }
        public int EmpresaId { get; set; } = Kirkenta.Helpers.EmpresaActual.Id;
    }

    public class DetalleEntregaBodega
    {
        public int Id { get; set; }
        public int EntregaBodegaId { get; set; }
        public int DetalleVentaId { get; set; }
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
    }
}
