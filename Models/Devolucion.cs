using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Devolución de cliente (funciona como nota de crédito interna).
    /// Estado: Aplicada | Anulada
    /// </summary>
    public class Devolucion
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int? FacturaId { get; set; }
        public int? VentaId { get; set; }
        public int ClienteId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string? Motivo { get; set; }
        public decimal Total { get; set; } = 0;
        public string Estado { get; set; } = "Aplicada";
        public int? UsuarioCreoId { get; set; }

        // ===== Detalle contable / operativo =====
        /// <summary>Si la mercadería regresó al inventario (false = merma/dañado).</summary>
        public bool ReingresaInventario { get; set; } = true;

        /// <summary>Parte del total que redujo el saldo pendiente de una venta a crédito.</summary>
        public decimal MontoAplicadoSaldo { get; set; } = 0;

        /// <summary>Parte del total devuelta al cliente en efectivo/banco.</summary>
        public decimal MontoReembolsado { get; set; } = 0;

        /// <summary>Parte del total que quedó como saldo a favor del cliente (para compras futuras).</summary>
        public decimal MontoSaldoFavor { get; set; } = 0;

        /// <summary>Costo de la mercadería que regresó a inventario (reversa del COGS).</summary>
        public decimal CostoRevertido { get; set; } = 0;

        public int? MovimientoReembolsoId { get; set; }
        public int? MovimientoCostoId { get; set; }

        [StringLength(50)]
        public string? FormaReembolso { get; set; }

        // ===== Snapshot fiscal de la nota de crédito (solo si la facturación fiscal está activa) =====
        [StringLength(50)]
        public string? CAI { get; set; }

        [StringLength(30)]
        public string? RangoInicial { get; set; }

        [StringLength(30)]
        public string? RangoFinal { get; set; }

        public DateTime? FechaLimiteEmision { get; set; }
    }

    public class DetalleDevolucion
    {
        public int Id { get; set; }
        public int DevolucionId { get; set; }
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }

        /// <summary>Línea de la venta original que se devuelve (para no devolver de más).</summary>
        public int? DetalleVentaId { get; set; }

        /// <summary>Costo unitario con el que se vendió (snapshot).</summary>
        public decimal CostoUnitario { get; set; } = 0;
    }
}
