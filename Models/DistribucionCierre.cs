using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Distribución del efectivo contado en un cierre de caja.
    /// Cada cierre puede tener varias distribuciones: retiro a banco, fondo
    /// para mañana, entrega a administración, etc.
    /// La suma de las distribuciones debe igualar el EfectivoContado del cierre.
    /// </summary>
    public class DistribucionCierre
    {
        public int Id { get; set; }

        public int CierreCajaId { get; set; }

        /// <summary>
        /// Tipo: "RetiroBanco" | "FondoCaja" | "EntregaAdmin" | "PagoDirecto" | "Otro"
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Tipo { get; set; } = "FondoCaja";

        public decimal Monto { get; set; }

        /// <summary>
        /// Cuenta destino (si es RetiroBanco)
        /// </summary>
        public int? CuentaDestinoId { get; set; }

        /// <summary>
        /// Descripción libre del destino (si no es cuenta)
        /// </summary>
        [StringLength(200)]
        public string? DestinoDescripcion { get; set; }

        /// <summary>
        /// Referencia externa: # de depósito, cheque, etc.
        /// </summary>
        [StringLength(100)]
        public string? Referencia { get; set; }

        /// <summary>
        /// Usuario que recibe el dinero (si es EntregaAdmin o PagoDirecto)
        /// </summary>
        public int? UsuarioRecibeId { get; set; }

        /// <summary>
        /// Nombre de quien recibe (si no es usuario del sistema)
        /// </summary>
        [StringLength(150)]
        public string? NombreRecibe { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        /// <summary>
        /// Movimiento financiero generado por esta distribución
        /// </summary>
        public int? MovimientoId { get; set; }
    }
}