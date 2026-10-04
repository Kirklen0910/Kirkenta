using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Documento adjunto de un empleado (foto, DNI, contrato, etc.).
    /// </summary>
    public class AdjuntoEmpleado
    {
        public int Id { get; set; }

        public int EmpleadoId { get; set; }
        public int TipoDocumentoId { get; set; }

        [Required]
        [StringLength(200)]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string RutaArchivo { get; set; } = string.Empty;

        [StringLength(20)]
        public string? TipoArchivo { get; set; } // pdf, jpg, png

        public int TamanoKB { get; set; }

        [StringLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Fecha del documento (cuándo fue emitido)
        /// </summary>
        public DateTime? FechaDocumento { get; set; }

        /// <summary>
        /// Fecha de vencimiento del documento (si aplica)
        /// </summary>
        public DateTime? FechaVencimiento { get; set; }

        /// <summary>
        /// Si el documento está vigente (calculado)
        /// </summary>
        public bool Vigente { get; set; } = true;

        public DateTime FechaSubida { get; set; } = DateTime.Now;
        public int? UsuarioSubioId { get; set; }
    }
}