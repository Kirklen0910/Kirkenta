using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Tramos del ISR por año fiscal.
    /// Ejemplo Honduras 2024 (mensual):
    ///   Hasta L. 15,692.25 → 0%
    ///   L. 15,692.26 a L. 23,246.20 → 15% sobre excedente
    ///   L. 23,246.21 a L. 55,170.68 → L. 1,133.09 + 20% sobre excedente
    ///   Más de L. 55,170.68 → L. 7,517.98 + 25% sobre excedente
    /// </summary>
    public class TramoISR
    {
        public int Id { get; set; }

        public int ConfiguracionDeduccionId { get; set; }

        /// <summary>
        /// Orden del tramo (1, 2, 3, ...)
        /// </summary>
        public int Orden { get; set; }

        /// <summary>
        /// Desde cuánto aplica (en L.)
        /// </summary>
        public decimal Desde { get; set; }

        /// <summary>
        /// Hasta cuánto aplica (en L.). Null = sin límite
        /// </summary>
        public decimal? Hasta { get; set; }

        /// <summary>
        /// Porcentaje sobre el excedente
        /// </summary>
        [Range(0, 100)]
        public decimal Porcentaje { get; set; }

        /// <summary>
        /// Monto fijo que se suma (si el tramo lo requiere)
        /// </summary>
        public decimal MontoFijo { get; set; } = 0;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}