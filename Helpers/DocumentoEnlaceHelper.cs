using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Convierte números de documento (VEN-0012, 000-001-01-00000120, OC-0003…) y nombres de clientes o
    /// proveedores en la dirección de su detalle. Solo devuelve lo que existe y lo que el usuario puede abrir,
    /// así en cualquier pantalla el número se vuelve enlace sin que cada página tenga que armarlo.
    /// </summary>
    public static class DocumentoEnlaceHelper
    {
        private record Fuente(string Modulo, string Sub, string Url,
            Func<ApplicationDbContext, List<string>, List<(string Numero, int Id)>> Buscar);

        private static readonly Fuente[] Fuentes =
        {
            new("Ventas", "Index", "/Ventas/Details/{0}", (db, n) => db.Ventas.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("Ventas", "Facturas", "/Facturas/Details/{0}", (db, n) => db.Facturas.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("Ventas", "Cotizaciones", "/Cotizaciones/Details/{0}", (db, n) => db.Cotizaciones.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("Ventas", "Pedidos", "/Pedidos/Details/{0}", (db, n) => db.Pedidos.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("Ventas", "Devoluciones", "/Devoluciones/Details/{0}", (db, n) => db.Devoluciones.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("Inventario", "EntregasBodega", "/EntregasBodega/Details/{0}", (db, n) => db.EntregasBodega.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("Compras", "Ordenes", "/OrdenesCompra/Details/{0}", (db, n) => db.OrdenesCompra.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("Compras", "DevolucionesProveedor", "/DevolucionesProveedor/Details/{0}", (db, n) => db.DevolucionesProveedor.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("Finanzas", "Cierres", "/Finanzas/Cierres/Details/{0}", (db, n) => db.CierresCaja.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("Finanzas", "Asientos", "/Finanzas/Asientos/Details/{0}", (db, n) => db.AsientosContables.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("Inventario", "Bajas", "/Bajas/Details/{0}", (db, n) => db.BajasInventario.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("Inventario", "TomaFisica", "/InventarioAvanzado/TomaFisica/Details/{0}", (db, n) => db.TomasFisicas.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("Produccion", "Index", "/Produccion/Details/{0}", (db, n) => db.OrdenesProduccion.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("RRHH", "Nomina", "/RRHH/Nomina/Details/{0}", (db, n) => db.Nominas.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
            new("Logistica", "Envios", "/Logistica/Envios/Details/{0}", (db, n) => db.Envios.AsNoTracking().Where(x => n.Contains(x.Numero)).Select(x => new { x.Numero, x.Id }).ToList().Select(x => (x.Numero, x.Id)).ToList()),
        };

        public const int MaxPorConsulta = 300;

        /// <summary>número → dirección del detalle (solo los que existen y el usuario puede abrir).</summary>
        public static Dictionary<string, string> Documentos(ApplicationDbContext db, string? usuario, IEnumerable<string> numeros)
        {
            var lista = numeros.Where(n => !string.IsNullOrWhiteSpace(n) && n.Length <= 40).Select(n => n.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxPorConsulta).ToList();
            var res = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (lista.Count == 0 || string.IsNullOrEmpty(usuario)) return res;
            foreach (var f in Fuentes)
            {
                if (!PermisoHelper.UsuarioTieneAcceso(db, usuario, f.Modulo, f.Sub)) continue;
                foreach (var (numero, id) in f.Buscar(db, lista))
                    res.TryAdd(numero, string.Format(f.Url, id));      // el primero gana (los prefijos no se repiten entre módulos)
            }
            return res;
        }

        /// <summary>nombre → ficha, solo cuando el nombre corresponde a una sola persona o empresa.</summary>
        public static Dictionary<string, string> Terceros(ApplicationDbContext db, string? usuario, IEnumerable<string> nombres, bool proveedores)
        {
            var lista = nombres.Where(n => !string.IsNullOrWhiteSpace(n) && n.Length <= 200).Select(n => n.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxPorConsulta).ToList();
            var res = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (lista.Count == 0 || string.IsNullOrEmpty(usuario)) return res;
            if (!PermisoHelper.UsuarioTieneAcceso(db, usuario, proveedores ? "Compras" : "Ventas", proveedores ? "Proveedores" : "Clientes")) return res;
            var encontrados = proveedores
                ? db.Proveedores.AsNoTracking().Where(x => lista.Contains(x.Nombre)).Select(x => new { x.Nombre, x.Id }).ToList()
                : db.Clientes.AsNoTracking().Where(x => lista.Contains(x.Nombre)).Select(x => new { x.Nombre, x.Id }).ToList();
            foreach (var g in encontrados.GroupBy(x => x.Nombre.Trim(), StringComparer.OrdinalIgnoreCase).Where(g => g.Count() == 1))
                res[g.Key] = (proveedores ? "/Proveedores/Details/" : "/Clientes/Details/") + g.First().Id;
            return res;
        }
    }
}
