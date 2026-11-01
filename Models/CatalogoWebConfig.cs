using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>Ajustes del catálogo en línea público (una sola fila).</summary>
    public class CatalogoWebConfig
    {
        public int Id { get; set; }

        public bool Habilitado { get; set; } = false;

        [StringLength(120)]
        public string? Titulo { get; set; }

        [StringLength(300)]
        public string? Mensaje { get; set; }

        /// <summary>WhatsApp que recibe los pedidos. Si queda vacío se usa el teléfono de la empresa.</summary>
        [StringLength(30)]
        public string? WhatsApp { get; set; }

        public bool MostrarStock { get; set; } = false;

        public decimal PedidoMinimo { get; set; } = 0;

        public int EmpresaId { get; set; } = Kirkenta.Helpers.EmpresaActual.Id;
    }
}
