using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Categoría de ingreso o egreso.
    /// Ejemplos: "Ventas al contado", "Alquiler", "Servicios básicos", "Nómina"
    /// Cada empresa puede crear las suyas; el sistema seedea las más comunes.
    /// </summary>
    public class CategoriaFinanciera
    {
        public int Id { get; set; }

        /// <summary>
        /// Tipo: "Ingreso" | "Egreso"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = "Egreso";

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Cuenta contable formal (ej: 5101)
        /// </summary>
        [StringLength(30)]
        public string? CuentaContable { get; set; }

        /// <summary>
        /// ⬇️ NUEVO: FK al Plan de Cuentas.
        /// Nullable para mantener compatibilidad con categorías existentes.
        /// </summary>
        public int? PlanCuentaId { get; set; }

        [StringLength(20)]
        public string Color { get; set; } = "#6b7280";

        public bool EsSistema { get; set; } = false;
        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int EmpresaId { get; set; } = 1;
    }
}