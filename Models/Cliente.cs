using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [StringLength(20)]
        public string? Codigo { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string? RazonSocial { get; set; }

        [StringLength(20)]
        public string? RTN { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Telefono { get; set; }

        [StringLength(300)]
        public string? Direccion { get; set; }

        [StringLength(100)]
        public string? Ciudad { get; set; }

        [StringLength(80)]
        public string? Pais { get; set; } = "Honduras";

        [StringLength(20)]
        public string TipoCliente { get; set; } = "Regular";

        public decimal LimiteCredito { get; set; } = 0;
        public int DiasCredito { get; set; } = 0;

        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
    }
}