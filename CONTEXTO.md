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
│   ├── Import/ (ExcelImporter, CsvImporter, ColumnMapper, ImportResult, ImportFile)
│   ├── Finanzas/ (AperturaHelper, CierreHelper, DistribucionHelper,
│   │             MovimientoAutomaticoHelper, SaldoHelper, AdjuntoCierreHelper,
│   │             FinanzasSeeder, CierreContableHelper, ConciliacionHelper,
│   │             ContabilidadHelper, ISVHelper, PlanCuentasHelper)
│   ├── RRHH/ (EmpleadoHelper, AdjuntoEmpleadoHelper, NominaHelper, VacacionHelper,
│   │         TipoDocumentoEmpleadoSeeder, FeriadoSeeder)
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
│   ├── Categorias/ (Index, Create, Edit, Delete)
│   ├── UnidadesMedida/ (Index, Create, Edit, Delete)
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
│   ├── Reportes/ (Index, Ventas, Compras)
│   ├── Configuracion/ (Index, Edit)
│   ├── Impuestos/ (Index, Create, Edit, Delete)
│   ├── MetodosPago/ (Index, Create, Edit, Delete)
│   ├── Nomenclatura/ (Index)
│   ├── Series/ (Index, Create, Edit, Delete)
│   └── Shared/ (_Layout, _LayoutPOS, _ValidationScriptsPartial)
├── wwwroot/ (css, js, images, lib, uploads)
├── Program.cs
├── appsettings.json
├── CONTEXTO.md
└── PROYECTO.md

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
Reportes: Index, Ventas, Compras
Logistica: Index, Envios
Activos: Index, Mantenimiento
Seguridad: Index, Auditoria
Configuracion: Index, Empresa, Nomenclatura, Series, MetodosPago

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
- Módulos padre: "Clientes", "Productos", "Cuentas", "Movimientos", "Cierres", "Empleados", "Vales", "Nomina", etc.
- Submódulos (con acción): "ClientesCreate", "ClientesEdit", "ClientesDelete", "CuentasCreate", "CierresAprobar", "ValesEntregar", "NominaPagar"
- Genéricos: "Index", "Create", "Edit", "Delete" (usados cuando aplican al módulo raíz, ej: "UsuariosCreate" no existe, pero "Usuarios/Index" sí como submódulo)

================================================================================
5. HELPERS CLAVE
================================================================================

NumeroDocumentoHelper
- GenerarSiguiente(context, tipo) → genera número y avanza el correlativo
- PreviewSiguiente(context, tipo) → solo muestra el próximo
- SincronizarSerie() → ajusta el correlativo si detecta desfase con la BD
- Soporta formato personalizado: {PREFIX}, {SUFFIX}, {SEP}, {NUM}, {YEAR}, {MONTH}, {DAY}
- Tipos soportados: Cotizacion, Pedido, Venta, Factura, Devolucion, Producto, Baja, Proveedor, OrdenCompra, PagoProveedor, DevolucionProveedor, MovimientoFinanciero, AperturaCaja, CierreCaja, ConciliacionBancaria, Empleado, ValeEmpleado, Vacacion, PermisoEmpleado, Nomina, PagoNomina

ActividadHelper
- Registrar(context, usuarioId, "acción", "detalle", ipAddress, userAgent)
- Nunca lanza excepción (silencioso si falla)

SaldoHelper (Finanzas)
- Aplicar(context, movimiento) → ajusta saldo de cuentas (Ingreso suma, Egreso resta, Transferencia resta origen y suma destino)
- Revertir(context, movimiento) → revierte al anular

MovimientoAutomaticoHelper (Finanzas)
- RegistrarIngresoVenta(context, ventaId, numeroVenta, monto, usuarioId, formaPago, cuentaId)
- RegistrarEgresoPagoProveedor(context, pagoId, numeroPago, monto, usuarioId, nombreProveedor, formaPago)
- RegistrarEgresoNomina(context, nominaId, numeroNomina, monto, usuarioId)
- Resuelve cuenta y categoría por defecto si no se especifican

