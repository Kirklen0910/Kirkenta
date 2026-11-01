using Kirkenta.Helpers;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>
    /// Verifica que los roles apliquen EXACTAMENTE lo que se marcó:
    /// solo Ver = solo ver, solo Editar = solo editar, etc.
    /// </summary>
    [Collection("BD")]
    public class PermisoHelperTests
    {
        [Fact]
        public void Admin_tiene_todos_los_permisos_sin_filas()
        {
            using var ctx = TestDb.Crear();
            foreach (var accion in new[] { "ver", "crear", "editar", "eliminar" })
                Assert.True(PermisoHelper.TienePermiso(ctx, "Admin", "Ventas", "Clientes", accion));
        }

        [Theory]
        [InlineData("ver", true)]
        [InlineData("crear", false)]
        [InlineData("editar", false)]
        [InlineData("eliminar", false)]
        public void Rol_con_solo_Ver_puede_unicamente_ver(string accion, bool esperado)
        {
            using var ctx = TestDb.Crear();
            TestDb.AgregarRol(ctx, "Consulta", ("Ventas", "Clientes", true, false, false, false));

            Assert.Equal(esperado, PermisoHelper.TienePermiso(ctx, "Consulta", "Ventas", "Clientes", accion));
        }

        [Theory]
        [InlineData("ver", false)]
        [InlineData("crear", false)]
        [InlineData("editar", true)]
        [InlineData("eliminar", false)]
        public void Rol_con_solo_Editar_puede_unicamente_editar(string accion, bool esperado)
        {
            using var ctx = TestDb.Crear();
            TestDb.AgregarRol(ctx, "Editor", ("Ventas", "Clientes", false, false, true, false));

            Assert.Equal(esperado, PermisoHelper.TienePermiso(ctx, "Editor", "Ventas", "Clientes", accion));
        }

        [Theory]
        [InlineData("ver", true)]
        [InlineData("crear", true)]
        [InlineData("editar", true)]
        [InlineData("eliminar", true)]
        public void Rol_con_todo_marcado_puede_todo(string accion, bool esperado)
        {
            using var ctx = TestDb.Crear();
            TestDb.AgregarRol(ctx, "Total", ("Ventas", "Clientes", true, true, true, true));

            Assert.Equal(esperado, PermisoHelper.TienePermiso(ctx, "Total", "Ventas", "Clientes", accion));
        }

        [Fact]
        public void Permiso_de_un_submodulo_no_se_filtra_a_otro()
        {
            using var ctx = TestDb.Crear();
            TestDb.AgregarRol(ctx, "Parcial", ("Ventas", "Clientes", true, true, true, true));

            Assert.False(PermisoHelper.TienePermiso(ctx, "Parcial", "Ventas", "Cotizaciones", "ver"));
            Assert.False(PermisoHelper.TienePermiso(ctx, "Parcial", "Inventario", "Clientes", "ver"));
        }

        [Fact]
        public void Rol_inexistente_o_vacio_no_tiene_permisos()
        {
            using var ctx = TestDb.Crear();

            Assert.False(PermisoHelper.TienePermiso(ctx, "NoExiste", "Ventas", "Clientes", "ver"));
            Assert.False(PermisoHelper.TienePermiso(ctx, "", "Ventas", "Clientes", "ver"));
        }

        [Fact]
        public void Accion_desconocida_se_niega()
        {
            using var ctx = TestDb.Crear();
            TestDb.AgregarRol(ctx, "Total", ("Ventas", "Clientes", true, true, true, true));

            Assert.False(PermisoHelper.TienePermiso(ctx, "Total", "Ventas", "Clientes", "borrar-todo"));
        }

        [Fact]
        public void UsuarioTienePermiso_usa_el_rol_del_usuario()
        {
            using var ctx = TestDb.Crear();
            TestDb.AgregarRol(ctx, "Consulta", ("Ventas", "Clientes", true, false, false, false));
            TestDb.AgregarUsuario(ctx, "ana", "Consulta");

            Assert.True(PermisoHelper.UsuarioTienePermiso(ctx, "ana", "Ventas", "Clientes", "ver"));
            Assert.False(PermisoHelper.UsuarioTienePermiso(ctx, "ana", "Ventas", "Clientes", "eliminar"));
        }

        [Fact]
        public void Usuario_inexistente_o_sin_nombre_no_tiene_permisos()
        {
            using var ctx = TestDb.Crear();

            Assert.False(PermisoHelper.UsuarioTienePermiso(ctx, "fantasma", "Ventas", "Clientes", "ver"));
            Assert.False(PermisoHelper.UsuarioTienePermiso(ctx, null, "Ventas", "Clientes", "ver"));
        }

        [Fact]
        public void Cambiar_un_permiso_se_aplica_de_inmediato_aunque_este_en_cache()
        {
            using var ctx = TestDb.Crear();
            TestDb.AgregarRol(ctx, "Cambiante", ("Ventas", "Clientes", true, false, false, false));

            Assert.False(PermisoHelper.TienePermiso(ctx, "Cambiante", "Ventas", "Clientes", "crear")); // llena el cache

            var fila = ctx.Permisos.Single(p => p.Modulo == "Ventas" && p.Submodulo == "Clientes");
            fila.PuedeCrear = true;
            ctx.SaveChanges(); // invalida el cache

            Assert.True(PermisoHelper.TienePermiso(ctx, "Cambiante", "Ventas", "Clientes", "crear"));
        }

        [Fact]
        public void Menu_muestra_un_modulo_solo_si_el_rol_puede_VER_algo()
        {
            using var ctx = TestDb.Crear();
            TestDb.AgregarRol(ctx, "SoloCrear", ("Ventas", "Clientes", false, true, false, false));
            TestDb.AgregarRol(ctx, "ConVer", ("Compras", "Proveedores", true, false, false, false));

            Assert.DoesNotContain("Ventas", PermisoHelper.ModulosVisibles(ctx, "SoloCrear"));
            Assert.Contains("Compras", PermisoHelper.ModulosVisibles(ctx, "ConVer"));
        }

        [Fact]
        public void Menu_submodulos_solo_con_Ver()
        {
            using var ctx = TestDb.Crear();
            TestDb.AgregarRol(ctx, "Mixto",
                ("Ventas", "Clientes", true, false, false, false),
                ("Ventas", "Cotizaciones", false, true, true, true));

            var visibles = PermisoHelper.SubmodulosVisibles(ctx, "Mixto", "Ventas");

            Assert.Contains("Clientes", visibles);
            Assert.DoesNotContain("Cotizaciones", visibles);
        }

        [Fact]
        public void Menu_paginas_de_accion_aparecen_con_cualquier_permiso()
        {
            using var ctx = TestDb.Crear();
            TestDb.AgregarRol(ctx, "Cajero", ("Ventas", "Create", false, true, false, false));

            Assert.Contains("Create", PermisoHelper.SubmodulosVisibles(ctx, "Cajero", "Ventas"));
        }

        [Fact]
        public void Usuario_inactivo_se_detecta()
        {
            using var ctx = TestDb.Crear();
            TestDb.AgregarUsuario(ctx, "activo", "Admin", activo: true);
            TestDb.AgregarUsuario(ctx, "baja", "Admin", activo: false);

            Assert.True(PermisoHelper.UsuarioActivo(ctx, "activo"));
            Assert.False(PermisoHelper.UsuarioActivo(ctx, "baja"));
            Assert.False(PermisoHelper.UsuarioActivo(ctx, "nadie"));
        }
    }
}
