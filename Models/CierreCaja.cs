using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Registro de cierre de caja (arqueo) realizado por un usuario.
    /// Guarda el efectivo esperado según sistema vs el efectivo contado físicamente.
    /// </summary>
    public class CierreCaja
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int CuentaId { get; set; }

        /// <summary>
        /// Apertura de caja asociada. De aquí se toma el saldo inicial.
        /// </summary>
        public int? AperturaId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Today;
        public DateTime FechaCierre { get; set; } = DateTime.Now;

        public int UsuarioCierraId { get; set; }

        // ===== CÁLCULO =====
        public decimal SaldoInicialSistema { get; set; }
        public decimal TotalIngresosEfectivo { get; set; }
        public decimal TotalEgresosEfectivo { get; set; }
        public decimal EfectivoEsperado { get; set; }

        public decimal EfectivoContado { get; set; }
        public decimal Diferencia { get; set; }

        [StringLength(20)]
        public string Resultado { get; set; } = "Cuadrado";

        public decimal Tolerancia { get; set; } = 20.00m;

        [StringLength(500)]
        public string? Notas { get; set; }

        // ===== DISTRIBUCIÓN =====
        /// <summary>
        /// Suma de los montos distribuidos. Debe igualar EfectivoContado.
        /// </summary>
        public decimal TotalDistribuido { get; set; }

        // ===== APROBACIÓN =====
        /// <summary>
        /// Estado del acta: "Borrador" | "Cerrado" | "Aprobado" | "Rechazado"
        /// </summary>
        [StringLength(20)]
        public string EstadoActa { get; set; } = "Cerrado";

        public int? UsuarioApruebaId { get; set; }
        public DateTime? FechaAprobacion { get; set; }

        [StringLength(500)]
        public string? MotivoRechazo { get; set; }

        // ===== MOVIMIENTO DE AJUSTE =====
        public int? MovimientoAjusteId { get; set; }

        public int EmpresaId { get; set; } = 1;
    }
}