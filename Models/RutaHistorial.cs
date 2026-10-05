using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Historial de eventos de una Ruta o de sus Envíos.
    /// Sirve para trazabilidad y para mostrar la línea de tiempo.
    /// </summary>
    public class RutaHistorial
    {
        public int Id { get; set; }

        public int RutaId { get; set; }

        /// <summary>
        /// Null si el evento es a nivel Ruta.
        /// Se llena si el evento es sobre una parada específica.
        /// </summary>
        public int? EnvioId { get; set; }

        [Required]
        [StringLength(100)]
        public string Evento { get; set; } = string.Empty;   // "RutaCreada" | "Despachada" | "Entregado" | "Fallido"

        [StringLength(500)]
        public string? Detalle { get; set; }

        [StringLength(30)]
        public string? EstadoAnterior { get; set; }

        [StringLength(30)]
        public string? EstadoNuevo { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public int? UsuarioId { get; set; }

        [StringLength(150)]
        public string? UsuarioNombre { get; set; }
    }
}