AperturaHelper (Finanzas)
- Abrir(context, cuentaId, saldoInicial, notas, usuarioId) → (apertura, error)
- ObtenerAperturaActiva(context, cuentaId)
- HayAperturaActiva(context, cuentaId)
- Cerrar(context, aperturaId, cierreId)

CierreHelper (Finanzas)
- Calcular(context, cuentaId, fecha) → CalculoCierre (con Apertura, SaldoInicial, Ingresos, Egresos, EfectivoEsperado)
- Registrar(context, cuentaId, fecha, efectivoContado, notas, tolerancia, usuarioId) → (cierre, error)
- DeterminarResultado(diferencia, tolerancia) → "Cuadrado" | "Sobrante" | "Faltante"
- Genera ajuste automático si hay diferencia (Ingreso/Egreso)

DistribucionHelper (Finanzas)
- RegistrarDistribuciones(context, cierreId, cuentaOrigenId, efectivoContado, distribuciones, usuarioId) → (ok, error)
- ObtenerDistribuciones(context, cierreId) → List<DistribucionView>
- Tipos: "RetiroBanco" (Transferencia), "FondoCaja" (sin movimiento), "EntregaAdmin" (Egreso), "PagoDirecto" (Egreso), "Otro" (Egreso)
- Validación: suma de distribuciones debe igualar efectivo contado

AdjuntoCierreHelper (Finanzas)
- Guardar(context, env, cierreId, archivo, descripcion, usuarioId)
- Eliminar(context, env, adjuntoId)
- Extensiones: .pdf, .jpg, .jpeg, .png. Máx 10 MB

CierreContableHelper (Finanzas) ⬅ NUEVO FASE 5
- ValidarFecha(context, fecha) → (ok, error) si el mes está cerrado
- Cerrar(context, anio, mes, usuarioId, notas) → (ok, error, cierre)
- Reabrir(context, anio, mes, usuarioId, motivo) → (ok, error)
- ObtenerEstadoAnual(context, anio) → List<CierreContable>
- Obtener(context, anio, mes) → CierreContable?

ConciliacionHelper (Finanzas) ⬅ NUEVO FASE 5
- Crear(context, cuentaId, fechaInicio, fechaFin, saldoBanco, notas, usuarioId) → (conciliacion, error)
- CargarLineasSistema(context, conciliacion)
- MatchingAutomatico(context, conciliacionId) → int (matches realizados)
- MatchingManual(context, detalleSistemaId, detalleBancoId) → (ok, error)
- DeshacerMatch(context, detalleId) → (ok, error)
- RecalcularTotales(context, conciliacion)
- Cerrar(context, conciliacionId, usuarioId, notasCierre) → (ok, error)
- Cancelar(context, conciliacionId, motivo) → (ok, error)

ContabilidadHelper (Finanzas) ⬅ NUEVO FASE 5
- GenerarEstadoResultados(context, desde, hasta) → EstadoResultados
- GenerarBalanceGeneral(context, fechaCorte) → BalanceGeneral
- ContarSinPlanCuenta(context, desde, hasta) → int
- DTOs: LineaReporte, EstadoResultados, BalanceGeneral

ISVHelper (Finanzas) ⬅ NUEVO FASE 5
- Calcular(context, items, descuentoGlobal) → ResultadoISV
- ObtenerTasaProducto(context, impuestoId) → decimal
- SincronizarItems(context, items) → bool
- RecalcularFactura(context, factura, items) → bool
- ObtenerTasasActivas(context) → List<Impuesto>
- NO hardcodea tasas: siempre lee del catálogo Impuestos

PlanCuentasHelper (Finanzas) ⬅ NUEVO FASE 5
- Seed(context) → pobla el plan de cuentas estándar de Honduras (81 cuentas)
- ObtenerTodas(context) → List<PlanCuenta>
- ObtenerCuentasMovimiento(context) → List<PlanCuenta> (solo EsMovimiento=true)
- RecalcularJerarquia(context) → int

