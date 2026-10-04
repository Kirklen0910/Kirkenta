using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Representa una cuenta en el Plan de Cuentas contable.
    /// La jerarquía se construye a partir del código (ej: 1, 1.1, 1.1.01).
    /// </summary>
    public class PlanCuenta
    {
        public int Id { get; set; }

        /// <summary>
        /// Código contable jerárquico (ej: 1.1.01). Debe ser único.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de la cuenta (ej: Caja General, Ventas).
        /// </summary>
        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Tipo contable principal (Activo, Pasivo, Patrimonio, Ingreso, Costo, Gasto).
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Tipo { get; set; } = "Activo";

        /// <summary>
        /// Código de la cuenta padre. Null para cuentas de primer nivel.
        /// </summary>
        [StringLength(20)]
        public string? CodigoPadre { get; set; }

        /// <summary>
        /// Nivel jerárquico (1 = mayor, 2 = subcuenta, 3 = auxiliar).
        /// </summary>
        public int Nivel { get; set; } = 1;

        /// <summary>
        /// Naturaleza: "Deudora" (Activo, Costo, Gasto) o "Acreedora" (Pasivo, Patrimonio, Ingreso).
        /// </summary>
        [StringLength(20)]
        public string Naturaleza { get; set; } = "Deudora";

        /// <summary>
        /// Si es true, permite recibir movimientos directamente.
        /// </summary>
        public bool EsMovimiento { get; set; } = false;

        public bool Activa { get; set; } = true;

        [StringLength(300)]
        public string? Descripcion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int EmpresaId { get; set; } = 1;
    }
}