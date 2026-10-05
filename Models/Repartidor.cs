using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Repartidor / motorista que realiza los envíos.
    /// Se asigna a una Ruta al momento de despachar.
    /// </summary>
    public class Repartidor
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        public string? Vehiculo { get; set; }         // Ej: "Moto", "Camión Isuzu"

        [StringLength(20)]
        public string? Placa { get; set; }

        [StringLength(50)]
        public string? Licencia { get; set; }

        [StringLength(300)]
        public string? Notas { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}