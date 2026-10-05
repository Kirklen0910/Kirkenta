# KIRKENTA ERP — CONTEXTO MAESTRO

Cómo usar: Pega este archivo completo al inicio de un chat nuevo, JUNTO con los snapshots que correspondan. Di: "Retomamos Kirkenta ERP. Contexto: [pegas esto]". El asistente retomará al instante sin explicaciones adicionales.

================================================================================
0. SISTEMA DE SNAPSHOTS (4 PARTES)
================================================================================

El proyecto está dividido en 4 snapshots generados automaticamente por
.\generar-snapshot.ps1. El script tiene auto-descubrimiento: NO hay que
editarlo cuando se crean modulos nuevos.

ARCHIVOS GENERADOS:
- PROYECTO_PARTE_1_Nucleo.md       (~900 KB, ~183 archivos)
- PROYECTO_PARTE_2_Operaciones.md  (~1.3 MB, ~201 archivos)
- PROYECTO_PARTE_3_Soporte.md      (~1 MB, ~112 archivos)
- PROYECTO_PARTE_4_Extras.md       (~10 KB, modulos nuevos)

CONTENIDO DE CADA PARTE:
- PARTE 1 - Nucleo: Data, Models, Migrations, Helpers base (Actividad,
  ModulosERP, NumeroDocumento, Permiso, PermisoSeeder, Export, Import),
  Pages/Auth, Pages/Usuarios, Pages/Configuracion, Pages/Nomenclatura,
  Pages/Series, Pages/MetodosPago, Pages/Impuestos, Pages/UnidadesMedida,
  Pages/Shared (_Layout, _ValidationScriptsPartial, _ImportStepIndicator,
  _ImportUploadForm), Pages raiz (_ViewImports, _ViewStart, Error, Index,
  Privacy), Program.cs, appsettings.json, Kirkenta.csproj, .gitignore,
  Properties/, wwwroot/css, wwwroot/js
- PARTE 2 - Operaciones: Helpers/Finanzas, Pages/POS, Pages/Ventas,
  Pages/Clientes, Pages/Cotizaciones, Pages/Pedidos, Pages/Facturas,
  Pages/Devoluciones, Pages/Productos, Pages/Categorias, Pages/Bajas,
  Pages/Compras, Pages/Proveedores, Pages/OrdenesCompra, Pages/PagosProveedor,
  Pages/Finanzas, Pages/Shared/_LayoutPOS.cshtml
- PARTE 3 - Soporte: Helpers/Logistica, Helpers/RRHH, Pages/Logistica,
  Pages/RRHH, Pages/Reportes
- PARTE 4 - Extras: Cualquier modulo nuevo (Produccion, Activos,
  Presupuestos, etc.) + scripts auxiliares en tools/. El script lo detecta
  automaticamente y avisa.

REGLA DE USO EN CHAT NUEVO:
- SIEMPRE pega: Parte 1 + CONTEXTO.md
- Luego pega la parte del modulo que vas a tocar:
  * Ventas/Compras/Finanzas/Inventario -> Parte 2
  * Logistica/RRHH/Reportes            -> Parte 3
  * Modulo nuevo (Produccion, etc.)    -> Parte 4
  * Si toca todo                       -> las 4 partes

EJEMPLO DE MENSAJE AL INICIAR CHAT:

Retomamos Kirkenta ERP.

Contexto Parte 1:
[pegas PROYECTO_PARTE_1_Nucleo.md]

Contexto Parte 3:
[pegas PROYECTO_PARTE_3_Soporte.md]

CONTEXTO.md:
[pegas CONTEXTO.md]

Vamos a trabajar en: [lo que quieras hacer]

LO QUE NO ESTA EN LOS SNAPSHOTS (a proposito):
- bin/, obj/, .vs/, .vscode/, node_modules/, .git/ (compilados y sistema)
- uploads/ (imagenes y PDFs de usuarios)
- appsettings.Development.json (contiene password real)
- CONTEXTO.md (lo pegas aparte, es este archivo)
- generar-snapshot.ps1 (evita recursion)
- Los propios PROYECTO_PARTE_*.md (evita recursion)

================================================================================
1. STACK TECNICO
================================================================================

- Framework: ASP.NET Core Razor Pages
- .NET: net10.0 (TargetFramework: net10.0)
- ORM: Entity Framework Core 9.0.0
- Base de datos: MySQL (Pomelo.EntityFrameworkCore.MySql 9.0.0)
- Autenticacion: Cookies (esquema "KirkentaAuth", 8 horas, sliding)
- Sesion: "KirkentaSession" (30 min, para wizard de importacion)
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
├── Migrations/ (desincronizado, ver seccion 10)
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
│   ├── Logistica/  <- EN DESARROLLO
│   │   ├── Index (Dashboard con alertas)  OK COMPLETADO
│   │   ├── Envios/
│   │   │   ├── Index  OK COMPLETADO
│   │   │   └── Details  OK COMPLETADO
│   │   ├── Rutas/
│   │   │   ├── Index  OK COMPLETADO
│   │   │   ├── Create  OK COMPLETADO
│   │   │   ├── Details  OK COMPLETADO
│   │   │   └── Edit  OK COMPLETADO
│   │   ├── Zonas/
│   │   │   ├── Index  OK COMPLETADO
│   │   │   ├── Create  OK COMPLETADO
│   │   │   ├── Edit  OK COMPLETADO
│   │   │   └── Delete  OK COMPLETADO
│   │   ├── Repartidores/
│   │   │   ├── Index  OK COMPLETADO
│   │   │   ├── Create  OK COMPLETADO
│   │   │   ├── Edit  OK COMPLETADO
│   │   │   └── Delete  OK COMPLETADO
│   │   ├── Tracking/
│   │   │   └── Index (publico, AllowAnonymous)  OK COMPLETADO
│   │   └── Reportes/ (Index)  <- PENDIENTE (ultimo archivo del Bloque 5)
│   ├── Reportes/ (Index, Ventas, Compras)
│   ├── Configuracion/ (Index, Edit)
│   ├── MetodosPago/ (Index, Create, Edit, Delete)
│   ├── Nomenclatura/ (Index)
│   ├── Series/ (Index, Create, Edit, Delete)
│   └── Shared/
│       ├── _Layout.cshtml  OK ACTUALIZADO (Logistica completa)
│       ├── _LayoutPOS.cshtml
│       ├── _ValidationScriptsPartial.cshtml
│       ├── _ImportStepIndicator.cshtml
│       └── _ImportUploadForm.cshtml
├── wwwroot/ (css, js, images, lib, uploads)
├── Program.cs  ⚠ ACTUALIZAR AllowAnonymousToPage
├── appsettings.json
├── CONTEXTO.md
├── PROYECTO_PARTE_1_Nucleo.md
├── PROYECTO_PARTE_2_Operaciones.md
├── PROYECTO_PARTE_3_Soporte.md
├── PROYECTO_PARTE_4_Extras.md
└── generar-snapshot.ps1

