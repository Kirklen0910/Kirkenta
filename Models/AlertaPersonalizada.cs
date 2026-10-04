using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Alerta personalizada creada manualmente por el usuario.
    /// Sirve para agregar recordatorios adicionales (ej: revisar algo, contactar a alguien).
    /// </summary>
    public class AlertaPersonalizada
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Empleado relacionado (opcional)
        /// </summary>
        public int? EmpleadoId { get; set; }

        /// <summary>
        /// Info | Advertencia | Urgente | Exito
        /// </summary>
        [StringLength(20)]
        public string Prioridad { get; set; } = "Info";

        public DateTime FechaAlerta { get; set; } = DateTime.Now;

        /// <summary>
        /// Si la alerta debe mostrarse en el dashboard
        /// </summary>
        public bool MostrarEnDashboard { get; set; } = true;

        /// <summary>
        /// Fecha hasta la que debe mostrarse (opcional)
        /// </summary>
        public DateTime? FechaVigenciaHasta { get; set; }

        public bool Completada { get; set; } = false;
        public DateTime? FechaCompletada { get; set; }

        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}