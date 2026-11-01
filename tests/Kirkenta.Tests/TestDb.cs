using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>
    /// Las pruebas que usan base de datos comparten el cache estático de PermisoHelper,
    /// así que se ejecutan una a la vez (colección "BD").
    /// </summary>
    [CollectionDefinition("BD", DisableParallelization = true)]
    public class ColeccionBd { }

    public static class TestDb
    {
        /// <summary>Base de datos en memoria nueva y vacía (y cache de permisos limpio).</summary>
        public static ApplicationDbContext Crear()
        {
            PermisoHelper.InvalidarCache();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ApplicationDbContext(options);
        }

        public static Usuario AgregarUsuario(ApplicationDbContext ctx, string username, string rol, bool activo = true)
        {
            var u = new Usuario
            {
                Username = username,
                PasswordHash = "x",
                Email = username + "@test.com",
                Rol = rol,
                Activo = activo
            };
            ctx.Usuarios.Add(u);
            ctx.SaveChanges();
            return u;
        }

        /// <summary>Crea un rol con filas de permiso: (módulo, submódulo, ver, crear, editar, eliminar).</summary>
        public static Rol AgregarRol(
            ApplicationDbContext ctx,
            string nombre,
            params (string Modulo, string Sub, bool Ver, bool Crear, bool Editar, bool Eliminar)[] permisos)
        {
            var rol = new Rol { Nombre = nombre };
            ctx.Roles.Add(rol);
            ctx.SaveChanges();

            foreach (var p in permisos)
            {
                ctx.Permisos.Add(new Permiso
                {
                    RolId = rol.Id,
                    Modulo = p.Modulo,
                    Submodulo = p.Sub,
                    PuedeVer = p.Ver,
                    PuedeCrear = p.Crear,
                    PuedeEditar = p.Editar,
                    PuedeEliminar = p.Eliminar
                });
            }
            ctx.SaveChanges();
            return rol;
        }
    }
}