================================================================================
3. MODULOS Y SUBMODULOS
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
- Clientes, Productos, Proveedores: ya tenian wizard (refactorizado a generico)
- Categorias, Impuestos, Unidades de medida: wizard AGREGADO
- Todos usan ImportWizardHelper + vistas parciales compartidas

NOTA sobre Logistica (sesion 5 - 04/10/2026):
- Bloque 1 COMPLETADO: AlertaEnvioHelper + Logistica/Index (dashboard con alertas)
- Bloque 2 COMPLETADO: Envios/Index + Envios/Details
- Bloque 3 COMPLETADO: Rutas/Index + Rutas/Create + Rutas/Details + Rutas/Edit
- Bloque 4 COMPLETADO: Zonas (Index/Create/Edit/Delete) + Repartidores (Index/Create/Edit/Delete)
- Bloque 4 COMPLETADO: _Layout.cshtml con submenu Logistica completo
- Bloque 5 EN PROGRESO: Tracking/Index OK COMPLETADO
- Bloque 5 PENDIENTE: Reportes/Index (ultimo archivo)

================================================================================
4. PERMISOS
================================================================================

Tabla Permisos: RolId, Modulo, Submodulo (nullable), PuedeVer, PuedeCrear, PuedeEditar, PuedeEliminar
Indice unico en (RolId, Modulo, Submodulo)
Submodulo = null -> permiso del modulo padre
Admin siempre tiene acceso total (bypass en PermisoHelper)
Pendiente = usuario sin rol

Helpers (PermisoHelper):
- TienePermiso(context, rol, modulo, submodulo, "ver|crear|editar|eliminar")
- ModulosVisibles(context, rol) -> modulos con PuedeVer (padre o algun hijo)
- SubmodulosVisibles(context, rol, modulo) -> submodulos con PuedeVer

PermisoSeeder.MigrarPermisosFaltantes(db):
- Se ejecuta al arrancar la app (Program.cs)
- Para cada rol (excepto Admin), crea permisos faltantes copiando del modulo padre
- Idempotente

Convencion de nombres:
- Modulos padre: "Clientes", "Productos", "Cuentas", "Movimientos", "Cierres", "Empleados", "Vales", "Nomina", "Logistica", etc.
- Submodulos (con accion): "ClientesCreate", "ClientesEdit", "ClientesDelete", "CuentasCreate", "CierresAprobar", "ValesEntregar", "NominaPagar", "EnviosDetails", "RutasCreate", "ZonasCreate", "RepartidoresCreate"
- Genericos: "Index", "Create", "Edit", "Delete"

================================================================================
5. HELPERS CLAVE
================================================================================

NumeroDocumentoHelper
- GenerarSiguiente(context, tipo) -> genera numero y avanza el correlativo
- PreviewSiguiente(context, tipo) -> solo muestra el proximo
- SincronizarSerie() -> ajusta el correlativo si detecta desfase con la BD
- Soporta formato personalizado: {PREFIX}, {SUFFIX}, {SEP}, {NUM}, {YEAR}, {MONTH}, {DAY}
- Tipos soportados: Cotizacion, Pedido, Venta, Factura, Devolucion, Producto, Baja, Proveedor, OrdenCompra, PagoProveedor, DevolucionProveedor, MovimientoFinanciero, AperturaCaja, CierreCaja, ConciliacionBancaria, Empleado, ValeEmpleado, Vacacion, PermisoEmpleado, Nomina, PagoNomina, Envio, Ruta, Repartidor

ActividadHelper
- Registrar(context, usuarioId, "accion", "detalle", ipAddress, userAgent)
- Nunca lanza excepcion (silencioso si falla)

SaldoHelper (Finanzas)
- Aplicar(context, movimiento) -> ajusta saldo de cuentas
- Revertir(context, movimiento) -> revierte al anular

MovimientoAutomaticoHelper (Finanzas)
- RegistrarIngresoVenta / RegistrarEgresoPagoProveedor / RegistrarEgresoNomina
- Resuelve cuenta y categoria por defecto si no se especifican

AperturaHelper (Finanzas)
- Abrir / ObtenerAperturaActiva / HayAperturaActiva / Cerrar

CierreHelper (Finanzas)
- Calcular / Registrar / DeterminarResultado
- Genera ajuste automatico si hay diferencia

DistribucionHelper (Finanzas)
- RegistrarDistribuciones / ObtenerDistribuciones
- Tipos: RetiroBanco, FondoCaja, EntregaAdmin, PagoDirecto, Otro

AdjuntoCierreHelper (Finanzas)
- Guardar / Eliminar (pdf, jpg, jpeg, png, max 10 MB)

