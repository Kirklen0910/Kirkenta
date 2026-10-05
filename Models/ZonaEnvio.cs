using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Zona de envío (Centro, Cercano, Lejano, etc.)
    /// Define un precio sugerido que el cajero puede usar o sobreescribir manualmente.
    /// </summary>
    public class ZonaEnvio
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Precio sugerido. El cajero puede sobreescribirlo al momento de la venta.
        /// </summary>
        public decimal PrecioSugerido { get; set; } = 0;

        [StringLength(20)]
        public string Color { get; set; } = "#6b7280";

        public bool Activa { get; set; } = true;

        public int Orden { get; set; } = 0;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int EmpresaId { get; set; } = 1;
    }
}