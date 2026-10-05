# KIRKENTA ERP - SNAPSHOT: PARTE 1: NUCLEO Y CONFIGURACION

**Fecha de generacion:** 04/10/2026 21:40
**Raiz del proyecto:** C:\Users\crobe\OneDrive\Kirkenta
**Descripcion:** Data, Models, Migrations, Helpers base, Usuarios, Auth, Configuracion, Series, wwwroot, Program.cs

---

**Total archivos en esta parte:** 183

## INDICE

- **Data** (1 archivo(s))
- **Helpers** (5 archivo(s))
- **Helpers\Export** (5 archivo(s))
- **Helpers\Import** (8 archivo(s))
- **Migrations** (5 archivo(s))
- **Models** (54 archivo(s))
- **Pages** (8 archivo(s))
- **Pages\Auth** (7 archivo(s))
- **Pages\Configuracion** (4 archivo(s))
- **Pages\Impuestos** (14 archivo(s))
- **Pages\MetodosPago** (8 archivo(s))
- **Pages\Nomenclatura** (2 archivo(s))
- **Pages\Series** (8 archivo(s))
- **Pages\Shared** (4 archivo(s))
- **Pages\UnidadesMedida** (14 archivo(s))
- **Pages\Usuarios** (22 archivo(s))
- **Properties** (1 archivo(s))
- **RAIZ** (7 archivo(s))
- **wwwroot\css** (5 archivo(s))
- **wwwroot\js** (1 archivo(s))

---

====================================================
 Data - 1 archivo(s)
====================================================

===== FILE: Data/ApplicationDbContext.cs =====

````csharp
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ===== USUARIOS Y PERMISOS =====
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Permiso> Permisos { get; set; }
        public DbSet<ActividadUsuario> Actividades { get; set; }

        // ===== VENTAS =====
        public DbSet<SerieDocumento> SeriesDocumentos { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Impuesto> Impuestos { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<MetodoPago> MetodosPago { get; set; }
        public DbSet<UnidadMedida> UnidadesMedida { get; set; }
        public DbSet<Cotizacion> Cotizaciones { get; set; }
        public DbSet<DetalleCotizacion> DetalleCotizaciones { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallePedidos { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetalleVentas { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<DetalleFactura> DetalleFacturas { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Devolucion> Devoluciones { get; set; }
        public DbSet<DetalleDevolucion> DetalleDevoluciones { get; set; }

        // ===== CONFIGURACIÓN =====
        public DbSet<ConfiguracionEmpresa> ConfiguracionEmpresa { get; set; }

        // ===== BAJAS =====
        public DbSet<BajaInventario> BajasInventario { get; set; }

        // ===== COMPRAS =====
        public DbSet<Moneda> Monedas { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<OrdenCompra> OrdenesCompra { get; set; }
        public DbSet<DetalleOrdenCompra> DetalleOrdenesCompra { get; set; }
        public DbSet<PagoProveedor> PagosProveedor { get; set; }
        public DbSet<AdjuntoCompra> AdjuntosCompras { get; set; }
        public DbSet<DevolucionProveedor> DevolucionesProveedor { get; set; }
        public DbSet<DetalleDevolucionProveedor> DetalleDevolucionesProveedor { get; set; }

        // ===== FINANZAS =====
        public DbSet<CuentaFinanciera> CuentasFinancieras { get; set; }
        public DbSet<CategoriaFinanciera> CategoriasFinancieras { get; set; }
        public DbSet<MovimientoFinanciero> MovimientosFinancieros { get; set; }
        public DbSet<CierreCaja> CierresCaja { get; set; }
        public DbSet<AperturaCaja> AperturasCaja { get; set; }
        public DbSet<DistribucionCierre> DistribucionesCierre { get; set; }
        public DbSet<AdjuntoCierre> AdjuntosCierre { get; set; }
        public DbSet<CierreContable> CierresContables { get; set; }
        public DbSet<ConciliacionBancaria> ConciliacionesBancarias { get; set; }
        public DbSet<ConciliacionDetalle> ConciliacionesDetalle { get; set; }
        public DbSet<PlanCuenta> PlanCuentas { get; set; }

        // ===== RRHH =====
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<TipoDocumentoEmpleado> TiposDocumentoEmpleado { get; set; }
        public DbSet<AdjuntoEmpleado> AdjuntosEmpleado { get; set; }
        public DbSet<ExpedienteEmpleado> ExpedientesEmpleado { get; set; }
        public DbSet<VacacionEmpleado> VacacionesEmpleado { get; set; }
        public DbSet<PermisoEmpleado> PermisosEmpleado { get; set; }
        public DbSet<ValeEmpleado> ValesEmpleado { get; set; }
        public DbSet<ValeDescuento> ValesDescuentos { get; set; }
        public DbSet<Nomina> Nominas { get; set; }
        public DbSet<DetalleNomina> DetalleNominas { get; set; }
        public DbSet<Feriado> Feriados { get; set; }
        public DbSet<AlertaPersonalizada> AlertasPersonalizadas { get; set; }
        public DbSet<ConfiguracionDeduccion> ConfiguracionDeducciones { get; set; }
        public DbSet<TramoISR> TramosISR { get; set; }
        public DbSet<PagoNominaEmpleado> PagosNominaEmpleado { get; set; }

        // ===== LOGÍSTICA =====
        public DbSet<ZonaEnvio> ZonasEnvio { get; set; }
        public DbSet<Repartidor> Repartidores { get; set; }
        public DbSet<Envio> Envios { get; set; }
        public DbSet<Ruta> Rutas { get; set; }
        public DbSet<RutaHistorial> RutasHistorial { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== ÍNDICES ÚNICOS =====
            modelBuilder.Entity<Permiso>()
                .HasIndex(p => new { p.RolId, p.Modulo, p.Submodulo })
                .IsUnique();

            modelBuilder.Entity<Cotizacion>().HasIndex(c => c.Numero).IsUnique();
            modelBuilder.Entity<Pedido>().HasIndex(p => p.Numero).IsUnique();
            modelBuilder.Entity<Venta>().HasIndex(v => v.Numero).IsUnique();
            modelBuilder.Entity<Factura>().HasIndex(f => f.Numero).IsUnique();
            modelBuilder.Entity<Devolucion>().HasIndex(d => d.Numero).IsUnique();
            modelBuilder.Entity<Cliente>().HasIndex(c => c.Codigo).IsUnique();
            modelBuilder.Entity<Producto>().HasIndex(p => p.SKU).IsUnique();
            modelBuilder.Entity<UnidadMedida>().HasIndex(u => u.Nombre).IsUnique();

            modelBuilder.Entity<BajaInventario>().HasIndex(b => b.Numero).IsUnique();
            modelBuilder.Entity<Moneda>().HasIndex(m => m.Codigo).IsUnique();
            modelBuilder.Entity<Proveedor>().HasIndex(p => p.Codigo).IsUnique();
            modelBuilder.Entity<OrdenCompra>().HasIndex(o => o.Numero).IsUnique();
            modelBuilder.Entity<DevolucionProveedor>().HasIndex(d => d.Numero).IsUnique();

            // Finanzas
            modelBuilder.Entity<CuentaFinanciera>().HasIndex(c => c.Codigo).IsUnique();
            modelBuilder.Entity<MovimientoFinanciero>().HasIndex(m => m.Numero).IsUnique();
            modelBuilder.Entity<CierreCaja>().HasIndex(c => c.Numero).IsUnique();
            modelBuilder.Entity<AperturaCaja>().HasIndex(a => a.Numero).IsUnique();

            // Un solo cierre contable por mes
            modelBuilder.Entity<CierreContable>()
                .HasIndex(c => new { c.Anio, c.Mes })
                .IsUnique();

            // Número de conciliación único
            modelBuilder.Entity<ConciliacionBancaria>().HasIndex(c => c.Numero).IsUnique();

            // Código de cuenta contable único
            modelBuilder.Entity<PlanCuenta>().HasIndex(c => c.Codigo).IsUnique();

            // RRHH
            modelBuilder.Entity<Empleado>().HasIndex(e => e.Codigo).IsUnique();
            modelBuilder.Entity<VacacionEmpleado>().HasIndex(v => v.Numero).IsUnique();
            modelBuilder.Entity<PermisoEmpleado>().HasIndex(p => p.Numero).IsUnique();
            modelBuilder.Entity<ValeEmpleado>().HasIndex(v => v.Numero).IsUnique();
            modelBuilder.Entity<Nomina>().HasIndex(n => n.Numero).IsUnique();
            modelBuilder.Entity<PagoNominaEmpleado>().HasIndex(p => p.Numero).IsUnique();

            // Config deducciones - único por año
            modelBuilder.Entity<ConfiguracionDeduccion>().HasIndex(c => c.Anio).IsUnique();

            // ===== LOGÍSTICA =====
            modelBuilder.Entity<Envio>().HasIndex(e => e.Numero).IsUnique();
            modelBuilder.Entity<Ruta>().HasIndex(r => r.Numero).IsUnique();
            modelBuilder.Entity<Ruta>().HasIndex(r => r.TrackingCode).IsUnique();
            modelBuilder.Entity<Repartidor>().HasIndex(r => r.Codigo).IsUnique();
            modelBuilder.Entity<ZonaEnvio>().HasIndex(z => z.Nombre).IsUnique();
        }
    }
}
````

====================================================
 Helpers - 5 archivo(s)
====================================================

===== FILE: Helpers/ActividadHelper.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    public static class ActividadHelper
    {
        /// <summary>
        /// Registra una actividad de usuario en la base de datos.
        /// Nunca lanza excepción (silencioso si falla).
        /// </summary>
        public static void Registrar(
            ApplicationDbContext context,
            int usuarioId,
            string accion,
            string? detalle = null,
            string? ip = null,
            string? userAgent = null)
        {
            try
            {
                if (usuarioId <= 0) return;

                var actividad = new ActividadUsuario
                {
                    UsuarioId = usuarioId,
                    Accion = accion,
                    Detalle = detalle,
                    Ip = string.IsNullOrEmpty(ip) ? "N/A" : ip,
                    UserAgent = userAgent,
                    Fecha = DateTime.Now
                };

                context.Actividades.Add(actividad);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log silencioso: no rompemos la app si falla el log
                Console.WriteLine($"Error al registrar actividad: {ex.Message}");
            }
        }
    }
}
````

===== FILE: Helpers/ModulosERP.cs =====

````csharp
namespace Kirkenta.Helpers
{
    public static class ModulosERP
    {
        public static readonly Dictionary<string, List<string>> Modulos = new()
        {
            { "Usuarios", new List<string> {
                "Index", "Create", "Edit", "Delete",
                "Roles", "RolesCreate", "RolesEdit", "RolesDelete",
                "Perfil", "CambiarPassword", "Actividad"
            } },
            { "Ventas", new List<string> {
                "POS", "Index", "Create", "Edit", "Delete",
                "Cotizaciones", "Pedidos", "Facturas",
                "Clientes", "ClientesCreate", "ClientesEdit", "ClientesDelete",
                "ClientesExport", "ClientesImport",
                "Devoluciones"
            } },
            { "Inventario", new List<string> {
                "Index",
                "Productos", "ProductosCreate", "ProductosEdit", "ProductosDelete",
                "ProductosExport", "ProductosImport",
                "Categorias", "UnidadesMedida",
                "Entradas", "Salidas", "Bajas"
            } },
            { "Compras", new List<string> {
                "Index",
                "Proveedores", "ProveedoresCreate", "ProveedoresEdit", "ProveedoresDelete",
                "ProveedoresExport", "ProveedoresImport",
                "Ordenes", "OrdenesCreate", "OrdenesEdit", "OrdenesDelete", "OrdenesRecibir",
                "Pagos", "PagosCreate",
                "CuentasPorPagar",
                "Reportes"
            } },
            { "Finanzas", new List<string> {
                "Index",
                "Cuentas", "CuentasCreate", "CuentasEdit", "CuentasDelete",
                "Categorias", "CategoriasCreate", "CategoriasEdit", "CategoriasDelete",
                "Movimientos", "MovimientosCreate", "MovimientosEdit", "MovimientosAnular",
                "Aperturas", "AperturasCreate",
                "Cierres", "CierresCreate", "CierresAprobar",
                "CierresContables", "CierresContablesCreate", "CierresContablesReabrir",
                "Conciliacion", "ConciliacionCreate", "ConciliacionDetails", "ConciliacionCerrar",
                "PlanCuentas", "PlanCuentasCreate", "PlanCuentasEdit", "PlanCuentasDelete",
                "ContabilidadEstadoResultados",
                "ContabilidadBalanceGeneral",
                "Reportes", "FlujoCaja", "EstadoResultados", "BalanceGeneral",
                "ISV"
            } },
            { "Produccion", new List<string> { "Index", "Calidad" } },
            { "RRHH", new List<string> {
                "Index",
                "Empleados", "EmpleadosCreate", "EmpleadosEdit", "EmpleadosDelete",
                "Documentos", "DocumentosCreate", "DocumentosDelete",
                "Expedientes", "ExpedientesCreate", "ExpedientesDelete",
                "Vacaciones", "VacacionesCreate", "VacacionesAprobar",
                "Permisos", "PermisosCreate", "PermisosAprobar",
                "Vales", "ValesCreate", "ValesAprobar", "ValesEntregar",
                "Nomina", "NominaCreate", "NominaAprobar", "NominaPagar",
                "Feriados", "FeriadosCreate",
                "Alertas", "AlertasCreate",
                "Reportes"
            } },
            { "Logistica", new List<string> {
                "Index",
                "Envios", "EnviosDetails", "EnviosAsignar",
                "Rutas", "RutasCreate", "RutasEdit", "RutasDetails", "RutasDespachar", "RutasCerrar",
                "Zonas", "ZonasCreate", "ZonasEdit", "ZonasDelete",
                "Repartidores", "RepartidoresCreate", "RepartidoresEdit", "RepartidoresDelete",
                "Tracking",
                "Reportes"
            } },
            { "Reportes", new List<string> { "Index", "Ventas", "Compras" } },
            { "Activos", new List<string> { "Index", "Mantenimiento" } },
            { "Seguridad", new List<string> { "Index", "Auditoria" } },
            { "Configuracion", new List<string> {
                "Index", "Empresa", "Nomenclatura", "Series", "MetodosPago"
            } }
        };

        public static string NombreBonito(string modulo)
        {
            return modulo switch
            {
                "Usuarios" => "Usuarios",
                "Ventas" => "Ventas",
                "Inventario" => "Inventario",
                "Compras" => "Compras",
                "Finanzas" => "Finanzas",
                "Produccion" => "Producción",
                "RRHH" => "Recursos Humanos",
                "Logistica" => "Logística",
                "Reportes" => "Reportes",
                "Activos" => "Activos",
                "Seguridad" => "Seguridad",
                "Configuracion" => "Configuración",
                _ => modulo
            };
        }

        public static string NombreBonitoSub(string submodulo)
        {
            return submodulo switch
            {
                "Index" => "Lista / Vista",
                "Create" => "Crear",
                "Edit" => "Editar",
                "Delete" => "Eliminar",
                "Roles" => "Roles",
                "RolesCreate" => "Nuevo rol",
                "RolesEdit" => "Editar rol",
                "RolesDelete" => "Eliminar rol",
                "Perfil" => "Mi perfil",
                "CambiarPassword" => "Cambiar contraseña",
                "Actividad" => "Historial de actividad",
                "POS" => "Punto de venta",
                "Cotizaciones" => "Cotizaciones",
                "Pedidos" => "Pedidos",
                "Facturas" => "Facturas",
                "Clientes" => "Clientes",
                "ClientesCreate" => "Nuevo cliente",
                "ClientesEdit" => "Editar cliente",
                "ClientesDelete" => "Eliminar cliente",
                "ClientesExport" => "Exportar clientes",
                "ClientesImport" => "Importar clientes",
                "Devoluciones" => "Devoluciones",
                "Productos" => "Productos",
                "ProductosCreate" => "Nuevo producto",
                "ProductosEdit" => "Editar producto",
                "ProductosDelete" => "Eliminar producto",
                "ProductosExport" => "Exportar productos",
                "ProductosImport" => "Importar productos",
                "Categorias" => "Categorías",
                "UnidadesMedida" => "Unidades de medida",
                "Entradas" => "Entradas",
                "Salidas" => "Salidas",
                "Bajas" => "Bajas de inventario",
                "Proveedores" => "Proveedores",
                "ProveedoresCreate" => "Nuevo proveedor",
                "ProveedoresEdit" => "Editar proveedor",
                "ProveedoresDelete" => "Eliminar proveedor",
                "ProveedoresExport" => "Exportar proveedores",
                "ProveedoresImport" => "Importar proveedores",
                "Ordenes" => "Órdenes de compra",
                "OrdenesCreate" => "Nueva orden",
                "OrdenesEdit" => "Editar orden",
                "OrdenesDelete" => "Eliminar orden",
                "OrdenesRecibir" => "Recibir mercancía",
                "Pagos" => "Pagos a proveedores",
                "PagosCreate" => "Registrar pago",
                "CuentasPorPagar" => "Cuentas por pagar",
                // FINANZAS
                "Cuentas" => "Cuentas financieras",
                "CuentasCreate" => "Nueva cuenta",
                "CuentasEdit" => "Editar cuenta",
                "CuentasDelete" => "Eliminar cuenta",
                "CategoriasFinancieras" => "Categorías financieras",
                "CategoriasCreateFin" => "Nueva categoría financiera",
                "CategoriasEditFin" => "Editar categoría financiera",
                "CategoriasDeleteFin" => "Eliminar categoría financiera",
                "Movimientos" => "Movimientos",
                "MovimientosCreate" => "Nuevo movimiento",
                "MovimientosEdit" => "Editar movimiento",
                "MovimientosAnular" => "Anular movimiento",
                "Aperturas" => "Aperturas de caja",
                "AperturasCreate" => "Nueva apertura",
                "Cierres" => "Cierres de caja",
                "CierresCreate" => "Nuevo cierre",
                "CierresAprobar" => "Aprobar cierre",
                "CierresContables" => "Cierres contables",
                "CierresContablesCreate" => "Cerrar mes",
                "CierresContablesReabrir" => "Reabrir mes",
                "Conciliacion" => "Conciliación bancaria",
                "ConciliacionCreate" => "Nueva conciliación",
                "ConciliacionDetails" => "Detalle de conciliación",
                "ConciliacionCerrar" => "Cerrar conciliación",
                "PlanCuentas" => "Plan de Cuentas",
                "PlanCuentasCreate" => "Nueva cuenta contable",
                "PlanCuentasEdit" => "Editar cuenta contable",
                "PlanCuentasDelete" => "Eliminar cuenta contable",
                "ContabilidadEstadoResultados" => "Estado de Resultados",
                "ContabilidadBalanceGeneral" => "Balance General",
                "FlujoCaja" => "Flujo de caja",
                "EstadoResultados" => "Estado de resultados",
                "BalanceGeneral" => "Balance general",
                "ISV" => "ISV / IVA",
                // RRHH
                "Empleados" => "Empleados",
                "EmpleadosCreate" => "Nuevo empleado",
                "EmpleadosEdit" => "Editar empleado",
                "EmpleadosDelete" => "Eliminar empleado",
                "Documentos" => "Documentos",
                "DocumentosCreate" => "Subir documento",
                "DocumentosDelete" => "Eliminar documento",
                "Expedientes" => "Expedientes",
                "ExpedientesCreate" => "Nuevo expediente",
                "ExpedientesDelete" => "Eliminar expediente",
                "Vacaciones" => "Vacaciones",
                "VacacionesCreate" => "Nueva solicitud",
                "VacacionesAprobar" => "Aprobar vacaciones",
                "Permisos" => "Permisos y licencias",
                "PermisosCreate" => "Nueva solicitud",
                "PermisosAprobar" => "Aprobar permiso",
                "Vales" => "Vales a empleados",
                "ValesCreate" => "Nuevo vale",
                "ValesAprobar" => "Aprobar vale",
                "ValesEntregar" => "Entregar vale",
                "Nomina" => "Nómina",
                "NominaCreate" => "Generar nómina",
                "NominaAprobar" => "Aprobar nómina",
                "NominaPagar" => "Pagar nómina",
                "Feriados" => "Feriados",
                "FeriadosCreate" => "Nuevo feriado",
                "Alertas" => "Alertas",
                "AlertasCreate" => "Nueva alerta",
                // LOGÍSTICA
                "Envios" => "Envíos",
                "EnviosDetails" => "Detalle de envío",
                "EnviosAsignar" => "Asignar envíos a ruta",
                "Rutas" => "Rutas de despacho",
                "RutasCreate" => "Nueva ruta",
                "RutasEdit" => "Editar ruta",
                "RutasDetails" => "Detalle de ruta",
                "RutasDespachar" => "Despachar ruta",
                "RutasCerrar" => "Cerrar ruta",
                "Zonas" => "Zonas de envío",
                "ZonasCreate" => "Nueva zona",
                "ZonasEdit" => "Editar zona",
                "ZonasDelete" => "Eliminar zona",
                "Repartidores" => "Repartidores",
                "RepartidoresCreate" => "Nuevo repartidor",
                "RepartidoresEdit" => "Editar repartidor",
                "RepartidoresDelete" => "Eliminar repartidor",
                "Tracking" => "Tracking público",
                // Genéricos
                "Ingresos" => "Ingresos",
                "Egresos" => "Egresos",
                "Impuestos" => "Impuestos",
                "Calidad" => "Calidad",
                "Mantenimiento" => "Mantenimiento",
                "Auditoria" => "Auditoría",
                "Empresa" => "Datos de la empresa",
                "Nomenclatura" => "Nomenclatura",
                "Series" => "Series de documentos",
                "MetodosPago" => "Métodos de pago",
                _ => submodulo
            };
        }
    }
}
````

===== FILE: Helpers/NumeroDocumentoHelper.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    public static class NumeroDocumentoHelper
    {
        public static string GenerarSiguiente(ApplicationDbContext context, string tipo)
        {
            var serie = context.SeriesDocumentos
                .FirstOrDefault(s => s.Tipo == tipo && s.EsPredeterminada && s.Activa);

            if (serie == null)
            {
                var count = ContarDocumentos(context, tipo);
                var prefijo = tipo.Substring(0, Math.Min(3, tipo.Length)).ToUpper();
                return $"{prefijo}-{(count + 1):D4}";
            }

            SincronizarSerie(context, serie, tipo);

            var numero = GenerarNumero(serie);
            serie.SiguienteNumero++;
            context.SaveChanges();

            return numero;
        }

        public static string PreviewSiguiente(ApplicationDbContext context, string tipo)
        {
            var serie = context.SeriesDocumentos
                .FirstOrDefault(s => s.Tipo == tipo && s.EsPredeterminada && s.Activa);

            if (serie == null)
            {
                var prefijo = tipo.Substring(0, Math.Min(3, tipo.Length)).ToUpper();
                return $"{prefijo}-0001";
            }

            SincronizarSerie(context, serie, tipo);
            return GenerarNumero(serie);
        }

        private static string GenerarNumero(SerieDocumento serie)
        {
            var numero = serie.SiguienteNumero.ToString().PadLeft(serie.LongitudNumero, '0');
            var prefijo = serie.Prefijo ?? "";
            var sufijo = serie.Sufijo ?? "";
            var sep = serie.Separador ?? "-";
            var anio = DateTime.Now.Year.ToString();
            var mes = DateTime.Now.Month.ToString("D2");
            var dia = DateTime.Now.Day.ToString("D2");

            if (!string.IsNullOrWhiteSpace(serie.FormatoPersonalizado))
            {
                return serie.FormatoPersonalizado
                    .Replace("{PREFIX}", prefijo)
                    .Replace("{PREFIJO}", prefijo)
                    .Replace("{SUFFIX}", sufijo)
                    .Replace("{SUFIJO}", sufijo)
                    .Replace("{SEP}", sep)
                    .Replace("{YEAR}", anio)
                    .Replace("{ANIO}", anio)
                    .Replace("{MONTH}", mes)
                    .Replace("{MES}", mes)
                    .Replace("{DAY}", dia)
                    .Replace("{DIA}", dia)
                    .Replace("{NUM}", numero);
            }

            return $"{prefijo}{sep}{numero}";
        }

        private static void SincronizarSerie(ApplicationDbContext context, SerieDocumento serie, string tipo)
        {
            try
            {
                int ultimoNumero = 0;
                var prefijoConSep = (serie.Prefijo ?? "") + (serie.Separador ?? "-");

                switch (tipo)
                {
                    // ===== VENTAS =====
                    case "Cotizacion":
                        ultimoNumero = context.Cotizaciones
                            .Where(c => c.Numero.StartsWith(prefijoConSep))
                            .Select(c => c.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Pedido":
                        ultimoNumero = context.Pedidos
                            .Where(p => p.Numero.StartsWith(prefijoConSep))
                            .Select(p => p.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Venta":
                        ultimoNumero = context.Ventas
                            .Where(v => v.Numero.StartsWith(prefijoConSep))
                            .Select(v => v.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Factura":
                        ultimoNumero = context.Facturas
                            .Where(f => f.Numero.StartsWith(prefijoConSep))
                            .Select(f => f.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Devolucion":
                        ultimoNumero = context.Devoluciones
                            .Where(d => d.Numero.StartsWith(prefijoConSep))
                            .Select(d => d.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    // ===== INVENTARIO =====
                    case "Producto":
                        ultimoNumero = context.Productos
                            .Where(p => p.SKU != null && p.SKU.StartsWith(prefijoConSep))
                            .Select(p => p.SKU)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Baja":
                        ultimoNumero = context.BajasInventario
                            .Where(b => b.Numero.StartsWith(prefijoConSep))
                            .Select(b => b.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    // ===== COMPRAS =====
                    case "Proveedor":
                        ultimoNumero = context.Proveedores
                            .Where(p => p.Codigo != null && p.Codigo.StartsWith(prefijoConSep))
                            .Select(p => p.Codigo!)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "OrdenCompra":
                        ultimoNumero = context.OrdenesCompra
                            .Where(o => o.Numero.StartsWith(prefijoConSep))
                            .Select(o => o.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "PagoProveedor":
                        ultimoNumero = context.PagosProveedor
                            .Where(p => p.Numero != null && p.Numero.StartsWith(prefijoConSep))
                            .Select(p => p.Numero!)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "DevolucionProveedor":
                        ultimoNumero = context.DevolucionesProveedor
                            .Where(d => d.Numero.StartsWith(prefijoConSep))
                            .Select(d => d.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    // ===== FINANZAS =====
                    case "MovimientoFinanciero":
                        ultimoNumero = context.MovimientosFinancieros
                            .Where(m => m.Numero.StartsWith(prefijoConSep))
                            .Select(m => m.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "AperturaCaja":
                        ultimoNumero = context.AperturasCaja
                            .Where(a => a.Numero.StartsWith(prefijoConSep))
                            .Select(a => a.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "CierreCaja":
                        ultimoNumero = context.CierresCaja
                            .Where(c => c.Numero.StartsWith(prefijoConSep))
                            .Select(c => c.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "ConciliacionBancaria":
                        ultimoNumero = context.ConciliacionesBancarias
                            .Where(c => c.Numero.StartsWith(prefijoConSep))
                            .Select(c => c.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    // ===== RRHH =====
                    case "Empleado":
                        ultimoNumero = context.Empleados
                            .Where(e => e.Codigo.StartsWith(prefijoConSep))
                            .Select(e => e.Codigo)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "ValeEmpleado":
                        ultimoNumero = context.ValesEmpleado
                            .Where(v => v.Numero.StartsWith(prefijoConSep))
                            .Select(v => v.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Vacacion":
                        ultimoNumero = context.VacacionesEmpleado
                            .Where(v => v.Numero.StartsWith(prefijoConSep))
                            .Select(v => v.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "PermisoEmpleado":
                        ultimoNumero = context.PermisosEmpleado
                            .Where(p => p.Numero.StartsWith(prefijoConSep))
                            .Select(p => p.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Nomina":
                        ultimoNumero = context.Nominas
                            .Where(n => n.Numero.StartsWith(prefijoConSep))
                            .Select(n => n.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "PagoNomina":
                        ultimoNumero = context.PagosNominaEmpleado
                            .Where(p => p.Numero.StartsWith(prefijoConSep))
                            .Select(p => p.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    // ===== LOGÍSTICA =====
                    case "Envio":
                        ultimoNumero = context.Envios
                            .Where(e => e.Numero.StartsWith(prefijoConSep))
                            .Select(e => e.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Ruta":
                        ultimoNumero = context.Rutas
                            .Where(r => r.Numero.StartsWith(prefijoConSep))
                            .Select(r => r.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Repartidor":
                        ultimoNumero = context.Repartidores
                            .Where(r => r.Codigo.StartsWith(prefijoConSep))
                            .Select(r => r.Codigo)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;
                }

                if (ultimoNumero >= serie.SiguienteNumero)
                {
                    serie.SiguienteNumero = ultimoNumero + 1;
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aviso al sincronizar serie {tipo}: {ex.Message}");
            }
        }

        private static int ExtraerNumero(string texto, string prefijoConSep)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(texto)) return 0;

                var sinPrefijo = texto;
                if (!string.IsNullOrEmpty(prefijoConSep) && texto.StartsWith(prefijoConSep))
                {
                    sinPrefijo = texto.Substring(prefijoConSep.Length);
                }

                var match = System.Text.RegularExpressions.Regex.Match(sinPrefijo, @"(\d+)(?!.*\d)");
                if (match.Success && int.TryParse(match.Value, out var num))
                    return num;

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private static int ContarDocumentos(ApplicationDbContext context, string tipo)
        {
            return tipo switch
            {
                // Ventas
                "Cotizacion" => context.Cotizaciones.Count(),
                "Pedido" => context.Pedidos.Count(),
                "Venta" => context.Ventas.Count(),
                "Factura" => context.Facturas.Count(),
                "Devolucion" => context.Devoluciones.Count(),

                // Inventario
                "Producto" => context.Productos.Count(),
                "Baja" => context.BajasInventario.Count(),

                // Compras
                "Proveedor" => context.Proveedores.Count(),
                "OrdenCompra" => context.OrdenesCompra.Count(),
                "PagoProveedor" => context.PagosProveedor.Count(),
                "DevolucionProveedor" => context.DevolucionesProveedor.Count(),

                // Finanzas
                "MovimientoFinanciero" => context.MovimientosFinancieros.Count(),
                "AperturaCaja" => context.AperturasCaja.Count(),
                "CierreCaja" => context.CierresCaja.Count(),
                "ConciliacionBancaria" => context.ConciliacionesBancarias.Count(),

                // RRHH
                "Empleado" => context.Empleados.Count(),
                "ValeEmpleado" => context.ValesEmpleado.Count(),
                "Vacacion" => context.VacacionesEmpleado.Count(),
                "PermisoEmpleado" => context.PermisosEmpleado.Count(),
                "Nomina" => context.Nominas.Count(),
                "PagoNomina" => context.PagosNominaEmpleado.Count(),

                // Logística
                "Envio" => context.Envios.Count(),
                "Ruta" => context.Rutas.Count(),
                "Repartidor" => context.Repartidores.Count(),

                _ => 0
            };
        }
    }
}
````

===== FILE: Helpers/PermisoHelper.cs =====

````csharp
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
````

===== FILE: Helpers/PermisoSeeder.cs =====

````csharp
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
````

====================================================
 Helpers\Export - 5 archivo(s)
====================================================

===== FILE: Helpers/Export/CsvExporter.cs =====

````csharp
using System.Globalization;
using System.Text;

namespace Kirkenta.Helpers.Export
{
    public static class CsvExporter
    {
        public static byte[] Export<T>(IEnumerable<T> items, List<ExportColumn<T>> columns)
        {
            var sb = new StringBuilder();

            // BOM para que Excel detecte UTF-8 (y muestre bien los acentos)
            sb.Append('\uFEFF');

            // Headers
            sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsv(c.Header))));

            // Data
            foreach (var item in items)
            {
                var valores = new List<string>();
                foreach (var col in columns)
                {
                    var value = col.Value(item);
                    valores.Add(FormatValue(value, col.Format));
                }
                sb.AppendLine(string.Join(",", valores));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string FormatValue(object? value, string? format)
        {
            if (value == null) return "";

            return value switch
            {
                decimal dec => dec.ToString(format ?? "0.00", CultureInfo.InvariantCulture),
                double dbl => dbl.ToString(format ?? "0.00", CultureInfo.InvariantCulture),
                int i => i.ToString(CultureInfo.InvariantCulture),
                DateTime dt => dt.ToString(format ?? "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                bool b => b ? "1" : "0",
                _ => value.ToString() ?? ""
            };
        }

        private static string EscapeCsv(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";

            // Si contiene coma, comilla o salto de línea, hay que entrecomillar
            if (texto.Contains(',') || texto.Contains('"') || texto.Contains('\n') || texto.Contains('\r'))
            {
                return "\"" + texto.Replace("\"", "\"\"") + "\"";
            }
            return texto;
        }
    }
}
````

===== FILE: Helpers/Export/ExcelExporter.cs =====

````csharp
using ClosedXML.Excel;

namespace Kirkenta.Helpers.Export
{
    public static class ExcelExporter
    {
        public static byte[] Export<T>(
            IEnumerable<T> items,
            List<ExportColumn<T>> columns,
            string sheetName = "Datos",
            string? titulo = null)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheetName);

            int headerRow = 1;
            int dataStartRow = 2;

            if (!string.IsNullOrWhiteSpace(titulo))
            {
                var tituloCell = ws.Cell(1, 1);
                tituloCell.Value = titulo;
                tituloCell.Style.Font.Bold = true;
                tituloCell.Style.Font.FontSize = 14;
                tituloCell.Style.Font.FontColor = XLColor.FromHtml("#4f46e5");
                ws.Range(1, 1, 1, columns.Count).Merge();

                headerRow = 2;
                dataStartRow = 3;
            }

            for (int c = 0; c < columns.Count; c++)
            {
                var cell = ws.Cell(headerRow, c + 1);
                cell.Value = columns[c].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4f46e5");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                ws.Column(c + 1).Width = columns[c].Width;
            }
            ws.Row(headerRow).Height = 22;

            int row = dataStartRow;
            foreach (var item in items)
            {
                for (int c = 0; c < columns.Count; c++)
                {
                    var cell = ws.Cell(row, c + 1);
                    var value = columns[c].Value(item);

                    if (value == null)
                    {
                        cell.Value = "";
                        continue;
                    }

                    switch (value)
                    {
                        case decimal dec:
                            cell.Value = dec;
                            cell.Style.NumberFormat.Format = columns[c].Format ?? "#,##0.00";
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            break;
                        case double dbl:
                            cell.Value = dbl;
                            cell.Style.NumberFormat.Format = columns[c].Format ?? "#,##0.00";
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            break;
                        case int i:
                            cell.Value = i;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            break;
                        case DateTime dt:
                            cell.Value = dt;
                            cell.Style.DateFormat.Format = columns[c].Format ?? "dd/MM/yyyy HH:mm";
                            break;
                        case bool b:
                            cell.Value = b ? "Sí" : "No";
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            break;
                        default:
                            cell.Value = value.ToString();
                            break;
                    }
                }
                row++;
            }

            if (row > dataStartRow)
            {
                var dataRange = ws.Range(headerRow, 1, row - 1, columns.Count);
                var borderColor = XLColor.FromHtml("#e5e7eb");

                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
                dataRange.Style.Border.TopBorderColor = borderColor;
                dataRange.Style.Border.BottomBorderColor = borderColor;
                dataRange.Style.Border.LeftBorderColor = borderColor;
                dataRange.Style.Border.RightBorderColor = borderColor;
                dataRange.Style.Border.InsideBorderColor = borderColor;

                ws.Range(headerRow, 1, row - 1, columns.Count).SetAutoFilter();
                ws.SheetView.FreezeRows(headerRow);
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public static byte[] Plantilla<T>(
            List<ExportColumn<T>> columns,
            string sheetName = "Plantilla",
            string? titulo = null)
        {
            return Export(new List<T>(), columns, sheetName, titulo);
        }
    }
}
````

===== FILE: Helpers/Export/ExportColumn.cs =====

````csharp
namespace Kirkenta.Helpers.Export
{
    public class ExportColumn<T>
    {
        public string Header { get; set; } = "";
        public Func<T, object?> Value { get; set; } = _ => null;
        public string? Format { get; set; }
        public int Width { get; set; } = 18;

        public ExportColumn(string header, Func<T, object?> value, string? format = null, int width = 18)
        {
            Header = header;
            Value = value;
            Format = format;
            Width = width;
        }
    }
}
````

===== FILE: Helpers/Export/ExportColumns.cs =====

````csharp
using Kirkenta.Models;

namespace Kirkenta.Helpers.Export
{
    public static class ExportColumns
    {
        // ============================================================
        // PRODUCTOS
        // ============================================================
        public static List<ExportColumn<Producto>> Productos(
            Dictionary<int, string> categorias,
            Dictionary<int, string> impuestos)
        {
            return new List<ExportColumn<Producto>>
            {
                new("SKU", p => p.SKU ?? "", width: 15),
                new("Nombre", p => p.Nombre, width: 35),
                new("Descripción", p => p.Descripcion ?? "", width: 40),
                new("Categoría", p => p.CategoriaId.HasValue ? categorias.GetValueOrDefault(p.CategoriaId.Value, "") : "", width: 20),
                new("Impuesto", p => p.ImpuestoId.HasValue ? impuestos.GetValueOrDefault(p.ImpuestoId.Value, "") : "", width: 15),
                new("Unidad de medida", p => p.UnidadMedida ?? "", width: 18),
                new("Código de barras", p => p.CodigoBarras ?? "", width: 18),
                new("Precio compra", p => p.PrecioCompra, format: "#,##0.00", width: 15),
                new("Precio venta", p => p.PrecioVenta, format: "#,##0.00", width: 15),
                new("Precio mayorista", p => p.PrecioMayorista, format: "#,##0.00", width: 16),
                new("Stock", p => p.Stock, format: "#,##0.00", width: 12),
                new("Stock mínimo", p => p.StockMinimo, format: "#,##0.00", width: 14),
                new("Activo", p => p.Activo, width: 10),
                new("Fecha creación", p => p.FechaCreacion, format: "dd/MM/yyyy HH:mm", width: 20),
            };
        }

        // ============================================================
        // CLIENTES
        // ============================================================
        public static List<ExportColumn<Cliente>> Clientes()
        {
            return new List<ExportColumn<Cliente>>
            {
                new("Código", c => c.Codigo ?? "", width: 15),
                new("Nombre", c => c.Nombre, width: 35),
                new("Razón social", c => c.RazonSocial ?? "", width: 35),
                new("RTN", c => c.RTN ?? "", width: 18),
                new("Email", c => c.Email ?? "", width: 28),
                new("Teléfono", c => c.Telefono ?? "", width: 16),
                new("Dirección", c => c.Direccion ?? "", width: 40),
                new("Ciudad", c => c.Ciudad ?? "", width: 20),
                new("País", c => c.Pais ?? "Honduras", width: 15),
                new("Tipo cliente", c => c.TipoCliente ?? "Regular", width: 16),
                new("Días crédito", c => c.DiasCredito, width: 14),
                new("Límite crédito", c => c.LimiteCredito, format: "#,##0.00", width: 16),
                new("Notas", c => c.Notas ?? "", width: 30),
                new("Activo", c => c.Activo, width: 10),
                new("Fecha registro", c => c.FechaCreacion, format: "dd/MM/yyyy HH:mm", width: 20),
            };
        }

        // ============================================================
        // PROVEEDORES
        // ============================================================
        public static List<ExportColumn<Proveedor>> Proveedores()
        {
            return new List<ExportColumn<Proveedor>>
            {
                new("Código", p => p.Codigo ?? "", width: 15),
                new("Nombre", p => p.Nombre, width: 35),
                new("Razón social", p => p.RazonSocial ?? "", width: 35),
                new("RTN", p => p.RTN ?? "", width: 18),
                new("Contacto", p => p.Contacto ?? "", width: 25),
                new("Teléfono contacto", p => p.TelefonoContacto ?? "", width: 16),
                new("Teléfono principal", p => p.Telefono ?? "", width: 16),
                new("Email", p => p.Email ?? "", width: 28),
                new("Dirección", p => p.Direccion ?? "", width: 40),
                new("Ciudad", p => p.Ciudad ?? "", width: 20),
                new("País", p => p.Pais ?? "Honduras", width: 15),
                new("Condición pago", p => p.CondicionPago ?? "Contado", width: 16),
                new("Días crédito", p => p.DiasCredito, width: 14),
                new("Límite crédito", p => p.LimiteCredito, format: "#,##0.00", width: 16),
                new("Banco", p => p.Banco ?? "", width: 20),
                new("Cuenta bancaria", p => p.CuentaBancaria ?? "", width: 20),
                new("Activo", p => p.Activo, width: 10),
                new("Fecha creación", p => p.FechaCreacion, format: "dd/MM/yyyy HH:mm", width: 20),
            };
        }

        // ============================================================
        // CATEGORÍAS
        // ============================================================
        public static List<ExportColumn<Categoria>> Categorias()
        {
            return new List<ExportColumn<Categoria>>
            {
                new("Nombre",         c => c.Nombre,                width: 30),
                new("Descripción",    c => c.Descripcion ?? "",     width: 40),
                new("Color",          c => c.Color,                 width: 12),
                new("Activa",         c => c.Activa,                width: 10),
                new("Fecha creación", c => c.FechaCreacion, format: "dd/MM/yyyy HH:mm", width: 20),
            };
        }

        // ============================================================
        // IMPUESTOS
        // ============================================================
        public static List<ExportColumn<Impuesto>> Impuestos()
        {
            return new List<ExportColumn<Impuesto>>
            {
                new("Nombre",         i => i.Nombre,              width: 30),
                new("Porcentaje",     i => i.Porcentaje, format: "0.00", width: 14),
                new("Descripción",    i => i.Descripcion ?? "",   width: 40),
                new("Predeterminado", i => i.EsPredeterminado,    width: 15),
                new("Activo",         i => i.Activo,              width: 10),
            };
        }

        // ============================================================
        // UNIDADES DE MEDIDA
        // ============================================================
        public static List<ExportColumn<UnidadMedida>> UnidadesMedida()
        {
            return new List<ExportColumn<UnidadMedida>>
            {
                new("Nombre",         u => u.Nombre,                width: 25),
                new("Abreviatura",    u => u.Abreviatura,           width: 15),
                new("Descripción",    u => u.Descripcion ?? "",     width: 40),
                new("Activa",         u => u.Activa,                width: 10),
                new("Fecha creación", u => u.FechaCreacion, format: "dd/MM/yyyy HH:mm", width: 20),
            };
        }
    }
}
````

===== FILE: Helpers/Export/JsonExporter.cs =====

````csharp
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Kirkenta.Helpers.Export
{
    public static class JsonExporter
    {
        public static byte[] Export<T>(IEnumerable<T> items, bool indentado = true)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = indentado,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(items, options);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
````

====================================================
 Helpers\Import - 8 archivo(s)
====================================================

===== FILE: Helpers/Import/CampoMapeo.cs =====

````csharp
namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Define un campo mapeable del sistema (ej: "Nombre", "RTN", "PrecioVenta").
    /// El wizard de importación usa esto para generar la UI de mapeo.
    /// </summary>
    public class CampoMapeo
    {
        /// <summary>
        /// Clave interna del campo (sin espacios). Ej: "RazonSocial", "PrecioVenta".
        /// </summary>
        public string Key { get; set; } = "";

        /// <summary>
        /// Etiqueta visible al usuario. Ej: "Razón social", "Precio de venta".
        /// </summary>
        public string Label { get; set; } = "";

        /// <summary>
        /// Si es true, el usuario DEBE mapearlo (no puede ignorarlo).
        /// </summary>
        public bool Requerido { get; set; }

        /// <summary>
        /// Texto de ayuda opcional (se muestra debajo del selector).
        /// </summary>
        public string? Ayuda { get; set; }
    }
}
````

===== FILE: Helpers/Import/ColumnMapper.cs =====

````csharp
namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Auto-detecta el mapeo entre las columnas del archivo y los campos del sistema.
    /// Compara por nombre, sin importar mayúsculas, acentos ni espacios.
    /// </summary>
    public static class ColumnMapper
    {
        /// <summary>
        /// Dado un conjunto de headers del archivo y un diccionario de alias por campo,
        /// devuelve un diccionario: campo → header del archivo (o null si no encontró).
        /// </summary>
        public static Dictionary<string, string?> Detectar(
            List<string> headersArchivo,
            Dictionary<string, string[]> aliasPorCampo)
        {
            var mapa = new Dictionary<string, string?>();

            foreach (var campo in aliasPorCampo.Keys)
            {
                var aliases = aliasPorCampo[campo];
                var match = headersArchivo.FirstOrDefault(h =>
                    aliases.Any(a => Normalizar(h) == Normalizar(a)));

                // Si no hay match exacto, probar contains
                if (match == null)
                {
                    match = headersArchivo.FirstOrDefault(h =>
                        aliases.Any(a =>
                        {
                            var hn = Normalizar(h);
                            var an = Normalizar(a);
                            return hn.Contains(an) || an.Contains(hn);
                        }));
                }

                mapa[campo] = match;
            }

            return mapa;
        }

        /// <summary>
        /// Normaliza un texto: sin acentos, sin espacios, sin guiones, minúsculas.
        /// </summary>
        public static string Normalizar(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";

            var normalized = texto.Trim().ToLowerInvariant()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n")
                .Replace("ü", "u");

            normalized = new string(normalized.Where(c => char.IsLetterOrDigit(c)).ToArray());
            return normalized;
        }
    }
}
````

===== FILE: Helpers/Import/CsvImporter.cs =====

````csharp
using System.Text;

namespace Kirkenta.Helpers.Import
{
    public static class CsvImporter
    {
        public static ImportFile Read(Stream stream)
        {
            var result = new ImportFile();

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var contenido = reader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(contenido))
            {
                result.Error = "El archivo está vacío";
                return result;
            }

            var lineas = contenido.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (lineas.Count < 2)
            {
                result.Error = "El archivo debe tener al menos un encabezado y una fila de datos";
                return result;
            }

            // Detectar separador
            char separador = DetectarSeparador(lineas[0]);

            // Parsear headers
            var headers = ParseLine(lineas[0], separador);
            result.Headers = headers;

            // Parsear filas
            for (int i = 1; i < lineas.Count; i++)
            {
                var valores = ParseLine(lineas[i], separador);
                var rowDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (int c = 0; c < headers.Count; c++)
                {
                    rowDict[headers[c]] = c < valores.Count ? valores[c].Trim() : "";
                }

                result.Rows.Add(rowDict);
            }

            return result;
        }

        private static char DetectarSeparador(string lineaHeader)
        {
            var candidatos = new[] { ',', ';', '\t', '|' };
            var conteos = candidatos.Select(c => new { Sep = c, Count = lineaHeader.Count(x => x == c) }).ToList();
            return conteos.OrderByDescending(x => x.Count).First().Sep;
        }

        private static List<string> ParseLine(string linea, char separador)
        {
            var resultado = new List<string>();
            var actual = new StringBuilder();
            bool enComillas = false;

            for (int i = 0; i < linea.Length; i++)
            {
                char c = linea[i];

                if (c == '"')
                {
                    if (enComillas && i + 1 < linea.Length && linea[i + 1] == '"')
                    {
                        actual.Append('"');
                        i++;
                    }
                    else
                    {
                        enComillas = !enComillas;
                    }
                }
                else if (c == separador && !enComillas)
                {
                    resultado.Add(actual.ToString());
                    actual.Clear();
                }
                else
                {
                    actual.Append(c);
                }
            }

            resultado.Add(actual.ToString());
            return resultado;
        }
    }
}
````

===== FILE: Helpers/Import/ExcelImporter.cs =====

````csharp
using ClosedXML.Excel;

namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Lee un Excel (.xlsx) a una lista de diccionarios: cada item es una fila,
    /// cada clave es el nombre de la columna (header) y el valor es el texto.
    /// </summary>
    public static class ExcelImporter
    {
        public static ImportFile Read(Stream stream)
        {
            var result = new ImportFile();

            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.First();

            var usedRange = ws.RangeUsed();
            if (usedRange == null)
            {
                result.Error = "El archivo está vacío";
                return result;
            }

            var firstRow = usedRange.FirstRow();
            var lastRow = usedRange.LastRow();
            int headerRowNum = firstRow.RowNumber();
            int lastRowNum = lastRow.RowNumber();

            // Detectar headers: primera fila con al menos 1 celda no vacía
            int headerRow = headerRowNum;
            for (int r = headerRowNum; r <= Math.Min(headerRowNum + 5, lastRowNum); r++)
            {
                if (ws.Row(r).CellsUsed().Any())
                {
                    headerRow = r;
                    break;
                }
            }

            // Leer headers
            var headers = new List<string>();
            var headerCells = ws.Row(headerRow).CellsUsed().ToList();
            int maxCol = usedRange.LastColumn().ColumnNumber();

            for (int c = 1; c <= maxCol; c++)
            {
                var cellValue = ws.Cell(headerRow, c).GetString().Trim();
                headers.Add(string.IsNullOrEmpty(cellValue) ? $"Columna{c}" : cellValue);
            }

            if (headers.Count == 0 || headers.All(string.IsNullOrWhiteSpace))
            {
                result.Error = "No se encontraron encabezados en el archivo";
                return result;
            }

            result.Headers = headers;

            // Leer filas
            for (int r = headerRow + 1; r <= lastRowNum; r++)
            {
                var rowDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool tieneDatos = false;

                for (int c = 1; c <= maxCol; c++)
                {
                    var cell = ws.Cell(r, c);
                    var valor = cell.GetString()?.Trim() ?? "";
                    rowDict[headers[c - 1]] = valor;
                    if (!string.IsNullOrEmpty(valor)) tieneDatos = true;
                }

                if (tieneDatos)
                {
                    result.Rows.Add(rowDict);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Estructura común para archivos importados (Excel o CSV).
    /// </summary>
    public class ImportFile
    {
        public string? Error { get; set; }
        public List<string> Headers { get; set; } = new();
        public List<Dictionary<string, string>> Rows { get; set; } = new();

        public bool Ok => string.IsNullOrEmpty(Error);
    }
}
````

===== FILE: Helpers/Import/ImportResult.cs =====

````csharp
namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Resultado de una operación de importación.
    /// Acumula cuántos se crearon, actualizaron, y errores por fila.
    /// </summary>
    public class ImportResult
    {
        public int TotalFilas { get; set; }
        public int Creados { get; set; }
        public int Actualizados { get; set; }
        public int SinCambios { get; set; }
        public int Ignorados { get; set; }
        public List<ImportRowError> Errores { get; set; } = new();
        public List<ImportRowError> Advertencias { get; set; } = new();

        public bool Exitoso => Errores.Count == 0;
        public int TotalProcesados => Creados + Actualizados + SinCambios + Ignorados;

        public void AgregarError(int numeroFila, string mensaje)
        {
            Errores.Add(new ImportRowError { NumeroFila = numeroFila, Mensaje = mensaje });
        }

        public void AgregarAdvertencia(int numeroFila, string mensaje)
        {
            Advertencias.Add(new ImportRowError { NumeroFila = numeroFila, Mensaje = mensaje });
        }

        public string Resumen()
        {
            var partes = new List<string>();
            if (Creados > 0) partes.Add($"{Creados} creados");
            if (Actualizados > 0) partes.Add($"{Actualizados} actualizados");
            if (SinCambios > 0) partes.Add($"{SinCambios} sin cambios");
            if (Ignorados > 0) partes.Add($"{Ignorados} ignorados");
            if (Errores.Count > 0) partes.Add($"{Errores.Count} errores");
            if (Advertencias.Count > 0) partes.Add($"{Advertencias.Count} advertencias");

            return partes.Count > 0 ? string.Join(", ", partes) : "Sin cambios";
        }
    }

    public class ImportRowError
    {
        public int NumeroFila { get; set; }
        public string Mensaje { get; set; } = "";
    }
}
````

===== FILE: Helpers/Import/ImportWizardConfig.cs =====

````csharp
namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Configuración completa del wizard de importación de un módulo.
    /// Cada módulo (Clientes, Productos, Proveedores, etc.) define uno.
    /// </summary>
    public class ImportWizardConfig
    {
        /// <summary>
        /// Nombre del módulo en singular/plural para textos.
        /// Ej: "clientes", "productos", "proveedores".
        /// </summary>
        public string NombrePlural { get; set; } = "";

        /// <summary>
        /// Nombre en singular (para mensajes tipo "Importar 1 cliente").
        /// Ej: "cliente", "producto", "proveedor".
        /// </summary>
        public string NombreSingular { get; set; } = "";

        /// <summary>
        /// Módulo del sistema para permisos. Ej: "Ventas", "Inventario", "Compras".
        /// </summary>
        public string ModuloPermiso { get; set; } = "";

        /// <summary>
        /// Submódulo del sistema para permisos. Ej: "ClientesImport", "ProductosImport".
        /// </summary>
        public string SubmoduloPermiso { get; set; } = "";

        /// <summary>
        /// Clave única para la sesión. Ej: "Clientes", "Productos", "Proveedores".
        /// El helper internamente usa "Import{SessionKey}_Headers" y "_Rows".
        /// </summary>
        public string SessionKey { get; set; } = "";

        /// <summary>
        /// URL de la página Index del módulo (botón Cancelar y redirect final).
        /// Ej: "/Clientes/Index".
        /// </summary>
        public string UrlIndex { get; set; } = "";

        /// <summary>
        /// URL de la página Import (paso 1). Ej: "/Clientes/Import".
        /// </summary>
        public string UrlImport { get; set; } = "";

        /// <summary>
        /// URL de la página ImportMap (paso 2). Ej: "/Clientes/ImportMap".
        /// </summary>
        public string UrlImportMap { get; set; } = "";

        /// <summary>
        /// Lista de campos mapeables que verá el usuario.
        /// </summary>
        public List<CampoMapeo> Campos { get; set; } = new();

        /// <summary>
        /// Alias por campo, para autodetección de columnas.
        /// Key = campo.Key, Value = array de alias.
        /// </summary>
        public Dictionary<string, string[]> AliasCampos { get; set; } = new();

        /// <summary>
        /// Mapeo mínimo requerido para proceder con la importación.
        /// Ej: { "Nombre" }. Si alguno falta, se rechaza el POST.
        /// </summary>
        public string[] CamposRequeridos { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Si es true, se ofrece el checkbox de "Crear Categorías automáticamente"
        /// (usado solo por Productos).
        /// </summary>
        public bool OfrecerCrearCategorias { get; set; } = false;

        /// <summary>
        /// Si es true, se ofrece el checkbox de "Crear Impuestos automáticamente"
        /// (usado solo por Productos).
        /// </summary>
        public bool OfrecerCrearImpuestos { get; set; } = false;

        // ===== Helpers de sesión =====
        public string SessionHeadersKey => $"Import{SessionKey}_Headers";
        public string SessionRowsKey => $"Import{SessionKey}_Rows";

        // ===== Helpers de textos =====
        public string TituloPagina => $"Importar {NombrePlural}";
        public string TituloMapa => "Mapear columnas";
    }
}
````

===== FILE: Helpers/Import/ImportWizardHelper.cs =====

````csharp
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Lógica compartida del wizard de importación (paso 1: subir archivo).
    /// Cada módulo llama a estos métodos desde su propia página Import.
    /// </summary>
    public static class ImportWizardHelper
    {
        public const long TamanoMaximoBytes = 20 * 1024 * 1024; // 20 MB
        public const int MaxFilas = 5000;

        /// <summary>
        /// Resultado de procesar un archivo subido.
        /// </summary>
        public class ResultadoLectura
        {
            public bool Ok { get; set; }
            public string? Error { get; set; }
            public List<string> Headers { get; set; } = new();
            public List<Dictionary<string, string>> Rows { get; set; } = new();
            public int TotalFilas => Rows.Count;
        }

        /// <summary>
        /// Valida y lee un archivo subido (Excel o CSV).
        /// </summary>
        public static ResultadoLectura ProcesarArchivo(IFormFile? archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                return new ResultadoLectura { Ok = false, Error = "Debes seleccionar un archivo" };
            }

            if (archivo.Length > TamanoMaximoBytes)
            {
                return new ResultadoLectura { Ok = false, Error = "El archivo no puede pesar más de 20 MB" };
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".csv")
            {
                return new ResultadoLectura { Ok = false, Error = "Solo se permiten archivos Excel (.xlsx) o CSV (.csv)" };
            }

            ImportFile archivoLeido;
            try
            {
                using var stream = archivo.OpenReadStream();
                archivoLeido = extension == ".xlsx"
                    ? ExcelImporter.Read(stream)
                    : CsvImporter.Read(stream);
            }
            catch (Exception ex)
            {
                return new ResultadoLectura { Ok = false, Error = $"No se pudo leer el archivo: {ex.Message}" };
            }

            if (!archivoLeido.Ok)
            {
                return new ResultadoLectura { Ok = false, Error = archivoLeido.Error ?? "No se pudo leer el archivo" };
            }

            if (archivoLeido.Rows.Count == 0)
            {
                return new ResultadoLectura { Ok = false, Error = "El archivo no contiene filas de datos" };
            }

            if (archivoLeido.Rows.Count > MaxFilas)
            {
                return new ResultadoLectura
                {
                    Ok = false,
                    Error = $"El archivo tiene {archivoLeido.Rows.Count} filas. El máximo es {MaxFilas:N0} filas por importación."
                };
            }

            return new ResultadoLectura
            {
                Ok = true,
                Headers = archivoLeido.Headers,
                Rows = archivoLeido.Rows
            };
        }

        /// <summary>
        /// Guarda headers y filas en la sesión (como JSON).
        /// </summary>
        public static void GuardarEnSesion(ISession session, ImportWizardConfig config,
            List<string> headers, List<Dictionary<string, string>> rows)
        {
            session.SetString(config.SessionHeadersKey, JsonSerializer.Serialize(headers));
            session.SetString(config.SessionRowsKey, JsonSerializer.Serialize(rows));
        }

        /// <summary>
        /// Recupera headers y filas de la sesión.
        /// Devuelve null si no hay datos (sesión expirada).
        /// </summary>
        public static (List<string> Headers, List<Dictionary<string, string>> Rows)? RecuperarDeSesion(
            ISession session, ImportWizardConfig config)
        {
            var jsonHeaders = session.GetString(config.SessionHeadersKey);
            var jsonRows = session.GetString(config.SessionRowsKey);

            if (string.IsNullOrEmpty(jsonHeaders) || string.IsNullOrEmpty(jsonRows))
                return null;

            try
            {
                var headers = JsonSerializer.Deserialize<List<string>>(jsonHeaders) ?? new();
                var rows = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonRows) ?? new();
                return (headers, rows);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Limpia los datos del wizard en la sesión.
        /// </summary>
        public static void LimpiarSesion(ISession session, ImportWizardConfig config)
        {
            session.Remove(config.SessionHeadersKey);
            session.Remove(config.SessionRowsKey);
        }
    }
}
````

===== FILE: Helpers/Import/ParserHelper.cs =====

````csharp
using System.Globalization;

namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Utilidades de parseo robustas para valores de importación.
    /// Acepta múltiples formatos (con/sin comas de miles, con/sin símbolos).
    /// </summary>
    public static class ParserHelper
    {
        /// <summary>
        /// Parsea un bool. Acepta: 1/0, true/false, sí/si/no, yes/no, activo/inactivo.
        /// Devuelve null si el valor está vacío o no es reconocible.
        /// </summary>
        public static bool? ParsearBool(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var normalizado = valor.Trim().ToLowerInvariant();

            if (normalizado == "1" || normalizado == "true" || normalizado == "sí" ||
                normalizado == "si" || normalizado == "yes" || normalizado == "y" ||
                normalizado == "activo" || normalizado == "activa" || normalizado == "on")
                return true;

            if (normalizado == "0" || normalizado == "false" || normalizado == "no" ||
                normalizado == "n" || normalizado == "inactivo" || normalizado == "inactiva" ||
                normalizado == "off")
                return false;

            return null;
        }

        /// <summary>
        /// Parsea un int. Devuelve null si el valor está vacío o no es numérico.
        /// </summary>
        public static int? ParsearInt(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            return int.TryParse(valor.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;
        }

        /// <summary>
        /// Parsea un decimal. Limpia símbolos comunes: L., $, %, comas de miles.
        /// Intenta primero con InvariantCulture, luego con cultura local.
        /// </summary>
        public static decimal? ParsearDecimal(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            var limpio = valor.Trim()
                .Replace("L.", "")
                .Replace("l.", "")
                .Replace("$", "")
                .Replace("%", "")
                .Trim();

            // Si tiene coma Y punto, asumimos coma = miles, punto = decimal
            if (limpio.Contains(',') && limpio.Contains('.'))
            {
                limpio = limpio.Replace(",", "");
            }
            // Si solo tiene coma, podría ser decimal (formato europeo) o miles
            // Convención: si hay 2 dígitos después de la coma, es decimal. Si hay 3, es miles.
            else if (limpio.Contains(','))
            {
                var partes = limpio.Split(',');
                if (partes.Length == 2 && partes[1].Length == 2)
                {
                    limpio = limpio.Replace(",", ".");
                }
                else
                {
                    limpio = limpio.Replace(",", "");
                }
            }

            if (decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;

            // Fallback: cultura local
            if (decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                return result;

            return null;
        }
    }
}
````

====================================================
 Migrations - 5 archivo(s)
====================================================

===== FILE: Migrations/20260928033900_InitialCreate.cs =====

````csharp
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kirkenta.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rol = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}

````

===== FILE: Migrations/20260928033900_InitialCreate.Designer.cs =====

````csharp
// <auto-generated />
using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Kirkenta.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260928033900_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("Kirkenta.Models.Usuario", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Rol")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("Usuarios");
                });
#pragma warning restore 612, 618
        }
    }
}

````

===== FILE: Migrations/20261003044330_FinanzasFase1.cs =====

````csharp
using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kirkenta.Migrations
{
    /// <inheritdoc />
    public partial class FinanzasFase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Usuarios",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "BloqueadoHasta",
                table: "Usuarios",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Usuarios",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Usuarios",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "IntentosFallidos",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NombreCompleto",
                table: "Usuarios",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Usuarios",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoAcceso",
                table: "Usuarios",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Actividades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Detalle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ip = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actividades", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdjuntosCompras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrdenCompraId = table.Column<int>(type: "int", nullable: true),
                    PagoProveedorId = table.Column<int>(type: "int", nullable: true),
                    TipoDocumento = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombreArchivo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RutaArchivo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoArchivo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanoKB = table.Column<int>(type: "int", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdjuntosCompras", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BajasInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TipoBaja = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Motivo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValorUnitario = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioSolicitaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioApruebaId = table.Column<int>(type: "int", nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Revertida = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaReversion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UsuarioRevierteId = table.Column<int>(type: "int", nullable: true),
                    MotivoReversion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BajasInventario", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CategoriasFinancieras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CuentaContable = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EsSistema = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasFinancieras", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RazonSocial = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RTN = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Direccion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ciudad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Pais = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoCliente = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LimiteCredito = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DiasCredito = table.Column<int>(type: "int", nullable: false),
                    Notas = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConfiguracionEmpresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RazonSocial = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RTN = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Direccion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ciudad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Pais = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SitioWeb = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LogoPath = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CAI = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RangoInicial = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RangoFinal = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaLimiteEmision = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NotasFactura = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionEmpresa", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Cotizaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Impuestos = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Validez = table.Column<int>(type: "int", nullable: false),
                    FacturaId = table.Column<int>(type: "int", nullable: true),
                    FechaConversion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cotizaciones", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CuentasFinancieras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Subtipo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumeroCuenta = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Banco = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MonedaId = table.Column<int>(type: "int", nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    SaldoActual = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    LimiteCredito = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    CuentaContable = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Responsable = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasFinancieras", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetalleCotizaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CotizacionId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cantidad = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ImpuestoPorcentaje = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleCotizaciones", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetalleDevoluciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DevolucionId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleDevoluciones", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetalleDevolucionesProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DevolucionId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleDevolucionesProveedor", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetalleFacturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FacturaId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cantidad = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ImpuestoPorcentaje = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleFacturas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetalleOrdenesCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrdenCompraId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CantidadRecibida = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ImpuestoPorcentaje = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleOrdenesCompra", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetallePedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cantidad = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ImpuestoPorcentaje = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallePedidos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetalleVentas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cantidad = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ImpuestoPorcentaje = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleVentas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Devoluciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FacturaId = table.Column<int>(type: "int", nullable: true),
                    VentaId = table.Column<int>(type: "int", nullable: true),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Motivo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devoluciones", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DevolucionesProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrdenCompraId = table.Column<int>(type: "int", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Motivo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucionesProveedor", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Facturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    CotizacionId = table.Column<int>(type: "int", nullable: true),
                    PedidoId = table.Column<int>(type: "int", nullable: true),
                    VentaId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Impuestos = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CAI = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RangoInicial = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RangoFinal = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaLimiteEmision = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Impuestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Porcentaje = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EsPredeterminado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Impuestos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MetodosPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiereReferencia = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetodosPago", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Monedas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Simbolo = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoCambio = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    EsPredeterminada = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Monedas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MovimientosFinancieros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    CuentaDestinoId = table.Column<int>(type: "int", nullable: true),
                    CategoriaId = table.Column<int>(type: "int", nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MonedaId = table.Column<int>(type: "int", nullable: false),
                    TipoCambio = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Concepto = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Referencia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FormaPago = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Origen = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrigenId = table.Column<int>(type: "int", nullable: true),
                    EsAutomatico = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MotivoAnulacion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UsuarioAnuloId = table.Column<int>(type: "int", nullable: true),
                    Conciliado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaConciliacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConciliadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosFinancieros", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrdenesCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaEntregaEstimada = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaRecepcion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Subtotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Impuestos = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MonedaId = table.Column<int>(type: "int", nullable: false),
                    TipoCambio = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Notas = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true),
                    UsuarioRecibioId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesCompra", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FacturaId = table.Column<int>(type: "int", nullable: true),
                    VentaId = table.Column<int>(type: "int", nullable: true),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MetodoPagoId = table.Column<int>(type: "int", nullable: false),
                    Referencia = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PagosProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    OrdenCompraId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MetodoPagoId = table.Column<int>(type: "int", nullable: false),
                    Referencia = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MonedaId = table.Column<int>(type: "int", nullable: false),
                    TipoCambio = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Notas = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosProveedor", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    CotizacionId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaEntrega = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Impuestos = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FacturaId = table.Column<int>(type: "int", nullable: true),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    Modulo = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Submodulo = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PuedeVer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PuedeCrear = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PuedeEditar = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PuedeEliminar = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SKU = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodigoBarras = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CategoriaId = table.Column<int>(type: "int", nullable: true),
                    ImpuestoId = table.Column<int>(type: "int", nullable: true),
                    PrecioCompra = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PrecioMayorista = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Stock = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    StockMinimo = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    UnidadMedida = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Imagen = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RazonSocial = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RTN = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Contacto = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TelefonoContacto = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Direccion = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ciudad = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Pais = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CondicionPago = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DiasCredito = table.Column<int>(type: "int", nullable: false),
                    LimiteCredito = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Banco = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CuentaBancaria = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MonedaId = table.Column<int>(type: "int", nullable: false),
                    Notas = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EsSistema = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SeriesDocumentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Tipo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Prefijo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sufijo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Separador = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LongitudNumero = table.Column<int>(type: "int", nullable: false),
                    SiguienteNumero = table.Column<int>(type: "int", nullable: false),
                    FormatoPersonalizado = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EsPredeterminada = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesDocumentos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UnidadesMedida",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Abreviatura = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesMedida", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ValesDescuentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ValeEmpleadoId = table.Column<int>(type: "int", nullable: false),
                    NumeroCuota = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NominaId = table.Column<int>(type: "int", nullable: true),
                    Notas = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValesDescuentos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ValesEmpleado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaEntrega = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Motivo = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AprobadoPorGerenteId = table.Column<int>(type: "int", nullable: true),
                    FechaAprobacionGerente = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AprobadoPorRRHHId = table.Column<int>(type: "int", nullable: true),
                    FechaAprobacionRRHH = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EntregadoPorId = table.Column<int>(type: "int", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cuotas = table.Column<int>(type: "int", nullable: false),
                    MontoCuota = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    FechaPrimerDescuento = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SaldoPendiente = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CuentaId = table.Column<int>(type: "int", nullable: true),
                    MovimientoId = table.Column<int>(type: "int", nullable: true),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValesEmpleado", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    CotizacionId = table.Column<int>(type: "int", nullable: true),
                    PedidoId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Impuestos = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notas = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioCreoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BajasInventario_Numero",
                table: "BajasInventario",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Codigo",
                table: "Clientes",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cotizaciones_Numero",
                table: "Cotizaciones",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuentasFinancieras_Codigo",
                table: "CuentasFinancieras",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_Numero",
                table: "Devoluciones",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesProveedor_Numero",
                table: "DevolucionesProveedor",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_Numero",
                table: "Facturas",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Monedas_Codigo",
                table: "Monedas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosFinancieros_Numero",
                table: "MovimientosFinancieros",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_Numero",
                table: "OrdenesCompra",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_Numero",
                table: "Pedidos",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_RolId_Modulo_Submodulo",
                table: "Permisos",
                columns: new[] { "RolId", "Modulo", "Submodulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_SKU",
                table: "Productos",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Codigo",
                table: "Proveedores",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesMedida_Nombre",
                table: "UnidadesMedida",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValesEmpleado_Numero",
                table: "ValesEmpleado",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Numero",
                table: "Ventas",
                column: "Numero",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Actividades");

            migrationBuilder.DropTable(
                name: "AdjuntosCompras");

            migrationBuilder.DropTable(
                name: "BajasInventario");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "CategoriasFinancieras");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "ConfiguracionEmpresa");

            migrationBuilder.DropTable(
                name: "Cotizaciones");

            migrationBuilder.DropTable(
                name: "CuentasFinancieras");

            migrationBuilder.DropTable(
                name: "DetalleCotizaciones");

            migrationBuilder.DropTable(
                name: "DetalleDevoluciones");

            migrationBuilder.DropTable(
                name: "DetalleDevolucionesProveedor");

            migrationBuilder.DropTable(
                name: "DetalleFacturas");

            migrationBuilder.DropTable(
                name: "DetalleOrdenesCompra");

            migrationBuilder.DropTable(
                name: "DetallePedidos");

            migrationBuilder.DropTable(
                name: "DetalleVentas");

            migrationBuilder.DropTable(
                name: "Devoluciones");

            migrationBuilder.DropTable(
                name: "DevolucionesProveedor");

            migrationBuilder.DropTable(
                name: "Facturas");

            migrationBuilder.DropTable(
                name: "Impuestos");

            migrationBuilder.DropTable(
                name: "MetodosPago");

            migrationBuilder.DropTable(
                name: "Monedas");

            migrationBuilder.DropTable(
                name: "MovimientosFinancieros");

            migrationBuilder.DropTable(
                name: "OrdenesCompra");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "PagosProveedor");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "SeriesDocumentos");

            migrationBuilder.DropTable(
                name: "UnidadesMedida");

            migrationBuilder.DropTable(
                name: "ValesDescuentos");

            migrationBuilder.DropTable(
                name: "ValesEmpleado");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "BloqueadoHasta",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "IntentosFallidos",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "NombreCompleto",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "UltimoAcceso",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Usuarios",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

````

===== FILE: Migrations/20261003044330_FinanzasFase1.Designer.cs =====

````csharp
// <auto-generated />
using System;
using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Kirkenta.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261003044330_FinanzasFase1")]
    partial class FinanzasFase1
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("Kirkenta.Models.ActividadUsuario", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Accion")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Detalle")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Ip")
                        .HasColumnType("longtext");

                    b.Property<string>("UserAgent")
                        .HasColumnType("longtext");

                    b.Property<int>("UsuarioId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Actividades");
                });

            modelBuilder.Entity("Kirkenta.Models.AdjuntoCompra", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("FechaSubida")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("NombreArchivo")
                        .HasColumnType("longtext");

                    b.Property<int?>("OrdenCompraId")
                        .HasColumnType("int");

                    b.Property<int?>("PagoProveedorId")
                        .HasColumnType("int");

                    b.Property<string>("RutaArchivo")
                        .HasColumnType("longtext");

                    b.Property<int>("TamanoKB")
                        .HasColumnType("int");

                    b.Property<string>("TipoArchivo")
                        .HasColumnType("longtext");

                    b.Property<string>("TipoDocumento")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("UsuarioId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("AdjuntosCompras");
                });

            modelBuilder.Entity("Kirkenta.Models.BajaInventario", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaAprobacion")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaReversion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Motivo")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("MotivoRechazo")
                        .HasColumnType("longtext");

                    b.Property<string>("MotivoReversion")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<bool>("Revertida")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("TipoBaja")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("UsuarioApruebaId")
                        .HasColumnType("int");

                    b.Property<int?>("UsuarioRevierteId")
                        .HasColumnType("int");

                    b.Property<int>("UsuarioSolicitaId")
                        .HasColumnType("int");

                    b.Property<decimal>("ValorTotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("ValorUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("BajasInventario");
                });

            modelBuilder.Entity("Kirkenta.Models.Categoria", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activa")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Color")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.HasKey("Id");

                    b.ToTable("Categorias");
                });

            modelBuilder.Entity("Kirkenta.Models.CategoriaFinanciera", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activa")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Color")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("CuentaContable")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<bool>("EsSistema")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Tipo")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.HasKey("Id");

                    b.ToTable("CategoriasFinancieras");
                });

            modelBuilder.Entity("Kirkenta.Models.Cliente", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Ciudad")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Codigo")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<int>("DiasCredito")
                        .HasColumnType("int");

                    b.Property<string>("Direccion")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<string>("Email")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("LimiteCredito")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Pais")
                        .HasMaxLength(80)
                        .HasColumnType("varchar(80)");

                    b.Property<string>("RTN")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("RazonSocial")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("Telefono")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("TipoCliente")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Codigo")
                        .IsUnique();

                    b.ToTable("Clientes");
                });

            modelBuilder.Entity("Kirkenta.Models.ConfiguracionEmpresa", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("CAI")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Ciudad")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Direccion")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<string>("Email")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<DateTime>("FechaActualizacion")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaLimiteEmision")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("LogoPath")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("NotasFactura")
                        .HasColumnType("longtext");

                    b.Property<string>("Pais")
                        .HasMaxLength(80)
                        .HasColumnType("varchar(80)");

                    b.Property<string>("RTN")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("RangoFinal")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("RangoInicial")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("RazonSocial")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("SitioWeb")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("Telefono")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.HasKey("Id");

                    b.ToTable("ConfiguracionEmpresa");
                });

            modelBuilder.Entity("Kirkenta.Models.Cotizacion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ClienteId")
                        .HasColumnType("int");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("FacturaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaConversion")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaVencimiento")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Impuestos")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.Property<int>("Validez")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("Cotizaciones");
                });

            modelBuilder.Entity("Kirkenta.Models.CuentaFinanciera", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activa")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Banco")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Codigo")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("CuentaContable")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal?>("LimiteCredito")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("MonedaId")
                        .HasColumnType("int");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("NumeroCuenta")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Responsable")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<decimal>("SaldoActual")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("SaldoInicial")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Subtipo")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("Tipo")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Codigo")
                        .IsUnique();

                    b.ToTable("CuentasFinancieras");
                });

            modelBuilder.Entity("Kirkenta.Models.DetalleCotizacion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("CotizacionId")
                        .HasColumnType("int");

                    b.Property<string>("Descripcion")
                        .HasColumnType("longtext");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("ImpuestoPorcentaje")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("DetalleCotizaciones");
                });

            modelBuilder.Entity("Kirkenta.Models.DetalleDevolucion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("DevolucionId")
                        .HasColumnType("int");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("DetalleDevoluciones");
                });

            modelBuilder.Entity("Kirkenta.Models.DetalleDevolucionProveedor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("DevolucionId")
                        .HasColumnType("int");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("DetalleDevolucionesProveedor");
                });

            modelBuilder.Entity("Kirkenta.Models.DetalleFactura", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Descripcion")
                        .HasColumnType("longtext");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("FacturaId")
                        .HasColumnType("int");

                    b.Property<decimal>("ImpuestoPorcentaje")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("DetalleFacturas");
                });

            modelBuilder.Entity("Kirkenta.Models.DetalleOrdenCompra", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("CantidadRecibida")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("ImpuestoPorcentaje")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("OrdenCompraId")
                        .HasColumnType("int");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("DetalleOrdenesCompra");
                });

            modelBuilder.Entity("Kirkenta.Models.DetallePedido", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Descripcion")
                        .HasColumnType("longtext");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("ImpuestoPorcentaje")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("PedidoId")
                        .HasColumnType("int");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("DetallePedidos");
                });

            modelBuilder.Entity("Kirkenta.Models.DetalleVenta", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Descripcion")
                        .HasColumnType("longtext");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("ImpuestoPorcentaje")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("VentaId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("DetalleVentas");
                });

            modelBuilder.Entity("Kirkenta.Models.Devolucion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ClienteId")
                        .HasColumnType("int");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("FacturaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Motivo")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.Property<int?>("VentaId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("Devoluciones");
                });

            modelBuilder.Entity("Kirkenta.Models.DevolucionProveedor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Motivo")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int>("OrdenCompraId")
                        .HasColumnType("int");

                    b.Property<int>("ProveedorId")
                        .HasColumnType("int");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("DevolucionesProveedor");
                });

            modelBuilder.Entity("Kirkenta.Models.Factura", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("CAI")
                        .HasColumnType("longtext");

                    b.Property<int>("ClienteId")
                        .HasColumnType("int");

                    b.Property<int?>("CotizacionId")
                        .HasColumnType("int");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaLimiteEmision")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaVencimiento")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Impuestos")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int?>("PedidoId")
                        .HasColumnType("int");

                    b.Property<string>("RangoFinal")
                        .HasColumnType("longtext");

                    b.Property<string>("RangoInicial")
                        .HasColumnType("longtext");

                    b.Property<decimal>("Saldo")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.Property<int?>("VentaId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("Facturas");
                });

            modelBuilder.Entity("Kirkenta.Models.Impuesto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<bool>("EsPredeterminado")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<decimal>("Porcentaje")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("Impuestos");
                });

            modelBuilder.Entity("Kirkenta.Models.MetodoPago", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("varchar(80)");

                    b.Property<bool>("RequiereReferencia")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Tipo")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.HasKey("Id");

                    b.ToTable("MetodosPago");
                });

            modelBuilder.Entity("Kirkenta.Models.Moneda", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activa")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Codigo")
                        .IsRequired()
                        .HasMaxLength(3)
                        .HasColumnType("varchar(3)");

                    b.Property<bool>("EsPredeterminada")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("FechaActualizacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Simbolo")
                        .HasMaxLength(5)
                        .HasColumnType("varchar(5)");

                    b.Property<decimal>("TipoCambio")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.HasIndex("Codigo")
                        .IsUnique();

                    b.ToTable("Monedas");
                });

            modelBuilder.Entity("Kirkenta.Models.MovimientoFinanciero", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("CategoriaId")
                        .HasColumnType("int");

                    b.Property<string>("Concepto")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<bool>("Conciliado")
                        .HasColumnType("tinyint(1)");

                    b.Property<int?>("ConciliadoPorId")
                        .HasColumnType("int");

                    b.Property<int?>("CuentaDestinoId")
                        .HasColumnType("int");

                    b.Property<int>("CuentaId")
                        .HasColumnType("int");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<bool>("EsAutomatico")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaAnulacion")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaConciliacion")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("FormaPago")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<int>("MonedaId")
                        .HasColumnType("int");

                    b.Property<decimal>("Monto")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("MotivoAnulacion")
                        .HasColumnType("longtext");

                    b.Property<string>("Notas")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("Origen")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<int?>("OrigenId")
                        .HasColumnType("int");

                    b.Property<string>("Referencia")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Tipo")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<decimal>("TipoCambio")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioAnuloId")
                        .HasColumnType("int");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("MovimientosFinancieros");
                });

            modelBuilder.Entity("Kirkenta.Models.OrdenCompra", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaEntregaEstimada")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaRecepcion")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Impuestos")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("MonedaId")
                        .HasColumnType("int");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int>("ProveedorId")
                        .HasColumnType("int");

                    b.Property<decimal>("Saldo")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("TipoCambio")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.Property<int?>("UsuarioRecibioId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("OrdenesCompra");
                });

            modelBuilder.Entity("Kirkenta.Models.Pago", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ClienteId")
                        .HasColumnType("int");

                    b.Property<int?>("FacturaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("MetodoPagoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Monto")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Referencia")
                        .HasColumnType("longtext");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.Property<int?>("VentaId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Pagos");
                });

            modelBuilder.Entity("Kirkenta.Models.PagoProveedor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("MetodoPagoId")
                        .HasColumnType("int");

                    b.Property<int>("MonedaId")
                        .HasColumnType("int");

                    b.Property<decimal>("Monto")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .HasColumnType("longtext");

                    b.Property<int?>("OrdenCompraId")
                        .HasColumnType("int");

                    b.Property<int>("ProveedorId")
                        .HasColumnType("int");

                    b.Property<string>("Referencia")
                        .HasColumnType("longtext");

                    b.Property<decimal>("TipoCambio")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("PagosProveedor");
                });

            modelBuilder.Entity("Kirkenta.Models.Pedido", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ClienteId")
                        .HasColumnType("int");

                    b.Property<int?>("CotizacionId")
                        .HasColumnType("int");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("FacturaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaEntrega")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Impuestos")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("Pedidos");
                });

            modelBuilder.Entity("Kirkenta.Models.Permiso", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Modulo")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<bool>("PuedeCrear")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("PuedeEditar")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("PuedeEliminar")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("PuedeVer")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("RolId")
                        .HasColumnType("int");

                    b.Property<string>("Submodulo")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("RolId", "Modulo", "Submodulo")
                        .IsUnique();

                    b.ToTable("Permisos");
                });

            modelBuilder.Entity("Kirkenta.Models.Producto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .HasColumnType("tinyint(1)");

                    b.Property<int?>("CategoriaId")
                        .HasColumnType("int");

                    b.Property<string>("CodigoBarras")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Descripcion")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Imagen")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<int?>("ImpuestoId")
                        .HasColumnType("int");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<decimal>("PrecioCompra")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("PrecioMayorista")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("PrecioVenta")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("SKU")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<decimal>("Stock")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("StockMinimo")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("UnidadMedida")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.HasKey("Id");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.ToTable("Productos");
                });

            modelBuilder.Entity("Kirkenta.Models.Proveedor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Banco")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Ciudad")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Codigo")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("CondicionPago")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Contacto")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("CuentaBancaria")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<int>("DiasCredito")
                        .HasColumnType("int");

                    b.Property<string>("Direccion")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<string>("Email")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("LimiteCredito")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("MonedaId")
                        .HasColumnType("int");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Pais")
                        .HasMaxLength(80)
                        .HasColumnType("varchar(80)");

                    b.Property<string>("RTN")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("RazonSocial")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("Telefono")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("TelefonoContacto")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Codigo")
                        .IsUnique();

                    b.ToTable("Proveedores");
                });

            modelBuilder.Entity("Kirkenta.Models.Rol", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Color")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<bool>("EsSistema")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.HasKey("Id");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("Kirkenta.Models.SerieDocumento", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activa")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("EsPredeterminada")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("FormatoPersonalizado")
                        .HasColumnType("longtext");

                    b.Property<int>("LongitudNumero")
                        .HasColumnType("int");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Prefijo")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Separador")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("SiguienteNumero")
                        .HasColumnType("int");

                    b.Property<string>("Sufijo")
                        .HasColumnType("longtext");

                    b.Property<string>("Tipo")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("SeriesDocumentos");
                });

            modelBuilder.Entity("Kirkenta.Models.UnidadMedida", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Abreviatura")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("varchar(10)");

                    b.Property<bool>("Activa")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("Nombre")
                        .IsUnique();

                    b.ToTable("UnidadesMedida");
                });

            modelBuilder.Entity("Kirkenta.Models.Usuario", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime?>("BloqueadoHasta")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("IntentosFallidos")
                        .HasColumnType("int");

                    b.Property<string>("NombreCompleto")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Rol")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Telefono")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<DateTime?>("UltimoAcceso")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.HasKey("Id");

                    b.ToTable("Usuarios");
                });

            modelBuilder.Entity("Kirkenta.Models.ValeDescuento", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Monto")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("NominaId")
                        .HasColumnType("int");

                    b.Property<string>("Notas")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<int>("NumeroCuota")
                        .HasColumnType("int");

                    b.Property<int>("ValeEmpleadoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("ValesDescuentos");
                });

            modelBuilder.Entity("Kirkenta.Models.ValeEmpleado", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("AprobadoPorGerenteId")
                        .HasColumnType("int");

                    b.Property<int?>("AprobadoPorRRHHId")
                        .HasColumnType("int");

                    b.Property<int?>("CuentaId")
                        .HasColumnType("int");

                    b.Property<int>("Cuotas")
                        .HasColumnType("int");

                    b.Property<int>("EmpleadoId")
                        .HasColumnType("int");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<int?>("EntregadoPorId")
                        .HasColumnType("int");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<DateTime?>("FechaAprobacionGerente")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaAprobacionRRHH")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaEntrega")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaPrimerDescuento")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("FechaSolicitud")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Monto")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("MontoCuota")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Motivo")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<string>("MotivoRechazo")
                        .HasColumnType("longtext");

                    b.Property<int?>("MovimientoId")
                        .HasColumnType("int");

                    b.Property<string>("Notas")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<decimal>("SaldoPendiente")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("ValesEmpleado");
                });

            modelBuilder.Entity("Kirkenta.Models.Venta", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("ClienteId")
                        .HasColumnType("int");

                    b.Property<int?>("CotizacionId")
                        .HasColumnType("int");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Impuestos")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int?>("PedidoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("Ventas");
                });
#pragma warning restore 612, 618
        }
    }
}

````

===== FILE: Migrations/ApplicationDbContextModelSnapshot.cs =====

````csharp
// <auto-generated />
using System;
using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Kirkenta.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("Kirkenta.Models.ActividadUsuario", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Accion")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Detalle")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Ip")
                        .HasColumnType("longtext");

                    b.Property<string>("UserAgent")
                        .HasColumnType("longtext");

                    b.Property<int>("UsuarioId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Actividades");
                });

            modelBuilder.Entity("Kirkenta.Models.AdjuntoCompra", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("FechaSubida")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("NombreArchivo")
                        .HasColumnType("longtext");

                    b.Property<int?>("OrdenCompraId")
                        .HasColumnType("int");

                    b.Property<int?>("PagoProveedorId")
                        .HasColumnType("int");

                    b.Property<string>("RutaArchivo")
                        .HasColumnType("longtext");

                    b.Property<int>("TamanoKB")
                        .HasColumnType("int");

                    b.Property<string>("TipoArchivo")
                        .HasColumnType("longtext");

                    b.Property<string>("TipoDocumento")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("UsuarioId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("AdjuntosCompras");
                });

            modelBuilder.Entity("Kirkenta.Models.BajaInventario", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaAprobacion")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaReversion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Motivo")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("MotivoRechazo")
                        .HasColumnType("longtext");

                    b.Property<string>("MotivoReversion")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<bool>("Revertida")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("TipoBaja")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("UsuarioApruebaId")
                        .HasColumnType("int");

                    b.Property<int?>("UsuarioRevierteId")
                        .HasColumnType("int");

                    b.Property<int>("UsuarioSolicitaId")
                        .HasColumnType("int");

                    b.Property<decimal>("ValorTotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("ValorUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("BajasInventario");
                });

            modelBuilder.Entity("Kirkenta.Models.Categoria", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activa")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Color")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.HasKey("Id");

                    b.ToTable("Categorias");
                });

            modelBuilder.Entity("Kirkenta.Models.CategoriaFinanciera", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activa")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Color")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("CuentaContable")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<bool>("EsSistema")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Tipo")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.HasKey("Id");

                    b.ToTable("CategoriasFinancieras");
                });

            modelBuilder.Entity("Kirkenta.Models.Cliente", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Ciudad")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Codigo")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<int>("DiasCredito")
                        .HasColumnType("int");

                    b.Property<string>("Direccion")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<string>("Email")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("LimiteCredito")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Pais")
                        .HasMaxLength(80)
                        .HasColumnType("varchar(80)");

                    b.Property<string>("RTN")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("RazonSocial")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("Telefono")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("TipoCliente")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Codigo")
                        .IsUnique();

                    b.ToTable("Clientes");
                });

            modelBuilder.Entity("Kirkenta.Models.ConfiguracionEmpresa", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("CAI")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Ciudad")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Direccion")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<string>("Email")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<DateTime>("FechaActualizacion")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaLimiteEmision")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("LogoPath")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("NotasFactura")
                        .HasColumnType("longtext");

                    b.Property<string>("Pais")
                        .HasMaxLength(80)
                        .HasColumnType("varchar(80)");

                    b.Property<string>("RTN")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("RangoFinal")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("RangoInicial")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("RazonSocial")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("SitioWeb")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("Telefono")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.HasKey("Id");

                    b.ToTable("ConfiguracionEmpresa");
                });

            modelBuilder.Entity("Kirkenta.Models.Cotizacion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ClienteId")
                        .HasColumnType("int");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("FacturaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaConversion")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaVencimiento")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Impuestos")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.Property<int>("Validez")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("Cotizaciones");
                });

            modelBuilder.Entity("Kirkenta.Models.CuentaFinanciera", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activa")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Banco")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Codigo")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("CuentaContable")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal?>("LimiteCredito")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("MonedaId")
                        .HasColumnType("int");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("NumeroCuenta")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Responsable")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<decimal>("SaldoActual")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("SaldoInicial")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Subtipo")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("Tipo")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Codigo")
                        .IsUnique();

                    b.ToTable("CuentasFinancieras");
                });

            modelBuilder.Entity("Kirkenta.Models.DetalleCotizacion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("CotizacionId")
                        .HasColumnType("int");

                    b.Property<string>("Descripcion")
                        .HasColumnType("longtext");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("ImpuestoPorcentaje")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("DetalleCotizaciones");
                });

            modelBuilder.Entity("Kirkenta.Models.DetalleDevolucion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("DevolucionId")
                        .HasColumnType("int");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("DetalleDevoluciones");
                });

            modelBuilder.Entity("Kirkenta.Models.DetalleDevolucionProveedor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("DevolucionId")
                        .HasColumnType("int");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("DetalleDevolucionesProveedor");
                });

            modelBuilder.Entity("Kirkenta.Models.DetalleFactura", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Descripcion")
                        .HasColumnType("longtext");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("FacturaId")
                        .HasColumnType("int");

                    b.Property<decimal>("ImpuestoPorcentaje")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("DetalleFacturas");
                });

            modelBuilder.Entity("Kirkenta.Models.DetalleOrdenCompra", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("CantidadRecibida")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("ImpuestoPorcentaje")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("OrdenCompraId")
                        .HasColumnType("int");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("DetalleOrdenesCompra");
                });

            modelBuilder.Entity("Kirkenta.Models.DetallePedido", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Descripcion")
                        .HasColumnType("longtext");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("ImpuestoPorcentaje")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("PedidoId")
                        .HasColumnType("int");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("DetallePedidos");
                });

            modelBuilder.Entity("Kirkenta.Models.DetalleVenta", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Descripcion")
                        .HasColumnType("longtext");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("ImpuestoPorcentaje")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("PrecioUnitario")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("ProductoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("VentaId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("DetalleVentas");
                });

            modelBuilder.Entity("Kirkenta.Models.Devolucion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ClienteId")
                        .HasColumnType("int");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("FacturaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Motivo")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.Property<int?>("VentaId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("Devoluciones");
                });

            modelBuilder.Entity("Kirkenta.Models.DevolucionProveedor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Motivo")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int>("OrdenCompraId")
                        .HasColumnType("int");

                    b.Property<int>("ProveedorId")
                        .HasColumnType("int");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("DevolucionesProveedor");
                });

            modelBuilder.Entity("Kirkenta.Models.Factura", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("CAI")
                        .HasColumnType("longtext");

                    b.Property<int>("ClienteId")
                        .HasColumnType("int");

                    b.Property<int?>("CotizacionId")
                        .HasColumnType("int");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaLimiteEmision")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaVencimiento")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Impuestos")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int?>("PedidoId")
                        .HasColumnType("int");

                    b.Property<string>("RangoFinal")
                        .HasColumnType("longtext");

                    b.Property<string>("RangoInicial")
                        .HasColumnType("longtext");

                    b.Property<decimal>("Saldo")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.Property<int?>("VentaId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("Facturas");
                });

            modelBuilder.Entity("Kirkenta.Models.Impuesto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<bool>("EsPredeterminado")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<decimal>("Porcentaje")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("Impuestos");
                });

            modelBuilder.Entity("Kirkenta.Models.MetodoPago", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("varchar(80)");

                    b.Property<bool>("RequiereReferencia")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Tipo")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.HasKey("Id");

                    b.ToTable("MetodosPago");
                });

            modelBuilder.Entity("Kirkenta.Models.Moneda", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activa")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Codigo")
                        .IsRequired()
                        .HasMaxLength(3)
                        .HasColumnType("varchar(3)");

                    b.Property<bool>("EsPredeterminada")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("FechaActualizacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Simbolo")
                        .HasMaxLength(5)
                        .HasColumnType("varchar(5)");

                    b.Property<decimal>("TipoCambio")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.HasIndex("Codigo")
                        .IsUnique();

                    b.ToTable("Monedas");
                });

            modelBuilder.Entity("Kirkenta.Models.MovimientoFinanciero", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("CategoriaId")
                        .HasColumnType("int");

                    b.Property<string>("Concepto")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<bool>("Conciliado")
                        .HasColumnType("tinyint(1)");

                    b.Property<int?>("ConciliadoPorId")
                        .HasColumnType("int");

                    b.Property<int?>("CuentaDestinoId")
                        .HasColumnType("int");

                    b.Property<int>("CuentaId")
                        .HasColumnType("int");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<bool>("EsAutomatico")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaAnulacion")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaConciliacion")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("FormaPago")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<int>("MonedaId")
                        .HasColumnType("int");

                    b.Property<decimal>("Monto")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("MotivoAnulacion")
                        .HasColumnType("longtext");

                    b.Property<string>("Notas")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("Origen")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<int?>("OrigenId")
                        .HasColumnType("int");

                    b.Property<string>("Referencia")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Tipo")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<decimal>("TipoCambio")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioAnuloId")
                        .HasColumnType("int");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("MovimientosFinancieros");
                });

            modelBuilder.Entity("Kirkenta.Models.OrdenCompra", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaEntregaEstimada")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaRecepcion")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Impuestos")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("MonedaId")
                        .HasColumnType("int");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int>("ProveedorId")
                        .HasColumnType("int");

                    b.Property<decimal>("Saldo")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("TipoCambio")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.Property<int?>("UsuarioRecibioId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("OrdenesCompra");
                });

            modelBuilder.Entity("Kirkenta.Models.Pago", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ClienteId")
                        .HasColumnType("int");

                    b.Property<int?>("FacturaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("MetodoPagoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Monto")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Referencia")
                        .HasColumnType("longtext");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.Property<int?>("VentaId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Pagos");
                });

            modelBuilder.Entity("Kirkenta.Models.PagoProveedor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("MetodoPagoId")
                        .HasColumnType("int");

                    b.Property<int>("MonedaId")
                        .HasColumnType("int");

                    b.Property<decimal>("Monto")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .HasColumnType("longtext");

                    b.Property<int?>("OrdenCompraId")
                        .HasColumnType("int");

                    b.Property<int>("ProveedorId")
                        .HasColumnType("int");

                    b.Property<string>("Referencia")
                        .HasColumnType("longtext");

                    b.Property<decimal>("TipoCambio")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("PagosProveedor");
                });

            modelBuilder.Entity("Kirkenta.Models.Pedido", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ClienteId")
                        .HasColumnType("int");

                    b.Property<int?>("CotizacionId")
                        .HasColumnType("int");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int?>("FacturaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaEntrega")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Impuestos")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("Pedidos");
                });

            modelBuilder.Entity("Kirkenta.Models.Permiso", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Modulo")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<bool>("PuedeCrear")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("PuedeEditar")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("PuedeEliminar")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("PuedeVer")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("RolId")
                        .HasColumnType("int");

                    b.Property<string>("Submodulo")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("RolId", "Modulo", "Submodulo")
                        .IsUnique();

                    b.ToTable("Permisos");
                });

            modelBuilder.Entity("Kirkenta.Models.Producto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .HasColumnType("tinyint(1)");

                    b.Property<int?>("CategoriaId")
                        .HasColumnType("int");

                    b.Property<string>("CodigoBarras")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Descripcion")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Imagen")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<int?>("ImpuestoId")
                        .HasColumnType("int");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<decimal>("PrecioCompra")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("PrecioMayorista")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("PrecioVenta")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("SKU")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<decimal>("Stock")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("StockMinimo")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("UnidadMedida")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.HasKey("Id");

                    b.HasIndex("SKU")
                        .IsUnique();

                    b.ToTable("Productos");
                });

            modelBuilder.Entity("Kirkenta.Models.Proveedor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Banco")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Ciudad")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Codigo")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("CondicionPago")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Contacto")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("CuentaBancaria")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<int>("DiasCredito")
                        .HasColumnType("int");

                    b.Property<string>("Direccion")
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<string>("Email")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("LimiteCredito")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("MonedaId")
                        .HasColumnType("int");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Pais")
                        .HasMaxLength(80)
                        .HasColumnType("varchar(80)");

                    b.Property<string>("RTN")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("RazonSocial")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("Telefono")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("TelefonoContacto")
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Codigo")
                        .IsUnique();

                    b.ToTable("Proveedores");
                });

            modelBuilder.Entity("Kirkenta.Models.Rol", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Color")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<bool>("EsSistema")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.HasKey("Id");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("Kirkenta.Models.SerieDocumento", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activa")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("EsPredeterminada")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("FormatoPersonalizado")
                        .HasColumnType("longtext");

                    b.Property<int>("LongitudNumero")
                        .HasColumnType("int");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Prefijo")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Separador")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("SiguienteNumero")
                        .HasColumnType("int");

                    b.Property<string>("Sufijo")
                        .HasColumnType("longtext");

                    b.Property<string>("Tipo")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("SeriesDocumentos");
                });

            modelBuilder.Entity("Kirkenta.Models.UnidadMedida", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Abreviatura")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("varchar(10)");

                    b.Property<bool>("Activa")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Descripcion")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("Nombre")
                        .IsUnique();

                    b.ToTable("UnidadesMedida");
                });

            modelBuilder.Entity("Kirkenta.Models.Usuario", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime?>("BloqueadoHasta")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("IntentosFallidos")
                        .HasColumnType("int");

                    b.Property<string>("NombreCompleto")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Rol")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Telefono")
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<DateTime?>("UltimoAcceso")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.HasKey("Id");

                    b.ToTable("Usuarios");
                });

            modelBuilder.Entity("Kirkenta.Models.ValeDescuento", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Monto")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("NominaId")
                        .HasColumnType("int");

                    b.Property<string>("Notas")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<int>("NumeroCuota")
                        .HasColumnType("int");

                    b.Property<int>("ValeEmpleadoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("ValesDescuentos");
                });

            modelBuilder.Entity("Kirkenta.Models.ValeEmpleado", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("AprobadoPorGerenteId")
                        .HasColumnType("int");

                    b.Property<int?>("AprobadoPorRRHHId")
                        .HasColumnType("int");

                    b.Property<int?>("CuentaId")
                        .HasColumnType("int");

                    b.Property<int>("Cuotas")
                        .HasColumnType("int");

                    b.Property<int>("EmpleadoId")
                        .HasColumnType("int");

                    b.Property<int>("EmpresaId")
                        .HasColumnType("int");

                    b.Property<int?>("EntregadoPorId")
                        .HasColumnType("int");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<DateTime?>("FechaAprobacionGerente")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaAprobacionRRHH")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("FechaCreacion")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaEntrega")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("FechaPrimerDescuento")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("FechaSolicitud")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Monto")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("MontoCuota")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Motivo")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<string>("MotivoRechazo")
                        .HasColumnType("longtext");

                    b.Property<int?>("MovimientoId")
                        .HasColumnType("int");

                    b.Property<string>("Notas")
                        .HasMaxLength(500)
                        .HasColumnType("varchar(500)");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("varchar(30)");

                    b.Property<decimal>("SaldoPendiente")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("ValesEmpleado");
                });

            modelBuilder.Entity("Kirkenta.Models.Venta", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("ClienteId")
                        .HasColumnType("int");

                    b.Property<int?>("CotizacionId")
                        .HasColumnType("int");

                    b.Property<decimal>("Descuento")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Estado")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Impuestos")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Notas")
                        .HasColumnType("longtext");

                    b.Property<string>("Numero")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int?>("PedidoId")
                        .HasColumnType("int");

                    b.Property<decimal>("Subtotal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("UsuarioCreoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Numero")
                        .IsUnique();

                    b.ToTable("Ventas");
                });
#pragma warning restore 612, 618
        }
    }
}

````

====================================================
 Models - 54 archivo(s)
====================================================

===== FILE: Models/ActividadUsuario.cs =====

````csharp
namespace Kirkenta.Models
{
    public class ActividadUsuario
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? Detalle { get; set; }
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
````

===== FILE: Models/AdjuntoCierre.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Adjuntos (comprobantes, fotos, boletas) asociados a un cierre de caja.
    /// </summary>
    public class AdjuntoCierre
    {
        public int Id { get; set; }
        public int CierreCajaId { get; set; }

        [Required]
        [StringLength(200)]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string RutaArchivo { get; set; } = string.Empty;

        [StringLength(20)]
        public string? TipoArchivo { get; set; }

        public int TamanoKB { get; set; }

        [StringLength(100)]
        public string? Descripcion { get; set; }

        public DateTime FechaSubida { get; set; } = DateTime.Now;
        public int? UsuarioId { get; set; }
    }
}
````

===== FILE: Models/AdjuntoCompra.cs =====

````csharp
namespace Kirkenta.Models
{
    public class AdjuntoCompra
    {
        public int Id { get; set; }
        public int? OrdenCompraId { get; set; }
        public int? PagoProveedorId { get; set; }
        public string TipoDocumento { get; set; } = "Factura";
        public string? NombreArchivo { get; set; }
        public string? RutaArchivo { get; set; }
        public string? TipoArchivo { get; set; }
        public int TamanoKB { get; set; }
        public DateTime FechaSubida { get; set; } = DateTime.Now;
        public int? UsuarioId { get; set; }
    }
}
````

===== FILE: Models/AdjuntoEmpleado.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Documento adjunto de un empleado (foto, DNI, contrato, etc.).
    /// </summary>
    public class AdjuntoEmpleado
    {
        public int Id { get; set; }

        public int EmpleadoId { get; set; }
        public int TipoDocumentoId { get; set; }

        [Required]
        [StringLength(200)]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string RutaArchivo { get; set; } = string.Empty;

        [StringLength(20)]
        public string? TipoArchivo { get; set; } // pdf, jpg, png

        public int TamanoKB { get; set; }

        [StringLength(500)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Fecha del documento (cuándo fue emitido)
        /// </summary>
        public DateTime? FechaDocumento { get; set; }

        /// <summary>
        /// Fecha de vencimiento del documento (si aplica)
        /// </summary>
        public DateTime? FechaVencimiento { get; set; }

        /// <summary>
        /// Si el documento está vigente (calculado)
        /// </summary>
        public bool Vigente { get; set; } = true;

        public DateTime FechaSubida { get; set; } = DateTime.Now;
        public int? UsuarioSubioId { get; set; }
    }
}
````

===== FILE: Models/AlertaPersonalizada.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Alerta personalizada creada manualmente por el usuario.
    /// Sirve para agregar recordatorios adicionales (ej: revisar algo, contactar a alguien).
    /// </summary>
    public class AlertaPersonalizada
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Empleado relacionado (opcional)
        /// </summary>
        public int? EmpleadoId { get; set; }

        /// <summary>
        /// Info | Advertencia | Urgente | Exito
        /// </summary>
        [StringLength(20)]
        public string Prioridad { get; set; } = "Info";

        public DateTime FechaAlerta { get; set; } = DateTime.Now;

        /// <summary>
        /// Si la alerta debe mostrarse en el dashboard
        /// </summary>
        public bool MostrarEnDashboard { get; set; } = true;

        /// <summary>
        /// Fecha hasta la que debe mostrarse (opcional)
        /// </summary>
        public DateTime? FechaVigenciaHasta { get; set; }

        public bool Completada { get; set; } = false;
        public DateTime? FechaCompletada { get; set; }

        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
````

===== FILE: Models/AperturaCaja.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Apertura de caja: se registra el saldo inicial con el que abre
    /// el cajero antes de empezar las ventas del día.
    /// Solo puede haber una apertura activa por cuenta.
    /// </summary>
    public class AperturaCaja
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int CuentaId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Today;
        public DateTime FechaApertura { get; set; } = DateTime.Now;

        public int UsuarioAbreId { get; set; }

        public decimal SaldoInicial { get; set; } = 0;

        [StringLength(500)]
        public string? Notas { get; set; }

        /// <summary>
        /// Activa mientras la caja está abierta. Se marca false al cerrar.
        /// </summary>
        public bool Activa { get; set; } = true;

        /// <summary>
        /// Se asigna cuando se cierra la caja.
        /// </summary>
        public int? CierreId { get; set; }

        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/BajaInventario.cs =====

````csharp
namespace Kirkenta.Models
{
    public class BajaInventario
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public string TipoBaja { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public int UsuarioSolicitaId { get; set; }
        public int? UsuarioApruebaId { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public string? MotivoRechazo { get; set; }
        public bool Revertida { get; set; } = false;
        public DateTime? FechaReversion { get; set; }
        public int? UsuarioRevierteId { get; set; }
        public string? MotivoReversion { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/Categoria.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Descripcion { get; set; }

        [StringLength(20)]
        public string Color { get; set; } = "#6b7280";

        public bool Activa { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
````

===== FILE: Models/CategoriaFinanciera.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Categoría de ingreso o egreso.
    /// Ejemplos: "Ventas al contado", "Alquiler", "Servicios básicos", "Nómina"
    /// Cada empresa puede crear las suyas; el sistema seedea las más comunes.
    /// </summary>
    public class CategoriaFinanciera
    {
        public int Id { get; set; }

        /// <summary>
        /// Tipo: "Ingreso" | "Egreso"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = "Egreso";

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Cuenta contable formal (ej: 5101)
        /// </summary>
        [StringLength(30)]
        public string? CuentaContable { get; set; }

        /// <summary>
        /// ⬇️ NUEVO: FK al Plan de Cuentas.
        /// Nullable para mantener compatibilidad con categorías existentes.
        /// </summary>
        public int? PlanCuentaId { get; set; }

        [StringLength(20)]
        public string Color { get; set; } = "#6b7280";

        public bool EsSistema { get; set; } = false;
        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/CierreCaja.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Registro de cierre de caja (arqueo) realizado por un usuario.
    /// Guarda el efectivo esperado según sistema vs el efectivo contado físicamente.
    /// </summary>
    public class CierreCaja
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int CuentaId { get; set; }

        /// <summary>
        /// Apertura de caja asociada. De aquí se toma el saldo inicial.
        /// </summary>
        public int? AperturaId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Today;
        public DateTime FechaCierre { get; set; } = DateTime.Now;

        public int UsuarioCierraId { get; set; }

        // ===== CÁLCULO =====
        public decimal SaldoInicialSistema { get; set; }
        public decimal TotalIngresosEfectivo { get; set; }
        public decimal TotalEgresosEfectivo { get; set; }
        public decimal EfectivoEsperado { get; set; }

        public decimal EfectivoContado { get; set; }
        public decimal Diferencia { get; set; }

        [StringLength(20)]
        public string Resultado { get; set; } = "Cuadrado";

        public decimal Tolerancia { get; set; } = 20.00m;

        [StringLength(500)]
        public string? Notas { get; set; }

        // ===== DISTRIBUCIÓN =====
        /// <summary>
        /// Suma de los montos distribuidos. Debe igualar EfectivoContado.
        /// </summary>
        public decimal TotalDistribuido { get; set; }

        // ===== APROBACIÓN =====
        /// <summary>
        /// Estado del acta: "Borrador" | "Cerrado" | "Aprobado" | "Rechazado"
        /// </summary>
        [StringLength(20)]
        public string EstadoActa { get; set; } = "Cerrado";

        public int? UsuarioApruebaId { get; set; }
        public DateTime? FechaAprobacion { get; set; }

        [StringLength(500)]
        public string? MotivoRechazo { get; set; }

        // ===== MOVIMIENTO DE AJUSTE =====
        public int? MovimientoAjusteId { get; set; }

        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/CierreContable.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Cierre contable mensual. Al cerrar un mes, se bloquean todos los movimientos
    /// financieros, aperturas, cierres de caja y ventas con fecha dentro de ese mes.
    /// Solo se puede reabrir (con motivo) por un usuario con permiso.
    /// Anio + Mes es único: una sola fila por mes.
    /// </summary>
    public class CierreContable
    {
        public int Id { get; set; }

        [Required]
        public int Anio { get; set; }

        [Required]
        [Range(1, 12)]
        public int Mes { get; set; }

        /// <summary>
        /// Estado: "Abierto" | "Cerrado"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Abierto";

        // ===== CIERRE =====
        public DateTime? FechaCierre { get; set; }
        public int? UsuarioCierreId { get; set; }

        // ===== REAPERTURA =====
        public DateTime? FechaReapertura { get; set; }
        public int? UsuarioReaperturaId { get; set; }

        [StringLength(500)]
        public string? MotivoReapertura { get; set; }

        // ===== TOTALES AL MOMENTO DEL CIERRE (auditoría) =====
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal Balance { get; set; }
        public int CantidadMovimientos { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int EmpresaId { get; set; } = 1;

        // ===== HELPERS =====
        public string MesNombre => System.Globalization.CultureInfo
            .GetCultureInfo("es-HN")
            .DateTimeFormat
            .GetMonthName(Mes);

        public string PeriodoTexto => $"{MesNombre} {Anio}";
    }
}
````

===== FILE: Models/Cliente.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [StringLength(20)]
        public string? Codigo { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string? RazonSocial { get; set; }

        [StringLength(20)]
        public string? RTN { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Telefono { get; set; }

        [StringLength(300)]
        public string? Direccion { get; set; }

        [StringLength(100)]
        public string? Ciudad { get; set; }

        [StringLength(80)]
        public string? Pais { get; set; } = "Honduras";

        [StringLength(20)]
        public string TipoCliente { get; set; } = "Regular";

        public decimal LimiteCredito { get; set; } = 0;
        public int DiasCredito { get; set; } = 0;

        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
    }
}
````

===== FILE: Models/ConciliacionBancaria.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Conciliación bancaria: compara los movimientos del sistema en una cuenta bancaria
    /// contra el estado de cuenta real del banco, para un período dado.
    /// </summary>
    public class ConciliacionBancaria
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int CuentaId { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        /// <summary>
        /// Saldo que reporta el banco al final del período.
        /// </summary>
        public decimal SaldoBanco { get; set; }

        /// <summary>
        /// Saldo que el sistema calcula al final del período (saldo contable).
        /// </summary>
        public decimal SaldoSistema { get; set; }

        /// <summary>
        /// Diferencia = SaldoBanco - SaldoSistema. Debería ser 0 si todo cuadra.
        /// </summary>
        public decimal Diferencia { get; set; }

        /// <summary>
        /// Abierta | EnRevision | Conciliada | Cancelada
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Abierta";

        // ===== CIERRE =====
        public int? UsuarioCierraId { get; set; }
        public DateTime? FechaCierre { get; set; }

        [StringLength(500)]
        public string? NotasCierre { get; set; }

        // ===== TOTALES (para listados rápidos) =====
        public int TotalLineasSistema { get; set; }
        public int TotalLineasBanco { get; set; }
        public int TotalMatcheadas { get; set; }
        public int TotalNoMatcheadas { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }

    /// <summary>
    /// Detalle de la conciliación: cada línea es un movimiento del sistema
    /// o una línea del estado de cuenta bancario.
    /// </summary>
    public class ConciliacionDetalle
    {
        public int Id { get; set; }

        public int ConciliacionBancariaId { get; set; }

        /// <summary>
        /// Origen: "Sistema" | "Banco"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Origen { get; set; } = "Sistema";

        /// <summary>
        /// Si origen = Sistema: ID del MovimientoFinanciero.
        /// </summary>
        public int? MovimientoId { get; set; }

        /// <summary>
        /// Fecha de la línea (del movimiento o del estado de cuenta).
        /// </summary>
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Descripción/concepto de la línea.
        /// </summary>
        [StringLength(300)]
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>
        /// Referencia externa (nº de cheque, transferencia, etc.).
        /// </summary>
        [StringLength(100)]
        public string? Referencia { get; set; }

        /// <summary>
        /// Monto de la línea. Positivo = crédito/ingreso, negativo = débito/egreso.
        /// </summary>
        public decimal Monto { get; set; }

        /// <summary>
        /// Si la línea ya fue matcheada con su contraparte.
        /// </summary>
        public bool Matcheada { get; set; } = false;

        /// <summary>
        /// Si matcheada, ID de la línea contraparte (del banco si esta es del sistema,
        /// o del sistema si esta es del banco).
        /// </summary>
        public int? MatcheadaConDetalleId { get; set; }

        /// <summary>
        /// Si el match fue automático o manual.
        /// </summary>
        [StringLength(20)]
        public string? TipoMatch { get; set; } // "Automatico" | "Manual"

        /// <summary>
        /// Notas de revisión.
        /// </summary>
        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
````

===== FILE: Models/ConfiguracionDeduccion.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Configuración de deducciones por año fiscal.
    /// Permite tener valores distintos por año (SAR actualiza cada año).
    /// </summary>
    public class ConfiguracionDeduccion
    {
        public int Id { get; set; }

        [Required]
        public int Anio { get; set; }

        // ===== IHSS =====
        public bool AplicaIHSS { get; set; } = true;

        [Range(0, 100)]
        public decimal PorcentajeIHSS { get; set; } = 2.5m;

        public decimal TopeIHSS { get; set; } = 11995.00m;

        // ===== RAP =====
        public bool AplicaRAP { get; set; } = true;

        [Range(0, 100)]
        public decimal PorcentajeRAP { get; set; } = 1.5m;

        public decimal? TopeRAP { get; set; }

        // ===== ISR =====
        public bool AplicaISR { get; set; } = true;

        /// <summary>
        /// Tope anual exento de ISR (L. 250,000 en Honduras 2024-2026)
        /// </summary>
        public decimal TopeAnualExentoISR { get; set; } = 250000.00m;

        /// <summary>
        /// Método: "Acumulativo" (SAR) o "MensualSimple"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string MetodoISR { get; set; } = "Acumulativo";

        // ===== INFORMACIÓN =====
        [StringLength(500)]
        public string? Notas { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
    }
}
````

===== FILE: Models/ConfiguracionEmpresa.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Configuración general de la empresa, incluyendo RRHH, nómina y calendario.
    /// </summary>
    public class ConfiguracionEmpresa
    {
        public int Id { get; set; }

        // ===== DATOS GENERALES (existentes) =====
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string? RazonSocial { get; set; }

        [StringLength(20)]
        public string? RTN { get; set; }

        [StringLength(300)]
        public string? Direccion { get; set; }

        [StringLength(100)]
        public string? Ciudad { get; set; }

        [StringLength(80)]
        public string? Pais { get; set; } = "Honduras";

        [StringLength(30)]
        public string? Telefono { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(150)]
        public string? SitioWeb { get; set; }

        [StringLength(300)]
        public string? LogoPath { get; set; }

        [StringLength(50)]
        public string? CAI { get; set; }

        [StringLength(30)]
        public string? RangoInicial { get; set; }

        [StringLength(30)]
        public string? RangoFinal { get; set; }

        public DateTime? FechaLimiteEmision { get; set; }

        public string? NotasFactura { get; set; }

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        // ===== PAÍS Y ZONA HORARIA (nuevos para feriados) =====
        /// <summary>
        /// Código ISO del país: HN, GT, SV, CR, NI, PA, MX, US
        /// </summary>
        [StringLength(3)]
        public string PaisCodigo { get; set; } = "HN";

        /// <summary>
        /// Zona horaria IANA: America/Tegucigalpa
        /// </summary>
        [StringLength(60)]
        public string ZonaHoraria { get; set; } = "America/Tegucigalpa";

        // ===== CONFIGURACIÓN RRHH =====
        /// <summary>
        /// Frecuencia de pago por defecto: Semanal | Catorcenal | Quincenal | Mensual
        /// </summary>
        [StringLength(20)]
        public string RHFrecuenciaPagoDefault { get; set; } = "Mensual";

        /// <summary>
        /// Día 1 de pago (para quincenal: 15, mensual: 30)
        /// </summary>
        public int RHDiaPago1 { get; set; } = 15;

        /// <summary>
        /// Día 2 de pago (solo quincenal)
        /// </summary>
        public int? RHDiaPago2 { get; set; } = 30;

        /// <summary>
        /// Día de la semana para pago semanal (1 = lunes, 5 = viernes)
        /// </summary>
        public int? RHDiaSemanalPago { get; set; } = 5;

        // ===== DEDUCCIONES (legacy — la config nueva está en ConfiguracionDeduccion) =====
        public bool RHAplicaIHSS { get; set; } = true;
        public decimal RHPorcentajeIHSS { get; set; } = 2.5m;
        public decimal RHTopeIHSS { get; set; } = 11995.00m;

        public bool RHAplicaRAP { get; set; } = true;
        public decimal RHPorcentajeRAP { get; set; } = 1.5m;

        public bool RHAplicaISR { get; set; } = true;

        /// <summary>
        /// Método por defecto para cálculo ISR: Acumulativo | MensualSimple
        /// </summary>
        [StringLength(20)]
        public string RHMetodoISRDefault { get; set; } = "Acumulativo";

        // ===== VACACIONES POR ANTIGÜEDAD =====
        /// <summary>
        /// Tabla de días de vacaciones por año de antigüedad.
        /// Formato: "1:10,2:12,3:15,4:20,5:20"
        /// </summary>
        [StringLength(200)]
        public string RHTablaVacaciones { get; set; } = "1:10,2:12,3:15,4:20,5:20";

        /// <summary>
        /// Años de antigüedad a partir de los cuales se repite el último valor.
        /// En Honduras: a partir del año 5 son 20 días cada año.
        /// </summary>
        public int RHAntiguedadMaxTabla { get; set; } = 5;

        // ===== FERIADOS =====
        /// <summary>
        /// Si el sistema debe cargar automáticamente los feriados del país
        /// </summary>
        public bool RHCargarFeriadosAuto { get; set; } = true;

        // ===== ALERTAS =====
        public bool RHAlertaFeriadosProximos { get; set; } = true;
        public int RHAlertaFeriadosDias { get; set; } = 7;

        public bool RHAlertaVacacionesProximas { get; set; } = true;
        public int RHAlertaVacacionesDias { get; set; } = 7;

        public bool RHAlertaCumpleanios { get; set; } = true;
        public bool RHAlertaAniversarios { get; set; } = true;
        public bool RHAlertaValesPorVencer { get; set; } = true;
        public bool RHAlertaContratosPorVencer { get; set; } = true;
        public int RHAlertaContratosDias { get; set; } = 30;
        public bool RHAlertaDocumentosVencidos { get; set; } = true;
    }
}
````

===== FILE: Models/Cotizacion.cs =====

````csharp
namespace Kirkenta.Models
{
    public class Cotizacion
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int ClienteId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public DateTime? FechaVencimiento { get; set; }
        public decimal Subtotal { get; set; } = 0;
        public decimal Descuento { get; set; } = 0;
        public decimal Impuestos { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public string Estado { get; set; } = "Borrador";
        public string? Notas { get; set; }
        public int Validez { get; set; } = 30;
        public int? FacturaId { get; set; }
        public DateTime? FechaConversion { get; set; }
        public int? UsuarioCreoId { get; set; }

        // ===== LOGÍSTICA =====
        public bool RequiereEnvio { get; set; } = false;
        public decimal MontoEnvio { get; set; } = 0;
    }

    public class DetalleCotizacion
    {
        public int Id { get; set; }
        public int CotizacionId { get; set; }
        public int ProductoId { get; set; }
        public string? Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; } = 0;
        public decimal ImpuestoPorcentaje { get; set; } = 0;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
    }
}
````

===== FILE: Models/CuentaFinanciera.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Representa una cuenta de la empresa: caja chica, caja general, caja POS,
    /// o una cuenta bancaria. Cada movimiento afecta el saldo de una cuenta.
    /// </summary>
    public class CuentaFinanciera
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Tipo: "Caja" | "Banco"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = "Caja";

        /// <summary>
        /// Subtipo para cajas: "Chica" | "General" | "POS"
        /// Para bancos: null
        /// </summary>
        [StringLength(30)]
        public string? Subtipo { get; set; }

        /// <summary>
        /// Número de cuenta bancaria (solo para tipo = Banco)
        /// </summary>
        [StringLength(50)]
        public string? NumeroCuenta { get; set; }

        /// <summary>
        /// Nombre del banco (solo para tipo = Banco)
        /// </summary>
        [StringLength(100)]
        public string? Banco { get; set; }

        public int MonedaId { get; set; } = 1;

        public decimal SaldoInicial { get; set; } = 0;
        public decimal SaldoActual { get; set; } = 0;

        public decimal? LimiteCredito { get; set; }

        /// <summary>
        /// Cuenta contable formal (ej: 1101) para contabilidad
        /// </summary>
        [StringLength(30)]
        public string? CuentaContable { get; set; }

        [StringLength(300)]
        public string? Descripcion { get; set; }

        [StringLength(150)]
        public string? Responsable { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/Devolucion.cs =====

````csharp
namespace Kirkenta.Models
{
    public class Devolucion
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int? FacturaId { get; set; }
        public int? VentaId { get; set; }
        public int ClienteId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string? Motivo { get; set; }
        public decimal Total { get; set; } = 0;
        public string Estado { get; set; } = "Aplicada";
        public int? UsuarioCreoId { get; set; }
    }

    public class DetalleDevolucion
    {
        public int Id { get; set; }
        public int DevolucionId { get; set; }
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }
    }
}
````

===== FILE: Models/DevolucionProveedor.cs =====

````csharp
namespace Kirkenta.Models
{
    public class DevolucionProveedor
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int OrdenCompraId { get; set; }
        public int ProveedorId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string? Motivo { get; set; }
        public decimal Total { get; set; } = 0;
        public string Estado { get; set; } = "Aplicada";
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }

    public class DetalleDevolucionProveedor
    {
        public int Id { get; set; }
        public int DevolucionId { get; set; }
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }
    }
}
````

===== FILE: Models/DistribucionCierre.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Distribución del efectivo contado en un cierre de caja.
    /// Cada cierre puede tener varias distribuciones: retiro a banco, fondo
    /// para mañana, entrega a administración, etc.
    /// La suma de las distribuciones debe igualar el EfectivoContado del cierre.
    /// </summary>
    public class DistribucionCierre
    {
        public int Id { get; set; }

        public int CierreCajaId { get; set; }

        /// <summary>
        /// Tipo: "RetiroBanco" | "FondoCaja" | "EntregaAdmin" | "PagoDirecto" | "Otro"
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Tipo { get; set; } = "FondoCaja";

        public decimal Monto { get; set; }

        /// <summary>
        /// Cuenta destino (si es RetiroBanco)
        /// </summary>
        public int? CuentaDestinoId { get; set; }

        /// <summary>
        /// Descripción libre del destino (si no es cuenta)
        /// </summary>
        [StringLength(200)]
        public string? DestinoDescripcion { get; set; }

        /// <summary>
        /// Referencia externa: # de depósito, cheque, etc.
        /// </summary>
        [StringLength(100)]
        public string? Referencia { get; set; }

        /// <summary>
        /// Usuario que recibe el dinero (si es EntregaAdmin o PagoDirecto)
        /// </summary>
        public int? UsuarioRecibeId { get; set; }

        /// <summary>
        /// Nombre de quien recibe (si no es usuario del sistema)
        /// </summary>
        [StringLength(150)]
        public string? NombreRecibe { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        /// <summary>
        /// Movimiento financiero generado por esta distribución
        /// </summary>
        public int? MovimientoId { get; set; }
    }
}
````

===== FILE: Models/Empleado.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Empleado de la empresa. Contiene todos los datos personales, laborales
    /// y de contacto. Se liga opcionalmente a un Usuario del sistema.
    /// </summary>
    public class Empleado
    {
        public int Id { get; set; }

        // ===== CÓDIGO Y USUARIO =====
        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Usuario del sistema vinculado (opcional). Si tiene, puede
        /// solicitar vacaciones online.
        /// </summary>
        public int? UsuarioId { get; set; }

        // ===== FOTO =====
        [StringLength(300)]
        public string? FotoPath { get; set; }

        // ===== DATOS PERSONALES =====
        [Required]
        [StringLength(150)]
        public string Nombres { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Apellidos { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Cedula { get; set; } = string.Empty;

        [StringLength(20)]
        public string? RTN { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        [StringLength(30)]
        public string? EstadoCivil { get; set; } // Soltero, Casado, Divorciado, Viudo, Unión libre

        [StringLength(30)]
        public string? Genero { get; set; } // Masculino, Femenino, Otro

        [StringLength(50)]
        public string? Nacionalidad { get; set; }

        // ===== CONTACTO =====
        [StringLength(30)]
        public string? Telefono { get; set; }

        [StringLength(30)]
        public string? TelefonoSecundario { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(300)]
        public string? Direccion { get; set; }

        [StringLength(100)]
        public string? Ciudad { get; set; }

        [StringLength(80)]
        public string? Pais { get; set; } = "Honduras";

        // ===== CONTACTO DE EMERGENCIA =====
        [StringLength(150)]
        public string? ContactoEmergenciaNombre { get; set; }

        [StringLength(30)]
        public string? ContactoEmergenciaTelefono { get; set; }

        [StringLength(50)]
        public string? ContactoEmergenciaParentesco { get; set; }

        // ===== DATOS LABORALES =====
        public int? PuestoId { get; set; }

        [StringLength(100)]
        public string? PuestoNombre { get; set; }

        [StringLength(100)]
        public string? Departamento { get; set; }

        public DateTime FechaIngreso { get; set; } = DateTime.Today;

        public DateTime? FechaBaja { get; set; }

        [StringLength(30)]
        public string TipoContrato { get; set; } = "Indefinido"; // Indefinido, Temporal, Obra, Temporada

        public DateTime? FechaFinContrato { get; set; }

        [StringLength(30)]
        public string TipoJornada { get; set; } = "TiempoCompleto"; // TiempoCompleto, MedioTiempo, PorHora

        [StringLength(150)]
        public string? JefeInmediato { get; set; }

        // ===== SALARIO =====
        public decimal SalarioBase { get; set; } = 0;

        [StringLength(20)]
        public string FrecuenciaPago { get; set; } = "Mensual"; // Semanal, Catorcenal, Quincenal, Mensual

        public decimal? BonoTransporte { get; set; }
        public decimal? BonoAlimentacion { get; set; }
        public decimal? OtrosBonos { get; set; }

        // ===== DEDUCCIONES =====
        public bool CotizaIHSS { get; set; } = true;
        public bool CotizaRAP { get; set; } = true;
        public bool AplicaISR { get; set; } = true;

        // ===== BANCO =====
        [StringLength(100)]
        public string? Banco { get; set; }

        [StringLength(50)]
        public string? CuentaBancaria { get; set; }

        // ===== ESTADO =====
        /// <summary>
        /// Activo | Inactivo | Suspendido | Vacaciones | Licencia | Baja
        /// </summary>
        [StringLength(30)]
        public string Estado { get; set; } = "Activo";

        // ===== VACACIONES (SALDOS) =====
        /// <summary>
        /// Días acumulados disponibles para tomar
        /// </summary>
        public decimal DiasVacacionesDisponibles { get; set; } = 0;

        /// <summary>
        /// Total de días que ha ganado por antigüedad (histórico)
        /// </summary>
        public decimal DiasVacacionesGanados { get; set; } = 0;

        /// <summary>
        /// Total de días que ha tomado (histórico)
        /// </summary>
        public decimal DiasVacacionesTomados { get; set; } = 0;

        /// <summary>
        /// Último año de antigüedad procesado (para acumulación anual)
        /// </summary>
        public int UltimoAnioVacacionesProcesado { get; set; } = 0;

        // ===== NOTAS =====
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/Envio.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Representa un envío individual (una parada / un destino).
    /// Puede venir de una Venta, Cotización, Pedido o Factura.
    /// Se agrupa dentro de una Ruta para despacho.
    /// </summary>
    public class Envio
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        // ===== ORIGEN (a qué documento pertenece) =====
        public int? VentaId { get; set; }
        public int? PedidoId { get; set; }
        public int? FacturaId { get; set; }
        public int? CotizacionId { get; set; }

        // ===== CLIENTE (denormalizado para reportes rápidos) =====
        public int ClienteId { get; set; }

        [StringLength(150)]
        public string ClienteNombre { get; set; } = string.Empty;

        // ===== INFO SNAPSHOT DE LA VENTA =====
        // (los datos se copian al momento de la venta y NO cambian después)
        [Required]
        [StringLength(300)]
        public string DireccionEntrega { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Referencia { get; set; }          // punto de referencia

        [StringLength(150)]
        public string ContactoNombre { get; set; } = string.Empty;

        [StringLength(30)]
        public string ContactoTelefono { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Ciudad { get; set; }

        // ===== ZONA =====
        public int? ZonaId { get; set; }

        [StringLength(100)]
        public string? ZonaNombre { get; set; }

        // ===== MONTO =====
        public decimal Monto { get; set; } = 0;
        public bool EsGratis { get; set; } = false;

        // ===== ESTADO INDIVIDUAL DE ESTA PARADA =====
        /// <summary>
        /// Pendiente | EnRuta | Entregado | Fallido | Reagendado | Cancelado
        /// </summary>
        [StringLength(30)]
        public string Estado { get; set; } = "Pendiente";

        // ===== RUTA (asignada por encargado de almacén) =====
        public int? RutaId { get; set; }
        public int? OrdenParada { get; set; }

        // ===== REPARTIDOR (se copia de la Ruta al despachar) =====
        public int? RepartidorId { get; set; }

        [StringLength(150)]
        public string? RepartidorNombre { get; set; }

        [StringLength(100)]
        public string? Vehiculo { get; set; }

        // ===== FECHAS =====
        public DateTime? FechaEntregaEstimada { get; set; }
        public DateTime? FechaSalida { get; set; }
        public DateTime? FechaEntregaReal { get; set; }

        // ===== ENTREGA / EVIDENCIA =====
        [StringLength(150)]
        public string? NombreRecibio { get; set; }        // quién firmó

        [StringLength(300)]
        public string? FirmaImagen { get; set; }          // ruta relativa de imagen

        [StringLength(300)]
        public string? FotoEntrega { get; set; }

        [StringLength(500)]
        public string? MotivoFallo { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/ExpedienteEmpleado.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Registro de expediente de un empleado: llamados de atención,
    /// amonestaciones, suspensiones, notas de mérito, etc.
    /// </summary>
    public class ExpedienteEmpleado
    {
        public int Id { get; set; }

        public int EmpleadoId { get; set; }

        /// <summary>
        /// LlamadoAtencion | AmonestacionVerbal | AmonestacionEscrita | 
        /// Suspension | NotaMerito | Reconocimiento | Otro
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } = "LlamadoAtencion";

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>
        /// Leve | Media | Grave | MuyGrave
        /// </summary>
        [StringLength(20)]
        public string? Gravedad { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public DateTime? FechaInicioSuspension { get; set; }
        public DateTime? FechaFinSuspension { get; set; }
        public int? DiasSuspension { get; set; }

        /// <summary>
        /// Quién levantó el expediente
        /// </summary>
        public int? UsuarioRegistroId { get; set; }

        [StringLength(150)]
        public string? NombreRegistro { get; set; }

        /// <summary>
        /// Testigos o personas presentes
        /// </summary>
        [StringLength(300)]
        public string? Testigos { get; set; }

        /// <summary>
        /// Si el empleado firmó de enterado
        /// </summary>
        public bool EmpleadoFirmo { get; set; } = false;

        public DateTime? FechaFirma { get; set; }

        /// <summary>
        /// Ruta de documento firmado (si se subió)
        /// </summary>
        [StringLength(300)]
        public string? RutaDocumentoFirmado { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
````

===== FILE: Models/Factura.cs =====

````csharp
namespace Kirkenta.Models
{
    public class Factura
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int ClienteId { get; set; }
        public int? CotizacionId { get; set; }
        public int? PedidoId { get; set; }
        public int? VentaId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public DateTime? FechaVencimiento { get; set; }
        public decimal Subtotal { get; set; } = 0;
        public decimal Descuento { get; set; } = 0;
        public decimal Impuestos { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public decimal Saldo { get; set; } = 0;
        public string Estado { get; set; } = "Emitida";
        public string? Notas { get; set; }
        public string? CAI { get; set; }
        public string? RangoInicial { get; set; }
        public string? RangoFinal { get; set; }
        public DateTime? FechaLimiteEmision { get; set; }
        public int? UsuarioCreoId { get; set; }

        // ===== LOGÍSTICA =====
        public bool RequiereEnvio { get; set; } = false;
        public decimal MontoEnvio { get; set; } = 0;
    }

    public class DetalleFactura
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }
        public int ProductoId { get; set; }
        public string? Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; } = 0;
        public decimal ImpuestoPorcentaje { get; set; } = 0;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
    }
}
````

===== FILE: Models/Feriado.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Feriado nacional o regional. Se usa para calcular días de vacaciones
    /// y para el calendario de RRHH.
    /// </summary>
    public class Feriado
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Fecha exacta del feriado (para el año específico)
        /// </summary>
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Código ISO del país: HN, GT, SV, CR, NI, PA, MX, US
        /// </summary>
        [Required]
        [StringLength(3)]
        public string PaisCodigo { get; set; } = "HN";

        /// <summary>
        /// Nacional | Religioso | Civico | Regional
        /// </summary>
        [StringLength(30)]
        public string Tipo { get; set; } = "Nacional";

        /// <summary>
        /// Si es un feriado recurrente (se repite cada año en misma fecha)
        /// </summary>
        public bool EsRecurrente { get; set; } = true;

        /// <summary>
        /// Si es móvil (ej: Semana Santa, depende del año)
        /// </summary>
        public bool EsMovil { get; set; } = false;

        /// <summary>
        /// Año al que aplica (null si es recurrente)
        /// </summary>
        public int? Anio { get; set; }

        [StringLength(300)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
````

===== FILE: Models/Impuesto.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Impuesto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        public decimal Porcentaje { get; set; }

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public bool EsPredeterminado { get; set; }
        public bool Activo { get; set; } = true;
    }
}
````

===== FILE: Models/Login.cshtml =====

````html
@page
@model Kirkenta.Pages.Auth.LoginModel
@{
    Layout = "_Layout";
}

<h2>Login</h2>

<form method="post">
    <div>
        <label>Usuario</label>
        <input asp-for="Username" />
    </div>
    <div>
        <label>Contraseña</label>
        <input asp-for="Password" type="password" />
    </div>
    <button type="submit">Ingresar</button>
</form>

<span asp-validation-summary="All"></span>

````

===== FILE: Models/MetodoPago.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class MetodoPago
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(30)]
        public string Tipo { get; set; } = "Efectivo";

        public bool RequiereReferencia { get; set; }
        public bool Activo { get; set; } = true;
    }
}
````

===== FILE: Models/Moneda.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Moneda
    {
        public int Id { get; set; }

        [Required]
        [StringLength(3)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(5)]
        public string? Simbolo { get; set; }

        public decimal TipoCambio { get; set; } = 1;

        public bool EsPredeterminada { get; set; } = false;
        public bool Activa { get; set; } = true;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}
````

===== FILE: Models/MovimientoFinanciero.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Un movimiento financiero: ingreso, egreso o transferencia entre cuentas.
    /// Cada movimiento afecta el saldo de la cuenta origen (y destino si es transferencia).
    /// Los movimientos no se borran, se anulan (para tener historial).
    /// </summary>
    public class MovimientoFinanciero
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        /// <summary>
        /// Tipo: "Ingreso" | "Egreso" | "Transferencia"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = "Egreso";

        public DateTime Fecha { get; set; } = DateTime.Now;

        /// <summary>
        /// Cuenta que recibe (ingreso) o entrega (egreso) el dinero.
        /// Para transferencias, es la cuenta origen.
        /// </summary>
        public int CuentaId { get; set; }

        /// <summary>
        /// Para transferencias: cuenta destino
        /// </summary>
        public int? CuentaDestinoId { get; set; }

        /// <summary>
        /// Categoría (solo para Ingreso/Egreso, no para Transferencia)
        /// </summary>
        public int? CategoriaId { get; set; }

        public decimal Monto { get; set; }

        public int MonedaId { get; set; } = 1;
        public decimal TipoCambio { get; set; } = 1;

        [StringLength(300)]
        public string Concepto { get; set; } = string.Empty;

        /// <summary>
        /// Referencia externa: nº de factura, cheque, transferencia
        /// </summary>
        [StringLength(100)]
        public string? Referencia { get; set; }

        /// <summary>
        /// Forma de pago/cobro: Efectivo, Transferencia, Cheque, Tarjeta
        /// </summary>
        [StringLength(30)]
        public string? FormaPago { get; set; }

        /// <summary>
        /// Origen del movimiento: Manual, Venta, Compra, Pago Proveedor, Nómina, Vale
        /// </summary>
        [StringLength(30)]
        public string Origen { get; set; } = "Manual";

        /// <summary>
        /// ID del documento origen (ej: IdVenta, IdPagoProveedor)
        /// </summary>
        public int? OrigenId { get; set; }

        /// <summary>
        /// Si el movimiento fue creado automáticamente por el sistema
        /// </summary>
        public bool EsAutomatico { get; set; } = false;

        [StringLength(500)]
        public string? Notas { get; set; }

        /// <summary>
        /// Estado: Activo | Anulado
        /// </summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Activo";

        public string? MotivoAnulacion { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public int? UsuarioAnuloId { get; set; }

        // Cierre contable
        public bool Conciliado { get; set; } = false;
        public DateTime? FechaConciliacion { get; set; }
        public int? ConciliadoPorId { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/Nomina.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Nómina (o planilla) de un período. Agrupa el pago de varios empleados.
    /// Se aprueba antes de pagarse y genera un egreso en Finanzas.
    /// </summary>
    public class Nomina
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        /// <summary>
        /// Semanal | Catorcenal | Quincenal | Mensual | Extraordinaria
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = "Mensual";

        [Required]
        [StringLength(200)]
        public string PeriodoDescripcion { get; set; } = string.Empty;

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public DateTime FechaPago { get; set; }

        /// <summary>
        /// Borrador | Calculada | Aprobada | Pagada | Anulada
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Borrador";

        // ===== TOTALES =====
        public int CantidadEmpleados { get; set; } = 0;

        public decimal TotalSalariosBrutos { get; set; } = 0;
        public decimal TotalBonos { get; set; } = 0;
        public decimal TotalHorasExtra { get; set; } = 0;

        public decimal TotalDeduccionIHSS { get; set; } = 0;
        public decimal TotalDeduccionRAP { get; set; } = 0;
        public decimal TotalDeduccionISR { get; set; } = 0;
        public decimal TotalDeduccionVales { get; set; } = 0;
        public decimal TotalOtrasDeducciones { get; set; } = 0;

        public decimal TotalNeto { get; set; } = 0;

        // ===== APROBACIÓN =====
        public int? UsuarioCalculoId { get; set; }
        public DateTime? FechaCalculo { get; set; }

        public int? UsuarioApruebaId { get; set; }
        public DateTime? FechaAprobacion { get; set; }

        // ===== PAGO =====
        public int? UsuarioPagaId { get; set; }
        public DateTime? FechaPagoReal { get; set; }

        public int? CuentaId { get; set; }
        public int? MovimientoId { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int EmpresaId { get; set; } = 1;
    }

    /// <summary>
    /// Detalle de la nómina por empleado.
    /// </summary>
    public class DetalleNomina
    {
        public int Id { get; set; }

        public int NominaId { get; set; }
        public int EmpleadoId { get; set; }

        // ===== DEVENGADO =====
        public decimal SalarioBase { get; set; } = 0;
        public decimal BonoTransporte { get; set; } = 0;
        public decimal BonoAlimentacion { get; set; } = 0;
        public decimal OtrosBonos { get; set; } = 0;
        public decimal HorasExtra { get; set; } = 0;
        public decimal MontoHorasExtra { get; set; } = 0;
        public decimal Comisiones { get; set; } = 0;

        public decimal TotalBruto { get; set; } = 0;

        // ===== DEDUCCIONES =====
        public decimal DeduccionIHSS { get; set; } = 0;
        public decimal DeduccionRAP { get; set; } = 0;
        public decimal DeduccionISR { get; set; } = 0;
        public decimal DeduccionVales { get; set; } = 0;
        public decimal OtrasDeducciones { get; set; } = 0;

        public decimal TotalDeducciones { get; set; } = 0;

        // ===== NETO =====
        public decimal SalarioNeto { get; set; } = 0;

        // ===== DÍAS Y AUSENCIAS =====
        public decimal DiasTrabajados { get; set; } = 30;
        public decimal DiasAusencia { get; set; } = 0;
        public decimal DiasVacaciones { get; set; } = 0;
        public decimal DiasPermiso { get; set; } = 0;

        [StringLength(500)]
        public string? Notas { get; set; }
    }
}
````

===== FILE: Models/OrdenCompra.cs =====

````csharp
namespace Kirkenta.Models
{
    public class OrdenCompra
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int ProveedorId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public DateTime? FechaEntregaEstimada { get; set; }
        public DateTime? FechaRecepcion { get; set; }
        public string Estado { get; set; } = "Borrador";
        public decimal Subtotal { get; set; } = 0;
        public decimal Descuento { get; set; } = 0;
        public decimal Impuestos { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public decimal Saldo { get; set; } = 0;
        public int MonedaId { get; set; } = 1;
        public decimal TipoCambio { get; set; } = 1;
        public string? Notas { get; set; }
        public int? UsuarioCreoId { get; set; }
        public int? UsuarioRecibioId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }

    public class DetalleOrdenCompra
    {
        public int Id { get; set; }
        public int OrdenCompraId { get; set; }
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CantidadRecibida { get; set; } = 0;
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; } = 0;
        public decimal ImpuestoPorcentaje { get; set; } = 0;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
    }
}
````

===== FILE: Models/Pago.cs =====

````csharp
namespace Kirkenta.Models
{
    public class Pago
    {
        public int Id { get; set; }
        public int? FacturaId { get; set; }
        public int? VentaId { get; set; }
        public int ClienteId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Monto { get; set; }
        public int MetodoPagoId { get; set; }
        public string? Referencia { get; set; }
        public string? Notas { get; set; }
        public int? UsuarioCreoId { get; set; }
    }
}
````

===== FILE: Models/PagoNominaEmpleado.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Pago individual de la nómina a un empleado específico.
    /// Permite pagar por empleado o en lote, con método de pago configurable.
    /// </summary>
    public class PagoNominaEmpleado
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int NominaId { get; set; }
        public int EmpleadoId { get; set; }
        public int DetalleNominaId { get; set; }

        public decimal Monto { get; set; }

        /// <summary>
        /// Efectivo | Transferencia | Cheque | Deposito
        /// </summary>
        [Required]
        [StringLength(30)]
        public string MetodoPago { get; set; } = "Transferencia";

        /// <summary>
        /// Referencia del pago (número de cheque, transferencia, etc.)
        /// </summary>
        [StringLength(100)]
        public string? Referencia { get; set; }

        /// <summary>
        /// Cuenta de la que sale el dinero
        /// </summary>
        public int? CuentaId { get; set; }

        /// <summary>
        /// Movimiento financiero generado
        /// </summary>
        public int? MovimientoId { get; set; }

        public DateTime FechaPago { get; set; } = DateTime.Now;
        public int? UsuarioPagoId { get; set; }

        /// <summary>
        /// Pendiente | Pagado | Anulado
        /// </summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente";

        [StringLength(500)]
        public string? Notas { get; set; }

        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/PagoProveedor.cs =====

````csharp
namespace Kirkenta.Models
{
    public class PagoProveedor
    {
        public int Id { get; set; }
        public string? Numero { get; set; }
        public int ProveedorId { get; set; }
        public int? OrdenCompraId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Monto { get; set; }
        public int MetodoPagoId { get; set; }
        public string? Referencia { get; set; }
        public int MonedaId { get; set; } = 1;
        public decimal TipoCambio { get; set; } = 1;
        public string? Notas { get; set; }
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/Pedido.cs =====

````csharp
namespace Kirkenta.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int ClienteId { get; set; }
        public int? CotizacionId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public DateTime? FechaEntrega { get; set; }
        public decimal Subtotal { get; set; } = 0;
        public decimal Descuento { get; set; } = 0;
        public decimal Impuestos { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public string Estado { get; set; } = "Pendiente";
        public string? Notas { get; set; }
        public int? FacturaId { get; set; }
        public int? UsuarioCreoId { get; set; }

        // ===== LOGÍSTICA =====
        public bool RequiereEnvio { get; set; } = false;
        public decimal MontoEnvio { get; set; } = 0;
    }

    public class DetallePedido
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public int ProductoId { get; set; }
        public string? Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; } = 0;
        public decimal ImpuestoPorcentaje { get; set; } = 0;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
    }
}
````

===== FILE: Models/Permiso.cs =====

````csharp
namespace Kirkenta.Models
{
    public class Permiso
    {
        public int Id { get; set; }
        public int RolId { get; set; }
        public string Modulo { get; set; } = string.Empty;
        public string? Submodulo { get; set; }
        public bool PuedeVer { get; set; } = true;
        public bool PuedeCrear { get; set; } = false;
        public bool PuedeEditar { get; set; } = false;
        public bool PuedeEliminar { get; set; } = false;
    }
}
````

===== FILE: Models/PermisoEmpleado.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Permiso o licencia de un empleado (con o sin goce de sueldo).
    /// </summary>
    public class PermisoEmpleado
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int EmpleadoId { get; set; }

        /// <summary>
        /// Personal | Enfermedad | Duelo | Maternidad | Paternidad | 
        /// Estudio | CitaMedica | Otro
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } = "Personal";

        /// <summary>
        /// Solicitado | Aprobado | Rechazado | Tomado | Cancelado
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Solicitado";

        // ===== PERÍODO =====
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal DiasSolicitados { get; set; }

        // ===== GOCE DE SUELDO =====
        public bool ConGoceSueldo { get; set; } = true;

        // ===== SOLICITUD =====
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        public int? UsuarioSolicitaId { get; set; }

        [Required]
        [StringLength(500)]
        public string Motivo { get; set; } = string.Empty;

        // ===== APROBACIÓN =====
        public int? UsuarioApruebaId { get; set; }
        public DateTime? FechaAprobacion { get; set; }

        [StringLength(500)]
        public string? MotivoRechazo { get; set; }

        /// <summary>
        /// Ruta de documento de soporte (certificado médico, etc.)
        /// </summary>
        [StringLength(300)]
        public string? RutaDocumentoSoporte { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/PlanCuenta.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Representa una cuenta en el Plan de Cuentas contable.
    /// La jerarquía se construye a partir del código (ej: 1, 1.1, 1.1.01).
    /// </summary>
    public class PlanCuenta
    {
        public int Id { get; set; }

        /// <summary>
        /// Código contable jerárquico (ej: 1.1.01). Debe ser único.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de la cuenta (ej: Caja General, Ventas).
        /// </summary>
        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Tipo contable principal (Activo, Pasivo, Patrimonio, Ingreso, Costo, Gasto).
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Tipo { get; set; } = "Activo";

        /// <summary>
        /// Código de la cuenta padre. Null para cuentas de primer nivel.
        /// </summary>
        [StringLength(20)]
        public string? CodigoPadre { get; set; }

        /// <summary>
        /// Nivel jerárquico (1 = mayor, 2 = subcuenta, 3 = auxiliar).
        /// </summary>
        public int Nivel { get; set; } = 1;

        /// <summary>
        /// Naturaleza: "Deudora" (Activo, Costo, Gasto) o "Acreedora" (Pasivo, Patrimonio, Ingreso).
        /// </summary>
        [StringLength(20)]
        public string Naturaleza { get; set; } = "Deudora";

        /// <summary>
        /// Si es true, permite recibir movimientos directamente.
        /// </summary>
        public bool EsMovimiento { get; set; } = false;

        public bool Activa { get; set; } = true;

        [StringLength(300)]
        public string? Descripcion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/Producto.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string? SKU { get; set; }

        [StringLength(50)]
        public string? CodigoBarras { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public int? CategoriaId { get; set; }
        public int? ImpuestoId { get; set; }

        public decimal PrecioCompra { get; set; } = 0;
        public decimal PrecioVenta { get; set; } = 0;
        public decimal PrecioMayorista { get; set; } = 0;
        public decimal Stock { get; set; } = 0;
        public decimal StockMinimo { get; set; } = 0;

        [StringLength(20)]
        public string UnidadMedida { get; set; } = "Unidad";

        [StringLength(300)]
        public string? Imagen { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
````

===== FILE: Models/Proveedor.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Proveedor
    {
        public int Id { get; set; }

        [StringLength(20)]
        public string? Codigo { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string? RazonSocial { get; set; }

        [StringLength(20)]
        public string? RTN { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        public string? Contacto { get; set; }

        [StringLength(30)]
        public string? TelefonoContacto { get; set; }

        [StringLength(300)]
        public string? Direccion { get; set; }

        [StringLength(100)]
        public string? Ciudad { get; set; }

        [StringLength(80)]
        public string? Pais { get; set; } = "Honduras";

        public string CondicionPago { get; set; } = "Contado";
        public int DiasCredito { get; set; } = 0;
        public decimal LimiteCredito { get; set; } = 0;

        [StringLength(100)]
        public string? Banco { get; set; }

        [StringLength(50)]
        public string? CuentaBancaria { get; set; }

        public int MonedaId { get; set; } = 1;

        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/Repartidor.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Repartidor / motorista que realiza los envíos.
    /// Se asigna a una Ruta al momento de despachar.
    /// </summary>
    public class Repartidor
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        public string? Vehiculo { get; set; }         // Ej: "Moto", "Camión Isuzu"

        [StringLength(20)]
        public string? Placa { get; set; }

        [StringLength(50)]
        public string? Licencia { get; set; }

        [StringLength(300)]
        public string? Notas { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/Rol.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class Rol
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Descripcion { get; set; }

        [StringLength(20)]
        public string Color { get; set; } = "#6b7280";

        public bool EsSistema { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
````

===== FILE: Models/Ruta.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Ruta de despacho. Agrupa varios Envíos (paradas) para que un repartidor
    /// los entregue en un solo viaje.
    /// </summary>
    public class Ruta
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.Today;

        // ===== REPARTIDOR / VEHÍCULO =====
        public int? RepartidorId { get; set; }

        [StringLength(150)]
        public string RepartidorNombre { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Vehiculo { get; set; }

        [StringLength(20)]
        public string? Placa { get; set; }

        // ===== ESTADO GENERAL =====
        /// <summary>
        /// Borrador | Despachada | EnReparto | Completada | Cancelada
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Borrador";

        // ===== ZONA / DESCRIPCIÓN =====
        public int? ZonaId { get; set; }

        [StringLength(100)]
        public string? ZonaNombre { get; set; }

        [StringLength(300)]
        public string? Descripcion { get; set; }

        // ===== FECHAS =====
        public DateTime? FechaSalida { get; set; }
        public DateTime? FechaRegreso { get; set; }

        // ===== TOTALES (denormalizados para reportes) =====
        public int TotalParadas { get; set; }
        public int ParadasEntregadas { get; set; }
        public int ParadasFallidas { get; set; }
        public decimal MontoTotalEnvios { get; set; }

        // ===== TRACKING PÚBLICO =====
        /// <summary>
        /// Código único para que el cliente pueda consultar el estado
        /// de su envío sin autenticarse. Ej: KRT-2026-AB12X9
        /// </summary>
        [StringLength(30)]
        public string? TrackingCode { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/RutaHistorial.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Historial de eventos de una Ruta o de sus Envíos.
    /// Sirve para trazabilidad y para mostrar la línea de tiempo.
    /// </summary>
    public class RutaHistorial
    {
        public int Id { get; set; }

        public int RutaId { get; set; }

        /// <summary>
        /// Null si el evento es a nivel Ruta.
        /// Se llena si el evento es sobre una parada específica.
        /// </summary>
        public int? EnvioId { get; set; }

        [Required]
        [StringLength(100)]
        public string Evento { get; set; } = string.Empty;   // "RutaCreada" | "Despachada" | "Entregado" | "Fallido"

        [StringLength(500)]
        public string? Detalle { get; set; }

        [StringLength(30)]
        public string? EstadoAnterior { get; set; }

        [StringLength(30)]
        public string? EstadoNuevo { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public int? UsuarioId { get; set; }

        [StringLength(150)]
        public string? UsuarioNombre { get; set; }
    }
}
````

===== FILE: Models/SerieDocumento.cs =====

````csharp
namespace Kirkenta.Models
{
    public class SerieDocumento
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;      // Cotizacion | Factura | Pedido | Venta | Devolucion
        public string Nombre { get; set; } = "Principal";
        public string Prefijo { get; set; } = string.Empty;
        public string? Sufijo { get; set; }
        public string Separador { get; set; } = "-";
        public int LongitudNumero { get; set; } = 4;
        public int SiguienteNumero { get; set; } = 1;
        public string? FormatoPersonalizado { get; set; }
        public bool EsPredeterminada { get; set; } = false;
        public bool Activa { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
````

===== FILE: Models/TipoDocumentoEmpleado.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Categoría de documento que se puede adjuntar a un empleado.
    /// Ejemplos: DNI, RTN, Antecedentes policiales, Contrato, etc.
    /// </summary>
    public class TipoDocumentoEmpleado
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Personales | Antecedentes | Referencias | Academicos | Contrato | Medicos | Otros
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Categoria { get; set; } = "Otros";

        /// <summary>
        /// Si es obligatorio que todos los empleados lo tengan
        /// </summary>
        public bool EsObligatorio { get; set; } = false;

        /// <summary>
        /// Si el documento tiene fecha de vencimiento
        /// </summary>
        public bool RequiereVencimiento { get; set; } = false;

        /// <summary>
        /// Días antes del vencimiento para alertar
        /// </summary>
        public int? DiasAlertaVencimiento { get; set; }

        [StringLength(100)]
        public string? Icono { get; set; }

        public bool EsSistema { get; set; } = false;
        public bool Activo { get; set; } = true;
        public int Orden { get; set; } = 0;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
````

===== FILE: Models/TramoISR.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Tramos del ISR por año fiscal.
    /// Ejemplo Honduras 2024 (mensual):
    ///   Hasta L. 15,692.25 → 0%
    ///   L. 15,692.26 a L. 23,246.20 → 15% sobre excedente
    ///   L. 23,246.21 a L. 55,170.68 → L. 1,133.09 + 20% sobre excedente
    ///   Más de L. 55,170.68 → L. 7,517.98 + 25% sobre excedente
    /// </summary>
    public class TramoISR
    {
        public int Id { get; set; }

        public int ConfiguracionDeduccionId { get; set; }

        /// <summary>
        /// Orden del tramo (1, 2, 3, ...)
        /// </summary>
        public int Orden { get; set; }

        /// <summary>
        /// Desde cuánto aplica (en L.)
        /// </summary>
        public decimal Desde { get; set; }

        /// <summary>
        /// Hasta cuánto aplica (en L.). Null = sin límite
        /// </summary>
        public decimal? Hasta { get; set; }

        /// <summary>
        /// Porcentaje sobre el excedente
        /// </summary>
        [Range(0, 100)]
        public decimal Porcentaje { get; set; }

        /// <summary>
        /// Monto fijo que se suma (si el tramo lo requiere)
        /// </summary>
        public decimal MontoFijo { get; set; } = 0;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
````

===== FILE: Models/UnidadMedida.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    public class UnidadMedida
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La abreviatura es obligatoria")]
        [StringLength(10)]
        public string Abreviatura { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
````

===== FILE: Models/Usuario.cs =====

````csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kirkenta.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? NombreCompleto { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        public string Rol { get; set; } = "User";

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? UltimoAcceso { get; set; }

        public int IntentosFallidos { get; set; } = 0;

        public DateTime? BloqueadoHasta { get; set; }
    }
}
````

===== FILE: Models/VacacionEmpleado.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Solicitud y registro de vacaciones de un empleado.
    /// Flujo: Solicitado → Aprobado → Tomado (o Rechazado/Cancelado).
    /// </summary>
    public class VacacionEmpleado
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int EmpleadoId { get; set; }

        /// <summary>
        /// Solicitado | Aprobado | Rechazado | Tomado | Cancelado
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Solicitado";

        // ===== PERÍODO =====
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        /// <summary>
        /// Días hábiles a tomar (sin contar feriados)
        /// </summary>
        public decimal DiasSolicitados { get; set; }

        /// <summary>
        /// Feriados que caen en el período
        /// </summary>
        public int DiasFeriados { get; set; } = 0;

        /// <summary>
        /// Días que se descuentan (solicitados - feriados)
        /// </summary>
        public decimal DiasADescontar { get; set; }

        // ===== SOLICITUD =====
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        public int? UsuarioSolicitaId { get; set; }

        [StringLength(500)]
        public string? Motivo { get; set; }

        // ===== APROBACIÓN =====
        public int? UsuarioApruebaId { get; set; }
        public DateTime? FechaAprobacion { get; set; }

        [StringLength(500)]
        public string? MotivoRechazo { get; set; }

        // ===== TOMA EFECTIVA =====
        public DateTime? FechaTomaReal { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public int EmpresaId { get; set; } = 1;
    }
}
````

===== FILE: Models/ValeEmpleado.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Vale o adelanto de salario a un empleado.
    /// Flujo: Solicitado → AprobadoGerente → AprobadoRRHH → Entregado → Descontado
    /// RRHH descuenta del salario en la nómina (1 o varias cuotas).
    /// </summary>
    public class ValeEmpleado
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Numero { get; set; } = string.Empty;

        public int EmpleadoId { get; set; }

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        public DateTime? FechaEntrega { get; set; }

        public decimal Monto { get; set; }

        [Required]
        [StringLength(300)]
        public string Motivo { get; set; } = string.Empty;

        /// <summary>
        /// Solicitado | AprobadoGerente | AprobadoRRHH | Entregado | Descontado | Rechazado | Cancelado
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Solicitado";

        // ===== APROBACIONES =====
        public int? AprobadoPorGerenteId { get; set; }
        public DateTime? FechaAprobacionGerente { get; set; }

        public int? AprobadoPorRRHHId { get; set; }
        public DateTime? FechaAprobacionRRHH { get; set; }

        public int? EntregadoPorId { get; set; }

        [StringLength(500)]
        public string? MotivoRechazo { get; set; }

        // ===== DESCUENTO =====
        public int Cuotas { get; set; } = 1;
        public decimal MontoCuota { get; set; } = 0;
        public DateTime? FechaPrimerDescuento { get; set; }
        public decimal SaldoPendiente { get; set; } = 0;

        // ===== CUENTA DE PAGO =====
        public int? CuentaId { get; set; }

        // ===== MOVIMIENTO FINANCIERO =====
        public int? MovimientoId { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? UsuarioCreoId { get; set; }
        public int EmpresaId { get; set; } = 1;
    }

    /// <summary>
    /// Detalle de cada descuento aplicado a un vale (una cuota por nómina).
    /// </summary>
    public class ValeDescuento
    {
        public int Id { get; set; }

        public int ValeEmpleadoId { get; set; }

        public int NumeroCuota { get; set; }

        public decimal Monto { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        /// <summary>
        /// ID de la nómina en la que se aplicó
        /// </summary>
        public int? NominaId { get; set; }

        [StringLength(200)]
        public string? Notas { get; set; }
    }
}
````

===== FILE: Models/Venta.cs =====

````csharp
namespace Kirkenta.Models
{
    public class Venta
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int? ClienteId { get; set; }
        public int? CotizacionId { get; set; }
        public int? PedidoId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Subtotal { get; set; } = 0;
        public decimal Descuento { get; set; } = 0;
        public decimal Impuestos { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public string Estado { get; set; } = "Completada";
        public string? Notas { get; set; }
        public int? UsuarioCreoId { get; set; }

        // ===== LOGÍSTICA =====
        /// <summary>
        /// Si esta venta requiere envío a domicilio.
        /// </summary>
        public bool RequiereEnvio { get; set; } = false;

        /// <summary>
        /// Monto cobrado por el envío. Se suma al Subtotal antes de ISV.
        /// 0 = envío gratis.
        /// </summary>
        public decimal MontoEnvio { get; set; } = 0;
    }

    public class DetalleVenta
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public int ProductoId { get; set; }
        public string? Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; } = 0;
        public decimal ImpuestoPorcentaje { get; set; } = 0;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
    }
}
````

===== FILE: Models/ZonaEnvio.cs =====

````csharp
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Models
{
    /// <summary>
    /// Zona de envío (Centro, Cercano, Lejano, etc.)
    /// Define un precio sugerido que el cajero puede usar o sobreescribir manualmente.
    /// </summary>
    public class ZonaEnvio
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Precio sugerido. El cajero puede sobreescribirlo al momento de la venta.
        /// </summary>
        public decimal PrecioSugerido { get; set; } = 0;

        [StringLength(20)]
        public string Color { get; set; } = "#6b7280";

        public bool Activa { get; set; } = true;

        public int Orden { get; set; } = 0;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int EmpresaId { get; set; } = 1;
    }
}
````

====================================================
 Pages - 8 archivo(s)
====================================================

===== FILE: Pages/_ViewImports.cshtml =====

````html
@using Kirkenta
@namespace Kirkenta.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

````

===== FILE: Pages/_ViewStart.cshtml =====

````html
@{
    Layout = "_Layout";
}
````

===== FILE: Pages/Error.cshtml =====

````html
@page
@model ErrorModel
@{
    ViewData["Title"] = "Error";
}

<h1 class="text-danger">Error.</h1>
<h2 class="text-danger">An error occurred while processing your request.</h2>

@if (Model.ShowRequestId)
{
    <p>
        <strong>Request ID:</strong> <code>@Model.RequestId</code>
    </p>
}

<h3>Development Mode</h3>
<p>
    Swapping to the <strong>Development</strong> environment displays detailed information about the error that occurred.
</p>
<p>
    <strong>The Development environment shouldn't be enabled for deployed applications.</strong>
    It can result in displaying sensitive information from exceptions to end users.
    For local debugging, enable the <strong>Development</strong> environment by setting the <strong>ASPNETCORE_ENVIRONMENT</strong> environment variable to <strong>Development</strong>
    and restarting the app.
</p>

````

===== FILE: Pages/Error.cshtml.cs =====

````csharp
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}


````

===== FILE: Pages/Index.cshtml =====

````html
@page
@model IndexModel
@{
    ViewData["Title"] = "Dashboard";
}

<div class="dashboard">
    <div class="dashboard-header">
        <h1>Hola, @User.Identity?.Name 👋</h1>
        <p>Este es el resumen de tu operación</p>
    </div>

    <!-- KPIs reales -->
    <div class="kpi-grid">
        <div class="kpi-card">
            <span class="kpi-label">Ventas de hoy</span>
            <span class="kpi-value">L. @Model.VentasHoy.ToString("N2")</span>
            <span class="kpi-trend @(Model.VentasHoy >= Model.VentasAyer ? "up" : "down")">
                @(Model.VentasHoy >= Model.VentasAyer ? "↑" : "↓")
                @(Model.VentasAyer > 0 ? Math.Abs((Model.VentasHoy - Model.VentasAyer) / Model.VentasAyer * 100).ToString("N1") : "0")% vs ayer
            </span>
        </div>
        <div class="kpi-card">
            <span class="kpi-label">Ventas del mes</span>
            <span class="kpi-value">L. @Model.VentasMes.ToString("N2")</span>
            <span class="kpi-trend @(Model.VentasMes >= Model.VentasMesAnterior ? "up" : "down")">
                @(Model.VentasMes >= Model.VentasMesAnterior ? "↑" : "↓")
                @(Model.VentasMesAnterior > 0 ? Math.Abs((Model.VentasMes - Model.VentasMesAnterior) / Model.VentasMesAnterior * 100).ToString("N1") : "0")% vs mes anterior
            </span>
        </div>
        <div class="kpi-card">
            <span class="kpi-label">Clientes activos</span>
            <span class="kpi-value">@Model.ClientesActivos</span>
            <span class="kpi-trend">Total registrados: @Model.ClientesTotal</span>
        </div>
        <div class="kpi-card">
            <span class="kpi-label">Productos en stock</span>
            <span class="kpi-value">@Model.ProductosTotal</span>
            <span class="kpi-trend @(Model.ProductosStockBajo > 0 ? "down" : "up")">
                @if (Model.ProductosStockBajo > 0)
                {
                    <span>⚠️ @Model.ProductosStockBajo con stock bajo</span>
                }
                else
                {
                    <span>✅ Stock saludable</span>
                }
            </span>
        </div>
    </div>

    <!-- Accesos rápidos -->
    <div class="section">
        <h2 class="section-title">Accesos rápidos</h2>
        <div class="quick-grid">
            <a asp-page="/POS/Index" class="quick-action">
                <span class="quick-icon">🛒</span>
                <span class="quick-label">Punto de venta</span>
            </a>
            <a asp-page="/Productos/Create" class="quick-action">
                <span class="quick-icon">📦</span>
                <span class="quick-label">Nuevo producto</span>
            </a>
            <a asp-page="/Clientes/Create" class="quick-action">
                <span class="quick-icon">👤</span>
                <span class="quick-label">Nuevo cliente</span>
            </a>
            <a asp-page="/Cotizaciones/Create" class="quick-action">
                <span class="quick-icon">📝</span>
                <span class="quick-label">Nueva cotización</span>
            </a>
            <a asp-page="/Reportes/Ventas" class="quick-action">
                <span class="quick-icon">📊</span>
                <span class="quick-label">Reportes</span>
            </a>
            <a asp-page="/Facturas/Index" class="quick-action">
                <span class="quick-icon">🧾</span>
                <span class="quick-label">Facturas</span>
            </a>
        </div>
    </div>

    <!-- Gráfico de ventas últimos 7 días -->
    <div class="module-card" style="padding: 20px;">
        <h3 class="panel-title" style="margin-bottom: 16px;">Ventas de los últimos 7 días</h3>
        <div style="height: 300px;">
            <canvas id="chartDashboardVentas"></canvas>
        </div>
    </div>

    <!-- Panel doble: Top productos + Últimas ventas -->
    <div class="panel-grid">
        <div class="module-card" style="padding: 20px;">
            <h3 class="panel-title" style="margin-bottom: 16px;">🏆 Top 5 productos del mes</h3>
            @if (Model.TopProductosMes.Count == 0)
            {
                <p style="color: var(--color-muted); text-align: center; padding: 40px;">Sin ventas este mes</p>
            }
            else
            {
                <div class="table-wrapper">
                    <table class="erp-table">
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>Producto</th>
                                <th style="text-align: right;">Cantidad</th>
                                <th style="text-align: right;">Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            @{
                                var i = 1;
                            }
                            @foreach (var p in Model.TopProductosMes)
                            {
                                <tr>
                                    <td><strong>@i</strong></td>
                                    <td>@p.Nombre</td>
                                    <td style="text-align: right;">@p.Cantidad</td>
                                    <td style="text-align: right;"><strong>L. @p.Total.ToString("N2")</strong></td>
                                </tr>
                                i++;
                            }
                        </tbody>
                    </table>
                </div>
            }
        </div>

        <div class="module-card" style="padding: 20px;">
            <h3 class="panel-title" style="margin-bottom: 16px;">📋 Últimas ventas</h3>
            @if (Model.UltimasVentas.Count == 0)
            {
                <p style="color: var(--color-muted); text-align: center; padding: 40px;">Aún no hay ventas</p>
            }
            else
            {
                <div class="timeline">
                    @foreach (var v in Model.UltimasVentas)
                    {
                        <div class="timeline-item">
                            <div class="timeline-dot"></div>
                            <div class="timeline-content">
                                <div class="timeline-header">
                                    <strong>@v.Numero</strong>
                                    <span class="timeline-date">@v.Fecha.ToString("dd/MM HH:mm")</span>
                                </div>
                                <p class="timeline-detail">@v.ClienteNombre — <strong>L. @v.Total.ToString("N2")</strong></p>
                            </div>
                        </div>
                    }
                </div>
            }
        </div>
    </div>
</div>

@section Scripts {
    <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
    <script>
        (function () {
            var ventasData = @Html.Raw(Model.Ventas7DiasJson);
            var ctx = document.getElementById('chartDashboardVentas');
            if (ctx && ventasData.labels && ventasData.labels.length > 0) {
                new Chart(ctx, {
                    type: 'bar',
                    data: {
                        labels: ventasData.labels,
                        datasets: [{
                            label: 'Ventas (L.)',
                            data: ventasData.data,
                            backgroundColor: 'rgba(79, 70, 229, 0.85)',
                            borderRadius: 8,
                            borderSkipped: false
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { display: false },
                            tooltip: {
                                callbacks: {
                                    label: function (context) {
                                        return 'L. ' + context.parsed.y.toLocaleString('es-HN', { minimumFractionDigits: 2 });
                                    }
                                }
                            }
                        },
                        scales: {
                            y: {
                                beginAtZero: true,
                                ticks: {
                                    callback: function (value) { return 'L. ' + value.toLocaleString(); }
                                }
                            }
                        }
                    }
                });
            }
        })();
    </script>
}
````

===== FILE: Pages/Index.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Kirkenta.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // KPIs
        public decimal VentasHoy { get; set; }
        public decimal VentasAyer { get; set; }
        public decimal VentasMes { get; set; }
        public decimal VentasMesAnterior { get; set; }

        public int ClientesActivos { get; set; }
        public int ClientesTotal { get; set; }
        public int ProductosTotal { get; set; }
        public int ProductosStockBajo { get; set; }

        // Datos para gráficos y paneles
        public string Ventas7DiasJson { get; set; } = "{}";
        public List<TopProductoMes> TopProductosMes { get; set; } = new();
        public List<VentaReciente> UltimasVentas { get; set; } = new();

        public class TopProductoMes
        {
            public string Nombre { get; set; } = "";
            public decimal Cantidad { get; set; }
            public decimal Total { get; set; }
        }

        public class VentaReciente
        {
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public decimal Total { get; set; }
            public string ClienteNombre { get; set; } = "Consumidor final";
        }

        public void OnGet()
        {
            var hoy = DateTime.Today;
            var ayer = hoy.AddDays(-1);
            var manana = hoy.AddDays(1);

            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1);
            var inicioMesAnterior = inicioMes.AddMonths(-1);
            var finMesAnterior = inicioMes;

            // === KPIs ===
            VentasHoy = _context.Ventas
                .Where(v => v.Fecha >= hoy && v.Fecha < manana && v.Estado == "Completada")
                .Sum(v => (decimal?)v.Total) ?? 0;

            VentasAyer = _context.Ventas
                .Where(v => v.Fecha >= ayer && v.Fecha < hoy && v.Estado == "Completada")
                .Sum(v => (decimal?)v.Total) ?? 0;

            VentasMes = _context.Ventas
                .Where(v => v.Fecha >= inicioMes && v.Fecha < finMes && v.Estado == "Completada")
                .Sum(v => (decimal?)v.Total) ?? 0;

            VentasMesAnterior = _context.Ventas
                .Where(v => v.Fecha >= inicioMesAnterior && v.Fecha < finMesAnterior && v.Estado == "Completada")
                .Sum(v => (decimal?)v.Total) ?? 0;

            ClientesActivos = _context.Clientes.Count(c => c.Activo);
            ClientesTotal = _context.Clientes.Count();
            ProductosTotal = _context.Productos.Count(p => p.Activo);
            ProductosStockBajo = _context.Productos.Count(p => p.Activo && p.Stock <= p.StockMinimo);

            // === Ventas últimos 7 días ===
            var hace7Dias = hoy.AddDays(-6);
            var ventas7 = _context.Ventas
                .Where(v => v.Fecha >= hace7Dias && v.Estado == "Completada")
                .ToList();

            var porDia = new List<(string Label, decimal Total)>();
            for (int i = 0; i < 7; i++)
            {
                var dia = hace7Dias.AddDays(i);
                var totalDia = ventas7
                    .Where(v => v.Fecha.Date == dia.Date)
                    .Sum(v => v.Total);
                porDia.Add((dia.ToString("ddd dd/MM"), totalDia));
            }

            Ventas7DiasJson = JsonSerializer.Serialize(new
            {
                labels = porDia.Select(x => x.Label).ToArray(),
                data = porDia.Select(x => x.Total).ToArray()
            });

            // === Top 5 productos del mes ===
            var ventaIdsMes = _context.Ventas
                .Where(v => v.Fecha >= inicioMes && v.Fecha < finMes && v.Estado == "Completada")
                .Select(v => v.Id)
                .ToList();

            var detallesMes = _context.DetalleVentas
                .Where(d => ventaIdsMes.Contains(d.VentaId))
                .ToList();

            var productos = _context.Productos.ToList();

            TopProductosMes = detallesMes
                .GroupBy(d => d.ProductoId)
                .Select(g => new TopProductoMes
                {
                    Nombre = productos.FirstOrDefault(p => p.Id == g.Key)?.Nombre ?? "—",
                    Cantidad = g.Sum(d => d.Cantidad),
                    Total = g.Sum(d => d.Total)
                })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToList();

            // === Últimas 5 ventas ===
            var ultimas = _context.Ventas
                .Where(v => v.Estado == "Completada")
                .OrderByDescending(v => v.Fecha)
                .Take(5)
                .ToList();

            var clientes = _context.Clientes.ToList();

            UltimasVentas = ultimas.Select(v => new VentaReciente
            {
                Numero = v.Numero,
                Fecha = v.Fecha,
                Total = v.Total,
                ClienteNombre = v.ClienteId.HasValue
                    ? (clientes.FirstOrDefault(c => c.Id == v.ClienteId.Value)?.Nombre ?? "Consumidor final")
                    : "Consumidor final"
            }).ToList();
        }
    }
}
````

===== FILE: Pages/Privacy.cshtml =====

````html
@page
@model PrivacyModel
@{
    ViewData["Title"] = "Privacy Policy";
}
<h1>@ViewData["Title"]</h1>

<p>Use this page to detail your site's privacy policy.</p>

````

===== FILE: Pages/Privacy.cshtml.cs =====

````csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages;

public class PrivacyModel : PageModel
{
    public void OnGet()
    {
    }
}


````

====================================================
 Pages\Auth - 7 archivo(s)
====================================================

===== FILE: Pages/Auth/_ViewStart.cshtml =====

````html
@{
    Layout = null;
}
````

===== FILE: Pages/Auth/Login.cshtml =====

````html
@page
@model Kirkenta.Pages.Auth.LoginModel
@{
    Layout = null;
    ViewData["Title"] = "Iniciar sesión";
}
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - Kirkenta ERP</title>
    <link rel="icon" type="image/png" href="~/favicon.png?v=@DateTime.Now.Ticks" />
    <link rel="apple-touch-icon" href="~/favicon.png" />
    <link rel="shortcut icon" type="image/png" href="~/favicon.png" />
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/theme.css?v=@DateTime.Now.Ticks" />
</head>
<body class="auth-body">
    <div class="auth-wrapper">
        <div class="auth-card">
            <div class="auth-brand">
                <img src="~/images/logo.png" alt="Kirkenta ERP" class="auth-logo" />
                <h1 class="auth-title">Kirkenta ERP</h1>
                <p class="auth-subtitle">Ingresa a tu cuenta</p>
            </div>

            @if (!ViewData.ModelState.IsValid)
            {
                <div class="auth-alert auth-alert-error" role="alert">
                    <svg class="auth-alert-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <circle cx="12" cy="12" r="10"/>
                        <line x1="12" y1="8" x2="12" y2="12"/>
                        <line x1="12" y1="16" x2="12.01" y2="16"/>
                    </svg>
                    <span asp-validation-summary="ModelOnly"></span>
                </div>
            }

            <form method="post" class="auth-form">
                <div class="form-field">
                    <label asp-for="Username">Usuario</label>
                    <input asp-for="Username"
                           class="form-input"
                           autocomplete="username"
                           placeholder="tu.usuario"
                           autofocus />
                </div>

                <div class="form-field">
                    <label asp-for="Password">Contraseña</label>
                    <div class="form-input-wrapper">
                        <input asp-for="Password"
                               id="passwordInput"
                               type="password"
                               class="form-input"
                               autocomplete="current-password"
                               placeholder="••••••••" />
                        <button type="button"
                                class="password-toggle"
                                id="passwordToggle"
                                aria-label="Mostrar contraseña"
                                tabindex="-1">
                            <svg class="eye-open" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                                <circle cx="12" cy="12" r="3"/>
                            </svg>
                            <svg class="eye-closed" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none;">
                                <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                                <line x1="1" y1="1" x2="23" y2="23"/>
                            </svg>
                        </button>
                    </div>
                </div>

                <button type="submit" class="btn-auth">Iniciar sesión</button>
            </form>

            <div class="auth-footer">
                ¿No tienes cuenta? <a asp-page="/Auth/Register">Regístrate</a>
            </div>
        </div>
    </div>

    <script>
        (function () {
            var toggle = document.getElementById('passwordToggle');
            var input = document.getElementById('passwordInput');
            if (!toggle || !input) return;
            var eyeOpen = toggle.querySelector('.eye-open');
            var eyeClosed = toggle.querySelector('.eye-closed');
            toggle.addEventListener('click', function () {
                var isPassword = input.type === 'password';
                input.type = isPassword ? 'text' : 'password';
                eyeOpen.style.display = isPassword ? 'none' : 'block';
                eyeClosed.style.display = isPassword ? 'block' : 'none';
                input.focus();
            });
        })();
    </script>
</body>
</html>
````

===== FILE: Pages/Auth/Login.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Kirkenta.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError(string.Empty, "Ingresa tu usuario y contraseña.");
                return Page();
            }

            var user = _context.Usuarios.FirstOrDefault(u => u.Username == Username);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
                return Page();
            }

            if (!user.Activo)
            {
                ModelState.AddModelError(string.Empty, "Tu cuenta está desactivada. Contacta al administrador.");
                return Page();
            }

            if (!BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                user.IntentosFallidos++;
                _context.SaveChanges();

                var ipFallido = HttpContext.Connection.RemoteIpAddress?.ToString();
                var uaFallido = Request.Headers["User-Agent"].ToString();

                ActividadHelper.Registrar(
                    _context,
                    user.Id,
                    "Login fallido",
                    $"Intento fallido #{user.IntentosFallidos}",
                    ipFallido,
                    uaFallido);

                ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
                return Page();
            }

            // ✅ Login exitoso
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            user.UltimoAcceso = DateTime.Now;
            user.IntentosFallidos = 0;
            _context.SaveChanges();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Rol ?? "User")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(8)
                });

            ActividadHelper.Registrar(
                _context,
                user.Id,
                "Inicio de sesión",
                "Ingresó al sistema",
                ip,
                userAgent);

            return RedirectToPage("/Index");
        }
    }
}
````

===== FILE: Pages/Auth/Logout.cshtml =====

````html
@page
@model Kirkenta.Pages.Auth.LogoutModel
@{
    Layout = null;
}
````

===== FILE: Pages/Auth/Logout.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Kirkenta.Pages.Auth
{
    public class LogoutModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LogoutModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out var userId))
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                ActividadHelper.Registrar(
                    _context,
                    userId,
                    "Cierre de sesión",
                    "Salió del sistema",
                    ip);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Auth/Login");
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("/Auth/Login");
        }
    }
}
````

===== FILE: Pages/Auth/Register.cshtml =====

````html
@page
@model Kirkenta.Pages.Auth.RegisterModel
@{
    Layout = null;
    ViewData["Title"] = "Crear cuenta";
}
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - Kirkenta ERP</title>
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/theme.css?v=@DateTime.Now.Ticks" />
</head>
<body class="auth-body">
    <div class="auth-wrapper">
        <div class="auth-card">
            <div class="auth-brand">
                <img src="~/images/logo.png" alt="Kirkenta ERP" class="auth-logo" />
                <h1 class="auth-title">Crear cuenta</h1>
                <p class="auth-subtitle">Regístrate en Kirkenta ERP</p>
            </div>

            <!-- ⚠️ MENSAJE DE ERROR -->
            @if (!ViewData.ModelState.IsValid)
            {
                <div class="auth-alert auth-alert-error" role="alert">
                    <svg class="auth-alert-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <circle cx="12" cy="12" r="10"/>
                        <line x1="12" y1="8" x2="12" y2="12"/>
                        <line x1="12" y1="16" x2="12.01" y2="16"/>
                    </svg>
                    <span asp-validation-summary="ModelOnly"></span>
                </div>
            }

            <form method="post" class="auth-form">
                <div class="form-field">
                    <label asp-for="Username">Usuario</label>
                    <input asp-for="Username" class="form-input" placeholder="tu.usuario" autocomplete="username" />
                </div>
                <div class="form-field">
                    <label asp-for="Email">Email</label>
                    <input asp-for="Email" type="email" class="form-input" placeholder="tu@correo.com" autocomplete="email" />
                </div>
                <div class="form-field">
                    <label asp-for="Password">Contraseña</label>
                    <div class="form-input-wrapper">
                        <input asp-for="Password"
                               id="passwordInput"
                               type="password"
                               class="form-input"
                               autocomplete="new-password"
                               placeholder="••••••••" />
                        <button type="button"
                                class="password-toggle"
                                id="passwordToggle"
                                aria-label="Mostrar contraseña"
                                tabindex="-1">
                            <svg class="eye-open" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                                <circle cx="12" cy="12" r="3"/>
                            </svg>
                            <svg class="eye-closed" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none;">
                                <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                                <line x1="1" y1="1" x2="23" y2="23"/>
                            </svg>
                        </button>
                    </div>
                </div>
                <div class="form-field">
                    <label asp-for="ConfirmPassword">Confirmar contraseña</label>
                    <div class="form-input-wrapper">
                        <input asp-for="ConfirmPassword"
                               id="confirmInput"
                               type="password"
                               class="form-input"
                               autocomplete="new-password"
                               placeholder="••••••••" />
                        <button type="button"
                                class="password-toggle"
                                id="confirmToggle"
                                aria-label="Mostrar contraseña"
                                tabindex="-1">
                            <svg class="eye-open" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                                <circle cx="12" cy="12" r="3"/>
                            </svg>
                            <svg class="eye-closed" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none;">
                                <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                                <line x1="1" y1="1" x2="23" y2="23"/>
                            </svg>
                        </button>
                    </div>
                </div>

                <button type="submit" class="btn-auth">Crear cuenta</button>
            </form>

            <div class="auth-footer">
                ¿Ya tienes cuenta? <a asp-page="/Auth/Login">Inicia sesión</a>
            </div>
        </div>
    </div>

    <script>
        (function () {
            function bindToggle(toggleId, inputId) {
                var toggle = document.getElementById(toggleId);
                var input = document.getElementById(inputId);
                if (!toggle || !input) return;

                var eyeOpen = toggle.querySelector('.eye-open');
                var eyeClosed = toggle.querySelector('.eye-closed');

                toggle.addEventListener('click', function () {
                    var isPassword = input.type === 'password';
                    input.type = isPassword ? 'text' : 'password';
                    eyeOpen.style.display = isPassword ? 'none' : 'block';
                    eyeClosed.style.display = isPassword ? 'block' : 'none';
                    toggle.setAttribute('aria-label', isPassword ? 'Ocultar contraseña' : 'Mostrar contraseña');
                    input.focus();
                });
            }

            bindToggle('passwordToggle', 'passwordInput');
            bindToggle('confirmToggle', 'confirmInput');
        })();
    </script>
</body>
</html>
````

===== FILE: Pages/Auth/Register.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegisterModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        public void OnGet() { }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Username) || Username.Length < 3)
            {
                ModelState.AddModelError(string.Empty, "El usuario debe tener al menos 3 caracteres");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ModelState.AddModelError(string.Empty, "El email es obligatorio");
                return Page();
            }

            if (Password != ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Las contraseñas no coinciden");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            {
                ModelState.AddModelError(string.Empty, "La contraseña debe tener al menos 6 caracteres");
                return Page();
            }

            if (_context.Usuarios.Any(u => u.Username == Username))
            {
                ModelState.AddModelError(string.Empty, "El usuario ya existe");
                return Page();
            }

            if (_context.Usuarios.Any(u => u.Email == Email))
            {
                ModelState.AddModelError(string.Empty, "El email ya está registrado");
                return Page();
            }

            var user = new Usuario
            {
                Username = Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                Rol = "Pendiente",
                Email = Email,
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            _context.Usuarios.Add(user);
            _context.SaveChanges();

            TempData["RegisterMessage"] = "Registro exitoso ✅ Espera a que un administrador te asigne un rol.";
            return RedirectToPage("/Auth/Login");
        }
    }
}
````

====================================================
 Pages\Configuracion - 4 archivo(s)
====================================================

===== FILE: Pages/Configuracion/Edit.cshtml =====

````html
@page
@model Kirkenta.Pages.Configuracion.EditModel
@{
    ViewData["Title"] = "Editar configuración de empresa";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Configuracion/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a configuración
        </a>
        <h2>Editar configuración</h2>
    </div>
</div>

<form method="post" enctype="multipart/form-data">
    <input type="hidden" asp-for="Input.Id" />
    <input type="hidden" asp-for="Input.LogoPath" />

    <div class="module-card form-card-wide">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span asp-validation-summary="ModelOnly"></span>
            </div>
        }

        <!-- Logo -->
        <h3 class="form-section-title">Logo de la empresa</h3>
        <p class="form-section-desc">Sube el logo (PNG, JPG — máximo 2 MB). Se mostrará en cotizaciones y facturas.</p>

        <div class="logo-upload-wrapper">
            <div class="logo-preview">
                @if (!string.IsNullOrEmpty(Model.Input.LogoPath))
                {
                    <img src="@Model.Input.LogoPath" alt="Logo empresa" id="logoPreviewImg" />
                }
                else
                {
                    <div class="logo-preview-placeholder" id="logoPreviewPlaceholder">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
                            <circle cx="8.5" cy="8.5" r="1.5"/>
                            <polyline points="21 15 16 10 5 21"/>
                        </svg>
                        <span>Sin logo</span>
                    </div>
                    <img src="" alt="" id="logoPreviewImg" style="display: none;" />
                }
            </div>
            <div class="logo-upload-controls">
                <input type="file" asp-for="LogoFile" accept="image/png,image/jpeg,image/jpg,image/webp" id="logoFileInput" style="display: none;" />
                <button type="button" class="btn-secondary" onclick="document.getElementById('logoFileInput').click();">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                        <polyline points="17 8 12 3 7 8"/>
                        <line x1="12" y1="3" x2="12" y2="15"/>
                    </svg>
                    Subir logo
                </button>
                <p class="form-help">PNG, JPG, WEBP. Máx 2 MB.</p>
            </div>
        </div>

        <div class="form-divider"></div>

        <!-- Datos básicos -->
        <h3 class="form-section-title">Información de la empresa</h3>

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Nombre">Nombre comercial *</label>
                <input asp-for="Input.Nombre" class="form-input" required />
            </div>
            <div class="form-field">
                <label asp-for="Input.RazonSocial">Razón social</label>
                <input asp-for="Input.RazonSocial" class="form-input" />
            </div>
        </div>

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.RTN">RTN</label>
                <input asp-for="Input.RTN" class="form-input" placeholder="0801-1990-12345" />
            </div>
            <div class="form-field">
                <label asp-for="Input.Telefono">Teléfono</label>
                <input asp-for="Input.Telefono" class="form-input" placeholder="+504 9999-9999" />
            </div>
        </div>

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Email">Email</label>
                <input asp-for="Input.Email" type="email" class="form-input" />
            </div>
            <div class="form-field">
                <label asp-for="Input.SitioWeb">Sitio web</label>
                <input asp-for="Input.SitioWeb" class="form-input" placeholder="https://miempresa.com" />
            </div>
        </div>

        <div class="form-field">
            <label asp-for="Input.Direccion">Dirección</label>
            <input asp-for="Input.Direccion" class="form-input" />
        </div>

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Ciudad">Ciudad</label>
                <input asp-for="Input.Ciudad" class="form-input" />
            </div>
            <div class="form-field">
                <label asp-for="Input.Pais">País</label>
                <input asp-for="Input.Pais" class="form-input" />
            </div>
        </div>

        <div class="form-divider"></div>

        <!-- Datos fiscales -->
        <h3 class="form-section-title">Datos fiscales (para facturas)</h3>

        <div class="form-field">
            <label asp-for="Input.CAI">CAI</label>
            <input asp-for="Input.CAI" class="form-input" placeholder="XXXXXX-XXXXXX-XXXXXX-XXXXXX" />
        </div>

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.RangoInicial">Rango inicial</label>
                <input asp-for="Input.RangoInicial" class="form-input" placeholder="000-001-01-00000001" />
            </div>
            <div class="form-field">
                <label asp-for="Input.RangoFinal">Rango final</label>
                <input asp-for="Input.RangoFinal" class="form-input" placeholder="000-001-01-00001000" />
            </div>
        </div>

        <div class="form-field">
            <label asp-for="Input.FechaLimiteEmision">Fecha límite de emisión</label>
            <input asp-for="Input.FechaLimiteEmision" type="date" class="form-input" />
        </div>

        <div class="form-field">
            <label asp-for="Input.NotasFactura">Notas al pie de factura</label>
            <textarea asp-for="Input.NotasFactura" class="form-input" rows="3" placeholder="Ej: La factura es beneficio de todos, exíjala..."></textarea>
        </div>

        <div class="form-actions">
            <a asp-page="/Configuracion/Index" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Guardar cambios
            </button>
        </div>
    </div>
</form>

@section Scripts {
    <script>
        (function () {
            var input = document.getElementById('logoFileInput');
            var img = document.getElementById('logoPreviewImg');
            var placeholder = document.getElementById('logoPreviewPlaceholder');

            input.addEventListener('change', function (e) {
                if (this.files && this.files[0]) {
                    var reader = new FileReader();
                    reader.onload = function (evt) {
                        img.src = evt.target.result;
                        img.style.display = 'block';
                        if (placeholder) placeholder.style.display = 'none';
                    };
                    reader.readAsDataURL(this.files[0]);
                }
            });
        })();
    </script>
}
````

===== FILE: Pages/Configuracion/Edit.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Configuracion
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EditModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public IFormFile? LogoFile { get; set; }

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(150)]
            public string Nombre { get; set; } = string.Empty;

            [StringLength(200)]
            public string? RazonSocial { get; set; }

            [StringLength(20)]
            public string? RTN { get; set; }

            [StringLength(300)]
            public string? Direccion { get; set; }

            [StringLength(100)]
            public string? Ciudad { get; set; }

            [StringLength(80)]
            public string? Pais { get; set; }

            [StringLength(30)]
            public string? Telefono { get; set; }

            [EmailAddress]
            public string? Email { get; set; }

            [StringLength(150)]
            public string? SitioWeb { get; set; }

            public string? LogoPath { get; set; }

            [StringLength(50)]
            public string? CAI { get; set; }

            [StringLength(30)]
            public string? RangoInicial { get; set; }

            [StringLength(30)]
            public string? RangoFinal { get; set; }

            public DateTime? FechaLimiteEmision { get; set; }

            public string? NotasFactura { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Index");
            }

            var empresa = _context.ConfiguracionEmpresa.FirstOrDefault();
            if (empresa == null)
            {
                empresa = new ConfiguracionEmpresa { Nombre = "Mi Empresa" };
                _context.ConfiguracionEmpresa.Add(empresa);
                _context.SaveChanges();
            }

            Input = new InputModel
            {
                Id = empresa.Id,
                Nombre = empresa.Nombre,
                RazonSocial = empresa.RazonSocial,
                RTN = empresa.RTN,
                Direccion = empresa.Direccion,
                Ciudad = empresa.Ciudad,
                Pais = empresa.Pais,
                Telefono = empresa.Telefono,
                Email = empresa.Email,
                SitioWeb = empresa.SitioWeb,
                LogoPath = empresa.LogoPath,
                CAI = empresa.CAI,
                RangoInicial = empresa.RangoInicial,
                RangoFinal = empresa.RangoFinal,
                FechaLimiteEmision = empresa.FechaLimiteEmision,
                NotasFactura = empresa.NotasFactura
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Index");
            }

            if (!ModelState.IsValid) return Page();

            var empresa = _context.ConfiguracionEmpresa.FirstOrDefault(e => e.Id == Input.Id);
            if (empresa == null)
            {
                TempData["Error"] = "Configuración no encontrada";
                return RedirectToPage("/Configuracion/Index");
            }

            // Procesar logo si se subió
            if (LogoFile != null && LogoFile.Length > 0)
            {
                // Validar tamaño (2 MB)
                if (LogoFile.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("LogoFile", "El archivo no puede pesar más de 2 MB");
                    return Page();
                }

                // Validar extensión
                var extensionesPermitidas = new[] { ".png", ".jpg", ".jpeg", ".webp" };
                var extension = Path.GetExtension(LogoFile.FileName).ToLowerInvariant();
                if (!extensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError("LogoFile", "Solo se permiten PNG, JPG o WEBP");
                    return Page();
                }

                // Guardar archivo
                var carpetaUploads = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(carpetaUploads))
                    Directory.CreateDirectory(carpetaUploads);

                var nombreArchivo = $"empresa-logo{extension}";
                var rutaCompleta = Path.Combine(carpetaUploads, nombreArchivo);

                // Eliminar archivo anterior si existe
                if (!string.IsNullOrEmpty(empresa.LogoPath))
                {
                    var rutaAnterior = Path.Combine(_env.WebRootPath, empresa.LogoPath.TrimStart('/').Replace("uploads/", "uploads/"));
                    if (System.IO.File.Exists(rutaAnterior))
                    {
                        try { System.IO.File.Delete(rutaAnterior); } catch { }
                    }
                }

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    LogoFile.CopyTo(stream);
                }

                // Guardar la ruta
                empresa.LogoPath = $"/uploads/{nombreArchivo}?v={DateTime.Now.Ticks}";
            }
            else
            {
                // Mantener el logo existente
                empresa.LogoPath = Input.LogoPath;
            }

            empresa.Nombre = Input.Nombre;
            empresa.RazonSocial = Input.RazonSocial;
            empresa.RTN = Input.RTN;
            empresa.Direccion = Input.Direccion;
            empresa.Ciudad = Input.Ciudad;
            empresa.Pais = Input.Pais;
            empresa.Telefono = Input.Telefono;
            empresa.Email = Input.Email;
            empresa.SitioWeb = Input.SitioWeb;
            empresa.CAI = Input.CAI;
            empresa.RangoInicial = Input.RangoInicial;
            empresa.RangoFinal = Input.RangoFinal;
            empresa.FechaLimiteEmision = Input.FechaLimiteEmision;
            empresa.NotasFactura = Input.NotasFactura;
            empresa.FechaActualizacion = DateTime.Now;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Actualizar configuración de empresa",
                "Actualizó los datos de la empresa",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Configuración actualizada correctamente";
            return RedirectToPage("/Configuracion/Index");
        }
    }
}
````

===== FILE: Pages/Configuracion/Index.cshtml =====

````html
@page
@model Kirkenta.Pages.Configuracion.IndexModel
@{
    ViewData["Title"] = "Configuración de empresa";
}

<div class="module-header">
    <div class="module-header-left">
        <h2>Configuración de empresa</h2>
        <p>Datos que aparecerán en tus cotizaciones, facturas y documentos</p>
    </div>
    <div class="module-header-right">
        <a asp-page="/Configuracion/Edit" class="btn-primary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
            </svg>
            Editar
        </a>
    </div>
</div>

@if (TempData["Success"] != null)
{
    <div class="module-alert module-alert-success">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
            <polyline points="22 4 12 14.01 9 11.01"/>
        </svg>
        @TempData["Success"]
    </div>
}

<div class="empresa-layout">
    <div class="module-card empresa-logo-card">
        <h3 class="form-section-title">Logo de la empresa</h3>
        <p class="form-section-desc">Se muestra en facturas, cotizaciones y pedidos</p>
        <div class="empresa-logo-preview">
            @if (!string.IsNullOrEmpty(Model.Empresa.LogoPath))
            {
                <img src="@Model.Empresa.LogoPath" alt="Logo empresa" />
            }
            else
            {
                <div class="empresa-logo-placeholder">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
                        <circle cx="8.5" cy="8.5" r="1.5"/>
                        <polyline points="21 15 16 10 5 21"/>
                    </svg>
                    <span>Sin logo</span>
                </div>
            }
        </div>
    </div>

    <div class="module-card empresa-info-card">
        <h3 class="form-section-title">Información de la empresa</h3>

        <div class="empresa-info-grid">
            <div class="empresa-info-item">
                <span class="empresa-info-label">Nombre</span>
                <span class="empresa-info-value">@Model.Empresa.Nombre</span>
            </div>
            <div class="empresa-info-item">
                <span class="empresa-info-label">Razón social</span>
                <span class="empresa-info-value">@(Model.Empresa.RazonSocial ?? "—")</span>
            </div>
            <div class="empresa-info-item">
                <span class="empresa-info-label">RTN</span>
                <span class="empresa-info-value">@(Model.Empresa.RTN ?? "—")</span>
            </div>
            <div class="empresa-info-item">
                <span class="empresa-info-label">Teléfono</span>
                <span class="empresa-info-value">@(Model.Empresa.Telefono ?? "—")</span>
            </div>
            <div class="empresa-info-item">
                <span class="empresa-info-label">Email</span>
                <span class="empresa-info-value">@(Model.Empresa.Email ?? "—")</span>
            </div>
            <div class="empresa-info-item">
                <span class="empresa-info-label">Sitio web</span>
                <span class="empresa-info-value">@(Model.Empresa.SitioWeb ?? "—")</span>
            </div>
            <div class="empresa-info-item empresa-info-full">
                <span class="empresa-info-label">Dirección</span>
                <span class="empresa-info-value">@(Model.Empresa.Direccion ?? "—")</span>
            </div>
            <div class="empresa-info-item">
                <span class="empresa-info-label">Ciudad</span>
                <span class="empresa-info-value">@(Model.Empresa.Ciudad ?? "—")</span>
            </div>
            <div class="empresa-info-item">
                <span class="empresa-info-label">País</span>
                <span class="empresa-info-value">@(Model.Empresa.Pais ?? "—")</span>
            </div>
        </div>

        <div class="form-divider"></div>
        <h3 class="form-section-title">Datos fiscales (para facturas)</h3>

        <div class="empresa-info-grid">
            <div class="empresa-info-item">
                <span class="empresa-info-label">CAI</span>
                <span class="empresa-info-value">@(Model.Empresa.CAI ?? "—")</span>
            </div>
            <div class="empresa-info-item">
                <span class="empresa-info-label">Rango autorizado</span>
                <span class="empresa-info-value">
                    @if (!string.IsNullOrEmpty(Model.Empresa.RangoInicial))
                    {
                        <span>@Model.Empresa.RangoInicial — @Model.Empresa.RangoFinal</span>
                    }
                    else
                    {
                        <span>—</span>
                    }
                </span>
            </div>
            <div class="empresa-info-item">
                <span class="empresa-info-label">Fecha límite emisión</span>
                <span class="empresa-info-value">@(Model.Empresa.FechaLimiteEmision?.ToString("dd/MM/yyyy") ?? "—")</span>
            </div>
        </div>

        @if (!string.IsNullOrEmpty(Model.Empresa.NotasFactura))
        {
            <div class="form-divider"></div>
            <h3 class="form-section-title">Notas al pie de factura</h3>
            <p class="empresa-notas">@Model.Empresa.NotasFactura</p>
        }
    </div>
</div>
````

===== FILE: Pages/Configuracion/Index.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Configuracion
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public ConfiguracionEmpresa Empresa { get; set; } = new();

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Index");
            }

            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault() ?? new ConfiguracionEmpresa();
            return Page();
        }
    }
}
````

====================================================
 Pages\Impuestos - 14 archivo(s)
====================================================

===== FILE: Pages/Impuestos/Create.cshtml =====

````html
@page
@model Kirkenta.Pages.Impuestos.CreateModel
@{
    ViewData["Title"] = "Nuevo impuesto";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Impuestos/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a impuestos
        </a>
        <h2>Nuevo impuesto</h2>
        <p>Registra un nuevo impuesto en el sistema</p>
    </div>
</div>

<div class="module-card form-card">
    <form method="post" class="erp-form">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span asp-validation-summary="ModelOnly"></span>
            </div>
        }

        <div class="form-field">
            <label asp-for="Input.Nombre">Nombre *</label>
            <input asp-for="Input.Nombre" class="form-input" placeholder="Ej: ISV 15%" required />
        </div>

        <div class="form-field">
            <label asp-for="Input.Porcentaje">Porcentaje (%) *</label>
            <input asp-for="Input.Porcentaje" type="number" step="0.01" min="0" max="100" class="form-input" placeholder="15.00" required />
        </div>

        <div class="form-field">
            <label asp-for="Input.Descripcion">Descripción</label>
            <textarea asp-for="Input.Descripcion" class="form-input" rows="2" placeholder="Descripción del impuesto..."></textarea>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.EsPredeterminado" type="checkbox" />
                <span>Usar como impuesto predeterminado</span>
            </label>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.Activo" type="checkbox" checked />
                <span>Impuesto activo</span>
            </label>
        </div>

        <div class="form-actions">
            <a asp-page="/Impuestos/Index" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Crear impuesto
            </button>
        </div>
    </form>
</div>
````

===== FILE: Pages/Impuestos/Create.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Impuestos
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(50)]
            public string Nombre { get; set; } = string.Empty;

            [Range(0, 100, ErrorMessage = "Debe estar entre 0 y 100")]
            public decimal Porcentaje { get; set; }

            [StringLength(200)]
            public string? Descripcion { get; set; }

            public bool EsPredeterminado { get; set; }
            public bool Activo { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Impuestos/Index");
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Impuestos/Index");
            }

            if (!ModelState.IsValid) return Page();

            if (_context.Impuestos.Any(i => i.Nombre.ToLower() == Input.Nombre.ToLower()))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe un impuesto con este nombre");
                return Page();
            }

            // Si es predeterminado, quitar el anterior
            if (Input.EsPredeterminado)
            {
                var anteriores = _context.Impuestos.Where(i => i.EsPredeterminado).ToList();
                foreach (var i in anteriores) i.EsPredeterminado = false;
            }

            var impuesto = new Impuesto
            {
                Nombre = Input.Nombre,
                Porcentaje = Input.Porcentaje,
                Descripcion = Input.Descripcion,
                EsPredeterminado = Input.EsPredeterminado,
                Activo = Input.Activo
            };

            _context.Impuestos.Add(impuesto);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear impuesto",
                $"Creó el impuesto '{impuesto.Nombre}' ({impuesto.Porcentaje}%)",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Impuesto '{impuesto.Nombre}' creado correctamente";
            return RedirectToPage("/Impuestos/Index");
        }
    }
}
````

===== FILE: Pages/Impuestos/Delete.cshtml =====

````html
@page "{id:int}"
@model Kirkenta.Pages.Impuestos.DeleteModel
@{
    ViewData["Title"] = "Eliminar impuesto";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Impuestos/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a impuestos
        </a>
        <h2>Eliminar impuesto</h2>
    </div>
</div>

<div class="module-card danger-card">
    <div class="danger-icon">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
            <line x1="12" y1="9" x2="12" y2="13"/>
            <line x1="12" y1="17" x2="12.01" y2="17"/>
        </svg>
    </div>

    <h3>¿Eliminar el impuesto "@Model.Impuesto.Nombre"?</h3>
    <p>Los productos que lo tengan asignado quedarán sin impuesto.</p>

    <form method="post" class="form-actions">
        <a asp-page="/Impuestos/Index" class="btn-secondary">Cancelar</a>
        <button type="submit" class="btn-danger">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <polyline points="3 6 5 6 21 6"/>
                <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
            </svg>
            Sí, eliminar
        </button>
    </form>
</div>
````

===== FILE: Pages/Impuestos/Delete.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Impuestos
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Impuesto Impuesto { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Impuestos/Index");
            }

            var impuesto = _context.Impuestos.FirstOrDefault(i => i.Id == id);
            if (impuesto == null)
            {
                TempData["Error"] = "Impuesto no encontrado";
                return RedirectToPage("/Impuestos/Index");
            }

            Impuesto = impuesto;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Impuestos/Index");
            }

            var impuesto = _context.Impuestos.FirstOrDefault(i => i.Id == id);
            if (impuesto == null)
            {
                TempData["Error"] = "Impuesto no encontrado";
                return RedirectToPage("/Impuestos/Index");
            }

            // Quitar el impuesto de los productos
            var productos = _context.Productos.Where(p => p.ImpuestoId == id).ToList();
            foreach (var p in productos) p.ImpuestoId = null;

            var nombre = impuesto.Nombre;
            _context.Impuestos.Remove(impuesto);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar impuesto",
                $"Eliminó el impuesto '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Impuesto '{nombre}' eliminado";
            return RedirectToPage("/Impuestos/Index");
        }
    }
}
````

===== FILE: Pages/Impuestos/Edit.cshtml =====

````html
@page "{id:int}"
@model Kirkenta.Pages.Impuestos.EditModel
@{
    ViewData["Title"] = "Editar impuesto";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Impuestos/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a impuestos
        </a>
        <h2>Editar impuesto</h2>
    </div>
</div>

<div class="module-card form-card">
    <form method="post" class="erp-form">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span asp-validation-summary="ModelOnly"></span>
            </div>
        }

        <input type="hidden" asp-for="Input.Id" />

        <div class="form-field">
            <label asp-for="Input.Nombre">Nombre *</label>
            <input asp-for="Input.Nombre" class="form-input" required />
        </div>

        <div class="form-field">
            <label asp-for="Input.Porcentaje">Porcentaje (%) *</label>
            <input asp-for="Input.Porcentaje" type="number" step="0.01" min="0" max="100" class="form-input" required />
        </div>

        <div class="form-field">
            <label asp-for="Input.Descripcion">Descripción</label>
            <textarea asp-for="Input.Descripcion" class="form-input" rows="2"></textarea>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.EsPredeterminado" type="checkbox" />
                <span>Usar como impuesto predeterminado</span>
            </label>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.Activo" type="checkbox" />
                <span>Impuesto activo</span>
            </label>
        </div>

        <div class="form-actions">
            <a asp-page="/Impuestos/Index" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Guardar cambios
            </button>
        </div>
    </form>
</div>
````

===== FILE: Pages/Impuestos/Edit.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Impuestos
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(50)]
            public string Nombre { get; set; } = string.Empty;

            [Range(0, 100, ErrorMessage = "Debe estar entre 0 y 100")]
            public decimal Porcentaje { get; set; }

            [StringLength(200)]
            public string? Descripcion { get; set; }

            public bool EsPredeterminado { get; set; }
            public bool Activo { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Impuestos/Index");
            }

            var impuesto = _context.Impuestos.FirstOrDefault(i => i.Id == id);
            if (impuesto == null)
            {
                TempData["Error"] = "Impuesto no encontrado";
                return RedirectToPage("/Impuestos/Index");
            }

            Input = new InputModel
            {
                Id = impuesto.Id,
                Nombre = impuesto.Nombre,
                Porcentaje = impuesto.Porcentaje,
                Descripcion = impuesto.Descripcion,
                EsPredeterminado = impuesto.EsPredeterminado,
                Activo = impuesto.Activo
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Impuestos/Index");
            }

            if (!ModelState.IsValid) return Page();

            var impuesto = _context.Impuestos.FirstOrDefault(i => i.Id == Input.Id);
            if (impuesto == null)
            {
                TempData["Error"] = "Impuesto no encontrado";
                return RedirectToPage("/Impuestos/Index");
            }

            if (_context.Impuestos.Any(i => i.Nombre.ToLower() == Input.Nombre.ToLower() && i.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe un impuesto con este nombre");
                return Page();
            }

            // Si es predeterminado, quitar el anterior
            if (Input.EsPredeterminado && !impuesto.EsPredeterminado)
            {
                var anteriores = _context.Impuestos.Where(i => i.EsPredeterminado && i.Id != Input.Id).ToList();
                foreach (var i in anteriores) i.EsPredeterminado = false;
            }

            impuesto.Nombre = Input.Nombre;
            impuesto.Porcentaje = Input.Porcentaje;
            impuesto.Descripcion = Input.Descripcion;
            impuesto.EsPredeterminado = Input.EsPredeterminado;
            impuesto.Activo = Input.Activo;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar impuesto",
                $"Editó el impuesto '{impuesto.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Impuesto '{impuesto.Nombre}' actualizado";
            return RedirectToPage("/Impuestos/Index");
        }
    }
}
````

===== FILE: Pages/Impuestos/Export.cshtml =====

````html
@page
@model Kirkenta.Pages.Impuestos.ExportModel
@{
    Layout = null;
}
````

===== FILE: Pages/Impuestos/Export.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers.Export;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Impuestos
{
    public class ExportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ExportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(string formato = "excel", bool plantilla = false)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin") return RedirectToPage("/Impuestos/Index");

            var columns = ExportColumns.Impuestos();

            if (plantilla)
            {
                var vacio = GenerarArchivo(new List<Impuesto>(), columns, formato, esPlantilla: true);
                return File(vacio, ContentType(formato), NombreArchivo("plantilla_impuestos", formato));
            }

            var lista = _context.Impuestos.OrderBy(i => i.Porcentaje).ToList();
            var archivo = GenerarArchivo(lista, columns, formato, esPlantilla: false);
            return File(archivo, ContentType(formato), NombreArchivo("impuestos", formato));
        }

        private static byte[] GenerarArchivo<T>(List<T> items, List<ExportColumn<T>> columns, string formato, bool esPlantilla)
        {
            var titulo = esPlantilla ? "Plantilla de importación de impuestos" : "Listado de impuestos";
            return formato.ToLower() switch
            {
                "csv" => CsvExporter.Export(items, columns),
                "json" => JsonExporter.Export(items),
                _ => ExcelExporter.Export(items, columns, "Impuestos", titulo)
            };
        }

        private static string ContentType(string formato) => formato.ToLower() switch
        {
            "csv" => "text/csv",
            "json" => "application/json",
            _ => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        private static string NombreArchivo(string baseNombre, string formato)
        {
            var ext = formato.ToLower() switch { "csv" => "csv", "json" => "json", _ => "xlsx" };
            return $"{baseNombre}_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
        }
    }
}
````

===== FILE: Pages/Impuestos/Import.cshtml =====

````html
@page
@model Kirkenta.Pages.Impuestos.ImportModel
@{
    ViewData["Title"] = Model.WizardConfig.TituloPagina;
}

<div class="module-header">
    <div class="module-header-left">
        <a href="@Model.WizardConfig.UrlIndex" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a @Model.WizardConfig.NombrePlural
        </a>
        <h2>@Model.WizardConfig.TituloPagina</h2>
        <p>Sube un archivo Excel o CSV con los @Model.WizardConfig.NombrePlural a importar</p>
    </div>
</div>

@await Html.PartialAsync("_ImportStepIndicator", new { Paso = 1 })

@await Html.PartialAsync("_ImportUploadForm", Model.WizardConfig)
````

===== FILE: Pages/Impuestos/Import.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers.Import;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Impuestos
{
    public class ImportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public IFormFile? Archivo { get; set; }

        public ImportWizardConfig WizardConfig { get; set; } = new();

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            WizardConfig = BuildConfig();
            if (currentRol != "Admin") return Redirect(WizardConfig.UrlIndex);

            ImportWizardHelper.LimpiarSesion(HttpContext.Session, WizardConfig);
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            WizardConfig = BuildConfig();
            if (currentRol != "Admin") return Redirect(WizardConfig.UrlIndex);

            var resultado = ImportWizardHelper.ProcesarArchivo(Archivo);
            if (!resultado.Ok)
            {
                ViewData["ImportError"] = resultado.Error;
                return Page();
            }

            ImportWizardHelper.GuardarEnSesion(HttpContext.Session, WizardConfig, resultado.Headers, resultado.Rows);
            return Redirect(WizardConfig.UrlImportMap);
        }

        private static ImportWizardConfig BuildConfig()
        {
            return new ImportWizardConfig
            {
                NombrePlural = "impuestos",
                NombreSingular = "impuesto",
                ModuloPermiso = "Configuracion",
                SubmoduloPermiso = "Impuestos",
                SessionKey = "Impuestos",
                UrlIndex = "/Impuestos/Index",
                UrlImport = "/Impuestos/Import",
                UrlImportMap = "/Impuestos/ImportMap",
                CamposRequeridos = new[] { "Nombre", "Porcentaje" }
            };
        }
    }
}
````

===== FILE: Pages/Impuestos/ImportMap.cshtml =====

````html
@page
@model Kirkenta.Pages.Impuestos.ImportMapModel
@{
    ViewData["Title"] = Model.WizardConfig.TituloMapa;
}

<div class="module-header">
    <div class="module-header-left">
        <a href="@Model.WizardConfig.UrlImport" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver al paso anterior
        </a>
        <h2>@Model.WizardConfig.TituloMapa</h2>
        <p>Asocia cada campo del sistema con una columna de tu archivo</p>
    </div>
</div>

@await Html.PartialAsync("_ImportStepIndicator", new { Paso = 2 })

<form method="post">
    <div class="module-card" style="padding: 28px; margin-bottom: 20px;">
        <h3 class="form-section-title">Mapeo de columnas</h3>
        <p class="form-section-desc">
            Detectadas <strong>@Model.HeadersArchivo.Count columnas</strong> y
            <strong>@Model.FilasArchivo.Count filas</strong>.
        </p>

        <div class="table-wrapper" style="margin-top: 16px;">
            <table class="erp-table">
                <thead>
                    <tr>
                        <th>Campo del sistema</th>
                        <th>Columna de tu archivo</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var campo in Model.WizardConfig.Campos)
                    {
                        var sugerido = Model.MapaSugerido.GetValueOrDefault(campo.Key);
                        <tr>
                            <td>
                                <strong>@campo.Label</strong>
                                @if (campo.Requerido)
                                {
                                    <span style="color: var(--color-danger);">*</span>
                                }
                            </td>
                            <td>
                                <select name="Mapeo[@campo.Key]" class="form-input">
                                    <option value="">— Ignorar este campo —</option>
                                    @foreach (var header in Model.HeadersArchivo)
                                    {
                                        if (header == sugerido)
                                        {
                                            <option value="@header" selected>@header</option>
                                        }
                                        else
                                        {
                                            <option value="@header">@header</option>
                                        }
                                    }
                                </select>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </div>

    <div class="module-card" style="padding: 28px; margin-bottom: 20px;">
        <h3 class="form-section-title">Vista previa (primeras 5 filas)</h3>
        <div class="table-wrapper" style="margin-top: 16px;">
            <table class="erp-table" style="font-size: 12.5px;">
                <thead>
                    <tr>
                        @foreach (var header in Model.HeadersArchivo)
                        {
                            <th>@header</th>
                        }
                    </tr>
                </thead>
                <tbody>
                    @foreach (var fila in Model.FilasArchivo.Take(5))
                    {
                        <tr>
                            @foreach (var header in Model.HeadersArchivo)
                            {
                                <td>@fila.GetValueOrDefault(header)</td>
                            }
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </div>

    <div class="form-actions" style="border-top:none; margin-top:0;">
        <a href="@Model.WizardConfig.UrlImport" class="btn-secondary">Atrás</a>
        <button type="submit" class="btn-primary" style="background: var(--color-success);">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <polyline points="20 6 9 17 4 12"/>
            </svg>
            Importar @Model.FilasArchivo.Count impuestos
        </button>
    </div>
</form>
````

===== FILE: Pages/Impuestos/ImportMap.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Impuestos
{
    public class ImportMapModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportMapModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public ImportWizardConfig WizardConfig { get; set; } = new();
        public List<string> HeadersArchivo { get; set; } = new();
        public List<Dictionary<string, string>> FilasArchivo { get; set; } = new();
        public Dictionary<string, string?> MapaSugerido { get; set; } = new();

        [BindProperty]
        public Dictionary<string, string> Mapeo { get; set; } = new();

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            WizardConfig = BuildConfig();
            if (currentRol != "Admin") return Redirect(WizardConfig.UrlIndex);

            if (!CargarDesdeSesion())
            {
                TempData["Error"] = "La sesión expiró.";
                return Redirect(WizardConfig.UrlImport);
            }

            MapaSugerido = ColumnMapper.Detectar(HeadersArchivo, WizardConfig.AliasCampos);
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            WizardConfig = BuildConfig();
            if (currentRol != "Admin") return Redirect(WizardConfig.UrlIndex);

            if (!CargarDesdeSesion())
            {
                TempData["Error"] = "La sesión expiró.";
                return Redirect(WizardConfig.UrlImport);
            }

            var faltantes = WizardConfig.CamposRequeridos
                .Where(campo => string.IsNullOrEmpty(Mapeo.GetValueOrDefault(campo)))
                .ToList();

            if (faltantes.Count > 0)
            {
                var labelsFaltantes = WizardConfig.Campos.Where(c => faltantes.Contains(c.Key)).Select(c => c.Label);
                TempData["Error"] = $"Debes mapear: {string.Join(", ", labelsFaltantes)}";
                return Redirect(WizardConfig.UrlImportMap);
            }

            var resultado = ImportarImpuestos(FilasArchivo, Mapeo, currentUser?.Id);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Importar impuestos",
                $"Importó impuestos: {resultado.Resumen()}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            ImportWizardHelper.LimpiarSesion(HttpContext.Session, WizardConfig);

            TempData["Success"] = $"Importación completada: {resultado.Resumen()}";
            return Redirect(WizardConfig.UrlIndex);
        }

        private bool CargarDesdeSesion()
        {
            var data = ImportWizardHelper.RecuperarDeSesion(HttpContext.Session, WizardConfig);
            if (data == null) return false;
            HeadersArchivo = data.Value.Headers;
            FilasArchivo = data.Value.Rows;
            return true;
        }

        private static ImportWizardConfig BuildConfig()
        {
            return new ImportWizardConfig
            {
                NombrePlural = "impuestos",
                NombreSingular = "impuesto",
                ModuloPermiso = "Configuracion",
                SubmoduloPermiso = "Impuestos",
                SessionKey = "Impuestos",
                UrlIndex = "/Impuestos/Index",
                UrlImport = "/Impuestos/Import",
                UrlImportMap = "/Impuestos/ImportMap",
                CamposRequeridos = new[] { "Nombre", "Porcentaje" },
                Campos = new List<CampoMapeo>
                {
                    new() { Key = "Nombre",          Label = "Nombre",        Requerido = true },
                    new() { Key = "Porcentaje",      Label = "Porcentaje (%)",Requerido = true },
                    new() { Key = "Descripcion",     Label = "Descripción",   Requerido = false },
                    new() { Key = "EsPredeterminado",Label = "Predeterminado (Sí/No)", Requerido = false },
                    new() { Key = "Activo",          Label = "Activo (Sí/No)", Requerido = false },
                },
                AliasCampos = new Dictionary<string, string[]>
                {
                    { "Nombre",          new[] { "nombre", "name", "impuesto", "tax" } },
                    { "Porcentaje",      new[] { "porcentaje", "tasa", "rate", "iva", "isv", "porcent" } },
                    { "Descripcion",     new[] { "descripcion", "description", "detalle" } },
                    { "EsPredeterminado",new[] { "predeterminado", "default", "por_defecto" } },
                    { "Activo",          new[] { "activo", "active", "estado" } },
                }
            };
        }

        private ImportResult ImportarImpuestos(
            List<Dictionary<string, string>> filas,
            Dictionary<string, string> mapa,
            int? usuarioId)
        {
            var resultado = new ImportResult { TotalFilas = filas.Count };

            int filaNum = 1;
            foreach (var fila in filas)
            {
                filaNum++;
                try
                {
                    string? Obtener(string campo)
                    {
                        var header = mapa.GetValueOrDefault(campo);
                        if (string.IsNullOrEmpty(header)) return null;
                        return fila.GetValueOrDefault(header)?.Trim();
                    }

                    var nombre = Obtener("Nombre");
                    if (string.IsNullOrWhiteSpace(nombre))
                    {
                        resultado.AgregarError(filaNum, "El campo 'Nombre' es obligatorio");
                        continue;
                    }
                    if (nombre.Length > 50)
                    {
                        resultado.AgregarError(filaNum, "El nombre excede 50 caracteres");
                        continue;
                    }

                    var porcentaje = ParserHelper.ParsearDecimal(Obtener("Porcentaje"));
                    if (porcentaje == null)
                    {
                        resultado.AgregarError(filaNum, "El porcentaje es obligatorio y debe ser numérico");
                        continue;
                    }
                    if (porcentaje < 0 || porcentaje > 100)
                    {
                        resultado.AgregarError(filaNum, "El porcentaje debe estar entre 0 y 100");
                        continue;
                    }

                    var existente = _context.Impuestos
                        .FirstOrDefault(i => i.Nombre.ToLower() == nombre.ToLower());

                    if (existente != null)
                    {
                        existente.Nombre = nombre;
                        existente.Porcentaje = porcentaje.Value;
                        existente.Descripcion = Obtener("Descripcion") ?? existente.Descripcion;
                        existente.Activo = ParserHelper.ParsearBool(Obtener("Activo")) ?? existente.Activo;
                        resultado.Actualizados++;
                    }
                    else
                    {
                        var imp = new Impuesto
                        {
                            Nombre = nombre,
                            Porcentaje = porcentaje.Value,
                            Descripcion = Obtener("Descripcion"),
                            EsPredeterminado = ParserHelper.ParsearBool(Obtener("EsPredeterminado")) ?? false,
                            Activo = ParserHelper.ParsearBool(Obtener("Activo")) ?? true
                        };
                        _context.Impuestos.Add(imp);
                        resultado.Creados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.AgregarError(filaNum, $"Error inesperado: {ex.Message}");
                }
            }

            _context.SaveChanges();
            return resultado;
        }
    }
}
````

===== FILE: Pages/Impuestos/Index.cshtml =====

````html
@page
@model Kirkenta.Pages.Impuestos.IndexModel
@{
    ViewData["Title"] = "Impuestos";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Configuracion/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a configuración
        </a>
        <h2>Impuestos</h2>
        <p>Configura los impuestos que se aplican a tus productos</p>
    </div>
    <div class="module-header-right">
        <a asp-page="/Impuestos/Create" class="btn-primary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <line x1="12" y1="5" x2="12" y2="19"/>
                <line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            Nuevo impuesto
        </a>
        <a asp-page="/Impuestos/Import" class="btn-secondary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                <polyline points="17 8 12 3 7 8"/>
                <line x1="12" y1="3" x2="12" y2="15"/>
            </svg>
            Importar
        </a>
        <a asp-page="/Impuestos/Export" asp-route-formato="excel" class="btn-secondary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                <polyline points="7 10 12 15 17 10"/>
                <line x1="12" y1="15" x2="12" y2="3"/>
            </svg>
            Exportar
        </a>
    </div>
</div>

<div class="module-card">
    @if (TempData["Success"] != null)
    {
        <div class="module-alert module-alert-success">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                <polyline points="22 4 12 14.01 9 11.01"/>
            </svg>
            @TempData["Success"]
        </div>
    }

    @if (TempData["Error"] != null)
    {
        <div class="module-alert module-alert-error">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10"/>
                <line x1="12" y1="8" x2="12" y2="12"/>
                <line x1="12" y1="16" x2="12.01" y2="16"/>
            </svg>
            @TempData["Error"]
        </div>
    }

    @if (Model.Impuestos.Count == 0)
    {
        <div class="empty-state">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                <line x1="12" y1="1" x2="12" y2="23"/>
                <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
            </svg>
            <h3>No hay impuestos configurados</h3>
            <p>Crea tu primer impuesto para aplicarlo a los productos</p>
        </div>
    }
    else
    {
        <div class="table-wrapper">
            <table class="erp-table">
                <thead>
                    <tr>
                        <th>Nombre</th>
                        <th>Porcentaje</th>
                        <th>Descripción</th>
                        <th>Predeterminado</th>
                        <th>Estado</th>
                        <th style="width: 120px; text-align: right;">Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var imp in Model.Impuestos)
                    {
                        <tr>
                            <td><strong>@imp.Nombre</strong></td>
                            <td>
                                <span class="badge badge-info">@imp.Porcentaje.ToString("N2")%</span>
                            </td>
                            <td>@(imp.Descripcion ?? "—")</td>
                            <td>
                                @if (imp.EsPredeterminado)
                                {
                                    <span class="badge badge-success">Sí</span>
                                }
                                else
                                {
                                    <span class="table-user-sub">—</span>
                                }
                            </td>
                            <td>
                                @if (imp.Activo)
                                {
                                    <span class="badge badge-success">Activo</span>
                                }
                                else
                                {
                                    <span class="badge badge-danger">Inactivo</span>
                                }
                            </td>
                            <td>
                                <div class="table-actions">
                                    <a asp-page="/Impuestos/Edit" asp-route-id="@imp.Id" class="btn-icon-action" title="Editar">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                                            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                                        </svg>
                                    </a>
                                    <a asp-page="/Impuestos/Delete" asp-route-id="@imp.Id" class="btn-icon-action btn-icon-action-danger" title="Eliminar">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                            <polyline points="3 6 5 6 21 6"/>
                                            <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                                        </svg>
                                    </a>
                                </div>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
</div>
````

===== FILE: Pages/Impuestos/Index.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Impuestos
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Impuesto> Impuestos { get; set; } = new();

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores pueden acceder a esta sección";
                return RedirectToPage("/Index");
            }

            Impuestos = _context.Impuestos.OrderBy(i => i.Porcentaje).ToList();
            return Page();
        }
    }
}
````

====================================================
 Pages\MetodosPago - 8 archivo(s)
====================================================

===== FILE: Pages/MetodosPago/Create.cshtml =====

````html
@page
@model Kirkenta.Pages.MetodosPago.CreateModel
@{
    ViewData["Title"] = "Nuevo método de pago";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/MetodosPago/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a métodos
        </a>
        <h2>Nuevo método de pago</h2>
    </div>
</div>

<div class="module-card form-card">
    <form method="post" class="erp-form">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span asp-validation-summary="ModelOnly"></span>
            </div>
        }

        <div class="form-field">
            <label asp-for="Input.Nombre">Nombre *</label>
            <input asp-for="Input.Nombre" class="form-input" placeholder="Ej: Pago con tarjeta BAC" required />
        </div>

        <div class="form-field">
            <label asp-for="Input.Tipo">Tipo *</label>
            <select asp-for="Input.Tipo" class="form-input" required>
                <option value="Efectivo">Efectivo</option>
                <option value="Tarjeta">Tarjeta</option>
                <option value="Transferencia">Transferencia</option>
                <option value="Credito">Crédito</option>
                <option value="Otro">Otro</option>
            </select>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.RequiereReferencia" type="checkbox" />
                <span>Requiere número de referencia</span>
            </label>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.Activo" type="checkbox" checked />
                <span>Método activo</span>
            </label>
        </div>

        <div class="form-actions">
            <a asp-page="/MetodosPago/Index" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Crear método
            </button>
        </div>
    </form>
</div>
````

===== FILE: Pages/MetodosPago/Create.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.MetodosPago
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(80)]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "El tipo es obligatorio")]
            public string Tipo { get; set; } = "Efectivo";

            public bool RequiereReferencia { get; set; }
            public bool Activo { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/MetodosPago/Index");
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/MetodosPago/Index");
            }

            if (!ModelState.IsValid) return Page();

            if (_context.MetodosPago.Any(m => m.Nombre.ToLower() == Input.Nombre.ToLower()))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe un método con este nombre");
                return Page();
            }

            var metodo = new MetodoPago
            {
                Nombre = Input.Nombre,
                Tipo = Input.Tipo,
                RequiereReferencia = Input.RequiereReferencia,
                Activo = Input.Activo
            };

            _context.MetodosPago.Add(metodo);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear método de pago",
                $"Creó el método '{metodo.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Método '{metodo.Nombre}' creado";
            return RedirectToPage("/MetodosPago/Index");
        }
    }
}
````

===== FILE: Pages/MetodosPago/Delete.cshtml =====

````html
@page "{id:int}"
@model Kirkenta.Pages.MetodosPago.DeleteModel
@{
    ViewData["Title"] = "Eliminar método de pago";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/MetodosPago/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a métodos
        </a>
        <h2>Eliminar método</h2>
    </div>
</div>

<div class="module-card danger-card">
    <div class="danger-icon">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
            <line x1="12" y1="9" x2="12" y2="13"/>
            <line x1="12" y1="17" x2="12.01" y2="17"/>
        </svg>
    </div>

    <h3>¿Eliminar el método "@Model.Metodo.Nombre"?</h3>
    <p>No se podrá usar para registrar pagos.</p>

    <form method="post" class="form-actions">
        <a asp-page="/MetodosPago/Index" class="btn-secondary">Cancelar</a>
        <button type="submit" class="btn-danger">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <polyline points="3 6 5 6 21 6"/>
                <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
            </svg>
            Sí, eliminar
        </button>
    </form>
</div>
````

===== FILE: Pages/MetodosPago/Delete.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.MetodosPago
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public MetodoPago Metodo { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/MetodosPago/Index");
            }

            var metodo = _context.MetodosPago.FirstOrDefault(m => m.Id == id);
            if (metodo == null)
            {
                TempData["Error"] = "Método no encontrado";
                return RedirectToPage("/MetodosPago/Index");
            }

            Metodo = metodo;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/MetodosPago/Index");
            }

            var metodo = _context.MetodosPago.FirstOrDefault(m => m.Id == id);
            if (metodo == null)
            {
                TempData["Error"] = "Método no encontrado";
                return RedirectToPage("/MetodosPago/Index");
            }

            // Verificar que no tenga pagos
            if (_context.Pagos.Any(p => p.MetodoPagoId == id))
            {
                TempData["Error"] = "No se puede eliminar un método que tiene pagos registrados";
                return RedirectToPage("/MetodosPago/Index");
            }

            var nombre = metodo.Nombre;
            _context.MetodosPago.Remove(metodo);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar método de pago",
                $"Eliminó el método '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Método '{nombre}' eliminado";
            return RedirectToPage("/MetodosPago/Index");
        }
    }
}
````

===== FILE: Pages/MetodosPago/Edit.cshtml =====

````html
@page "{id:int}"
@model Kirkenta.Pages.MetodosPago.EditModel
@{
    ViewData["Title"] = "Editar método de pago";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/MetodosPago/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a métodos
        </a>
        <h2>Editar método</h2>
    </div>
</div>

<div class="module-card form-card">
    <form method="post" class="erp-form">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span asp-validation-summary="ModelOnly"></span>
            </div>
        }

        <input type="hidden" asp-for="Input.Id" />

        <div class="form-field">
            <label asp-for="Input.Nombre">Nombre *</label>
            <input asp-for="Input.Nombre" class="form-input" required />
        </div>

        <div class="form-field">
            <label asp-for="Input.Tipo">Tipo *</label>
            <select asp-for="Input.Tipo" class="form-input" required>
                <option value="Efectivo">Efectivo</option>
                <option value="Tarjeta">Tarjeta</option>
                <option value="Transferencia">Transferencia</option>
                <option value="Credito">Crédito</option>
                <option value="Otro">Otro</option>
            </select>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.RequiereReferencia" type="checkbox" />
                <span>Requiere número de referencia</span>
            </label>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.Activo" type="checkbox" />
                <span>Método activo</span>
            </label>
        </div>

        <div class="form-actions">
            <a asp-page="/MetodosPago/Index" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Guardar cambios
            </button>
        </div>
    </form>
</div>
````

===== FILE: Pages/MetodosPago/Edit.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.MetodosPago
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(80)]
            public string Nombre { get; set; } = string.Empty;

            [Required]
            public string Tipo { get; set; } = "Efectivo";

            public bool RequiereReferencia { get; set; }
            public bool Activo { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/MetodosPago/Index");
            }

            var metodo = _context.MetodosPago.FirstOrDefault(m => m.Id == id);
            if (metodo == null)
            {
                TempData["Error"] = "Método no encontrado";
                return RedirectToPage("/MetodosPago/Index");
            }

            Input = new InputModel
            {
                Id = metodo.Id,
                Nombre = metodo.Nombre,
                Tipo = metodo.Tipo,
                RequiereReferencia = metodo.RequiereReferencia,
                Activo = metodo.Activo
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/MetodosPago/Index");
            }

            if (!ModelState.IsValid) return Page();

            var metodo = _context.MetodosPago.FirstOrDefault(m => m.Id == Input.Id);
            if (metodo == null)
            {
                TempData["Error"] = "Método no encontrado";
                return RedirectToPage("/MetodosPago/Index");
            }

            if (_context.MetodosPago.Any(m => m.Nombre.ToLower() == Input.Nombre.ToLower() && m.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe un método con este nombre");
                return Page();
            }

            metodo.Nombre = Input.Nombre;
            metodo.Tipo = Input.Tipo;
            metodo.RequiereReferencia = Input.RequiereReferencia;
            metodo.Activo = Input.Activo;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar método de pago",
                $"Editó el método '{metodo.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Método '{metodo.Nombre}' actualizado";
            return RedirectToPage("/MetodosPago/Index");
        }
    }
}
````

===== FILE: Pages/MetodosPago/Index.cshtml =====

````html
@page
@model Kirkenta.Pages.MetodosPago.IndexModel
@{
    ViewData["Title"] = "Métodos de pago";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Configuracion/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a configuración
        </a>
        <h2>Métodos de pago</h2>
        <p>Configura las formas de pago aceptadas</p>
    </div>
    <div class="module-header-right">
        <a asp-page="/MetodosPago/Create" class="btn-primary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <line x1="12" y1="5" x2="12" y2="19"/>
                <line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            Nuevo método
        </a>
    </div>
</div>

<div class="module-card">
    @if (TempData["Success"] != null)
    {
        <div class="module-alert module-alert-success">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                <polyline points="22 4 12 14.01 9 11.01"/>
            </svg>
            @TempData["Success"]
        </div>
    }

    @if (Model.Metodos.Count == 0)
    {
        <div class="empty-state">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                <rect x="1" y="4" width="22" height="16" rx="2" ry="2"/>
                <line x1="1" y1="10" x2="23" y2="10"/>
            </svg>
            <h3>No hay métodos de pago</h3>
            <p>Crea tu primer método de pago</p>
        </div>
    }
    else
    {
        <div class="roles-grid">
            @foreach (var m in Model.Metodos)
            {
                <div class="role-card">
                    <div class="role-card-header">
                        <div class="role-color" style="background: var(--color-primary); display:flex; align-items:center; justify-content:center; font-size:20px;">
                            @switch (m.Tipo)
                            {
                                case "Efectivo":
                                    <span>💵</span>
                                    break;
                                case "Tarjeta":
                                    <span>💳</span>
                                    break;
                                case "Transferencia":
                                    <span>🏦</span>
                                    break;
                                case "Credito":
                                    <span>📋</span>
                                    break;
                                default:
                                    <span>💰</span>
                                    break;
                            }
                        </div>
                        <div class="role-info">
                            <h3>@m.Nombre</h3>
                            <span class="badge badge-info">@m.Tipo</span>
                        </div>
                    </div>

                    @if (m.RequiereReferencia)
                    {
                        <p class="role-desc">Requiere número de referencia</p>
                    }
                    else
                    {
                        <p class="role-desc">Sin referencia requerida</p>
                    }

                    <div class="role-actions">
                        <a asp-page="/MetodosPago/Edit" asp-route-id="@m.Id" class="btn-sm btn-secondary">Editar</a>
                        <a asp-page="/MetodosPago/Delete" asp-route-id="@m.Id" class="btn-sm btn-danger">Eliminar</a>
                    </div>
                </div>
            }
        </div>
    }
</div>
````

===== FILE: Pages/MetodosPago/Index.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.MetodosPago
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<MetodoPago> Metodos { get; set; } = new();

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Index");
            }

            Metodos = _context.MetodosPago.OrderBy(m => m.Nombre).ToList();
            return Page();
        }
    }
}
````

====================================================
 Pages\Nomenclatura - 2 archivo(s)
====================================================

===== FILE: Pages/Nomenclatura/Index.cshtml =====

````html
@page
@model Kirkenta.Pages.Nomenclatura.IndexModel
@{
    ViewData["Title"] = "Nomenclatura";
}

<div class="module-header">
    <div class="module-header-left">
        <h2>Nomenclatura</h2>
        <p>Configura el formato de numeración de tus documentos</p>
    </div>
</div>

<div class="nomenclatura-tabs">
    <a asp-page="/Series/Index" class="nomenclatura-tab @(Model.TabActiva == "Series" ? "is-active" : "")">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M4 7V4h16v3M9 20h6M12 4v16"/>
        </svg>
        Series de documentos
    </a>
</div>

<div class="module-card" style="padding: 40px; text-align: center;">
    <h3 style="font-size: 18px; font-weight: 700; margin-bottom: 8px; color: var(--color-text);">
        Series de documentos
    </h3>
    <p style="color: var(--color-muted); margin-bottom: 24px;">
        Configura el prefijo, formato y numeración correlativa de cada tipo de documento (Cotizaciones, Pedidos, Ventas, Facturas, Productos, etc.)
    </p>
    <a asp-page="/Series/Index" class="btn-primary">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
            <circle cx="12" cy="12" r="3"/>
            <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/>
        </svg>
        Gestionar series
    </a>
</div>
````

===== FILE: Pages/Nomenclatura/Index.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Nomenclatura
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string TabActiva { get; set; } = "Series";

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores pueden acceder";
                return RedirectToPage("/Index");
            }

            return Page();
        }
    }
}
````

====================================================
 Pages\Series - 8 archivo(s)
====================================================

===== FILE: Pages/Series/Create.cshtml =====

````html
@page
@model Kirkenta.Pages.Series.CreateModel
@{
    ViewData["Title"] = "Nueva serie";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Series/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a series
        </a>
        <h2>Nueva serie</h2>
    </div>
</div>

<div class="module-card form-card">
    <form method="post" class="erp-form">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span asp-validation-summary="ModelOnly"></span>
            </div>
        }

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Tipo">Tipo de documento *</label>
                <select asp-for="Input.Tipo" class="form-input" required>
                    <option value="">— Selecciona —</option>
                    <optgroup label="Ventas">
                        <option value="Cotizacion">Cotización</option>
                        <option value="Pedido">Pedido</option>
                        <option value="Venta">Venta</option>
                        <option value="Factura">Factura</option>
                        <option value="Devolucion">Devolución</option>
                    </optgroup>
                    <optgroup label="Inventario">
                        <option value="Producto">Producto (SKU)</option>
                        <option value="Baja">Baja de inventario</option>
                    </optgroup>
                    <optgroup label="Compras">
                        <option value="Proveedor">Proveedor</option>
                        <option value="OrdenCompra">Orden de compra</option>
                        <option value="PagoProveedor">Pago a proveedor</option>
                        <option value="DevolucionProveedor">Devolución a proveedor</option>
                    </optgroup>
                </select>
            </div>
            <div class="form-field">
                <label asp-for="Input.Nombre">Nombre de la serie *</label>
                <input asp-for="Input.Nombre" class="form-input" placeholder="Ej: Principal" required />
            </div>
        </div>

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Prefijo">Prefijo *</label>
                <input asp-for="Input.Prefijo" class="form-input" placeholder="PROV" required />
            </div>
            <div class="form-field">
                <label asp-for="Input.Sufijo">Sufijo</label>
                <input asp-for="Input.Sufijo" class="form-input" placeholder="-2026 (opcional)" />
            </div>
        </div>

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Separador">Separador</label>
                <input asp-for="Input.Separador" class="form-input" value="-" maxlength="5" />
            </div>
            <div class="form-field">
                <label asp-for="Input.LongitudNumero">Longitud del número</label>
                <input asp-for="Input.LongitudNumero" type="number" min="1" max="10" class="form-input" value="4" />
                <small class="form-help">4 → 0001, 5 → 00001</small>
            </div>
        </div>

        <div class="form-field">
            <label asp-for="Input.SiguienteNumero">Siguiente número</label>
            <input asp-for="Input.SiguienteNumero" type="number" min="1" class="form-input" value="1" />
        </div>

        <div class="form-divider"></div>
        <h3 class="form-section-title">Formato personalizado (opcional)</h3>
        <p class="form-section-desc">
            Si lo dejas vacío, se usará <code>{PREFIX}{SEP}{NUM}</code> por defecto.
        </p>

        <div class="form-field">
            <label asp-for="Input.FormatoPersonalizado">Formato</label>
            <input asp-for="Input.FormatoPersonalizado" class="form-input" id="formatoInput" placeholder="{PREFIX}{SEP}{NUM}" />
        </div>

        <div class="preview-box">
            <span class="preview-label">Preview:</span>
            <span class="preview-value" id="previewValue">PROV-0001</span>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.EsPredeterminada" type="checkbox" />
                <span>Usar como predeterminada para este tipo</span>
            </label>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.Activa" type="checkbox" checked />
                <span>Serie activa</span>
            </label>
        </div>

        <div class="form-actions">
            <a asp-page="/Series/Index" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Crear serie
            </button>
        </div>
    </form>
</div>

@section Scripts {
    <script>
        (function () {
            var prefijoInput = document.getElementById('Input_Prefijo');
            var sufijoInput = document.getElementById('Input_Sufijo');
            var separadorInput = document.getElementById('Input_Separador');
            var longitudInput = document.getElementById('Input_LongitudNumero');
            var siguienteInput = document.getElementById('Input_SiguienteNumero');
            var formatoInput = document.getElementById('formatoInput');
            var previewValue = document.getElementById('previewValue');

            function actualizarPreview() {
                var prefijo = prefijoInput?.value || '';
                var sufijo = sufijoInput?.value || '';
                var sep = separadorInput?.value || '-';
                var long = parseInt(longitudInput?.value || '4', 10);
                var num = parseInt(siguienteInput?.value || '1', 10);
                var formato = formatoInput?.value || '';

                var numStr = String(num).padStart(long, '0');
                var anio = new Date().getFullYear().toString();
                var mes = String(new Date().getMonth() + 1).padStart(2, '0');
                var dia = String(new Date().getDate()).padStart(2, '0');

                var resultado;
                if (formato.trim()) {
                    resultado = formato
                        .replace(/{PREFIX}/g, prefijo)
                        .replace(/{SUFFIX}/g, sufijo)
                        .replace(/{SEP}/g, sep)
                        .replace(/{YEAR}/g, anio)
                        .replace(/{MONTH}/g, mes)
                        .replace(/{DAY}/g, dia)
                        .replace(/{NUM}/g, numStr);
                } else {
                    resultado = prefijo + sep + numStr;
                }

                if (previewValue) previewValue.textContent = resultado;
            }

            [prefijoInput, sufijoInput, separadorInput, longitudInput, siguienteInput, formatoInput].forEach(function (el) {
                el?.addEventListener('input', actualizarPreview);
            });

            actualizarPreview();
        })();
    </script>
}
````

===== FILE: Pages/Series/Create.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Series
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El tipo es obligatorio")]
            public string Tipo { get; set; } = string.Empty;

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(50)]
            public string Nombre { get; set; } = "Principal";

            [Required(ErrorMessage = "El prefijo es obligatorio")]
            [StringLength(20)]
            public string Prefijo { get; set; } = string.Empty;

            [StringLength(20)]
            public string? Sufijo { get; set; }

            [StringLength(5)]
            public string Separador { get; set; } = "-";

            [Range(1, 10)]
            public int LongitudNumero { get; set; } = 4;

            [Range(1, int.MaxValue)]
            public int SiguienteNumero { get; set; } = 1;

            [StringLength(100)]
            public string? FormatoPersonalizado { get; set; }

            public bool EsPredeterminada { get; set; } = false;
            public bool Activa { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores pueden crear series";
                return RedirectToPage("/Series/Index");
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores pueden crear series";
                return RedirectToPage("/Series/Index");
            }

            if (!ModelState.IsValid) return Page();

            var serie = new SerieDocumento
            {
                Tipo = Input.Tipo,
                Nombre = Input.Nombre,
                Prefijo = Input.Prefijo,
                Sufijo = Input.Sufijo,
                Separador = Input.Separador,
                LongitudNumero = Input.LongitudNumero,
                SiguienteNumero = Input.SiguienteNumero,
                FormatoPersonalizado = Input.FormatoPersonalizado,
                EsPredeterminada = Input.EsPredeterminada,
                Activa = Input.Activa,
                FechaCreacion = DateTime.Now
            };

            // Si es predeterminada, quitar la predeterminada anterior del mismo tipo
            if (serie.EsPredeterminada)
            {
                var anteriores = _context.SeriesDocumentos
                    .Where(s => s.Tipo == serie.Tipo && s.EsPredeterminada)
                    .ToList();
                foreach (var s in anteriores) s.EsPredeterminada = false;
            }

            _context.SeriesDocumentos.Add(serie);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear serie",
                $"Creó la serie '{serie.Nombre}' para {serie.Tipo} con prefijo '{serie.Prefijo}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Serie '{serie.Nombre}' creada correctamente";
            return RedirectToPage("/Series/Index");
        }
    }
}
````

===== FILE: Pages/Series/Delete.cshtml =====

````html
@page "{id:int}"
@model Kirkenta.Pages.Series.DeleteModel
@{
    ViewData["Title"] = "Eliminar serie";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Series/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a series
        </a>
        <h2>Eliminar serie</h2>
    </div>
</div>

<div class="module-card danger-card">
    <div class="danger-icon">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
            <line x1="12" y1="9" x2="12" y2="13"/>
            <line x1="12" y1="17" x2="12.01" y2="17"/>
        </svg>
    </div>

    <h3>¿Eliminar la serie "@Model.Serie.Nombre"?</h3>
    <p>Ya no se podrán generar documentos con esta serie.</p>

    <form method="post" class="form-actions">
        <a asp-page="/Series/Index" class="btn-secondary">Cancelar</a>
        <button type="submit" class="btn-danger">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <polyline points="3 6 5 6 21 6"/>
                <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
            </svg>
            Sí, eliminar
        </button>
    </form>
</div>
````

===== FILE: Pages/Series/Delete.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Series
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public SerieDocumento Serie { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Series/Index");
            }

            var serie = _context.SeriesDocumentos.FirstOrDefault(s => s.Id == id);
            if (serie == null)
            {
                TempData["Error"] = "Serie no encontrada";
                return RedirectToPage("/Series/Index");
            }

            Serie = serie;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Series/Index");
            }

            var serie = _context.SeriesDocumentos.FirstOrDefault(s => s.Id == id);
            if (serie == null)
            {
                TempData["Error"] = "Serie no encontrada";
                return RedirectToPage("/Series/Index");
            }

            var nombre = serie.Nombre;
            _context.SeriesDocumentos.Remove(serie);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar serie",
                $"Eliminó la serie '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Serie '{nombre}' eliminada";
            return RedirectToPage("/Series/Index");
        }
    }
}
````

===== FILE: Pages/Series/Edit.cshtml =====

````html
@page "{id:int}"
@model Kirkenta.Pages.Series.EditModel
@{
    ViewData["Title"] = "Editar serie";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Series/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a series
        </a>
        <h2>Editar serie</h2>
    </div>
</div>

<div class="module-card form-card">
    <form method="post" class="erp-form">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span asp-validation-summary="ModelOnly"></span>
            </div>
        }

        <input type="hidden" asp-for="Input.Id" />

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Tipo">Tipo de documento *</label>
                <select asp-for="Input.Tipo" class="form-input" required>
                    <optgroup label="Ventas">
                        <option value="Cotizacion">Cotización</option>
                        <option value="Pedido">Pedido</option>
                        <option value="Venta">Venta</option>
                        <option value="Factura">Factura</option>
                        <option value="Devolucion">Devolución</option>
                    </optgroup>
                    <optgroup label="Inventario">
                        <option value="Producto">Producto (SKU)</option>
                        <option value="Baja">Baja de inventario</option>
                    </optgroup>
                    <optgroup label="Compras">
                        <option value="Proveedor">Proveedor</option>
                        <option value="OrdenCompra">Orden de compra</option>
                        <option value="PagoProveedor">Pago a proveedor</option>
                        <option value="DevolucionProveedor">Devolución a proveedor</option>
                    </optgroup>
                </select>
            </div>
            <div class="form-field">
                <label asp-for="Input.Nombre">Nombre *</label>
                <input asp-for="Input.Nombre" class="form-input" required />
            </div>
        </div>

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Prefijo">Prefijo *</label>
                <input asp-for="Input.Prefijo" class="form-input" required />
            </div>
            <div class="form-field">
                <label asp-for="Input.Sufijo">Sufijo</label>
                <input asp-for="Input.Sufijo" class="form-input" />
            </div>
        </div>

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Separador">Separador</label>
                <input asp-for="Input.Separador" class="form-input" maxlength="5" />
            </div>
            <div class="form-field">
                <label asp-for="Input.LongitudNumero">Longitud del número</label>
                <input asp-for="Input.LongitudNumero" type="number" min="1" max="10" class="form-input" />
            </div>
        </div>

        <div class="form-field">
            <label asp-for="Input.SiguienteNumero">Siguiente número</label>
            <input asp-for="Input.SiguienteNumero" type="number" min="1" class="form-input" />
            <small class="form-help">Si lo cambias, el próximo documento usará este número</small>
        </div>

        <div class="form-divider"></div>
        <h3 class="form-section-title">Formato personalizado (opcional)</h3>
        <p class="form-section-desc">
            Variables: <code>{PREFIX}</code> <code>{SUFFIX}</code> <code>{SEP}</code> <code>{NUM}</code> <code>{YEAR}</code> <code>{MONTH}</code> <code>{DAY}</code>
        </p>

        <div class="form-field">
            <label asp-for="Input.FormatoPersonalizado">Formato</label>
            <input asp-for="Input.FormatoPersonalizado" class="form-input" id="formatoInput" />
        </div>

        <div class="preview-box">
            <span class="preview-label">Preview:</span>
            <span class="preview-value" id="previewValue">COT-0001</span>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.EsPredeterminada" type="checkbox" />
                <span>Usar como predeterminada para este tipo</span>
            </label>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.Activa" type="checkbox" />
                <span>Serie activa</span>
            </label>
        </div>

        <div class="form-actions">
            <a asp-page="/Series/Index" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Guardar cambios
            </button>
        </div>
    </form>
</div>

@section Scripts {
    <script>
        (function () {
            var prefijoInput = document.getElementById('Input_Prefijo');
            var sufijoInput = document.getElementById('Input_Sufijo');
            var separadorInput = document.getElementById('Input_Separador');
            var longitudInput = document.getElementById('Input_LongitudNumero');
            var siguienteInput = document.getElementById('Input_SiguienteNumero');
            var formatoInput = document.getElementById('formatoInput');
            var previewValue = document.getElementById('previewValue');

            function actualizarPreview() {
                var prefijo = prefijoInput?.value || '';
                var sufijo = sufijoInput?.value || '';
                var sep = separadorInput?.value || '-';
                var long = parseInt(longitudInput?.value || '4', 10);
                var num = parseInt(siguienteInput?.value || '1', 10);
                var formato = formatoInput?.value || '';

                var numStr = String(num).padStart(long, '0');
                var anio = new Date().getFullYear().toString();
                var mes = String(new Date().getMonth() + 1).padStart(2, '0');
                var dia = String(new Date().getDate()).padStart(2, '0');

                var resultado;
                if (formato.trim()) {
                    resultado = formato
                        .replace(/{PREFIX}/g, prefijo)
                        .replace(/{SUFFIX}/g, sufijo)
                        .replace(/{SEP}/g, sep)
                        .replace(/{YEAR}/g, anio)
                        .replace(/{MONTH}/g, mes)
                        .replace(/{DAY}/g, dia)
                        .replace(/{NUM}/g, numStr);
                } else {
                    resultado = prefijo + sep + numStr;
                }

                if (previewValue) previewValue.textContent = resultado;
            }

            [prefijoInput, sufijoInput, separadorInput, longitudInput, siguienteInput, formatoInput].forEach(function (el) {
                el?.addEventListener('input', actualizarPreview);
            });

            actualizarPreview();
        })();
    </script>
}
````

===== FILE: Pages/Series/Edit.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Series
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required]
            public string Tipo { get; set; } = string.Empty;

            [Required]
            [StringLength(50)]
            public string Nombre { get; set; } = "Principal";

            [Required]
            [StringLength(20)]
            public string Prefijo { get; set; } = string.Empty;

            [StringLength(20)]
            public string? Sufijo { get; set; }

            [StringLength(5)]
            public string Separador { get; set; } = "-";

            [Range(1, 10)]
            public int LongitudNumero { get; set; } = 4;

            [Range(1, int.MaxValue)]
            public int SiguienteNumero { get; set; } = 1;

            [StringLength(100)]
            public string? FormatoPersonalizado { get; set; }

            public bool EsPredeterminada { get; set; }
            public bool Activa { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Series/Index");
            }

            var serie = _context.SeriesDocumentos.FirstOrDefault(s => s.Id == id);
            if (serie == null)
            {
                TempData["Error"] = "Serie no encontrada";
                return RedirectToPage("/Series/Index");
            }

            Input = new InputModel
            {
                Id = serie.Id,
                Tipo = serie.Tipo,
                Nombre = serie.Nombre,
                Prefijo = serie.Prefijo,
                Sufijo = serie.Sufijo,
                Separador = serie.Separador,
                LongitudNumero = serie.LongitudNumero,
                SiguienteNumero = serie.SiguienteNumero,
                FormatoPersonalizado = serie.FormatoPersonalizado,
                EsPredeterminada = serie.EsPredeterminada,
                Activa = serie.Activa
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Series/Index");
            }

            if (!ModelState.IsValid) return Page();

            var serie = _context.SeriesDocumentos.FirstOrDefault(s => s.Id == Input.Id);
            if (serie == null)
            {
                TempData["Error"] = "Serie no encontrada";
                return RedirectToPage("/Series/Index");
            }

            // Si es predeterminada, quitar la predeterminada anterior del mismo tipo
            if (Input.EsPredeterminada && !serie.EsPredeterminada)
            {
                var anteriores = _context.SeriesDocumentos
                    .Where(s => s.Tipo == Input.Tipo && s.EsPredeterminada && s.Id != Input.Id)
                    .ToList();
                foreach (var s in anteriores) s.EsPredeterminada = false;
            }

            serie.Tipo = Input.Tipo;
            serie.Nombre = Input.Nombre;
            serie.Prefijo = Input.Prefijo;
            serie.Sufijo = Input.Sufijo;
            serie.Separador = Input.Separador;
            serie.LongitudNumero = Input.LongitudNumero;
            serie.SiguienteNumero = Input.SiguienteNumero;
            serie.FormatoPersonalizado = Input.FormatoPersonalizado;
            serie.EsPredeterminada = Input.EsPredeterminada;
            serie.Activa = Input.Activa;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar serie",
                $"Editó la serie '{serie.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Serie '{serie.Nombre}' actualizada";
            return RedirectToPage("/Series/Index");
        }
    }
}
````

===== FILE: Pages/Series/Index.cshtml =====

````html
@page
@model Kirkenta.Pages.Series.IndexModel
@{
    ViewData["Title"] = "Series de documentos";
}

<div class="module-header">
    <div class="module-header-left">
        <h2>Series de documentos</h2>
        <p>Configura el formato y numeración de tus documentos</p>
    </div>
    <div class="module-header-right">
        <a asp-page="/Series/Create" class="btn-primary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <line x1="12" y1="5" x2="12" y2="19"/>
                <line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            Nueva serie
        </a>
    </div>
</div>

<div class="module-card">
    @if (TempData["Success"] != null)
    {
        <div class="module-alert module-alert-success">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                <polyline points="22 4 12 14.01 9 11.01"/>
            </svg>
            @TempData["Success"]
        </div>
    }

    <div class="table-wrapper">
        <table class="erp-table">
            <thead>
                <tr>
                    <th>Tipo</th>
                    <th>Nombre</th>
                    <th>Formato</th>
                    <th>Próximo número</th>
                    <th>Preview</th>
                    <th>Predeterminada</th>
                    <th>Estado</th>
                    <th style="width: 120px; text-align: right;">Acciones</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var s in Model.Series)
                {
                    <tr>
                        <td><strong>@s.Tipo</strong></td>
                        <td>@s.Nombre</td>
                        <td>
                            <code style="font-family: monospace; font-size: 12px; background: #f3f4f6; padding: 2px 6px; border-radius: 4px;">
                                @(s.FormatoPersonalizado ?? $"{s.Prefijo}{s.Separador}{"NUM"}")
                            </code>
                        </td>
                        <td>@s.SiguienteNumero</td>
                        <td><strong style="color: var(--color-primary);">@Model.Previews[s.Id]</strong></td>
                        <td>
                            @if (s.EsPredeterminada)
                            {
                                <span class="badge badge-success">Sí</span>
                            }
                            else
                            {
                                <span class="table-user-sub">—</span>
                            }
                        </td>
                        <td>
                            @if (s.Activa)
                            {
                                <span class="badge badge-success">Activa</span>
                            }
                            else
                            {
                                <span class="badge badge-danger">Inactiva</span>
                            }
                        </td>
                        <td>
                            <div class="table-actions">
                                <a asp-page="/Series/Edit" asp-route-id="@s.Id" class="btn-icon-action" title="Editar">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                        <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                                        <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                                    </svg>
                                </a>
                                <a asp-page="/Series/Delete" asp-route-id="@s.Id" class="btn-icon-action btn-icon-action-danger" title="Eliminar">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                        <polyline points="3 6 5 6 21 6"/>
                                        <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                                    </svg>
                                </a>
                            </div>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>

    <div class="series-help">
        <h4>💡 Variables disponibles para el formato personalizado</h4>
        <ul>
            <li><code>{PREFIX}</code> — Prefijo (Ej: COT)</li>
            <li><code>{SUFFIX}</code> — Sufijo (Ej: -2026)</li>
            <li><code>{SEP}</code> — Separador (Ej: -)</li>
            <li><code>{NUM}</code> — Número correlativo (Ej: 0001)</li>
            <li><code>{YEAR}</code> — Año (Ej: 2026)</li>
            <li><code>{MONTH}</code> — Mes (Ej: 10)</li>
            <li><code>{DAY}</code> — Día (Ej: 01)</li>
        </ul>
        <p><strong>Ejemplo:</strong> <code>{PREFIX}{SEP}{YEAR}{SEP}{NUM}</code> → COT-2026-0001</p>
    </div>
</div>
````

===== FILE: Pages/Series/Index.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Series
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<SerieDocumento> Series { get; set; } = new();
        public Dictionary<int, string> Previews { get; set; } = new();

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores pueden ver esta configuración";
                return RedirectToPage("/Index");
            }

            Series = _context.SeriesDocumentos.OrderBy(s => s.Tipo).ToList();

            foreach (var serie in Series)
            {
                var numero = serie.SiguienteNumero.ToString().PadLeft(serie.LongitudNumero, '0');
                var prefijo = serie.Prefijo ?? "";
                var sufijo = serie.Sufijo ?? "";
                var sep = serie.Separador ?? "-";
                var anio = DateTime.Now.Year.ToString();
                var mes = DateTime.Now.Month.ToString("D2");
                var dia = DateTime.Now.Day.ToString("D2");

                string preview;
                if (!string.IsNullOrWhiteSpace(serie.FormatoPersonalizado))
                {
                    preview = serie.FormatoPersonalizado
                        .Replace("{PREFIX}", prefijo)
                        .Replace("{SUFFIX}", sufijo)
                        .Replace("{SEP}", sep)
                        .Replace("{YEAR}", anio)
                        .Replace("{MONTH}", mes)
                        .Replace("{DAY}", dia)
                        .Replace("{NUM}", numero);
                }
                else
                {
                    preview = prefijo + sep + numero;
                }

                Previews[serie.Id] = preview;
            }

            return Page();
        }
    }
}
````

====================================================
 Pages\Shared - 4 archivo(s)
====================================================

===== FILE: Pages/Shared/_ImportStepIndicator.cshtml =====

````html
@* Indicador visual de pasos del wizard de importación *@
@* Uso: @await Html.PartialAsync("_ImportStepIndicator", new { Paso = 1 }) *@
@model dynamic
@{
    int pasoActual = (int)(Model?.Paso ?? 1);
}

<div style="display:flex; gap:8px; margin-bottom:20px; align-items:center;">
    @* Paso 1 *@
    @if (pasoActual == 1)
    {
        <span style="background:var(--color-primary); color:#fff; width:28px; height:28px; border-radius:50%; display:inline-flex; align-items:center; justify-content:center; font-weight:700; font-size:13px;">1</span>
        <span style="font-size:13px; font-weight:600; color:var(--color-primary);">Subir archivo</span>
    }
    else
    {
        <span style="background:var(--color-success); color:#fff; width:28px; height:28px; border-radius:50%; display:inline-flex; align-items:center; justify-content:center; font-weight:700; font-size:13px;">✓</span>
        <span style="font-size:13px; color:var(--color-muted);">Subir archivo</span>
    }

    <span style="width:40px; height:2px; background:@(pasoActual >= 2 ? "var(--color-primary)" : "var(--color-border)");"></span>

    @* Paso 2 *@
    @if (pasoActual == 2)
    {
        <span style="background:var(--color-primary); color:#fff; width:28px; height:28px; border-radius:50%; display:inline-flex; align-items:center; justify-content:center; font-weight:700; font-size:13px;">2</span>
        <span style="font-size:13px; font-weight:600; color:var(--color-primary);">Mapear columnas</span>
    }
    else if (pasoActual > 2)
    {
        <span style="background:var(--color-success); color:#fff; width:28px; height:28px; border-radius:50%; display:inline-flex; align-items:center; justify-content:center; font-weight:700; font-size:13px;">✓</span>
        <span style="font-size:13px; color:var(--color-muted);">Mapear columnas</span>
    }
    else
    {
        <span style="background:var(--color-border); color:var(--color-muted); width:28px; height:28px; border-radius:50%; display:inline-flex; align-items:center; justify-content:center; font-weight:700; font-size:13px;">2</span>
        <span style="font-size:13px; color:var(--color-muted);">Mapear columnas</span>
    }

    <span style="width:40px; height:2px; background:var(--color-border);"></span>

    @* Paso 3 *@
    <span style="background:var(--color-border); color:var(--color-muted); width:28px; height:28px; border-radius:50%; display:inline-flex; align-items:center; justify-content:center; font-weight:700; font-size:13px;">3</span>
    <span style="font-size:13px; color:var(--color-muted);">Importar</span>
</div>
````

===== FILE: Pages/Shared/_ImportUploadForm.cshtml =====

````html
@* Formulario del paso 1 del wizard de importación *@
@* Uso: @await Html.PartialAsync("_ImportUploadForm", config) *@
@model Kirkenta.Helpers.Import.ImportWizardConfig
@{
    var error = ViewData["ImportError"] as string;
}

@if (!string.IsNullOrEmpty(error))
{
    <div class="module-alert module-alert-error">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"/>
            <line x1="12" y1="8" x2="12" y2="12"/>
            <line x1="12" y1="16" x2="12.01" y2="16"/>
        </svg>
        @error
    </div>
}

<div class="module-card form-card-wide">
    <form method="post" enctype="multipart/form-data" class="erp-form">

        <div style="padding: 40px 20px; border: 2px dashed var(--color-border); border-radius: var(--radius-md); text-align: center; background: #f9fafb;">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" style="width:48px; height:48px; color:var(--color-muted); opacity:0.5; margin-bottom:12px;">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                <polyline points="17 8 12 3 7 8"/>
                <line x1="12" y1="3" x2="12" y2="15"/>
            </svg>
            <h3 style="font-size:15px; font-weight:600; margin-bottom:6px;">Selecciona tu archivo</h3>
            <p style="font-size:13px; color:var(--color-muted); margin-bottom:16px;">
                Excel (.xlsx) o CSV (.csv) — Máximo 5,000 filas y 20 MB
            </p>
            <input type="file" name="Archivo" accept=".xlsx,.csv" required
                   style="display:block; margin:0 auto; font-size:13px;" />
        </div>

        <div class="module-alert" style="background: #eef2ff; border: 1px solid #c7d2fe; color: var(--color-primary); margin: 16px 0 0 0;">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10"/>
                <line x1="12" y1="16" x2="12" y2="12"/>
                <line x1="12" y1="8" x2="12.01" y2="8"/>
            </svg>
            <div>
                <strong style="display:block; font-size:13px;">No importa si tus columnas tienen otros nombres</strong>
                <span style="font-size:12.5px;">En el siguiente paso podrás mapear cada columna de tu archivo a los campos del sistema.</span>
            </div>
        </div>

        <div class="form-actions">
            <a href="@Model.UrlIndex" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Continuar
            </button>
        </div>
    </form>
</div>
````

===== FILE: Pages/Shared/_Layout.cshtml =====

````html
@using System.Security.Claims
@using Kirkenta.Data
@using Kirkenta.Helpers
@using Kirkenta.Models
@inject ApplicationDbContext DbContext
@{
    var currentPath = Context.Request.Path.Value ?? "/";
    var segments = currentPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    var breadcrumb = new List<(string Label, string Url)>();
    breadcrumb.Add(("Kirkenta", "/Index"));

    var moduleNames = new Dictionary<string, string>
    {
        { "Usuarios", "Usuarios" }, { "Ventas", "Ventas" }, { "Inventario", "Inventario" },
        { "Compras", "Compras" }, { "Finanzas", "Finanzas" }, { "Produccion", "Producción" },
        { "RRHH", "RRHH" }, { "Reportes", "Reportes" }, { "Logistica", "Logística" },
        { "Activos", "Activos" }, { "Seguridad", "Seguridad" }, { "Configuracion", "Configuración" },
        { "Auth", "Autenticación" }, { "Clientes", "Clientes" }, { "Productos", "Productos" },
        { "Categorias", "Categorías" }, { "Series", "Series" }, { "Impuestos", "Impuestos" },
        { "MetodosPago", "Métodos de pago" }, { "UnidadesMedida", "Unidades de medida" },
        { "Cotizaciones", "Cotizaciones" }, { "Pedidos", "Pedidos" }, { "POS", "Punto de venta" },
        { "Facturas", "Facturas" }, { "Devoluciones", "Devoluciones" }, { "Nomenclatura", "Nomenclatura" },
        { "Bajas", "Bajas" }, { "Proveedores", "Proveedores" }, { "OrdenesCompra", "Órdenes de compra" },
        { "PagosProveedor", "Pagos a proveedores" },
        // FINANZAS
        { "Cuentas", "Cuentas financieras" }, { "Movimientos", "Movimientos" },
        { "Vales", "Vales a empleados" }, { "CategoriasFinancieras", "Categorías financieras" },
        { "Aperturas", "Aperturas de caja" }, { "Cierres", "Cierres de caja" },
        { "CierresContables", "Cierres contables" },
        { "PlanCuentas", "Plan de Cuentas" },
        { "Contabilidad", "Contabilidad" },
        // RRHH
        { "Empleados", "Empleados" }, { "Documentos", "Documentos" },
        { "Expedientes", "Expedientes" }, { "Vacaciones", "Vacaciones" },
        { "Permisos", "Permisos y licencias" }, { "Nomina", "Nómina" },
        { "Feriados", "Feriados" }, { "Alertas", "Alertas" },
        // LOGÍSTICA
        { "Envios", "Envíos" }, { "Rutas", "Rutas de despacho" },
        { "Zonas", "Zonas de envío" }, { "Repartidores", "Repartidores" },
        { "Tracking", "Tracking público" }
    };

    var actionNames = new Dictionary<string, string>
    {
        { "Index", "Lista" }, { "Create", "Crear" }, { "Edit", "Editar" }, { "Delete", "Eliminar" },
        { "Roles", "Roles" }, { "RolesCreate", "Nuevo rol" }, { "RolesEdit", "Editar rol" },
        { "RolesDelete", "Eliminar rol" }, { "Perfil", "Mi perfil" },
        { "CambiarPassword", "Cambiar contraseña" }, { "Actividad", "Actividad" },
        { "Login", "Iniciar sesión" }, { "Register", "Registrarse" },
        { "Details", "Detalle" }, { "RegistrarPago", "Registrar pago" },
        { "CreateFromVenta", "Generar factura" }, { "Aprobar", "Aprobar" },
        { "Revertir", "Revertir" }, { "Recibir", "Recibir" },
        { "HistorialProducto", "Historial de producto" },
        { "Anular", "Anular" }, { "Entregar", "Entregar" },
        { "Import", "Importar" }, { "ImportMap", "Mapear columnas" },
        { "Export", "Exportar" }, { "Pagar", "Pagar" },
        { "Reabrir", "Reabrir" },
        { "EstadoResultados", "Estado de Resultados" },
        { "BalanceGeneral", "Balance General" }
    };

    for (int i = 0; i < segments.Length; i++)
    {
        var seg = segments[i];
        var url = "/" + string.Join("/", segments.Take(i + 1));

        string label = seg;
        if (moduleNames.ContainsKey(seg)) label = moduleNames[seg];
        else if (actionNames.ContainsKey(seg)) label = actionNames[seg];

        if (i == segments.Length - 1 && ViewData["Title"] != null)
        {
            var titleStr = ViewData["Title"]?.ToString() ?? label;
            if (!string.IsNullOrWhiteSpace(titleStr)) label = titleStr;
        }

        breadcrumb.Add((label, url));
    }

    var currentUsername = User.Identity?.Name;
    var currentUser = !string.IsNullOrEmpty(currentUsername)
        ? DbContext.Usuarios.FirstOrDefault(u => u.Username == currentUsername)
        : null;

    var currentRol = currentUser?.Rol ?? "Pendiente";
    var esPendiente = currentRol == "Pendiente";
    var esAdmin = currentRol == "Admin";

    var modulosVisibles = PermisoHelper.ModulosVisibles(DbContext, currentRol);

    bool puedeVerModulo(string modulo) => modulosVisibles.Contains(modulo);

    bool puedeVerSubmodulo(string modulo, string submodulo)
    {
        if (esAdmin) return true;
        var subs = PermisoHelper.SubmodulosVisibles(DbContext, currentRol, modulo);
        if (subs.Count == 0) return false;
        return subs.Contains(submodulo);
    }
}
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - Kirkenta ERP</title>
    <link rel="icon" type="image/png" href="~/favicon.png?v=@DateTime.Now.Ticks" />
    <link rel="apple-touch-icon" href="~/favicon.png" />
    <link rel="shortcut icon" type="image/png" href="~/favicon.png" />
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/theme.css?v=@DateTime.Now.Ticks" />
</head>
<body class="app-body">

    <aside class="erp-sidebar" id="erpSidebar">
        <div class="erp-sidebar-header">
            <a asp-page="/Index" class="erp-sidebar-logo">
                <img src="~/images/logo.png" alt="Kirkenta ERP" class="erp-logo-img" />
                <span class="erp-logo-text">Kirkenta</span>
            </a>
        </div>

        <nav class="erp-sidebar-nav">

            <!-- DASHBOARD -->
            <a asp-page="/Index" class="erp-nav-item">
                <span class="erp-nav-icon">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <rect x="3" y="3" width="7" height="9"/>
                        <rect x="14" y="3" width="7" height="5"/>
                        <rect x="14" y="12" width="7" height="9"/>
                        <rect x="3" y="16" width="7" height="5"/>
                    </svg>
                </span>
                <span class="erp-nav-text">Dashboard</span>
            </a>

            <!-- USUARIOS -->
            @if (puedeVerModulo("Usuarios"))
            {
                <div class="erp-nav-group">
                    <button type="button" class="erp-nav-item erp-nav-toggle" data-target="menu-usuarios">
                        <span class="erp-nav-icon">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                                <circle cx="9" cy="7" r="4"/>
                                <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
                                <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                            </svg>
                        </span>
                        <span class="erp-nav-text">Usuarios</span>
                        <span class="erp-nav-arrow">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                        </span>
                    </button>
                    <div class="erp-nav-submenu" id="menu-usuarios">
                        @if (puedeVerSubmodulo("Usuarios", "Index")) { <a asp-page="/Usuarios/Index">📋 Lista de usuarios</a> }
                        @if (puedeVerSubmodulo("Usuarios", "Create")) { <a asp-page="/Usuarios/Create">➕ Nuevo usuario</a> }
                        @if (puedeVerSubmodulo("Usuarios", "Roles")) { <a asp-page="/Usuarios/Roles">🔑 Roles y permisos</a> }
                        @if (puedeVerSubmodulo("Usuarios", "Perfil")) { <a asp-page="/Usuarios/Perfil">👤 Mi perfil</a> }
                        @if (puedeVerSubmodulo("Usuarios", "CambiarPassword")) { <a asp-page="/Usuarios/CambiarPassword">🔒 Cambiar contraseña</a> }
                    </div>
                </div>
            }
            else if (!esPendiente)
            {
                <a asp-page="/Usuarios/Perfil" class="erp-nav-item">
                    <span class="erp-nav-icon">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
                            <circle cx="12" cy="7" r="4"/>
                        </svg>
                    </span>
                    <span class="erp-nav-text">Mi perfil</span>
                </a>
            }

            <!-- VENTAS -->
            @if (puedeVerModulo("Ventas"))
            {
                <div class="erp-nav-group">
                    <button type="button" class="erp-nav-item erp-nav-toggle" data-target="menu-ventas">
                        <span class="erp-nav-icon">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <circle cx="9" cy="21" r="1"/>
                                <circle cx="20" cy="21" r="1"/>
                                <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"/>
                            </svg>
                        </span>
                        <span class="erp-nav-text">Ventas</span>
                        <span class="erp-nav-arrow">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                        </span>
                    </button>
                    <div class="erp-nav-submenu" id="menu-ventas">
                        @if (esAdmin || puedeVerSubmodulo("Ventas", "Create")) { <a asp-page="/POS/Index">🛒 Punto de venta</a> }
                        @if (puedeVerSubmodulo("Ventas", "Index")) { <a asp-page="/Ventas/Index">💰 Ventas</a> }
                        @if (puedeVerSubmodulo("Ventas", "Cotizaciones")) { <a asp-page="/Cotizaciones/Index">📝 Cotizaciones</a> }
                        @if (puedeVerSubmodulo("Ventas", "Pedidos")) { <a asp-page="/Pedidos/Index">📦 Pedidos</a> }
                        @if (puedeVerSubmodulo("Ventas", "Facturas")) { <a asp-page="/Facturas/Index">🧾 Facturas</a> }
                        @if (puedeVerSubmodulo("Ventas", "Clientes")) { <a asp-page="/Clientes/Index">👥 Clientes</a> }
                        <a asp-page="/Devoluciones/Index">↩️ Devoluciones</a>
                    </div>
                </div>
            }

            <!-- INVENTARIO -->
            @if (puedeVerModulo("Inventario"))
            {
                <div class="erp-nav-group">
                    <button type="button" class="erp-nav-item erp-nav-toggle" data-target="menu-inventario">
                        <span class="erp-nav-icon">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/>
                                <polyline points="3.27 6.96 12 12.01 20.73 6.96"/>
                                <line x1="12" y1="22.08" x2="12" y2="12"/>
                            </svg>
                        </span>
                        <span class="erp-nav-text">Inventario</span>
                        <span class="erp-nav-arrow">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                        </span>
                    </button>
                    <div class="erp-nav-submenu" id="menu-inventario">
                        <a asp-page="/Productos/Index">📦 Productos</a>
                        <a asp-page="/Categorias/Index">🏷️ Categorías</a>
                        <a asp-page="/UnidadesMedida/Index">📏 Unidades de medida</a>
                        <a asp-page="/Bajas/Index">🗑️ Bajas de inventario</a>
                    </div>
                </div>
            }

            <!-- COMPRAS -->
            @if (puedeVerModulo("Compras"))
            {
                <div class="erp-nav-group">
                    <button type="button" class="erp-nav-item erp-nav-toggle" data-target="menu-compras">
                        <span class="erp-nav-icon">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <circle cx="9" cy="21" r="1"/>
                                <circle cx="20" cy="21" r="1"/>
                                <path d="M1 1h15v13H1z"/>
                                <path d="M16 8h4l3 3v3h-7z"/>
                            </svg>
                        </span>
                        <span class="erp-nav-text">Compras</span>
                        <span class="erp-nav-arrow">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                        </span>
                    </button>
                    <div class="erp-nav-submenu" id="menu-compras">
                        <a asp-page="/Compras/Index">📊 Dashboard</a>
                        <a asp-page="/Proveedores/Index">🏢 Proveedores</a>
                        <a asp-page="/OrdenesCompra/Index">📦 Órdenes de compra</a>
                        <a asp-page="/PagosProveedor/Index">💳 Pagos a proveedores</a>
                        <a asp-page="/PagosProveedor/Index" asp-route-tab="porpagar">💸 Cuentas por pagar</a>
                        <a asp-page="/Reportes/Compras">📈 Reportes de compras</a>
                    </div>
                </div>
            }

            <!-- FINANZAS -->
            @if (puedeVerModulo("Finanzas"))
            {
                <div class="erp-nav-group">
                    <button type="button" class="erp-nav-item erp-nav-toggle" data-target="menu-finanzas">
                        <span class="erp-nav-icon">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <line x1="12" y1="1" x2="12" y2="23"/>
                                <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
                            </svg>
                        </span>
                        <span class="erp-nav-text">Finanzas</span>
                        <span class="erp-nav-arrow">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                        </span>
                    </button>
                    <div class="erp-nav-submenu" id="menu-finanzas">
                        @if (puedeVerSubmodulo("Finanzas", "Index")) { <a asp-page="/Finanzas/Index">📊 Dashboard</a> }
                        @if (puedeVerSubmodulo("Finanzas", "Cuentas")) { <a asp-page="/Finanzas/Cuentas/Index">💳 Cuentas financieras</a> }
                        @if (puedeVerSubmodulo("Finanzas", "Categorias")) { <a asp-page="/Finanzas/Categorias/Index">🏷️ Categorías</a> }
                        @if (puedeVerSubmodulo("Finanzas", "Movimientos")) { <a asp-page="/Finanzas/Movimientos/Index">💵 Movimientos</a> }
                        @if (puedeVerSubmodulo("Finanzas", "Aperturas")) { <a asp-page="/Finanzas/Aperturas/Index">🔓 Aperturas de caja</a> }
                        @if (puedeVerSubmodulo("Finanzas", "Cierres")) { <a asp-page="/Finanzas/Cierres/Index">🔐 Cierres de caja</a> }
                        @if (puedeVerSubmodulo("Finanzas", "CierresContables")) { <a asp-page="/Finanzas/CierresContables/Index">📅 Cierres contables</a> }

                        @if (puedeVerSubmodulo("Finanzas", "PlanCuentas"))
                        {
                            <div class="erp-nav-submenu-divider"></div>
                            <a asp-page="/Finanzas/PlanCuentas/Index">📒 Plan de Cuentas</a>
                        }
                        @if (puedeVerSubmodulo("Finanzas", "ContabilidadEstadoResultados"))
                        {
                            <a asp-page="/Finanzas/Contabilidad/EstadoResultados">📈 Estado de Resultados</a>
                        }
                        @if (puedeVerSubmodulo("Finanzas", "ContabilidadBalanceGeneral"))
                        {
                            <a asp-page="/Finanzas/Contabilidad/BalanceGeneral">⚖️ Balance General</a>
                        }

                        @if (puedeVerSubmodulo("Finanzas", "Reportes")) { <a asp-page="/Finanzas/Reportes/Index">📈 Reportes</a> }
                    </div>
                </div>
            }

            <!-- PRODUCCIÓN -->
            @if (puedeVerModulo("Produccion"))
            {
                <div class="erp-nav-group">
                    <button type="button" class="erp-nav-item erp-nav-toggle" data-target="menu-produccion">
                        <span class="erp-nav-icon">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M2 20h20"/>
                                <path d="M4 20V10l5 3V10l5 3V6l5 3v11"/>
                            </svg>
                        </span>
                        <span class="erp-nav-text">Producción</span>
                        <span class="erp-nav-arrow">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                        </span>
                    </button>
                    <div class="erp-nav-submenu" id="menu-produccion">
                        @if (puedeVerSubmodulo("Produccion", "Index")) { <a asp-page="/Produccion/Index">🏭 Órdenes de producción</a> }
                        @if (puedeVerSubmodulo("Produccion", "Calidad")) { <a asp-page="/Produccion/Calidad">✅ Calidad</a> }
                    </div>
                </div>
            }

            <!-- RRHH -->
            @if (puedeVerModulo("RRHH"))
            {
                <div class="erp-nav-group">
                    <button type="button" class="erp-nav-item erp-nav-toggle" data-target="menu-rrhh">
                        <span class="erp-nav-icon">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
                                <circle cx="12" cy="7" r="4"/>
                            </svg>
                        </span>
                        <span class="erp-nav-text">RRHH</span>
                        <span class="erp-nav-arrow">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                        </span>
                    </button>
                    <div class="erp-nav-submenu" id="menu-rrhh">
                        @if (puedeVerSubmodulo("RRHH", "Index")) { <a asp-page="/RRHH/Index">📊 Dashboard</a> }
                        @if (puedeVerSubmodulo("RRHH", "Empleados")) { <a asp-page="/RRHH/Empleados/Index">👥 Empleados</a> }
                        @if (puedeVerSubmodulo("RRHH", "Vales")) { <a asp-page="/RRHH/Vales/Index">🎫 Vales</a> }
                        @if (puedeVerSubmodulo("RRHH", "Vacaciones")) { <a asp-page="/RRHH/Vacaciones/Index">🏖️ Vacaciones</a> }
                        @if (puedeVerSubmodulo("RRHH", "Permisos")) { <a asp-page="/RRHH/Permisos/Index">📝 Permisos y licencias</a> }
                        @if (puedeVerSubmodulo("RRHH", "Nomina")) { <a asp-page="/RRHH/Nomina/Index">💵 Nómina</a> }
                        @if (puedeVerSubmodulo("RRHH", "Reportes")) { <a asp-page="/RRHH/Reportes/Index">📈 Reportes</a> }
                    </div>
                </div>
            }

            <!-- REPORTES -->
            @if (puedeVerModulo("Reportes"))
            {
                <div class="erp-nav-group">
                    <button type="button" class="erp-nav-item erp-nav-toggle" data-target="menu-reportes">
                        <span class="erp-nav-icon">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <line x1="18" y1="20" x2="18" y2="10"/>
                                <line x1="12" y1="20" x2="12" y2="4"/>
                                <line x1="6" y1="20" x2="6" y2="14"/>
                            </svg>
                        </span>
                        <span class="erp-nav-text">Reportes</span>
                        <span class="erp-nav-arrow">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                        </span>
                    </button>
                    <div class="erp-nav-submenu" id="menu-reportes">
                        <a asp-page="/Reportes/Ventas">📈 Reportes de ventas</a>
                        <a asp-page="/Reportes/Compras">🚚 Reportes de compras</a>
                    </div>
                </div>
            }

            <!-- LOGÍSTICA -->
            @if (puedeVerModulo("Logistica"))
            {
                <div class="erp-nav-group">
                    <button type="button" class="erp-nav-item erp-nav-toggle" data-target="menu-logistica">
                        <span class="erp-nav-icon">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <rect x="1" y="3" width="15" height="13"/>
                                <polygon points="16 8 20 8 23 11 23 16 16 16 16 8"/>
                                <circle cx="5.5" cy="18.5" r="2.5"/>
                                <circle cx="18.5" cy="18.5" r="2.5"/>
                            </svg>
                        </span>
                        <span class="erp-nav-text">Logística</span>
                        <span class="erp-nav-arrow">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                        </span>
                    </button>
                    <div class="erp-nav-submenu" id="menu-logistica">
                        @if (puedeVerSubmodulo("Logistica", "Index")) { <a asp-page="/Logistica/Index">📊 Dashboard</a> }
                        @if (puedeVerSubmodulo("Logistica", "Envios")) { <a asp-page="/Logistica/Envios/Index">🚚 Envíos</a> }
                        @if (puedeVerSubmodulo("Logistica", "Rutas")) { <a asp-page="/Logistica/Rutas/Index">🗺️ Rutas</a> }
                        @if (puedeVerSubmodulo("Logistica", "Zonas")) { <a asp-page="/Logistica/Zonas/Index">📍 Zonas de envío</a> }
                        @if (puedeVerSubmodulo("Logistica", "Repartidores")) { <a asp-page="/Logistica/Repartidores/Index">🏍️ Repartidores</a> }
                        <div class="erp-nav-submenu-divider"></div>
                        <a asp-page="/Logistica/Tracking/Index">🔍 Tracking público</a>
                        @if (puedeVerSubmodulo("Logistica", "Reportes")) { <a asp-page="/Logistica/Reportes/Index">📈 Reportes</a> }
                    </div>
                </div>
            }

            <!-- ACTIVOS -->
            @if (puedeVerModulo("Activos"))
            {
                <div class="erp-nav-group">
                    <button type="button" class="erp-nav-item erp-nav-toggle" data-target="menu-activos">
                        <span class="erp-nav-icon">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <rect x="4" y="2" width="16" height="20" rx="2"/>
                                <path d="M9 6h6M9 10h6M9 14h4"/>
                            </svg>
                        </span>
                        <span class="erp-nav-text">Activos</span>
                        <span class="erp-nav-arrow">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                        </span>
                    </button>
                    <div class="erp-nav-submenu" id="menu-activos">
                        @if (puedeVerSubmodulo("Activos", "Index")) { <a asp-page="/Activos/Index">🏛️ Inventario</a> }
                        @if (puedeVerSubmodulo("Activos", "Mantenimiento")) { <a asp-page="/Activos/Mantenimiento">🔧 Mantenimiento</a> }
                    </div>
                </div>
            }

            <!-- SEGURIDAD -->
            @if (puedeVerModulo("Seguridad"))
            {
                <div class="erp-nav-group">
                    <button type="button" class="erp-nav-item erp-nav-toggle" data-target="menu-seguridad">
                        <span class="erp-nav-icon">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
                            </svg>
                        </span>
                        <span class="erp-nav-text">Seguridad</span>
                        <span class="erp-nav-arrow">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                        </span>
                    </button>
                    <div class="erp-nav-submenu" id="menu-seguridad">
                        @if (puedeVerSubmodulo("Seguridad", "Index")) { <a asp-page="/Seguridad/Index">🛡️ Usuarios y roles</a> }
                        @if (puedeVerSubmodulo("Seguridad", "Auditoria")) { <a asp-page="/Seguridad/Auditoria">📋 Auditoría</a> }
                    </div>
                </div>
            }

            <!-- CONFIGURACIÓN -->
            @if (esAdmin)
            {
                <div class="erp-nav-group">
                    <button type="button" class="erp-nav-item erp-nav-toggle" data-target="menu-config">
                        <span class="erp-nav-icon">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <circle cx="12" cy="12" r="3"/>
                                <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/>
                            </svg>
                        </span>
                        <span class="erp-nav-text">Configuración</span>
                        <span class="erp-nav-arrow">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                        </span>
                    </button>
                    <div class="erp-nav-submenu" id="menu-config">
                        <a asp-page="/Configuracion/Index">🏢 Datos de la empresa</a>
                        <a asp-page="/Nomenclatura/Index">🔢 Nomenclatura</a>
                        <a asp-page="/MetodosPago/Index">💳 Métodos de pago</a>
                    </div>
                </div>
            }

        </nav>

        <div class="erp-sidebar-footer">
            <div class="erp-user">
                <div class="erp-user-avatar">
                    @(User.Identity?.Name != null && User.Identity.Name.Length > 0
                        ? User.Identity.Name.Substring(0, 1).ToUpper()
                        : "?")
                </div>
                <div class="erp-user-meta">
                    <span class="erp-user-name">@User.Identity?.Name</span>
                    <span class="erp-user-role">@currentRol</span>
                </div>
            </div>
        </div>
    </aside>

    <div class="erp-main-wrapper">
        <header class="erp-topbar">
            <nav class="erp-breadcrumb" aria-label="Breadcrumb">
                @for (int i = 0; i < breadcrumb.Count; i++)
                {
                    var item = breadcrumb[i];
                    var isLast = i == breadcrumb.Count - 1;

                    if (isLast)
                    {
                        <span class="erp-breadcrumb-current">@item.Label</span>
                    }
                    else
                    {
                        <a href="@item.Url" class="erp-breadcrumb-link">@item.Label</a>
                        <svg class="erp-breadcrumb-sep" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <polyline points="9 18 15 12 9 6"/>
                        </svg>
                    }
                }
            </nav>

            <div class="erp-topbar-right">
                <form method="post" asp-page="/Auth/Logout" class="erp-logout-form">
                    <button type="submit" class="erp-logout-btn">Salir</button>
                </form>
            </div>
        </header>

        <main role="main" class="erp-main">

            @if (esPendiente)
            {
                <div class="pending-banner">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <circle cx="12" cy="12" r="10"/>
                        <line x1="12" y1="8" x2="12" y2="12"/>
                        <line x1="12" y1="16" x2="12.01" y2="16"/>
                    </svg>
                    <div>
                        <strong>Tu cuenta está pendiente de aprobación</strong>
                        <p>Un administrador debe asignarte un rol para que puedas acceder a los módulos del sistema.</p>
                    </div>
                </div>
            }

            @if (TempData["Error"] != null)
            {
                <div class="module-alert module-alert-error">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <circle cx="12" cy="12" r="10"/>
                        <line x1="12" y1="8" x2="12" y2="12"/>
                        <line x1="12" y1="16" x2="12.01" y2="16"/>
                    </svg>
                    @TempData["Error"]
                </div>
            }

            @RenderBody()
        </main>

        <footer class="erp-footer">
            <p>&copy; 2026 Kirkenta ERP — Todos los derechos reservados</p>
        </footer>
    </div>

    <script src="~/lib/jquery/dist/jquery.min.js"></script>
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    <script src="~/js/site.js" asp-append-version="true"></script>

    <script>
        (function () {
            var toggles = document.querySelectorAll('.erp-nav-toggle');
            toggles.forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var targetId = btn.getAttribute('data-target');
                    var submenu = document.getElementById(targetId);
                    if (!submenu) return;

                    document.querySelectorAll('.erp-nav-toggle.is-expanded').forEach(function (other) {
                        if (other !== btn) {
                            other.classList.remove('is-expanded');
                            var otherId = other.getAttribute('data-target');
                            var otherSub = document.getElementById(otherId);
                            if (otherSub) otherSub.classList.remove('is-open');
                        }
                    });

                    btn.classList.toggle('is-expanded');
                    submenu.classList.toggle('is-open');
                });
            });
        })();
    </script>

    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
````

===== FILE: Pages/Shared/_ValidationScriptsPartial.cshtml =====

````html
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"></script>
````

====================================================
 Pages\UnidadesMedida - 14 archivo(s)
====================================================

===== FILE: Pages/UnidadesMedida/Create.cshtml =====

````html
@page
@model Kirkenta.Pages.UnidadesMedida.CreateModel
@{
    ViewData["Title"] = "Nueva unidad de medida";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/UnidadesMedida/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a unidades
        </a>
        <h2>Nueva unidad de medida</h2>
    </div>
</div>

<div class="module-card form-card">
    <form method="post" class="erp-form">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span asp-validation-summary="ModelOnly"></span>
            </div>
        }

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Nombre">Nombre *</label>
                <input asp-for="Input.Nombre" class="form-input" placeholder="Ej: Botella" required />
            </div>
            <div class="form-field">
                <label asp-for="Input.Abreviatura">Abreviatura *</label>
                <input asp-for="Input.Abreviatura" class="form-input" placeholder="Ej: Bot" maxlength="10" required />
            </div>
        </div>

        <div class="form-field">
            <label asp-for="Input.Descripcion">Descripción</label>
            <textarea asp-for="Input.Descripcion" class="form-input" rows="2" placeholder="Detalles de la unidad..."></textarea>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.Activa" type="checkbox" checked />
                <span>Unidad activa</span>
            </label>
        </div>

        <div class="form-actions">
            <a asp-page="/UnidadesMedida/Index" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Crear unidad
            </button>
        </div>
    </form>
</div>
````

===== FILE: Pages/UnidadesMedida/Create.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.UnidadesMedida
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(50)]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "La abreviatura es obligatoria")]
            [StringLength(10)]
            public string Abreviatura { get; set; } = string.Empty;

            [StringLength(200)]
            public string? Descripcion { get; set; }

            public bool Activa { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/UnidadesMedida/Index");
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            if (!ModelState.IsValid) return Page();

            if (_context.UnidadesMedida.Any(u => u.Nombre.ToLower() == Input.Nombre.ToLower()))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe una unidad con este nombre");
                return Page();
            }

            var unidad = new UnidadMedida
            {
                Nombre = Input.Nombre,
                Abreviatura = Input.Abreviatura,
                Descripcion = Input.Descripcion,
                Activa = Input.Activa,
                FechaCreacion = DateTime.Now
            };

            _context.UnidadesMedida.Add(unidad);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear unidad de medida",
                $"Creó la unidad '{unidad.Nombre} ({unidad.Abreviatura})'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Unidad '{unidad.Nombre}' creada correctamente";
            return RedirectToPage("/UnidadesMedida/Index");
        }
    }
}
````

===== FILE: Pages/UnidadesMedida/Delete.cshtml =====

````html
@page "{id:int}"
@model Kirkenta.Pages.UnidadesMedida.DeleteModel
@{
    ViewData["Title"] = "Eliminar unidad de medida";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/UnidadesMedida/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a unidades
        </a>
        <h2>Eliminar unidad</h2>
    </div>
</div>

<div class="module-card danger-card">
    <div class="danger-icon">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
            <line x1="12" y1="9" x2="12" y2="13"/>
            <line x1="12" y1="17" x2="12.01" y2="17"/>
        </svg>
    </div>

    <h3>¿Eliminar la unidad "@Model.Unidad.Nombre"?</h3>
    <p>Los productos que la usen quedarán con la unidad por defecto.</p>

    <form method="post" class="form-actions">
        <a asp-page="/UnidadesMedida/Index" class="btn-secondary">Cancelar</a>
        <button type="submit" class="btn-danger">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <polyline points="3 6 5 6 21 6"/>
                <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
            </svg>
            Sí, eliminar
        </button>
    </form>
</div>
````

===== FILE: Pages/UnidadesMedida/Delete.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.UnidadesMedida
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public UnidadMedida Unidad { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            var unidad = _context.UnidadesMedida.FirstOrDefault(u => u.Id == id);
            if (unidad == null)
            {
                TempData["Error"] = "Unidad no encontrada";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            Unidad = unidad;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            var unidad = _context.UnidadesMedida.FirstOrDefault(u => u.Id == id);
            if (unidad == null)
            {
                TempData["Error"] = "Unidad no encontrada";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            var nombre = unidad.Nombre;
            _context.UnidadesMedida.Remove(unidad);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar unidad de medida",
                $"Eliminó la unidad '{nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Unidad '{nombre}' eliminada";
            return RedirectToPage("/UnidadesMedida/Index");
        }
    }
}
````

===== FILE: Pages/UnidadesMedida/Edit.cshtml =====

````html
@page "{id:int}"
@model Kirkenta.Pages.UnidadesMedida.EditModel
@{
    ViewData["Title"] = "Editar unidad de medida";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/UnidadesMedida/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a unidades
        </a>
        <h2>Editar unidad</h2>
    </div>
</div>

<div class="module-card form-card">
    <form method="post" class="erp-form">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span asp-validation-summary="ModelOnly"></span>
            </div>
        }

        <input type="hidden" asp-for="Input.Id" />

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Nombre">Nombre *</label>
                <input asp-for="Input.Nombre" class="form-input" required />
            </div>
            <div class="form-field">
                <label asp-for="Input.Abreviatura">Abreviatura *</label>
                <input asp-for="Input.Abreviatura" class="form-input" maxlength="10" required />
            </div>
        </div>

        <div class="form-field">
            <label asp-for="Input.Descripcion">Descripción</label>
            <textarea asp-for="Input.Descripcion" class="form-input" rows="2"></textarea>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.Activa" type="checkbox" />
                <span>Unidad activa</span>
            </label>
        </div>

        <div class="form-actions">
            <a asp-page="/UnidadesMedida/Index" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Guardar cambios
            </button>
        </div>
    </form>
</div>
````

===== FILE: Pages/UnidadesMedida/Edit.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.UnidadesMedida
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(50)]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "La abreviatura es obligatoria")]
            [StringLength(10)]
            public string Abreviatura { get; set; } = string.Empty;

            [StringLength(200)]
            public string? Descripcion { get; set; }

            public bool Activa { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            var unidad = _context.UnidadesMedida.FirstOrDefault(u => u.Id == id);
            if (unidad == null)
            {
                TempData["Error"] = "Unidad no encontrada";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            Input = new InputModel
            {
                Id = unidad.Id,
                Nombre = unidad.Nombre,
                Abreviatura = unidad.Abreviatura,
                Descripcion = unidad.Descripcion,
                Activa = unidad.Activa
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            if (!ModelState.IsValid) return Page();

            var unidad = _context.UnidadesMedida.FirstOrDefault(u => u.Id == Input.Id);
            if (unidad == null)
            {
                TempData["Error"] = "Unidad no encontrada";
                return RedirectToPage("/UnidadesMedida/Index");
            }

            if (_context.UnidadesMedida.Any(u => u.Nombre.ToLower() == Input.Nombre.ToLower() && u.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe una unidad con este nombre");
                return Page();
            }

            unidad.Nombre = Input.Nombre;
            unidad.Abreviatura = Input.Abreviatura;
            unidad.Descripcion = Input.Descripcion;
            unidad.Activa = Input.Activa;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar unidad de medida",
                $"Editó la unidad '{unidad.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Unidad '{unidad.Nombre}' actualizada";
            return RedirectToPage("/UnidadesMedida/Index");
        }
    }
}
````

===== FILE: Pages/UnidadesMedida/Export.cshtml =====

````html
@page
@model Kirkenta.Pages.UnidadesMedida.ExportModel
@{
    Layout = null;
}
````

===== FILE: Pages/UnidadesMedida/Export.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers.Export;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.UnidadesMedida
{
    public class ExportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ExportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(string formato = "excel", bool plantilla = false)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin") return RedirectToPage("/UnidadesMedida/Index");

            var columns = ExportColumns.UnidadesMedida();

            if (plantilla)
            {
                var vacio = GenerarArchivo(new List<UnidadMedida>(), columns, formato, esPlantilla: true);
                return File(vacio, ContentType(formato), NombreArchivo("plantilla_unidades", formato));
            }

            var lista = _context.UnidadesMedida.OrderBy(u => u.Nombre).ToList();
            var archivo = GenerarArchivo(lista, columns, formato, esPlantilla: false);
            return File(archivo, ContentType(formato), NombreArchivo("unidades", formato));
        }

        private static byte[] GenerarArchivo<T>(List<T> items, List<ExportColumn<T>> columns, string formato, bool esPlantilla)
        {
            var titulo = esPlantilla ? "Plantilla de importación de unidades" : "Listado de unidades";
            return formato.ToLower() switch
            {
                "csv" => CsvExporter.Export(items, columns),
                "json" => JsonExporter.Export(items),
                _ => ExcelExporter.Export(items, columns, "Unidades", titulo)
            };
        }

        private static string ContentType(string formato) => formato.ToLower() switch
        {
            "csv" => "text/csv",
            "json" => "application/json",
            _ => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        private static string NombreArchivo(string baseNombre, string formato)
        {
            var ext = formato.ToLower() switch { "csv" => "csv", "json" => "json", _ => "xlsx" };
            return $"{baseNombre}_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
        }
    }
}
````

===== FILE: Pages/UnidadesMedida/Import.cshtml =====

````html
@page
@model Kirkenta.Pages.UnidadesMedida.ImportModel
@{
    ViewData["Title"] = Model.WizardConfig.TituloPagina;
}

<div class="module-header">
    <div class="module-header-left">
        <a href="@Model.WizardConfig.UrlIndex" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a @Model.WizardConfig.NombrePlural
        </a>
        <h2>@Model.WizardConfig.TituloPagina</h2>
        <p>Sube un archivo Excel o CSV con las @Model.WizardConfig.NombrePlural a importar</p>
    </div>
</div>

@await Html.PartialAsync("_ImportStepIndicator", new { Paso = 1 })

@await Html.PartialAsync("_ImportUploadForm", Model.WizardConfig)
````

===== FILE: Pages/UnidadesMedida/Import.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers.Import;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.UnidadesMedida
{
    public class ImportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public IFormFile? Archivo { get; set; }

        public ImportWizardConfig WizardConfig { get; set; } = new();

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            WizardConfig = BuildConfig();
            if (currentRol != "Admin") return Redirect(WizardConfig.UrlIndex);

            ImportWizardHelper.LimpiarSesion(HttpContext.Session, WizardConfig);
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            WizardConfig = BuildConfig();
            if (currentRol != "Admin") return Redirect(WizardConfig.UrlIndex);

            var resultado = ImportWizardHelper.ProcesarArchivo(Archivo);
            if (!resultado.Ok)
            {
                ViewData["ImportError"] = resultado.Error;
                return Page();
            }

            ImportWizardHelper.GuardarEnSesion(HttpContext.Session, WizardConfig, resultado.Headers, resultado.Rows);
            return Redirect(WizardConfig.UrlImportMap);
        }

        private static ImportWizardConfig BuildConfig()
        {
            return new ImportWizardConfig
            {
                NombrePlural = "unidades de medida",
                NombreSingular = "unidad",
                ModuloPermiso = "Configuracion",
                SubmoduloPermiso = "UnidadesMedida",
                SessionKey = "UnidadesMedida",
                UrlIndex = "/UnidadesMedida/Index",
                UrlImport = "/UnidadesMedida/Import",
                UrlImportMap = "/UnidadesMedida/ImportMap",
                CamposRequeridos = new[] { "Nombre", "Abreviatura" }
            };
        }
    }
}
````

===== FILE: Pages/UnidadesMedida/ImportMap.cshtml =====

````html
@page
@model Kirkenta.Pages.UnidadesMedida.ImportMapModel
@{
    ViewData["Title"] = Model.WizardConfig.TituloMapa;
}

<div class="module-header">
    <div class="module-header-left">
        <a href="@Model.WizardConfig.UrlImport" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver al paso anterior
        </a>
        <h2>@Model.WizardConfig.TituloMapa</h2>
        <p>Asocia cada campo del sistema con una columna de tu archivo</p>
    </div>
</div>

@await Html.PartialAsync("_ImportStepIndicator", new { Paso = 2 })

<form method="post">
    <div class="module-card" style="padding: 28px; margin-bottom: 20px;">
        <h3 class="form-section-title">Mapeo de columnas</h3>
        <p class="form-section-desc">
            Detectadas <strong>@Model.HeadersArchivo.Count columnas</strong> y
            <strong>@Model.FilasArchivo.Count filas</strong>.
        </p>

        <div class="table-wrapper" style="margin-top: 16px;">
            <table class="erp-table">
                <thead>
                    <tr>
                        <th>Campo del sistema</th>
                        <th>Columna de tu archivo</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var campo in Model.WizardConfig.Campos)
                    {
                        var sugerido = Model.MapaSugerido.GetValueOrDefault(campo.Key);
                        <tr>
                            <td>
                                <strong>@campo.Label</strong>
                                @if (campo.Requerido)
                                {
                                    <span style="color: var(--color-danger);">*</span>
                                }
                            </td>
                            <td>
                                <select name="Mapeo[@campo.Key]" class="form-input">
                                    <option value="">— Ignorar este campo —</option>
                                    @foreach (var header in Model.HeadersArchivo)
                                    {
                                        if (header == sugerido)
                                        {
                                            <option value="@header" selected>@header</option>
                                        }
                                        else
                                        {
                                            <option value="@header">@header</option>
                                        }
                                    }
                                </select>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </div>

    <div class="module-card" style="padding: 28px; margin-bottom: 20px;">
        <h3 class="form-section-title">Vista previa (primeras 5 filas)</h3>
        <div class="table-wrapper" style="margin-top: 16px;">
            <table class="erp-table" style="font-size: 12.5px;">
                <thead>
                    <tr>
                        @foreach (var header in Model.HeadersArchivo)
                        {
                            <th>@header</th>
                        }
                    </tr>
                </thead>
                <tbody>
                    @foreach (var fila in Model.FilasArchivo.Take(5))
                    {
                        <tr>
                            @foreach (var header in Model.HeadersArchivo)
                            {
                                <td>@fila.GetValueOrDefault(header)</td>
                            }
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </div>

    <div class="form-actions" style="border-top:none; margin-top:0;">
        <a href="@Model.WizardConfig.UrlImport" class="btn-secondary">Atrás</a>
        <button type="submit" class="btn-primary" style="background: var(--color-success);">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <polyline points="20 6 9 17 4 12"/>
            </svg>
            Importar @Model.FilasArchivo.Count unidades
        </button>
    </div>
</form>
````

===== FILE: Pages/UnidadesMedida/ImportMap.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.UnidadesMedida
{
    public class ImportMapModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportMapModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public ImportWizardConfig WizardConfig { get; set; } = new();
        public List<string> HeadersArchivo { get; set; } = new();
        public List<Dictionary<string, string>> FilasArchivo { get; set; } = new();
        public Dictionary<string, string?> MapaSugerido { get; set; } = new();

        [BindProperty]
        public Dictionary<string, string> Mapeo { get; set; } = new();

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            WizardConfig = BuildConfig();
            if (currentRol != "Admin") return Redirect(WizardConfig.UrlIndex);

            if (!CargarDesdeSesion())
            {
                TempData["Error"] = "La sesión expiró.";
                return Redirect(WizardConfig.UrlImport);
            }

            MapaSugerido = ColumnMapper.Detectar(HeadersArchivo, WizardConfig.AliasCampos);
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            WizardConfig = BuildConfig();
            if (currentRol != "Admin") return Redirect(WizardConfig.UrlIndex);

            if (!CargarDesdeSesion())
            {
                TempData["Error"] = "La sesión expiró.";
                return Redirect(WizardConfig.UrlImport);
            }

            var faltantes = WizardConfig.CamposRequeridos
                .Where(campo => string.IsNullOrEmpty(Mapeo.GetValueOrDefault(campo)))
                .ToList();

            if (faltantes.Count > 0)
            {
                var labelsFaltantes = WizardConfig.Campos.Where(c => faltantes.Contains(c.Key)).Select(c => c.Label);
                TempData["Error"] = $"Debes mapear: {string.Join(", ", labelsFaltantes)}";
                return Redirect(WizardConfig.UrlImportMap);
            }

            var resultado = ImportarUnidades(FilasArchivo, Mapeo, currentUser?.Id);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Importar unidades de medida",
                $"Importó unidades: {resultado.Resumen()}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            ImportWizardHelper.LimpiarSesion(HttpContext.Session, WizardConfig);

            TempData["Success"] = $"Importación completada: {resultado.Resumen()}";
            return Redirect(WizardConfig.UrlIndex);
        }

        private bool CargarDesdeSesion()
        {
            var data = ImportWizardHelper.RecuperarDeSesion(HttpContext.Session, WizardConfig);
            if (data == null) return false;
            HeadersArchivo = data.Value.Headers;
            FilasArchivo = data.Value.Rows;
            return true;
        }

        private static ImportWizardConfig BuildConfig()
        {
            return new ImportWizardConfig
            {
                NombrePlural = "unidades de medida",
                NombreSingular = "unidad",
                ModuloPermiso = "Configuracion",
                SubmoduloPermiso = "UnidadesMedida",
                SessionKey = "UnidadesMedida",
                UrlIndex = "/UnidadesMedida/Index",
                UrlImport = "/UnidadesMedida/Import",
                UrlImportMap = "/UnidadesMedida/ImportMap",
                CamposRequeridos = new[] { "Nombre", "Abreviatura" },
                Campos = new List<CampoMapeo>
                {
                    new() { Key = "Nombre",      Label = "Nombre",       Requerido = true },
                    new() { Key = "Abreviatura", Label = "Abreviatura",  Requerido = true, Ayuda = "Máx 10 caracteres" },
                    new() { Key = "Descripcion", Label = "Descripción",  Requerido = false },
                    new() { Key = "Activa",      Label = "Activa (Sí/No)", Requerido = false },
                },
                AliasCampos = new Dictionary<string, string[]>
                {
                    { "Nombre",      new[] { "nombre", "name", "unidad", "medida" } },
                    { "Abreviatura", new[] { "abreviatura", "abrev", "abbr", "sigla", "short" } },
                    { "Descripcion", new[] { "descripcion", "description", "detalle" } },
                    { "Activa",      new[] { "activa", "activo", "active", "estado" } },
                }
            };
        }

        private ImportResult ImportarUnidades(
            List<Dictionary<string, string>> filas,
            Dictionary<string, string> mapa,
            int? usuarioId)
        {
            var resultado = new ImportResult { TotalFilas = filas.Count };

            int filaNum = 1;
            foreach (var fila in filas)
            {
                filaNum++;
                try
                {
                    string? Obtener(string campo)
                    {
                        var header = mapa.GetValueOrDefault(campo);
                        if (string.IsNullOrEmpty(header)) return null;
                        return fila.GetValueOrDefault(header)?.Trim();
                    }

                    var nombre = Obtener("Nombre");
                    if (string.IsNullOrWhiteSpace(nombre))
                    {
                        resultado.AgregarError(filaNum, "El campo 'Nombre' es obligatorio");
                        continue;
                    }
                    if (nombre.Length > 50)
                    {
                        resultado.AgregarError(filaNum, "El nombre excede 50 caracteres");
                        continue;
                    }

                    var abreviatura = Obtener("Abreviatura");
                    if (string.IsNullOrWhiteSpace(abreviatura))
                    {
                        resultado.AgregarError(filaNum, "La abreviatura es obligatoria");
                        continue;
                    }
                    if (abreviatura.Length > 10)
                    {
                        abreviatura = abreviatura.Substring(0, 10);
                    }

                    var existente = _context.UnidadesMedida
                        .FirstOrDefault(u => u.Nombre.ToLower() == nombre.ToLower());

                    var activa = ParserHelper.ParsearBool(Obtener("Activa"));

                    if (existente != null)
                    {
                        existente.Nombre = nombre;
                        existente.Abreviatura = abreviatura;
                        existente.Descripcion = Obtener("Descripcion") ?? existente.Descripcion;
                        existente.Activa = activa ?? existente.Activa;
                        resultado.Actualizados++;
                    }
                    else
                    {
                        var unidad = new UnidadMedida
                        {
                            Nombre = nombre,
                            Abreviatura = abreviatura,
                            Descripcion = Obtener("Descripcion"),
                            Activa = activa ?? true,
                            FechaCreacion = DateTime.Now
                        };
                        _context.UnidadesMedida.Add(unidad);
                        resultado.Creados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.AgregarError(filaNum, $"Error inesperado: {ex.Message}");
                }
            }

            _context.SaveChanges();
            return resultado;
        }
    }
}
````

===== FILE: Pages/UnidadesMedida/Index.cshtml =====

````html
@page
@model Kirkenta.Pages.UnidadesMedida.IndexModel
@{
    ViewData["Title"] = "Unidades de medida";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Configuracion/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a configuración
        </a>
        <h2>Unidades de medida</h2>
        <p>Administra las unidades de medida de tus productos</p>
    </div>
    <div class="module-header-right">
        <a asp-page="/UnidadesMedida/Create" class="btn-primary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <line x1="12" y1="5" x2="12" y2="19"/>
                <line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            Nueva unidad
        </a>
        <a asp-page="/UnidadesMedida/Import" class="btn-secondary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                <polyline points="17 8 12 3 7 8"/>
                <line x1="12" y1="3" x2="12" y2="15"/>
            </svg>
            Importar
        </a>
        <a asp-page="/UnidadesMedida/Export" asp-route-formato="excel" class="btn-secondary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                <polyline points="7 10 12 15 17 10"/>
                <line x1="12" y1="15" x2="12" y2="3"/>
            </svg>
            Exportar
        </a>
    </div>
</div>

<div class="module-card">
    @if (TempData["Success"] != null)
    {
        <div class="module-alert module-alert-success">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                <polyline points="22 4 12 14.01 9 11.01"/>
            </svg>
            @TempData["Success"]
        </div>
    }

    @if (TempData["Error"] != null)
    {
        <div class="module-alert module-alert-error">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10"/>
                <line x1="12" y1="8" x2="12" y2="12"/>
                <line x1="12" y1="16" x2="12.01" y2="16"/>
            </svg>
            @TempData["Error"]
        </div>
    }

    @if (Model.Unidades.Count == 0)
    {
        <div class="empty-state">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                <path d="M2 20h20"/>
                <path d="M4 20V10l5 3V10l5 3V6l5 3v11"/>
            </svg>
            <h3>No hay unidades de medida</h3>
            <p>Crea tu primera unidad de medida</p>
        </div>
    }
    else
    {
        <div class="table-wrapper">
            <table class="erp-table">
                <thead>
                    <tr>
                        <th>Nombre</th>
                        <th>Abreviatura</th>
                        <th>Descripción</th>
                        <th>Estado</th>
                        <th style="width: 120px; text-align: right;">Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var u in Model.Unidades)
                    {
                        <tr>
                            <td><strong>@u.Nombre</strong></td>
                            <td>
                                <span class="badge badge-info" style="font-family: monospace;">@u.Abreviatura</span>
                            </td>
                            <td>@(u.Descripcion ?? "—")</td>
                            <td>
                                @if (u.Activa)
                                {
                                    <span class="badge badge-success">Activa</span>
                                }
                                else
                                {
                                    <span class="badge badge-danger">Inactiva</span>
                                }
                            </td>
                            <td>
                                <div class="table-actions">
                                    <a asp-page="/UnidadesMedida/Edit" asp-route-id="@u.Id" class="btn-icon-action" title="Editar">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                                            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                                        </svg>
                                    </a>
                                    <a asp-page="/UnidadesMedida/Delete" asp-route-id="@u.Id" class="btn-icon-action btn-icon-action-danger" title="Eliminar">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                            <polyline points="3 6 5 6 21 6"/>
                                            <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                                        </svg>
                                    </a>
                                </div>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
</div>
````

===== FILE: Pages/UnidadesMedida/Index.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.UnidadesMedida
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<UnidadMedida> Unidades { get; set; } = new();

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Index");
            }

            Unidades = _context.UnidadesMedida.OrderBy(u => u.Nombre).ToList();
            return Page();
        }
    }
}
````

====================================================
 Pages\Usuarios - 22 archivo(s)
====================================================

===== FILE: Pages/Usuarios/Actividad.cshtml =====

````html
@page "{id:int}"
@model Kirkenta.Pages.Usuarios.ActividadModel
@{
    ViewData["Title"] = "Actividad del usuario";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Usuarios/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a usuarios
        </a>
        <h2>Actividad de @Model.Usuario.Username</h2>
        <p>Historial de todas las acciones del usuario</p>
    </div>
</div>

<div class="activity-header-card">
    <div class="activity-user-info">
        <div class="user-avatar-sm" style="background: @Model.RolColor; width: 52px; height: 52px; font-size: 20px;">
            @(string.IsNullOrEmpty(Model.Usuario.Username) ? "?" : Model.Usuario.Username.Substring(0, 1).ToUpper())
        </div>
        <div class="activity-user-meta">
            <h3>@Model.Usuario.Username</h3>
            <p>@Model.Usuario.Email</p>
        </div>
    </div>
    <div class="activity-stats">
        <div class="activity-stat">
            <span class="activity-stat-value">@Model.Actividades.Count</span>
            <span class="activity-stat-label">Acciones registradas</span>
        </div>
    </div>
</div>

<div class="module-card">
    @if (Model.Actividades.Count == 0)
    {
        <div class="empty-state">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
            </svg>
            <h3>Sin actividad registrada</h3>
            <p>Este usuario aún no ha realizado acciones en el sistema</p>
        </div>
    }
    else
    {
        <div class="timeline">
            @foreach (var act in Model.Actividades)
            {
                var iconColor = act.Accion switch
                {
                    var a when a.Contains("Inicio de sesión") => "#10b981",
                    var a when a.Contains("Cierre de sesión") => "#6b7280",
                    var a when a.Contains("Login fallido") => "#ef4444",
                    var a when a.Contains("Crear") => "#3b82f6",
                    var a when a.Contains("Editar") => "#f59e0b",
                    var a when a.Contains("Eliminar") => "#ef4444",
                    var a when a.Contains("Contraseña") => "#8b5cf6",
                    _ => "#6b7280"
                };

                <div class="timeline-item">
                    <div class="timeline-dot" style="background: @iconColor;"></div>
                    <div class="timeline-content">
                        <div class="timeline-header">
                            <strong>@act.Accion</strong>
                            <span class="timeline-date">@act.Fecha.ToString("dd/MM/yyyy HH:mm:ss")</span>
                        </div>
                        @if (!string.IsNullOrEmpty(act.Detalle))
                        {
                            <p class="timeline-detail">@act.Detalle</p>
                        }
                        @if (!string.IsNullOrEmpty(act.Ip) && act.Ip != "N/A")
                        {
                            <span class="timeline-ip">IP: @act.Ip</span>
                        }
                    </div>
                </div>
            }
        </div>
    }
</div>
````

===== FILE: Pages/Usuarios/Actividad.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Kirkenta.Pages.Usuarios
{
    public class ActividadModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ActividadModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Usuario Usuario { get; set; } = new();
        public List<ActividadUsuario> Actividades { get; set; } = new();
        public string RolColor { get; set; } = "#6b7280";

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin")
            {
                var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(currentUserIdStr, out var currentUserId) || currentUserId != id)
                {
                    TempData["Error"] = "No tienes permiso para ver esta actividad";
                    return RedirectToPage("/Index");
                }
            }

            var user = _context.Usuarios.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToPage("/Usuarios/Index");
            }

            Usuario = user;
            RolColor = _context.Roles.FirstOrDefault(r => r.Nombre == user.Rol)?.Color ?? "#6b7280";

            Actividades = _context.Actividades
                .Where(a => a.UsuarioId == id)
                .OrderByDescending(a => a.Fecha)
                .Take(200)
                .ToList();

            return Page();
        }
    }
}
````

===== FILE: Pages/Usuarios/CambiarPassword.cshtml =====

````html
@page
@model Kirkenta.Pages.Usuarios.CambiarPasswordModel
@{
    ViewData["Title"] = "Cambiar contraseña";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Usuarios/Perfil" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver al perfil
        </a>
        <h2>Cambiar contraseña</h2>
        <p>Actualiza tu contraseña de acceso</p>
    </div>
</div>

<div class="module-card form-card" style="max-width: 480px;">
    <form method="post" class="erp-form">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span asp-validation-summary="ModelOnly"></span>
            </div>
        }

        <div class="form-field">
            <label asp-for="Input.CurrentPassword">Contraseña actual *</label>
            <div class="form-input-wrapper">
                <input asp-for="Input.CurrentPassword" id="currentInput" type="password" class="form-input" required />
                <button type="button" class="password-toggle" id="currentToggle" tabindex="-1">
                    <svg class="eye-open" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                        <circle cx="12" cy="12" r="3"/>
                    </svg>
                    <svg class="eye-closed" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none;">
                        <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                        <line x1="1" y1="1" x2="23" y2="23"/>
                    </svg>
                </button>
            </div>
        </div>

        <div class="form-field">
            <label asp-for="Input.NewPassword">Nueva contraseña *</label>
            <div class="form-input-wrapper">
                <input asp-for="Input.NewPassword" id="newInput" type="password" class="form-input" placeholder="Mínimo 6 caracteres" required />
                <button type="button" class="password-toggle" id="newToggle" tabindex="-1">
                    <svg class="eye-open" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                        <circle cx="12" cy="12" r="3"/>
                    </svg>
                    <svg class="eye-closed" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none;">
                        <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                        <line x1="1" y1="1" x2="23" y2="23"/>
                    </svg>
                </button>
            </div>
        </div>

        <div class="form-field">
            <label asp-for="Input.ConfirmPassword">Confirmar nueva contraseña *</label>
            <div class="form-input-wrapper">
                <input asp-for="Input.ConfirmPassword" id="confirmInput" type="password" class="form-input" placeholder="Repite la nueva contraseña" required />
                <button type="button" class="password-toggle" id="confirmToggle" tabindex="-1">
                    <svg class="eye-open" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                        <circle cx="12" cy="12" r="3"/>
                    </svg>
                    <svg class="eye-closed" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none;">
                        <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                        <line x1="1" y1="1" x2="23" y2="23"/>
                    </svg>
                </button>
            </div>
        </div>

        <div class="form-actions">
            <a asp-page="/Usuarios/Perfil" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
                    <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                </svg>
                Cambiar contraseña
            </button>
        </div>
    </form>
</div>

@section Scripts {
    <script>
        (function () {
            function bindToggle(toggleId, inputId) {
                var toggle = document.getElementById(toggleId);
                var input = document.getElementById(inputId);
                if (!toggle || !input) return;
                var eyeOpen = toggle.querySelector('.eye-open');
                var eyeClosed = toggle.querySelector('.eye-closed');
                toggle.addEventListener('click', function () {
                    var isPassword = input.type === 'password';
                    input.type = isPassword ? 'text' : 'password';
                    eyeOpen.style.display = isPassword ? 'none' : 'block';
                    eyeClosed.style.display = isPassword ? 'block' : 'none';
                    input.focus();
                });
            }
            bindToggle('currentToggle', 'currentInput');
            bindToggle('newToggle', 'newInput');
            bindToggle('confirmToggle', 'confirmInput');
        })();
    </script>
}
````

===== FILE: Pages/Usuarios/CambiarPassword.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Kirkenta.Pages.Usuarios
{
    public class CambiarPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CambiarPasswordModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "La contraseña actual es obligatoria")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Confirma la nueva contraseña")]
            [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet() { }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToPage("/Auth/Login");

            var user = _context.Usuarios.FirstOrDefault(u => u.Id == userId);
            if (user == null) return RedirectToPage("/Auth/Login");

            if (!BCrypt.Net.BCrypt.Verify(Input.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("Input.CurrentPassword", "La contraseña actual es incorrecta");
                return Page();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.NewPassword);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                userId,
                "Cambiar contraseña",
                "Cambió su contraseña",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Contraseña cambiada correctamente";
            return RedirectToPage("/Usuarios/Perfil");
        }
    }
}
````

===== FILE: Pages/Usuarios/Create.cshtml =====

````html
@page
@model Kirkenta.Pages.Usuarios.CreateModel
@{
    ViewData["Title"] = "Nuevo usuario";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Usuarios/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a usuarios
        </a>
        <h2>Nuevo usuario</h2>
        <p>Crea un nuevo usuario en el sistema</p>
    </div>
</div>

<div class="module-card form-card">
    <form method="post" class="erp-form">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span asp-validation-summary="ModelOnly"></span>
            </div>
        }

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Username">Usuario *</label>
                <input asp-for="Input.Username" class="form-input" placeholder="juan.perez" required />
            </div>
            <div class="form-field">
                <label asp-for="Input.Email">Email *</label>
                <input asp-for="Input.Email" type="email" class="form-input" placeholder="juan@empresa.com" required />
            </div>
        </div>

        <div class="form-field">
            <label asp-for="Input.NombreCompleto">Nombre completo</label>
            <input asp-for="Input.NombreCompleto" class="form-input" placeholder="Juan Pérez García" />
        </div>

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Telefono">Teléfono</label>
                <input asp-for="Input.Telefono" class="form-input" placeholder="+504 9999-9999" />
            </div>
            <div class="form-field">
                <label asp-for="Input.Rol">Rol *</label>
                <select asp-for="Input.Rol" class="form-input" required>
                    <option value="">— Selecciona un rol —</option>
                    @foreach (var rol in Model.Roles)
                    {
                        <option value="@rol.Nombre">@rol.Nombre</option>
                    }
                </select>
            </div>
        </div>

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Password">Contraseña *</label>
                <div class="form-input-wrapper">
                    <input asp-for="Input.Password" id="passwordInput" type="password" class="form-input" placeholder="Mínimo 6 caracteres" required />
                    <button type="button" class="password-toggle" id="passwordToggle" tabindex="-1">
                        <svg class="eye-open" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                            <circle cx="12" cy="12" r="3"/>
                        </svg>
                        <svg class="eye-closed" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none;">
                            <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                            <line x1="1" y1="1" x2="23" y2="23"/>
                        </svg>
                    </button>
                </div>
            </div>
            <div class="form-field">
                <label asp-for="Input.ConfirmPassword">Confirmar contraseña *</label>
                <div class="form-input-wrapper">
                    <input asp-for="Input.ConfirmPassword" id="confirmInput" type="password" class="form-input" placeholder="Repite la contraseña" required />
                    <button type="button" class="password-toggle" id="confirmToggle" tabindex="-1">
                        <svg class="eye-open" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                            <circle cx="12" cy="12" r="3"/>
                        </svg>
                        <svg class="eye-closed" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none;">
                            <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                            <line x1="1" y1="1" x2="23" y2="23"/>
                        </svg>
                    </button>
                </div>
            </div>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.Activo" type="checkbox" checked />
                <span>Usuario activo (puede iniciar sesión)</span>
            </label>
        </div>

        <div class="form-actions">
            <a asp-page="/Usuarios/Index" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Crear usuario
            </button>
        </div>
    </form>
</div>

@section Scripts {
    <script>
        (function () {
            function bindToggle(toggleId, inputId) {
                var toggle = document.getElementById(toggleId);
                var input = document.getElementById(inputId);
                if (!toggle || !input) return;
                var eyeOpen = toggle.querySelector('.eye-open');
                var eyeClosed = toggle.querySelector('.eye-closed');
                toggle.addEventListener('click', function () {
                    var isPassword = input.type === 'password';
                    input.type = isPassword ? 'text' : 'password';
                    eyeOpen.style.display = isPassword ? 'none' : 'block';
                    eyeClosed.style.display = isPassword ? 'block' : 'none';
                    input.focus();
                });
            }
            bindToggle('passwordToggle', 'passwordInput');
            bindToggle('confirmToggle', 'confirmInput');
        })();
    </script>
}
````

===== FILE: Pages/Usuarios/Create.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Usuarios
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<Rol> Roles { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El usuario es obligatorio")]
            [StringLength(50, MinimumLength = 3)]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "El email es obligatorio")]
            [EmailAddress(ErrorMessage = "Email inválido")]
            public string Email { get; set; } = string.Empty;

            [StringLength(100)]
            public string? NombreCompleto { get; set; }

            [StringLength(20)]
            public string? Telefono { get; set; }

            [Required(ErrorMessage = "El rol es obligatorio")]
            public string Rol { get; set; } = "Pendiente";

            [Required(ErrorMessage = "La contraseña es obligatoria")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Confirma la contraseña")]
            [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
            public string ConfirmPassword { get; set; } = string.Empty;

            public bool Activo { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Create", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear usuarios";
                return RedirectToPage("/Usuarios/Index");
            }

            Roles = _context.Roles.OrderBy(r => r.Nombre).ToList();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Create", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear usuarios";
                return RedirectToPage("/Usuarios/Index");
            }

            Roles = _context.Roles.OrderBy(r => r.Nombre).ToList();

            if (!ModelState.IsValid)
                return Page();

            if (_context.Usuarios.Any(u => u.Username == Input.Username))
            {
                ModelState.AddModelError("Input.Username", "Este usuario ya existe");
                return Page();
            }

            if (_context.Usuarios.Any(u => u.Email == Input.Email))
            {
                ModelState.AddModelError("Input.Email", "Este email ya está registrado");
                return Page();
            }

            var user = new Usuario
            {
                Username = Input.Username,
                Email = Input.Email,
                NombreCompleto = Input.NombreCompleto,
                Telefono = Input.Telefono,
                Rol = Input.Rol,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
                Activo = Input.Activo,
                FechaCreacion = DateTime.Now
            };

            _context.Usuarios.Add(user);
            _context.SaveChanges();

            if (currentUser != null)
            {
                ActividadHelper.Registrar(
                    _context,
                    currentUser.Id,
                    "Crear usuario",
                    $"Creó al usuario '{user.Username}' con rol '{user.Rol}'",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            TempData["Success"] = $"Usuario '{user.Username}' creado correctamente";
            return RedirectToPage("/Usuarios/Index");
        }
    }
}
````

===== FILE: Pages/Usuarios/Delete.cshtml =====

````html
@page "{id:int}"
@model Kirkenta.Pages.Usuarios.DeleteModel
@{
    ViewData["Title"] = "Eliminar usuario";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Usuarios/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a usuarios
        </a>
        <h2>Eliminar usuario</h2>
        <p>Esta acción no se puede deshacer</p>
    </div>
</div>

<div class="module-card danger-card">
    <div class="danger-icon">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
            <line x1="12" y1="9" x2="12" y2="13"/>
            <line x1="12" y1="17" x2="12.01" y2="17"/>
        </svg>
    </div>

    <h3>¿Eliminar al usuario "@Model.Usuario.Username"?</h3>
    <p>Toda la información asociada a este usuario será eliminada permanentemente.</p>

    <div class="user-preview">
        <div class="user-avatar-sm">
            @(string.IsNullOrEmpty(Model.Usuario.Username) ? "?" : Model.Usuario.Username.Substring(0, 1).ToUpper())
        </div>
        <div>
            <strong>@Model.Usuario.Username</strong>
            <span>@Model.Usuario.Email</span>
        </div>
    </div>

    <form method="post" class="form-actions">
        <a asp-page="/Usuarios/Index" class="btn-secondary">Cancelar</a>
        <button type="submit" class="btn-danger">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <polyline points="3 6 5 6 21 6"/>
                <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                <path d="M10 11v6"/>
                <path d="M14 11v6"/>
            </svg>
            Sí, eliminar usuario
        </button>
    </form>
</div>
````

===== FILE: Pages/Usuarios/Delete.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Usuarios
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Usuario Usuario { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Delete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar usuarios";
                return RedirectToPage("/Usuarios/Index");
            }

            var user = _context.Usuarios.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToPage("/Usuarios/Index");
            }

            if (user.Username == User.Identity?.Name)
            {
                TempData["Error"] = "No puedes eliminar tu propio usuario";
                return RedirectToPage("/Usuarios/Index");
            }

            if (user.Rol == "Admin")
            {
                var adminCount = _context.Usuarios.Count(u => u.Rol == "Admin" && u.Activo);
                if (adminCount <= 1)
                {
                    TempData["Error"] = "No puedes eliminar al último administrador del sistema";
                    return RedirectToPage("/Usuarios/Index");
                }
            }

            Usuario = user;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Delete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar usuarios";
                return RedirectToPage("/Usuarios/Index");
            }

            var user = _context.Usuarios.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToPage("/Usuarios/Index");
            }

            if (user.Username == User.Identity?.Name)
            {
                TempData["Error"] = "No puedes eliminar tu propio usuario";
                return RedirectToPage("/Usuarios/Index");
            }

            if (user.Rol == "Admin")
            {
                var adminCount = _context.Usuarios.Count(u => u.Rol == "Admin" && u.Activo);
                if (adminCount <= 1)
                {
                    TempData["Error"] = "No puedes eliminar al último administrador del sistema";
                    return RedirectToPage("/Usuarios/Index");
                }
            }

            var username = user.Username;

            if (currentUser != null)
            {
                ActividadHelper.Registrar(
                    _context,
                    currentUser.Id,
                    "Eliminar usuario",
                    $"Eliminó al usuario '{username}'",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            _context.Usuarios.Remove(user);
            _context.SaveChanges();

            TempData["Success"] = $"Usuario '{username}' eliminado correctamente";
            return RedirectToPage("/Usuarios/Index");
        }
    }
}
````

===== FILE: Pages/Usuarios/Edit.cshtml =====

````html
@page "{id:int}"
@model Kirkenta.Pages.Usuarios.EditModel
@{
    ViewData["Title"] = "Editar usuario";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Usuarios/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a usuarios
        </a>
        <h2>Editar usuario</h2>
        <p>Modifica la información del usuario</p>
    </div>
</div>

<div class="module-card form-card">
    <form method="post" class="erp-form">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span asp-validation-summary="ModelOnly"></span>
            </div>
        }

        <input type="hidden" asp-for="Input.Id" />

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Username">Usuario *</label>
                <input asp-for="Input.Username" class="form-input" required />
            </div>
            <div class="form-field">
                <label asp-for="Input.Email">Email *</label>
                <input asp-for="Input.Email" type="email" class="form-input" required />
            </div>
        </div>

        <div class="form-field">
            <label asp-for="Input.NombreCompleto">Nombre completo</label>
            <input asp-for="Input.NombreCompleto" class="form-input" />
        </div>

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Telefono">Teléfono</label>
                <input asp-for="Input.Telefono" class="form-input" />
            </div>
            <div class="form-field">
                <label asp-for="Input.Rol">Rol *</label>
                <select asp-for="Input.Rol" class="form-input" required>
                    <option value="Pendiente">Pendiente</option>
                    @foreach (var rol in Model.Roles)
                    {
                        <option value="@rol.Nombre">@rol.Nombre</option>
                    }
                </select>
            </div>
        </div>

        <div class="form-field">
            <label class="form-checkbox">
                <input asp-for="Input.Activo" type="checkbox" />
                <span>Usuario activo</span>
            </label>
        </div>

        <div class="form-divider"></div>

        <h3 class="form-section-title">Cambiar contraseña (opcional)</h3>
        <p class="form-section-desc">Déjalo vacío para mantener la contraseña actual</p>

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.NewPassword">Nueva contraseña</label>
                <div class="form-input-wrapper">
                    <input asp-for="Input.NewPassword" id="passwordInput" type="password" class="form-input" placeholder="Nueva contraseña" />
                    <button type="button" class="password-toggle" id="passwordToggle" tabindex="-1">
                        <svg class="eye-open" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                            <circle cx="12" cy="12" r="3"/>
                        </svg>
                        <svg class="eye-closed" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none;">
                            <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                            <line x1="1" y1="1" x2="23" y2="23"/>
                        </svg>
                    </button>
                </div>
            </div>
            <div class="form-field">
                <label asp-for="Input.ConfirmNewPassword">Confirmar contraseña</label>
                <div class="form-input-wrapper">
                    <input asp-for="Input.ConfirmNewPassword" id="confirmInput" type="password" class="form-input" placeholder="Confirma la nueva" />
                    <button type="button" class="password-toggle" id="confirmToggle" tabindex="-1">
                        <svg class="eye-open" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                            <circle cx="12" cy="12" r="3"/>
                        </svg>
                        <svg class="eye-closed" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none;">
                            <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                            <line x1="1" y1="1" x2="23" y2="23"/>
                        </svg>
                    </button>
                </div>
            </div>
        </div>

        <div class="form-actions">
            <a asp-page="/Usuarios/Index" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Guardar cambios
            </button>
        </div>
    </form>
</div>

@section Scripts {
    <script>
        (function () {
            function bindToggle(toggleId, inputId) {
                var toggle = document.getElementById(toggleId);
                var input = document.getElementById(inputId);
                if (!toggle || !input) return;
                var eyeOpen = toggle.querySelector('.eye-open');
                var eyeClosed = toggle.querySelector('.eye-closed');
                toggle.addEventListener('click', function () {
                    var isPassword = input.type === 'password';
                    input.type = isPassword ? 'text' : 'password';
                    eyeOpen.style.display = isPassword ? 'none' : 'block';
                    eyeClosed.style.display = isPassword ? 'block' : 'none';
                    input.focus();
                });
            }
            bindToggle('passwordToggle', 'passwordInput');
            bindToggle('confirmToggle', 'confirmInput');
        })();
    </script>
}
````

===== FILE: Pages/Usuarios/Edit.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Kirkenta.Pages.Usuarios
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<Rol> Roles { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El usuario es obligatorio")]
            [StringLength(50, MinimumLength = 3)]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "El email es obligatorio")]
            [EmailAddress(ErrorMessage = "Email inválido")]
            public string Email { get; set; } = string.Empty;

            [StringLength(100)]
            public string? NombreCompleto { get; set; }

            [StringLength(20)]
            public string? Telefono { get; set; }

            [Required(ErrorMessage = "El rol es obligatorio")]
            public string Rol { get; set; } = "Pendiente";

            public bool Activo { get; set; } = true;

            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
            public string? NewPassword { get; set; }

            [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden")]
            public string? ConfirmNewPassword { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Edit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar usuarios";
                return RedirectToPage("/Usuarios/Index");
            }

            Roles = _context.Roles.OrderBy(r => r.Nombre).ToList();

            var user = _context.Usuarios.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToPage("/Usuarios/Index");
            }

            Input = new InputModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                NombreCompleto = user.NombreCompleto,
                Telefono = user.Telefono,
                Rol = user.Rol,
                Activo = user.Activo
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Edit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar usuarios";
                return RedirectToPage("/Usuarios/Index");
            }

            Roles = _context.Roles.OrderBy(r => r.Nombre).ToList();

            if (!ModelState.IsValid)
                return Page();

            var user = _context.Usuarios.FirstOrDefault(u => u.Id == Input.Id);
            if (user == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToPage("/Usuarios/Index");
            }

            if (_context.Usuarios.Any(u => u.Username == Input.Username && u.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Username", "Este usuario ya existe");
                return Page();
            }

            if (_context.Usuarios.Any(u => u.Email == Input.Email && u.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Email", "Este email ya está registrado");
                return Page();
            }

            if (user.Rol == "Admin" && Input.Rol != "Admin")
            {
                var adminCount = _context.Usuarios.Count(u => u.Rol == "Admin" && u.Activo);
                if (adminCount <= 1)
                {
                    ModelState.AddModelError(string.Empty, "No puedes quitar el rol Admin al último administrador del sistema");
                    return Page();
                }
            }

            user.Username = Input.Username;
            user.Email = Input.Email;
            user.NombreCompleto = Input.NombreCompleto;
            user.Telefono = Input.Telefono;
            user.Rol = Input.Rol;
            user.Activo = Input.Activo;

            if (!string.IsNullOrWhiteSpace(Input.NewPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.NewPassword);
            }

            try
            {
                _context.SaveChanges();

                if (currentUser != null)
                {
                    var detalle = $"Editó al usuario '{user.Username}'";
                    if (!string.IsNullOrWhiteSpace(Input.NewPassword))
                        detalle += " (incluyendo contraseña)";

                    ActividadHelper.Registrar(
                        _context,
                        currentUser.Id,
                        "Editar usuario",
                        detalle,
                        HttpContext.Connection.RemoteIpAddress?.ToString());
                }

                TempData["Success"] = $"Usuario '{user.Username}' actualizado correctamente";
                return RedirectToPage("/Usuarios/Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al guardar: {ex.Message}");
                return Page();
            }
        }
    }
}
````

===== FILE: Pages/Usuarios/Index.cshtml =====

````html
@page
@model Kirkenta.Pages.Usuarios.IndexModel
@{
    ViewData["Title"] = "Usuarios";
}

<div class="module-header">
    <div class="module-header-left">
        <h2>Usuarios</h2>
        <p>Administra los usuarios del sistema</p>
    </div>
    <div class="module-header-right">
        @if (Model.PuedeCrear)
        {
            <a asp-page="/Usuarios/Create" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <line x1="12" y1="5" x2="12" y2="19"/>
                    <line x1="5" y1="12" x2="19" y2="12"/>
                </svg>
                Nuevo usuario
            </a>
        }
        @if (Model.PuedeGestionarRoles)
        {
            <a asp-page="/Usuarios/Roles" class="btn-secondary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
                </svg>
                Roles
            </a>
        }
    </div>
</div>

<div class="module-card">
    <div class="module-toolbar">
        <div class="search-box">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="11" cy="11" r="8"/>
                <line x1="21" y1="21" x2="16.65" y2="16.65"/>
            </svg>
            <input type="text" id="searchInput" placeholder="Buscar por usuario, email o nombre..." />
        </div>
        <div class="filter-group">
            <select id="filterRol" class="filter-select">
                <option value="">Todos los roles</option>
                @foreach (var rol in Model.Roles)
                {
                    <option value="@rol.Nombre">@rol.Nombre</option>
                }
            </select>
            <select id="filterEstado" class="filter-select">
                <option value="">Todos los estados</option>
                <option value="activo">Activos</option>
                <option value="inactivo">Inactivos</option>
            </select>
        </div>
    </div>

    @if (TempData["Success"] != null)
    {
        <div class="module-alert module-alert-success">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                <polyline points="22 4 12 14.01 9 11.01"/>
            </svg>
            @TempData["Success"]
        </div>
    }

    @if (TempData["Error"] != null)
    {
        <div class="module-alert module-alert-error">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10"/>
                <line x1="12" y1="8" x2="12" y2="12"/>
                <line x1="12" y1="16" x2="12.01" y2="16"/>
            </svg>
            @TempData["Error"]
        </div>
    }

    @if (Model.Usuarios.Count == 0)
    {
        <div class="empty-state">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                <circle cx="9" cy="7" r="4"/>
                <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
                <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
            </svg>
            <h3>No hay usuarios registrados</h3>
            <p>Crea el primer usuario para empezar</p>
        </div>
    }
    else
    {
        <div class="table-wrapper">
            <table class="erp-table" id="usuariosTable">
                <thead>
                    <tr>
                        <th style="width: 50px;"></th>
                        <th>Usuario</th>
                        <th>Email</th>
                        <th>Rol</th>
                        <th>Estado</th>
                        <th>Último acceso</th>
                        <th style="width: 140px; text-align: right;">Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var u in Model.Usuarios)
                    {
                        var rolColor = Model.Roles.FirstOrDefault(r => r.Nombre == u.Rol)?.Color ?? "#6b7280";
                        <tr data-username="@u.Username.ToLower()"
                            data-email="@u.Email.ToLower()"
                            data-nombre="@(u.NombreCompleto?.ToLower() ?? "")"
                            data-rol="@u.Rol"
                            data-estado="@(u.Activo ? "activo" : "inactivo")">
                            <td>
                                <div class="user-avatar-sm" style="background: @rolColor;">
                                    @(string.IsNullOrEmpty(u.Username) ? "?" : u.Username.Substring(0, 1).ToUpper())
                                </div>
                            </td>
                            <td>
                                <div class="table-user-cell">
                                    <span class="table-user-name">@u.Username</span>
                                    @if (!string.IsNullOrEmpty(u.NombreCompleto))
                                    {
                                        <span class="table-user-sub">@u.NombreCompleto</span>
                                    }
                                </div>
                            </td>
                            <td>@u.Email</td>
                            <td>
                                <span class="badge" style="background: @(rolColor)20; color: @rolColor;">
                                    @u.Rol
                                </span>
                            </td>
                            <td>
                                @if (u.Activo)
                                {
                                    <span class="badge badge-success">Activo</span>
                                }
                                else
                                {
                                    <span class="badge badge-danger">Inactivo</span>
                                }
                            </td>
                            <td>
                                @(u.UltimoAcceso.HasValue ? u.UltimoAcceso.Value.ToString("dd/MM/yyyy HH:mm") : "Nunca")
                            </td>
                            <td>
                                <div class="table-actions">
                                    @if (Model.PuedeEditar)
                                    {
                                        <a asp-page="/Usuarios/Edit" asp-route-id="@u.Id" class="btn-icon-action" title="Editar">
                                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                                                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                                            </svg>
                                        </a>
                                    }
                                    <a asp-page="/Usuarios/Actividad" asp-route-id="@u.Id" class="btn-icon-action" title="Actividad">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                            <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
                                        </svg>
                                    </a>
                                    @if (Model.PuedeEliminar && u.Username != User.Identity?.Name)
                                    {
                                        <a asp-page="/Usuarios/Delete" asp-route-id="@u.Id" class="btn-icon-action btn-icon-action-danger" title="Eliminar">
                                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                                <polyline points="3 6 5 6 21 6"/>
                                                <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                                                <path d="M10 11v6"/>
                                                <path d="M14 11v6"/>
                                            </svg>
                                        </a>
                                    }
                                </div>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
</div>

@section Scripts {
    <script>
        (function () {
            var searchInput = document.getElementById('searchInput');
            var filterRol = document.getElementById('filterRol');
            var filterEstado = document.getElementById('filterEstado');
            var table = document.getElementById('usuariosTable');
            if (!table) return;
            var rows = table.querySelectorAll('tbody tr');

            function filterRows() {
                var search = (searchInput?.value || '').toLowerCase().trim();
                var rol = filterRol?.value || '';
                var estado = filterEstado?.value || '';

                rows.forEach(function (row) {
                    var username = row.dataset.username || '';
                    var email = row.dataset.email || '';
                    var nombre = row.dataset.nombre || '';
                    var rowRol = row.dataset.rol || '';
                    var rowEstado = row.dataset.estado || '';

                    var matchSearch = !search || username.includes(search) || email.includes(search) || nombre.includes(search);
                    var matchRol = !rol || rowRol === rol;
                    var matchEstado = !estado || rowEstado === estado;

                    row.style.display = (matchSearch && matchRol && matchEstado) ? '' : 'none';
                });
            }

            searchInput?.addEventListener('input', filterRows);
            filterRol?.addEventListener('change', filterRows);
            filterEstado?.addEventListener('change', filterRows);
        })();
    </script>
}
````

===== FILE: Pages/Usuarios/Index.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Usuarios
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Usuario> Usuarios { get; set; } = new();
        public List<Rol> Roles { get; set; } = new();

        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }
        public bool PuedeGestionarRoles { get; set; }

        public void OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            // Verificar permisos
            PuedeCrear = PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Create", "crear");
            PuedeEditar = PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Edit", "editar");
            PuedeEliminar = PermisoHelper.TienePermiso(_context, currentRol, "Usuarios", "Delete", "eliminar");
            PuedeGestionarRoles = currentRol == "Admin";

            Usuarios = _context.Usuarios.OrderBy(u => u.Username).ToList();
            Roles = _context.Roles.OrderBy(r => r.Nombre).ToList();
        }
    }
}
````

===== FILE: Pages/Usuarios/Perfil.cshtml =====

````html
@page
@model Kirkenta.Pages.Usuarios.PerfilModel
@{
    ViewData["Title"] = "Mi perfil";
}

<div class="module-header">
    <div class="module-header-left">
        <h2>Mi perfil</h2>
        <p>Administra tu información personal</p>
    </div>
</div>

@if (TempData["Success"] != null)
{
    <div class="module-alert module-alert-success">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
            <polyline points="22 4 12 14.01 9 11.01"/>
        </svg>
        @TempData["Success"]
    </div>
}

<div class="profile-layout">
    <div class="module-card profile-sidebar">
        <div class="profile-avatar-big">
            @(string.IsNullOrEmpty(Model.Usuario.Username) ? "?" : Model.Usuario.Username.Substring(0, 1).ToUpper())
        </div>
        <h3>@Model.Usuario.Username</h3>
        <p class="profile-email">@Model.Usuario.Email</p>
        <span class="badge" style="background: @Model.RolColor; color: #fff;">@Model.Usuario.Rol</span>

        <div class="profile-stats">
            <div class="profile-stat">
                <span class="profile-stat-label">Miembro desde</span>
                <span class="profile-stat-value">@Model.Usuario.FechaCreacion.ToString("dd/MM/yyyy")</span>
            </div>
            <div class="profile-stat">
                <span class="profile-stat-label">Último acceso</span>
                <span class="profile-stat-value">@(Model.Usuario.UltimoAcceso?.ToString("dd/MM/yyyy HH:mm") ?? "Nunca")</span>
            </div>
        </div>
    </div>

    <div class="module-card form-card">
        <form method="post" class="erp-form">
            @if (!ViewData.ModelState.IsValid)
            {
                <div class="module-alert module-alert-error">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <circle cx="12" cy="12" r="10"/>
                        <line x1="12" y1="8" x2="12" y2="12"/>
                        <line x1="12" y1="16" x2="12.01" y2="16"/>
                    </svg>
                    <span asp-validation-summary="ModelOnly"></span>
                </div>
            }

            <h3 class="form-section-title">Información personal</h3>

            <div class="form-row">
                <div class="form-field">
                    <label asp-for="Input.Username">Usuario</label>
                    <input asp-for="Input.Username" class="form-input" readonly />
                </div>
                <div class="form-field">
                    <label asp-for="Input.Email">Email *</label>
                    <input asp-for="Input.Email" type="email" class="form-input" required />
                </div>
            </div>

            <div class="form-row">
                <div class="form-field">
                    <label asp-for="Input.NombreCompleto">Nombre completo</label>
                    <input asp-for="Input.NombreCompleto" class="form-input" />
                </div>
                <div class="form-field">
                    <label asp-for="Input.Telefono">Teléfono</label>
                    <input asp-for="Input.Telefono" class="form-input" />
                </div>
            </div>

            <div class="form-actions">
                <a asp-page="/Usuarios/CambiarPassword" class="btn-secondary">Cambiar contraseña</a>
                <button type="submit" class="btn-primary">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                        <polyline points="20 6 9 17 4 12"/>
                    </svg>
                    Guardar cambios
                </button>
            </div>
        </form>
    </div>
</div>
````

===== FILE: Pages/Usuarios/Perfil.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Kirkenta.Pages.Usuarios
{
    public class PerfilModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PerfilModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Usuario Usuario { get; set; } = new();
        public string RolColor { get; set; } = "#6b7280";

        public class InputModel
        {
            [Required]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "El email es obligatorio")]
            [EmailAddress(ErrorMessage = "Email inválido")]
            public string Email { get; set; } = string.Empty;

            [StringLength(100)]
            public string? NombreCompleto { get; set; }

            [StringLength(20)]
            public string? Telefono { get; set; }
        }

        private Usuario? GetCurrentUser()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId)) return null;
            return _context.Usuarios.FirstOrDefault(u => u.Id == userId);
        }

        public IActionResult OnGet()
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToPage("/Auth/Login");

            Usuario = user;
            RolColor = _context.Roles.FirstOrDefault(r => r.Nombre == user.Rol)?.Color ?? "#6b7280";

            Input = new InputModel
            {
                Username = user.Username,
                Email = user.Email,
                NombreCompleto = user.NombreCompleto,
                Telefono = user.Telefono
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToPage("/Auth/Login");

            Usuario = user;
            RolColor = _context.Roles.FirstOrDefault(r => r.Nombre == user.Rol)?.Color ?? "#6b7280";

            if (!ModelState.IsValid) return Page();

            if (_context.Usuarios.Any(u => u.Email == Input.Email && u.Id != user.Id))
            {
                ModelState.AddModelError("Input.Email", "Este email ya está en uso");
                return Page();
            }

            user.Email = Input.Email;
            user.NombreCompleto = Input.NombreCompleto;
            user.Telefono = Input.Telefono;
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                user.Id,
                "Editar perfil",
                "Actualizó su información personal",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Perfil actualizado correctamente";
            return RedirectToPage("/Usuarios/Perfil");
        }
    }
}
````

===== FILE: Pages/Usuarios/Roles.cshtml =====

````html
@page
@model Kirkenta.Pages.Usuarios.RolesModel
@{
    ViewData["Title"] = "Roles y permisos";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Usuarios/Index" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a usuarios
        </a>
        <h2>Roles y permisos</h2>
        <p>Administra los roles y sus permisos en el sistema</p>
    </div>
    <div class="module-header-right">
        <a asp-page="/Usuarios/RolesCreate" class="btn-primary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <line x1="12" y1="5" x2="12" y2="19"/>
                <line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            Nuevo rol
        </a>
    </div>
</div>

@if (TempData["Success"] != null)
{
    <div class="module-alert module-alert-success">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
            <polyline points="22 4 12 14.01 9 11.01"/>
        </svg>
        @TempData["Success"]
    </div>
}

@if (TempData["Error"] != null)
{
    <div class="module-alert module-alert-error">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"/>
            <line x1="12" y1="8" x2="12" y2="12"/>
            <line x1="12" y1="16" x2="12.01" y2="16"/>
        </svg>
        @TempData["Error"]
    </div>
}

<div class="roles-grid">
    @foreach (var rol in Model.Roles)
    {
        var permisosCount = Model.ConteoPermisos.GetValueOrDefault(rol.Id, 0);
        var usuariosCount = Model.ConteoUsuarios.GetValueOrDefault(rol.Nombre, 0);

        <div class="role-card">
            <div class="role-card-header">
                <div class="role-color" style="background: @rol.Color;"></div>
                <div class="role-info">
                    <h3>@rol.Nombre</h3>
                    @if (rol.EsSistema)
                    {
                        <span class="badge badge-info">Sistema</span>
                    }
                </div>
            </div>

            <p class="role-desc">@(rol.Descripcion ?? "Sin descripción")</p>

            <div class="role-stats">
                <span>
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                        <circle cx="9" cy="7" r="4"/>
                    </svg>
                    @usuariosCount usuarios
                </span>
                <span>
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
                        <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                    </svg>
                    @permisosCount permisos
                </span>
            </div>

            <div class="role-actions">
                <a asp-page="/Usuarios/RolesEdit" asp-route-id="@rol.Id" class="btn-sm btn-secondary">
                    Editar permisos
                </a>
                @if (!rol.EsSistema && usuariosCount == 0)
                {
                    <a asp-page="/Usuarios/RolesDelete" asp-route-id="@rol.Id" class="btn-sm btn-danger">Eliminar</a>
                }
            </div>
        </div>
    }
</div>
````

===== FILE: Pages/Usuarios/Roles.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Kirkenta.Pages.Usuarios
{
    public class RolesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RolesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Rol> Roles { get; set; } = new();
        public Dictionary<string, int> ConteoUsuarios { get; set; } = new();
        public Dictionary<int, int> ConteoPermisos { get; set; } = new();

        public IActionResult OnGet()
        {
            // 🔒 Solo Admin puede ver esta página
            var currentUsername = User.Identity?.Name;
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == currentUsername);

            if (currentUser == null || currentUser.Rol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            Roles = _context.Roles.OrderBy(r => r.Nombre).ToList();

            ConteoUsuarios = _context.Usuarios
                .GroupBy(u => u.Rol)
                .ToDictionary(g => g.Key, g => g.Count());

            ConteoPermisos = _context.Permisos
                .Where(p => p.PuedeVer)
                .GroupBy(p => p.RolId)
                .ToDictionary(g => g.Key, g => g.Count());

            return Page();
        }
    }
}
````

===== FILE: Pages/Usuarios/RolesCreate.cshtml =====

````html
@page
@model Kirkenta.Pages.Usuarios.RolesCreateModel
@{
    ViewData["Title"] = "Nuevo rol";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Usuarios/Roles" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a roles
        </a>
        <h2>Nuevo rol</h2>
        <p>Crea un nuevo rol y define sus permisos</p>
    </div>
</div>

<div class="module-card form-card-wide">
    <form method="post" class="erp-form">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <div asp-validation-summary="All"></div>
            </div>
        }

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Nombre">Nombre del rol *</label>
                <input asp-for="Input.Nombre" class="form-input" placeholder="Ej: Supervisor" required />
                <span asp-validation-for="Input.Nombre" class="form-help" style="color: var(--color-danger);"></span>
            </div>
            <div class="form-field">
                <label asp-for="Input.Color">Color del rol</label>
                <div class="color-picker-wrapper">
                    <input asp-for="Input.Color" type="color" class="color-picker" id="colorPicker" />
                    <span class="color-preview" id="colorPreview" style="background: #4f46e5; color: #fff; padding: 4px 10px; border-radius: 6px; font-size: 12px; font-weight: 600;">#4f46e5</span>
                </div>
            </div>
        </div>

        <div class="form-field">
            <label asp-for="Input.Descripcion">Descripción</label>
            <textarea asp-for="Input.Descripcion" class="form-input" rows="2" placeholder="Describe qué puede hacer este rol..."></textarea>
        </div>

        <div class="form-divider"></div>

        <div class="permisos-header">
            <h3 class="form-section-title">Permisos del rol</h3>
            <p class="form-section-desc">
                Marca el módulo para dar acceso completo, o selecciona submódulos específicos.
            </p>
        </div>

        <div class="permisos-toolbar">
            <button type="button" class="btn-sm btn-secondary" id="btnSelectAll">Seleccionar todo</button>
            <button type="button" class="btn-sm btn-secondary" id="btnClearAll">Limpiar todo</button>
            <span class="permisos-counter">
                <strong id="totalSeleccionados">0</strong> permisos seleccionados
            </span>
        </div>

        <div class="permisos-table-wrapper">
            <table class="permisos-table">
                <thead>
                    <tr>
                        <th style="width: 40px;">
                            <input type="checkbox" id="checkAllModulos" title="Seleccionar todos los módulos" />
                        </th>
                        <th>Módulo / Submódulo</th>
                        <th style="width: 70px; text-align: center;">Ver</th>
                        <th style="width: 70px; text-align: center;">Crear</th>
                        <th style="width: 70px; text-align: center;">Editar</th>
                        <th style="width: 70px; text-align: center;">Eliminar</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var modulo in Model.Modulos)
                    {
                        var moduloKey = modulo.Key;
                        var submodulos = modulo.Value;

                        <!-- Módulo padre -->
                        <tr class="permiso-row-modulo">
                            <td>
                                <input type="checkbox"
                                       class="check-modulo"
                                       data-modulo="@moduloKey" />
                            </td>
                            <td>
                                <strong>@Kirkenta.Helpers.ModulosERP.NombreBonito(moduloKey)</strong>
                            </td>
                            <td style="text-align: center;">
                                <input type="checkbox"
                                       name="Input.Permisos[@moduloKey].PuedeVer"
                                       value="true"
                                       class="permiso-ver permiso-modulo-ver"
                                       data-modulo="@moduloKey"
                                       data-tipo="ver" />
                            </td>
                            <td style="text-align: center;">
                                <input type="checkbox"
                                       name="Input.Permisos[@moduloKey].PuedeCrear"
                                       value="true"
                                       class="permiso-crear permiso-modulo-crear"
                                       data-modulo="@moduloKey"
                                       data-tipo="crear" />
                            </td>
                            <td style="text-align: center;">
                                <input type="checkbox"
                                       name="Input.Permisos[@moduloKey].PuedeEditar"
                                       value="true"
                                       class="permiso-editar permiso-modulo-editar"
                                       data-modulo="@moduloKey"
                                       data-tipo="editar" />
                            </td>
                            <td style="text-align: center;">
                                <input type="checkbox"
                                       name="Input.Permisos[@moduloKey].PuedeEliminar"
                                       value="true"
                                       class="permiso-eliminar permiso-modulo-eliminar"
                                       data-modulo="@moduloKey"
                                       data-tipo="eliminar" />
                            </td>
                        </tr>

                        <!-- Submódulos -->
                        @foreach (var sub in submodulos)
                        {
                            <tr class="permiso-row-submodulo">
                                <td></td>
                                <td>
                                    <span class="submodulo-label">└ @Kirkenta.Helpers.ModulosERP.NombreBonitoSub(sub)</span>
                                </td>
                                <td style="text-align: center;">
                                    <input type="checkbox"
                                           name="Input.Permisos[@moduloKey].Submodulos[@sub].PuedeVer"
                                           value="true"
                                           class="permiso-ver permiso-sub-ver"
                                           data-modulo="@moduloKey"
                                           data-submodulo="@sub"
                                           data-tipo="ver" />
                                </td>
                                <td style="text-align: center;">
                                    <input type="checkbox"
                                           name="Input.Permisos[@moduloKey].Submodulos[@sub].PuedeCrear"
                                           value="true"
                                           class="permiso-crear permiso-sub-crear"
                                           data-modulo="@moduloKey"
                                           data-submodulo="@sub"
                                           data-tipo="crear" />
                                </td>
                                <td style="text-align: center;">
                                    <input type="checkbox"
                                           name="Input.Permisos[@moduloKey].Submodulos[@sub].PuedeEditar"
                                           value="true"
                                           class="permiso-editar permiso-sub-editar"
                                           data-modulo="@moduloKey"
                                           data-submodulo="@sub"
                                           data-tipo="editar" />
                                </td>
                                <td style="text-align: center;">
                                    <input type="checkbox"
                                           name="Input.Permisos[@moduloKey].Submodulos[@sub].PuedeEliminar"
                                           value="true"
                                           class="permiso-eliminar permiso-sub-eliminar"
                                           data-modulo="@moduloKey"
                                           data-submodulo="@sub"
                                           data-tipo="eliminar" />
                                </td>
                            </tr>
                        }
                    }
                </tbody>
            </table>
        </div>

        <div class="form-actions">
            <a asp-page="/Usuarios/Roles" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Crear rol
            </button>
        </div>
    </form>
</div>

@section Scripts {
    <script>
        (function () {
            // === Color picker ===
            var colorPicker = document.getElementById('colorPicker');
            var colorPreview = document.getElementById('colorPreview');
            if (colorPicker && colorPreview) {
                colorPreview.style.background = colorPicker.value;
                colorPicker.addEventListener('input', function () {
                    colorPreview.textContent = colorPicker.value;
                    colorPreview.style.background = colorPicker.value;
                });
            }

            // === Contador ===
            function updateCounter() {
                var total = document.querySelectorAll('.permisos-table input[type="checkbox"][name]:checked').length;
                var counter = document.getElementById('totalSeleccionados');
                if (counter) counter.textContent = total;
            }

            // === Check del módulo padre ===
            // Marca TODOS los permisos (ver/crear/editar/eliminar) del módulo Y de sus submódulos
            document.querySelectorAll('.check-modulo').forEach(function (checkPadre) {
                checkPadre.addEventListener('change', function () {
                    var modulo = this.getAttribute('data-modulo');
                    var estado = this.checked;

                    // Todos los permisos del módulo padre
                    document.querySelectorAll(
                        '.permiso-modulo-ver[data-modulo="' + modulo + '"],' +
                        '.permiso-modulo-crear[data-modulo="' + modulo + '"],' +
                        '.permiso-modulo-editar[data-modulo="' + modulo + '"],' +
                        '.permiso-modulo-eliminar[data-modulo="' + modulo + '"]'
                    ).forEach(function (cb) {
                        cb.checked = estado;
                    });

                    // Todos los permisos de los submódulos
                    document.querySelectorAll(
                        '.permiso-sub-ver[data-modulo="' + modulo + '"],' +
                        '.permiso-sub-crear[data-modulo="' + modulo + '"],' +
                        '.permiso-sub-editar[data-modulo="' + modulo + '"],' +
                        '.permiso-sub-eliminar[data-modulo="' + modulo + '"]'
                    ).forEach(function (cb) {
                        cb.checked = estado;
                    });

                    updateCounter();
                });
            });

            // === Click en "Ver" del módulo padre → marca "Ver" de todos sus submódulos ===
            document.querySelectorAll('.permiso-modulo-ver').forEach(function (checkVer) {
                checkVer.addEventListener('change', function () {
                    var modulo = this.getAttribute('data-modulo');
                    var estado = this.checked;

                    document.querySelectorAll('.permiso-sub-ver[data-modulo="' + modulo + '"]').forEach(function (cb) {
                        cb.checked = estado;
                    });

                    updateCounter();
                });
            });

            // === Cualquier checkbox del módulo padre → sincronizar el check-modulo si todos están marcados ===
            document.querySelectorAll('.permiso-modulo-ver, .permiso-modulo-crear, .permiso-modulo-editar, .permiso-modulo-eliminar').forEach(function (cb) {
                cb.addEventListener('change', function () {
                    var modulo = this.getAttribute('data-modulo');
                    var todosPadre = document.querySelectorAll(
                        '.permiso-modulo-ver[data-modulo="' + modulo + '"],' +
                        '.permiso-modulo-crear[data-modulo="' + modulo + '"],' +
                        '.permiso-modulo-editar[data-modulo="' + modulo + '"],' +
                        '.permiso-modulo-eliminar[data-modulo="' + modulo + '"]'
                    );
                    var todosMarcados = Array.from(todosPadre).every(function (c) { return c.checked; });

                    var checkPadre = document.querySelector('.check-modulo[data-modulo="' + modulo + '"]');
                    if (checkPadre) checkPadre.checked = todosMarcados;

                    updateCounter();
                });
            });

            // === Todos los checkboxes actualizan el contador ===
            document.querySelectorAll('.permisos-table input[type="checkbox"][name]').forEach(function (cb) {
                cb.addEventListener('change', updateCounter);
            });

            // === Botón "Seleccionar todo" ===
            document.getElementById('btnSelectAll').addEventListener('click', function () {
                document.querySelectorAll('.permisos-table input[type="checkbox"]').forEach(function (cb) {
                    if (cb.id !== 'checkAllModulos') cb.checked = true;
                });
                updateCounter();
            });

            // === Botón "Limpiar todo" ===
            document.getElementById('btnClearAll').addEventListener('click', function () {
                document.querySelectorAll('.permisos-table input[type="checkbox"]').forEach(function (cb) {
                    if (cb.id !== 'checkAllModulos') cb.checked = false;
                });
                updateCounter();
            });

            // === Checkbox "Seleccionar todos los módulos" del header ===
            document.getElementById('checkAllModulos').addEventListener('change', function () {
                var estado = this.checked;
                document.querySelectorAll('.permisos-table input[type="checkbox"]').forEach(function (cb) {
                    if (cb.id !== 'checkAllModulos') cb.checked = estado;
                });
                updateCounter();
            });

            // Inicializar
            updateCounter();
        })();
    </script>
}
````

===== FILE: Pages/Usuarios/RolesCreate.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Usuarios
{
    public class RolesCreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RolesCreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Dictionary<string, List<string>> Modulos { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(50)]
            public string Nombre { get; set; } = string.Empty;

            [StringLength(255)]
            public string? Descripcion { get; set; }

            public string Color { get; set; } = "#4f46e5";

            public Dictionary<string, PermisoModuloInput> Permisos { get; set; } = new();
        }

        public class PermisoModuloInput
        {
            public bool PuedeVer { get; set; }
            public bool PuedeCrear { get; set; }
            public bool PuedeEditar { get; set; }
            public bool PuedeEliminar { get; set; }
            public Dictionary<string, PermisoModuloInput> Submodulos { get; set; } = new();
        }

        public IActionResult OnGet()
        {
            // 🔒 Solo Admin
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            Modulos = ModulosERP.Modulos;
            return Page();
        }

        public IActionResult OnPost()
        {
            // 🔒 Solo Admin
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            Modulos = ModulosERP.Modulos;

            if (!ModelState.IsValid)
                return Page();

            if (_context.Roles.Any(r => r.Nombre.ToLower() == Input.Nombre.ToLower()))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe un rol con este nombre");
                return Page();
            }

            // Crear el rol
            var rol = new Rol
            {
                Nombre = Input.Nombre,
                Descripcion = Input.Descripcion,
                Color = Input.Color,
                EsSistema = false,
                FechaCreacion = DateTime.Now
            };

            _context.Roles.Add(rol);
            _context.SaveChanges();

            // Guardar permisos
            foreach (var moduloKvp in Input.Permisos)
            {
                var modulo = moduloKvp.Key;
                var permisoModulo = moduloKvp.Value;

                // Permiso del módulo padre (Submodulo = NULL)
                var permiso = new Permiso
                {
                    RolId = rol.Id,
                    Modulo = modulo,
                    Submodulo = null,
                    PuedeVer = permisoModulo.PuedeVer,
                    PuedeCrear = permisoModulo.PuedeCrear,
                    PuedeEditar = permisoModulo.PuedeEditar,
                    PuedeEliminar = permisoModulo.PuedeEliminar
                };
                _context.Permisos.Add(permiso);

                // Permisos de submódulos
                foreach (var subKvp in permisoModulo.Submodulos)
                {
                    var submodulo = subKvp.Key;
                    var permisoSub = subKvp.Value;

                    var permisoSubmodulo = new Permiso
                    {
                        RolId = rol.Id,
                        Modulo = modulo,
                        Submodulo = submodulo,
                        PuedeVer = permisoSub.PuedeVer,
                        PuedeCrear = permisoSub.PuedeCrear,
                        PuedeEditar = permisoSub.PuedeEditar,
                        PuedeEliminar = permisoSub.PuedeEliminar
                    };
                    _context.Permisos.Add(permisoSubmodulo);
                }
            }

            _context.SaveChanges();

            TempData["Success"] = $"Rol '{rol.Nombre}' creado correctamente";
            return RedirectToPage("/Usuarios/Roles");
        }
    }
}
````

===== FILE: Pages/Usuarios/RolesDelete.cshtml =====

````html
@page "{id:int}"
@model Kirkenta.Pages.Usuarios.RolesDeleteModel
@{
    ViewData["Title"] = "Eliminar rol";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Usuarios/Roles" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a roles
        </a>
        <h2>Eliminar rol</h2>
        <p>Esta acción no se puede deshacer</p>
    </div>
</div>

<div class="module-card danger-card">
    <div class="danger-icon">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
            <line x1="12" y1="9" x2="12" y2="13"/>
            <line x1="12" y1="17" x2="12.01" y2="17"/>
        </svg>
    </div>

    <h3>¿Eliminar el rol "@Model.Rol.Nombre"?</h3>
    <p>El rol y todos sus permisos serán eliminados permanentemente.</p>

    <div class="user-preview">
        <div class="role-color" style="background: @Model.Rol.Color; width: 40px; height: 40px; border-radius: 10px;"></div>
        <div>
            <strong>@Model.Rol.Nombre</strong>
            <span>@Model.Rol.Descripcion</span>
        </div>
    </div>

    <form method="post" class="form-actions">
        <a asp-page="/Usuarios/Roles" class="btn-secondary">Cancelar</a>
        <button type="submit" class="btn-danger">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                <polyline points="3 6 5 6 21 6"/>
                <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
            </svg>
            Sí, eliminar rol
        </button>
    </form>
</div>
````

===== FILE: Pages/Usuarios/RolesDelete.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Usuarios
{
    public class RolesDeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RolesDeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Rol Rol { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            // 🔒 Solo Admin
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var rol = _context.Roles.FirstOrDefault(r => r.Id == id);
            if (rol == null)
            {
                TempData["Error"] = "Rol no encontrado";
                return RedirectToPage("/Usuarios/Roles");
            }

            if (rol.EsSistema)
            {
                TempData["Error"] = "No se pueden eliminar roles del sistema";
                return RedirectToPage("/Usuarios/Roles");
            }

            if (_context.Usuarios.Any(u => u.Rol == rol.Nombre))
            {
                TempData["Error"] = "No se puede eliminar un rol que tiene usuarios asignados";
                return RedirectToPage("/Usuarios/Roles");
            }

            Rol = rol;
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var rol = _context.Roles.FirstOrDefault(r => r.Id == id);
            if (rol == null)
            {
                TempData["Error"] = "Rol no encontrado";
                return RedirectToPage("/Usuarios/Roles");
            }

            if (rol.EsSistema)
            {
                TempData["Error"] = "No se pueden eliminar roles del sistema";
                return RedirectToPage("/Usuarios/Roles");
            }

            if (_context.Usuarios.Any(u => u.Rol == rol.Nombre))
            {
                TempData["Error"] = "No se puede eliminar un rol que tiene usuarios asignados";
                return RedirectToPage("/Usuarios/Roles");
            }

            var nombre = rol.Nombre;
            _context.Roles.Remove(rol);
            _context.SaveChanges();

            TempData["Success"] = $"Rol '{nombre}' eliminado correctamente";
            return RedirectToPage("/Usuarios/Roles");
        }
    }
}
````

===== FILE: Pages/Usuarios/RolesEdit.cshtml =====

````html
@page "{id:int}"
@model Kirkenta.Pages.Usuarios.RolesEditModel
@{
    ViewData["Title"] = "Editar rol";
}

<div class="module-header">
    <div class="module-header-left">
        <a asp-page="/Usuarios/Roles" class="back-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="19" y1="12" x2="5" y2="12"/>
                <polyline points="12 19 5 12 12 5"/>
            </svg>
            Volver a roles
        </a>
        <h2>Editar rol: @Model.Input.Nombre</h2>
        <p>Modifica la información y permisos del rol</p>
    </div>
</div>

<div class="module-card form-card-wide">
    <form method="post" class="erp-form">
        @if (!ViewData.ModelState.IsValid)
        {
            <div class="module-alert module-alert-error">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <div asp-validation-summary="All"></div>
            </div>
        }

        <input type="hidden" asp-for="Input.Id" />
        <input type="hidden" asp-for="Input.EsSistema" />

        <div class="form-row">
            <div class="form-field">
                <label asp-for="Input.Nombre">Nombre del rol *</label>
                <input asp-for="Input.Nombre" class="form-input" required readonly="@Model.Input.EsSistema" />
                @if (Model.Input.EsSistema)
                {
                    <small class="form-help">Los roles del sistema no pueden cambiar de nombre</small>
                }
            </div>
            <div class="form-field">
                <label asp-for="Input.Color">Color del rol</label>
                <div class="color-picker-wrapper">
                    <input asp-for="Input.Color" type="color" class="color-picker" id="colorPicker" />
                    <span class="color-preview" id="colorPreview" style="background: @Model.Input.Color; color: #fff; padding: 4px 10px; border-radius: 6px; font-size: 12px; font-weight: 600;">@Model.Input.Color</span>
                </div>
            </div>
        </div>

        <div class="form-field">
            <label asp-for="Input.Descripcion">Descripción</label>
            <textarea asp-for="Input.Descripcion" class="form-input" rows="2"></textarea>
        </div>

        <div class="form-divider"></div>

        <div class="permisos-header">
            <h3 class="form-section-title">Permisos del rol</h3>
            <p class="form-section-desc">
                Marca el módulo para dar acceso completo, o selecciona submódulos específicos.
            </p>
        </div>

        <div class="permisos-toolbar">
            <button type="button" class="btn-sm btn-secondary" id="btnSelectAll">Seleccionar todo</button>
            <button type="button" class="btn-sm btn-secondary" id="btnClearAll">Limpiar todo</button>
            <span class="permisos-counter">
                <strong id="totalSeleccionados">0</strong> permisos seleccionados
            </span>
        </div>

        <div class="permisos-table-wrapper">
            <table class="permisos-table">
                <thead>
                    <tr>
                        <th style="width: 40px;">
                            <input type="checkbox" id="checkAllModulos" title="Seleccionar todos los módulos" />
                        </th>
                        <th>Módulo / Submódulo</th>
                        <th style="width: 70px; text-align: center;">Ver</th>
                        <th style="width: 70px; text-align: center;">Crear</th>
                        <th style="width: 70px; text-align: center;">Editar</th>
                        <th style="width: 70px; text-align: center;">Eliminar</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var modulo in Model.Modulos)
                    {
                        var moduloKey = modulo.Key;
                        var submodulos = modulo.Value;
                        var permisoModulo = Model.PermisosExistentes.GetValueOrDefault(moduloKey);

                        <!-- Módulo padre -->
                        <tr class="permiso-row-modulo">
                            <td>
                                <input type="checkbox"
                                       class="check-modulo"
                                       data-modulo="@moduloKey"
                                       @(permisoModulo?.PuedeVer == true ? "checked" : "") />
                            </td>
                            <td>
                                <strong>@Kirkenta.Helpers.ModulosERP.NombreBonito(moduloKey)</strong>
                            </td>
                            <td style="text-align: center;">
                                <input type="checkbox"
                                       name="Input.Permisos[@moduloKey].PuedeVer"
                                       value="true"
                                       class="permiso-ver permiso-modulo-ver"
                                       data-modulo="@moduloKey"
                                       data-tipo="ver"
                                       @(permisoModulo?.PuedeVer == true ? "checked" : "") />
                            </td>
                            <td style="text-align: center;">
                                <input type="checkbox"
                                       name="Input.Permisos[@moduloKey].PuedeCrear"
                                       value="true"
                                       class="permiso-crear permiso-modulo-crear"
                                       data-modulo="@moduloKey"
                                       data-tipo="crear"
                                       @(permisoModulo?.PuedeCrear == true ? "checked" : "") />
                            </td>
                            <td style="text-align: center;">
                                <input type="checkbox"
                                       name="Input.Permisos[@moduloKey].PuedeEditar"
                                       value="true"
                                       class="permiso-editar permiso-modulo-editar"
                                       data-modulo="@moduloKey"
                                       data-tipo="editar"
                                       @(permisoModulo?.PuedeEditar == true ? "checked" : "") />
                            </td>
                            <td style="text-align: center;">
                                <input type="checkbox"
                                       name="Input.Permisos[@moduloKey].PuedeEliminar"
                                       value="true"
                                       class="permiso-eliminar permiso-modulo-eliminar"
                                       data-modulo="@moduloKey"
                                       data-tipo="eliminar"
                                       @(permisoModulo?.PuedeEliminar == true ? "checked" : "") />
                            </td>
                        </tr>

                        <!-- Submódulos -->
                        @foreach (var sub in submodulos)
                        {
                            var permisoSub = permisoModulo?.Submodulos?.GetValueOrDefault(sub);
                            <tr class="permiso-row-submodulo">
                                <td></td>
                                <td>
                                    <span class="submodulo-label">└ @Kirkenta.Helpers.ModulosERP.NombreBonitoSub(sub)</span>
                                </td>
                                <td style="text-align: center;">
                                    <input type="checkbox"
                                           name="Input.Permisos[@moduloKey].Submodulos[@sub].PuedeVer"
                                           value="true"
                                           class="permiso-ver permiso-sub-ver"
                                           data-modulo="@moduloKey"
                                           data-submodulo="@sub"
                                           data-tipo="ver"
                                           @(permisoSub?.PuedeVer == true ? "checked" : "") />
                                </td>
                                <td style="text-align: center;">
                                    <input type="checkbox"
                                           name="Input.Permisos[@moduloKey].Submodulos[@sub].PuedeCrear"
                                           value="true"
                                           class="permiso-crear permiso-sub-crear"
                                           data-modulo="@moduloKey"
                                           data-submodulo="@sub"
                                           data-tipo="crear"
                                           @(permisoSub?.PuedeCrear == true ? "checked" : "") />
                                </td>
                                <td style="text-align: center;">
                                    <input type="checkbox"
                                           name="Input.Permisos[@moduloKey].Submodulos[@sub].PuedeEditar"
                                           value="true"
                                           class="permiso-editar permiso-sub-editar"
                                           data-modulo="@moduloKey"
                                           data-submodulo="@sub"
                                           data-tipo="editar"
                                           @(permisoSub?.PuedeEditar == true ? "checked" : "") />
                                </td>
                                <td style="text-align: center;">
                                    <input type="checkbox"
                                           name="Input.Permisos[@moduloKey].Submodulos[@sub].PuedeEliminar"
                                           value="true"
                                           class="permiso-eliminar permiso-sub-eliminar"
                                           data-modulo="@moduloKey"
                                           data-submodulo="@sub"
                                           data-tipo="eliminar"
                                           @(permisoSub?.PuedeEliminar == true ? "checked" : "") />
                                </td>
                            </tr>
                        }
                    }
                </tbody>
            </table>
        </div>

        <div class="form-actions">
            <a asp-page="/Usuarios/Roles" class="btn-secondary">Cancelar</a>
            <button type="submit" class="btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="btn-icon">
                    <polyline points="20 6 9 17 4 12"/>
                </svg>
                Guardar cambios
            </button>
        </div>
    </form>
</div>

@section Scripts {
    <script>
        (function () {
            var colorPicker = document.getElementById('colorPicker');
            var colorPreview = document.getElementById('colorPreview');
            if (colorPicker && colorPreview) {
                colorPreview.style.background = colorPicker.value;
                colorPreview.style.color = '#fff';
                colorPicker.addEventListener('input', function () {
                    colorPreview.textContent = colorPicker.value;
                    colorPreview.style.background = colorPicker.value;
                });
            }

            function updateCounter() {
                var total = document.querySelectorAll('.permisos-table input[type="checkbox"][name]:checked').length;
                var counter = document.getElementById('totalSeleccionados');
                if (counter) counter.textContent = total;
            }

            // === Check del módulo padre ===
            document.querySelectorAll('.check-modulo').forEach(function (checkPadre) {
                checkPadre.addEventListener('change', function () {
                    var modulo = this.getAttribute('data-modulo');
                    var estado = this.checked;

                    document.querySelectorAll(
                        '.permiso-modulo-ver[data-modulo="' + modulo + '"],' +
                        '.permiso-modulo-crear[data-modulo="' + modulo + '"],' +
                        '.permiso-modulo-editar[data-modulo="' + modulo + '"],' +
                        '.permiso-modulo-eliminar[data-modulo="' + modulo + '"]'
                    ).forEach(function (cb) { cb.checked = estado; });

                    document.querySelectorAll(
                        '.permiso-sub-ver[data-modulo="' + modulo + '"],' +
                        '.permiso-sub-crear[data-modulo="' + modulo + '"],' +
                        '.permiso-sub-editar[data-modulo="' + modulo + '"],' +
                        '.permiso-sub-eliminar[data-modulo="' + modulo + '"]'
                    ).forEach(function (cb) { cb.checked = estado; });

                    updateCounter();
                });
            });

            // === "Ver" del módulo padre → propaga a "Ver" de submódulos ===
            document.querySelectorAll('.permiso-modulo-ver').forEach(function (checkVer) {
                checkVer.addEventListener('change', function () {
                    var modulo = this.getAttribute('data-modulo');
                    var estado = this.checked;

                    document.querySelectorAll('.permiso-sub-ver[data-modulo="' + modulo + '"]').forEach(function (cb) {
                        cb.checked = estado;
                    });
                    updateCounter();
                });
            });

            // === Sincronizar check-modulo si todos los permisos del módulo están marcados ===
            document.querySelectorAll('.permiso-modulo-ver, .permiso-modulo-crear, .permiso-modulo-editar, .permiso-modulo-eliminar').forEach(function (cb) {
                cb.addEventListener('change', function () {
                    var modulo = this.getAttribute('data-modulo');
                    var todosPadre = document.querySelectorAll(
                        '.permiso-modulo-ver[data-modulo="' + modulo + '"],' +
                        '.permiso-modulo-crear[data-modulo="' + modulo + '"],' +
                        '.permiso-modulo-editar[data-modulo="' + modulo + '"],' +
                        '.permiso-modulo-eliminar[data-modulo="' + modulo + '"]'
                    );
                    var todosMarcados = Array.from(todosPadre).every(function (c) { return c.checked; });
                    var checkPadre = document.querySelector('.check-modulo[data-modulo="' + modulo + '"]');
                    if (checkPadre) checkPadre.checked = todosMarcados;
                    updateCounter();
                });
            });

            document.querySelectorAll('.permisos-table input[type="checkbox"][name]').forEach(function (cb) {
                cb.addEventListener('change', updateCounter);
            });

            document.getElementById('btnSelectAll').addEventListener('click', function () {
                document.querySelectorAll('.permisos-table input[type="checkbox"]').forEach(function (cb) {
                    if (cb.id !== 'checkAllModulos') cb.checked = true;
                });
                updateCounter();
            });

            document.getElementById('btnClearAll').addEventListener('click', function () {
                document.querySelectorAll('.permisos-table input[type="checkbox"]').forEach(function (cb) {
                    if (cb.id !== 'checkAllModulos') cb.checked = false;
                });
                updateCounter();
            });

            document.getElementById('checkAllModulos').addEventListener('change', function () {
                var estado = this.checked;
                document.querySelectorAll('.permisos-table input[type="checkbox"]').forEach(function (cb) {
                    if (cb.id !== 'checkAllModulos') cb.checked = estado;
                });
                updateCounter();
            });

            updateCounter();
        })();
    </script>
}
````

===== FILE: Pages/Usuarios/RolesEdit.cshtml.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Usuarios
{
    public class RolesEditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RolesEditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Dictionary<string, List<string>> Modulos { get; set; } = new();

        public Dictionary<string, PermisoModuloView> PermisosExistentes { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(50)]
            public string Nombre { get; set; } = string.Empty;

            [StringLength(255)]
            public string? Descripcion { get; set; }

            public string Color { get; set; } = "#4f46e5";

            public bool EsSistema { get; set; }

            public Dictionary<string, PermisoModuloInput> Permisos { get; set; } = new();
        }

        public class PermisoModuloInput
        {
            public bool PuedeVer { get; set; }
            public bool PuedeCrear { get; set; }
            public bool PuedeEditar { get; set; }
            public bool PuedeEliminar { get; set; }
            public Dictionary<string, PermisoModuloInput> Submodulos { get; set; } = new();
        }

        public class PermisoModuloView
        {
            public bool PuedeVer { get; set; }
            public bool PuedeCrear { get; set; }
            public bool PuedeEditar { get; set; }
            public bool PuedeEliminar { get; set; }
            public Dictionary<string, PermisoModuloView> Submodulos { get; set; } = new();
        }

        public IActionResult OnGet(int id)
        {
            // 🔒 Solo Admin
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var rol = _context.Roles.FirstOrDefault(r => r.Id == id);
            if (rol == null)
            {
                TempData["Error"] = "Rol no encontrado";
                return RedirectToPage("/Usuarios/Roles");
            }

            Modulos = ModulosERP.Modulos;

            Input = new InputModel
            {
                Id = rol.Id,
                Nombre = rol.Nombre,
                Descripcion = rol.Descripcion,
                Color = rol.Color,
                EsSistema = rol.EsSistema
            };

            // Cargar permisos existentes
            var permisosBd = _context.Permisos.Where(p => p.RolId == id).ToList();

            foreach (var permiso in permisosBd)
            {
                if (permiso.Submodulo == null)
                {
                    // Permiso del módulo padre
                    if (!PermisosExistentes.ContainsKey(permiso.Modulo))
                    {
                        PermisosExistentes[permiso.Modulo] = new PermisoModuloView();
                    }
                    PermisosExistentes[permiso.Modulo].PuedeVer = permiso.PuedeVer;
                    PermisosExistentes[permiso.Modulo].PuedeCrear = permiso.PuedeCrear;
                    PermisosExistentes[permiso.Modulo].PuedeEditar = permiso.PuedeEditar;
                    PermisosExistentes[permiso.Modulo].PuedeEliminar = permiso.PuedeEliminar;
                }
                else
                {
                    // Permiso de submódulo
                    if (!PermisosExistentes.ContainsKey(permiso.Modulo))
                    {
                        PermisosExistentes[permiso.Modulo] = new PermisoModuloView();
                    }
                    if (!PermisosExistentes[permiso.Modulo].Submodulos.ContainsKey(permiso.Submodulo))
                    {
                        PermisosExistentes[permiso.Modulo].Submodulos[permiso.Submodulo] = new PermisoModuloView();
                    }
                    PermisosExistentes[permiso.Modulo].Submodulos[permiso.Submodulo].PuedeVer = permiso.PuedeVer;
                    PermisosExistentes[permiso.Modulo].Submodulos[permiso.Submodulo].PuedeCrear = permiso.PuedeCrear;
                    PermisosExistentes[permiso.Modulo].Submodulos[permiso.Submodulo].PuedeEditar = permiso.PuedeEditar;
                    PermisosExistentes[permiso.Modulo].Submodulos[permiso.Submodulo].PuedeEliminar = permiso.PuedeEliminar;
                }
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            // 🔒 Solo Admin
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            Modulos = ModulosERP.Modulos;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var rol = _context.Roles.FirstOrDefault(r => r.Id == Input.Id);
            if (rol == null)
            {
                TempData["Error"] = "Rol no encontrado";
                return RedirectToPage("/Usuarios/Roles");
            }

            // Verificar nombre duplicado
            if (_context.Roles.Any(r => r.Nombre.ToLower() == Input.Nombre.ToLower() && r.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe un rol con este nombre");
                return Page();
            }

            // Actualizar rol
            if (!rol.EsSistema)
            {
                rol.Nombre = Input.Nombre;
            }
            rol.Descripcion = Input.Descripcion;
            rol.Color = Input.Color;

            // Borrar permisos viejos
            var permisosViejos = _context.Permisos.Where(p => p.RolId == rol.Id);
            _context.Permisos.RemoveRange(permisosViejos);

            // Insertar permisos nuevos
            foreach (var moduloKvp in Input.Permisos)
            {
                var modulo = moduloKvp.Key;
                var permisoModulo = moduloKvp.Value;

                _context.Permisos.Add(new Permiso
                {
                    RolId = rol.Id,
                    Modulo = modulo,
                    Submodulo = null,
                    PuedeVer = permisoModulo.PuedeVer,
                    PuedeCrear = permisoModulo.PuedeCrear,
                    PuedeEditar = permisoModulo.PuedeEditar,
                    PuedeEliminar = permisoModulo.PuedeEliminar
                });

                foreach (var subKvp in permisoModulo.Submodulos)
                {
                    var submodulo = subKvp.Key;
                    var permisoSub = subKvp.Value;

                    _context.Permisos.Add(new Permiso
                    {
                        RolId = rol.Id,
                        Modulo = modulo,
                        Submodulo = submodulo,
                        PuedeVer = permisoSub.PuedeVer,
                        PuedeCrear = permisoSub.PuedeCrear,
                        PuedeEditar = permisoSub.PuedeEditar,
                        PuedeEliminar = permisoSub.PuedeEliminar
                    });
                }
            }

            _context.SaveChanges();

            TempData["Success"] = $"Rol '{rol.Nombre}' actualizado correctamente";
            return RedirectToPage("/Usuarios/Roles");
        }
    }
}
````

====================================================
 Properties - 1 archivo(s)
====================================================

===== FILE: Properties/launchSettings.json =====

````json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5114",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:7060;http://localhost:5114",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}

````

====================================================
 RAIZ - 7 archivo(s)
====================================================

===== FILE: .gitignore =====

````plaintext
# ===== BUILD =====
bin/
obj/
[Dd]ebug/
[Rr]elease/
*.dll
*.pdb
*.user
*.suo

# ===== IDE =====
.vs/
.vscode/
.idea/

# ===== NUGET =====
*.nupkg
packages/

# ===== LOGS =====
*.log

# ===== CONFIG SENSIBLE =====
appsettings.Development.json
appsettings.Production.json

# ===== SISTEMA =====
.DS_Store
Thumbs.db

# ===== UPLOADS DE USUARIOS =====
wwwroot/uploads/

# ===== ACCESOS DIRECTOS =====
*.lnk

# ===== CARPETAS DEL PROYECTO PHP VIEJO =====
app/
config/
public/
storage/
vendor/
includes/
modules/
backups/
logs/
assets/
uploads/

````

===== FILE: appsettings.json =====

````json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=Kirkenta;User=root;Password='Noacp0910';"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
````

===== FILE: CONTEXTO.md =====

````markdown
# KIRKENTA ERP — CONTEXTO MAESTRO

Cómo usar: Pega este archivo completo al inicio de un chat nuevo. Di: "Retomamos Kirkenta ERP. Contexto: [pegas esto]". El asistente retomará al instante sin explicaciones adicionales.

================================================================================
1. STACK TÉCNICO
================================================================================

- Framework: ASP.NET Core Razor Pages
- .NET: net10.0 (TargetFramework: net10.0)
- ORM: Entity Framework Core 9.0.0
- Base de datos: MySQL (Pomelo.EntityFrameworkCore.MySql 9.0.0)
- Autenticación: Cookies (esquema "KirkentaAuth", 8 horas, sliding)
- Sesión: "KirkentaSession" (30 min, para wizard de importación)
- Frontend: Bootstrap 5 + CSS custom (wwwroot/css/theme.css)
- Charts: Chart.js 4.4.0 (CDN jsdelivr)
- Excel: ClosedXML 0.105.1
- CSV: CsvHelper 33.1.0
- Password hashing: BCrypt.Net-Next 4.2.0
- Git: https://github.com/Kirklen0910/Kirkenta
- Ruta local: C:\Users\crobe\OneDrive\Kirkenta
- Puerto local: http://localhost:5114 (HTTP, no HTTPS)

================================================================================
2. ESTRUCTURA DE CARPETAS
================================================================================

Kirkenta/
├── Data/ApplicationDbContext.cs
├── Helpers/
│   ├── Export/ (ExcelExporter, CsvExporter, JsonExporter, ExportColumn, ExportColumns)
│   ├── Import/ (CampoMapeo, ImportWizardConfig, ImportWizardHelper, ParserHelper,
│   │             ColumnMapper, ImportResult, ImportFile, ExcelImporter, CsvImporter)
│   ├── Finanzas/ (AperturaHelper, CierreHelper, DistribucionHelper,
│   │             MovimientoAutomaticoHelper, SaldoHelper, AdjuntoCierreHelper,
│   │             FinanzasSeeder, CierreContableHelper, ConciliacionHelper,
│   │             ContabilidadHelper, ISVHelper, PlanCuentasHelper)
│   ├── RRHH/ (EmpleadoHelper, AdjuntoEmpleadoHelper, NominaHelper, VacacionHelper,
│   │         TipoDocumentoEmpleadoSeeder, FeriadoSeeder)
│   ├── Logistica/ (EnvioHelper, RutaHelper, ZonaEnvioSeeder, AlertaEnvioHelper)
│   ├── ActividadHelper.cs
│   ├── ModulosERP.cs
│   ├── NumeroDocumentoHelper.cs
│   ├── PermisoHelper.cs
│   └── PermisoSeeder.cs
├── Migrations/ (desincronizado, ver sección 10)
├── Models/ (todos los modelos)
├── Pages/
│   ├── Auth/ (Login, Logout, Register)
│   ├── Usuarios/ (Index, Create, Edit, Delete, Roles, RolesCreate, RolesEdit, RolesDelete, Perfil, CambiarPassword, Actividad)
│   ├── POS/ (Index)
│   ├── Ventas/ (Index, Details)
│   ├── Clientes/ (Index, Create, Edit, Delete, Export, Import, ImportMap)
│   ├── Cotizaciones/ (Index, Create, Edit, Delete, Details)
│   ├── Pedidos/ (Index, Create, Edit, Details)
│   ├── Facturas/ (Index, Details, RegistrarPago, CreateFromVenta)
│   ├── Devoluciones/ (Index)
│   ├── Productos/ (Index, Create, Edit, Delete, Export, Import, ImportMap)
│   ├── Categorias/ (Index, Create, Edit, Delete, Export, Import, ImportMap)
│   ├── UnidadesMedida/ (Index, Create, Edit, Delete, Export, Import, ImportMap)
│   ├── Impuestos/ (Index, Create, Edit, Delete, Export, Import, ImportMap)
│   ├── Bajas/ (Index, Create, Details, Aprobar, Revertir)
│   ├── Compras/ (Index, HistorialProducto)
│   ├── Proveedores/ (Index, Create, Edit, Delete, Details, Export, Import, ImportMap)
│   ├── OrdenesCompra/ (Index, Create, Edit, Delete, Details, Recibir)
│   ├── PagosProveedor/ (Index, Create, Details)
│   ├── Finanzas/
│   │   ├── Index
│   │   ├── Cuentas/ (Index, Create, Edit, Delete)
│   │   ├── Categorias/ (Index, Create, Edit, Delete)
│   │   ├── Movimientos/ (Index, Create, Edit, Anular)
│   │   ├── Aperturas/ (Index, Create)
│   │   ├── Cierres/ (Index, Create, Details, Aprobar)
│   │   ├── CierresContables/ (Index, Create, Reabrir)
│   │   ├── Conciliacion/ (Index, Create, Details)
│   │   ├── Contabilidad/ (EstadoResultados, BalanceGeneral)
│   │   ├── PlanCuentas/ (Index, Create, Edit, Delete)
│   │   └── Reportes/ (Index)
│   ├── RRHH/
│   │   ├── Index
│   │   ├── Empleados/ (Index, Create, Edit, Delete, Details)
│   │   ├── Documentos/ (Index)
│   │   ├── Expedientes/ (Index, Create)
│   │   ├── Vales/ (Index, Create, Aprobar, Entregar)
│   │   ├── Vacaciones/ (Index, Create, Aprobar)
│   │   ├── Permisos/ (Index, Create, Aprobar)
│   │   ├── Nomina/ (Index, Create, Details, Aprobar, Pagar, PagarEmpleado)
│   │   ├── Feriados/ (Index, Create, Edit)
│   │   ├── Alertas/ (Index, Create)
│   │   └── Reportes/ (Index)
│   ├── Logistica/  ⬅ EN DESARROLLO
│   │   ├── Index (Dashboard con alertas)  ✅ COMPLETADO
│   │   ├── Envios/
│   │   │   ├── Index  ✅ COMPLETADO
│   │   │   └── Details  ✅ COMPLETADO
│   │   ├── Rutas/
│   │   │   ├── Index  ✅ COMPLETADO
│   │   │   ├── Create  ✅ COMPLETADO
│   │   │   ├── Details  ✅ COMPLETADO
│   │   │   └── Edit  ✅ COMPLETADO
│   │   ├── Zonas/
│   │   │   ├── Index  ✅ COMPLETADO
│   │   │   ├── Create  ✅ COMPLETADO
│   │   │   ├── Edit  ✅ COMPLETADO
│   │   │   └── Delete  ✅ COMPLETADO
│   │   ├── Repartidores/
│   │   │   ├── Index  ✅ COMPLETADO
│   │   │   ├── Create  ✅ COMPLETADO
│   │   │   ├── Edit  ✅ COMPLETADO
│   │   │   └── Delete  ✅ COMPLETADO
│   │   ├── Tracking/
│   │   │   └── Index (público, AllowAnonymous)  ✅ COMPLETADO
│   │   └── Reportes/ (Index)  ⬅ PENDIENTE (último archivo del Bloque 5)
│   ├── Reportes/ (Index, Ventas, Compras)
│   ├── Configuracion/ (Index, Edit)
│   ├── MetodosPago/ (Index, Create, Edit, Delete)
│   ├── Nomenclatura/ (Index)
│   ├── Series/ (Index, Create, Edit, Delete)
│   └── Shared/
│       ├── _Layout.cshtml  ✅ ACTUALIZADO (Logística completa)
│       ├── _LayoutPOS.cshtml
│       ├── _ValidationScriptsPartial.cshtml
│       ├── _ImportStepIndicator.cshtml
│       └── _ImportUploadForm.cshtml
├── wwwroot/ (css, js, images, lib, uploads)
├── Program.cs  ⚠️ ACTUALIZAR AllowAnonymousToPage
├── appsettings.json
├── CONTEXTO.md
├── PROYECTO.md
└── generar-snapshot.ps1

================================================================================
3. MÓDULOS Y SUBMÓDULOS
================================================================================

Diccionario en Helpers/ModulosERP.cs:

Usuarios: Index, Create, Edit, Delete, Roles, RolesCreate, RolesEdit, RolesDelete, Perfil, CambiarPassword, Actividad
Ventas: POS, Index, Create, Edit, Delete, Cotizaciones, Pedidos, Facturas, Clientes, ClientesCreate, ClientesEdit, ClientesDelete, ClientesExport, ClientesImport, Devoluciones
Inventario: Index, Productos (+Create/Edit/Delete/Export/Import), Categorias, UnidadesMedida, Entradas, Salidas, Bajas
Compras: Index, Proveedores (+Create/Edit/Delete/Export/Import), Ordenes (+Create/Edit/Delete/Recibir), Pagos (+Create), CuentasPorPagar, Reportes
Finanzas: Index, Cuentas (+Create/Edit/Delete), Categorias (+Create/Edit/Delete),
         Movimientos (+Create/Edit/Anular), Aperturas (+Create),
         Cierres (+Create/Aprobar),
         CierresContables (+Create/Reabrir),
         Conciliacion (+Create/Details/Cerrar),
         PlanCuentas (+Create/Edit/Delete),
         ContabilidadEstadoResultados, ContabilidadBalanceGeneral,
         Reportes, FlujoCaja, EstadoResultados, BalanceGeneral, ISV
RRHH: Index, Empleados (+Create/Edit/Delete), Documentos (+Create/Delete), Expedientes (+Create/Delete), Vacaciones (+Create/Aprobar), Permisos (+Create/Aprobar), Vales (+Create/Aprobar/Entregar), Nomina (+Create/Aprobar/Pagar), Feriados (+Create), Alertas (+Create), Reportes
Logistica: Index,
           Envios, EnviosDetails, EnviosAsignar,
           Rutas, RutasCreate, RutasEdit, RutasDetails, RutasDespachar, RutasCerrar,
           Zonas, ZonasCreate, ZonasEdit, ZonasDelete,
           Repartidores, RepartidoresCreate, RepartidoresEdit, RepartidoresDelete,
           Tracking,
           Reportes
Reportes: Index, Ventas, Compras
Activos: Index, Mantenimiento
Seguridad: Index, Auditoria
Configuracion: Index, Empresa, Nomenclatura, Series, MetodosPago

NOTA sobre Import/Export (04/10/2026):
- Clientes, Productos, Proveedores: ya tenían wizard (refactorizado a genérico)
- Categorías, Impuestos, Unidades de medida: wizard AGREGADO
- Todos usan ImportWizardHelper + vistas parciales compartidas

NOTA sobre Logística (sesión actual — 04/10/2026 sesión 5):
- Bloque 1 COMPLETADO: AlertaEnvioHelper + Logistica/Index (dashboard con alertas)
- Bloque 2 COMPLETADO: Envios/Index + Envios/Details
- Bloque 3 COMPLETADO: Rutas/Index + Rutas/Create + Rutas/Details + Rutas/Edit
- Bloque 4 COMPLETADO: Zonas (Index/Create/Edit/Delete) + Repartidores (Index/Create/Edit/Delete)
- Bloque 4 COMPLETADO: _Layout.cshtml con submenú Logística completo
- Bloque 5 EN PROGRESO: Tracking/Index ✅ COMPLETADO
- Bloque 5 PENDIENTE: Reportes/Index (último archivo)

================================================================================
4. PERMISOS
================================================================================

Tabla Permisos: RolId, Modulo, Submodulo (nullable), PuedeVer, PuedeCrear, PuedeEditar, PuedeEliminar
Índice único en (RolId, Modulo, Submodulo)
Submodulo = null → permiso del módulo padre
Admin siempre tiene acceso total (bypass en PermisoHelper)
Pendiente = usuario sin rol

Helpers (PermisoHelper):
- TienePermiso(context, rol, modulo, submodulo, "ver|crear|editar|eliminar")
- ModulosVisibles(context, rol) → módulos con PuedeVer (padre o algún hijo)
- SubmodulosVisibles(context, rol, modulo) → submódulos con PuedeVer

PermisoSeeder.MigrarPermisosFaltantes(db):
- Se ejecuta al arrancar la app (Program.cs)
- Para cada rol (excepto Admin), crea permisos faltantes copiando del módulo padre
- Idempotente

Convención de nombres:
- Módulos padre: "Clientes", "Productos", "Cuentas", "Movimientos", "Cierres", "Empleados", "Vales", "Nomina", "Logistica", etc.
- Submódulos (con acción): "ClientesCreate", "ClientesEdit", "ClientesDelete", "CuentasCreate", "CierresAprobar", "ValesEntregar", "NominaPagar", "EnviosDetails", "RutasCreate", "ZonasCreate", "RepartidoresCreate"
- Genéricos: "Index", "Create", "Edit", "Delete"

================================================================================
5. HELPERS CLAVE
================================================================================

NumeroDocumentoHelper
- GenerarSiguiente(context, tipo) → genera número y avanza el correlativo
- PreviewSiguiente(context, tipo) → solo muestra el próximo
- SincronizarSerie() → ajusta el correlativo si detecta desfase con la BD
- Soporta formato personalizado: {PREFIX}, {SUFFIX}, {SEP}, {NUM}, {YEAR}, {MONTH}, {DAY}
- Tipos soportados: Cotizacion, Pedido, Venta, Factura, Devolucion, Producto, Baja, Proveedor, OrdenCompra, PagoProveedor, DevolucionProveedor, MovimientoFinanciero, AperturaCaja, CierreCaja, ConciliacionBancaria, Empleado, ValeEmpleado, Vacacion, PermisoEmpleado, Nomina, PagoNomina, Envio, Ruta, Repartidor

ActividadHelper
- Registrar(context, usuarioId, "acción", "detalle", ipAddress, userAgent)
- Nunca lanza excepción (silencioso si falla)

SaldoHelper (Finanzas)
- Aplicar(context, movimiento) → ajusta saldo de cuentas
- Revertir(context, movimiento) → revierte al anular

MovimientoAutomaticoHelper (Finanzas)
- RegistrarIngresoVenta / RegistrarEgresoPagoProveedor / RegistrarEgresoNomina
- Resuelve cuenta y categoría por defecto si no se especifican

AperturaHelper (Finanzas)
- Abrir / ObtenerAperturaActiva / HayAperturaActiva / Cerrar

CierreHelper (Finanzas)
- Calcular / Registrar / DeterminarResultado
- Genera ajuste automático si hay diferencia

DistribucionHelper (Finanzas)
- RegistrarDistribuciones / ObtenerDistribuciones
- Tipos: RetiroBanco, FondoCaja, EntregaAdmin, PagoDirecto, Otro

AdjuntoCierreHelper (Finanzas)
- Guardar / Eliminar (pdf, jpg, jpeg, png, máx 10 MB)

CierreContableHelper (Finanzas)
- ValidarFecha / Cerrar / Reabrir / ObtenerEstadoAnual / Obtener

ConciliacionHelper (Finanzas)
- Crear / CargarLineasSistema / MatchingAutomatico / MatchingManual
- DeshacerMatch / RecalcularTotales / Cerrar / Cancelar

ContabilidadHelper (Finanzas)
- GenerarEstadoResultados / GenerarBalanceGeneral / ContarSinPlanCuenta

ISVHelper (Finanzas)
- Calcular / ObtenerTasaProducto / SincronizarItems / RecalcularFactura / ObtenerTasasActivas
- NO hardcodea tasas: siempre lee del catálogo Impuestos
- Soporta montoEnvio (el envío se grava con la tasa predeterminada)

PlanCuentasHelper (Finanzas)
- Seed (81 cuentas estándar Honduras) / ObtenerTodas / ObtenerCuentasMovimiento / RecalcularJerarquia

EmpleadoHelper (RRHH)
- GenerarCodigo / PreviewCodigo
- CalcularAniosAntiguedad / CalcularMesesAntiguedad
- CalcularDiasVacacionesPorAntiguedad (lee ConfiguracionEmpresa.RHTablaVacaciones)
- ProcesarAcumulacionVacaciones / ProcesarAcumulacionGlobal
- ObtenerFeriados

AdjuntoEmpleadoHelper (RRHH)
- Guardar / GuardarFotoPerfil / Eliminar / ActualizarVigencias
- Extensiones: pdf, jpg, jpeg, png, docx, doc. Máx 15 MB (foto 5 MB)

NominaHelper (RRHH)
- ObtenerConfiguracion (crea config por defecto si no existe)
- CalcularISRMensual / CalcularISRAcumulativo
- CalcularIHSS / CalcularRAP
- CalcularSalarioProporcional
- CalcularPeriodo / DiasPeriodo
- ObtenerValesActivos / CalcularDescuentoVales
- CalcularDetalleEmpleado

VacacionHelper (RRHH)
- CalcularDias (excluye fines de semana y feriados del país)
- Solicitar / Aprobar / Rechazar

EnvioHelper (Logística)
- CrearParaVenta(context, venta, direccion, referencia, contactoNombre, contactoTelefono, ciudad, zonaId, monto, usuarioId) → Envio
- CrearParaCotizacion / CrearParaPedido / CrearParaFactura (mismos parámetros)
- MarcarEntregado(context, envioId, nombreRecibio, firmaImagen, fotoEntrega, notas, usuarioId) → (ok, error)
- MarcarFallido(context, envioId, motivoFallo, usuarioId) → (ok, error)
- Reagendar(context, envioId, notas, usuarioId) → (ok, error)
- ObtenerPendientesSinRuta(context) → List<Envio>
- GenerarTrackingCode() → string (formato KRT-2026-AB12X9)

RutaHelper (Logística)
- Crear(context, envioIds, repartidorId, repartidorNombre, vehiculo, placa, zonaId, descripcion, fechaEntregaEstimada, notas, usuarioId) → (ruta, error)
- Despachar(context, rutaId, usuarioId) → (ok, error)
- Cerrar(context, rutaId, notas, usuarioId) → (ok, error)
- Cancelar(context, rutaId, motivo, usuarioId) → (ok, error)
- AgregarEnvio(context, rutaId, envioId) → (ok, error)
- QuitarEnvio(context, rutaId, envioId) → (ok, error)
- Reordenar(context, rutaId, envioIdsEnOrden) → (ok, error)
- ObtenerEnviosDeRuta(context, rutaId) → List<Envio>
- RecalcularTotales(context, rutaId)

ZonaEnvioSeeder (Logística)
- Seed(context) → crea 5 zonas típicas de Honduras
  (Centro/Casco Urbano L.50, Cercana L.80, Media L.150, Lejana L.300, Envío Gratis L.0)
- Idempotente

AlertaEnvioHelper (Logística)
- CalcularSemaforo(envio) → "Verde" | "Amarillo" | "Rojo"
  - Entregado → Verde
  - Fallido → Rojo
  - Cancelado → Verde (no alerta)
  - Pendiente/EnRuta + fecha estimada > hoy → Verde
  - Pendiente/EnRuta + fecha estimada == hoy → Amarillo
  - Pendiente/EnRuta + fecha estimada < hoy → Rojo
- EtiquetaSemaforo(semaforo) → string con emoji + texto
- DiasDiferencia(envio) → int? (positivo = atrasado, negativo = días por venir)
- ObtenerEnviosAtrasados(context) → List<Envio>
- ObtenerEnviosVencenHoy(context) → List<Envio>
- ObtenerEnviosProximosAVencer(context, dias = 2) → List<Envio>
- ObtenerEnviosFallidos(context) → List<Envio>
- ObtenerEnviosEntregadosHoy(context) → List<Envio>
- ObtenerRutasAtrasadas(context) → List<Ruta> (EnReparto con paradas vencidas)
- ObtenerRutasDespachadasHoy(context) → List<Ruta>
- ObtenerResumenAlertas(context) → ResumenAlertas { EnviosAtrasados, EnviosVencenHoy, EnviosProximosAVencer, EnviosFallidos, EnviosEntregadosHoy, RutasActivas, RutasAtrasadas, RutasDespachadasHoy, HayAlertas }
- TODO SE CALCULA EN TIEMPO REAL, no persiste nada

Export
- ExportColumns.Clientes() / .Productos(cats, imps) / .Proveedores()
- ExportColumns.Categorias() / .Impuestos() / .UnidadesMedida()
- ExcelExporter.Export / CsvExporter.Export / JsonExporter.Export

Import (wizard genérico) — REFACTORIZADO 04/10/2026
- ImportWizardHelper.ProcesarArchivo(archivo) → ResultadoLectura
- ImportWizardHelper.GuardarEnSesion / RecuperarDeSesion / LimpiarSesion
- ParserHelper.ParsearBool / ParsearInt / ParsearDecimal
- CampoMapeo: { Key, Label, Requerido, Ayuda }
- ImportWizardConfig: { NombrePlural, NombreSingular, ModuloPermiso, SubmoduloPermiso,
                        SessionKey, UrlIndex, UrlImport, UrlImportMap,
                        Campos, AliasCampos, CamposRequeridos }
- Vistas parciales: _ImportStepIndicator, _ImportUploadForm
- Wizard en uso: Clientes, Productos, Proveedores, Categorías, Impuestos, UnidadesMedida
- ExcelImporter.Read / CsvImporter.Read → ImportFile
- ColumnMapper.Detectar(headers, aliasPorCampo) → Dictionary<string, string?>
- Modos de importación: "upsert" | "crear" | "actualizar"

================================================================================
6. CONVENCIONES
================================================================================

Nombres:
- Modelos y propiedades en español (Producto, PrecioVenta)
- Namespace: Kirkenta.Pages.[Modulo] o Kirkenta.Pages.[Modulo].[Sub]
- PageModels: IndexModel, CreateModel, EditModel, DeleteModel, DetailsModel

Patrón de permisos en code-behind:
var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
var currentRol = currentUser?.Rol ?? "Pendiente";
if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Modulo", "Submodulo", "ver"))
{
    TempData["Error"] = "No tienes permiso";
    return RedirectToPage("/Modulo/Index");
}

Exponer permisos a la vista:
public bool PuedeCrear { get; set; }
public bool PuedeEditar { get; set; }
public bool PuedeEliminar { get; set; }
// En OnGet:
PuedeCrear = currentRol == "Admin" || PermisoHelper.TienePermiso(..., "crear");

TempData:
- TempData["Success"] → banner verde
- TempData["Error"] → banner rojo
- TempData["Warning"] → banner amarillo

Actividad:
ActividadHelper.Registrar(_context, currentUser?.Id ?? 0, "Verbo + objeto", $"Detalle L. {monto:N2}", HttpContext.Connection.RemoteIpAddress?.ToString());

Estructura típica de página:
- Vista .cshtml con layout principal (_Layout)
- POS usa layout propio (_LayoutPOS)
- Tracking público usa Layout = null con diseño propio standalone
- Cabecera con breadcrumb (automática desde _Layout)
- `module-header` con título + botones de acción
- Banner TempData Success/Error
- KPIs en `kpi-grid`
- Contenido principal en `module-card`
- Modales con estilo `.pos-modal` reutilizado

Convención de semáforos (Logística):
- Verde 🟢 → completado o a tiempo
- Amarillo 🟡 → vence hoy
- Rojo 🔴 → atrasado o fallido
- Los KPIs clickeables usan `asp-route-filtro="atrasados|vencenHoy|pendientes|enruta|entregados|fallidos|sinRuta"` (envíos) o `asp-route-filtro="borrador|enreparto|completadas|atrasadas"` (rutas)

Convención de modales:
- Se abren con clase `is-open` (no display: inline)
- Cierre con función JS `cerrarModal(id)` que remueve `is-open`
- Estructura: `.pos-modal` > `.pos-modal-content` > header/body/footer

Convención de validación "no eliminar si tiene dependencias":
- Zonas: no se puede eliminar si tiene envíos asociados (`Envios.ZonaId`)
- Repartidores: no se puede eliminar si tiene rutas o envíos asignados (`Rutas.RepartidorId`, `Envios.RepartidorId`)
- La vista muestra el bloqueo con opción "Ir a Editar para desactivar"

================================================================================
7. REGLAS CRÍTICAS
================================================================================

1. SIEMPRE entregar archivos COMPLETOS, nunca fragmentos.
2. Automatizar pero permitir modo manual (checkbox "Registrar en Finanzas").
3. Movimientos NO se borran, se ANULAN.
4. Cierres requieren APROBACIÓN.
5. Nomenclatura personalizable desde /Nomenclatura → /Series.
6. Si import trae SKU/Código → respetar. Si vacío → autogenerar.
7. Apertura de caja OBLIGATORIA antes de vender en POS.
8. Al cerrar → se cierra la apertura. Al día siguiente se sugiere saldo del último cierre.
9. Distribución del efectivo: suma DEBE igualar efectivo contado.
10. Cada distribución genera su movimiento (excepto FondoCaja).
11. Nómina: flujo Calculada → Aprobada → Pagada (con pagos individuales o en lote).
12. Vales: doble aprobación (Gerente + RRHH) → Entregado → Descontado en nómina.
13. Vacaciones: cálculo excluye fines de semana y feriados del país configurado.
14. Salario proporcional: se calcula sobre 30 días (base mensual estándar).
15. ISR Honduras: método acumulativo anual por defecto (SAR).
16. Vales: límite del 50% del salario mensual del empleado.
17. Nómina: no se puede editar si tiene recepciones o pagos. Se anula.
18. OrdenCompra: no se puede editar si tiene recepciones o pagos. Estados: Borrador → Enviada → RecibidaParcial → Recibida → Pagada.
19. Período contable cerrado: bloquea movimientos, aperturas, cierres, ventas, pagos, nóminas y vales.
20. Cierre contable reabrible: requiere motivo + contraseña, queda en auditoría.
21. Conciliación: solo cuentas tipo "Banco". Matching automático por monto (±0.01) y fecha (±3 días).
22. Plan de Cuentas: código jerárquico con puntos (1, 1.1, 1.1.01). Naturaleza calculada por tipo.
23. Solo cuentas con EsMovimiento=true aceptan movimientos directos.
24. Wizard de importación GENÉRICO: usar ImportWizardHelper + vistas parciales. NO duplicar lógica.
25. Imports con códigos correlativos: pre-cargar códigos existentes en HashSet<string>, mantener correlativo local en memoria, UN solo SaveChanges al final. NUNCA llamar a NumeroDocumentoHelper.GenerarSiguiente por fila.
26. EF Core 9 en .NET 10: NUNCA usar string[] con .Contains() dentro de queries EF. Usar HashSet<string> o List<string>. El array rompe el ExpressionTreeFuncletizer (TypeLoadException con ReadOnlySpan).
27. Truncar campos de texto antes de insertar en importaciones (Nombre→150, SKU→50, Codigo→20, etc.).

REGLAS DE LOGÍSTICA:
28. Los envíos nacen al facturar una venta con "RequiereEnvio=true". El POS ya llama a EnvioHelper.CrearParaVenta automáticamente.
29. Los envíos NO se borran. Se marcan Entregado/Fallido/Cancelado.
30. Para marcar Entregado es OBLIGATORIO el nombre de quien recibe.
31. Para marcar Fallido es OBLIGATORIO el motivo.
32. Un envío Fallido se puede REAGENDAR (vuelve a Pendiente, sin ruta).
33. Las rutas agrupan envíos Pendientes. Estados: Borrador → EnReparto → Completada (o Cancelada).
34. Solo se pueden editar/quitar envíos de rutas en estado Borrador.
35. Al despachar una ruta, TODOS sus envíos pasan a estado EnRuta y se les asigna el repartidor/vehículo.
36. Cuando TODAS las paradas de una ruta se resuelven (Entregado o Fallido), la ruta se cierra automáticamente (Completada).
37. Las alertas de envíos/rutas se calculan EN TIEMPO REAL con AlertaEnvioHelper, no se persisten.
38. El semáforo se calcula: Entregado→Verde, Fallido→Rojo, Cancelado→Verde, Pendiente/EnRuta con fecha>hoy→Verde, ==hoy→Amarillo, <hoy→Rojo.
39. Una ruta NO se puede quedar sin paradas. Si el usuario quiere vaciarla, debe CANCELARLA (los envíos vuelven a Pendiente).
40. Al cancelar una ruta en Borrador, todos sus envíos vuelven a estado Pendiente y se desligan de la ruta.
41. El tracking code se genera UNA VEZ al crear la ruta (formato KRT-2026-AB12X9), es único y no cambia.
42. Las ZONAS no se eliminan si tienen envíos asociados. Se recomienda desactivarlas.
43. Los REPARTIDORES no se eliminan si tienen rutas o envíos asignados. Se recomienda desactivarlos.
44. El filtro de zona en envíos usa `Envios.ZonaId`; el nombre se denormaliza en `Envio.ZonaNombre` al crear.
45. El tracking público (/Logistica/Tracking/Index) es AllowAnonymous y NO usa _Layout (Layout = null con diseño propio).
46. En el tracking público NO se exponen montos, IDs internos, notas privadas ni motivos de fallo internos.
47. La búsqueda de tracking normaliza el código (uppercase + trim) para aceptar variaciones.

================================================================================
8. MÓDULOS COMPLETADOS
================================================================================

Core:
- Auth (Login, Register, Logout) con BCrypt
- Usuarios + Roles + Permisos (CRUD completo)
- Perfil, Cambiar contraseña, Actividad (con IP y UserAgent)
- Permisos con seeder automático
- Menú lateral con iconos + permisos dinámicos
- Breadcrumb automático desde ruta

Ventas:
- POS completo (búsqueda, carrito, cobro, modal, integración Finanzas con checkbox)
- POS crea envío automáticamente si "RequiereEnvio" está marcado
- Clientes (CRUD + Import + Export con wizard)
- Cotizaciones (CRUD + conversión a factura y a POS)
- Pedidos (CRUD + estados)
- Facturas (Index, Details, RegistrarPago, CreateFromVenta)
- Devoluciones (Index)

Inventario:
- Productos (CRUD + Import + Export con wizard)
- Categorías (CRUD + Import + Export con wizard)
- Unidades de medida (CRUD + Import + Export con wizard)
- Bajas (con aprobación y reversión + auditoría)
- Historial de compras por producto

Compras:
- Proveedores (CRUD + Import + Export con wizard)
- Órdenes de compra (Create, Edit, Recibir con recepción parcial)
- Pagos a proveedores + Cuentas por pagar (con integración Finanzas)
- Adjuntos a órdenes de compra (con validación IDOR)
- Reportes de compras

Finanzas (100%):
- Dashboard con gráficos
- Cuentas financieras (Cajas + Bancos)
- Categorías financieras (Ingreso/Egreso, +PlanCuentaId)
- Movimientos (Ingresos, Egresos, Transferencias, Anulaciones)
- Reportes (Flujo de caja, Estado de resultados, Estado de cuenta)
- Aperturas de caja
- Cierres con distribución del efectivo
- Aprobación de cierres
- Adjuntos / comprobantes
- Movimientos automáticos desde POS, PagosProveedor, Vales, Nómina
- Plan de Cuentas (81 cuentas Honduras)
- Estado de Resultados y Balance General
- Cierres Contables (bloqueo de meses + reapertura)
- Conciliación Bancaria (matching automático y manual)
- ISV Helper (dinámico por catálogo, soporta monto de envío)

RRHH (100%):
- Dashboard con KPIs + Cumpleaños + Aniversarios + Feriados + Vacaciones + Alertas
- Empleados: CRUD + foto + documentos + expediente + hoja de vida con 8 tabs
- Categorías de documentos con seeder
- Expedientes (llamados, amonestaciones, suspensiones, méritos)
- Vacaciones (excluye fines de semana y feriados) + descuento automático de saldo
- Permisos y licencias con/sin goce de sueldo
- Vales con doble aprobación → Entregado → Descontado en nómina
- Nómina completa: ISR + IHSS + RAP + vales + pago individual o en lote
- Feriados multi-país (HN, GT, SV, CR, NI, PA, MX, US)
- Alertas personalizadas
- Reportes con Chart.js
- Integración con Finanzas (egresos automáticos)

Logística (EN DESARROLLO — 4.5/5 bloques completados):
- ✅ EnvioHelper, RutaHelper, ZonaEnvioSeeder, AlertaEnvioHelper
- ✅ Modelos: Envio, Ruta, RutaHistorial, Repartidor, ZonaEnvio
- ✅ DbContext con DbSets e índices únicos
- ✅ Program.cs ejecuta ZonaEnvioSeeder.Seed
- ✅ ModulosERP actualizado con submódulos de Logística
- ✅ _Layout.cshtml con menú Logística COMPLETO
- ✅ POS integrado (crea envío al cobrar con RequiereEnvio=true)
- ✅ ISVHelper soporta montoEnvio

**Bloque 1 ✅ — Dashboard:**
- **Pages/Logistica/Index** — Dashboard con:
  - Banner de alertas críticas (rojo si hay atrasos/fallos/rutas en riesgo)
  - 4 KPIs de alertas con semáforo (atrasados, vencen hoy, entregados hoy, fallidos)
  - 4 KPIs generales (total envíos, rutas activas, repartidores, zonas)
  - Accesos rápidos
  - Panel de envíos atrasados (con días de atraso)
  - Panel de rutas atrasadas (con progreso)
  - Panel doble vencen hoy + entregados hoy
  - Panel de fallidos
  - Últimos envíos y últimas rutas

**Bloque 2 ✅ — Envíos:**
- **Pages/Logistica/Envios/Index** — Listado con:
  - 6 KPIs clickeables que aplican filtros por querystring (atrasados, vencen hoy, pendientes, en ruta, entregados, fallidos)
  - Filtros: búsqueda, estado, zona, rango de fechas
  - Semáforo visual por fila (🔴🟡🟢)
  - Columna de días de atraso
  - Muestra ruta + número de parada
  - Indicador de filtro activo con link para quitarlo
- **Pages/Logistica/Envios/Details** — Detalle con:
  - Banner rojo si atrasado, amarillo si vence hoy
  - 4 KPIs (estado, semáforo, monto, ruta)
  - Info completa de entrega (cliente, contacto, dirección, zona, notas)
  - Info post-entrega (si Entregado) o motivo (si Fallido)
  - Panel de acciones según estado: entregar/fallar (Pendiente/EnRuta), reagendar (Fallido)
  - Historial de eventos (timeline con RutaHistorial)

**Bloque 3 ✅ — Rutas:**
- **Pages/Logistica/Rutas/Index** — Listado con:
  - 5 KPIs clickeables (borrador, en reparto, atrasadas, completadas, canceladas)
  - 4 KPIs de paradas activas (activas, pendientes, entregadas, fallidas)
  - Filtros: búsqueda, estado, repartidor, zona, rango de fechas
  - Barra de progreso visual por ruta (verde 100%, rojo si atrasada, morado en proceso)
  - Indicador visual (🔴 atrasada, 🚚 en reparto, ✅ completada, 📝 borrador)
  - Muestra tracking code
- **Pages/Logistica/Rutas/Create** — Crear con:
  - Formulario: repartidor, zona, vehículo, placa, fecha estimada, descripción, notas
  - Autocompletar vehículo/placa al elegir repartidor
  - Tabla de envíos disponibles (pendientes sin ruta) con semáforo
  - Buscador en vivo
  - Botones: Seleccionar todos / Limpiar / Contador en vivo
  - Total seleccionado en footer
  - Pre-selección desde querystring `?envioId=X`
- **Pages/Logistica/Rutas/Details** — Detalle con:
  - Banner rojo si hay paradas atrasadas
  - KPIs: estado, progreso (%), monto total, paradas atrasadas
  - Info completa: repartidor, vehículo, salida/regreso, zona, descripción, notas
  - 4 KPIs de paradas (total, entregadas, fallidas, pendientes)
  - Tabla de paradas ordenada por OrdenParada con semáforo, días de atraso, info de entrega/fallo
  - Acciones individuales por parada: entregar, fallar, ver envío
  - Historial de eventos (timeline)
  - 4 modales: entregar parada, fallar parada, cerrar ruta, cancelar ruta
  - Botones contextuales: Editar (Borrador), Despachar (Borrador con paradas), Cerrar (EnReparto), Cancelar (Borrador)
- **Pages/Logistica/Rutas/Edit** — Editar con:
  - Solo permite editar rutas en Borrador
  - 3 secciones: datos, paradas actuales (quitar), envíos disponibles (agregar)
  - Checkbox "Quitar" en paradas actuales → tacha la fila + hidden input
  - Checkbox "Agregar" en disponibles → `Input.EnviosSeleccionados[i]`
  - Buscador en vivo de disponibles
  - Contador dinámico
  - Validación: no permite dejar la ruta sin paradas
  - Autocompletar vehículo/placa
  - Al guardar: actualiza datos + quita + agrega + recalcula totales

**Bloque 4 ✅ — Zonas + Repartidores:**
- **Pages/Logistica/Zonas/Index** — Listado con:
  - 3 KPIs (zonas activas, inactivas, envíos asociados)
  - Tabla con: orden, color, nombre, descripción, precio sugerido, conteo de envíos, estado
  - Botones de acción (editar, eliminar) según permisos
- **Pages/Logistica/Zonas/Create** — Crear con:
  - Nombre, descripción, precio sugerido, orden, color (con preview), activa
  - Sugerencia automática de orden (max + 1)
  - Validación de nombre único
- **Pages/Logistica/Zonas/Edit** — Editar con:
  - Aviso si tiene envíos asociados (sugerir desactivar en lugar de eliminar)
  - Validación de nombre único (excluyendo la propia)
- **Pages/Logistica/Zonas/Delete** — Eliminar con:
  - Bloqueo si tiene envíos asociados (muestra opción "Ir a Editar")
  - Confirmación con color de la zona
- **Pages/Logistica/Repartidores/Index** — Listado con:
  - 4 KPIs (activos, inactivos, rutas activas, entregas completadas)
  - Tabla con: código, nombre, contacto, vehículo + placa, rutas activas (link a filtrar), entregas, estado
  - Filtros: búsqueda (nombre/código/teléfono/placa), estado, vehículo
- **Pages/Logistica/Repartidores/Create** — Crear con:
  - Código autogenerado (NumeroDocumentoHelper tipo "Repartidor")
  - Nombre, teléfono, licencia, vehículo, placa, notas, activo
  - Validación de código único si se especifica manualmente
- **Pages/Logistica/Repartidores/Edit** — Editar con:
  - Aviso si tiene rutas activas (sugerir desactivar en lugar de eliminar)
  - Validación de código único (excluyendo el propio)
- **Pages/Logistica/Repartidores/Delete** — Eliminar con:
  - Bloqueo si tiene rutas o envíos (muestra contadores y opción "Ir a Editar")
  - Confirmación

**_Layout.cshtml ✅ ACTUALIZADO:**
- Submenú Logística reorganizado:
  - 📊 Dashboard (Index)
  - 🚚 Envíos
  - 🗺️ Rutas
  - 📍 Zonas de envío
  - 🏍️ Repartidores
  - ─── (divider)
  - 🔍 Tracking público (sin condicional, AllowAnonymous)
  - 📈 Reportes
- Diccionario `moduleNames` ampliado con: Envios, Rutas, Zonas, Repartidores, Tracking
- Filtrado por permisos con `puedeVerSubmodulo("Logistica", "X")`

**Bloque 5 EN PROGRESO — Tracking + Reportes:**
- **Pages/Logistica/Tracking/Index ✅ COMPLETADO:**
  - Página pública con `Layout = null` y diseño standalone (gradiente, tarjetas, sombras)
  - **NO** usa `_Layout.cshtml` — es una página independiente
  - Buscador por código de tracking (normaliza uppercase + trim)
  - Al encontrar ruta: muestra KPIs, progreso, repartidor, vehículo, paradas y timeline
  - Semáforos visuales por parada (entregado/fallido/enruta/pendiente)
  - NO expone datos sensibles (montos, IDs internos, notas privadas)
  - Responsive (móvil-friendly)
  - Requiere `AllowAnonymousToPage("/Logistica/Tracking/Index")` en `Program.cs`
- **Pages/Logistica/Reportes/Index ⬅ PENDIENTE** (último archivo del módulo)

Configuración:
- Datos de la empresa (con país, zona horaria, config RRHH, config alertas)
- Nomenclatura → Series de documentos
- Métodos de pago
- Impuestos (CRUD + Import + Export con wizard)

Reportes:
- Reportes de ventas (KPIs, gráficos, top productos, top clientes)
- Reportes de compras (KPIs, gráficos, detalle por proveedor y producto)

Infraestructura de Import/Export:
- Wizard genérico con vistas parciales reutilizables
- Aplicado a: Clientes, Productos, Proveedores, Categorías, Impuestos, UnidadesMedida

================================================================================
9. MÓDULOS PENDIENTES
================================================================================

Logística (último archivo del Bloque 5):
- Pages/Logistica/Reportes/Index — KPIs por repartidor, zona, tasa de éxito, tiempos promedio
  - KPIs generales: total envíos, entregados, fallidos, tasa de éxito, tiempo promedio de entrega
  - Filtros: rango de fechas, zona, repartidor
  - Gráficos con Chart.js:
    - Envíos por zona (bar)
    - Tasa de éxito por repartidor (bar horizontal o doughnut)
    - Tendencia de entregas por día/semana (line)
  - Tabla detallada por repartidor: entregas, fallos, tasa de éxito, tiempo promedio
  - Tabla detallada por zona: total, entregas, fallos, monto facturado
- Program.cs — Corregir `AllowAnonymousToPage` de Tracking (ver sección 10)
- Actualizar PROYECTO.md con `.\generar-snapshot.ps1`
- git commit + push

Fase 6 (futuro):
- Producción (Órdenes, Calidad)
- Activos (Inventario, Mantenimiento)
- Seguridad (Index, Auditoría)
- Presupuestos por categoría
- Portal del empleado
- Evaluaciones de desempeño
- Capacitaciones / Cursos
- Reclutamiento / Vacantes

================================================================================
10. NOTAS IMPORTANTES
================================================================================

⚠️ **PENDIENTE EN Program.cs (crítico para Tracking):**
- Actualmente tiene: `options.Conventions.AllowAnonymousToPage("/Tracking/Index");`
- Debe cambiar a: `options.Conventions.AllowAnonymousToPage("/Logistica/Tracking/Index");`
- Sin este cambio, la página de Tracking público va a requerir login y redirigir a /Auth/Login

Sobre migraciones:
- ApplicationDbContextModelSnapshot está DESINCRONIZADO con la BD
- Al generar migración, EF intenta crear TODAS las tablas
- Solución actual: SQL manual + INSERT en __EFMigrationsHistory
- Logística (Envios, Rutas, Zonas, Repartidores) — verificar si las tablas existen en la BD; si no, crear por SQL manual

Sobre OneDrive:
- Proyecto en C:\Users\crobe\OneDrive\Kirkenta
- Archivos son symlinks de OneDrive
- Recomendado mover a C:\Dev\Kirkenta

Sobre .gitignore:
- Excluye: bin/, obj/, .vs/, .vscode/, *.lnk, appsettings.Development.json
- Excluye carpetas del proyecto PHP viejo: app/, config/, public/, storage/, vendor/, includes/, modules/, backups/, logs/, assets/, uploads/

Sobre appsettings.json (NO SUBIR A GIT):
- ConnectionStrings.DefaultConnection con password real
- Versionar solo appsettings.json con placeholder

Sobre ConfiguracionEmpresa (campos RRHH):
- PaisCodigo (string, "HN")
- ZonaHoraria (string, "America/Tegucigalpa")
- RHFrecuenciaPagoDefault (string, "Mensual")
- RHDiaPago1, RHDiaPago2, RHDiaSemanalPago (int)
- RHAplicaIHSS, RHPorcentajeIHSS, RHTopeIHSS
- RHAplicaRAP, RHPorcentajeRAP
- RHAplicaISR, RHMetodoISRDefault
- RHTablaVacaciones (string, "1:10,2:12,3:15,4:20,5:20")
- RHAntiguedadMaxTabla (int, 5)
- RHCargarFeriadosAuto (bool)
- RHAlertaFeriadosProximos/Dias, RHAlertaVacacionesProximas/Dias,
  RHAlertaCumpleanios, RHAlertaAniversarios, RHAlertaValesPorVencer,
  RHAlertaContratosPorVencer/Dias, RHAlertaDocumentosVencidos

Sobre ConfiguracionDeduccion (RRHH nómina):
- Anio, AplicaIHSS, PorcentajeIHSS, TopeIHSS
- AplicaRAP, PorcentajeRAP, TopeRAP
- AplicaISR, TopeAnualExentoISR, MetodoISR ("Acumulativo" | "MensualSimple")
- Se crea automáticamente con valores por defecto de Honduras 2024
- TramosISR asociados por ConfiguracionDeduccionId

Sobre tipos de nómina:
- Semanal → 7 días, Catorcenal → 14, Quincenal → 15, Mensual → 30
- Empleados filtrados por FrecuenciaPago

Sobre el Plan de Cuentas:
- Seed inicial: 81 cuentas estándar de Honduras
- Jerarquía por código con puntos
- Tipos: Activo, Pasivo, Patrimonio, Ingreso, Costo, Gasto
- Naturaleza calculada automáticamente
- Solo EsMovimiento=true acepta movimientos directos

Sobre __EFMigrationsHistory:
- Snapshot desincronizado → no usar dotnet ef migrations
- Agregar migraciones manualmente por SQL

Sobre puerto:
- Solo se usa http://localhost:5114
- NO se usa HTTPS ni el puerto 7060

Sobre el wizard de importación (04/10/2026):
- Refactorizado a componente genérico: ImportWizardHelper + vistas parciales
- Clientes, Productos, Proveedores refactorizados
- Categorías, Impuestos, UnidadesMedida agregados con el mismo patrón
- 6 módulos en total usan el wizard genérico

Sobre el bug EF Core 9 (04/10/2026):
- `string[]` + `.Contains()` en queries EF rompe en .NET 10
- Error: TypeLoadException: GenericArguments[1], 'System.ReadOnlySpan`1[System.String]'
- Solución: usar HashSet<string> o List<string>

Sobre el snapshot automático:
- Script: generar-snapshot.ps1 en la raíz
- Genera: PROYECTO.md (todos los .cs/.cshtml/.json/.css/.js/.md/.sql)
- Excluye: bin/, obj/, .vs/, .vscode/, uploads/, node_modules/, .git/
- Ejecutar: `.\generar-snapshot.ps1` en PowerShell

Sobre Logística:
- Modulo activo en desarrollo. Bloques 1-4 completados + Tracking del Bloque 5.
- 5 modelos: Envio, Ruta, RutaHistorial, Repartidor, ZonaEnvio
- 4 helpers: EnvioHelper, RutaHelper, ZonaEnvioSeeder, AlertaEnvioHelper
- Estados de Envio: Pendiente → EnRuta → Entregado (o Fallido/Cancelado)
- Estados de Ruta: Borrador → EnReparto → Completada (o Cancelada)
- Al reagendar un envío fallido, vuelve a Pendiente sin ruta
- La ruta se cierra automáticamente cuando todas las paradas están resueltas
- Las alertas NO se persisten: se calculan en tiempo real con AlertaEnvioHelper
- Los KPIs del Index de Envios son clickeables y aplican filtros via querystring
- Los KPIs del Index de Rutas son clickeables y aplican filtros via querystring
- Los KPIs del Index de Zonas no son clickeables (solo informativos)
- Los KPIs del Index de Repartidores no son clickeables excepto "Rutas activas" (linkea a Rutas con filtro)
- El POS ya crea envíos automáticamente al cobrar con RequiereEnvio=true
- El tracking code se genera con formato KRT-2026-AB12X9 (único por ruta)
- Los modales usan la clase `is-open` (no display: inline), con función JS cerrarModal(id)
- El _Layout.cshtml tiene el submenú Logística completo con separadores visuales
- Las Zonas no se eliminan si tienen envíos asociados (se sugiere desactivar)
- Los Repartidores no se eliminan si tienen rutas o envíos (se sugiere desactivar)
- El catálogo de Zonas tiene 5 zonas típicas de Honduras por seeder (Centro, Cercana, Media, Lejana, Gratis)
- El Tracking público tiene su propio diseño (Layout=null) y NO usa _Layout.cshtml
- El Tracking público está exento de autenticación con AllowAnonymousToPage (verificar Program.cs)

================================================================================
11. FLUJO DE TRABAJO
================================================================================

Al iniciar chat nuevo:
1. Pega este archivo completo
2. Di: "Retomamos Kirkenta ERP"
3. Dime qué módulo vamos a trabajar
4. Si vamos a tocar un archivo específico, pásalo (solo ese)

Al terminar sesión:
1. Actualizar este archivo si hubo cambios importantes
2. Actualizar PROYECTO.md (ejecutar .\generar-snapshot.ps1)
3. Guardar
4. git add . && git commit -m "..." && git push

Al pedir código:
- Archivos COMPLETOS, no fragmentos (regla #1)
- Nombre del archivo + ruta
- Un archivo por mensaje (o agrupados si son cortos)

================================================================================
ÚLTIMA ACTUALIZACIÓN
================================================================================

Fecha: 04/10/2026 (sesión 5 — Bloque 5 Tracking completado)

Estado:
- Módulos operativos: Usuarios, Ventas, POS, Inventario, Compras, Finanzas,
  RRHH, Reportes, Configuración.
- Finanzas 100% (incluye Fase 5: Contabilidad completa).
- RRHH 100%.
- **Logística EN DESARROLLO — 4.5/5 bloques completados:**
  - ✅ Bloque 1: AlertaEnvioHelper + Logistica/Index (dashboard con alertas)
  - ✅ Bloque 2: Logistica/Envios/Index + Logistica/Envios/Details
  - ✅ Bloque 3: Logistica/Rutas/Index + Create + Details + Edit
  - ✅ Bloque 4: Logistica/Zonas (Index/Create/Edit/Delete) + Logistica/Repartidores (Index/Create/Edit/Delete)
  - ✅ Bloque 4: _Layout.cshtml actualizado con submenú Logística completo
  - ✅ Bloque 5 (parcial): Logistica/Tracking/Index (público, AllowAnonymous)
  - ⬅ Bloque 5 PENDIENTE: Logistica/Reportes/Index (último archivo)

Acciones críticas pendientes:
1. **⚠️ ACTUALIZAR Program.cs**: cambiar `AllowAnonymousToPage("/Tracking/Index")` 
   por `AllowAnonymousToPage("/Logistica/Tracking/Index")`
   Sin esto, el tracking público va a requerir login.
2. Crear Logistica/Reportes/Index + .cs
3. Actualizar PROYECTO.md con `.\generar-snapshot.ps1`
4. git commit + push

Fase 6 (futuro):
- Producción, Activos, Seguridad, Presupuestos, Portal del empleado,
  Evaluaciones, Capacitaciones, Reclutamiento
````

===== FILE: generar-snapshot1.ps1 =====

````powershell
# ============================================
# GENERADOR DE SNAPSHOT PARA KIRKENTA ERP
# Genera PROYECTO.md con TODO el codigo fuente
# ============================================

$raiz = $PSScriptRoot
$output = Join-Path $raiz "PROYECTO.md"

# Extensiones a incluir
$extensiones = @("*.cs", "*.cshtml", "*.csproj", "*.json", "*.css", "*.js", "*.md", "*.sql")
# Extensiones a EXCLUIR (archivos generados)
$excluir = @("*.Designer.cs", "*.AssemblyInfo.cs", "*.g.cs")

# Carpetas a EXCLUIR
$carpetasExcluir = @(
    "bin", "obj", ".vs", ".vscode", ".idea",
    "node_modules", ".git",
    "uploads"
)

function DebeExcluirCarpeta {
    param($ruta)
    foreach ($c in $carpetasExcluir) {
        if ($ruta -match "\\$c\\" -or $ruta -match "\\$c$") { return $true }
    }
    return $false
}

function DebeExcluirArchivo {
    param($nombre)
    foreach ($e in $excluir) {
        if ($nombre -like $e) { return $true }
    }
    return $false
}

# Iniciar el archivo
$sb = [System.Text.StringBuilder]::new()

[void]$sb.AppendLine("# KIRKENTA ERP - SNAPSHOT COMPLETO DEL PROYECTO")
[void]$sb.AppendLine()
[void]$sb.AppendLine("**Fecha de generacion:** $(Get-Date -Format 'dd/MM/yyyy HH:mm')")
[void]$sb.AppendLine("**Raiz del proyecto:** $raiz")
[void]$sb.AppendLine()
[void]$sb.AppendLine("---")
[void]$sb.AppendLine()

# Contar archivos primero
$todosLosArchivos = @()
foreach ($ext in $extensiones) {
    $encontrados = Get-ChildItem -Path $raiz -Filter $ext -Recurse -File |
        Where-Object {
            -not (DebeExcluirCarpeta $_.FullName) -and
            -not (DebeExcluirArchivo $_.Name)
        }
    $todosLosArchivos += $encontrados
}

$todosLosArchivos = $todosLosArchivos | Sort-Object FullName -Unique
$total = $todosLosArchivos.Count

[void]$sb.AppendLine("**Total archivos:** $total")
[void]$sb.AppendLine()

# Agrupar por carpeta
$agrupados = $todosLosArchivos | Group-Object { 
    $rel = $_.FullName.Replace("$raiz\", "")
    $dir = Split-Path $rel -Parent
    if ([string]::IsNullOrEmpty($dir)) { return "RAIZ" }
    return $dir
} | Sort-Object Name

foreach ($grupo in $agrupados) {
    $nombreGrupo = $grupo.Name
    $archivosGrupo = $grupo.Group | Sort-Object Name
    
    [void]$sb.AppendLine("====================================================")
    [void]$sb.AppendLine(" $nombreGrupo - $($archivosGrupo.Count) archivo(s)")
    [void]$sb.AppendLine("====================================================")
    [void]$sb.AppendLine()

    foreach ($archivo in $archivosGrupo) {
        $rel = $archivo.FullName.Replace("$raiz\", "").Replace("\", "/")
        
        [void]$sb.AppendLine("===== FILE: $rel =====")
        [void]$sb.AppendLine()
        
        # Detectar extension para el bloque de codigo
        $ext = $archivo.Extension.TrimStart('.').ToLower()
        $lenguaje = switch ($ext) {
            "cs" { "csharp" }
            "cshtml" { "html" }
            "csproj" { "xml" }
            "json" { "json" }
            "css" { "css" }
            "js" { "javascript" }
            "md" { "markdown" }
            "sql" { "sql" }
            default { "" }
        }
        
        [void]$sb.AppendLine("````$lenguaje")
        
        try {
            $contenido = Get-Content -Path $archivo.FullName -Raw -Encoding UTF8
            if ($null -eq $contenido) { $contenido = "" }
            [void]$sb.AppendLine($contenido)
        } catch {
            [void]$sb.AppendLine("// ERROR AL LEER: $($_.Exception.Message)")
        }
        
        [void]$sb.AppendLine("````")
        [void]$sb.AppendLine()
    }
}

# Escribir el archivo
$sb.ToString() | Out-File -FilePath $output -Encoding UTF8

$tamanoMB = [math]::Round((Get-Item $output).Length / 1MB, 2)
Write-Host "OK - Snapshot generado: $output" -ForegroundColor Green
Write-Host "Total archivos: $total" -ForegroundColor Cyan
Write-Host "Tamano: $tamanoMB MB" -ForegroundColor Cyan
````

===== FILE: Kirkenta.csproj =====

````xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.2.0" />
    <PackageReference Include="ClosedXML" Version="0.105.1" />
    <PackageReference Include="CsvHelper" Version="33.1.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="9.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="9.0.0" />
  </ItemGroup>

</Project>

````

===== FILE: Menu.txt =====

````plaintext
Dashboard        → Vista general
Usuarios         → Lista, Crear, Roles, Mi perfil, Cambiar password, Actividad
Ventas           → Pipeline, Nueva venta, Cotizaciones, Pedidos, Facturas,
                   Notas de crédito, Clientes, Nuevo cliente, Descuentos, Comisiones
Inventario       → Stock, Productos, Nuevo producto, Categorías, Unidades,
                   Entradas, Salidas, Transferencias, Ajustes, Bodegas,
                   Alertas, Kardex
Compras          → Órdenes, Nueva orden, Cotizaciones, Proveedores,
                   Nuevo proveedor, Recepciones, Pagos, Devoluciones
Finanzas         → Contabilidad, Ingresos, Egresos, Cuentas, Conciliación,
                   Facturación, Por cobrar, Por pagar, Impuestos, Presupuestos,
                   Reportes, Balance, Resultados, Flujo de caja
Producción       → Órdenes, Nueva orden, Planificación, Calidad, Materias,
                   Recetas/BOM, Máquinas, Mantenimiento, Costos, Mermas
RRHH             → Empleados, Nuevo empleado, Departamentos, Puestos, Contratos,
                   Nómina, Recibos, Asistencia, Vacaciones, Permisos,
                   Evaluaciones, Capacitaciones, Reclutamiento
Reportes         → KPIs, Ventas, Inventario, Compras, Financieros, RRHH,
                   Personalizados, Exportar, Programados
Logística        → Rutas, Envíos, Nuevo envío, Transportistas, Vehículos,
                   Conductores, Seguimiento, Entregas, Devoluciones, Costos
Activos          → Inventario, Nuevo activo, Categorías, Ubicaciones,
                   Responsables, Mantenimiento, Depreciación, Bajas, Etiquetas
Seguridad        → Usuarios y roles, Permisos, Auditoría, Logs, Sesiones,
                   Intentos fallidos, Respaldos, Restaurar, Políticas
Configuración    → General, Empresa, Sucursales, Monedas, Impuestos,
                   Métodos de pago, Notificaciones, Integraciones,
                   Plantillas, Folios, Idiomas, Apariencia
````

===== FILE: Program.cs =====

````csharp
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Helpers.Logistica;
using Kirkenta.Helpers.RRHH;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 🔌 Base de datos MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 📄 Razor Pages + Reglas de autorización
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Auth/Login");
    options.Conventions.AllowAnonymousToPage("/Auth/Register");
    options.Conventions.AllowAnonymousToPage("/Auth/Logout");
    options.Conventions.AllowAnonymousToPage("/Logistica/Tracking/Index");
});

// 🔐 Autenticación por cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "KirkentaAuth";
    });

builder.Services.AddAuthorization();

// 📦 Sesión
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "KirkentaSession";
});

// 🌐 Acceso a HttpContext
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 🔄 SEEDERS AL ARRANCAR
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Migrar permisos faltantes
        PermisoSeeder.MigrarPermisosFaltantes(db);

        // Seed de Finanzas (cajas y categorías)
        FinanzasSeeder.Seed(db);

        // Seed del Plan de Cuentas contable
        PlanCuentasHelper.Seed(db);

        // Seed de RRHH — Tipos de documentos
        TipoDocumentoEmpleadoSeeder.Seed(db);

        // Seed de Feriados (año actual)
        FeriadoSeeder.Seed(db, DateTime.Today.Year);

        // Seed de Logística — Zonas de envío
        ZonaEnvioSeeder.Seed(db);

        // Acumulación de vacaciones anuales
        var procesados = EmpleadoHelper.ProcesarAcumulacionGlobal(db);
        if (procesados > 0)
        {
            Console.WriteLine($"[EmpleadoHelper] Se procesaron {procesados} empleados para acumulación de vacaciones");
        }

        // Actualizar vigencias de documentos
        AdjuntoEmpleadoHelper.ActualizarVigencias(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup] Error en seeders: {ex.Message}");
    }
}

// 🌐 Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
````

====================================================
 wwwroot\css - 5 archivo(s)
====================================================

===== FILE: wwwroot/css/dashboard.css =====

````css
.dashboard-container {
    display: flex;
    flex-wrap: wrap;
    gap: 20px;
    padding: 30px;
}

.dashboard-card {
    flex: 1 1 250px;
    background: #fff;
    border-radius: var(--border-radius);
    box-shadow: var(--card-shadow);
    padding: 20px;
    text-align: center;
    transition: transform 0.2s ease;
}

.dashboard-card:hover {
    transform: translateY(-5px);
}

.dashboard-card h3 {
    color: var(--color-secondary);
    margin-bottom: 10px;
}

````

===== FILE: wwwroot/css/login.css =====

````css
.login-container {
    background: url('../images/erp-background.jpg') no-repeat center center fixed;
    background-size: cover;
    height: 100vh;
    display: flex;
    justify-content: center;
    align-items: center;
}

.login-card {
    width: 340px;
    text-align: center;
}

.logo img {
    width: 70px;
    margin-bottom: 10px;
}

.logo h2 {
    margin-bottom: 20px;
    font-weight: 700;
    color: var(--color-secondary);
}

````

===== FILE: wwwroot/css/register.css =====

````css
.register-container {
    background-color: var(--color-bg);
    min-height: 100vh;
    display: flex;
    justify-content: center;
    align-items: center;
}

.register-card {
    width: 400px;
    text-align: left;
}

.register-card h2 {
    margin-bottom: 20px;
    color: var(--color-secondary);
}

.register-card input {
    margin-bottom: 15px;
}

````

===== FILE: wwwroot/css/site.css =====

````css
/* Fondo corporativo */
.login-container {
    background: url('../images/erp-background.jpg') no-repeat center center fixed;
    background-size: cover;
    height: 100vh;
    display: flex;
    justify-content: center;
    align-items: center;
}

/* Tarjeta de login */
.login-card {
    background: rgba(255, 255, 255, 0.95);
    padding: 40px;
    border-radius: 12px;
    box-shadow: 0 6px 18px rgba(0,0,0,0.2);
    width: 360px;
    text-align: center;
}

/* Logo */
.logo img {
    width: 80px;
    margin-bottom: 10px;
}

.logo h2 {
    margin-bottom: 25px;
    font-weight: 700;
    color: #2c3e50;
}

/* Inputs */
.form-group {
    margin-bottom: 20px;
    text-align: left;
}

input {
    width: 100%;
    padding: 12px;
    border-radius: 6px;
    border: 1px solid #ddd;
    font-size: 14px;
}

input:focus {
    border-color: #1abc9c;
    outline: none;
    box-shadow: 0 0 4px rgba(26, 188, 156, 0.4);
}

/* Botón */
.btn-login {
    width: 100%;
    padding: 12px;
    background-color: #1abc9c;
    color: #fff;
    font-weight: 600;
    border: none;
    border-radius: 6px;
    cursor: pointer;
}

.btn-login:hover {
    background-color: #16a085;
}

/* Link de registro */
.register-link {
    margin-top: 15px;
    font-size: 14px;
}
.register-link a {
    color: #1abc9c;
    text-decoration: none;
}
.register-link a:hover {
    text-decoration: underline;
}

````

===== FILE: wwwroot/css/theme.css =====

````css
/* ============================================
   KIRKENTA ERP — SISTEMA DE DISEÑO MINIMALISTA
   ============================================ */

:root {
    --color-primary:       #4f46e5;
    --color-primary-hover: #4338ca;
    --color-primary-soft:  #eef2ff;
    --color-bg:            #fafafa;
    --color-surface:       #ffffff;
    --color-text:          #111827;
    --color-muted:         #6b7280;
    --color-border:        #e5e7eb;
    --color-success:       #10b981;
    --color-danger:        #ef4444;
    --color-danger-bg:     #fef2f2;
    --color-danger-border: #fecaca;
    --color-warning-bg:    #fffbeb;
    --color-warning-border:#fde68a;
    --color-warning:       #d97706;

    --shadow-xs: 0 1px 2px rgba(0,0,0,0.04);
    --shadow-sm: 0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04);
    --shadow-md: 0 4px 6px rgba(0,0,0,0.05), 0 2px 4px rgba(0,0,0,0.03);
    --shadow-lg: 0 10px 20px rgba(0,0,0,0.08), 0 4px 8px rgba(0,0,0,0.04);

    --radius-sm: 6px;
    --radius-md: 10px;
    --radius-lg: 14px;

    --font-sans: 'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif;

    --sidebar-width: 260px;
    --sidebar-collapsed: 68px;
    --topbar-height: 60px;
}

/* ===== Reset ===== */
*, *::before, *::after { box-sizing: border-box; }
* { margin: 0; padding: 0; }

html, body { height: 100%; }

body {
    font-family: var(--font-sans);
    background: var(--color-bg);
    color: var(--color-text);
    font-size: 14px;
    line-height: 1.55;
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
}

a { color: var(--color-primary); text-decoration: none; }
a:hover { text-decoration: none; }

ul { list-style: none; }

/* ============================================
   LAYOUT
   ============================================ */
.app-body {
    display: block;
    min-height: 100vh;
}

/* ============================================
   SIDEBAR
   ============================================ */
.erp-sidebar {
    position: fixed;
    top: 0;
    left: 0;
    width: var(--sidebar-collapsed);
    height: 100vh;
    background: var(--color-surface);
    border-right: 1px solid var(--color-border);
    display: flex;
    flex-direction: column;
    z-index: 1000;
    transition: width 0.25s ease;
    overflow: hidden;
}

.erp-sidebar:hover,
.erp-sidebar.is-open {
    width: var(--sidebar-width);
    box-shadow: 4px 0 20px rgba(0,0,0,0.06);
}

.erp-sidebar-header {
    display: flex;
    align-items: center;
    height: var(--topbar-height);
    padding: 0 14px;
    border-bottom: 1px solid var(--color-border);
    flex-shrink: 0;
}

.erp-sidebar-logo {
    display: flex;
    align-items: center;
    gap: 10px;
    text-decoration: none;
    color: var(--color-text);
    font-weight: 700;
    font-size: 15px;
    white-space: nowrap;
    overflow: hidden;
}

.erp-logo-img {
    width: 38px;
    height: 38px;
    object-fit: contain;
    flex-shrink: 0;
    border-radius: 8px;
    background: #ffffff;
    padding: 2px;
    transition: transform 0.2s ease;
}

.erp-sidebar:hover .erp-logo-img,
.erp-sidebar.is-open .erp-logo-img {
    transform: scale(1.05);
}

.erp-logo-text {
    letter-spacing: -0.01em;
    opacity: 0;
    transition: opacity 0.2s ease;
}

.erp-sidebar:hover .erp-logo-text,
.erp-sidebar.is-open .erp-logo-text { opacity: 1; }

.erp-sidebar-nav {
    flex: 1;
    overflow-y: auto;
    overflow-x: hidden;
    padding: 12px 8px;
}

.erp-sidebar-nav::-webkit-scrollbar { width: 4px; }
.erp-sidebar-nav::-webkit-scrollbar-thumb {
    background: var(--color-border);
    border-radius: 4px;
}

.erp-nav-group { margin: 0; }

.erp-nav-item {
    display: flex;
    align-items: center;
    gap: 12px;
    width: 100%;
    padding: 10px 12px;
    margin-bottom: 2px;
    border: none;
    background: transparent;
    border-radius: var(--radius-sm);
    color: var(--color-muted);
    font-family: inherit;
    font-size: 13.5px;
    font-weight: 500;
    text-decoration: none;
    cursor: pointer;
    text-align: left;
    white-space: nowrap;
    transition: background 0.15s ease, color 0.15s ease;
    position: relative;
}

.erp-nav-item:hover {
    background: #f3f4f6;
    color: var(--color-text);
    text-decoration: none;
}

.erp-nav-item.is-active,
.erp-nav-item.is-expanded {
    background: var(--color-primary-soft);
    color: var(--color-primary);
}

.erp-nav-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 24px;
    height: 24px;
    min-width: 24px;
    flex-shrink: 0;
}

.erp-nav-icon svg {
    width: 18px !important;
    height: 18px !important;
    max-width: 18px;
    max-height: 18px;
    display: block;
}

.erp-nav-text {
    flex: 1;
    opacity: 0;
    transition: opacity 0.2s ease;
    overflow: hidden;
    text-overflow: ellipsis;
}

.erp-sidebar:hover .erp-nav-text,
.erp-sidebar.is-open .erp-nav-text { opacity: 1; }

.erp-nav-arrow {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 16px;
    height: 16px;
    flex-shrink: 0;
    opacity: 0;
    transition: opacity 0.2s ease, transform 0.25s ease;
}

.erp-nav-arrow svg {
    width: 14px !important;
    height: 14px !important;
    display: block;
}

.erp-sidebar:hover .erp-nav-arrow,
.erp-sidebar.is-open .erp-nav-arrow { opacity: 0.6; }

.erp-nav-toggle.is-expanded .erp-nav-arrow { transform: rotate(180deg); }

.erp-nav-submenu {
    display: none;
    padding: 4px 0 8px 0;
}

.erp-nav-submenu.is-open { display: block; }

.erp-nav-submenu a {
    display: block;
    padding: 7px 12px 7px 48px;
    font-size: 13px;
    font-weight: 500;
    color: var(--color-muted);
    border-radius: var(--radius-sm);
    text-decoration: none;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    transition: background 0.15s ease, color 0.15s ease, padding-left 0.15s ease;
}

.erp-nav-submenu a:hover {
    background: #f3f4f6;
    color: var(--color-text);
    padding-left: 52px;
    text-decoration: none;
}

.erp-sidebar-footer {
    border-top: 1px solid var(--color-border);
    padding: 12px;
    flex-shrink: 0;
}

.erp-user {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 8px;
    border-radius: var(--radius-sm);
}

.erp-user-avatar {
    width: 32px;
    height: 32px;
    min-width: 32px;
    border-radius: 50%;
    background: var(--color-primary);
    color: #fff;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 600;
    font-size: 13px;
    flex-shrink: 0;
}

.erp-user-meta {
    display: flex;
    flex-direction: column;
    opacity: 0;
    transition: opacity 0.2s ease;
    overflow: hidden;
}

.erp-sidebar:hover .erp-user-meta,
.erp-sidebar.is-open .erp-user-meta { opacity: 1; }

.erp-user-name {
    font-size: 13px;
    font-weight: 600;
    color: var(--color-text);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.erp-user-role {
    font-size: 11px;
    color: var(--color-muted);
    white-space: nowrap;
}

/* ============================================
   MAIN WRAPPER
   ============================================ */
.erp-main-wrapper {
    margin-left: var(--sidebar-collapsed);
    display: flex;
    flex-direction: column;
    min-height: 100vh;
    transition: margin-left 0.25s ease;
}

.erp-topbar {
    position: sticky;
    top: 0;
    display: flex;
    align-items: center;
    gap: 16px;
    height: var(--topbar-height);
    padding: 0 24px;
    background: var(--color-surface);
    border-bottom: 1px solid var(--color-border);
    z-index: 900;
}

/* ===== Breadcrumb ===== */
.erp-breadcrumb {
    display: flex;
    align-items: center;
    gap: 8px;
    flex: 1;
    font-size: 13.5px;
    min-width: 0;
    overflow: hidden;
    line-height: 1;
}

.erp-breadcrumb-link {
    color: var(--color-muted);
    font-weight: 500;
    white-space: nowrap;
    transition: color 0.15s ease;
    text-decoration: none;
    line-height: 1;
}

.erp-breadcrumb-link:hover {
    color: var(--color-primary);
    text-decoration: none;
}

.erp-breadcrumb-sep {
    width: 14px !important;
    height: 14px !important;
    max-width: 14px !important;
    max-height: 14px !important;
    min-width: 14px !important;
    min-height: 14px !important;
    color: var(--color-muted);
    opacity: 0.4;
    flex-shrink: 0;
    display: block;
}

.erp-breadcrumb-current {
    color: var(--color-text);
    font-weight: 600;
    letter-spacing: -0.01em;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    line-height: 1;
}

.erp-topbar-right {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-shrink: 0;
}

.erp-logout-form { margin: 0; }

.erp-logout-btn {
    background: transparent;
    border: 1px solid var(--color-border);
    color: var(--color-muted);
    padding: 6px 14px;
    border-radius: var(--radius-sm);
    font-family: inherit;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.15s ease;
}

.erp-logout-btn:hover {
    background: var(--color-danger-bg);
    border-color: var(--color-danger-border);
    color: var(--color-danger);
}

.erp-main {
    flex: 1;
    padding: 32px 24px;
    max-width: 1500px;
    width: 100%;
}

.erp-footer {
    text-align: center;
    padding: 20px;
    color: var(--color-muted);
    font-size: 12.5px;
    border-top: 1px solid var(--color-border);
    background: var(--color-surface);
}

/* ===== Banner de pendiente ===== */
.pending-banner {
    display: flex;
    align-items: flex-start;
    gap: 14px;
    padding: 16px 20px;
    background: var(--color-warning-bg);
    border: 1px solid var(--color-warning-border);
    border-radius: var(--radius-md);
    margin-bottom: 24px;
    color: var(--color-warning);
}

.pending-banner svg {
    width: 22px !important;
    height: 22px !important;
    flex-shrink: 0;
    margin-top: 2px;
}

.pending-banner strong {
    display: block;
    font-size: 14px;
    font-weight: 700;
    margin-bottom: 2px;
}

.pending-banner p {
    font-size: 13px;
    color: #92400e;
    margin: 0;
}

/* ============================================
   DASHBOARD
   ============================================ */
.dashboard { display: flex; flex-direction: column; gap: 32px; }

.dashboard-header h1 {
    font-size: 24px;
    font-weight: 700;
    letter-spacing: -0.02em;
    margin-bottom: 4px;
}
.dashboard-header p {
    color: var(--color-muted);
    font-size: 14px;
}

.kpi-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: 16px;
}

.kpi-card {
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    padding: 20px;
    display: flex;
    flex-direction: column;
    gap: 4px;
    box-shadow: var(--shadow-xs);
    transition: box-shadow 0.2s ease, transform 0.2s ease;
}
.kpi-card:hover {
    box-shadow: var(--shadow-md);
    transform: translateY(-1px);
}

.kpi-label {
    font-size: 12.5px;
    color: var(--color-muted);
    font-weight: 500;
}

.kpi-value {
    font-size: 26px;
    font-weight: 700;
    letter-spacing: -0.02em;
    color: var(--color-text);
    margin-top: 2px;
}

.kpi-trend {
    font-size: 12px;
    font-weight: 500;
    margin-top: 4px;
}
.kpi-trend.up { color: var(--color-success); }
.kpi-trend.down { color: var(--color-danger); }

.section { display: flex; flex-direction: column; gap: 14px; }

.section-title {
    font-size: 15px;
    font-weight: 600;
    color: var(--color-text);
    letter-spacing: -0.01em;
}

.quick-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
    gap: 12px;
}

.quick-action {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 8px;
    padding: 22px 14px;
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    text-decoration: none;
    color: var(--color-text);
    font-weight: 500;
    font-size: 13px;
    text-align: center;
    box-shadow: var(--shadow-xs);
    transition: all 0.18s ease;
}
.quick-action:hover {
    border-color: var(--color-primary);
    color: var(--color-primary);
    box-shadow: var(--shadow-md);
    transform: translateY(-2px);
    text-decoration: none;
}

.quick-icon { font-size: 22px; line-height: 1; }

.panel-grid {
    display: grid;
    grid-template-columns: 2fr 1fr;
    gap: 16px;
}
@media (max-width: 900px) {
    .panel-grid { grid-template-columns: 1fr; }
}

.panel {
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    padding: 20px;
    box-shadow: var(--shadow-xs);
}

.panel-title {
    font-size: 14px;
    font-weight: 600;
    margin-bottom: 4px;
}
.panel-muted {
    font-size: 12.5px;
    color: var(--color-muted);
    margin-bottom: 14px;
}
.panel-placeholder {
    height: 180px;
    background: #f9fafb;
    border: 1px dashed var(--color-border);
    border-radius: var(--radius-sm);
}

/* ============================================
   AUTH (Login / Register)
   ============================================ */
.auth-body {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    background: radial-gradient(circle at top, #eef2ff 0%, #fafafa 55%);
    padding: 24px;
}

.auth-wrapper { width: 100%; max-width: 380px; }

.auth-card {
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-lg);
    box-shadow: var(--shadow-lg);
    padding: 32px 28px;
}

.auth-brand {
    text-align: center;
    margin-bottom: 24px;
}

.auth-logo {
    width: 130px;
    height: 130px;
    object-fit: contain;
    margin: 0 auto 12px auto;
    display: block;
}

.auth-title {
    font-size: 20px;
    font-weight: 700;
    letter-spacing: -0.02em;
    margin-bottom: 4px;
}

.auth-subtitle {
    font-size: 13px;
    color: var(--color-muted);
}

.auth-form {
    display: flex;
    flex-direction: column;
    gap: 14px;
}

.form-field {
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.form-field label {
    font-size: 12.5px;
    font-weight: 500;
    color: var(--color-text);
}

.form-input {
    width: 100%;
    padding: 10px 12px;
    font-size: 13.5px;
    font-family: inherit;
    color: var(--color-text);
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    outline: none;
    transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.form-input:focus {
    border-color: var(--color-primary);
    box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.12);
}

.form-input::placeholder { color: #9ca3af; }

.form-input-wrapper {
    position: relative;
    display: flex;
    align-items: center;
}

.form-input-wrapper .form-input {
    padding-right: 42px;
}

.password-toggle {
    position: absolute;
    right: 8px;
    top: 50%;
    transform: translateY(-50%);
    display: flex;
    align-items: center;
    justify-content: center;
    width: 30px;
    height: 30px;
    border: none;
    background: transparent;
    color: var(--color-muted);
    border-radius: var(--radius-sm);
    cursor: pointer;
    transition: background 0.15s ease, color 0.15s ease;
    padding: 0;
}

.password-toggle:hover {
    background: #f3f4f6;
    color: var(--color-text);
}

.password-toggle svg {
    width: 16px !important;
    height: 16px !important;
    display: block;
}

.auth-alert {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 12px;
    border-radius: var(--radius-sm);
    font-size: 13px;
    font-weight: 500;
    margin-bottom: 16px;
    line-height: 1.4;
}

.auth-alert-error {
    background: var(--color-danger-bg);
    border: 1px solid var(--color-danger-border);
    color: var(--color-danger);
}

.auth-alert-icon {
    width: 18px !important;
    height: 18px !important;
    flex-shrink: 0;
}

.auth-alert ul { list-style: none; padding: 0; margin: 0; }
.auth-alert li { padding: 0; }
.auth-alert span { display: block; }

.btn-auth {
    margin-top: 6px;
    padding: 10px 14px;
    background: var(--color-primary);
    color: #fff;
    font-family: inherit;
    font-size: 13.5px;
    font-weight: 600;
    border: none;
    border-radius: var(--radius-sm);
    cursor: pointer;
    transition: background 0.15s ease, transform 0.1s ease;
}

.btn-auth:hover { background: var(--color-primary-hover); }
.btn-auth:active { transform: scale(0.99); }

.auth-footer {
    text-align: center;
    margin-top: 20px;
    font-size: 13px;
    color: var(--color-muted);
}
.auth-footer a {
    font-weight: 600;
    color: var(--color-primary);
}

/* ============================================
   MÓDULOS — GENERAL
   ============================================ */
.module-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 16px;
    margin-bottom: 24px;
    flex-wrap: wrap;
}

.module-header-left h2 {
    font-size: 22px;
    font-weight: 700;
    letter-spacing: -0.02em;
    margin-bottom: 4px;
}

.module-header-left p {
    font-size: 13.5px;
    color: var(--color-muted);
}

.module-header-right {
    display: flex;
    gap: 10px;
    flex-shrink: 0;
    flex-wrap: wrap;
}

.back-link {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-size: 13px;
    font-weight: 500;
    color: var(--color-muted);
    margin-bottom: 12px;
    transition: color 0.15s ease;
}

.back-link:hover { color: var(--color-primary); }

.back-link svg {
    width: 14px !important;
    height: 14px !important;
}

.btn-primary,
.btn-secondary,
.btn-danger {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 9px 16px;
    border-radius: var(--radius-sm);
    font-family: inherit;
    font-size: 13px;
    font-weight: 600;
    text-decoration: none;
    border: 1px solid transparent;
    cursor: pointer;
    transition: all 0.15s ease;
    white-space: nowrap;
}

.btn-primary {
    background: var(--color-primary);
    color: #fff;
}
.btn-primary:hover {
    background: var(--color-primary-hover);
    text-decoration: none;
    color: #fff;
}

.btn-secondary {
    background: var(--color-surface);
    color: var(--color-text);
    border-color: var(--color-border);
}
.btn-secondary:hover {
    background: #f3f4f6;
    text-decoration: none;
    color: var(--color-text);
}

.btn-danger {
    background: var(--color-danger);
    color: #fff;
}
.btn-danger:hover {
    background: #dc2626;
    text-decoration: none;
    color: #fff;
}

.btn-sm {
    padding: 6px 12px;
    font-size: 12.5px;
}

.btn-icon {
    width: 16px !important;
    height: 16px !important;
    display: block;
}

.module-card {
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    box-shadow: var(--shadow-xs);
    overflow: hidden;
}

.form-card {
    padding: 28px;
    max-width: 720px;
}

.form-card-wide {
    padding: 28px;
    max-width: 1100px;
}

.module-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 16px;
    padding: 16px 20px;
    border-bottom: 1px solid var(--color-border);
    flex-wrap: wrap;
}

.search-box {
    position: relative;
    flex: 1;
    min-width: 240px;
    max-width: 400px;
}

.search-box svg {
    position: absolute;
    left: 12px;
    top: 50%;
    transform: translateY(-50%);
    width: 16px !important;
    height: 16px !important;
    color: var(--color-muted);
    pointer-events: none;
}

.search-box input {
    width: 100%;
    padding: 9px 12px 9px 38px;
    font-family: inherit;
    font-size: 13px;
    color: var(--color-text);
    background: var(--color-bg);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    outline: none;
    transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.search-box input:focus {
    border-color: var(--color-primary);
    background: var(--color-surface);
    box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.1);
}

.filter-group {
    display: flex;
    gap: 8px;
}

.filter-select {
    padding: 9px 12px;
    font-family: inherit;
    font-size: 13px;
    color: var(--color-text);
    background: var(--color-bg);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    outline: none;
    cursor: pointer;
    transition: border-color 0.15s ease;
}

.filter-select:focus {
    border-color: var(--color-primary);
}

.table-wrapper {
    overflow-x: auto;
}

.erp-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 13.5px;
}

.erp-table thead {
    background: #f9fafb;
    border-bottom: 1px solid var(--color-border);
}

.erp-table th {
    text-align: left;
    padding: 12px 20px;
    font-size: 11.5px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--color-muted);
    white-space: nowrap;
}

.erp-table tbody tr {
    border-bottom: 1px solid var(--color-border);
    transition: background 0.15s ease;
}

.erp-table tbody tr:hover {
    background: #f9fafb;
}

.erp-table tbody tr:last-child {
    border-bottom: none;
}

.erp-table td {
    padding: 14px 20px;
    vertical-align: middle;
    color: var(--color-text);
}

.user-avatar-sm {
    width: 34px;
    height: 34px;
    border-radius: 50%;
    background: var(--color-primary);
    color: #fff;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 600;
    font-size: 13px;
    flex-shrink: 0;
}

.table-user-cell {
    display: flex;
    flex-direction: column;
    gap: 2px;
}

.table-user-name {
    font-weight: 600;
    color: var(--color-text);
}

.table-user-sub {
    font-size: 12px;
    color: var(--color-muted);
}

.badge {
    display: inline-flex;
    align-items: center;
    padding: 3px 10px;
    border-radius: 999px;
    font-size: 11.5px;
    font-weight: 600;
    white-space: nowrap;
    line-height: 1.6;
}

.badge-success {
    background: #d1fae5;
    color: #065f46;
}

.badge-danger {
    background: #fee2e2;
    color: #991b1b;
}

.badge-info {
    background: #dbeafe;
    color: #1e40af;
}

.badge-warning {
    background: #fef3c7;
    color: #92400e;
}

.table-actions {
    display: flex;
    gap: 4px;
    justify-content: flex-end;
    align-items: center;
}

.btn-icon-action {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 32px;
    height: 32px;
    border-radius: var(--radius-sm);
    color: var(--color-muted);
    background: transparent;
    border: 1px solid transparent;
    cursor: pointer;
    transition: all 0.15s ease;
    text-decoration: none;
}

.btn-icon-action:hover {
    background: #f3f4f6;
    color: var(--color-text);
    text-decoration: none;
}

.btn-icon-action-danger:hover {
    background: #fef2f2;
    color: var(--color-danger);
}

.btn-icon-action svg {
    width: 15px !important;
    height: 15px !important;
    display: block;
}

.module-alert {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 12px 16px;
    border-radius: var(--radius-sm);
    font-size: 13.5px;
    font-weight: 500;
    margin: 16px 20px;
}

.module-alert svg {
    width: 18px !important;
    height: 18px !important;
    flex-shrink: 0;
}

.module-alert-success {
    background: #ecfdf5;
    border: 1px solid #a7f3d0;
    color: #065f46;
}

.module-alert-error {
    background: #fef2f2;
    border: 1px solid #fecaca;
    color: var(--color-danger);
}

.module-alert ul { list-style: none; padding: 0; margin: 0; }

.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 60px 20px;
    text-align: center;
    gap: 8px;
}

.empty-state svg {
    width: 48px !important;
    height: 48px !important;
    color: var(--color-muted);
    opacity: 0.4;
    margin-bottom: 8px;
}

.empty-state h3 {
    font-size: 16px;
    font-weight: 600;
    color: var(--color-text);
}

.empty-state p {
    color: var(--color-muted);
    font-size: 13.5px;
    margin-bottom: 16px;
}

.erp-form {
    display: flex;
    flex-direction: column;
    gap: 18px;
}

.form-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 18px;
}

@media (max-width: 640px) {
    .form-row { grid-template-columns: 1fr; }
}

.form-section-title {
    font-size: 14px;
    font-weight: 600;
    color: var(--color-text);
    margin-bottom: 2px;
}

.form-section-desc {
    font-size: 12.5px;
    color: var(--color-muted);
    margin-bottom: 8px;
}

.form-divider {
    height: 1px;
    background: var(--color-border);
    margin: 8px 0;
}

.form-actions {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
    padding-top: 12px;
    border-top: 1px solid var(--color-border);
    margin-top: 8px;
}

.form-checkbox {
    display: flex;
    align-items: center;
    gap: 8px;
    cursor: pointer;
    font-size: 13.5px;
    font-weight: 500;
    color: var(--color-text);
}

.form-checkbox input[type="checkbox"] {
    width: 16px;
    height: 16px;
    accent-color: var(--color-primary);
    cursor: pointer;
}

.form-help {
    font-size: 12px;
    color: var(--color-muted);
    margin-top: 2px;
}

textarea.form-input {
    resize: vertical;
    font-family: inherit;
    min-height: 80px;
}

.color-picker-wrapper {
    display: flex;
    align-items: center;
    gap: 12px;
}

.color-picker {
    width: 48px;
    height: 40px;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    cursor: pointer;
    background: transparent;
    padding: 2px;
}

.danger-card {
    padding: 40px 32px;
    text-align: center;
    max-width: 520px;
    margin: 0 auto;
}

.danger-icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 64px;
    height: 64px;
    background: #fef2f2;
    color: var(--color-danger);
    border-radius: 50%;
    margin-bottom: 16px;
}

.danger-icon svg {
    width: 32px !important;
    height: 32px !important;
}

.danger-card h3 {
    font-size: 18px;
    font-weight: 700;
    margin-bottom: 8px;
    color: var(--color-text);
}

.danger-card > p {
    color: var(--color-muted);
    font-size: 13.5px;
    margin-bottom: 20px;
}

.user-preview {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px 16px;
    background: #f9fafb;
    border-radius: var(--radius-sm);
    margin-bottom: 20px;
    text-align: left;
}

.user-preview div:last-child {
    display: flex;
    flex-direction: column;
}

.user-preview strong {
    font-size: 14px;
    font-weight: 600;
    color: var(--color-text);
}

.user-preview span {
    font-size: 12.5px;
    color: var(--color-muted);
}

/* ============================================
   ROLES
   ============================================ */
.roles-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 16px;
    padding: 20px;
}

.role-card {
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    padding: 20px;
    box-shadow: var(--shadow-xs);
    display: flex;
    flex-direction: column;
    gap: 12px;
    transition: box-shadow 0.2s ease, transform 0.2s ease;
}

.role-card:hover {
    box-shadow: var(--shadow-md);
    transform: translateY(-2px);
}

.role-card-header {
    display: flex;
    align-items: center;
    gap: 12px;
}

.role-color {
    width: 40px;
    height: 40px;
    border-radius: 10px;
    flex-shrink: 0;
}

.role-info {
    display: flex;
    flex-direction: column;
    gap: 2px;
    flex: 1;
    min-width: 0;
}

.role-info h3 {
    font-size: 15px;
    font-weight: 700;
    color: var(--color-text);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.role-desc {
    font-size: 13px;
    color: var(--color-muted);
    line-height: 1.5;
    min-height: 40px;
    flex: 1;
}

.role-stats {
    display: flex;
    gap: 12px;
    font-size: 12px;
    color: var(--color-muted);
}

.role-stats span {
    display: inline-flex;
    align-items: center;
    gap: 4px;
}

.role-stats svg {
    width: 14px !important;
    height: 14px !important;
}

.role-actions {
    display: flex;
    gap: 8px;
    padding-top: 12px;
    border-top: 1px solid var(--color-border);
}

/* ============================================
   PERFIL
   ============================================ */
.profile-layout {
    display: grid;
    grid-template-columns: 300px 1fr;
    gap: 20px;
    align-items: start;
}

@media (max-width: 900px) {
    .profile-layout { grid-template-columns: 1fr; }
}

.profile-sidebar {
    padding: 28px 20px;
    text-align: center;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 12px;
}

.profile-avatar-big {
    width: 80px;
    height: 80px;
    border-radius: 50%;
    background: var(--color-primary);
    color: #fff;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 32px;
}

.profile-sidebar h3 {
    font-size: 18px;
    font-weight: 700;
    color: var(--color-text);
}

.profile-email {
    font-size: 13px;
    color: var(--color-muted);
}

.profile-stats {
    width: 100%;
    padding-top: 16px;
    margin-top: 8px;
    border-top: 1px solid var(--color-border);
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.profile-stat {
    display: flex;
    justify-content: space-between;
    font-size: 12.5px;
}

.profile-stat-label { color: var(--color-muted); }
.profile-stat-value { color: var(--color-text); font-weight: 600; }

/* ============================================
   ACTIVIDAD
   ============================================ */
.activity-header-card {
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    padding: 20px 24px;
    box-shadow: var(--shadow-xs);
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 20px;
    margin-bottom: 20px;
    flex-wrap: wrap;
}

.activity-user-info {
    display: flex;
    align-items: center;
    gap: 16px;
}

.activity-user-meta h3 {
    font-size: 17px;
    font-weight: 700;
    color: var(--color-text);
    margin-bottom: 2px;
}

.activity-user-meta p {
    font-size: 13px;
    color: var(--color-muted);
}

.activity-stats {
    display: flex;
    gap: 24px;
}

.activity-stat {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
}

.activity-stat-value {
    font-size: 22px;
    font-weight: 700;
    color: var(--color-primary);
    letter-spacing: -0.02em;
}

.activity-stat-label {
    font-size: 11.5px;
    color: var(--color-muted);
    text-transform: uppercase;
    letter-spacing: 0.05em;
    font-weight: 600;
}

.timeline {
    padding: 20px 24px;
    position: relative;
}

.timeline-item {
    display: flex;
    gap: 16px;
    padding-bottom: 20px;
    position: relative;
}

.timeline-item:not(:last-child)::before {
    content: '';
    position: absolute;
    left: 5px;
    top: 20px;
    bottom: 0;
    width: 2px;
    background: var(--color-border);
}

.timeline-dot {
    width: 12px;
    height: 12px;
    border-radius: 50%;
    background: var(--color-primary);
    flex-shrink: 0;
    margin-top: 4px;
    position: relative;
    z-index: 1;
}

.timeline-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.timeline-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
}

.timeline-header strong {
    font-size: 13.5px;
    font-weight: 600;
    color: var(--color-text);
}

.timeline-date {
    font-size: 12px;
    color: var(--color-muted);
}

.timeline-detail {
    font-size: 13px;
    color: var(--color-muted);
}

.timeline-ip {
    font-size: 11.5px;
    color: var(--color-muted);
    font-family: 'Courier New', monospace;
    opacity: 0.7;
}

/* ============================================
   PERMISOS — TABLA
   ============================================ */
.permisos-header {
    margin-bottom: 12px;
}

.permisos-toolbar {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 12px 16px;
    background: #f9fafb;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    margin-bottom: 16px;
    flex-wrap: wrap;
}

.permisos-counter {
    margin-left: auto;
    font-size: 13px;
    color: var(--color-muted);
}

.permisos-counter strong {
    color: var(--color-primary);
    font-weight: 700;
}

.permisos-table-wrapper {
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    overflow: hidden;
    max-height: 600px;
    overflow-y: auto;
}

.permisos-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 13px;
}

.permisos-table thead {
    background: #f9fafb;
    border-bottom: 1px solid var(--color-border);
    position: sticky;
    top: 0;
    z-index: 2;
}

.permisos-table th {
    padding: 12px 16px;
    font-size: 11.5px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--color-muted);
    text-align: left;
    white-space: nowrap;
}

.permisos-table tbody tr {
    border-bottom: 1px solid var(--color-border);
}

.permisos-table tbody tr:last-child {
    border-bottom: none;
}

.permisos-table tbody tr:hover {
    background: #fafafa;
}

.permisos-table td {
    padding: 10px 16px;
    vertical-align: middle;
}

.permiso-row-modulo {
    background: #f9fafb;
}

.permiso-row-modulo td {
    padding: 12px 16px;
}

.permiso-row-submodulo td {
    padding: 8px 16px;
}

.submodulo-label {
    color: var(--color-muted);
    font-size: 12.5px;
    padding-left: 12px;
}

.permisos-table input[type="checkbox"] {
    width: 16px;
    height: 16px;
    cursor: pointer;
    accent-color: var(--color-primary);
}

.check-modulo {
    width: 18px !important;
    height: 18px !important;
}

/* ============================================
   SERIES DE DOCUMENTOS
   ============================================ */
.series-help {
    margin: 20px;
    padding: 20px;
    background: #f9fafb;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
}

.series-help h4 {
    font-size: 14px;
    font-weight: 600;
    color: var(--color-text);
    margin-bottom: 10px;
}

.series-help ul {
    list-style: none;
    padding: 0;
    margin: 0 0 12px 0;
    display: flex;
    flex-wrap: wrap;
    gap: 8px 20px;
}

.series-help li {
    font-size: 13px;
    color: var(--color-text);
}

.series-help code {
    font-family: 'Courier New', monospace;
    background: #fff;
    border: 1px solid var(--color-border);
    padding: 2px 6px;
    border-radius: 4px;
    font-size: 12px;
    color: var(--color-primary);
}

.series-help p {
    font-size: 13px;
    color: var(--color-muted);
}

.preview-box {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px 16px;
    background: var(--color-primary-soft);
    border: 1px solid #c7d2fe;
    border-radius: var(--radius-sm);
}

.preview-label {
    font-size: 12.5px;
    font-weight: 600;
    color: var(--color-primary);
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.preview-value {
    font-family: 'Courier New', monospace;
    font-size: 15px;
    font-weight: 700;
    color: var(--color-primary);
    letter-spacing: 0.02em;
}

/* ============================================
   NOMENCLATURA — TABS
   ============================================ */
.nomenclatura-tabs {
    display: flex;
    gap: 8px;
    margin-bottom: 20px;
    border-bottom: 1px solid var(--color-border);
    padding-bottom: 0;
}

.nomenclatura-tab {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 10px 16px;
    font-size: 13.5px;
    font-weight: 500;
    color: var(--color-muted);
    border-bottom: 2px solid transparent;
    transition: all 0.15s ease;
    text-decoration: none;
    margin-bottom: -1px;
}

.nomenclatura-tab svg {
    width: 16px;
    height: 16px;
}

.nomenclatura-tab:hover {
    color: var(--color-text);
    text-decoration: none;
}

.nomenclatura-tab.is-active {
    color: var(--color-primary);
    border-bottom-color: var(--color-primary);
}

/* ============================================
   CONFIGURACIÓN DE EMPRESA
   ============================================ */
.empresa-layout {
    display: grid;
    grid-template-columns: 320px 1fr;
    gap: 20px;
    align-items: start;
}

@media (max-width: 900px) {
    .empresa-layout { grid-template-columns: 1fr; }
}

.empresa-logo-card {
    padding: 24px;
    text-align: center;
}

.empresa-logo-preview {
    margin-top: 16px;
    display: flex;
    align-items: center;
    justify-content: center;
}

.empresa-logo-preview img {
    max-width: 200px;
    max-height: 200px;
    object-fit: contain;
    border-radius: 10px;
    background: #fff;
    padding: 8px;
    border: 1px solid var(--color-border);
}

.empresa-logo-placeholder {
    width: 180px;
    height: 180px;
    border: 2px dashed var(--color-border);
    border-radius: 10px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 8px;
    color: var(--color-muted);
    font-size: 12.5px;
    background: #f9fafb;
}

.empresa-logo-placeholder svg {
    width: 40px !important;
    height: 40px !important;
    opacity: 0.4;
}

.empresa-info-card {
    padding: 28px;
}

.empresa-info-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px 24px;
    margin-top: 16px;
}

.empresa-info-full {
    grid-column: 1 / -1;
}

.empresa-info-item {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.empresa-info-label {
    font-size: 11.5px;
    font-weight: 600;
    color: var(--color-muted);
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.empresa-info-value {
    font-size: 13.5px;
    color: var(--color-text);
    word-break: break-word;
}

.empresa-notas {
    font-size: 13px;
    color: var(--color-muted);
    line-height: 1.6;
    margin-top: 8px;
}

/* Upload de logo */
.logo-upload-wrapper {
    display: flex;
    align-items: center;
    gap: 24px;
    padding: 20px;
    background: #f9fafb;
    border: 1px dashed var(--color-border);
    border-radius: var(--radius-md);
    margin-bottom: 8px;
}

@media (max-width: 640px) {
    .logo-upload-wrapper {
        flex-direction: column;
        text-align: center;
    }
}

.logo-preview {
    width: 140px;
    height: 140px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: #fff;
    border-radius: 10px;
    border: 1px solid var(--color-border);
    overflow: hidden;
    flex-shrink: 0;
}

.logo-preview img {
    max-width: 100%;
    max-height: 100%;
    object-fit: contain;
}

.logo-preview-placeholder {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 6px;
    color: var(--color-muted);
    font-size: 12px;
}

.logo-preview-placeholder svg {
    width: 36px !important;
    height: 36px !important;
    opacity: 0.4;
}

.logo-upload-controls {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

/* ============================================
   COTIZACIONES / VENTAS — ITEMS Y TOTALES
   ============================================ */
.items-toolbar {
    position: relative;
    margin-bottom: 12px;
}

.producto-results {
    display: none;
    position: absolute;
    top: 100%;
    left: 0;
    right: 0;
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    box-shadow: var(--shadow-lg);
    max-height: 320px;
    overflow-y: auto;
    z-index: 100;
    margin-top: 4px;
}

.producto-result-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 12px 16px;
    cursor: pointer;
    border-bottom: 1px solid var(--color-border);
    transition: background 0.15s ease;
}

.producto-result-item:last-child { border-bottom: none; }
.producto-result-item:hover { background: #f9fafb; }

.producto-result-item strong {
    font-size: 13.5px;
    color: var(--color-text);
}

.producto-result-item small {
    font-size: 11.5px;
    color: var(--color-muted);
}

.totales-panel {
    margin-top: 24px;
    margin-left: auto;
    max-width: 340px;
    padding: 20px;
    background: #f9fafb;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
}

.totales-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px 0;
    font-size: 14px;
    color: var(--color-text);
}

.totales-row span {
    color: var(--color-muted);
}

.totales-row strong {
    font-weight: 600;
}

.totales-row-final {
    border-top: 1px solid var(--color-border);
    margin-top: 8px;
    padding-top: 14px;
    font-size: 18px;
}

.totales-row-final span {
    font-weight: 700;
    color: var(--color-text);
    text-transform: uppercase;
    letter-spacing: 0.03em;
}

.totales-row-final strong {
    font-size: 20px;
    font-weight: 700;
    color: var(--color-primary);
}

/* Detalle de cotización */
.cotizacion-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 20px;
    padding-bottom: 20px;
    border-bottom: 1px solid var(--color-border);
    flex-wrap: wrap;
}

.cotizacion-info h3 {
    font-size: 12.5px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--color-muted);
    margin-bottom: 10px;
}

.cotizacion-info p {
    font-size: 13.5px;
    color: var(--color-text);
    margin-bottom: 3px;
}

.cotizacion-estado {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
}

/* ============================================
   POS — PUNTO DE VENTA
   ============================================ */
.pos-body {
    height: 100vh;
    overflow: hidden;
    background: #f3f4f6;
}

.pos-container {
    display: flex;
    flex-direction: column;
    height: 100vh;
}

.pos-header {
    height: 64px;
    background: var(--color-surface);
    border-bottom: 1px solid var(--color-border);
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0 20px;
    flex-shrink: 0;
}

.pos-header-left {
    display: flex;
    align-items: center;
    gap: 16px;
}

.pos-back-btn {
    width: 40px;
    height: 40px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--color-bg);
    border-radius: var(--radius-sm);
    color: var(--color-text);
    transition: background 0.15s ease;
}

.pos-back-btn:hover { background: #e5e7eb; }

.pos-back-btn svg { width: 20px; height: 20px; }

.pos-brand {
    display: flex;
    align-items: center;
    gap: 12px;
}

.pos-brand img {
    width: 40px;
    height: 40px;
    object-fit: contain;
    border-radius: 8px;
}

.pos-brand h1 {
    font-size: 15px;
    font-weight: 700;
    color: var(--color-text);
    margin-bottom: 2px;
}

.pos-brand p {
    font-size: 11.5px;
    color: var(--color-muted);
}

.pos-user {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
}

.pos-user-name {
    font-size: 13px;
    font-weight: 600;
    color: var(--color-text);
}

.pos-user-role {
    font-size: 11px;
    color: var(--color-muted);
}

.pos-grid {
    flex: 1;
    display: grid;
    grid-template-columns: 1fr 420px;
    overflow: hidden;
}

.pos-products-panel {
    display: flex;
    flex-direction: column;
    padding: 16px;
    overflow: hidden;
}

.pos-search-wrapper {
    position: relative;
    margin-bottom: 12px;
}

.pos-search-wrapper svg {
    position: absolute;
    left: 16px;
    top: 50%;
    transform: translateY(-50%);
    width: 20px;
    height: 20px;
    color: var(--color-muted);
    pointer-events: none;
}

.pos-search-wrapper input {
    width: 100%;
    padding: 14px 16px 14px 48px;
    font-size: 15px;
    font-family: inherit;
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    outline: none;
    transition: border-color 0.15s ease;
}

.pos-search-wrapper input:focus {
    border-color: var(--color-primary);
    box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.1);
}

.pos-categories {
    display: flex;
    gap: 8px;
    margin-bottom: 12px;
    overflow-x: auto;
    scrollbar-width: none;
    padding-bottom: 4px;
}

.pos-categories::-webkit-scrollbar { display: none; }

.pos-category-btn {
    padding: 8px 16px;
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: 999px;
    font-size: 13px;
    font-weight: 500;
    font-family: inherit;
    color: var(--color-muted);
    cursor: pointer;
    white-space: nowrap;
    transition: all 0.15s ease;
}

.pos-category-btn:hover {
    color: var(--color-text);
    border-color: var(--color-muted);
}

.pos-category-btn.is-active {
    background: var(--color-primary);
    border-color: var(--color-primary);
    color: #fff;
}

.pos-products-grid {
    flex: 1;
    overflow-y: auto;
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
    gap: 12px;
    align-content: start;
    padding-right: 4px;
}

.pos-product-card {
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    padding: 14px;
    display: flex;
    flex-direction: column;
    justify-content: space-between;
    min-height: 110px;
    cursor: pointer;
    text-align: left;
    font-family: inherit;
    transition: all 0.15s ease;
}

.pos-product-card:hover {
    border-color: var(--color-primary);
    transform: translateY(-2px);
    box-shadow: var(--shadow-md);
}

.pos-product-card.is-agotado {
    opacity: 0.5;
    cursor: not-allowed;
}

.pos-product-name {
    display: block;
    font-size: 13.5px;
    font-weight: 600;
    color: var(--color-text);
    line-height: 1.3;
    margin-bottom: 4px;
}

.pos-product-sku {
    display: block;
    font-size: 11px;
    color: var(--color-muted);
    font-family: monospace;
}

.pos-product-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: 10px;
    padding-top: 8px;
    border-top: 1px solid var(--color-border);
}

.pos-product-price {
    font-size: 14px;
    font-weight: 700;
    color: var(--color-primary);
}

.pos-product-stock {
    font-size: 11px;
    color: var(--color-muted);
}

.pos-empty-products {
    grid-column: 1 / -1;
    text-align: center;
    padding: 40px;
    color: var(--color-muted);
}

.pos-cart-panel {
    background: var(--color-surface);
    border-left: 1px solid var(--color-border);
    display: flex;
    flex-direction: column;
    overflow: hidden;
}

.pos-cart-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 16px 20px;
    border-bottom: 1px solid var(--color-border);
    flex-shrink: 0;
}

.pos-cart-header h2 {
    font-size: 15px;
    font-weight: 700;
    color: var(--color-text);
}

.pos-cotizacion-banner {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 16px;
    background: var(--color-primary-soft);
    border-bottom: 1px solid #c7d2fe;
    color: var(--color-primary);
    font-size: 12.5px;
    font-weight: 600;
}

.pos-cotizacion-banner svg {
    width: 16px;
    height: 16px;
    flex-shrink: 0;
}

.pos-client-select {
    padding: 12px 20px;
    border-bottom: 1px solid var(--color-border);
    flex-shrink: 0;
}

.pos-client-select label {
    display: block;
    font-size: 11.5px;
    font-weight: 600;
    color: var(--color-muted);
    text-transform: uppercase;
    letter-spacing: 0.05em;
    margin-bottom: 6px;
}

/* ===== Buscador de cliente POS ===== */
.pos-cliente-wrapper {
    position: relative;
}

.pos-cliente-icon {
    position: absolute;
    left: 12px;
    top: 50%;
    transform: translateY(-50%);
    width: 18px;
    height: 18px;
    color: var(--color-muted);
    pointer-events: none;
    z-index: 1;
}

.pos-cliente-input {
    width: 100%;
    padding: 10px 12px 10px 40px;
    font-family: inherit;
    font-size: 13.5px;
    color: var(--color-text);
    background: var(--color-bg);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    outline: none;
    transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.pos-cliente-input:focus {
    border-color: var(--color-primary);
    background: var(--color-surface);
    box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.1);
}

.pos-cliente-input::placeholder {
    color: #9ca3af;
}

.pos-cliente-results {
    display: none;
    position: absolute;
    top: 100%;
    left: 0;
    right: 0;
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    box-shadow: var(--shadow-lg);
    max-height: 300px;
    overflow-y: auto;
    z-index: 1000;
    margin-top: 4px;
}

.pos-cliente-result {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 14px;
    cursor: pointer;
    border-bottom: 1px solid var(--color-border);
    transition: background 0.15s ease;
}

.pos-cliente-result:last-child { border-bottom: none; }
.pos-cliente-result:hover { background: #f9fafb; }

.pos-cliente-result-avatar {
    width: 32px;
    height: 32px;
    min-width: 32px;
    border-radius: 50%;
    background: var(--color-primary);
    color: #fff;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 600;
    font-size: 13px;
    flex-shrink: 0;
}

.pos-cliente-result-info {
    display: flex;
    flex-direction: column;
    min-width: 0;
    flex: 1;
}

.pos-cliente-result-info strong {
    font-size: 13px;
    font-weight: 600;
    color: var(--color-text);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.pos-cliente-result-info small {
    font-size: 11.5px;
    color: var(--color-muted);
}

/* ============================================
   POS — BLOQUE DE ENVÍO (LOGÍSTICA)
   ============================================ */
.pos-envio-toggle {
    padding: 10px 20px;
    background: #f9fafb;
    border-bottom: 1px solid var(--color-border);
    flex-shrink: 0;
}

.pos-checkbox-label {
    display: flex;
    align-items: center;
    gap: 10px;
    cursor: pointer;
    font-size: 13.5px;
    font-weight: 600;
    color: var(--color-text);
}

.pos-checkbox-label input[type="checkbox"] {
    width: 18px;
    height: 18px;
    cursor: pointer;
    accent-color: var(--color-primary);
}

.pos-envio-panel {
    padding: 14px 20px;
    background: #eef2ff;
    border-bottom: 1px solid #c7d2fe;
    display: flex;
    flex-direction: column;
    gap: 10px;
    flex-shrink: 0;
}

.pos-envio-panel .form-field {
    margin: 0;
    gap: 4px;
}

.pos-envio-panel label {
    font-size: 11.5px;
    font-weight: 600;
    color: var(--color-primary);
    margin-bottom: 0;
    display: block;
    text-transform: uppercase;
    letter-spacing: 0.03em;
}

.pos-envio-panel .form-input {
    font-size: 13px;
    padding: 7px 10px;
    background: #fff;
    border-color: #c7d2fe;
}

.pos-envio-panel .form-input:focus {
    border-color: var(--color-primary);
    box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.12);
}

.pos-cart-items {
    flex: 1;
    overflow-y: auto;
    padding: 12px 20px;
}

.pos-cart-empty {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 100%;
    text-align: center;
    color: var(--color-muted);
    gap: 12px;
}

.pos-cart-empty svg {
    width: 48px;
    height: 48px;
    opacity: 0.3;
}

.pos-cart-empty p { font-size: 13px; }

.pos-cart-item {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 10px 0;
    border-bottom: 1px solid var(--color-border);
}

.pos-cart-item-info {
    flex: 1;
    min-width: 0;
}

.pos-cart-item-name {
    display: block;
    font-size: 13px;
    font-weight: 600;
    color: var(--color-text);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.pos-cart-item-price {
    display: block;
    font-size: 11px;
    color: var(--color-muted);
}

.pos-cart-item-controls {
    display: flex;
    align-items: center;
    gap: 4px;
    flex-shrink: 0;
}

.pos-cart-item-controls button {
    width: 24px;
    height: 24px;
    border: 1px solid var(--color-border);
    background: var(--color-surface);
    border-radius: var(--radius-sm);
    cursor: pointer;
    font-size: 14px;
    font-weight: 600;
    color: var(--color-text);
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.15s ease;
}

.pos-cart-item-controls button:hover {
    background: var(--color-primary);
    border-color: var(--color-primary);
    color: #fff;
}

.pos-cart-item-qty {
    min-width: 26px;
    text-align: center;
    font-size: 13px;
    font-weight: 600;
    color: var(--color-text);
}

.pos-cart-item-total {
    font-size: 13px;
    font-weight: 700;
    color: var(--color-text);
    min-width: 70px;
    text-align: right;
    flex-shrink: 0;
}

.pos-cart-item-remove {
    width: 24px;
    height: 24px;
    border: none;
    background: transparent;
    color: var(--color-muted);
    cursor: pointer;
    font-size: 14px;
    flex-shrink: 0;
    transition: color 0.15s ease;
}

.pos-cart-item-remove:hover { color: var(--color-danger); }

.pos-cart-totals {
    padding: 16px 20px;
    border-top: 1px solid var(--color-border);
    background: #f9fafb;
    flex-shrink: 0;
}

.pos-total-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 6px 0;
    font-size: 13.5px;
}

.pos-total-row span { color: var(--color-muted); }

.pos-total-row input {
    width: 100px;
    padding: 4px 8px;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    font-family: inherit;
    font-size: 13px;
    text-align: right;
    outline: none;
}

.pos-total-row-final {
    border-top: 1px solid var(--color-border);
    margin-top: 8px;
    padding-top: 12px;
    font-size: 16px;
}

.pos-total-row-final span {
    font-weight: 700;
    color: var(--color-text);
}

.pos-total-row-final strong {
    font-size: 22px;
    font-weight: 700;
    color: var(--color-primary);
}

.pos-cart-actions {
    padding: 16px 20px;
    border-top: 1px solid var(--color-border);
    flex-shrink: 0;
}

.pos-btn-cobrar {
    width: 100%;
    justify-content: center;
    padding: 14px 20px;
    font-size: 15px;
    font-weight: 700;
    letter-spacing: 0.03em;
}

.pos-btn-cobrar:disabled {
    background: #d1d5db;
    cursor: not-allowed;
}

/* Modal POS */
.pos-modal {
    display: none;
    position: fixed;
    inset: 0;
    background: rgba(0,0,0,0.5);
    z-index: 9999;
    align-items: center;
    justify-content: center;
    padding: 20px;
}

.pos-modal.is-open {
    display: flex;
}

.pos-modal-content {
    background: var(--color-surface);
    border-radius: var(--radius-lg);
    box-shadow: 0 20px 60px rgba(0,0,0,0.3);
    width: 100%;
    max-width: 440px;
    overflow: hidden;
    max-height: 90vh;
    overflow-y: auto;
}

.pos-modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 20px 24px;
    border-bottom: 1px solid var(--color-border);
}

.pos-modal-header h3 {
    font-size: 16px;
    font-weight: 700;
    color: var(--color-text);
}

.pos-modal-body {
    padding: 24px;
    display: flex;
    flex-direction: column;
    gap: 16px;
}

.pos-modal-total {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 16px 20px;
    background: var(--color-primary-soft);
    border-radius: var(--radius-md);
}

.pos-modal-total span {
    font-size: 13px;
    font-weight: 600;
    color: var(--color-primary);
}

.pos-modal-total strong {
    font-size: 24px;
    font-weight: 700;
    color: var(--color-primary);
}

.pos-modal-cambio {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 14px 20px;
    background: #ecfdf5;
    border: 1px solid #a7f3d0;
    border-radius: var(--radius-md);
}

.pos-modal-cambio span {
    font-size: 13px;
    font-weight: 600;
    color: #065f46;
}

.pos-modal-cambio strong {
    font-size: 20px;
    font-weight: 700;
    color: #065f46;
}

.pos-modal-footer {
    display: flex;
    gap: 12px;
    justify-content: flex-end;
    padding: 16px 24px;
    background: #f9fafb;
    border-top: 1px solid var(--color-border);
}

.pos-modal-footer .btn-primary,
.pos-modal-footer .btn-secondary {
    padding: 10px 20px;
}

/* Modal de éxito */
.pos-modal-success {
    text-align: center;
    padding: 40px 32px;
}

.pos-success-icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 80px;
    height: 80px;
    background: #d1fae5;
    border-radius: 50%;
    margin: 0 auto 20px;
}

.pos-success-icon svg {
    width: 40px;
    height: 40px;
    color: #10b981;
}

.pos-modal-success h3 {
    font-size: 22px;
    font-weight: 700;
    color: var(--color-text);
    margin-bottom: 8px;
}

.pos-modal-success > p {
    font-size: 14px;
    color: var(--color-muted);
    margin-bottom: 20px;
}

.pos-success-total {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 14px 20px;
    background: #f9fafb;
    border-radius: var(--radius-md);
    margin-bottom: 24px;
}

.pos-success-total span {
    font-size: 13px;
    color: var(--color-muted);
    font-weight: 600;
}

.pos-success-total strong {
    font-size: 20px;
    font-weight: 700;
    color: var(--color-primary);
}

.pos-modal-success .pos-modal-footer {
    justify-content: center;
    background: transparent;
    border-top: none;
    padding: 0;
}

/* ============================================
   FACTURA — IMPRESIÓN
   ============================================ */
.factura-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 20px;
    padding-bottom: 24px;
    border-bottom: 2px solid var(--color-border);
    flex-wrap: wrap;
}

.factura-empresa {
    display: flex;
    align-items: center;
    gap: 16px;
}

.factura-numero {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
}

.factura-numero-label {
    font-size: 11px;
    font-weight: 700;
    color: var(--color-muted);
    text-transform: uppercase;
    letter-spacing: 0.1em;
}

.factura-numero-value {
    font-size: 22px;
    font-weight: 700;
    color: var(--color-text);
    font-family: 'Courier New', monospace;
}

.factura-info-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 32px;
    padding: 24px 0;
    border-bottom: 1px solid var(--color-border);
}

@media (max-width: 640px) {
    .factura-info-grid { grid-template-columns: 1fr; }
}

.factura-info-label {
    font-size: 11px;
    font-weight: 700;
    color: var(--color-muted);
    text-transform: uppercase;
    letter-spacing: 0.1em;
    margin-bottom: 8px;
}

.factura-info-grid p {
    font-size: 13.5px;
    color: var(--color-text);
    margin-bottom: 3px;
}

@media print {
    .erp-sidebar,
    .erp-topbar,
    .module-header,
    .module-alert,
    .erp-footer { display: none !important; }

    .erp-main-wrapper { margin-left: 0 !important; }
    .erp-main { padding: 0 !important; max-width: 100% !important; }
    .module-card { box-shadow: none; border: none; padding: 0 !important; }
}

/* ============================================
   BUSCADOR DE CLIENTES (Cotizaciones)
   ============================================ */
.cliente-search-wrapper {
    position: relative;
}

.cliente-search-results {
    display: none;
    position: absolute;
    top: 100%;
    left: 0;
    right: 0;
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    box-shadow: var(--shadow-lg);
    max-height: 300px;
    overflow-y: auto;
    z-index: 500;
    margin-top: 4px;
}

.cliente-result-item {
    padding: 10px 14px;
    cursor: pointer;
    border-bottom: 1px solid var(--color-border);
    transition: background 0.15s ease;
    font-size: 13px;
}

.cliente-result-item:last-child { border-bottom: none; }
.cliente-result-item:hover { background: #f9fafb; }

.cliente-result-item strong {
    display: block;
    font-size: 13px;
    font-weight: 600;
    color: var(--color-text);
    margin-bottom: 2px;
}

.cliente-result-item small {
    font-size: 11.5px;
    color: var(--color-muted);
}

/* ============================================
   REPORTES — FILTROS
   ============================================ */
.reportes-filtros {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.filtros-row {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 12px;
    align-items: end;
}

.filtros-row .form-field {
    min-width: 0;
}

.filtros-row button,
.filtros-row .btn-secondary {
    white-space: nowrap;
    width: 100%;
    justify-content: center;
}

@media (max-width: 900px) {
    .filtros-row {
        grid-template-columns: 1fr;
    }
}

/* Charts */
canvas {
    max-width: 100%;
}

/* ============================================
   BAJAS DE INVENTARIO
   ============================================ */

.producto-search-wrapper {
    position: relative;
}

.producto-search-results {
    display: none;
    position: absolute;
    top: 100%;
    left: 0;
    right: 0;
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    box-shadow: var(--shadow-lg);
    max-height: 320px;
    overflow-y: auto;
    z-index: 500;
    margin-top: 4px;
}

.radio-group {
    display: flex;
    flex-direction: column;
    gap: 12px;
    margin-top: 8px;
}

.radio-option {
    display: flex;
    align-items: flex-start;
    gap: 12px;
    padding: 14px 16px;
    background: #f9fafb;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    cursor: pointer;
    transition: all 0.15s ease;
}

.radio-option:hover {
    border-color: var(--color-primary);
    background: var(--color-primary-soft);
}

.radio-option input[type="radio"] {
    margin-top: 3px;
    width: 18px;
    height: 18px;
    accent-color: var(--color-primary);
    cursor: pointer;
    flex-shrink: 0;
}

.radio-option span {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.radio-option strong {
    font-size: 13.5px;
    font-weight: 600;
}

.radio-option small {
    font-size: 12px;
    color: var(--color-muted);
}

/* ============================================
   ÓRDENES DE COMPRA Y PAGOS
   ============================================ */

.recibir-checkbox {
    width: 18px !important;
    height: 18px !important;
    cursor: pointer;
    accent-color: var(--color-success);
}

.cantidad-recibir:disabled {
    background: #f3f4f6;
    color: var(--color-muted);
    cursor: not-allowed;
}

/* ============================================
   RESPONSIVE POS
   ============================================ */
@media (max-width: 1100px) {
    .pos-grid {
        grid-template-columns: 1fr 360px;
    }
    .pos-products-grid {
        grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
    }
}

@media (max-width: 900px) {
    .pos-grid {
        grid-template-columns: 1fr;
        grid-template-rows: 1fr auto;
    }
    .pos-cart-panel {
        max-height: 50vh;
        border-left: none;
        border-top: 1px solid var(--color-border);
    }
}

/* ============================================
   RESPONSIVE GENERAL
   ============================================ */
@media (max-width: 900px) {
    .erp-sidebar {
        width: var(--sidebar-width);
        transform: translateX(-100%);
        transition: transform 0.25s ease, width 0.25s ease;
    }

    .erp-sidebar.is-open {
        transform: translateX(0);
        box-shadow: 4px 0 20px rgba(0,0,0,0.15);
    }

    .erp-sidebar.is-open .erp-logo-text,
    .erp-sidebar.is-open .erp-nav-text,
    .erp-sidebar.is-open .erp-nav-arrow,
    .erp-sidebar.is-open .erp-user-meta { opacity: 1; }

    .erp-main-wrapper { margin-left: 0; }

    .erp-topbar { padding: 0 16px; }

    .erp-main { padding: 20px 16px; }

    .auth-logo { width: 100px; height: 100px; }
}

/* ============================================
   CONTABILIDAD — ESTADO DE RESULTADOS / BALANCE GENERAL
   ============================================ */

.reporte-seccion {
    margin-bottom: 24px;
}

.reporte-seccion-titulo {
    font-size: 13px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    padding-bottom: 8px;
    border-bottom: 2px solid currentColor;
    margin-bottom: 12px;
}

.reporte-tabla {
    width: 100%;
    font-size: 13.5px;
    border-collapse: collapse;
}

.reporte-tabla td {
    padding: 6px 8px;
}

.reporte-tabla tr:not(.reporte-total):hover {
    background: #f9fafb;
}

.reporte-total td {
    border-top: 1px solid var(--color-border);
    padding-top: 10px;
    font-size: 14px;
}

.reporte-vacio {
    font-size: 13px;
    color: var(--color-muted);
    font-style: italic;
    padding: 8px 0;
}

.reporte-subtotal {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 14px 20px;
    background: #f3f4f6;
    border-radius: var(--radius-sm);
    margin: 20px 0;
    font-size: 14px;
    font-weight: 700;
}

.reporte-total-final {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 20px 24px;
    background: linear-gradient(135deg, #eef2ff 0%, #e0e7ff 100%);
    border: 2px solid var(--color-primary);
    border-radius: 8px;
    margin-top: 24px;
    font-weight: 700;
}

/* ===== BALANCE GENERAL — LAYOUT DE 2 COLUMNAS ===== */
.balance-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 40px;
}

@media (max-width: 900px) {
    .balance-grid {
        grid-template-columns: 1fr;
    }
}

/* ===== DIVISOR DE SUBMENÚ (Finanzas) ===== */
.erp-nav-submenu-divider {
    height: 1px;
    background: var(--color-border);
    margin: 8px 12px;
}

/* ===== PRINT: CONTABILIDAD ===== */
@media print {
    .balance-grid {
        grid-template-columns: 1fr 1fr;
        gap: 24px;
    }
    .reporte-total-final {
        -webkit-print-color-adjust: exact;
        print-color-adjust: exact;
    }
    .reporte-subtotal {
        -webkit-print-color-adjust: exact;
        print-color-adjust: exact;
    }
}
````

====================================================
 wwwroot\js - 1 archivo(s)
====================================================

===== FILE: wwwroot/js/site.js =====

````javascript
// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

````