CierreContableHelper (Finanzas)
- ValidarFecha / Cerrar / Reabrir / ObtenerEstadoAnual / Obtener

ConciliacionHelper (Finanzas)
- Crear / CargarLineasSistema / MatchingAutomatico / MatchingManual
- DeshacerMatch / RecalcularTotales / Cerrar / Cancelar

ContabilidadHelper (Finanzas)
- GenerarEstadoResultados / GenerarBalanceGeneral / ContarSinPlanCuenta

ISVHelper (Finanzas)
- Calcular / ObtenerTasaProducto / SincronizarItems / RecalcularFactura / ObtenerTasasActivas
- NO hardcodea tasas: siempre lee del catalogo Impuestos
- Soporta montoEnvio (el envio se grava con la tasa predeterminada)

PlanCuentasHelper (Finanzas)
- Seed (81 cuentas estandar Honduras) / ObtenerTodas / ObtenerCuentasMovimiento / RecalcularJerarquia

EmpleadoHelper (RRHH)
- GenerarCodigo / PreviewCodigo
- CalcularAniosAntiguedad / CalcularMesesAntiguedad
- CalcularDiasVacacionesPorAntiguedad (lee ConfiguracionEmpresa.RHTablaVacaciones)
- ProcesarAcumulacionVacaciones / ProcesarAcumulacionGlobal
- ObtenerFeriados

AdjuntoEmpleadoHelper (RRHH)
- Guardar / GuardarFotoPerfil / Eliminar / ActualizarVigencias
- Extensiones: pdf, jpg, jpeg, png, docx, doc. Max 15 MB (foto 5 MB)

NominaHelper (RRHH)
- ObtenerConfiguracion (crea config por defecto si no existe)
- CalcularISRMensual / CalcularISRAcumulativo
- CalcularIHSS / CalcularRAP
- CalcularSalarioProporcional
- CalcularPeriodo / DiasPeriodo
- ObtenerValesActivos / CalcularDescuentoVales
- CalcularDetalleEmpleado

VacacionHelper (RRHH)
- CalcularDias (excluye fines de semana y feriados del pais)
- Solicitar / Aprobar / Rechazar

EnvioHelper (Logistica)
- CrearParaVenta(context, venta, direccion, referencia, contactoNombre, contactoTelefono, ciudad, zonaId, monto, usuarioId) -> Envio
- CrearParaCotizacion / CrearParaPedido / CrearParaFactura (mismos parametros)
- MarcarEntregado(context, envioId, nombreRecibio, firmaImagen, fotoEntrega, notas, usuarioId) -> (ok, error)
- MarcarFallido(context, envioId, motivoFallo, usuarioId) -> (ok, error)
- Reagendar(context, envioId, notas, usuarioId) -> (ok, error)
- ObtenerPendientesSinRuta(context) -> List<Envio>
- GenerarTrackingCode() -> string (formato KRT-2026-AB12X9)

RutaHelper (Logistica)
- Crear(context, envioIds, repartidorId, repartidorNombre, vehiculo, placa, zonaId, descripcion, fechaEntregaEstimada, notas, usuarioId) -> (ruta, error)
- Despachar(context, rutaId, usuarioId) -> (ok, error)
- Cerrar(context, rutaId, notas, usuarioId) -> (ok, error)
- Cancelar(context, rutaId, motivo, usuarioId) -> (ok, error)
- AgregarEnvio(context, rutaId, envioId) -> (ok, error)
- QuitarEnvio(context, rutaId, envioId) -> (ok, error)
- Reordenar(context, rutaId, envioIdsEnOrden) -> (ok, error)
- ObtenerEnviosDeRuta(context, rutaId) -> List<Envio>
- RecalcularTotales(context, rutaId)

ZonaEnvioSeeder (Logistica)
- Seed(context) -> crea 5 zonas tipicas de Honduras
  (Centro/Casco Urbano L.50, Cercana L.80, Media L.150, Lejana L.300, Envio Gratis L.0)
- Idempotente

AlertaEnvioHelper (Logistica)
- CalcularSemaforo(envio) -> "Verde" | "Amarillo" | "Rojo"
  - Entregado -> Verde
  - Fallido -> Rojo
  - Cancelado -> Verde (no alerta)
  - Pendiente/EnRuta + fecha estimada > hoy -> Verde
  - Pendiente/EnRuta + fecha estimada == hoy -> Amarillo
  - Pendiente/EnRuta + fecha estimada < hoy -> Rojo
- EtiquetaSemaforo(semaforo) -> string con emoji + texto
- DiasDiferencia(envio) -> int? (positivo = atrasado, negativo = dias por venir)
- ObtenerEnviosAtrasados(context) -> List<Envio>
- ObtenerEnviosVencenHoy(context) -> List<Envio>
- ObtenerEnviosProximosAVencer(context, dias = 2) -> List<Envio>
- ObtenerEnviosFallidos(context) -> List<Envio>
- ObtenerEnviosEntregadosHoy(context) -> List<Envio>
- ObtenerRutasAtrasadas(context) -> List<Ruta> (EnReparto con paradas vencidas)
- ObtenerRutasDespachadasHoy(context) -> List<Ruta>
- ObtenerResumenAlertas(context) -> ResumenAlertas { EnviosAtrasados, EnviosVencenHoy, EnviosProximosAVencer, EnviosFallidos, EnviosEntregadosHoy, RutasActivas, RutasAtrasadas, RutasDespachadasHoy, HayAlertas }
- TODO SE CALCULA EN TIEMPO REAL, no persiste nada

Export
- ExportColumns.Clientes() / .Productos(cats, imps) / .Proveedores()
- ExportColumns.Categorias() / .Impuestos() / .UnidadesMedida()
- ExcelExporter.Export / CsvExporter.Export / JsonExporter.Export

