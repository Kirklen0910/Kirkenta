using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Actualiza saldos de cuentas y vales cuando se crean movimientos.
    /// </summary>
    public static class SaldoHelper
    {
        /// <summary>
        /// Marca que se guarda en Notas de los movimientos de costo de ventas
        /// (COGS) para indicar que NO afectan el saldo real de caja/banco.
        /// </summary>
        public const string MarcaSoloContable = "[SOLO-CONTABLE]";

        /// <summary>
        /// Un movimiento es "solo contable" cuando su categoría está enlazada a la
        /// cuenta 5.1.01 (Costo de Mercadería Vendida). El costo de ventas NO saca
        /// dinero de la caja: solo reduce el inventario / la utilidad.
        /// </summary>
        public static bool EsSoloContable(ApplicationDbContext context, MovimientoFinanciero mov)
        {
            // Marcado explícitamente como contable (reversa de costo, etc.)
            if (mov.Notas != null && mov.Notas.Contains(MarcaSoloContable)) return true;

            if (mov.Tipo != "Egreso" || mov.CategoriaId == null) return false;

            return context.CategoriasFinancieras
                .Where(c => c.Id == mov.CategoriaId)
                .Join(context.PlanCuentas,
                      c => c.PlanCuentaId,
                      p => (int?)p.Id,
                      (c, p) => p.Codigo)
                .Any(codigo => codigo == "5.1.01");
        }

        /// <summary>
        /// Aplica el efecto de un movimiento sobre el saldo de las cuentas.
        /// Se llama al crear un movimiento.
        ///
        /// EXCEPCIONES (no modifican SaldoActual):
        ///  - Origen = "Apertura": la cuenta ya nació con su saldo inicial.
        ///  - Costo de ventas (5.1.01): es un movimiento contable, no sale dinero.
        /// </summary>
        public static void Aplicar(ApplicationDbContext context, MovimientoFinanciero mov)
        {
            if (mov.Estado != "Activo") return;

            if (mov.Origen == "Apertura") return;
            if (EsSoloContable(context, mov)) return;

            switch (mov.Tipo)
            {
                case "Ingreso":
                    Ajustar(context, mov.CuentaId, mov.Monto);
                    break;

                case "Egreso":
                    Ajustar(context, mov.CuentaId, -mov.Monto);
                    break;

                case "Transferencia":
                    Ajustar(context, mov.CuentaId, -mov.Monto);
                    Ajustar(context, mov.CuentaDestinoId, mov.Monto);
                    break;
            }
        }

        /// <summary>
        /// Revierte el efecto de un movimiento (al anularlo).
        /// </summary>
        public static void Revertir(ApplicationDbContext context, MovimientoFinanciero mov)
        {
            if (mov.Estado == "Activo") return;

            if (mov.Origen == "Apertura") return;
            if (EsSoloContable(context, mov)) return;

            switch (mov.Tipo)
            {
                case "Ingreso":
                    Ajustar(context, mov.CuentaId, -mov.Monto);
                    break;

                case "Egreso":
                    Ajustar(context, mov.CuentaId, mov.Monto);
                    break;

                case "Transferencia":
                    Ajustar(context, mov.CuentaId, mov.Monto);
                    Ajustar(context, mov.CuentaDestinoId, -mov.Monto);
                    break;
            }
        }

        /// <summary>
        /// Suma (o resta, si es negativo) un monto al saldo de una cuenta.
        /// En MySQL se hace con un UPDATE atómico (SaldoActual = SaldoActual + monto): dos ventas o cobros
        /// simultáneos en la misma caja ya no se pisan el saldo ni fallan por conflicto de concurrencia.
        /// Funciona dentro de la transacción en curso.
        /// </summary>
        private static void Ajustar(ApplicationDbContext context, int? cuentaId, decimal delta)
        {
            if (cuentaId == null || delta == 0) return;
            var id = cuentaId.Value;

            if (context.Database.IsRelational())
            {
                var filas = context.CuentasFinancieras
                    .Where(c => c.Id == id)
                    .ExecuteUpdate(u => u.SetProperty(c => c.SaldoActual, c => c.SaldoActual + delta));

                if (filas > 0)
                {
                    // Si la cuenta ya estaba cargada en memoria, se refresca para no leer un saldo viejo
                    var entrada = context.ChangeTracker.Entries<CuentaFinanciera>().FirstOrDefault(e => e.Entity.Id == id);
                    entrada?.Reload();
                }
                return;
            }

            var cuenta = context.CuentasFinancieras.FirstOrDefault(c => c.Id == id);
            if (cuenta != null) cuenta.SaldoActual += delta;
        }
    }
}