using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Moneda
    {
        public int Id { get; set; }

        [Required]
        [StringLength(3)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(5)]
        public string? Simbolo { get; set; }

        public decimal TipoCambio { get; set; } = 1;

        public bool EsPredeterminada { get; set; } = false;
        public bool Activa { get; set; } = true;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}