Import (wizard generico) - REFACTORIZADO 04/10/2026
- ImportWizardHelper.ProcesarArchivo(archivo) -> ResultadoLectura
- ImportWizardHelper.GuardarEnSesion / RecuperarDeSesion / LimpiarSesion
- ParserHelper.ParsearBool / ParsearInt / ParsearDecimal
- CampoMapeo: { Key, Label, Requerido, Ayuda }
- ImportWizardConfig: { NombrePlural, NombreSingular, ModuloPermiso, SubmoduloPermiso,
                        SessionKey, UrlIndex, UrlImport, UrlImportMap,
                        Campos, AliasCampos, CamposRequeridos }
- Vistas parciales: _ImportStepIndicator, _ImportUploadForm
- Wizard en uso: Clientes, Productos, Proveedores, Categorias, Impuestos, UnidadesMedida
- ExcelImporter.Read / CsvImporter.Read -> ImportFile
- ColumnMapper.Detectar(headers, aliasPorCampo) -> Dictionary<string, string?>
- Modos de importacion: "upsert" | "crear" | "actualizar"

================================================================================
6. CONVENCIONES
================================================================================

Nombres:
- Modelos y propiedades en espanol (Producto, PrecioVenta)
- Namespace: Kirkenta.Pages.[Modulo] o Kirkenta.Pages.[Modulo].[Sub]
- PageModels: IndexModel, CreateModel, EditModel, DeleteModel, DetailsModel

Patron de permisos en code-behind:
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
- TempData["Success"] -> banner verde
- TempData["Error"] -> banner rojo
- TempData["Warning"] -> banner amarillo

Actividad:
ActividadHelper.Registrar(_context, currentUser?.Id ?? 0, "Verbo + objeto", $"Detalle L. {monto:N2}", HttpContext.Connection.RemoteIpAddress?.ToString());

Estructura tipica de pagina:
- Vista .cshtml con layout principal (_Layout)
- POS usa layout propio (_LayoutPOS)
- Tracking publico usa Layout = null con diseno propio standalone
- Cabecera con breadcrumb (automatica desde _Layout)
- `module-header` con titulo + botones de accion
- Banner TempData Success/Error
- KPIs en `kpi-grid`
- Contenido principal en `module-card`
- Modales con estilo `.pos-modal` reutilizado

Convencion de semaforos (Logistica):
- Verde -> completado o a tiempo
- Amarillo -> vence hoy
- Rojo -> atrasado o fallido
- Los KPIs clickeables usan `asp-route-filtro="atrasados|vencenHoy|pendientes|enruta|entregados|fallidos|sinRuta"` (envios) o `asp-route-filtro="borrador|enreparto|completadas|atrasadas"` (rutas)

Convencion de modales:
- Se abren con clase `is-open` (no display: inline)
- Cierre con funcion JS `cerrarModal(id)` que remueve `is-open`
- Estructura: `.pos-modal` > `.pos-modal-content` > header/body/footer

Convencion de validacion "no eliminar si tiene dependencias":
- Zonas: no se puede eliminar si tiene envios asociados (`Envios.ZonaId`)
- Repartidores: no se puede eliminar si tiene rutas o envios asignados (`Rutas.RepartidorId`, `Envios.RepartidorId`)
- La vista muestra el bloqueo con opcion "Ir a Editar para desactivar"

================================================================================
7. REGLAS CRITICAS
================================================================================

1. SIEMPRE entregar archivos COMPLETOS, nunca fragmentos.
2. Automatizar pero permitir modo manual (checkbox "Registrar en Finanzas").
3. Movimientos NO se borran, se ANULAN.
4. Cierres requieren APROBACION.
5. Nomenclatura personalizable desde /Nomenclatura -> /Series.
6. Si import trae SKU/Codigo -> respetar. Si vacio -> autogenerar.
7. Apertura de caja OBLIGATORIA antes de vender en POS.
8. Al cerrar -> se cierra la apertura. Al dia siguiente se sugiere saldo del ultimo cierre.
9. Distribucion del efectivo: suma DEBE igualar efectivo contado.
10. Cada distribucion genera su movimiento (excepto FondoCaja).
11. Nomina: flujo Calculada -> Aprobada -> Pagada (con pagos individuales o en lote).
12. Vales: doble aprobacion (Gerente + RRHH) -> Entregado -> Descontado en nomina.
13. Vacaciones: calculo excluye fines de semana y feriados del pais configurado.
14. Salario proporcional: se calcula sobre 30 dias (base mensual estandar).
15. ISR Honduras: metodo acumulativo anual por defecto (SAR).
16. Vales: limite del 50% del salario mensual del empleado.
17. Nomina: no se puede editar si tiene recepciones o pagos. Se anula.
18. OrdenCompra: no se puede editar si tiene recepciones o pagos. Estados: Borrador -> Enviada -> RecibidaParcial -> Recibida -> Pagada.
19. Periodo contable cerrado: bloquea movimientos, aperturas, cierres, ventas, pagos, nominas y vales.
20. Cierre contable reabrible: requiere motivo + contrasena, queda en auditoria.
21. Conciliacion: solo cuentas tipo "Banco". Matching automatico por monto (+/-0.01) y fecha (+/-3 dias).
22. Plan de Cuentas: codigo jerarquico con puntos (1, 1.1, 1.1.01). Naturaleza calculada por tipo.
23. Solo cuentas con EsMovimiento=true aceptan movimientos directos.
24. Wizard de importacion GENERICO: usar ImportWizardHelper + vistas parciales. NO duplicar logica.
25. Imports con codigos correlativos: pre-cargar codigos existentes en HashSet<string>, mantener correlativo local en memoria, UN solo SaveChanges al final. NUNCA llamar a NumeroDocumentoHelper.GenerarSiguiente por fila.
26. EF Core 9 en .NET 10: NUNCA usar string[] con .Contains() dentro de queries EF. Usar HashSet<string> o List<string>. El array rompe el ExpressionTreeFuncletizer (TypeLoadException con ReadOnlySpan).
27. Truncar campos de texto antes de insertar en importaciones (Nombre->150, SKU->50, Codigo->20, etc.).

