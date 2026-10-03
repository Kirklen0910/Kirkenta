using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Actualiza saldos de cuentas y vales cuando se crean movimientos.
    /// </summary>
    public static class SaldoHelper
    {
        /// <summary>
        /// Aplica el efecto de un movimiento sobre el saldo de las cuentas.
        /// Se llama al crear un movimiento.
        /// </summary>
        public static void Aplicar(ApplicationDbContext context, MovimientoFinanciero mov)
        {
            if (mov.Estado != "Activo") return;

            switch (mov.Tipo)
            {
                case "Ingreso":
                    var cuentaIngreso = context.CuentasFinancieras.FirstOrDefault(c => c.Id == mov.CuentaId);
                    if (cuentaIngreso != null)
                    {
                        cuentaIngreso.SaldoActual += mov.Monto;
                    }
                    break;

                case "Egreso":
                    var cuentaEgreso = context.CuentasFinancieras.FirstOrDefault(c => c.Id == mov.CuentaId);
                    if (cuentaEgreso != null)
                    {
                        cuentaEgreso.SaldoActual -= mov.Monto;
                    }
                    break;

                case "Transferencia":
                    var origen = context.CuentasFinancieras.FirstOrDefault(c => c.Id == mov.CuentaId);
                    var destino = context.CuentasFinancieras.FirstOrDefault(c => c.Id == mov.CuentaDestinoId);
                    if (origen != null) origen.SaldoActual -= mov.Monto;
                    if (destino != null) destino.SaldoActual += mov.Monto;
                    break;
            }
        }

        /// <summary>
        /// Revierte el efecto de un movimiento (al anularlo).
        /// </summary>
        public static void Revertir(ApplicationDbContext context, MovimientoFinanciero mov)
        {
            if (mov.Estado == "Activo") return; // Ya fue revertido

            switch (mov.Tipo)
            {
                case "Ingreso":
                    var cuentaIngreso = context.CuentasFinancieras.FirstOrDefault(c => c.Id == mov.CuentaId);
                    if (cuentaIngreso != null)
                    {
                        cuentaIngreso.SaldoActual -= mov.Monto;
                    }
                    break;

                case "Egreso":
                    var cuentaEgreso = context.CuentasFinancieras.FirstOrDefault(c => c.Id == mov.CuentaId);
                    if (cuentaEgreso != null)
                    {
                        cuentaEgreso.SaldoActual += mov.Monto;
                    }
                    break;

                case "Transferencia":
                    var origen = context.CuentasFinancieras.FirstOrDefault(c => c.Id == mov.CuentaId);
                    var destino = context.CuentasFinancieras.FirstOrDefault(c => c.Id == mov.CuentaDestinoId);
                    if (origen != null) origen.SaldoActual += mov.Monto;
                    if (destino != null) destino.SaldoActual -= mov.Monto;
                    break;
            }
        }
    }
}