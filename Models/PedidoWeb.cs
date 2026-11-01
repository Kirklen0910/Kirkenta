using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>Pedido que un cliente armó en el catálogo en línea y envió por WhatsApp.</summary>
    public class PedidoWeb
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string Numero { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required, StringLength(120)]
        public string Nombre { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Direccion { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        /// <summary>Líneas del pedido en JSON: [{id, nombre, cantidad, precio}].</summary>
        public string ItemsJson { get; set; } = "[]";

        public decimal Total { get; set; }

        /// <summary>Nuevo | Atendido | Cancelado</summary>
        [Required, StringLength(20)]
        public string Estado { get; set; } = "Nuevo";

        [StringLength(64)]
        public string? Ip { get; set; }

        public int EmpresaId { get; set; } = Kirkenta.Helpers.EmpresaActual.Id;
    }
}
