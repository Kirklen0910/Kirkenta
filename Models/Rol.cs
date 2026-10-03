using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Rol
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Descripcion { get; set; }

        [StringLength(20)]
        public string Color { get; set; } = "#6b7280";

        public bool EsSistema { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}