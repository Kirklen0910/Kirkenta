using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Reglas para proteger el Plan de Cuentas: el sistema usa ciertos codigos fijos
    /// (caja, bancos, capital, inventario, ingresos/gastos por defecto, depreciacion...)
    /// y los asientos guardan el Id de la cuenta; cambiar su codigo o tipo a la ligera rompe reportes.
    /// </summary>
    public static class PlanCuentaProteccion
    {
        /// <summary>Codigos que el codigo del sistema busca por nombre fijo.</summary>
        public static readonly HashSet<string> CodigosDelSistema = new()
        {
            "1.1.01.001", "1.1.01.004", "1.1.02.001", "1.1.03.001", "1.1.04",
            "2.1.01.001", "2.1.02.001", "2.1.06", "2.1.99",
            "3.1.01", "3.2.02", "3.2.04",
            "4.1", "4.2", "4.2.02",
            "5.1", "5.1.01",
            "6.1", "6.2", "6.2.06", "6.2.08", "6.3", "6.3.02"
        };

        /// <summary>
        /// Motivo por el que NO se puede cambiar el codigo o el tipo de la cuenta; null si se puede.
        /// </summary>
        public static string? MotivoBloqueoCodigo(ApplicationDbContext context, PlanCuenta cuenta)
        {
            if (CodigosDelSistema.Contains(cuenta.Codigo))
                return "es una cuenta que el sistema usa de forma automatica";
            if (context.DetallesAsiento.Any(d => d.PlanCuentaId == cuenta.Id))
                return "ya tiene asientos contables";
            if (context.PlanCuentas.Any(c => c.CodigoPadre == cuenta.Codigo))
                return "tiene subcuentas";
            if (context.CuentasFinancieras.Any(c => c.CuentaContable == cuenta.Codigo))
                return "esta ligada a una cuenta de caja/banco";
            if (context.CategoriasFinancieras.Any(c => c.PlanCuentaId == cuenta.Id || c.CuentaContable == cuenta.Codigo))
                return "esta ligada a categorias financieras";
            return null;
        }

        /// <summary>Motivo por el que NO se puede eliminar; null si se puede.</summary>
        public static string? MotivoBloqueoEliminar(ApplicationDbContext context, PlanCuenta cuenta)
        {
            if (CodigosDelSistema.Contains(cuenta.Codigo))
                return "es una cuenta que el sistema usa de forma automatica. Desactivala si ya no la quieres ver";
            if (context.DetallesAsiento.Any(d => d.PlanCuentaId == cuenta.Id))
                return "tiene asientos contables. Desactivala en su lugar";
            if (context.CuentasFinancieras.Any(c => c.CuentaContable == cuenta.Codigo))
                return "esta ligada a una cuenta de caja/banco. Desactivala en su lugar";
            if (context.CategoriasFinancieras.Any(c => c.CuentaContable == cuenta.Codigo))
                return "esta ligada a categorias financieras. Desactivala en su lugar";
            return null;
        }
    }
}
