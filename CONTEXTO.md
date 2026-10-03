# KIRKENTA ERP — CONTEXTO MAESTRO

Cómo usar: Pega este archivo completo al inicio de un chat nuevo. Di: "Retomamos Kirkenta ERP. Contexto: [pegas esto]". El asistente retomará al instante sin explicaciones adicionales.

================================================================================
1. STACK TÉCNICO
================================================================================

- Framework: ASP.NET Core Razor Pages
- .NET: net10.0
- ORM: Entity Framework Core 10
- Base de datos: MySQL (Pomelo.EntityFrameworkCore.MySql)
- Autenticación: Cookies (esquema "KirkentaAuth")
- Sesión: "KirkentaSession" (30 min)
- Frontend: Bootstrap 5 + CSS custom (wwwroot/css/theme.css)
- Charts: Chart.js 4.4.0 (CDN jsdelivr)
- Excel: ClosedXML
- CSV: custom (sin dependencias)
- Git: https://github.com/Kirklen0910/Kirkenta

================================================================================
2. ESTRUCTURA DE CARPETAS
================================================================================

Kirkenta/
├── Data/ApplicationDbContext.cs
├── Helpers/
│   ├── Export/ (ExcelExporter, CsvExporter, JsonExporter, ExportColumn, ExportColumns)
│   ├── Import/ (ExcelImporter, CsvImporter, ColumnMapper, ImportResult)
│   ├── Finanzas/ (AperturaHelper, CierreHelper, DistribucionHelper, MovimientoAutomaticoHelper, SaldoHelper, AdjuntoCierreHelper, FinanzasSeeder)
│   ├── ActividadHelper.cs
│   ├── ModulosERP.cs
│   ├── NumeroDocumentoHelper.cs
│   ├── PermisoHelper.cs
│   └── PermisoSeeder.cs
├── Migrations/
├── Models/
├── Pages/
│   ├── Auth/
│   ├── Usuarios/
│   ├── POS/
│   ├── Ventas/
│   ├── Clientes/
│   ├── Cotizaciones/
│   ├── Pedidos/
│   ├── Facturas/
│   ├── Devoluciones/
│   ├── Productos/
│   ├── Categorias/
│   ├── UnidadesMedida/
│   ├── Bajas/
│   ├── Compras/
│   ├── Proveedores/
│   ├── OrdenesCompra/
│   ├── PagosProveedor/
│   ├── Finanzas/ (Index, Cuentas, Categorias, Movimientos, Aperturas, Cierres, Reportes)
│   ├── Reportes/
│   └── Shared/_Layout.cshtml
├── wwwroot/ (css, js, images, lib, uploads)
├── Program.cs
├── appsettings.json
└── CONTEXTO.md

================================================================================
3. MÓDULOS Y SUBMÓDULOS
================================================================================

Diccionario en Helpers/ModulosERP.cs:

Usuarios: Index, Create, Edit, Delete, Roles, RolesCreate, RolesEdit, RolesDelete, Perfil, CambiarPassword, Actividad
Ventas: POS, Index, Cotizaciones, Pedidos, Facturas, Clientes (+Create/Edit/Delete/Export/Import), Devoluciones
Inventario: Productos (+Create/Edit/Delete/Export/Import), Categorias, UnidadesMedida, Bajas
Compras: Proveedores (+Create/Edit/Delete/Export/Import), Ordenes (+Create/Edit/Delete/Recibir), Pagos (+Create), CuentasPorPagar, Reportes
Finanzas: Cuentas (+Create/Edit/Delete), Categorias (+Create/Edit/Delete), Movimientos (+Create/Edit/Anular), Aperturas (+Create), Cierres (+Create/Aprobar), Reportes
RRHH: Index, Empleados, Nomina, Vales (PENDIENTE)
Reportes: Ventas, Compras
Configuracion: Empresa, Nomenclatura, Series, MetodosPago

================================================================================
4. PERMISOS
================================================================================

