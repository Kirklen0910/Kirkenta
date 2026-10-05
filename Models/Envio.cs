using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Representa un envío individual (una parada / un destino).
    /// Puede venir de una Venta, Cotización, Pedido o Factura.
    /// Se agrupa dentro de una Ruta para despacho.
    /// </summary>
    public class Envio
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        // ===== ORIGEN (a qué documento pertenece) =====
        public int? VentaId { get; set; }
        public int? PedidoId { get; set; }
        public int? FacturaId { get; set; }
        public int? CotizacionId { get; set; }

        // ===== CLIENTE (denormalizado para reportes rápidos) =====
        public int ClienteId { get; set; }

        [StringLength(150)]
        public string ClienteNombre { get; set; } = string.Empty;

        // ===== INFO SNAPSHOT DE LA VENTA =====
        // (los datos se copian al momento de la venta y NO cambian después)
        [Required]
        [StringLength(300)]
        public string DireccionEntrega { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Referencia { get; set; }          // punto de referencia

        [StringLength(150)]
        public string ContactoNombre { get; set; } = string.Empty;

        [StringLength(30)]
        public string ContactoTelefono { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Ciudad { get; set; }

        // ===== ZONA =====
        public int? ZonaId { get; set; }

        [StringLength(100)]
        public string? ZonaNombre { get; set; }

        // ===== MONTO =====
        public decimal Monto { get; set; } = 0;
        public bool EsGratis { get; set; } = false;

        // ===== ESTADO INDIVIDUAL DE ESTA PARADA =====
        /// <summary>
        /// Pendiente | EnRuta | Entregado | Fallido | Reagendado | Cancelado
        /// </summary>
        [StringLength(30)]
        public string Estado { get; set; } = "Pendiente";

        // ===== RUTA (asignada por encargado de almacén) =====
        public int? RutaId { get; set; }
        public int? OrdenParada { get; set; }

        // ===== REPARTIDOR (se copia de la Ruta al despachar) =====
        public int? RepartidorId { get; set; }

        [StringLength(150)]
        public string? RepartidorNombre { get; set; }

        [StringLength(100)]
        public string? Vehiculo { get; set; }

        // ===== FECHAS =====
        public DateTime? FechaEntregaEstimada { get; set; }
        public DateTime? FechaSalida { get; set; }
        public DateTime? FechaEntregaReal { get; set; }

        // ===== ENTREGA / EVIDENCIA =====
        [StringLength(150)]
        public string? NombreRecibio { get; set; }        // quién firmó

        [StringLength(300)]
        public string? FirmaImagen { get; set; }          // ruta relativa de imagen

        [StringLength(300)]
        public string? FotoEntrega { get; set; }

        [StringLength(500)]
        public string? MotivoFallo { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}