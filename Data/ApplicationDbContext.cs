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
        public DbSet<PlanCuenta> PlanCuentas { get; set; }                          // ← NUEVO

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
            modelBuilder.Entity<PlanCuenta>().HasIndex(c => c.Codigo).IsUnique();       // ← NUEVO

            // RRHH
            modelBuilder.Entity<Empleado>().HasIndex(e => e.Codigo).IsUnique();
            modelBuilder.Entity<VacacionEmpleado>().HasIndex(v => v.Numero).IsUnique();
            modelBuilder.Entity<PermisoEmpleado>().HasIndex(p => p.Numero).IsUnique();
            modelBuilder.Entity<ValeEmpleado>().HasIndex(v => v.Numero).IsUnique();
            modelBuilder.Entity<Nomina>().HasIndex(n => n.Numero).IsUnique();
            modelBuilder.Entity<PagoNominaEmpleado>().HasIndex(p => p.Numero).IsUnique();

            // Config deducciones - único por año
            modelBuilder.Entity<ConfiguracionDeduccion>().HasIndex(c => c.Anio).IsUnique();
        }
    }
}