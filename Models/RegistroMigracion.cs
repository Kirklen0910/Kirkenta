using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Qué creó cada lote de migración (empleados, cuentas, cotizaciones, movimientos…), para poder
    /// mostrarlo en el historial y deshacerlo. Las ventas y compras migradas usan su propio campo LoteMigracion.
    /// </summary>
    public class RegistroMigracion
    {
        public int Id { get; set; }

        [Required, StringLength(30)]
        public string Lote { get; set; } = string.Empty;

        /// <summary>Empleado, CuentaFinanciera, MetodoPago, PlanCuenta, Cotizacion, MovimientoFinanciero.</summary>
        [Required, StringLength(40)]
        public string Entidad { get; set; } = string.Empty;

        public int EntidadId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