EmpleadoHelper (RRHH)
- GenerarCodigo / PreviewCodigo
- CalcularAniosAntiguedad(empleado, fechaRef) / CalcularMesesAntiguedad
- CalcularDiasVacacionesPorAntiguedad(context, empleado, anioAntiguedad) → lee ConfiguracionEmpresa.RHTablaVacaciones
- ProcesarAcumulacionVacaciones(context, empleado, fechaRef)
- ProcesarAcumulacionGlobal(context) → llamada al arrancar la app
- ObtenerFeriados(context, desde, hasta)

AdjuntoEmpleadoHelper (RRHH)
- Guardar(context, env, empleadoId, tipoDocumentoId, archivo, descripcion, fechaDoc, fechaVenc, usuarioId)
- GuardarFotoPerfil(context, env, empleadoId, archivo)
- Eliminar(context, env, adjuntoId)
- ActualizarVigencias(context) → llamada al arrancar la app
- Extensiones: .pdf, .jpg, .jpeg, .png, .docx, .doc. Máx 15 MB (foto 5 MB)

NominaHelper (RRHH)
- ObtenerConfiguracion(context, anio) → crea config por defecto si no existe
- CalcularISRMensual(salarioBruto, tramos)
- CalcularISRAcumulativo(context, empleadoId, salarioBrutoMes, anio, mesActual, config)
- CalcularIHSS(salarioBase, config) / CalcularRAP(salarioBase, config)
- CalcularSalarioProporcional(salarioBase, diasTrabajados, diasPeriodo)
- CalcularPeriodo(frecuencia, referencia) → (inicio, fin, pago)
- DiasPeriodo(frecuencia) → 7/14/15/30
- ObtenerValesActivos(context, empleadoId)
- CalcularDescuentoVales(context, empleadoId) → (total, valesAfectados)
- CalcularDetalleEmpleado(context, empleado, tipoNomina, diasPeriodo, fechaInicio, fechaFin) → DetalleNomina

VacacionHelper (RRHH)
- CalcularDias(context, fechaInicio, fechaFin) → (diasHabiles, diasFeriados, feriados)
  Excluye fines de semana y feriados del país
- Solicitar(context, empleadoId, fechaInicio, fechaFin, motivo, usuarioSolicitaId) → (vacacion, error)
- Aprobar(context, vacacionId, usuarioApruebaId) → descuenta saldo
- Rechazar(context, vacacionId, usuarioApruebaId, motivo)

Export
- ExportColumns.Clientes() / .Productos(cats, imps) / .Proveedores()
- ExcelExporter.Export(items, columns, "Sheet", "Título") → byte[]
- CsvExporter.Export(items, columns) → byte[]
- JsonExporter.Export(items) → byte[]

Import
- ExcelImporter.Read(stream) → ImportFile
- CsvImporter.Read(stream) → ImportFile
- ImportFile: Headers (List<string>), Rows (List<Dictionary<string,string>>), Ok, Error
- ColumnMapper.Detectar(headers, aliasPorCampo) → Dictionary<string, string?>
- ColumnMapper.Normalizar(texto) → sin acentos/espacios, lowercase
- Wizard: /[Modulo]/Import → /[Modulo]/ImportMap → confirmar → Index
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

Actividad (siempre registrar acciones importantes):
ActividadHelper.Registrar(_context, currentUser?.Id ?? 0, "Verbo + objeto", $"Detalle L. {monto:N2}", HttpContext.Connection.RemoteIpAddress?.ToString());

