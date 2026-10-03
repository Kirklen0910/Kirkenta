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

        [StringLength(300)]
        public string Motivo { get; set; } = string.Empty;

        /// <summary>
        /// Estado: Solicitado | AprobadoGerente | AprobadoRRHH | Entregado | Descontado | Rechazado | Cancelado
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Solicitado";

        // Aprobaciones
        public int? AprobadoPorGerenteId { get; set; }
        public DateTime? FechaAprobacionGerente { get; set; }

        public int? AprobadoPorRRHHId { get; set; }
        public DateTime? FechaAprobacionRRHH { get; set; }

        public int? EntregadoPorId { get; set; }

        public string? MotivoRechazo { get; set; }

        // Descuento
        public int Cuotas { get; set; } = 1;
        public decimal MontoCuota { get; set; } = 0;
        public DateTime? FechaPrimerDescuento { get; set; }
        public decimal SaldoPendiente { get; set; } = 0;

        // Cuenta de donde se paga
        public int? CuentaId { get; set; }

        // Movimiento financiero generado
        public int? MovimientoId { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }

    /// <summary>
    /// Detalle de cada descuento aplicado a un vale.
    /// </summary>
    public class ValeDescuento
    {
        public int Id { get; set; }
        public int ValeEmpleadoId { get; set; }
        public int NumeroCuota { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;

        /// <summary>
        /// ID de la nómina en la que se aplicó (cuando exista el módulo RRHH)
        /// </summary>
        public int? NominaId { get; set; }

        [StringLength(200)]
        public string? Notas { get; set; }
    }
}