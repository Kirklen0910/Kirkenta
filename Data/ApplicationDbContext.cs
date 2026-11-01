using Kirkenta.Helpers;
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
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }

        // ===== CONFIGURACIÓN =====
        public DbSet<ConfiguracionEmpresa> ConfiguracionEmpresa { get; set; }

        // ===== BAJAS =====
        public DbSet<BajaInventario> BajasInventario { get; set; }
        public DbSet<ConversionInventario> ConversionesInventario { get; set; }
        public DbSet<SaldoFavorMovimiento> SaldoFavorMovimientos { get; set; }
        public DbSet<EntregaBodega> EntregasBodega { get; set; }
        public DbSet<DetalleEntregaBodega> DetallesEntregaBodega { get; set; }

        // ===== ACTIVOS FIJOS =====
        public DbSet<ActivoFijo> ActivosFijos { get; set; }
        public DbSet<DepreciacionActivo> DepreciacionesActivo { get; set; }
        public DbSet<MantenimientoActivo> MantenimientosActivo { get; set; }

        // ===== PRODUCCIÓN =====
        public DbSet<OrdenProduccion> OrdenesProduccion { get; set; }
        public DbSet<DetalleOrdenProduccion> DetallesOrdenProduccion { get; set; }
        public DbSet<InspeccionCalidad> InspeccionesCalidad { get; set; }

        // ===== CAJA CHICA =====
        public DbSet<GastoCajaChica> GastosCajaChica { get; set; }

        // ===== TOMA FÍSICA DE INVENTARIO =====
        public DbSet<TomaFisica> TomasFisicas { get; set; }
        public DbSet<DetalleTomaFisica> DetallesTomaFisica { get; set; }

        // ===== ASIENTOS CONTABLES =====
        public DbSet<AsientoContable> AsientosContables { get; set; }
        public DbSet<DetalleAsiento> DetallesAsiento { get; set; }

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
        public DbSet<CierreCajaConteo> ConteosCierreCaja { get; set; }
        public DbSet<VentaOffline> VentasOffline { get; set; }
        public DbSet<CatalogoWebConfig> CatalogoWebConfig { get; set; }
        public DbSet<PedidoWeb> PedidosWeb { get; set; }
        public DbSet<RegistroMigracion> RegistrosMigracion { get; set; }
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

        // ===== CACHE DE PERMISOS =====
        // Cuando se guardan cambios que afectan a usuarios (rol / activo / username),
        // roles o permisos, se invalida el cache de PermisoHelper para que el cambio
        // se aplique de inmediato en todas las páginas. Cambios que no afectan la
        // seguridad (UltimoAcceso, IntentosFallidos, etc.) NO vacían el cache.
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var invalidarCache = HayCambiosDeSeguridad();
            var resultado = base.SaveChanges(acceptAllChangesOnSuccess);

            if (invalidarCache)
                PermisoHelper.InvalidarCache();

            return resultado;
        }

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            var invalidarCache = HayCambiosDeSeguridad();
            var resultado = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            if (invalidarCache)
                PermisoHelper.InvalidarCache();

            return resultado;
        }

        private bool HayCambiosDeSeguridad()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State != EntityState.Added
                    && entry.State != EntityState.Modified
                    && entry.State != EntityState.Deleted)
                {
                    continue;
                }

                if (entry.Entity is Rol || entry.Entity is Permiso)
                    return true;

                if (entry.Entity is Usuario)
                {
                    // Usuario nuevo o eliminado: siempre afecta
                    if (entry.State != EntityState.Modified)
                        return true;

                    if (entry.Property(nameof(Usuario.Rol)).IsModified
                        || entry.Property(nameof(Usuario.Activo)).IsModified
                        || entry.Property(nameof(Usuario.Username)).IsModified
                        || entry.Property(nameof(Usuario.PasswordHash)).IsModified)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== CONCURRENCIA OPTIMISTA =====
            // DateTime mapeado a TIMESTAMP(6). MySQL lo actualiza en cada UPDATE.
            // Se usa IsConcurrencyToken + ValueGeneratedOnAddOrUpdate para evitar
            // el bug de EF Core 9 + Pomelo con IsRowVersion() en byte[].
            modelBuilder.Entity<Producto>()
                .Property(p => p.RowVersion)
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("timestamp(6)");

            // Saldo de cuentas: dos ventas/pagos simultaneos ya no pueden perder un movimiento.
            modelBuilder.Entity<CuentaFinanciera>()
                .Property(c => c.RowVersion)
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("timestamp(6)");

            // ===== ÍNDICES ÚNICOS =====
            modelBuilder.Entity<Permiso>()
                .HasIndex(p => new { p.RolId, p.Modulo, p.Submodulo })
                .IsUnique();

            modelBuilder.Entity<Cotizacion>().HasIndex(c => c.Numero).IsUnique();
            modelBuilder.Entity<Pedido>().HasIndex(p => p.Numero).IsUnique();
            modelBuilder.Entity<Venta>().HasIndex(v => v.Numero).IsUnique();
            modelBuilder.Entity<Factura>().HasIndex(f => f.Numero).IsUnique();
            modelBuilder.Entity<Devolucion>().HasIndex(d => d.Numero).IsUnique();
            modelBuilder.Entity<MovimientoInventario>().HasIndex(m => new { m.ProductoId, m.Fecha });
            modelBuilder.Entity<MovimientoInventario>().Property(m => m.Cantidad).HasPrecision(18, 4);
            modelBuilder.Entity<MovimientoInventario>().Property(m => m.StockAnterior).HasPrecision(18, 4);
            modelBuilder.Entity<MovimientoInventario>().Property(m => m.StockPosterior).HasPrecision(18, 4);
            modelBuilder.Entity<MovimientoInventario>().Property(m => m.CostoUnitario).HasPrecision(18, 4);
            modelBuilder.Entity<Cliente>().HasIndex(c => c.Codigo).IsUnique();
            modelBuilder.Entity<Producto>().HasIndex(p => p.SKU).IsUnique();
            modelBuilder.Entity<UnidadMedida>().HasIndex(u => u.Nombre).IsUnique();

            modelBuilder.Entity<BajaInventario>().HasIndex(b => b.Numero).IsUnique();
            modelBuilder.Entity<ConversionInventario>().HasIndex(c => c.Numero);
            modelBuilder.Entity<SaldoFavorMovimiento>().HasIndex(m => m.ClienteId);
            modelBuilder.Entity<EntregaBodega>().HasIndex(e => e.VentaId);
            modelBuilder.Entity<DetalleEntregaBodega>().HasIndex(d => d.EntregaBodegaId);
            modelBuilder.Entity<DetalleEntregaBodega>().Property(d => d.Cantidad).HasPrecision(18, 4);
            modelBuilder.Entity<SaldoFavorMovimiento>().Property(m => m.Monto).HasPrecision(18, 2);
            modelBuilder.Entity<Devolucion>().Property(d => d.MontoSaldoFavor).HasPrecision(18, 2);
            modelBuilder.Entity<Venta>().Property(v => v.SaldoFavorAplicado).HasPrecision(18, 2);
            modelBuilder.Entity<ConversionInventario>().Property(c => c.CantidadOrigen).HasPrecision(18, 4);
            modelBuilder.Entity<ConversionInventario>().Property(c => c.CantidadDestino).HasPrecision(18, 4);
            modelBuilder.Entity<ConversionInventario>().Property(c => c.CostoUnitarioOrigen).HasPrecision(18, 4);
            modelBuilder.Entity<ConversionInventario>().Property(c => c.CostoUnitarioDestino).HasPrecision(18, 4);
            modelBuilder.Entity<ConversionInventario>().Property(c => c.CostoTotal).HasPrecision(18, 4);
            modelBuilder.Entity<ConversionInventario>().Property(c => c.NuevoProductoPrecioVenta).HasPrecision(18, 2);

            // ===== MÓDULOS NUEVOS (tablas creadas por EsquemaModulosNuevos al arrancar) =====
            modelBuilder.Entity<ActivoFijo>().HasIndex(a => a.Codigo).IsUnique();
            modelBuilder.Entity<DepreciacionActivo>().HasIndex(d => new { d.ActivoFijoId, d.Anio, d.Mes }).IsUnique();
            modelBuilder.Entity<MantenimientoActivo>().HasIndex(m => m.ActivoFijoId);
            modelBuilder.Entity<OrdenProduccion>().HasIndex(o => o.Numero).IsUnique();
            modelBuilder.Entity<DetalleOrdenProduccion>().HasIndex(d => d.OrdenProduccionId);
            modelBuilder.Entity<OrdenProduccion>().Property(o => o.Cantidad).HasPrecision(18, 4);
            modelBuilder.Entity<OrdenProduccion>().Property(o => o.CantidadProducida).HasPrecision(18, 4);
            modelBuilder.Entity<DetalleOrdenProduccion>().Property(d => d.Cantidad).HasPrecision(18, 4);
            modelBuilder.Entity<DetalleOrdenProduccion>().Property(d => d.CostoUnitario).HasPrecision(18, 4);
            modelBuilder.Entity<InspeccionCalidad>().Property(i => i.CantidadInspeccionada).HasPrecision(18, 4);
            modelBuilder.Entity<InspeccionCalidad>().Property(i => i.CantidadDefectuosa).HasPrecision(18, 4);
            modelBuilder.Entity<GastoCajaChica>().HasIndex(g => g.Numero).IsUnique();
            modelBuilder.Entity<TomaFisica>().HasIndex(t => t.Numero).IsUnique();
            modelBuilder.Entity<DetalleTomaFisica>().HasIndex(d => d.TomaFisicaId);
            modelBuilder.Entity<DetalleTomaFisica>().Property(d => d.StockSistema).HasPrecision(18, 4);
            modelBuilder.Entity<DetalleTomaFisica>().Property(d => d.StockContado).HasPrecision(18, 4);
            modelBuilder.Entity<DetalleTomaFisica>().Property(d => d.CostoUnitario).HasPrecision(18, 4);
            modelBuilder.Entity<AsientoContable>().HasIndex(a => a.Numero).IsUnique();
            modelBuilder.Entity<DetalleAsiento>().HasIndex(d => d.AsientoContableId);
            modelBuilder.Entity<DetalleAsiento>().HasIndex(d => d.PlanCuentaId);
            modelBuilder.Entity<Moneda>().HasIndex(m => m.Codigo).IsUnique();
            modelBuilder.Entity<Proveedor>().HasIndex(p => p.Codigo).IsUnique();
            modelBuilder.Entity<OrdenCompra>().HasIndex(o => o.Numero).IsUnique();
            modelBuilder.Entity<DevolucionProveedor>().HasIndex(d => d.Numero).IsUnique();

            modelBuilder.Entity<CuentaFinanciera>().HasIndex(c => c.Codigo).IsUnique();
            modelBuilder.Entity<MovimientoFinanciero>().HasIndex(m => m.Numero).IsUnique();
            modelBuilder.Entity<CierreCaja>().HasIndex(c => c.Numero).IsUnique();
            modelBuilder.Entity<CierreCajaConteo>().HasIndex(c => c.CierreCajaId);
            modelBuilder.Entity<VentaOffline>().HasIndex(v => v.OfflineId).IsUnique();
            modelBuilder.Entity<PedidoWeb>().HasIndex(p => p.Numero);
            modelBuilder.Entity<PedidoWeb>().HasIndex(p => new { p.Ip, p.Fecha });
            modelBuilder.Entity<AperturaCaja>().HasIndex(a => a.Numero).IsUnique();

            modelBuilder.Entity<CierreContable>()
                .HasIndex(c => new { c.Anio, c.Mes })
                .IsUnique();

            modelBuilder.Entity<ConciliacionBancaria>().HasIndex(c => c.Numero).IsUnique();
            modelBuilder.Entity<PlanCuenta>().HasIndex(c => c.Codigo).IsUnique();

            modelBuilder.Entity<Empleado>().HasIndex(e => e.Codigo).IsUnique();
            modelBuilder.Entity<VacacionEmpleado>().HasIndex(v => v.Numero).IsUnique();
            modelBuilder.Entity<PermisoEmpleado>().HasIndex(p => p.Numero).IsUnique();
            modelBuilder.Entity<ValeEmpleado>().HasIndex(v => v.Numero).IsUnique();
            modelBuilder.Entity<Nomina>().HasIndex(n => n.Numero).IsUnique();
            modelBuilder.Entity<PagoNominaEmpleado>().HasIndex(p => p.Numero).IsUnique();

            modelBuilder.Entity<ConfiguracionDeduccion>().HasIndex(c => c.Anio).IsUnique();

            modelBuilder.Entity<Envio>().HasIndex(e => e.Numero).IsUnique();
            modelBuilder.Entity<Ruta>().HasIndex(r => r.Numero).IsUnique();
            modelBuilder.Entity<Ruta>().HasIndex(r => r.TrackingCode).IsUnique();
            modelBuilder.Entity<Repartidor>().HasIndex(r => r.Codigo).IsUnique();
            modelBuilder.Entity<ZonaEnvio>().HasIndex(z => z.Nombre).IsUnique();
        }
    }
}