Estructura típica de página:
- Vista .cshtml con layout principal (_Layout)
- POS usa layout propio (_LayoutPOS)
- Cabecera con breadcrumb (automática desde _Layout)
- `module-header` con título + botones de acción
- Banner TempData Success/Error
- KPIs en `kpi-grid` (opcional)
- Contenido principal en `module-card`
- Modales con estilo `.pos-modal` reutilizado

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
16. Vales: límite del 50% del salario mensual del empleado (validación al crear).
17. Nómina: no se puede editar si tiene recepciones o pagos. Se anula.
18. OrdenCompra: no se puede editar si tiene recepciones o pagos. Estados: Borrador → Enviada → RecibidaParcial → Recibida → Pagada.
19. Período contable cerrado: bloquea movimientos, aperturas, cierres, ventas, pagos, nóminas y vales con fecha dentro del mes cerrado.
20. Cierre contable reabrible: requiere motivo + contraseña, queda en auditoría.
21. Conciliación: solo cuentas tipo "Banco". Matching automático por monto (±0.01) y fecha (±3 días).
22. Plan de Cuentas: código jerárquico con puntos (1, 1.1, 1.1.01). Naturaleza calculada por tipo.
23. Solo cuentas con EsMovimiento=true aceptan movimientos directos.

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
- Clientes (CRUD + Import + Export con wizard)
- Cotizaciones (CRUD + conversión a factura y a POS)
- Pedidos (CRUD + estados)
- Facturas (Index, Details, RegistrarPago, CreateFromVenta)
- Devoluciones (Index)

Inventario:
- Productos (CRUD + Import + Export con wizard)
- Categorías, Unidades de medida
- Bajas (con aprobación y reversión + auditoría)
- Historial de compras por producto (con gráfico de evolución de precios)

Compras:
- Proveedores (CRUD + Import + Export con wizard)
- Órdenes de compra (Create, Edit, Recibir con recepción parcial)
- Pagos a proveedores + Cuentas por pagar (con integración Finanzas)
- Adjuntos a órdenes de compra (con validación IDOR)
- Reportes de compras (gráficos + detalle por proveedor y producto)

Finanzas:
- Dashboard con gráficos (Flujo mensual, Top categorías egresos)
- Cuentas financieras (Cajas + Bancos, con saldo inicial y actual)
- Categorías financieras (Ingreso/Egreso, sistema + custom, +PlanCuentaId)
- Movimientos (Ingresos, Egresos, Transferencias, Anulaciones)
- Reportes (Flujo de caja, Estado de resultados, Estado de cuenta)
- Aperturas de caja (una activa por cuenta)
- Cierres con distribución del efectivo (RetiroBanco, FondoCaja, EntregaAdmin, PagoDirecto, Otro)
- Aprobación de cierres (Cerrado → Aprobado/Rechazado)
- Adjuntos / comprobantes (con validación)
- Movimientos automáticos desde POS, PagosProveedor, Vales, Nómina
- Ajuste automático por diferencia en cierre
- ⬅ FASE 5: Plan de Cuentas (catálogo jerárquico con seeder de Honduras)
- ⬅ FASE 5: Estado de Resultados (Ingresos - Costos - Gastos = Utilidad)
- ⬅ FASE 5: Balance General (Activos = Pasivos + Patrimonio)
- ⬅ FASE 5: Cierres Contables (bloqueo de meses + reapertura con motivo)
- ⬅ FASE 5: Conciliación Bancaria (matching automático y manual)
- ⬅ FASE 5: ISV Helper (cálculo dinámico de ISV por tasas del catálogo)

RRHH (100% completado):
- Dashboard con KPIs + Cumpleaños + Aniversarios + Feriados + Vacaciones + Alertas + Empleados recientes
- Empleados: CRUD + foto + documentos adjuntos + expediente + hoja de vida con 8 tabs
- Categorías de documentos (TipoDocumentoEmpleado) con seeder
- Expedientes (llamados, amonestaciones, suspensiones, méritos, reconocimientos)
- Vacaciones con cálculo hábil (excluye fines de semana y feriados) y descuento automático de saldo
- Permisos y licencias con/sin goce de sueldo
- Vales con doble aprobación (Gerente + RRHH) → Entregado → Descontado en nómina
- Nómina completa: cálculo ISR (acumulativo anual) + IHSS + RAP + vales + pago individual o en lote
- Feriados multi-país con seeder (HN, GT, SV, CR, NI, PA, MX, US) + Semana Santa por algoritmo
- Alertas personalizadas con dashboard y prioridades
- Reportes: KPIs, gráficos (Chart.js), por departamento, nómina por mes, top antigüedad, cumpleaños, rotación
- Integración con Finanzas (egresos automáticos por nómina y vales)

