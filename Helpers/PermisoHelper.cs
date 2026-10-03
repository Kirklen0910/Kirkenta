using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    public static class PermisoHelper
    {
        /// <summary>
        /// Verifica si un rol tiene un permiso específico sobre un módulo/submódulo.
        /// </summary>
        /// <param name="accion">"ver" | "crear" | "editar" | "eliminar"</param>
        public static bool TienePermiso(
            ApplicationDbContext context,
            string rolNombre,
            string modulo,
            string? submodulo,
            string accion)
        {
            // Admin siempre tiene todos los permisos
            if (rolNombre == "Admin")
                return true;

            var rol = context.Roles.FirstOrDefault(r => r.Nombre == rolNombre);
            if (rol == null)
                return false;

            var permiso = context.Permisos
                .FirstOrDefault(p => p.RolId == rol.Id
                    && p.Modulo == modulo
                    && p.Submodulo == submodulo);

            if (permiso == null)
                return false;

            return accion.ToLower() switch
            {
                "ver" => permiso.PuedeVer,
                "crear" => permiso.PuedeCrear,
                "editar" => permiso.PuedeEditar,
                "eliminar" => permiso.PuedeEliminar,
                _ => false
            };
        }

        /// <summary>
        /// Devuelve la lista de módulos donde el rol tiene acceso de "ver".
        /// </summary>
        public static List<string> ModulosVisibles(ApplicationDbContext context, string rolNombre)
        {
            // Admin ve todos los módulos definidos en ModulosERP
            if (rolNombre == "Admin")
            {
                return ModulosERP.Modulos.Keys.ToList();
            }

            var rol = context.Roles.FirstOrDefault(r => r.Nombre == rolNombre);
            if (rol == null)
                return new List<string>();

            // Un módulo es visible si:
            // - Tiene un permiso a nivel módulo (submodulo == null) con PuedeVer=true
            // - O si tiene al menos un permiso de submódulo con PuedeVer=true
            var modulosPorPadre = context.Permisos
                .Where(p => p.RolId == rol.Id && p.Submodulo == null && p.PuedeVer)
                .Select(p => p.Modulo)
                .ToList();

            var modulosPorHijo = context.Permisos
                .Where(p => p.RolId == rol.Id && p.Submodulo != null && p.PuedeVer)
                .Select(p => p.Modulo)
                .ToList();

            return modulosPorPadre
                .Union(modulosPorHijo)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Devuelve la lista de submódulos visibles para un módulo específico.
        /// Lista vacía = sin restricciones (Admin).
        /// </summary>
        public static List<string> SubmodulosVisibles(
            ApplicationDbContext context,
            string rolNombre,
            string modulo)
        {
            if (rolNombre == "Admin")
            {
                return new List<string>(); // Lista vacía = mostrar todo
            }

            var rol = context.Roles.FirstOrDefault(r => r.Nombre == rolNombre);
            if (rol == null)
                return new List<string>();

            return context.Permisos
                .Where(p => p.RolId == rol.Id
                    && p.Modulo == modulo
                    && p.Submodulo != null
                    && p.PuedeVer)
                .Select(p => p.Submodulo!)
                .ToList();
        }
    }
}