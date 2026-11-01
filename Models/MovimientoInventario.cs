using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Kardex: cada entrada/salida/ajuste de stock queda registrada aquí.
    /// Cantidad es con signo (+ entra, - sale).
    /// </summary>
    public class MovimientoInventario
    {
        public int Id { get; set; }

        public int ProductoId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        /// <summary>Entrada | Salida | Ajuste</summary>
        [StringLength(20)]
        public string Tipo { get; set; } = "Ajuste";

        /// <summary>Venta, Compra, Baja, ReversionBaja, Devolucion, AnulacionVenta, Cotizacion, AjusteManual, StockInicial, Importacion...</summary>
        [StringLength(40)]
        public string Origen { get; set; } = string.Empty;

        public int? DocumentoId { get; set; }

        [StringLength(50)]
        public string? DocumentoNumero { get; set; }

        /// <summary>Cantidad con signo.</summary>
        public decimal Cantidad { get; set; }

        public decimal StockAnterior { get; set; }
        public decimal StockPosterior { get; set; }

        /// <summary>Costo unitario aplicado al movimiento (si se conoce).</summary>
        public decimal? CostoUnitario { get; set; }

        public int? UsuarioId { get; set; }

        [StringLength(300)]
        public string? Notas { get; set; }
    }
}
