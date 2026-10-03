using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Un movimiento financiero: ingreso, egreso o transferencia entre cuentas.
    /// Cada movimiento afecta el saldo de la cuenta origen (y destino si es transferencia).
    /// Los movimientos no se borran, se anulan (para tener historial).
    /// </summary>
    public class MovimientoFinanciero
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        /// <summary>
        /// Tipo: "Ingreso" | "Egreso" | "Transferencia"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = "Egreso";

        public DateTime Fecha { get; set; } = DateTime.Now;

        /// <summary>
        /// Cuenta que recibe (ingreso) o entrega (egreso) el dinero.
        /// Para transferencias, es la cuenta origen.
        /// </summary>
        public int CuentaId { get; set; }

        /// <summary>
        /// Para transferencias: cuenta destino
        /// </summary>
        public int? CuentaDestinoId { get; set; }

        /// <summary>
        /// Categoría (solo para Ingreso/Egreso, no para Transferencia)
        /// </summary>
        public int? CategoriaId { get; set; }

        public decimal Monto { get; set; }

        public int MonedaId { get; set; } = 1;
        public decimal TipoCambio { get; set; } = 1;

        [StringLength(300)]
        public string Concepto { get; set; } = string.Empty;

        /// <summary>
        /// Referencia externa: nº de factura, cheque, transferencia
        /// </summary>
        [StringLength(100)]
        public string? Referencia { get; set; }

        /// <summary>
        /// Forma de pago/cobro: Efectivo, Transferencia, Cheque, Tarjeta
        /// </summary>
        [StringLength(30)]
        public string? FormaPago { get; set; }

        /// <summary>
        /// Origen del movimiento: Manual, Venta, Compra, Pago Proveedor, Nómina, Vale
        /// </summary>
        [StringLength(30)]
        public string Origen { get; set; } = "Manual";

        /// <summary>
        /// ID del documento origen (ej: IdVenta, IdPagoProveedor)
        /// </summary>
        public int? OrigenId { get; set; }

        /// <summary>
        /// Si el movimiento fue creado automáticamente por el sistema
        /// </summary>
        public bool EsAutomatico { get; set; } = false;

        [StringLength(500)]
        public string? Notas { get; set; }

        /// <summary>
        /// Estado: Activo | Anulado
        /// </summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Activo";

        public string? MotivoAnulacion { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public int? UsuarioAnuloId { get; set; }

        // Cierre contable
        public bool Conciliado { get; set; } = false;
        public DateTime? FechaConciliacion { get; set; }
        public int? ConciliadoPorId { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}