REGLAS DE LOGISTICA:
28. Los envios nacen al facturar una venta con "RequiereEnvio=true". El POS ya llama a EnvioHelper.CrearParaVenta automaticamente.
29. Los envios NO se borran. Se marcan Entregado/Fallido/Cancelado.
30. Para marcar Entregado es OBLIGATORIO el nombre de quien recibe.
31. Para marcar Fallido es OBLIGATORIO el motivo.
32. Un envio Fallido se puede REAGENDAR (vuelve a Pendiente, sin ruta).
33. Las rutas agrupan envios Pendientes. Estados: Borrador -> EnReparto -> Completada (o Cancelada).
34. Solo se pueden editar/quitar envios de rutas en estado Borrador.
35. Al despachar una ruta, TODOS sus envios pasan a estado EnRuta y se les asigna el repartidor/vehiculo.
36. Cuando TODAS las paradas de una ruta se resuelven (Entregado o Fallido), la ruta se cierra automaticamente (Completada).
37. Las alertas de envios/rutas se calculan EN TIEMPO REAL con AlertaEnvioHelper, no se persisten.
38. El semaforo se calcula: Entregado->Verde, Fallido->Rojo, Cancelado->Verde, Pendiente/EnRuta con fecha>hoy->Verde, ==hoy->Amarillo, <hoy->Rojo.
39. Una ruta NO se puede quedar sin paradas. Si el usuario quiere vaciarla, debe CANCELARLA (los envios vuelven a Pendiente).
40. Al cancelar una ruta en Borrador, todos sus envios vuelven a estado Pendiente y se desligan de la ruta.
41. El tracking code se genera UNA VEZ al crear la ruta (formato KRT-2026-AB12X9), es unico y no cambia.
42. Las ZONAS no se eliminan si tienen envios asociados. Se recomienda desactivarlas.
43. Los REPARTIDORES no se eliminan si tienen rutas o envios asignados. Se recomienda desactivarlos.
44. El filtro de zona en envios usa `Envios.ZonaId`; el nombre se denormaliza en `Envio.ZonaNombre` al crear.
45. El tracking publico (/Logistica/Tracking/Index) es AllowAnonymous y NO usa _Layout (Layout = null con diseno propio).
46. En el tracking publico NO se exponen montos, IDs internos, notas privadas ni motivos de fallo internos.
47. La busqueda de tracking normaliza el codigo (uppercase + trim) para aceptar variaciones.

================================================================================
8. MODULOS COMPLETADOS
================================================================================

Core:
- Auth (Login, Register, Logout) con BCrypt
- Usuarios + Roles + Permisos (CRUD completo)
- Perfil, Cambiar contrasena, Actividad (con IP y UserAgent)
- Permisos con seeder automatico
- Menu lateral con iconos + permisos dinamicos
- Breadcrumb automatico desde ruta

Ventas:
- POS completo (busqueda, carrito, cobro, modal, integracion Finanzas con checkbox)
- POS crea envio automaticamente si "RequiereEnvio" esta marcado
- Clientes (CRUD + Import + Export con wizard)
- Cotizaciones (CRUD + conversion a factura y a POS)
- Pedidos (CRUD + estados)
- Facturas (Index, Details, RegistrarPago, CreateFromVenta)
- Devoluciones (Index)

Inventario:
- Productos (CRUD + Import + Export con wizard)
- Categorias (CRUD + Import + Export con wizard)
- Unidades de medida (CRUD + Import + Export con wizard)
- Bajas (con aprobacion y reversion + auditoria)
- Historial de compras por producto

Compras:
- Proveedores (CRUD + Import + Export con wizard)
- Ordenes de compra (Create, Edit, Recibir con recepcion parcial)
- Pagos a proveedores + Cuentas por pagar (con integracion Finanzas)
- Adjuntos a ordenes de compra (con validacion IDOR)
- Reportes de compras

Finanzas (100%):
- Dashboard con graficos
- Cuentas financieras (Cajas + Bancos)
- Categorias financieras (Ingreso/Egreso, +PlanCuentaId)
- Movimientos (Ingresos, Egresos, Transferencias, Anulaciones)
- Reportes (Flujo de caja, Estado de resultados, Estado de cuenta)
- Aperturas de caja
- Cierres con distribucion del efectivo
- Aprobacion de cierres
- Adjuntos / comprobantes
- Movimientos automaticos desde POS, PagosProveedor, Vales, Nomina
- Plan de Cuentas (81 cuentas Honduras)
- Estado de Resultados y Balance General
- Cierres Contables (bloqueo de meses + reapertura)
- Conciliacion Bancaria (matching automatico y manual)
- ISV Helper (dinamico por catalogo, soporta monto de envio)

RRHH (100%):
- Dashboard con KPIs + Cumpleanos + Aniversarios + Feriados + Vacaciones + Alertas
- Empleados: CRUD + foto + documentos + expediente + hoja de vida con 8 tabs
- Categorias de documentos con seeder
- Expedientes (llamados, amonestaciones, suspensiones, meritos)
- Vacaciones (excluye fines de semana y feriados) + descuento automatico de saldo
- Permisos y licencias con/sin goce de sueldo
- Vales con doble aprobacion -> Entregado -> Descontado en nomina
- Nomina completa: ISR + IHSS + RAP + vales + pago individual o en lote
- Feriados multi-pais (HN, GT, SV, CR, NI, PA, MX, US)
- Alertas personalizadas
- Reportes con Chart.js
- Integracion con Finanzas (egresos automaticos)

