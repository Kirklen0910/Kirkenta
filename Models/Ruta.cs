using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Ruta de despacho. Agrupa varios Envíos (paradas) para que un repartidor
    /// los entregue en un solo viaje.
    /// </summary>
    public class Ruta
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.Today;

        // ===== REPARTIDOR / VEHÍCULO =====
        public int? RepartidorId { get; set; }

        [StringLength(150)]
        public string RepartidorNombre { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Vehiculo { get; set; }

        [StringLength(20)]
        public string? Placa { get; set; }

        // ===== ESTADO GENERAL =====
        /// <summary>
        /// Borrador | Despachada | EnReparto | Completada | Cancelada
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Borrador";

        // ===== ZONA / DESCRIPCIÓN =====
        public int? ZonaId { get; set; }

        [StringLength(100)]
        public string? ZonaNombre { get; set; }

        [StringLength(300)]
        public string? Descripcion { get; set; }

        // ===== FECHAS =====
        public DateTime? FechaSalida { get; set; }
        public DateTime? FechaRegreso { get; set; }

        // ===== TOTALES (denormalizados para reportes) =====
        public int TotalParadas { get; set; }
        public int ParadasEntregadas { get; set; }
        public int ParadasFallidas { get; set; }
        public decimal MontoTotalEnvios { get; set; }

        // ===== TRACKING PÚBLICO =====
        /// <summary>
        /// Código único para que el cliente pueda consultar el estado
        /// de su envío sin autenticarse. Ej: KRT-2026-AB12X9
        /// </summary>
        [StringLength(30)]
        public string? TrackingCode { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}