Tabla Permisos: RolId, Modulo, Submodulo (nullable), PuedeVer, PuedeCrear, PuedeEditar, PuedeEliminar
Índice único en (RolId, Modulo, Submodulo)
Submodulo = null → permiso del módulo padre
Admin siempre tiene acceso total
Pendiente = usuario sin rol

Helpers:
- PermisoHelper.TienePermiso(context, rol, modulo, submodulo, "ver|crear|editar|eliminar")
- PermisoHelper.ModulosVisibles(context, rol)
- PermisoHelper.SubmodulosVisibles(context, rol, modulo)
- PermisoSeeder.MigrarPermisosFaltantes(db) → ejecutado al arrancar

Convención:
- Padres: "Cuentas", "Movimientos", "Cierres", "Clientes", "Productos"
- Hijos: "CuentasCreate", "CierresAprobar", etc.

================================================================================
5. HELPERS CLAVE
================================================================================

NumeroDocumentoHelper
- GenerarSiguiente(context, "Venta") → genera y avanza
- PreviewSiguiente(context, "Venta") → solo muestra
- Tipos: Venta, Factura, Cotizacion, Pedido, Devolucion, OrdenCompra, PagoProveedor, DevolucionProveedor, Proveedor, Producto, Baja, MovimientoFinanciero, ValeEmpleado, CierreCaja, AperturaCaja

ActividadHelper
- Registrar(context, usuarioId, "acción", "detalle", ipAddress)
- Nunca lanza excepción

SaldoHelper
- Aplicar(context, movimiento) → ajusta saldo de cuentas
- Revertir(context, movimiento) → revierte al anular

MovimientoAutomaticoHelper
- RegistrarIngresoVenta(context, ventaId, numeroVenta, monto, usuarioId, formaPago, cuentaId)
- RegistrarEgresoPagoProveedor(context, pagoId, numeroPago, monto, usuarioId, nombreProveedor, formaPago)
- RegistrarEgresoNomina(context, nominaId, numeroNomina, monto, usuarioId)

AperturaHelper
- Abrir(context, cuentaId, saldoInicial, notas, usuarioId, fechaManual)
- ObtenerAperturaActiva(context, cuentaId)
- SugerirSaldoInicial(context, cuentaId) → basado en último cierre
- Cerrar(context, aperturaId, cierreId)

CierreHelper
- Calcular(context, cuentaId, fecha)
- Registrar(context, cuentaId, fecha, efectivoContado, notas, tolerancia, usuarioId)
- DeterminarResultado(diferencia, tolerancia) → Cuadrado/Sobrante/Faltante

DistribucionHelper
- RegistrarDistribuciones(context, cierreId, cuentaOrigenId, efectivoContado, distribuciones, usuarioId)
- ObtenerDistribuciones(context, cierreId)
- Tipos: RetiroBanco (Transferencia), FondoCaja (sin movimiento), EntregaAdmin (Egreso), PagoDirecto (Egreso), Otro (Egreso)

AdjuntoCierreHelper
- Guardar(context, env, cierreId, archivo, descripcion, usuarioId)
- Eliminar(context, env, adjuntoId)
- Extensiones: .pdf, .jpg, .jpeg, .png. Máx 10 MB

Export
- ExportColumns.Clientes() / .Productos(cats, imps) / .Proveedores()
- ExcelExporter.Export(items, columns, "Sheet", "Título") → byte[]
- CsvExporter.Export(items, columns) → byte[]
- JsonExporter.Export(items) → byte[]

Import
- ExcelImporter.Read(stream) → ImportFile
- CsvImporter.Read(stream) → ImportFile
- ImportFile: Headers (List<string>), Rows (List<Dictionary<string,string>>)
- ColumnMapper.Detectar(headers, aliasPorCampo) → Dictionary<string, string?>
- Wizard: /[Modulo]/Import → /[Modulo]/ImportMap → confirmar → Index

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

Actividad (siempre registrar acciones importantes):
ActividadHelper.Registrar(_context, currentUser?.Id ?? 0, "Verbo + objeto", $"Detalle L. {monto:N2}", HttpContext.Connection.RemoteIpAddress?.ToString());

