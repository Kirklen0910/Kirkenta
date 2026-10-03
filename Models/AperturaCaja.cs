using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Apertura de caja: se registra el saldo inicial con el que abre
    /// el cajero antes de empezar las ventas del día.
    /// Solo puede haber una apertura activa por cuenta.
    /// </summary>
    public class AperturaCaja
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int CuentaId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Today;
        public DateTime FechaApertura { get; set; } = DateTime.Now;

        public int UsuarioAbreId { get; set; }

        public decimal SaldoInicial { get; set; } = 0;

        [StringLength(500)]
        public string? Notas { get; set; }

        /// <summary>
        /// Activa mientras la caja está abierta. Se marca false al cerrar.
        /// </summary>
        public bool Activa { get; set; } = true;

        /// <summary>
        /// Se asigna cuando se cierra la caja.
        /// </summary>
        public int? CierreId { get; set; }

        public int EmpresaId { get; set; } = 1;
    }
}