Logistica (EN DESARROLLO - 4.5/5 bloques completados):
- OK EnvioHelper, RutaHelper, ZonaEnvioSeeder, AlertaEnvioHelper
- OK Modelos: Envio, Ruta, RutaHistorial, Repartidor, ZonaEnvio
- OK DbContext con DbSets e indices unicos
- OK Program.cs ejecuta ZonaEnvioSeeder.Seed
- OK ModulosERP actualizado con submodulos de Logistica
- OK _Layout.cshtml con menu Logistica COMPLETO
- OK POS integrado (crea envio al cobrar con RequiereEnvio=true)
- OK ISVHelper soporta montoEnvio

Bloque 1 COMPLETADO - Dashboard:
- Pages/Logistica/Index - Dashboard con:
  - Banner de alertas criticas (rojo si hay atrasos/fallos/rutas en riesgo)
  - 4 KPIs de alertas con semaforo (atrasados, vencen hoy, entregados hoy, fallidos)
  - 4 KPIs generales (total envios, rutas activas, repartidores, zonas)
  - Accesos rapidos
  - Panel de envios atrasados (con dias de atraso)
  - Panel de rutas atrasadas (con progreso)
  - Panel doble vencen hoy + entregados hoy
  - Panel de fallidos
  - Ultimos envios y ultimas rutas

Bloque 2 COMPLETADO - Envios:
- Pages/Logistica/Envios/Index - Listado con:
  - 6 KPIs clickeables que aplican filtros por querystring
  - Filtros: busqueda, estado, zona, rango de fechas
  - Semaforo visual por fila
  - Columna de dias de atraso
  - Muestra ruta + numero de parada
  - Indicador de filtro activo con link para quitarlo
- Pages/Logistica/Envios/Details - Detalle con:
  - Banner rojo si atrasado, amarillo si vence hoy
  - 4 KPIs (estado, semaforo, monto, ruta)
  - Info completa de entrega (cliente, contacto, direccion, zona, notas)
  - Info post-entrega (si Entregado) o motivo (si Fallido)
  - Panel de acciones segun estado
  - Historial de eventos (timeline con RutaHistorial)

Bloque 3 COMPLETADO - Rutas:
- Pages/Logistica/Rutas/Index - Listado con:
  - 5 KPIs clickeables (borrador, en reparto, atrasadas, completadas, canceladas)
  - 4 KPIs de paradas activas
  - Filtros: busqueda, estado, repartidor, zona, rango de fechas
  - Barra de progreso visual por ruta
  - Indicador visual
  - Muestra tracking code
- Pages/Logistica/Rutas/Create - Crear con:
  - Formulario: repartidor, zona, vehiculo, placa, fecha estimada, descripcion, notas
  - Autocompletar vehiculo/placa al elegir repartidor
  - Tabla de envios disponibles (pendientes sin ruta)
  - Buscador en vivo
  - Botones: Seleccionar todos / Limpiar / Contador en vivo
  - Total seleccionado en footer
  - Pre-seleccion desde querystring ?envioId=X
- Pages/Logistica/Rutas/Details - Detalle con:
  - Banner rojo si hay paradas atrasadas
  - KPIs: estado, progreso, monto total, paradas atrasadas
  - Info completa: repartidor, vehiculo, salida/regreso, zona, descripcion, notas
  - 4 KPIs de paradas
  - Tabla de paradas ordenada por OrdenParada
  - Acciones individuales por parada
  - Historial de eventos
  - 4 modales: entregar parada, fallar parada, cerrar ruta, cancelar ruta
  - Botones contextuales
- Pages/Logistica/Rutas/Edit - Editar con:
  - Solo permite editar rutas en Borrador
  - 3 secciones: datos, paradas actuales, envios disponibles
  - Checkbox "Quitar" en paradas actuales
  - Checkbox "Agregar" en disponibles
  - Buscador en vivo
  - Contador dinamico
  - Validacion: no permite dejar la ruta sin paradas
  - Autocompletar vehiculo/placa

Bloque 4 COMPLETADO - Zonas + Repartidores:
- Pages/Logistica/Zonas/Index - Listado con:
  - 3 KPIs (zonas activas, inactivas, envios asociados)
  - Tabla con: orden, color, nombre, descripcion, precio sugerido, conteo de envios, estado
  - Botones de accion segun permisos
- Pages/Logistica/Zonas/Create - Crear con:
  - Nombre, descripcion, precio sugerido, orden, color (con preview), activa
  - Sugerencia automatica de orden (max + 1)
  - Validacion de nombre unico
- Pages/Logistica/Zonas/Edit - Editar con:
  - Aviso si tiene envios asociados
  - Validacion de nombre unico
- Pages/Logistica/Zonas/Delete - Eliminar con:
  - Bloqueo si tiene envios asociados
  - Confirmacion con color de la zona
- Pages/Logistica/Repartidores/Index - Listado con:
  - 4 KPIs (activos, inactivos, rutas activas, entregas completadas)
  - Tabla con: codigo, nombre, contacto, vehiculo + placa, rutas activas, entregas, estado
  - Filtros: busqueda, estado, vehiculo
- Pages/Logistica/Repartidores/Create - Crear con:
  - Codigo autogenerado (NumeroDocumentoHelper tipo "Repartidor")
  - Nombre, telefono, licencia, vehiculo, placa, notas, activo
  - Validacion de codigo unico si se especifica manualmente
- Pages/Logistica/Repartidores/Edit - Editar con:
  - Aviso si tiene rutas activas
  - Validacion de codigo unico
- Pages/Logistica/Repartidores/Delete - Eliminar con:
  - Bloqueo si tiene rutas o envios
  - Confirmacion

_Layout.cshtml OK ACTUALIZADO:
- Submenu Logistica reorganizado:
  - Dashboard (Index)
  - Envios
  - Rutas
  - Zonas de envio
  - Repartidores
  - --- (divider)
  - Tracking publico (sin condicional, AllowAnonymous)
  - Reportes
