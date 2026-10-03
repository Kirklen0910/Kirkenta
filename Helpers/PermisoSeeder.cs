using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Seeder de migración de permisos.
    /// Cuando se agregan nuevos submódulos a ModulosERP.Modulos, los roles
    /// existentes no tienen permisos para ellos. Este helper los crea copiando
    /// los permisos del módulo padre.
    /// Es idempotente: se puede ejecutar múltiples veces sin duplicar.
    /// </summary>
    public static class PermisoSeeder
    {
        /// <summary>
        /// Migra los permisos de todos los roles existentes hacia los submódulos
        /// definidos en ModulosERP que aún no tienen permiso en la BD.
        /// </summary>
        /// <param name="soloSiHayNuevos">
        /// Si es true, solo actúa cuando detecta submódulos faltantes.
        /// Si es false, recorre todo siempre.
        /// </param>
        public static void MigrarPermisosFaltantes(ApplicationDbContext context, bool soloSiHayNuevos = true)
        {
            try
            {
                var roles = context.Roles.ToList();
                if (roles.Count == 0) return;

                int permisosCreados = 0;
                int rolesProcesados = 0;

                foreach (var rol in roles)
                {
                    // El Admin no necesita permisos en BD (bypass en PermisoHelper)
                    if (rol.Nombre == "Admin") continue;

                    var permisosExistentes = context.Permisos
                        .Where(p => p.RolId == rol.Id)
                        .ToList();

                    // Convertimos a un HashSet para búsquedas O(1)
                    var clavesExistentes = permisosExistentes
                        .Select(p => ClavePermiso(p.Modulo, p.Submodulo))
                        .ToHashSet();

                    bool rolModificado = false;

                    foreach (var moduloKvp in ModulosERP.Modulos)
                    {
                        var modulo = moduloKvp.Key;
                        var submodulos = moduloKvp.Value;

                        // Buscar el permiso del módulo padre
                        var permisoPadre = permisosExistentes
                            .FirstOrDefault(p => p.Modulo == modulo && p.Submodulo == null);

                        // Si no existe permiso padre, no podemos migrar (nada que copiar)
                        if (permisoPadre == null)
                        {
                            // Crear uno por defecto con solo "Ver"
                            permisoPadre = new Permiso
                            {
                                RolId = rol.Id,
                                Modulo = modulo,
                                Submodulo = null,
                                PuedeVer = false,
                                PuedeCrear = false,
                                PuedeEditar = false,
                                PuedeEliminar = false
                            };
                            context.Permisos.Add(permisoPadre);
                            permisosExistentes.Add(permisoPadre);
                            clavesExistentes.Add(ClavePermiso(modulo, null));
                            permisosCreados++;
                            rolModificado = true;
                        }

                        // Para cada submódulo del módulo, crear el permiso si no existe
                        foreach (var submodulo in submodulos)
                        {
                            var clave = ClavePermiso(modulo, submodulo);
                            if (clavesExistentes.Contains(clave)) continue;

                            // Copia los permisos del padre (excepto "Ver" que se toma tal cual)
                            var nuevoPermiso = new Permiso
                            {
                                RolId = rol.Id,
                                Modulo = modulo,
                                Submodulo = submodulo,
                                PuedeVer = permisoPadre.PuedeVer,
                                PuedeCrear = permisoPadre.PuedeCrear,
                                PuedeEditar = permisoPadre.PuedeEditar,
                                PuedeEliminar = permisoPadre.PuedeEliminar
                            };

                            context.Permisos.Add(nuevoPermiso);
                            clavesExistentes.Add(clave);
                            permisosCreados++;
                            rolModificado = true;
                        }
                    }

                    if (rolModificado)
                    {
                        rolesProcesados++;
                    }
                }

                if (permisosCreados > 0)
                {
                    context.SaveChanges();
                    Console.WriteLine($"[PermisoSeeder] ✅ Migración completada: " +
                        $"{permisosCreados} permisos creados en {rolesProcesados} roles.");
                }
                else if (!soloSiHayNuevos)
                {
                    Console.WriteLine("[PermisoSeeder] ℹ️ No había permisos nuevos por crear. Todo al día.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PermisoSeeder] ❌ Error al migrar permisos: {ex.Message}");
                // No relanzamos: si falla la migración, la app debe seguir funcionando
            }
        }

        private static string ClavePermiso(string modulo, string? submodulo)
        {
            return $"{modulo}|{submodulo ?? ""}";
        }
    }
}