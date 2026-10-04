using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Nómina (o planilla) de un período. Agrupa el pago de varios empleados.
    /// Se aprueba antes de pagarse y genera un egreso en Finanzas.
    /// </summary>
    public class Nomina
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        /// <summary>
        /// Semanal | Catorcenal | Quincenal | Mensual | Extraordinaria
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = "Mensual";

        [Required]
        [StringLength(200)]
        public string PeriodoDescripcion { get; set; } = string.Empty;

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public DateTime FechaPago { get; set; }

        /// <summary>
        /// Borrador | Calculada | Aprobada | Pagada | Anulada
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Borrador";

        // ===== TOTALES =====
        public int CantidadEmpleados { get; set; } = 0;

        public decimal TotalSalariosBrutos { get; set; } = 0;
        public decimal TotalBonos { get; set; } = 0;
        public decimal TotalHorasExtra { get; set; } = 0;

        public decimal TotalDeduccionIHSS { get; set; } = 0;
        public decimal TotalDeduccionRAP { get; set; } = 0;
        public decimal TotalDeduccionISR { get; set; } = 0;
        public decimal TotalDeduccionVales { get; set; } = 0;
        public decimal TotalOtrasDeducciones { get; set; } = 0;

        public decimal TotalNeto { get; set; } = 0;

        // ===== APROBACIÓN =====
        public int? UsuarioCalculoId { get; set; }
        public DateTime? FechaCalculo { get; set; }

        public int? UsuarioApruebaId { get; set; }
        public DateTime? FechaAprobacion { get; set; }

        // ===== PAGO =====
        public int? UsuarioPagaId { get; set; }
        public DateTime? FechaPagoReal { get; set; }

        public int? CuentaId { get; set; }
        public int? MovimientoId { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int EmpresaId { get; set; } = 1;
    }

    /// <summary>
    /// Detalle de la nómina por empleado.
    /// </summary>
    public class DetalleNomina
    {
        public int Id { get; set; }

        public int NominaId { get; set; }
        public int EmpleadoId { get; set; }

        // ===== DEVENGADO =====
        public decimal SalarioBase { get; set; } = 0;
        public decimal BonoTransporte { get; set; } = 0;
        public decimal BonoAlimentacion { get; set; } = 0;
        public decimal OtrosBonos { get; set; } = 0;
        public decimal HorasExtra { get; set; } = 0;
        public decimal MontoHorasExtra { get; set; } = 0;
        public decimal Comisiones { get; set; } = 0;

        public decimal TotalBruto { get; set; } = 0;

        // ===== DEDUCCIONES =====
        public decimal DeduccionIHSS { get; set; } = 0;
        public decimal DeduccionRAP { get; set; } = 0;
        public decimal DeduccionISR { get; set; } = 0;
        public decimal DeduccionVales { get; set; } = 0;
        public decimal OtrasDeducciones { get; set; } = 0;

        public decimal TotalDeducciones { get; set; } = 0;

        // ===== NETO =====
        public decimal SalarioNeto { get; set; } = 0;

        // ===== DÍAS Y AUSENCIAS =====
        public decimal DiasTrabajados { get; set; } = 30;
        public decimal DiasAusencia { get; set; } = 0;
        public decimal DiasVacaciones { get; set; } = 0;
        public decimal DiasPermiso { get; set; } = 0;

        [StringLength(500)]
        public string? Notas { get; set; }
    }
}