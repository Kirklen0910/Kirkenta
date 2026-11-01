using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string? SKU { get; set; }

        [StringLength(50)]
        public string? CodigoBarras { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public int? CategoriaId { get; set; }
        public int? ImpuestoId { get; set; }

        public decimal PrecioCompra { get; set; } = 0;
        public decimal PrecioVenta { get; set; } = 0;
        public decimal PrecioMayorista { get; set; } = 0;
        public decimal Stock { get; set; } = 0;
        public decimal StockMinimo { get; set; } = 0;

        [StringLength(20)]
        public string UnidadMedida { get; set; } = "Unidad";

        [StringLength(300)]
        public string? Imagen { get; set; }

        public bool Activo { get; set; } = true;

        /// <summary>Si es false, el producto nunca recibe descuentos en la venta (ni por línea ni el descuento general).</summary>
        public bool PermiteDescuento { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // ===== CONCURRENCIA OPTIMISTA =====
        // EF Core 9 con Pomelo MySQL tiene un bug al mapear byte[] con
        // IsRowVersion(). Usamos DateTime (que mapea a TIMESTAMP(6)) como
        // token de concurrencia. MySQL lo actualiza en cada UPDATE con
        // ON UPDATE CURRENT_TIMESTAMP(6).
        public DateTime RowVersion { get; set; }
    }
}