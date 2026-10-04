using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Registro de expediente de un empleado: llamados de atención,
    /// amonestaciones, suspensiones, notas de mérito, etc.
    /// </summary>
    public class ExpedienteEmpleado
    {
        public int Id { get; set; }

        public int EmpleadoId { get; set; }

        /// <summary>
        /// LlamadoAtencion | AmonestacionVerbal | AmonestacionEscrita | 
        /// Suspension | NotaMerito | Reconocimiento | Otro
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } = "LlamadoAtencion";

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>
        /// Leve | Media | Grave | MuyGrave
        /// </summary>
        [StringLength(20)]
        public string? Gravedad { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public DateTime? FechaInicioSuspension { get; set; }
        public DateTime? FechaFinSuspension { get; set; }
        public int? DiasSuspension { get; set; }

        /// <summary>
        /// Quién levantó el expediente
        /// </summary>
        public int? UsuarioRegistroId { get; set; }

        [StringLength(150)]
        public string? NombreRegistro { get; set; }

        /// <summary>
        /// Testigos o personas presentes
        /// </summary>
        [StringLength(300)]
        public string? Testigos { get; set; }

        /// <summary>
        /// Si el empleado firmó de enterado
        /// </summary>
        public bool EmpleadoFirmo { get; set; } = false;

        public DateTime? FechaFirma { get; set; }

        /// <summary>
        /// Ruta de documento firmado (si se subió)
        /// </summary>
        [StringLength(300)]
        public string? RutaDocumentoFirmado { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}