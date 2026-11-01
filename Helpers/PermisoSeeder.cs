using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Seeder de migración de permisos.
    /// Cuando se agregan nuevos módulos o submódulos a ModulosERP.Modulos, los roles
    /// existentes no tienen filas de permisos para ellos. Este helper las crea
    /// SIEMPRE con todos los permisos en falso: un submódulo nuevo nunca da acceso
    /// por sí solo. El administrador debe activarlo desde Roles → Editar.
    /// (Antes copiaba los permisos del módulo padre, y eso daba acceso a submódulos
    /// que el administrador no había marcado.)
    /// Es idempotente: se puede ejecutar múltiples veces sin duplicar.
    /// </summary>
    public static class PermisoSeeder
    {
        /// <summary>
        /// Crea, para todos los roles existentes (excepto Admin), las filas de permiso
        /// que falten para los módulos/submódulos definidos en ModulosERP.
        /// </summary>
        /// <param name="soloSiHayNuevos">
        /// Si es true, solo escribe en consola cuando detecta submódulos faltantes.
        /// Si es false, también avisa cuando no había nada por crear.
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

                        // Fila del módulo padre: si falta, se crea sin ningún permiso
                        if (!clavesExistentes.Contains(ClavePermiso(modulo, null)))
                        {
                            context.Permisos.Add(NuevoPermisoSinAcceso(rol.Id, modulo, null));
                            clavesExistentes.Add(ClavePermiso(modulo, null));
                            permisosCreados++;
                            rolModificado = true;
                        }

                        // Filas de submódulos: si faltan, se crean sin ningún permiso
                        foreach (var submodulo in submodulos)
                        {
                            var clave = ClavePermiso(modulo, submodulo);
                            if (clavesExistentes.Contains(clave)) continue;

                            context.Permisos.Add(NuevoPermisoSinAcceso(rol.Id, modulo, submodulo));
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
                        $"{permisosCreados} permisos creados (sin acceso) en {rolesProcesados} roles.");
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

        private sealed record Regla(
            string Modulo, string Submodulo,
            string RefModulo, string RefSubmodulo,
            Func<Permiso, bool> DaVer, Func<Permiso, bool> DaCrear,
            bool CrearImplicaVer);

        /// <summary>
        /// Da permisos iniciales razonables a los roles (no Admin) para las pantallas de
        /// devoluciones, tomando como referencia lo que cada rol ya puede hacer:
        ///   - Ventas/Devoluciones: ver si puede ver Facturas; crear si puede anular facturas.
        ///   - Compras/DevolucionesProveedor: ver si puede ver Órdenes de compra.
        ///   - Compras/DevolucionesProveedorCreate: crear si puede editar Órdenes de compra.
        /// Solo actúa si NINGÚN rol tiene todavía algún permiso en esa pantalla (o sea,
        /// el administrador nunca la ha configurado), así que no pisa decisiones propias:
        /// una vez aplicado, ya no vuelve a tocarla. Idempotente.
        /// </summary>
        public static void AplicarPermisosPredeterminados(ApplicationDbContext context)
        {
            try
            {
                var roles = context.Roles.Where(r => r.Nombre != "Admin").ToList();
                if (roles.Count == 0) return;

                var rolIds = roles.Select(r => r.Id).ToList();
                var permisos = context.Permisos.Where(p => rolIds.Contains(p.RolId)).ToList();

                var reglas = new[]
                {
                    new Regla("Ventas", "Devoluciones", "Ventas", "Facturas",
                        p => p.PuedeVer, p => p.PuedeEliminar, true),
                    new Regla("Compras", "DevolucionesProveedor", "Compras", "Ordenes",
                        p => p.PuedeVer, p => false, false),
                    new Regla("Compras", "DevolucionesProveedorCreate", "Compras", "Ordenes",
                        p => false, p => p.PuedeEditar, false),
                    // Reportes ahora tiene permiso propio por reporte: quien ya veia Reportes sigue viendolos
                    new Regla("Reportes", "Inventario", "Reportes", "Index", p => p.PuedeVer, p => false, false),
                    new Regla("Reportes", "Rentabilidad", "Reportes", "Index", p => p.PuedeVer, p => false, false),
                    new Regla("Reportes", "CuentasPorCobrar", "Reportes", "Index", p => p.PuedeVer, p => false, false),
                    new Regla("Reportes", "CuentasPorPagar", "Reportes", "Index", p => p.PuedeVer, p => false, false),
                    new Regla("Reportes", "ISV", "Reportes", "Index", p => p.PuedeVer, p => false, false),
                    new Regla("Reportes", "PanelDueno", "Reportes", "Index", p => p.PuedeVer, p => false, false)
                };

                int cambios = 0;
                foreach (var regla in reglas)
                {
                    var objetivo = permisos
                        .Where(p => p.Modulo == regla.Modulo && p.Submodulo == regla.Submodulo)
                        .ToList();

                    // Aún no existen las filas, o alguien ya configuró esta pantalla: no tocar
                    if (objetivo.Count == 0) continue;
                    if (objetivo.Any(p => p.PuedeVer || p.PuedeCrear || p.PuedeEditar || p.PuedeEliminar)) continue;

                    foreach (var rol in roles)
                    {
                        var destino = objetivo.FirstOrDefault(p => p.RolId == rol.Id);
                        var referencia = permisos.FirstOrDefault(p => p.RolId == rol.Id
                            && p.Modulo == regla.RefModulo && p.Submodulo == regla.RefSubmodulo);
                        if (destino == null || referencia == null) continue;

                        if (regla.DaVer(referencia)) { destino.PuedeVer = true; cambios++; }
                        if (regla.DaCrear(referencia))
                        {
                            destino.PuedeCrear = true;
                            if (regla.CrearImplicaVer) destino.PuedeVer = true;
                            cambios++;
                        }
                    }
                }

                if (cambios > 0)
                {
                    context.SaveChanges();
                    Console.WriteLine($"[PermisoSeeder] ✅ Permisos iniciales de devoluciones aplicados ({cambios} ajustes).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PermisoSeeder] ❌ Error al aplicar permisos predeterminados: {ex.Message}");
            }
        }

        private static Permiso NuevoPermisoSinAcceso(int rolId, string modulo, string? submodulo)
        {
            return new Permiso
            {
                RolId = rolId,
                Modulo = modulo,
                Submodulo = submodulo,
                PuedeVer = false,
                PuedeCrear = false,
                PuedeEditar = false,
                PuedeEliminar = false
            };
        }

        private static string ClavePermiso(string modulo, string? submodulo)
        {
            return $"{modulo}|{submodulo ?? ""}";
        }
    }
}