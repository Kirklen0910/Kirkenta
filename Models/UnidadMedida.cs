using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class UnidadMedida
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La abreviatura es obligatoria")]
        [StringLength(10)]
        public string Abreviatura { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}