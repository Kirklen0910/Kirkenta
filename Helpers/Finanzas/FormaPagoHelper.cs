using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Regla única para saber si un movimiento es dinero físico. El arqueo de caja depende de ella:
    /// si cada pantalla decide por su cuenta, el efectivo esperado deja de cuadrar.
    /// </summary>
    public static class FormaPagoHelper
    {
        public const string Efectivo = "Efectivo";

        /// <summary>Texto para el movimiento: si el método es de tipo efectivo se rotula «Efectivo», se llame como se llame.</summary>
        public static string? Rotulo(MetodoPago? metodo)
        {
            if (metodo == null) return null;
            var esEfectivo = (metodo.Tipo ?? "").Contains("efectivo", StringComparison.OrdinalIgnoreCase);
            return esEfectivo && !(metodo.Nombre ?? "").Contains("efectivo", StringComparison.OrdinalIgnoreCase)
                ? Efectivo : metodo.Nombre;
        }

        /// <summary>
        /// ¿Este movimiento de una caja mueve billetes? Una transferencia entre cuentas siempre (el dinero sale o entra
        /// de la caja física, sea cual sea el rótulo). Sin forma de pago, también: en una caja lo normal es efectivo.
        /// No lo son tarjeta, transferencia de un cliente, saldo a favor ni los movimientos solo contables.
        /// </summary>
        public static bool EsEfectivo(MovimientoFinanciero m)
        {
            if (m.Notas != null && m.Notas.Contains(SaldoHelper.MarcaSoloContable)) return false;
            if (m.Tipo == "Transferencia") return true;
            return string.IsNullOrWhiteSpace(m.FormaPago)
                   || m.FormaPago.Contains("efectivo", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Fecha de un movimiento que el usuario escribe como día: si es hoy lleva la hora actual. Guardarlo a las 00:00
        /// lo dejaba antes de la apertura de la caja y fuera del arqueo del turno.
        /// </summary>
        public static DateTime FechaReal(DateTime fecha) =>
            fecha.TimeOfDay == TimeSpan.Zero && fecha.Date == DateTime.Today ? DateTime.Now : fecha;
    }
}
