using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Proveedor
    {
        public int Id { get; set; }

        [StringLength(20)]
        public string? Codigo { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
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

        [StringLength(100)]
        public string? Contacto { get; set; }

        [StringLength(30)]
        public string? TelefonoContacto { get; set; }

        [StringLength(300)]
        public string? Direccion { get; set; }

        [StringLength(100)]
        public string? Ciudad { get; set; }

        [StringLength(80)]
        public string? Pais { get; set; } = "Honduras";

        public string CondicionPago { get; set; } = "Contado";
        public int DiasCredito { get; set; } = 0;
        public decimal LimiteCredito { get; set; } = 0;

        [StringLength(100)]
        public string? Banco { get; set; }

        [StringLength(50)]
        public string? CuentaBancaria { get; set; }

        public int MonedaId { get; set; } = 1;

        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}