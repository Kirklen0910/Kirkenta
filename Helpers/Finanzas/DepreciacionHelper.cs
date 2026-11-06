using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>Calculo de la depreciacion mensual en linea recta de un activo fijo.</summary>
    public static class DepreciacionHelper
    {
        /// <summary>
        /// Monto a depreciar este mes: la cuota mensual, sin pasarse de lo que falta
        /// para llegar al valor residual. Nunca negativo.
        /// </summary>
        public static decimal CalcularMonto(ActivoFijo a)
        {
            var restante = a.CostoAdquisicion - a.ValorResidual - a.DepreciacionAcumulada;
            if (restante <= 0) return 0;
            return Math.Round(Math.Min(a.DepreciacionMensual, restante), 2, MidpointRounding.AwayFromZero);
        }
    }
}
