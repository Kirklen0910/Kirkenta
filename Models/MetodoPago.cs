using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class MetodoPago
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(30)]
        public string Tipo { get; set; } = "Efectivo";

        public bool RequiereReferencia { get; set; }
        public bool Activo { get; set; } = true;
    }
}