namespace Kirkenta.Models
{
    public class Venta
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int? ClienteId { get; set; }
        public int? CotizacionId { get; set; }
        public int? PedidoId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Subtotal { get; set; } = 0;
        public decimal Descuento { get; set; } = 0;
        public decimal Impuestos { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public string Estado { get; set; } = "Completada";
        public string? Notas { get; set; }
        public int? UsuarioCreoId { get; set; }

        // ===== LOGÍSTICA =====
        public bool RequiereEnvio { get; set; } = false;
        public decimal MontoEnvio { get; set; } = 0;

        /// <summary>La venta se hizo a un cliente exonerado de ISV (todas las líneas a 0%).</summary>
        public bool EsExonerada { get; set; } = false;

        // ===== CRÉDITO =====
        /// <summary>
        /// Indica si la venta fue al crédito.
        /// </summary>
        public bool EsCredito { get; set; } = false;

        /// <summary>
        /// Saldo pendiente de cobro. Al inicio es igual al Total.
        /// Se va reduciendo conforme se registran cobros.
        /// </summary>
        public decimal SaldoPendiente { get; set; } = 0;

        /// <summary>
        /// Fecha de vencimiento del crédito (calculada al crear la venta).
        /// </summary>
        public DateTime? FechaVencimientoCredito { get; set; }

        // ===== FINANZAS =====
        /// <summary>
        /// Indica si ya se registró el movimiento financiero de la venta.
        /// Para ventas al contado: se registra al momento de la venta.
        /// Para ventas al crédito: NO se registra al momento (solo al cobrar).
        /// </summary>
        public bool MovimientoRegistrado { get; set; } = false;

        /// <summary>
        /// Fecha en que se registró el movimiento financiero.
        /// </summary>
        public DateTime? FechaMovimiento { get; set; }

        /// <summary>
        /// ID del movimiento de ingreso generado.
        /// </summary>
        public int? MovimientoIngresoId { get; set; }

        /// <summary>
        /// ID del movimiento de cambio (egreso) generado, si aplica.
        /// </summary>
        public int? MovimientoCambioId { get; set; }

        /// <summary>Parte del total pagada con el saldo a favor del cliente.</summary>
        public decimal SaldoFavorAplicado { get; set; } = 0;

        /// <summary>La mercadería se entrega por partes: se llevan comprobantes de entrega en bodega hasta llegar a 0 pendiente.</summary>
        public bool ControlEntrega { get; set; } = false;

        // ===== COSTO DE VENTAS (COGS) =====
        /// <summary>
        /// Suma del costo de todos los productos vendidos.
        /// Se calcula con el PrecioCompra del producto al momento de la venta
        /// (snapshot guardado en cada DetalleVenta).
        /// </summary>
        public decimal CostoTotalVendido { get; set; } = 0;

        /// <summary>
        /// Indica si ya se registró el movimiento contable de costo
        /// (egreso con categoría "Costo de mercadería vendida" → plan 5.1.01)
        /// para esta venta.
        /// </summary>
        public bool CostoRegistrado { get; set; } = false;

        /// <summary>
        /// ID del movimiento de egreso generado por el costo de ventas.
        /// </summary>
        public int? MovimientoCostoId { get; set; }

        /// <summary>
        /// Si la venta vino de la herramienta de Migración (historial o saldo inicial del sistema anterior),
        /// guarda el lote. Estas ventas no mueven stock ni caja y no se pueden anular/devolver desde el sistema.
        /// </summary>
        [System.ComponentModel.DataAnnotations.StringLength(40)]
        public string? LoteMigracion { get; set; }
    }

    public class DetalleVenta
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public int ProductoId { get; set; }
        public string? Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; } = 0;
        public decimal ImpuestoPorcentaje { get; set; } = 0;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }

        // ===== COSTO (COGS) =====
        /// <summary>
        /// Precio de compra del producto al momento de la venta (snapshot).
        /// Necesario para que el asiento contable del costo sea histórico y no
        /// dependa del PrecioCompra actual del producto (que puede cambiar
        /// después por una nueva compra).
        /// </summary>
        public decimal CostoUnitarioSnapshot { get; set; } = 0;

        /// <summary>
        /// Costo total de la línea: CostoUnitarioSnapshot × Cantidad.
        /// Sumando todas las líneas se obtiene Venta.CostoTotalVendido.
        /// </summary>
        public decimal CostoTotalLinea { get; set; } = 0;
    }
}