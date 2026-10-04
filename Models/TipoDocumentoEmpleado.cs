using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Categoría de documento que se puede adjuntar a un empleado.
    /// Ejemplos: DNI, RTN, Antecedentes policiales, Contrato, etc.
    /// </summary>
    public class TipoDocumentoEmpleado
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Personales | Antecedentes | Referencias | Academicos | Contrato | Medicos | Otros
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Categoria { get; set; } = "Otros";

        /// <summary>
        /// Si es obligatorio que todos los empleados lo tengan
        /// </summary>
        public bool EsObligatorio { get; set; } = false;

        /// <summary>
        /// Si el documento tiene fecha de vencimiento
        /// </summary>
        public bool RequiereVencimiento { get; set; } = false;

        /// <summary>
        /// Días antes del vencimiento para alertar
        /// </summary>
        public int? DiasAlertaVencimiento { get; set; }

        [StringLength(100)]
        public string? Icono { get; set; }

        public bool EsSistema { get; set; } = false;
        public bool Activo { get; set; } = true;
        public int Orden { get; set; } = 0;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}