Configuración:
- Datos de la empresa (con país, zona horaria, config RRHH, config alertas)
- Nomenclatura → Series de documentos (con formato personalizable)
- Métodos de pago
- Impuestos

Reportes:
- Reportes de ventas (KPIs, gráficos, top productos, top clientes, menos vendidos, por día)
- Reportes de compras (KPIs, gráficos, detalle por proveedor, detalle por producto, evolución de precios)

================================================================================
9. MÓDULOS PENDIENTES
================================================================================

- Producción (Órdenes, Calidad)
- Logística (Rutas, Envíos)
- Activos (Inventario, Mantenimiento)
- Seguridad (Index, Auditoría)
- Import/Export de Categorías, Impuestos, Unidades de medida
- Refactor del wizard a componente genérico
- Presupuestos por categoría
- Portal del empleado (login propio para ver recibos, solicitar vacaciones)
- Evaluaciones de desempeño
- Capacitaciones / Cursos
- Reclutamiento / Vacantes

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

Sobre el schema de ConfiguracionDeduccion (RRHH nómina):
- Anio, AplicaIHSS, PorcentajeIHSS, TopeIHSS
- AplicaRAP, PorcentajeRAP, TopeRAP
- AplicaISR, TopeAnualExentoISR, MetodoISR ("Acumulativo" | "MensualSimple")
- Se crea automáticamente con valores por defecto de Honduras 2024 si no existe
- TramosISR asociados por ConfiguracionDeduccionId

Sobre el flujo de ISR:
- Método "Acumulativo" (SAR): acumula salario bruto del año, aplica tabla sobre acumulado, resta lo ya retenido
- Método "MensualSimple": aplica tabla directo sobre el salario del mes

Sobre tipos de nómina:
- Semanal → 7 días
- Catorcenal → 14 días
- Quincenal → 15 días
- Mensual → 30 días
- Empleados filtrados por FrecuenciaPago

Sobre el Plan de Cuentas (FASE 5):
- Seed inicial: 81 cuentas estándar de Honduras
- Jerarquía por código con puntos: 1 (Activo), 1.1 (Activo Corriente), 1.1.01 (Caja y Bancos), etc.
- Tipos: Activo, Pasivo, Patrimonio, Ingreso, Costo, Gasto
- Naturaleza calculada automáticamente: Deudora (Activo/Costo/Gasto) o Acreedora (Pasivo/Patrimonio/Ingreso)
- Solo EsMovimiento=true acepta movimientos directos (hojas del árbol)
- CategoriaFinanciera.PlanCuentaId → FK opcional
- El Estado de Resultados y Balance General se generan agrupando MovimientoFinanciero por PlanCuenta

Sobre la tabla __EFMigrationsHistory:
- Snapshot desincronizado → no usar dotnet ef migrations
- Agregar migraciones manualmente por SQL
- Para Fase 5 ya se agregó manualmente la migración

Sobre puerto:
- Solo se usa http://localhost:5114
- NO se usa HTTPS ni el puerto 7060

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

Fecha: 04/10/2026
Estado: RRHH 100%. Finanzas 100% (incluye Fase 5: Contabilidad completa con
        Plan de Cuentas, Estado de Resultados, Balance General, Cierres
        Contables, Conciliación Bancaria e ISV Helper).
        Módulos operativos: Usuarios, Ventas, POS, Inventario, Compras,
        Finanzas, RRHH, Reportes, Configuración.
Próximo: Fase 6 (Producción, Logística, Activos, Seguridad) o cerrar
         pendientes de Finanzas (Presupuestos por categoría, Portal del
         empleado, Refactor wizard import).