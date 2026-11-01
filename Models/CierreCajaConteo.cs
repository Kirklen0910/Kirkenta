using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Conteo por método de pago de un cierre de caja: lo que esperaba el sistema vs lo que el cajero contó
    /// (efectivo, vouchers de tarjeta, comprobantes de transferencia, etc.).
    /// </summary>
    public class CierreCajaConteo
    {
        public int Id { get; set; }

        public int CierreCajaId { get; set; }

        [Required, StringLength(60)]
        public string Metodo { get; set; } = string.Empty;

        public decimal EsperadoSistema { get; set; }
        public decimal Contado { get; set; }
        public decimal Diferencia { get; set; }

        public int EmpresaId { get; set; } = Kirkenta.Helpers.EmpresaActual.Id;
    }
}
