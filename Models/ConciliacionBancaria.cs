using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Conciliación bancaria: compara los movimientos del sistema en una cuenta bancaria
    /// contra el estado de cuenta real del banco, para un período dado.
    /// </summary>
    public class ConciliacionBancaria
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int CuentaId { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        /// <summary>
        /// Saldo que reporta el banco al final del período.
        /// </summary>
        public decimal SaldoBanco { get; set; }

        /// <summary>
        /// Saldo que el sistema calcula al final del período (saldo contable).
        /// </summary>
        public decimal SaldoSistema { get; set; }

        /// <summary>
        /// Diferencia = SaldoBanco - SaldoSistema. Debería ser 0 si todo cuadra.
        /// </summary>
        public decimal Diferencia { get; set; }

        /// <summary>
        /// Abierta | EnRevision | Conciliada | Cancelada
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Abierta";

        // ===== CIERRE =====
        public int? UsuarioCierraId { get; set; }
        public DateTime? FechaCierre { get; set; }

        [StringLength(500)]
        public string? NotasCierre { get; set; }

        // ===== TOTALES (para listados rápidos) =====
        public int TotalLineasSistema { get; set; }
        public int TotalLineasBanco { get; set; }
        public int TotalMatcheadas { get; set; }
        public int TotalNoMatcheadas { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }

    /// <summary>
    /// Detalle de la conciliación: cada línea es un movimiento del sistema
    /// o una línea del estado de cuenta bancario.
    /// </summary>
    public class ConciliacionDetalle
    {
        public int Id { get; set; }

        public int ConciliacionBancariaId { get; set; }

        /// <summary>
        /// Origen: "Sistema" | "Banco"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Origen { get; set; } = "Sistema";

        /// <summary>
        /// Si origen = Sistema: ID del MovimientoFinanciero.
        /// </summary>
        public int? MovimientoId { get; set; }

        /// <summary>
        /// Fecha de la línea (del movimiento o del estado de cuenta).
        /// </summary>
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Descripción/concepto de la línea.
        /// </summary>
        [StringLength(300)]
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>
        /// Referencia externa (nº de cheque, transferencia, etc.).
        /// </summary>
        [StringLength(100)]
        public string? Referencia { get; set; }

        /// <summary>
        /// Monto de la línea. Positivo = crédito/ingreso, negativo = débito/egreso.
        /// </summary>
        public decimal Monto { get; set; }

        /// <summary>
        /// Si la línea ya fue matcheada con su contraparte.
        /// </summary>
        public bool Matcheada { get; set; } = false;

        /// <summary>
        /// Si matcheada, ID de la línea contraparte (del banco si esta es del sistema,
        /// o del sistema si esta es del banco).
        /// </summary>
        public int? MatcheadaConDetalleId { get; set; }

        /// <summary>
        /// Si el match fue automático o manual.
        /// </summary>
        [StringLength(20)]
        public string? TipoMatch { get; set; } // "Automatico" | "Manual"

        /// <summary>
        /// Notas de revisión.
        /// </summary>
        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}