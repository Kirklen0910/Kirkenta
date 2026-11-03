using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>Un punto de la lista de configuración inicial.</summary>
    public record PasoConfiguracion(string Clave, string Titulo, string Detalle, bool Completo, bool Opcional, string Pagina);

    /// <summary>Calcula qué falta por configurar en un negocio recién instalado.</summary>
    public static class ConfiguracionInicialHelper
    {
        public const string NombrePorDefecto = "Mi Empresa";

        public static readonly (string Nombre, string Abreviatura)[] UnidadesSugeridas =
        {
            ("Unidad", "und"), ("Libra", "lb"), ("Kilogramo", "kg"), ("Litro", "lt"), ("Galón", "gal"),
            ("Metro", "m"), ("Caja", "cj"), ("Docena", "doc"), ("Paquete", "paq"), ("Par", "par"),
        };

        public static readonly string[] ColoresCategorias = { "#2563eb", "#16a34a", "#d97706", "#dc2626", "#7c3aed", "#0891b2", "#db2777", "#4b5563" };

        public static bool EmpresaConfigurada(ConfiguracionEmpresa? e)
            => e != null && !string.IsNullOrWhiteSpace(e.Nombre)
               && !string.Equals(e.Nombre.Trim(), NombrePorDefecto, StringComparison.OrdinalIgnoreCase)
               && !string.IsNullOrWhiteSpace(e.RTN);

        /// <summary>Lista completa (pasos del asistente + tareas de arranque), en el orden recomendado.</summary>
        public static List<PasoConfiguracion> Estado(ApplicationDbContext db)
        {
            var empresa = db.ConfiguracionEmpresa.AsNoTracking().FirstOrDefault();
            bool caja = db.CuentasFinancieras.Any(c => c.Tipo == "Caja" && c.Activa);
            bool categorias = db.Categorias.Any();
            bool productos = db.Productos.Any();
            bool clientes = db.Clientes.Any();
            bool usuarios = db.Usuarios.Count(u => u.Activo) > 1;
            bool venta = db.Ventas.Any(v => v.LoteMigracion == null);
            bool migrado = db.RegistrosMigracion.Any() || db.Ventas.Any(v => v.LoteMigracion != null) || db.OrdenesCompra.Any(o => o.LoteMigracion != null);
            var respaldos = RespaldoEstado.Leer();

            return new List<PasoConfiguracion>
            {
                new("empresa", "Datos de la empresa", "Nombre, RTN, dirección y teléfono que salen en facturas y reportes.", EmpresaConfigurada(empresa), false, "/Configuracion/Inicio?paso=1"),
                new("fiscal", "Facturación fiscal (SAR)", "CAI, rango autorizado y fecha límite. Si todavía no los tienes, puedes hacerlo después.", empresa?.FacturacionFiscalActiva == true, true, "/Configuracion/Inicio?paso=2"),
                new("caja", "Caja y cuenta bancaria", "La caja donde entra el efectivo de las ventas y, si quieres, tu cuenta del banco.", caja, false, "/Configuracion/Inicio?paso=3"),
                new("catalogo", "Categorías y unidades de medida", "Para organizar tus productos y venderlos por libra, caja, litro, etc.", categorias, false, "/Configuracion/Inicio?paso=4"),
                new("productos", "Cargar tus productos", "Créalos uno por uno o impórtalos desde Excel.", productos, false, "/Productos/Index"),
                new("clientes", "Cargar tus clientes", "Opcional: así puedes facturar a crédito y cobrar con recordatorios.", clientes, true, "/Clientes/Index"),
                new("usuarios", "Crear los usuarios del negocio", "Un usuario para cada cajero o vendedor, con el rol que les toca.", usuarios, true, "/Usuarios/Index"),
                new("respaldo", "Hacer tu primer respaldo", "Con un clic queda guardada toda la información del negocio.", respaldos.Ultimo != null, false, "/Configuracion/Respaldos"),
                new("venta", "Hacer tu primera venta", "Una venta real (o de prueba) en el punto de venta para ver el ticket, la caja y el inventario moverse.", venta, false, "/POS/Index"),
            }.Concat(migrado
                ? new[] { new PasoConfiguracion("migracion", "Revisar lo que trajiste del sistema anterior", "Lotes migrados, su conciliación y, si algo no cuadra, deshacerlo.", false, true, "/Migracion/Index?continuar=1") }
                : Array.Empty<PasoConfiguracion>()).ToList();
        }

        /// <summary>true si falta algo obligatorio.</summary>
        public static bool FaltaConfigurar(ApplicationDbContext db)
            => Estado(db).Any(p => !p.Completo && !p.Opcional && p.Clave is "empresa" or "caja" or "catalogo");

        /// <summary>Código único para una cuenta nueva: PREFIJO-01, PREFIJO-02…</summary>
        public static string CodigoCuentaLibre(ApplicationDbContext db, string prefijo)
        {
            var usados = new HashSet<string>(db.CuentasFinancieras.Select(c => c.Codigo), StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < 1000; i++)
            {
                var c = $"{prefijo}-{i:00}";
                if (!usados.Contains(c)) return c;
            }
            return $"{prefijo}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        }

        /// <summary>Separa un texto en líneas/comas, quita vacíos y repetidos (sin importar mayúsculas) y limita el largo de cada nombre.</summary>
        public static List<string> ParsearLista(string? texto, int maxLargo = 100, int maxItems = 50)
        {
            var res = new List<string>();
            if (string.IsNullOrWhiteSpace(texto)) return res;
            foreach (var parte in texto.Split(new[] { '\n', '\r', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var n = parte.Length > maxLargo ? parte[..maxLargo].Trim() : parte;
                if (n.Length == 0 || res.Contains(n, StringComparer.OrdinalIgnoreCase)) continue;
                res.Add(n);
                if (res.Count >= maxItems) break;
            }
            return res;
        }
    }
}
