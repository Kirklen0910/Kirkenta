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
        public decimal PorcentajeIHSS { get; set; } = 5.0m;   // 2.5% EM + 2.5% IVM

        /// <summary>Techo de cotización IHSS (L. 11,903.13 en 2026). También es el piso del RAP.</summary>
        public decimal TopeIHSS { get; set; } = 11903.13m;

        // ===== RAP =====
        public bool AplicaRAP { get; set; } = true;

        [Range(0, 100)]
        public decimal PorcentajeRAP { get; set; } = 1.5m;

        public decimal? TopeRAP { get; set; }

        // ===== ISR =====
        public bool AplicaISR { get; set; } = true;

        /// <summary>
        /// Exento anual de ISR publicado por el SAR (L. 228,324.32 en 2026). Define el primer tramo de la tabla.
        /// </summary>
        public decimal TopeAnualExentoISR { get; set; } = 228324.32m;

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