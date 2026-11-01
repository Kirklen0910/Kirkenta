using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    public class PasswordPolicyTests
    {
        [Theory]
        [InlineData("abc123", false)]          // muy corta
        [InlineData("abcdefgh", false)]        // sin número
        [InlineData("12345678", false)]        // sin letra
        [InlineData("admin123", false)]        // común
        [InlineData("Segura2026", true)]
        [InlineData("mi clave 99", true)]
        public void Valida_largo_letra_numero_y_comunes(string clave, bool esValida)
        {
            Assert.Equal(esValida, PasswordPolicy.Validar(clave, "carlos") == null);
        }

        [Fact]
        public void No_acepta_la_clave_igual_al_usuario()
        {
            Assert.NotNull(PasswordPolicy.Validar("Carlos2026", "carlos2026"));
        }

        [Fact]
        public void Clave_vacia_se_rechaza()
        {
            Assert.NotNull(PasswordPolicy.Validar("", "x"));
            Assert.NotNull(PasswordPolicy.Validar(null, "x"));
        }
    }

    [Collection("BD")]
    public class PlanCuentaProteccionTests
    {
        [Fact]
        public void Cuenta_del_sistema_no_se_puede_recodificar_ni_borrar()
        {
            using var ctx = TestDb.Crear();
            var caja = new PlanCuenta { Codigo = "1.1.01.001", Nombre = "Caja", Tipo = "Activo" };
            ctx.PlanCuentas.Add(caja);
            ctx.SaveChanges();

            Assert.NotNull(PlanCuentaProteccion.MotivoBloqueoCodigo(ctx, caja));
            Assert.NotNull(PlanCuentaProteccion.MotivoBloqueoEliminar(ctx, caja));
        }

        [Fact]
        public void Cuenta_libre_se_puede_editar_y_borrar()
        {
            using var ctx = TestDb.Crear();
            var c = new PlanCuenta { Codigo = "6.2.99", Nombre = "Otra", Tipo = "Gasto" };
            ctx.PlanCuentas.Add(c);
            ctx.SaveChanges();

            Assert.Null(PlanCuentaProteccion.MotivoBloqueoCodigo(ctx, c));
            Assert.Null(PlanCuentaProteccion.MotivoBloqueoEliminar(ctx, c));
        }

        [Fact]
        public void Cuenta_con_asientos_queda_bloqueada()
        {
            using var ctx = TestDb.Crear();
            var c = new PlanCuenta { Codigo = "6.2.98", Nombre = "Con asientos", Tipo = "Gasto" };
            ctx.PlanCuentas.Add(c);
            ctx.SaveChanges();
            var asiento = new AsientoContable { Numero = "A-1", Fecha = DateTime.Today, Concepto = "x", Estado = "Activo" };
            ctx.AsientosContables.Add(asiento);
            ctx.SaveChanges();
            ctx.DetallesAsiento.Add(new DetalleAsiento { AsientoContableId = asiento.Id, PlanCuentaId = c.Id, Debe = 10m, Haber = 0m });
            ctx.SaveChanges();

            Assert.NotNull(PlanCuentaProteccion.MotivoBloqueoCodigo(ctx, c));
            Assert.NotNull(PlanCuentaProteccion.MotivoBloqueoEliminar(ctx, c));
        }

        [Fact]
        public void Cuenta_padre_con_hijos_no_cambia_de_codigo()
        {
            using var ctx = TestDb.Crear();
            var padre = new PlanCuenta { Codigo = "6.2.90", Nombre = "Padre", Tipo = "Gasto" };
            var hijo = new PlanCuenta { Codigo = "6.2.90.001", Nombre = "Hijo", Tipo = "Gasto", CodigoPadre = "6.2.90" };
            ctx.PlanCuentas.AddRange(padre, hijo);
            ctx.SaveChanges();

            Assert.NotNull(PlanCuentaProteccion.MotivoBloqueoCodigo(ctx, padre));
        }
    }
}
