using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class PermisosReportesTests
    {
        [Fact]
        public void Reportes_tiene_permiso_propio_por_reporte()
        {
            var subs = ModulosERP.Modulos["Reportes"];
            foreach (var s in new[] { "Inventario", "Rentabilidad", "CuentasPorCobrar", "CuentasPorPagar", "ISV", "PanelDueno" })
                Assert.Contains(s, subs);
        }

        [Fact]
        public void Quien_ya_veia_Reportes_conserva_acceso_a_cada_reporte_y_quien_no_veia_sigue_sin_acceso()
        {
            using var ctx = TestDb.Crear();
            var conAcceso = new Rol { Nombre = "Contador" };
            var sinAcceso = new Rol { Nombre = "Bodega" };
            ctx.Roles.AddRange(conAcceso, sinAcceso);
            ctx.SaveChanges();

            PermisoSeeder.MigrarPermisosFaltantes(ctx);   // crea las filas (todas sin acceso)
            var idx = ctx.Permisos.Single(p => p.RolId == conAcceso.Id && p.Modulo == "Reportes" && p.Submodulo == "Index");
            idx.PuedeVer = true;
            ctx.SaveChanges();

            PermisoSeeder.AplicarPermisosPredeterminados(ctx);
            PermisoHelper.InvalidarCache();

            foreach (var s in new[] { "Inventario", "Rentabilidad", "CuentasPorCobrar", "CuentasPorPagar", "ISV", "PanelDueno" })
            {
                Assert.True(PermisoHelper.TienePermiso(ctx, "Contador", "Reportes", s, "ver"), s);
                Assert.False(PermisoHelper.TienePermiso(ctx, "Bodega", "Reportes", s, "ver"), s);
            }
        }
    }
}
