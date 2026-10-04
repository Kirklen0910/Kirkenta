using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Vale o adelanto de salario a un empleado.
    /// Flujo: Solicitado → AprobadoGerente → AprobadoRRHH → Entregado → Descontado
    /// RRHH descuenta del salario en la nómina (1 o varias cuotas).
    /// </summary>
    public class ValeEmpleado
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int EmpleadoId { get; set; }

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        public DateTime? FechaEntrega { get; set; }

        public decimal Monto { get; set; }

        [Required]
        [StringLength(300)]
        public string Motivo { get; set; } = string.Empty;

        /// <summary>
        /// Solicitado | AprobadoGerente | AprobadoRRHH | Entregado | Descontado | Rechazado | Cancelado
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Solicitado";

        // ===== APROBACIONES =====
        public int? AprobadoPorGerenteId { get; set; }
        public DateTime? FechaAprobacionGerente { get; set; }

        public int? AprobadoPorRRHHId { get; set; }
        public DateTime? FechaAprobacionRRHH { get; set; }

        public int? EntregadoPorId { get; set; }

        [StringLength(500)]
        public string? MotivoRechazo { get; set; }

        // ===== DESCUENTO =====
        public int Cuotas { get; set; } = 1;
        public decimal MontoCuota { get; set; } = 0;
        public DateTime? FechaPrimerDescuento { get; set; }
        public decimal SaldoPendiente { get; set; } = 0;

        // ===== CUENTA DE PAGO =====
        public int? CuentaId { get; set; }

        // ===== MOVIMIENTO FINANCIERO =====
        public int? MovimientoId { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }

    /// <summary>
    /// Detalle de cada descuento aplicado a un vale (una cuota por nómina).
    /// </summary>
    public class ValeDescuento
    {
        public int Id { get; set; }

        public int ValeEmpleadoId { get; set; }

        public int NumeroCuota { get; set; }

        public decimal Monto { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        /// <summary>
        /// ID de la nómina en la que se aplicó
        /// </summary>
        public int? NominaId { get; set; }

        [StringLength(200)]
        public string? Notas { get; set; }
    }
}