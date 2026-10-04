using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Pago individual de la nómina a un empleado específico.
    /// Permite pagar por empleado o en lote, con método de pago configurable.
    /// </summary>
    public class PagoNominaEmpleado
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int NominaId { get; set; }
        public int EmpleadoId { get; set; }
        public int DetalleNominaId { get; set; }

        public decimal Monto { get; set; }

        /// <summary>
        /// Efectivo | Transferencia | Cheque | Deposito
        /// </summary>
        [Required]
        [StringLength(30)]
        public string MetodoPago { get; set; } = "Transferencia";

        /// <summary>
        /// Referencia del pago (número de cheque, transferencia, etc.)
        /// </summary>
        [StringLength(100)]
        public string? Referencia { get; set; }

        /// <summary>
        /// Cuenta de la que sale el dinero
        /// </summary>
        public int? CuentaId { get; set; }

        /// <summary>
        /// Movimiento financiero generado
        /// </summary>
        public int? MovimientoId { get; set; }

        public DateTime FechaPago { get; set; } = DateTime.Now;
        public int? UsuarioPagoId { get; set; }

        /// <summary>
        /// Pendiente | Pagado | Anulado
        /// </summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente";

        [StringLength(500)]
        public string? Notas { get; set; }

        public int EmpresaId { get; set; } = 1;
    }
}