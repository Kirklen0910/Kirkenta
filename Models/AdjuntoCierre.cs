using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Adjuntos (comprobantes, fotos, boletas) asociados a un cierre de caja.
    /// </summary>
    public class AdjuntoCierre
    {
        public int Id { get; set; }
        public int CierreCajaId { get; set; }

        [Required]
        [StringLength(200)]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string RutaArchivo { get; set; } = string.Empty;

        [StringLength(20)]
        public string? TipoArchivo { get; set; }

        public int TamanoKB { get; set; }

        [StringLength(100)]
        public string? Descripcion { get; set; }

        public DateTime FechaSubida { get; set; } = DateTime.Now;
        public int? UsuarioId { get; set; }
    }
}