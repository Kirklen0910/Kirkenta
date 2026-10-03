using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Representa una cuenta de la empresa: caja chica, caja general, caja POS,
    /// o una cuenta bancaria. Cada movimiento afecta el saldo de una cuenta.
    /// </summary>
    public class CuentaFinanciera
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Tipo: "Caja" | "Banco"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = "Caja";

        /// <summary>
        /// Subtipo para cajas: "Chica" | "General" | "POS"
        /// Para bancos: null
        /// </summary>
        [StringLength(30)]
        public string? Subtipo { get; set; }

        /// <summary>
        /// Número de cuenta bancaria (solo para tipo = Banco)
        /// </summary>
        [StringLength(50)]
        public string? NumeroCuenta { get; set; }

        /// <summary>
        /// Nombre del banco (solo para tipo = Banco)
        /// </summary>
        [StringLength(100)]
        public string? Banco { get; set; }

        public int MonedaId { get; set; } = 1;

        public decimal SaldoInicial { get; set; } = 0;
        public decimal SaldoActual { get; set; } = 0;

        public decimal? LimiteCredito { get; set; }

        /// <summary>
        /// Cuenta contable formal (ej: 1101) para contabilidad
        /// </summary>
        [StringLength(30)]
        public string? CuentaContable { get; set; }

        [StringLength(300)]
        public string? Descripcion { get; set; }

        [StringLength(150)]
        public string? Responsable { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}