- Diccionario moduleNames ampliado con: Envios, Rutas, Zonas, Repartidores, Tracking
- Filtrado por permisos con puedeVerSubmodulo("Logistica", "X")

Bloque 5 EN PROGRESO - Tracking + Reportes:
- Pages/Logistica/Tracking/Index OK COMPLETADO:
  - Pagina publica con Layout = null y diseno standalone
  - NO usa _Layout.cshtml
  - Buscador por codigo de tracking (normaliza uppercase + trim)
  - Al encontrar ruta: muestra KPIs, progreso, repartidor, vehiculo, paradas y timeline
  - Semaforos visuales por parada
  - NO expone datos sensibles
  - Responsive
  - Requiere AllowAnonymousToPage("/Logistica/Tracking/Index") en Program.cs
- Pages/Logistica/Reportes/Index <- PENDIENTE (ultimo archivo del modulo)

Configuracion:
- Datos de la empresa (con pais, zona horaria, config RRHH, config alertas)
- Nomenclatura -> Series de documentos
- Metodos de pago
- Impuestos (CRUD + Import + Export con wizard)

Reportes:
- Reportes de ventas (KPIs, graficos, top productos, top clientes)
- Reportes de compras (KPIs, graficos, detalle por proveedor y producto)

Infraestructura de Import/Export:
- Wizard generico con vistas parciales reutilizables
- Aplicado a: Clientes, Productos, Proveedores, Categorias, Impuestos, UnidadesMedida

================================================================================
9. MODULOS PENDIENTES
================================================================================

Logistica (ultimo archivo del Bloque 5):
- Pages/Logistica/Reportes/Index - KPIs por repartidor, zona, tasa de exito,
  tiempos promedio
  - KPIs generales: total envios, entregados, fallidos, tasa de exito, tiempo
    promedio de entrega
  - Filtros: rango de fechas, zona, repartidor
  - Graficos con Chart.js:
    - Envios por zona (bar)
    - Tasa de exito por repartidor (bar horizontal o doughnut)
    - Tendencia de entregas por dia/semana (line)
  - Tabla detallada por repartidor
  - Tabla detallada por zona
- Program.cs - Corregir AllowAnonymousToPage de Tracking (ver seccion 10)
- Actualizar snapshots con .\generar-snapshot.ps1
- git commit + push

Fase 6 (futuro):
- Produccion (Ordenes, Calidad)
- Activos (Inventario, Mantenimiento)
- Seguridad (Index, Auditoria)
- Presupuestos por categoria
- Portal del empleado
- Evaluaciones de desempeno
- Capacitaciones / Cursos
- Reclutamiento / Vacantes

================================================================================
10. NOTAS IMPORTANTES
================================================================================

PENDIENTE EN Program.cs (critico para Tracking):
- Actualmente tiene: options.Conventions.AllowAnonymousToPage("/Tracking/Index");
- Debe cambiar a: options.Conventions.AllowAnonymousToPage("/Logistica/Tracking/Index");
- Sin este cambio, la pagina de Tracking publico va a requerir login y
  redirigir a /Auth/Login

Sobre migraciones:
- ApplicationDbContextModelSnapshot esta DESINCRONIZADO con la BD
- Al generar migracion, EF intenta crear TODAS las tablas
- Solucion actual: SQL manual + INSERT en __EFMigrationsHistory
- Logistica (Envios, Rutas, Zonas, Repartidores) - verificar si las tablas
  existen en la BD; si no, crear por SQL manual

Sobre OneDrive:
- Proyecto en C:\Users\crobe\OneDrive\Kirkenta
- Archivos son symlinks de OneDrive
- Recomendado mover a C:\Dev\Kirkenta

Sobre .gitignore:
- Excluye: bin/, obj/, .vs/, .vscode/, *.lnk, appsettings.Development.json
- Excluye carpetas del proyecto PHP viejo: app/, config/, public/, storage/,
  vendor/, includes/, modules/, backups/, logs/, assets/, uploads/

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

Sobre ConfiguracionDeduccion (RRHH nomina):
- Anio, AplicaIHSS, PorcentajeIHSS, TopeIHSS
- AplicaRAP, PorcentajeRAP, TopeRAP
- AplicaISR, TopeAnualExentoISR, MetodoISR ("Acumulativo" | "MensualSimple")
- Se crea automaticamente con valores por defecto de Honduras 2024
- TramosISR asociados por ConfiguracionDeduccionId

Sobre tipos de nomina:
- Semanal -> 7 dias, Catorcenal -> 14, Quincenal -> 15, Mensual -> 30
- Empleados filtrados por FrecuenciaPago

Sobre el Plan de Cuentas:
- Seed inicial: 81 cuentas estandar de Honduras
- Jerarquia por codigo con puntos
- Tipos: Activo, Pasivo, Patrimonio, Ingreso, Costo, Gasto
- Naturaleza calculada automaticamente
- Solo EsMovimiento=true acepta movimientos directos

Sobre __EFMigrationsHistory:
- Snapshot desincronizado -> no usar dotnet ef migrations
- Agregar migraciones manualmente por SQL

Sobre puerto:
- Solo se usa http://localhost:5114
- NO se usa HTTPS ni el puerto 7060

Sobre el wizard de importacion (04/10/2026):
- Refactorizado a componente generico: ImportWizardHelper + vistas parciales
- Clientes, Productos, Proveedores refactorizados
- Categorias, Impuestos, UnidadesMedida agregados con el mismo patron
- 6 modulos en total usan el wizard generico

Sobre el bug EF Core 9 (04/10/2026):
- string[] + .Contains() en queries EF rompe en .NET 10
- Error: TypeLoadException: GenericArguments[1], 'System.ReadOnlySpan`1[System.String]'
- Solucion: usar HashSet<string> o List<string>