================================================================================
7. REGLAS CRÍTICAS
================================================================================

1. SIEMPRE entregar archivos COMPLETOS, nunca fragmentos.
2. Automatizar pero permitir modo manual (checkbox "Registrar en Finanzas").
3. Movimientos NO se borran, se ANULAN.
4. Cierres requieren APROBACIÓN.
5. Nomenclatura personalizable desde /Nomenclatura.
6. Si import trae SKU/Código → respetar. Si vacío → autogenerar.
7. Apertura de caja OBLIGATORIA antes de vender en POS.
8. Al cerrar → se cierra la apertura. Al día siguiente se sugiere saldo del último cierre.
9. Distribución del efectivo: suma DEBE igualar efectivo contado.
10. Cada distribución genera su movimiento (excepto FondoCaja).

================================================================================
8. MÓDULOS COMPLETADOS
================================================================================

Core:
- Auth (Login, Register, Logout)
- Usuarios + Roles + Permisos (CRUD completo)
- Perfil, Cambiar contraseña, Actividad
- Permisos con seeder automático
- Menú lateral con iconos + permisos dinámicos

Ventas:
- POS completo (búsqueda, carrito, cobro, modal)
- Clientes (CRUD + Import + Export)
- Cotizaciones, Pedidos, Facturas, Devoluciones

Inventario:
- Productos (CRUD + Import + Export)
- Categorías, Unidades de medida
- Bajas (con aprobación y reversión)

Compras:
- Proveedores (CRUD + Import + Export)
- Órdenes de compra (Create, Edit, Recibir)
- Pagos a proveedores + Cuentas por pagar
- Reportes de compras

Finanzas:
- Dashboard con gráficos
- Cuentas financieras (Cajas + Bancos)
- Categorías financieras
- Movimientos (Ingresos, Egresos, Transferencias, Anulaciones)
- Reportes (Flujo, Resultados, Estado de cuenta)
- Aperturas de caja
- Cierres con distribución del efectivo
- Aprobación de cierres
- Adjuntos / comprobantes
- Integración automática desde POS y PagosProveedor

Configuración:
- Datos de la empresa, Nomenclatura, Métodos de pago, Impuestos

================================================================================
9. MÓDULOS PENDIENTES
================================================================================

- RRHH: Empleados, Nómina, Vales/Adelantos
- Import/Export de Categorías, Impuestos, Unidades de medida
- Refactor del wizard a componente genérico
- Conciliación bancaria
- ISV/IVA automático
- Presupuestos por categoría
- Cierre contable mensual

================================================================================
10. NOTAS IMPORTANTES
================================================================================

Sobre migraciones:
- ApplicationDbContextModelSnapshot está DESINCRONIZADO con la BD
- Al generar migración, EF intenta crear TODAS las tablas
- Solución actual: SQL manual + INSERT en __EFMigrationsHistory

Sobre OneDrive:
- Proyecto en C:\Users\crobe\OneDrive\Kirkenta
- Archivos son symlinks de OneDrive
- Recomendado mover a C:\Dev\Kirkenta

Sobre .gitignore:
- Excluye: bin/, obj/, .vs/, .vscode/, *.lnk, appsettings.Development.json
- Excluye carpetas del proyecto PHP viejo: app/, config/, public/, storage/, vendor/, includes/, modules/, backups/, logs/, assets/, uploads/

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
2. Guardar
3. git add . && git commit -m "..." && git push

Al pedir código:
- Archivos COMPLETOS, no fragmentos
- Nombre del archivo + ruta
- Un archivo por mensaje (o agrupados si son cortos)

================================================================================
ÚLTIMA ACTUALIZACIÓN
================================================================================

Fecha: 03/10/2026
Estado: Finanzas 100% completado (Aperturas, Cierres, Distribución, Aprobación, Adjuntos)
Próximo: RRHH (Empleados + Nómina + Vales) o probar flujo completo de Finanzas