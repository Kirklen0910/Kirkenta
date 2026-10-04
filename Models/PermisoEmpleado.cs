using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Permiso o licencia de un empleado (con o sin goce de sueldo).
    /// </summary>
    public class PermisoEmpleado
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int EmpleadoId { get; set; }

        /// <summary>
        /// Personal | Enfermedad | Duelo | Maternidad | Paternidad | 
        /// Estudio | CitaMedica | Otro
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } = "Personal";

        /// <summary>
        /// Solicitado | Aprobado | Rechazado | Tomado | Cancelado
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Solicitado";

        // ===== PERÍODO =====
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal DiasSolicitados { get; set; }

        // ===== GOCE DE SUELDO =====
        public bool ConGoceSueldo { get; set; } = true;

        // ===== SOLICITUD =====
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        public int? UsuarioSolicitaId { get; set; }

        [Required]
        [StringLength(500)]
        public string Motivo { get; set; } = string.Empty;

        // ===== APROBACIÓN =====
        public int? UsuarioApruebaId { get; set; }
        public DateTime? FechaAprobacion { get; set; }

        [StringLength(500)]
        public string? MotivoRechazo { get; set; }

        /// <summary>
        /// Ruta de documento de soporte (certificado médico, etc.)
        /// </summary>
        [StringLength(300)]
        public string? RutaDocumentoSoporte { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public int EmpresaId { get; set; } = 1;
    }
}