Sobre el snapshot automatico (04/10/2026):
- Script: generar-snapshot.ps1 en la raiz
- Genera 4 archivos: PROYECTO_PARTE_1_Nucleo.md, PROYECTO_PARTE_2_Operaciones.md,
  PROYECTO_PARTE_3_Soporte.md, PROYECTO_PARTE_4_Extras.md
- Auto-descubrimiento: si creas Pages/Produccion o Helpers/Presupuestos,
  van automaticamente a Parte 4 (el script avisa)
- NO hay que editar el script cuando se crean modulos nuevos
- Excluye: bin/, obj/, .vs/, .vscode/, node_modules/, .git/, uploads/,
  wwwroot/lib/
- Ejecutar: .\generar-snapshot.ps1 en PowerShell
- Ejemplo de salida: si detecta modulos nuevos, muestra
  "[!] Modulos NUEVOS detectados (van a Parte 4):"

Sobre Logistica:
- Modulo activo en desarrollo. Bloques 1-4 completados + Tracking del Bloque 5.
- 5 modelos: Envio, Ruta, RutaHistorial, Repartidor, ZonaEnvio
- 4 helpers: EnvioHelper, RutaHelper, ZonaEnvioSeeder, AlertaEnvioHelper
- Estados de Envio: Pendiente -> EnRuta -> Entregado (o Fallido/Cancelado)
- Estados de Ruta: Borrador -> EnReparto -> Completada (o Cancelada)
- Al reagendar un envio fallido, vuelve a Pendiente sin ruta
- La ruta se cierra automaticamente cuando todas las paradas estan resueltas
- Las alertas NO se persisten: se calculan en tiempo real con AlertaEnvioHelper
- Los KPIs del Index de Envios son clickeables y aplican filtros via querystring
- Los KPIs del Index de Rutas son clickeables y aplican filtros via querystring
- Los KPIs del Index de Zonas no son clickeables
- Los KPIs del Index de Repartidores no son clickeables excepto "Rutas activas"
- El POS ya crea envios automaticamente al cobrar con RequiereEnvio=true
- El tracking code se genera con formato KRT-2026-AB12X9 (unico por ruta)
- Los modales usan la clase `is-open` (no display: inline)
- El _Layout.cshtml tiene el submenu Logistica completo con separadores visuales
- Las Zonas no se eliminan si tienen envios asociados
- Los Repartidores no se eliminan si tienen rutas o envios
- El catalogo de Zonas tiene 5 zonas tipicas de Honduras por seeder
- El Tracking publico tiene su propio diseno (Layout=null) y NO usa _Layout.cshtml
- El Tracking publico esta exento de autenticacion con AllowAnonymousToPage

================================================================================
11. FLUJO DE TRABAJO
================================================================================

Al iniciar chat nuevo:
1. Pega SIEMPRE la Parte 1 + CONTEXTO.md
2. Pega la parte del modulo que vas a tocar:
   - Ventas/Compras/Finanzas/Inventario -> Parte 2
   - Logistica/RRHH/Reportes            -> Parte 3
   - Modulo nuevo (Produccion, etc.)    -> Parte 4
   - Si toca todo                       -> las 4 partes
3. Di: "Retomamos Kirkenta ERP"
4. Dime que modulo vamos a trabajar
5. Si vamos a tocar un archivo especifico, pasalo (solo ese)

Al terminar sesion:
1. Actualizar este archivo si hubo cambios importantes
2. Ejecutar .\generar-snapshot.ps1 (regenera los 4 snapshots)
3. Guardar
4. git add . && git commit -m "..." && git push

Al pedir codigo:
- Archivos COMPLETOS, no fragmentos (regla #1)
- Nombre del archivo + ruta
- Un archivo por mensaje (o agrupados si son cortos)

================================================================================
ULTIMA ACTUALIZACION
================================================================================

Fecha: 04/10/2026 (sesion 5 - Bloque 5 Tracking completado + sistema de 4 snapshots)

Estado:
- Modulos operativos: Usuarios, Ventas, POS, Inventario, Compras, Finanzas,
  RRHH, Reportes, Configuracion.
- Finanzas 100% (incluye Fase 5: Contabilidad completa).
- RRHH 100%.
- Logistica EN DESARROLLO - 4.5/5 bloques completados:
  - OK Bloque 1: AlertaEnvioHelper + Logistica/Index (dashboard con alertas)
  - OK Bloque 2: Logistica/Envios/Index + Logistica/Envios/Details
  - OK Bloque 3: Logistica/Rutas/Index + Create + Details + Edit
  - OK Bloque 4: Logistica/Zonas + Logistica/Repartidores + _Layout actualizado
  - OK Bloque 5 (parcial): Logistica/Tracking/Index (publico)
  - <- Bloque 5 PENDIENTE: Logistica/Reportes/Index (ultimo archivo)

Sistema de snapshots:
- 4 partes generadas automaticamente
- Auto-descubrimiento de modulos nuevos
- Parte 1: 183 archivos (~900 KB)
- Parte 2: 201 archivos (~1.3 MB)
- Parte 3: 112 archivos (~1 MB)
- Parte 4: auto-descubrimiento (actualmente solo tools/generar-proyecto.ps1)

Acciones criticas pendientes:
1. ACTUALIZAR Program.cs: cambiar AllowAnonymousToPage("/Tracking/Index")
   por AllowAnonymousToPage("/Logistica/Tracking/Index")
   Sin esto, el tracking publico va a requerir login.
2. Crear Logistica/Reportes/Index + .cs
3. Ejecutar .\generar-snapshot.ps1 (regenerar los 4 snapshots)
4. git commit + push

Fase 6 (futuro):
- Produccion, Activos, Seguridad, Presupuestos, Portal del empleado,
  Evaluaciones, Capacitaciones, Reclutamiento