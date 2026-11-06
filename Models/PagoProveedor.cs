using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class PagoProveedor
    {
        public int Id { get; set; }
        public string? Numero { get; set; }
        public int ProveedorId { get; set; }
        public int? OrdenCompraId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Monto { get; set; }
        public int MetodoPagoId { get; set; }
        public string? Referencia { get; set; }
        public int MonedaId { get; set; } = 1;
        public decimal TipoCambio { get; set; } = 1;
        public string? Notas { get; set; }
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = Kirkenta.Helpers.EmpresaActual.Id;

        /// <summary>
        /// Estado: Activo | Anulado. Un pago anulado se conserva como historial (y su número queda consumido),
        /// pero no cuenta como pagado en ningún saldo ni reporte.
        /// </summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Activo";

        public string? MotivoAnulacion { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public int? UsuarioAnuloId { get; set; }
    }
}
