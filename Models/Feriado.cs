using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Feriado nacional o regional. Se usa para calcular días de vacaciones
    /// y para el calendario de RRHH.
    /// </summary>
    public class Feriado
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Fecha exacta del feriado (para el año específico)
        /// </summary>
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Código ISO del país: HN, GT, SV, CR, NI, PA, MX, US
        /// </summary>
        [Required]
        [StringLength(3)]
        public string PaisCodigo { get; set; } = "HN";

        /// <summary>
        /// Nacional | Religioso | Civico | Regional
        /// </summary>
        [StringLength(30)]
        public string Tipo { get; set; } = "Nacional";

        /// <summary>
        /// Si es un feriado recurrente (se repite cada año en misma fecha)
        /// </summary>
        public bool EsRecurrente { get; set; } = true;

        /// <summary>
        /// Si es móvil (ej: Semana Santa, depende del año)
        /// </summary>
        public bool EsMovil { get; set; } = false;

        /// <summary>
        /// Año al que aplica (null si es recurrente)
        /// </summary>
        public int? Anio { get; set; }

        [StringLength(300)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}