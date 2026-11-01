using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Ayuda para reportes de FLUJO DE CAJA (efectivo real): excluye los movimientos
    /// "solo contables" (costo de ventas 5.1.01 y su reversa), que no mueven dinero.
    /// </summary>
    public static class FlujoCajaHelper
    {
        /// <summary>Ids de las categorías enlazadas al plan de cuentas 5.1.01 (costo de ventas).</summary>
        public static HashSet<int> CategoriasSoloContables(ApplicationDbContext context)
        {
            var idsPlan = context.PlanCuentas
                .Where(p => p.Codigo == "5.1.01")
                .Select(p => (int?)p.Id)
                .ToList();
            return context.CategoriasFinancieras
                .Where(c => c.PlanCuentaId != null && idsPlan.Contains(c.PlanCuentaId))
                .Select(c => c.Id)
                .ToHashSet();
        }

        public static bool EsSoloContable(string? notas, string tipo, int? categoriaId, HashSet<int> categoriasContables)
        {
            if (notas != null && notas.Contains(SaldoHelper.MarcaSoloContable)) return true;
            return tipo == "Egreso" && categoriaId.HasValue && categoriasContables.Contains(categoriaId.Value);
        }

        public static bool EsSoloContable(MovimientoFinanciero m, HashSet<int> categoriasContables)
            => EsSoloContable(m.Notas, m.Tipo, m.CategoriaId, categoriasContables);
    }
}
