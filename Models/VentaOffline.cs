using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Registro de las ventas que el POS hizo sin internet y luego sincronizó. Guarda el identificador
    /// que generó el navegador para que, si el envío se repite (se cayó la señal justo al responder),
    /// la venta no se registre dos veces.
    /// </summary>
    public class VentaOffline
    {
        public int Id { get; set; }

        [Required, StringLength(64)]
        public string OfflineId { get; set; } = string.Empty;

        public int VentaId { get; set; }

        [StringLength(40)]
        public string? Numero { get; set; }

        public decimal Total { get; set; }

        public DateTime FechaSincronizacion { get; set; } = DateTime.Now;

        public int EmpresaId { get; set; } = Kirkenta.Helpers.EmpresaActual.Id;
    }
}
