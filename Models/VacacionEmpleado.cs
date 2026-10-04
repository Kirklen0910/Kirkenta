using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Solicitud y registro de vacaciones de un empleado.
    /// Flujo: Solicitado → Aprobado → Tomado (o Rechazado/Cancelado).
    /// </summary>
    public class VacacionEmpleado
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int EmpleadoId { get; set; }

        /// <summary>
        /// Solicitado | Aprobado | Rechazado | Tomado | Cancelado
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Solicitado";

        // ===== PERÍODO =====
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        /// <summary>
        /// Días hábiles a tomar (sin contar feriados)
        /// </summary>
        public decimal DiasSolicitados { get; set; }

        /// <summary>
        /// Feriados que caen en el período
        /// </summary>
        public int DiasFeriados { get; set; } = 0;

        /// <summary>
        /// Días que se descuentan (solicitados - feriados)
        /// </summary>
        public decimal DiasADescontar { get; set; }

        // ===== SOLICITUD =====
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        public int? UsuarioSolicitaId { get; set; }

        [StringLength(500)]
        public string? Motivo { get; set; }

        // ===== APROBACIÓN =====
        public int? UsuarioApruebaId { get; set; }
        public DateTime? FechaAprobacion { get; set; }

        [StringLength(500)]
        public string? MotivoRechazo { get; set; }

        // ===== TOMA EFECTIVA =====
        public DateTime? FechaTomaReal { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public int EmpresaId { get; set; } = 1;
    }
}