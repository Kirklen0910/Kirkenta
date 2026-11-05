using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>«Entregas en bodega» vive en Inventario con permiso propio, y los roles de antes no pierden su acceso.</summary>
    [Collection("BD")]
    public class EntregasBodegaPermisoTests
    {
        [Fact]
        public void EstaEnInventario_ConNombreClaro()
        {
            Assert.Contains("EntregasBodega", ModulosERP.Modulos["Inventario"]);
            Assert.Equal("Entregas en bodega", ModulosERP.NombreBonitoSub("EntregasBodega"));
        }

        [Fact]
        public void RolesExistentes_ConservanElAccesoQueTenian_YLuegoNoSeTocan()
        {
            using var db = TestDb.Crear();
            var cajero = new Rol { Nombre = "Cajero" };
            var bodega = new Rol { Nombre = "Bodega" };
            var rrhh = new Rol { Nombre = "RRHH" };
            db.Roles.AddRange(cajero, bodega, rrhh);
            db.SaveChanges();
            db.Permisos.AddRange(
                new Permiso { RolId = cajero.Id, Modulo = "Ventas", Submodulo = "Index", PuedeVer = true },
                new Permiso { RolId = cajero.Id, Modulo = "Ventas", Submodulo = "Create", PuedeCrear = true },
                new Permiso { RolId = bodega.Id, Modulo = "Inventario", Submodulo = "Salidas", PuedeVer = true, PuedeCrear = true, PuedeEliminar = true });
            db.SaveChanges();

            PermisoSeeder.MigrarPermisosFaltantes(db);
            PermisoSeeder.MigrarEntregasBodega(db);

            Permiso P(Rol r) => db.Permisos.Single(p => p.RolId == r.Id && p.Modulo == "Inventario" && p.Submodulo == "EntregasBodega");
            Assert.True(P(cajero).PuedeVer); Assert.True(P(cajero).PuedeCrear); Assert.False(P(cajero).PuedeEliminar);
            Assert.True(P(bodega).PuedeVer); Assert.True(P(bodega).PuedeCrear); Assert.True(P(bodega).PuedeEliminar);
            Assert.False(P(rrhh).PuedeVer);

            // el administrador se lo quita al cajero: la migración ya no lo vuelve a dar
            var pc = P(cajero); pc.PuedeVer = pc.PuedeCrear = false; db.SaveChanges();
            PermisoSeeder.MigrarEntregasBodega(db);
            Assert.False(P(cajero).PuedeVer);
        }
    }
}
