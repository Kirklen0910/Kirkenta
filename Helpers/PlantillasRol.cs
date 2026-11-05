using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Editor de roles: plantillas por puesto (un clic y después se ajusta), niveles por módulo
    /// (sin acceso / solo ver / trabajar / todo) y las opciones delicadas que conviene dar con cuidado.
    /// </summary>
    public static class PlantillasRol
    {
        /// <summary>0 = sin acceso, 1 = solo ver, 2 = trabajar (ver, crear, editar), 3 = todo (incluye eliminar).</summary>
        public static bool[] Banderas(int nivel) => nivel switch
        {
            1 => new[] { true, false, false, false },
            2 => new[] { true, true, true, false },
            3 => new[] { true, true, true, true },
            _ => new[] { false, false, false, false }
        };

        public record Plantilla(string Clave, string Nombre, string Descripcion, string Color,
            Dictionary<string, int> Modulos, Dictionary<string, int> Opciones);

        private static Dictionary<string, int> D(params (string, int)[] x) => x.ToDictionary(p => p.Item1, p => p.Item2);

        public static readonly List<Plantilla> Todas = new()
        {
            new("cajero", "Cajero", "Vende en el punto de venta, cobra, abre y cierra su caja.", "#0891b2",
                D(),
                D(("Ventas/POS", 2), ("Ventas/Index", 1), ("Ventas/Facturas", 2), ("Ventas/Clientes", 2), ("Ventas/Cotizaciones", 1),
                  ("Inventario/Productos", 1),
                  ("Finanzas/Aperturas", 2), ("Finanzas/AperturasCreate", 2), ("Finanzas/Cierres", 2), ("Finanzas/CierresCreate", 2), ("Finanzas/Cobros", 2))),
            new("vendedor", "Vendedor", "Atiende clientes: cotiza, toma pedidos, vende y factura.", "#2563eb",
                D(),
                D(("Ventas/POS", 2), ("Ventas/Index", 1), ("Ventas/Cotizaciones", 2), ("Ventas/Pedidos", 2), ("Ventas/Facturas", 2),
                  ("Ventas/Clientes", 2), ("Ventas/ClientesExport", 1), ("Ventas/Devoluciones", 1),
                  ("Inventario/Productos", 1), ("Logistica/Tracking", 1), ("Reportes/Ventas", 1))),
            new("bodeguero", "Bodeguero", "Maneja el inventario: entradas, bajas, conteos y recepción de compras.", "#16a34a",
                D(("Inventario", 2)),
                D(("Inventario/ProductosImport", 0), ("Inventario/ProductosExport", 1), ("Inventario/Kardex", 1), ("Inventario/Valoracion", 1),
                  ("Compras/Ordenes", 1), ("Compras/OrdenesRecibir", 2), ("Logistica/Envios", 1), ("Reportes/Inventario", 1))),
            new("comprador", "Comprador", "Compra a proveedores, recibe mercadería y devuelve lo dañado.", "#d97706",
                D(("Compras", 2)),
                D(("Compras/Pagos", 1), ("Compras/PagosCreate", 0), ("Compras/CuentasPorPagar", 1),
                  ("Inventario/Productos", 1), ("Inventario/Kardex", 1), ("Reportes/Compras", 1))),
            new("contador", "Contador", "Lleva finanzas y contabilidad, cierres contables, ISV y reportes.", "#7c3aed",
                D(("Finanzas", 2), ("Reportes", 1), ("Activos", 1)),
                D(("Finanzas/CierresContablesReabrir", 0), ("Finanzas/MovimientosAnular", 0),
                  ("Ventas/Facturas", 1), ("Compras/CuentasPorPagar", 1), ("Compras/Pagos", 1))),
            new("rrhh", "Recursos Humanos", "Empleados, expedientes, vacaciones, permisos, vales y planilla.", "#db2777",
                D(("RRHH", 2)),
                D(("RRHH/NominaAprobar", 0), ("RRHH/NominaPagar", 0), ("RRHH/ValesEntregar", 0))),
            new("supervisor", "Supervisor", "Ve y trabaja en toda la operación, aprueba cierres; no toca usuarios ni configuración.", "#4f46e5",
                D(("Ventas", 2), ("Inventario", 2), ("Compras", 2), ("Finanzas", 2), ("Logistica", 2), ("Produccion", 2), ("Reportes", 1), ("Activos", 1)),
                D(("Finanzas/CierresContablesReabrir", 0))),
            new("repartidor", "Repartidor", "Ve sus envíos y rutas de entrega.", "#4b5563",
                D(),
                D(("Logistica/Envios", 1), ("Logistica/Rutas", 1), ("Logistica/Tracking", 1), ("Ventas/Pedidos", 1))),
        };

        /// <summary>«Módulo/Opción» → nivel que le da la plantilla (solo opciones que existen).</summary>
        public static Dictionary<string, int> Niveles(Plantilla p)
        {
            var r = new Dictionary<string, int>();
            foreach (var (mod, subs) in ModulosERP.Modulos)
                foreach (var sub in subs)
                {
                    var clave = mod + "/" + sub;
                    var nivel = p.Opciones.TryGetValue(clave, out var n) ? n : p.Modulos.GetValueOrDefault(mod);
                    if (nivel > 0) r[clave] = nivel;
                }
            return r;
        }

        /// <summary>Opciones que conviene dar con cuidado (además de cualquier «eliminar»).</summary>
        public static readonly HashSet<string> Delicadas = new(StringComparer.OrdinalIgnoreCase)
        {
            "Ventas/Delete", "Ventas/Devoluciones",
            "Compras/Pagos", "Compras/PagosCreate",
            "Finanzas/MovimientosAnular", "Finanzas/MovimientosEdit", "Finanzas/CierresAprobar",
            "Finanzas/CierresContablesCreate", "Finanzas/CierresContablesReabrir", "Finanzas/CuentasDelete", "Finanzas/PlanCuentas",
            "RRHH/Nomina", "RRHH/NominaCreate", "RRHH/NominaAprobar", "RRHH/NominaPagar", "RRHH/ValesAprobar", "RRHH/ValesEntregar",
            "Usuarios/Index", "Usuarios/Create", "Usuarios/Edit", "Usuarios/Delete", "Usuarios/Actividad",
            "Seguridad/Index", "Seguridad/Auditoria",
            "Configuracion/Respaldos", "Configuracion/Licencia", "Configuracion/Actualizaciones", "Configuracion/Empresa", "Configuracion/Series",
            "Reportes/PanelDueno",
        };

        /// <summary>Permisos actuales de un rol: «Módulo» o «Módulo/Opción» → [ver, crear, editar, eliminar].</summary>
        public static Dictionary<string, bool[]> EstadoDeRol(ApplicationDbContext db, int rolId) =>
            db.Permisos.AsNoTracking().Where(p => p.RolId == rolId).ToList()
              .GroupBy(p => p.Submodulo == null ? p.Modulo : p.Modulo + "/" + p.Submodulo)
              .ToDictionary(g => g.Key, g =>
              {
                  var p = g.First();
                  return new[] { p.PuedeVer, p.PuedeCrear, p.PuedeEditar, p.PuedeEliminar };
              });
    }

    /// <summary>Lo que necesita el editor de permisos (Shared: Usuarios/_EditorPermisos).</summary>
    public class EditorPermisos
    {
        public Dictionary<string, bool[]> Estado { get; set; } = new();
        public string? PlantillaInicial { get; set; }
        /// <summary>Otros roles para «copiar permisos de…»: nombre → estado.</summary>
        public Dictionary<string, Dictionary<string, bool[]>> OtrosRoles { get; set; } = new();

        public static EditorPermisos Crear(ApplicationDbContext db, int? rolActualId, string? plantilla = null)
        {
            var e = new EditorPermisos { PlantillaInicial = plantilla };
            if (rolActualId.HasValue) e.Estado = PlantillasRol.EstadoDeRol(db, rolActualId.Value);
            foreach (var r in db.Roles.AsNoTracking().Where(r => r.Nombre != "Admin" && (!rolActualId.HasValue || r.Id != rolActualId.Value)).OrderBy(r => r.Nombre).ToList())
                e.OtrosRoles[r.Nombre] = PlantillasRol.EstadoDeRol(db, r.Id);
            return e;
        }
    }
}
