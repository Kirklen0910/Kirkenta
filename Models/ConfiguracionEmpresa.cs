using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class ConfiguracionEmpresa
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string? RazonSocial { get; set; }

        [StringLength(20)]
        public string? RTN { get; set; }

        [StringLength(300)]
        public string? Direccion { get; set; }

        [StringLength(100)]
        public string? Ciudad { get; set; }

        [StringLength(80)]
        public string? Pais { get; set; } = "Honduras";

        [StringLength(30)]
        public string? Telefono { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(150)]
        public string? SitioWeb { get; set; }

        [StringLength(300)]
        public string? LogoPath { get; set; }

        [StringLength(50)]
        public string? CAI { get; set; }

        [StringLength(30)]
        public string? RangoInicial { get; set; }

        [StringLength(30)]
        public string? RangoFinal { get; set; }

        public DateTime? FechaLimiteEmision { get; set; }

        public string? NotasFactura { get; set; }

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}