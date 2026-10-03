using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Descripcion { get; set; }

        [StringLength(20)]
        public string Color { get; set; } = "#6b7280";

        public bool Activa { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}