namespace Kirkenta.Models
{
    public class SerieDocumento
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;      // Cotizacion | Factura | Pedido | Venta | Devolucion
        public string Nombre { get; set; } = "Principal";
        public string Prefijo { get; set; } = string.Empty;
        public string? Sufijo { get; set; }
        public string Separador { get; set; } = "-";
        public int LongitudNumero { get; set; } = 4;
        public int SiguienteNumero { get; set; } = 1;
        public string? FormatoPersonalizado { get; set; }
        public bool EsPredeterminada { get; set; } = false;
        public bool Activa { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}