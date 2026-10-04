using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Configuración de deducciones por año fiscal.
    /// Permite tener valores distintos por año (SAR actualiza cada año).
    /// </summary>
    public class ConfiguracionDeduccion
    {
        public int Id { get; set; }

        [Required]
        public int Anio { get; set; }

        // ===== IHSS =====
        public bool AplicaIHSS { get; set; } = true;

        [Range(0, 100)]
        public decimal PorcentajeIHSS { get; set; } = 2.5m;

        public decimal TopeIHSS { get; set; } = 11995.00m;

        // ===== RAP =====
        public bool AplicaRAP { get; set; } = true;

        [Range(0, 100)]
        public decimal PorcentajeRAP { get; set; } = 1.5m;

        public decimal? TopeRAP { get; set; }

        // ===== ISR =====
        public bool AplicaISR { get; set; } = true;

        /// <summary>
        /// Tope anual exento de ISR (L. 250,000 en Honduras 2024-2026)
        /// </summary>
        public decimal TopeAnualExentoISR { get; set; } = 250000.00m;

        /// <summary>
        /// Método: "Acumulativo" (SAR) o "MensualSimple"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string MetodoISR { get; set; } = "Acumulativo";

        // ===== INFORMACIÓN =====
        [StringLength(500)]
        public string? Notas { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
    }
}