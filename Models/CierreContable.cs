using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Cierre contable mensual. Al cerrar un mes, se bloquean todos los movimientos
    /// financieros, aperturas, cierres de caja y ventas con fecha dentro de ese mes.
    /// Solo se puede reabrir (con motivo) por un usuario con permiso.
    /// Anio + Mes es único: una sola fila por mes.
    /// </summary>
    public class CierreContable
    {
        public int Id { get; set; }

        [Required]
        public int Anio { get; set; }

        [Required]
        [Range(1, 12)]
        public int Mes { get; set; }

        /// <summary>
        /// Estado: "Abierto" | "Cerrado"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Abierto";

        // ===== CIERRE =====
        public DateTime? FechaCierre { get; set; }
        public int? UsuarioCierreId { get; set; }

        // ===== REAPERTURA =====
        public DateTime? FechaReapertura { get; set; }
        public int? UsuarioReaperturaId { get; set; }

        [StringLength(500)]
        public string? MotivoReapertura { get; set; }

        // ===== TOTALES AL MOMENTO DEL CIERRE (auditoría) =====
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal Balance { get; set; }
        public int CantidadMovimientos { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int EmpresaId { get; set; } = 1;

        // ===== HELPERS =====
        public string MesNombre => System.Globalization.CultureInfo
            .GetCultureInfo("es-HN")
            .DateTimeFormat
            .GetMonthName(Mes);

        public string PeriodoTexto => $"{MesNombre} {Anio}";
    }
}