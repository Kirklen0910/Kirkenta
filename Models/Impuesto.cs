using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Impuesto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        public decimal Porcentaje { get; set; }

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public bool EsPredeterminado { get; set; }
        public bool Activo { get; set; } = true;
    }
}