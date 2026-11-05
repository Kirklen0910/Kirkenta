using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class PlantillasRolTests
    {
        [Fact]
        public void CadaPlantilla_SoloUsaModulosYOpcionesQueExisten()
        {
            foreach (var p in PlantillasRol.Todas)
            {
                foreach (var mod in p.Modulos.Keys) Assert.True(ModulosERP.Modulos.ContainsKey(mod), $"{p.Nombre}: módulo «{mod}» no existe");
                foreach (var clave in p.Opciones.Keys)
                {
                    var partes = clave.Split('/');
                    Assert.True(ModulosERP.Modulos.TryGetValue(partes[0], out var subs) && subs.Contains(partes[1]), $"{p.Nombre}: opción «{clave}» no existe");
                }
                Assert.NotEmpty(PlantillasRol.Niveles(p));
                // ninguna plantilla de puesto toca usuarios, seguridad ni configuración
                Assert.DoesNotContain(PlantillasRol.Niveles(p).Keys, k => k.StartsWith("Usuarios/") || k.StartsWith("Seguridad/") || k.StartsWith("Configuracion/"));
            }
        }

        [Fact]
        public void Cajero_VendeYCierraCaja_PeroNoAnulaNiVeNomina()
        {
            var n = PlantillasRol.Niveles(PlantillasRol.Todas.Single(p => p.Clave == "cajero"));
            Assert.Equal(2, n["Ventas/POS"]);
            Assert.Equal(2, n["Finanzas/CierresCreate"]);
            Assert.False(n.ContainsKey("Finanzas/MovimientosAnular"));
            Assert.False(n.ContainsKey("RRHH/Nomina"));
            Assert.Equal(new[] { true, true, true, false }, PlantillasRol.Banderas(2));
        }

        [Fact]
        public void Editor_TraeElEstadoDelRolYLosOtrosRolesParaCopiar()
        {
            using var ctx = TestDb.Crear();
            var cajero = new Rol { Nombre = "Cajero" };
            var otro = new Rol { Nombre = "Bodega" };
            ctx.Roles.AddRange(cajero, otro);
            ctx.SaveChanges();
            ctx.Permisos.Add(new Permiso { RolId = cajero.Id, Modulo = "Ventas", Submodulo = "POS", PuedeVer = true, PuedeCrear = true });
            ctx.SaveChanges();

            var e = EditorPermisos.Crear(ctx, cajero.Id);
            Assert.Equal(new[] { true, true, false, false }, e.Estado["Ventas/POS"]);
            Assert.True(e.OtrosRoles.ContainsKey("Bodega"));
            Assert.False(e.OtrosRoles.ContainsKey("Cajero"));          // no se copia de sí mismo
        }
    }
}
