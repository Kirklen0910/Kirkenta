# 16. CONTABILIDAD, COSTO DE VENTAS (COGS) Y BALANCE GENERAL

**Fecha de actualización:** 22/10/2026
**Estado:** ✅ **IMPLEMENTADO Y FUNCIONAL**

## ¿El sistema calcula el costo de los productos al vender?
**SÍ.** A partir de esta actualización, el sistema maneja la contabilidad de partida doble para las ventas y el inventario.
Cuando se hace una venta (POS o Facturación):
1. Se descuenta el `Stock` del producto y se guarda un *snapshot* del costo en `DetalleVenta.CostoUnitarioSnapshot`.
2. Se crea un `MovimientoFinanciero` de `Ingreso` (cuenta 4.1.01 - Ventas).
3. Se crea un `MovimientoFinanciero` de `Egreso` por el Costo de Mercadería Vendida (cuenta 5.1.01 - Costos).
4. El campo `Venta.CostoTotalVendido` refleja la suma exacta de los costos de los productos en esa venta.

**Consecuencia:**
El Estado de Resultados ahora es contablemente correcto:
* `Utilidad Bruta = Total Ingresos - Total Costos` (ahora sí resta el costo del producto).
* `Utilidad Neta = Utilidad Bruta - Gastos Operativos`.

## Fix del Balance General (Activos reales)
Anteriormente, el `TotalActivos` en el Balance General daba `0` porque los movimientos de Caja/Banco no se asociaban a una cuenta contable de Activo (solo a categorías de ingreso/egreso).
**Solución implementada:**
* El `ContabilidadHelper` ahora lee directamente los saldos reales (`SaldoActual`) de la tabla `CuentasFinancieras` (Cajas y Bancos) y los inyecta como líneas de Activos en el Balance General.
* Se agregó el helper `AperturaCuentaHelper` que genera un `MovimientoFinanciero` de tipo "Apertura" por cada cuenta nueva que tenga `SaldoInicial > 0`. Esto mantiene la consistencia histórica y contable.
* El seeder corre al arrancar la app (`Program.cs`), asegurando que las cuentas viejas con saldo inicial también tengan su movimiento de apertura.

## Corrección de Signos (Reportes en Positivo)
Se corrigió la fórmula en `ContabilidadHelper` para que respete la naturaleza contable:
* Cuentas Acreedoras (Ingresos, Pasivos, Patrimonio): se muestran en **positivo** (suman).
* Cuentas Deudoras (Activos, Costos, Gastos): se muestran en **positivo** para ser restados de los ingresos en los reportes.
* Los reportes de **Estado de Resultados** y **Balance General** ya no muestran los ingresos con signos negativos (ej. `-L. 25,847.00`), sino positivos (`L. 25,847.00`).

## Configuración requerida (Nuevas Categorías Contables)
Para que el sistema funcione al 100% con la nueva contabilidad, las categorías financieras deben estar mapeadas a su cuenta del Plan de Cuentas.
El `FinanzasSeeder` (al arrancar) asocia automáticamente:
* Categoría de Ingreso **"Ventas al contado"** → Plan `4.1.01` (Ventas de Mercadería)
* Categoría de Egreso **"Costo de mercadería vendida"** → Plan `5.1.01` (Costo de Mercadería Vendida)
* Categoría de Ingreso **"Apertura de cuenta"** → Plan `1.1.01.001` (Caja General)
* Otras categorías (Alquiler, Energía, etc.) se asocian a sus respectivos gastos (6.1, 6.2, 6.3).

## Notas sobre migraciones (Base de datos)
* Se agregó la migración `AddCogsToVentaYDetalle` que añade las columnas `CostoRegistrado`, `CostoTotalVendido`, `MovimientoCostoId`, `CostoUnitarioSnapshot` y `CostoTotalLinea`.
* Se corrigió el historial de `__EFMigrationsHistory` para que no chocara con columnas que ya existían (ej. `EsCredito`).
* Si se agregan nuevas ventas, los reportes de contabilidad reflejarán los costos automáticamente sin necesidad de ajustes manuales.

---

# 17. FASES 1-4: FACTURACIÓN FISCAL, KARDEX, DEVOLUCIONES, ANULACIONES, REPORTES

**Fecha de actualización:** 24/10/2026
**Estado:** ✅ Implementado. Requiere migración (`dotnet ef migrations add ...` + `database update`).

## Facturación fiscal (CAI)
* `ConfiguracionEmpresa.FacturacionFiscalActiva` (checkbox en Configuración → Editar). Si está apagada, se usa la serie interna "Factura" y no hay avisos de CAI.
* Encendida: `FacturacionFiscalHelper` (Previsualizar/Reservar/AsignarNumero/ValidarConfiguracion) numera dentro del rango `000-001-01-00000001`, bloquea si el CAI venció o el rango se agotó y avisa a 15 días / 20 números. La factura guarda un snapshot de CAI, rango y fecha límite. El contador (`SiguienteNumeroFiscal`) se reinicia si cambia el CAI o el rango.
* Aplica en: Facturas/CreateFromVenta, conversión de Cotización a factura (Index y Details) y Pedidos → Facturar.
* **Nota de crédito fiscal**: CAI, rango y fecha límite propios (`NotaCreditoCAI`, `NotaCreditoRangoInicial/Final`, `NotaCreditoFechaLimite`, `SiguienteNumeroNotaCredito`). `NotaCreditoFiscalHelper.Reservar` se usa en `DevolucionVentaHelper.Crear`. Si la facturación fiscal está activa pero no se llenó el rango de NC, la devolución usa la serie interna "Devolucion". La `Devolucion` guarda el snapshot (CAI, RangoInicial, RangoFinal, FechaLimiteEmision).

## Kardex e inventario
* `MovimientoInventario` (Cantidad con signo, StockAnterior/Posterior, Origen, DocumentoNumero, CostoUnitario, UsuarioId).
* `InventarioHelper.Registrar/Ajustar/CostoPromedio` es la ÚNICA vía autorizada para cambiar stock (no hace SaveChanges).
* Orígenes: Venta, Compra, Baja, ReversionBaja, FacturaCotizacion, FacturaPedido, StockInicial, AjusteManual, Importacion, Devolucion, AnulacionVenta, DevolucionProveedor.
* Costo promedio ponderado al recibir órdenes de compra (casilla "RecalcularCosto").
* Pantallas: Productos → Kardex y Productos → Ajuste.

## Devoluciones y anulaciones
* `DevolucionVentaHelper.Crear`: prorratea el descuento global; la parte a crédito baja `Venta.SaldoPendiente` y `Factura.Saldo`; el resto se reembolsa (Egreso real en categoría "Devoluciones en ventas", plan 4.1.03). Reversa del COGS = Egreso negativo "solo contable" (`[SOLO-CONTABLE]`).
* `AnulacionVentaHelper.Anular`: restaura stock con kardex, anula movimientos financieros de la venta, cancela el envío (bloqueado si ya va en ruta/entregado), marca Factura y Venta "Anulada" (el número fiscal queda consumido). Bloqueada si el periodo está cerrado o la venta tiene devoluciones. Permiso: Ventas → Delete.
* `DevolucionProveedorHelper.Crear`: baja stock, reduce `OrdenCompra.Saldo`; si la orden ya estaba pagada deja `MontoPorRecuperar`. Módulo Compras → DevolucionesProveedor.
* Ventas a crédito: el ingreso se reconoce al cobrar; los pagos de facturas de ventas a crédito se redirigen a Finanzas → Cobros. Límite de crédito validado en `VentaService`.

## Pedidos
* Pedidos/Details: cambios de estado (Pendiente → En preparacion → Enviado → Entregado, o Cancelado) y botón "Facturar pedido" (factura con numeración fiscal, descuenta inventario con origen FacturaPedido, guarda `Pedido.FacturaId`).

## Impresión y reportes
* Factura: botón "Ticket 80 mm" (`Facturas/Ticket`) y "PDF / Carta" (`Facturas/Carta`, tamaño carta; usar Imprimir → Guardar como PDF).
* Estado de cuenta del cliente (`Clientes/EstadoCuenta`).
* Reportes (con exportación CSV): Antigüedad de saldos de clientes, Cuentas por pagar (proveedores), Libro de ISV, Valoración de inventario.
* Paginación y filtros del lado del servidor (50 por página, parámetro `p`, partial `_Pager`) en Ventas, Facturas, Clientes, Productos y Cotizaciones.

## Operación
* Logs de advertencia/error en `logs/kirkenta-yyyyMMdd.log` (`FileLoggerProvider`).
* Respaldo de BD: `tools/respaldo-bd.ps1` (mysqldump + zip + rotación 30 días). Agregar `respaldos/` al .gitignore.
* Pruebas: `dotnet test tests\Kirkenta.Tests` (xUnit + EF InMemory; las de BD usan `[Collection("BD")]`).

## Facturas sin venta (de cotización o de pedido)
* Pago (Facturas → Registrar pago): genera un Ingreso real en Finanzas (`RegistrarIngresoPagoFactura`, origen "Factura"; efectivo a la caja activa, otros métodos al primer banco activo; categoría "Cobros de facturas", plan 4.1.01). Si Finanzas falla, se revierte todo el pago.
* Costo de ventas: al emitirse la factura se registra el COGS contable (`RegistrarCostoFactura`, origen "Factura", solo contable, idempotente).
* Anulación (`AnulacionFacturaHelper`, permiso Ventas → Facturas → eliminar): devuelve stock con kardex ("AnulacionFactura"), anula los movimientos de Finanzas de la factura, reabre la cotización (vuelve a "Aceptada") o libera el pedido, y marca la factura "Anulada" (el número fiscal queda consumido). Las facturas de una venta se anulan desde la venta.
* El estado de cuenta de una cuenta en Finanzas → Reportes ignora los movimientos "solo contables" (costo de ventas y su reversa) al calcular saldos.

## Dashboard dinámico por permisos
* `Pages/Index` arma el dashboard según los permisos del rol, leídos en cada visita (si se cambia un rol, cambia de inmediato). Cada bloque exige su permiso "ver": Ventas/Index (ventas hoy/mes, gráfico 7 días, top productos, últimas ventas), Ventas/Clientes, Ventas/Cotizaciones, Ventas/Pedidos, Inventario/Productos (productos y stock bajo), Ventas/Facturas o Finanzas/Cobros (por cobrar), Compras/CuentasPorPagar o Ordenes (por pagar), Finanzas/Index (caja y bancos, flujo del mes), Logistica/Envios (envíos pendientes). Las alertas (CxC/CxP vencidas, CAI por vencer) y los accesos rápidos también se filtran por permiso. Si el rol no tiene nada, se muestra un mensaje.
* Para agregar un indicador nuevo: añadir un bloque `if (Puede("Modulo","Sub")) Kpis.Add(...)` en `IndexModel.OnGet`.

## Flujo de caja = efectivo real
* `FlujoCajaHelper` excluye los movimientos solo contables (costo de ventas 5.1.01 y su reversa) en Finanzas/Index y Finanzas/Reportes. El costo de ventas solo aparece en el Estado de Resultados.

## Auditoría de módulos (2026-10-25)
* `LineasDocumentoHelper.Validar` valida en servidor las líneas de Cotización/Pedido (cantidad > 0, precio/descuento >= 0, impuesto 0-100, producto activo).
* Pedidos/Edit ya NO cambia el estado (solo desde Pedidos/Details). Cotizaciones/Edit bloquea "Convertida" y estados no manuales.
* Crear Cotización/Pedido y PagosProveedor/Create van en transacción; el pago se revierte si falla el egreso financiero.
* Todo cambio de stock pasa por `InventarioHelper` (Bajas aprobar/revertir, OrdenesCompra/Recibir, Productos/Edit → AjusteManual). Bajas no aprueba con stock insuficiente.
* `NumeroDocumentoHelper.GenerarSiguiente` reclama el correlativo con UPDATE atómico (ExecuteUpdate) en MySQL; InMemory usa la ruta vieja.
* Borrados protegidos: Usuarios con historial (se desactivan), Series que ya emitieron, Unidades en uso, Cotizaciones con factura.
* Paginación/filtros en servidor (50 por página, partial _Pager): Finanzas/Movimientos, OrdenesCompra, Bajas (KPIs agregados en la BD; pérdidas de Bajas = solo Aprobadas no revertidas).
* Transacciones añadidas en: RRHH/Nomina/Pagar y PagarEmpleado, RRHH/Vales/Entregar, Finanzas/Movimientos Create y Edit (movimiento + saldo + documento origen todo o nada).
* Auditados sin hallazgos graves: borrados de Finanzas, Logística y RRHH (ya validan referencias).
* Proveedores también paginado/filtrado en servidor.
* OrdenesCompra Create/Edit: estado validado (Create: Borrador/Enviada; Edit: Borrador/Enviada/Cancelada), líneas validadas, proveedor existente, Create en transacción.
* Productos/ImportMap: el stock ya no se pisa; ajuste "Importacion" y "StockInicial" pasan por el kardex.
* RRHH/Nomina/Create: bloquea traslape de período del mismo tipo (anular la anterior para recalcular) y va en transacción.
* VentaService/POS revisado: valida precio vigente, stock, límite de crédito y reintenta concurrencia (sin hallazgos).
* Pendiente: probar con un rol limitado; tests de integración con MySQL real para la reserva atómica de correlativos.

## Pendiente / notas
* La reversa de COGS es un Egreso negativo "solo contable"; el Estado de Resultados y el Balance lo manejan bien.
* Permisos nuevos que hay que asignar a roles no Admin: Compras → DevolucionesProveedor / DevolucionesProveedorCreate; Ventas → Devoluciones.


### Auditoría de módulos — continuación (2026-10-25)
- Logística: tracking público sin datos personales (nombres enmascarados, dirección protegida), código de tracking criptográfico (KRT-AAAA-XXXXXXXXXX), entregado/fallido solo desde Pendiente/EnRuta.
- RRHH: Nómina bloquea períodos traslapados del mismo tipo; Vales: la 2ª aprobación debe ser de otra persona (Admin exento); Permisos: tipo válido, empleado activo, máx. 90 días y sin traslapes; Vacaciones: no permite empleados de baja y descuenta solicitudes pendientes del saldo; Empleados: bonos no negativos, estado válido, usuario vinculado existente y único.
- Pendiente de auditar: Reportes, Finanzas (Cierres/Conciliación/Cobros/Contabilidad), Facturas, Devoluciones, Configuración (catch vacío al borrar logo).


### Mapa del sistema y estado de auditoría (2026-10-25)
Módulos (Pages/): Auth, Usuarios, Configuracion, Series, Nomenclatura, Impuestos, MetodosPago, UnidadesMedida | Productos (Kardex, Ajuste, Importación), Categorias, Bajas | Clientes, Cotizaciones, Pedidos, Ventas, POS, Facturas, Devoluciones | Proveedores, OrdenesCompra, Compras, PagosProveedor, DevolucionesProveedor | Finanzas (Cuentas, Movimientos, Cierres de caja, Aperturas, Cobros, Conciliación, PlanCuentas, Contabilidad, CierresContables, Reportes) | Reportes (Ventas, Compras, Inventario, CxC, CxP, Libro ISV) | RRHH (Empleados, Vacaciones, Permisos, Vales, Nómina, Documentos, Feriados) | Logística (Envíos, Rutas, Repartidores, Zonas, Tracking público).
Helpers clave: InventarioHelper (único punto de cambios de stock/kardex), NumeroDocumentoHelper (correlativos atómicos), LineasDocumentoHelper (validación de líneas), SaldoHelper, CierreContableHelper, MovimientoAutomaticoHelper, PermisoHelper.
Auditado y corregido: todos los módulos de ventas, compras, inventario, logística, RRHH, Reportes (Ventas/Compras), Cobros, Cierres de caja y contables, Facturas/RegistrarPago, Configuración, Clientes y Productos (validaciones de montos negativos, estados, duplicados, transacciones).
Revisado sin hallazgos: CxC, CxP, Inventario, Libro ISV, Devoluciones, Conciliación, Movimientos/Anular.
Pendiente: vistas .cshtml y JS (no auditadas), Contabilidad (asientos) a fondo, pruebas con MySQL real. El archivo backup_kirkenta_2026-10-23.sql pesa 0 bytes: regenerar el respaldo.

## Cambios recientes (2026-10-25)

- **Teléfonos por país**: `Helpers/TelefonoHelper.cs` (29 países, atributo `[Telefono]`) + `wwwroot/js/telefono.js` (selector de país con código y máscara; se activa solo por el `name` del input). Guarda "+504 9999-9999".
- **RTN / Identidad (Honduras)**: `Helpers/DocumentoHonduras.cs` (`[Rtn]` 14 dígitos, `[Identidad]` 13 dígitos, formato con guiones) + `wwwroot/js/documentos.js`. Campos opcionales. Valores viejos con largo incorrecto bloquean el guardado hasta corregirse.
- **Decimales en formularios**: inputs de dinero con `asp-format="{0:0.00}"` (cantidades `{0:0.##}`) porque la BD guarda decimal(18,4). Días de vacaciones/permisos se muestran con `.ToString("0.##")`.
- **Caché de CSS/íconos**: `asp-append-version="true"` en lugar de `?v=@DateTime.Now.Ticks` (causaba el "logo grande" al navegar). Logo del menú con width/height fijos.
- **XSS**: `window.escHtml` global en `_Layout`; usado al armar innerHTML en POS, OrdenesCompra Create/Edit, Cotizaciones Create/Edit y Pedidos Create.
- **POS**: al marcar envío con cliente elegido se prellena la dirección/contacto del cliente.
- **Historial de producto**: nueva `Pages/Productos/Historial` (ventas con factura/cliente/costo/precio/utilidad, devoluciones netas; compras con orden/proveedor/costo/recepción). El ícono de gráfica en Productos/Index apunta a ella. `Compras/HistorialProducto` se conserva por su gráfico de precios.
- **Reporte de rentabilidad**: `Pages/Reportes/Rentabilidad` (utilidad y margen por producto, sin ventas, capital sin movimiento, margen bajo; CSV).
- **Estado de Resultados**: si Desde > Hasta se invierten.
- **Impuesto por producto / desglose fiscal** (25/10): formularios de Productos (Crear/Editar) ahora tienen selector de Impuesto (antes Editar lo borraba a null en cada guardado). `ImpuestoSeeder` asegura ISV 15% (predeterminado), 18% y Exento 0%. Regla única: producto sin impuesto = predeterminado del sistema (POS, Cotizaciones y Pedidos ya coinciden con el servidor). `ISVHelper.DesglosarFactura` arma el desglose con los porcentajes guardados en la factura; se imprime en Carta y Ticket (exonerado, exento, gravado e ISV por tasa) y en Detalles. El POS calcula en pantalla igual que el servidor (descuento general antes del ISV, envío con tasa predeterminada).
- **Permisos de devoluciones**: `PermisoSeeder.AplicarPermisosPredeterminados` (una sola vez, solo si nadie ha configurado la pantalla).

### Pendiente conocido
- **Balance General**: corregido (25/10). Para fechas de corte pasadas, Caja/Banco se calculan como saldo inicial + movimientos hasta la fecha (`CalcularSaldosAFecha`, mismas reglas que `SaldoHelper`); para hoy se usa `SaldoActual`. Falta probarlo con datos reales.
- `GenerarEstadoResultados`/`GenerarBalanceGeneral` usan subconsultas correlacionadas por movimiento (lento con muchos movimientos): precargar categorías y plan de cuentas en diccionarios.
- Respaldo `backup_kirkenta_2026-10-23.sql` está en 0 bytes: regenerar.
- Cambiar contraseña de MySQL al final del proyecto (decisión del usuario).
- Nada de lo anterior fue compilado desde el asistente: correr `dotnet build` y `dotnet test tests\Kirkenta.Tests`.

## Revisión de lógica (2026-10-25, 2ª pasada)
- **Pagos a proveedor** (`PagosProveedor/Create`): el pago toma moneda y tipo de cambio de la orden; el egreso en Finanzas se registra convertido a moneda local (antes salía sin convertir).
- **Recepción de compras** (`OrdenesCompra/Recibir`): "Recalcular costo" usa costo promedio ponderado (`InventarioHelper.CostoPromedio`) en vez de pisar con el último precio.
- **Cierre de caja** (`CierreHelper`): solo cierra aperturas ACTIVAS (no se puede cerrar dos veces la misma); el efectivo esperado cuenta desde la hora de apertura (no desde medianoche) y excluye movimientos de apertura; cierre + cierre de apertura + ajuste van en una transacción.
- **Facturar venta a crédito** (`Facturas/CreateFromVenta`): la factura nace con el saldo real de la venta (si ya hubo cobros/devoluciones) y estado Emitida/PagadaParcial/Pagada; los cobros previos se ligan a la factura.
- Revisados sin hallazgos: VentaService, Anulación de venta/factura, Devolución de venta, MovimientoAutomaticoHelper, SaldoHelper, Cobrar, RegistrarPago, cobertura de permisos en todas las páginas, validación de período cerrado en todo creador de movimientos.
- Riesgo conocido (no corregido): doble envío simultáneo de un cobro podría pasar la validación de saldo (no hay bloqueo de fila); aceptable en operación de una sola caja.

## Revisión de módulos restantes (2026-10-25, 3ª pasada)
- **Nómina/Create**: ya no se pisan las fechas elegidas con las del mes actual al generar (solo se sugieren en el GET).
- **Nómina/Pagar**: paga solo a quienes faltan (descuenta los pagados individualmente: antes pagaba doble y descontaba vales dos veces); FormaPago Efectivo si la cuenta es Caja. **PagarEmpleado** ignora empleados ya pagados.
- **NominaHelper**: ISR acumulativo ignora nóminas anuladas y cuenta solo meses realmente pagados; nóminas no mensuales usan equivalente mensual prorrateado; los vales no descuentan más de lo disponible tras IHSS/RAP/ISR.
- **ConciliacionHelper**: el saldo del sistema y las líneas excluyen movimientos de Apertura y solo contables (COGS), que nunca movieron el saldo del banco.
- **EnvioHelper**: envío de venta sin cliente usa ClienteId 1 (consumidor final) en vez de 0.
- Revisados sin hallazgos: Vales/Entregar, Nómina/Aprobar, Productos/Ajuste, EnvioHelper (entregas/fallos/reagendar), ContabilidadHelper (consistente con SaldoHelper).
- **Importadores** (Productos, Clientes, Proveedores): una fila nueva con SKU/código (o código de barras en productos) repetido dentro del mismo archivo ya no tumba toda la importación; se ignora con advertencia.
- Revisados sin hallazgos: Vacaciones (VacacionHelper), Permisos de RRHH.

## Módulos nuevos (Seguridad, Activos, Producción, Caja chica, Inventario avanzado, Contabilidad)

**Tablas nuevas** (11): ActivosFijos, DepreciacionesActivo, MantenimientosActivo, OrdenesProduccion, DetallesOrdenProduccion, InspeccionesCalidad, GastosCajaChica, TomasFisicas, DetallesTomaFisica, AsientosContables, DetallesAsiento. Modelos en `Models/ModulosAdicionales.cs` (sin navegaciones/FK). NO hay migración de EF: `Helpers/EsquemaModulosNuevos.cs` (se llama en Program.cs antes de DiagnosticoEsquema) usa `Database.GenerateCreateScript()` y ejecuta solo los CREATE TABLE/INDEX de estas tablas si faltan. Si algún día se corre `dotnet ef migrations add`, esa migración incluirá estas tablas: quitar los CreateTable (ya existen) o borrar las tablas antes.

**Seguridad** (`Pages/Seguridad`): Index (KPIs, roles, usuarios bloqueados/pendientes, desbloquear) y Auditoria (filtros, paginación, CSV, resalta logins fallidos). Sin tablas nuevas (usa Usuarios/Roles/ActividadUsuario).

**Activos fijos** (`Pages/Activos`): Index, Edit (crea y edita), Details (baja con motivo), Depreciacion (línea recta mensual; un movimiento Egreso "solo contable" categoría 'Depreciación de activos fijos' enlazada a 6.2.08), Mantenimiento (programar, completar con pago opcional → egreso 'Mantenimiento y reparaciones' 6.2.06, cancelar). Un activo depreciado no permite cambiar costo/residual/vida/fecha. Serie de documento: ActivoFijo.

**Producción** (`Pages/Produccion`): Index, Edit, Details y Calidad. Planificada → Iniciar (descuenta insumos con InventarioHelper, origen 'Produccion') → Terminar (ingresa producto terminado con costo promedio) o Cancelar (devuelve insumos si estaba en proceso). Costo de insumos = PrecioCompra del producto al iniciar.

**Caja chica** (`Pages/Finanzas/CajaChica`): fondo = cuenta financiera activa con código CAJA-CHICA (o subtipo 'Chica'). Gasto Pendiente → Aprobar (crea Egreso real y descuenta el fondo; exige saldo) / Rechazar (motivo). Reponer fondo = Transferencia desde otra cuenta. Helper común `Helpers/Finanzas/MovimientoManualHelper.cs` (crea movimiento + SaldoHelper; el llamador abre la transacción).

**Inventario avanzado** (`Pages/InventarioAvanzado`): Kardex por producto, Valoración (a costo y a precio, por categoría, bajo mínimo, sin movimiento 90 días) y TomaFisica (Index/Details: congela stock del sistema, captura conteos, Aplicar ajusta con InventarioHelper.Ajustar origen 'TomaFisica'; solo una toma en conteo a la vez). No hay multi-bodega: el stock sigue siendo único por producto.

**Contabilidad** (`Pages/Finanzas/Asientos` y `Pages/Finanzas/Contabilidad`): asientos manuales en partida doble (valida cuadre, cuentas de movimiento, período cerrado), Details con anulación, Libro diario, Libro mayor (saldo corrido por naturaleza) y Balanza de comprobación. Estos libros usan SOLO asientos manuales; el Estado de Resultados y el Balance General siguen saliendo de los movimientos financieros.

**Permisos/menú**: nuevos submódulos en `ModulosERP` (Inventario: TomaFisica, Kardex, Valoracion; Finanzas: Asientos, LibroDiario, LibroMayor, Balanza, CajaChica; Activos: Depreciacion) y enlaces en `_Layout.cshtml`; PermisoSeeder crea las filas (solo Admin al inicio; asignar a otros roles en Usuarios → Roles). Tipos de documento nuevos: TomaFisica, AsientoContable, GastoCajaChica, ActivoFijo, OrdenProduccion.

**Pendiente de verificar por el usuario**: `dotnet build`, `dotnet test tests\Kirkenta.Tests`, y en el log de arranque `[Arranque] EsquemaModulosNuevos OK` y `[Arranque] Esquema de base de datos completo.`.

## Revisión general (2026-10-25)
- Rutas (RutaHelper): cerrar ruta cancelada bloqueado; envíos EnRuta/Pendiente sin resolver vuelven a Pendiente al cerrar; AgregarEnvio/QuitarEnvio guardan antes de recalcular totales (antes quedaban totales desfasados).
- RRHH: tabla de vacaciones se parsea con cultura invariante (evita fallo con coma decimal).
- Revisados sin cambios: ISVHelper, FacturacionFiscalHelper, NotaCreditoFiscalHelper, CierreContableHelper, DevolucionProveedorHelper, AperturaCuentaHelper, PermisoHelper, AlertaEnvioHelper.
- Pendiente de revisar a fondo: páginas de Logística/Finanzas/RRHH antiguas; DistribucionHelper sin transacción propia ni validación de cierre contable.

## Mejoras (2026-10-25)
- DistribucionHelper: transacción única + validación de cierre contable.
- Helpers/EsquemaIndices.cs: 15 índices de rendimiento creados al arrancar (idempotente; nombre IX_perf_*).
- tests/Kirkenta.Tests/ImpuestosCierresTests.cs: pruebas de ISV, cierre contable y antigüedad.
- tools/migrar-bd.ps1: migración completa (respaldo -> borra tablas nuevas vacías -> dotnet ef migrations add ModulosNuevos -> revisión -> database update).
- Numeración de series ya era atómica (UPDATE condicional con reintentos). Login ya tiene IntentosFallidos.
- Pendiente a propósito (rediseño grande, hacerlo con build verde): asiento de partida doble por documento, auditoría con valor anterior/nuevo, separar empresa/sucursal (EmpresaId=1 fijo), mover lógica de páginas a servicios.

## Revisión de páginas y auditoría (2026-10-25, 2da tanda)
- BUG corregido: Movimientos/Anular no revertía el saldo de la cuenta (SaldoHelper.Revertir exige Estado != Activo; ahora se marca Anulado antes y va en transacción).
- BUG corregido: Movimientos/Edit contaba doble el saldo (mismo motivo); ahora marca temporalmente Anulado, revierte lo viejo y reactiva. Valida monto > 0, concepto y cuenta activa.
- Conciliación: Eliminar línea banco / matching auto / manual / deshacer ya no funcionan sobre conciliaciones cerradas o canceladas.
- Nómina/Aprobar: rechazar exige motivo.
- Helpers/CambiosHelper.cs: arma "Campo: antes → después". Usado en Productos/Edit y Usuarios/Edit (bitácora de Actividad con valor anterior y nuevo).
- tests/Kirkenta.Tests/SaldoMovimientosTests.cs: pruebas de SaldoHelper (aplicar, revertir, transferencia, apertura).
- Ya existían: alertas en el inicio (CxC/CxP vencidas, CAI, stock bajo), registro público desactivado desde Program.cs, bloqueo de login por intentos.

## Asientos automáticos (2026-10-25, 3ra tanda)
- Helpers/Finanzas/AsientoAutomaticoHelper.cs: genera asientos de partida doble desde MovimientoFinanciero (Referencia "MOV:{id}", Numero "AUT-0000123").
  Ingreso: Debe cuenta financiera / Haber categoría. Egreso: Debe categoría / Haber cuenta financiera. Transferencia: Debe destino / Haber origen. Apertura: Debe cuenta / Haber 3.1.01. Solo-contable (5.1.01): contrapartida 1.1.03.001.
  Cuenta financiera -> plan: CuentaFinanciera.CuentaContable, o 1.1.01.001 (Caja) / 1.1.01.004 (Banco) / 1.1.02.001 (CxC). Categoría -> PlanCuentaId, o 4.2.02 / 6.3.02.
  Idempotente: si el movimiento cambia monto/fecha reemplaza el asiento; si se anula, anula el asiento.
- Se ejecuta: al arrancar (tarea AsientosAutomaticos), al abrir Libro diario/Mayor/Balanza (máx. 1 vez por minuto) y con el botón "Generar asientos automáticos" en Finanzas > Asientos.
- tests/Kirkenta.Tests/AsientoAutomaticoTests.cs.
- Ya existían (no se rehicieron): importación masiva (Productos/Clientes/Proveedores/Categorías/Impuestos/Unidades) y exportaciones CSV/Excel.
- (Resuelto en la 4ta tanda: EmpresaActual y primer servicio. Ver abajo.)

## Cuarta tanda (2026-10-25): empresa, paginación, seguridad, servicios
- Helpers/EmpresaActual.cs: UN solo punto para el EmpresaId (claim "EmpresaId" -> variable KIRKENTA_EMPRESA_ID -> 1). Reemplaza los ~48 `EmpresaId = 1` fijos y los 24 defaults de modelos. Middleware en Program.cs lo fija por petición. Sigue siendo UNA empresa: para multi-empresa real faltaría la tabla Empresa/Sucursal, el claim al iniciar sesión y filtrar consultas por EmpresaId (no hecho).
- Paginación (PagerInfo + _Pager, 50 por página) y totales/KPIs en la base de datos en: Pedidos (filtro del lado del servidor), Devoluciones, Devoluciones proveedor, Pagos proveedor (filtro Q y proveedor), Logística Envíos (KPIs con GroupBy/Count), Finanzas Cierres y Aperturas. Cobros ya no carga todas las facturas/clientes.
- Búsqueda global: Pages/Buscar (clientes, productos, proveedores, facturas, ventas, pedidos; respeta permisos). Caja en la barra superior y atajo Ctrl+K.
- POS: F2 buscar producto, F4 cliente, F9 cobrar, Esc cierra el cobro; Enter en el buscador agrega si hay una sola coincidencia o código/SKU exacto (lector de barras). Ahora también busca por código de barras.
- Auditoría con antes/después: Roles (permisos modificados), Configuración de empresa (CAI, rangos, RTN...), Plan de cuentas, Impuestos.
- Plan de cuentas blindado (Helpers/Finanzas/PlanCuentaProteccion.cs): no se cambia código/tipo ni se borra una cuenta que el sistema usa, que tiene asientos, hijos o está ligada a caja/banco/categorías.
- Impuestos: no se borra ni desactiva el predeterminado (el ISV saldría en 0); porcentaje 0-100. Métodos de pago: no se borran si tienen pagos a proveedor. Productos: no se borran con kardex/producción/calidad/toma física. Usuarios: no se borran con movimientos, pagos o cierres/aperturas.
- Helpers/PasswordPolicy.cs: contraseñas nuevas >= 8, letra + número, no común, no igual al usuario (Crear/Editar usuario y Cambiar contraseña).
- Rate limit del seguimiento público /Logistica/Tracking: 30 consultas por minuto por IP.
- tools/respaldo-bd.ps1 ahora VERIFICA el volcado (tamaño, "Dump completed", >=5 tablas, zip no vacío) y acepta -ArchivoOpciones (.cnf). tools/programar-respaldo.ps1 crea la tarea diaria de Windows (guarda usuario/clave en C:\Kirkenta-secretos, fuera del proyecto).
- Servicios: Services/PagoProveedorService.cs (primer flujo sacado de la página: valida, descuenta el saldo con UPDATE atómico, genera el egreso, todo en una transacción). Pages/PagosProveedor/Create solo valida permisos y muestra el resultado. Los demás flujos de dinero ya vivían en Helpers/ (Anulación, Devoluciones, Cierres, Distribución, MovimientoManual...); el resto de páginas se pasan a servicio de a poco con el mismo patrón.
- Depreciación: Helpers/Finanzas/DepreciacionHelper.cs (cálculo testeado).
- Nómina/Details: ya no se cae si hubiera dos pagos del mismo detalle.
- Pruebas nuevas: InventarioKardexTests, DepreciacionTests, SeguridadYProteccionTests (PasswordPolicy + PlanCuentaProteccion), PagoProveedorServiceTests.
- Controles de concurrencia: Producto ya tenía RowVersion; el saldo de la orden de compra ahora se descuenta con UPDATE condicional. Saldos de cuentas financieras siguen dentro de transacción sin token (agregar un RowVersion requiere migración).

## Quinta tanda: tema claro/oscuro y pulido minimalista
- Todo usuario (cualquier rol) elige tema en Configuración > Apariencia (Pages/Configuracion/Apariencia.cshtml) o con el botón sol/luna de la barra superior. Opciones: claro (por defecto), oscuro, igual que mi equipo.
- Preferencia guardada por cookie `kk_tema_u{userId}` (+ `kk_tema_last` para Login/Tracking). Es por navegador/dispositivo; para sincronizar entre equipos haría falta una columna en BD (migración).
- Helpers/TemaHelper.cs (Uid, Preferencia, Resuelto). Parciales Shared/_TemaHead (script temprano anti-parpadeo) y Shared/_TemaScripts (para el layout POS).
- `<html data-pref data-theme data-bs-theme>` en _Layout, _LayoutPOS, Login, Register y Tracking. JS: `window.kkTema.guardar(pref)` y `window.kkCss(nombre)` (lee variables CSS para Chart.js). Si hay gráficas, la página recarga al cambiar de tema.
- wwwroot/css/theme.css: tokens (`--color-*`, `--shadow-*`) en `:root` y override en `:root[data-theme="dark"]`. No usar colores hex fijos en vistas; usar `var(--color-...)`. Bloque final "PULIDO MINIMALISTA + TEMA": tablas aireadas, badges tipo pill, toasts, scrollbars, focus.
- Alertas de éxito se convierten en toasts que se cierran solos.
- Se arregló una línea suelta `using Kirkenta.Helpers;` al inicio de _Layout que se mostraba como texto.
- Facturas/Carta y Facturas/Ticket quedan siempre claros (documentos de impresión).
- tools/migrar-bd.ps1 reutiliza una migración ya creada en vez de fallar.

## WhatsApp (enlaces wa.me)
- Helpers/WhatsAppHelper.cs: Normalizar(telefono), Enlace(telefono, mensaje) y mensajes (factura, cotización, pedido, envío, cobranza, general). Usa los teléfonos ya guardados como "+504 9999-9999"; sin migración ni cuenta de Meta.
- Parcial Shared/_WhatsAppBtn (record WhatsAppBtn). Botones en: Facturas/Details, Cotizaciones/Details, Pedidos/Details, Clientes/Index, Clientes/EstadoCuenta (cobranza), Reportes/CuentasPorCobrar (columna), Logistica/Envios/Details (teléfono de contacto del envío).
- Si el teléfono no es válido el botón sale deshabilitado. Estilos `.btn-whatsapp` en theme.css (claro/oscuro).
- Pendiente opcional: WhatsApp Business Cloud API (envío automático) requiere cuenta Meta, plantillas aprobadas y migración para token/bitácora.
- Pruebas: tests/Kirkenta.Tests/WhatsAppHelperTests.cs.

## Asistente del sistema (chat flotante)
- Botón flotante en todas las pantallas (menos POS/Login) y pantalla /Asistente con el catálogo. Entiende español con un motor de reglas gratis y offline (Services/Asistente/AsistenteMotor.cs): normaliza texto, detecta fechas (hoy, ayer, esta semana, mes pasado, dd/mm/aaaa...), montos y la acción por frases con comodín (`vend*`).
- Catálogo único: Services/Asistente/AsistenteCatalogo.cs. Para agregar una acción basta con añadir un AccionDef (frases, permiso, Ejecutar). Cubre Ventas, Inventario, Compras, Finanzas, RRHH, Logística, Seguridad y "abre <pantalla>" para navegar a cualquier módulo.
- Permisos: cada acción valida con PermisoHelper (consultas: acceso al submódulo; operaciones: permiso crear/eliminar). 
- Operaciones que cambian datos (cerrar caja, anular factura) piden datos que falten, muestran resumen y exigen contraseña (propia o de otro usuario con permiso, ej. admin). Token firmado con DataProtection, vence en 5 min, un solo uso; 5 intentos fallidos bloquean 10 min (Usuario.IntentosFallidos/BloqueadoHasta). Usan los helpers existentes (CierreHelper, DistribucionHelper, AnulacionFacturaHelper). Información sensible (nómina) también pide contraseña. Todo queda en ActividadUsuario.
- Reportes salen en tabla con botones Imprimir/PDF (ventana de impresión del navegador) y CSV; sin dependencias externas.
- IA OPCIONAL (apagada por defecto): Services/Asistente/AsistenteIA.cs usa cualquier endpoint compatible con OpenAI/Ollama. Se configura (solo Admin) en Configuración > Asistente con IA (Pages/Configuracion/AsistenteIA): presets Ollama/OpenAI/Groq/OpenRouter, botón Probar conexión. La config se guarda en App_Data/asistente-ia.json (clave de API protegida con DataProtection; NO subir a git, agregar App_Data/ al .gitignore) vía AsistenteIAConfigStore (singleton). Respaldo: claves `Asistente:IA:Url/Modelo/ApiKey` de configuración. Solo recibe el texto del usuario y la lista de acciones (nunca datos); se usa solo cuando las reglas no entienden.
- Sin migración. Pruebas: tests/Kirkenta.Tests/AsistenteMotorTests.cs (incluye una que verifica que cada ejemplo del catálogo lleve a su propia acción).
- Acciones extra en Services/Asistente/AsistenteCatalogoExtra.cs (partial del catálogo): estado de rutas, envíos entregados/fallidos, estado de un envío, repartidores, mejores clientes, cotizaciones por vencer, devoluciones, bajas, órdenes de compra por recibir, permisos por aprobar, usuarios bloqueados y "qué tengo pendiente" (panorama de todos los módulos).
- Informe por método de pago (Helpers/Finanzas/InformeMetodosPagoHelper.cs + Pages/Finanzas/Cierres/Informe): cobros reales (tabla Pagos) agrupados por método (efectivo, tarjeta, transferencia y cualquier MetodoPago activo), ventas contado vs crédito, saldo de crédito por cobrar y salidas (egresos) por forma de pago. Filtros tipo chip (Todos / cada método / Crédito) y vista en tarjetas con detalle imprimible. Se abre desde Cierres/Details (rango apertura→cierre) y Cierres/Index (cualquier período). Asistente: acción `informe_metodos` ("ventas en efectivo de hoy", "cobros por transferencia de la semana"); al cerrar caja desde el asistente se ofrece el enlace al informe.
- Flexibilidad del asistente: el motor tolera 1 letra de error en las palabras clave (AsistenteMotor.TokenCoincide/Distancia) y hay más sinónimos. IA: catálogo compacto (id: título) y respuesta JSON corta para modelos locales en CPU; diagnóstico del último intento en Configuración > Asistente con IA.
- IMPORTANTE (.NET 10 + EF Core): en consultas LINQ a BD NO usar `new[] {..}.Contains(x)` (se enlaza a ReadOnlySpan y revienta); usar `List<string>`.
- Informe por método de pago DINÁMICO: parcial Pages/Shared/_InformeMetodos + wwwroot/js/informe-metodos.js (+ css/informe-metodos.css). Pestañas Todo / cada método activo / Crédito que recalculan tarjetas, salidas y detalle sin recargar (todo con textContent). Se usa en Cierres/Informe, Cierres/Details, Cierres/Aprobar y como informe PREVIO dentro de Cierres/Create (handler `?handler=Previo`, calcula desde la apertura activa hasta ahora).
- Cierres/Create: conteo opcional de vouchers/comprobantes por método (se guarda como texto en Notas, sin migración, límite 380 caracteres) y opción "Cerrar sin contar" que exige usuario+contraseña de un Admin (Helpers/AutorizacionHelper.VerificarAdmin, bloqueo tras 5 intentos); toma el efectivo esperado, omite distribuciones y deja la marca "[CIERRE SIN CONTEO FÍSICO, autorizado por X]" en las notas y en la bitácora.
- Asistente abierto: decir solo la palabra de un tema lista datos (productos, clientes, proveedores, empleados con nombres, usuarios, categorías, facturas; ver Services/Asistente/AsistenteCatalogoListados.cs) y se puede filtrar agregando un nombre ("productos de limpieza"). Decir solo un módulo ("ventas", "finanzas", "rrhh", "inventario", "compras", "logística", "seguridad", "qué puedo hacer en ventas") muestra sus opciones + enlaces a sus pantallas (AsistenteMotor.ModuloMencionado + AsistenteService.MenuModulo). Si no entiende, ofrece los módulos como botones.

## Mejoras de gestión (2026-10-25, noche)
- **Recordatorios de cobro por WhatsApp**: `Pages/Finanzas/Cobros/Recordatorios` (un renglón por cliente, mensaje formal `WhatsAppHelper.MensajeCobranza`, botón "Enviar al siguiente cliente"). Cada envío queda en `ActividadUsuario` con Accion "Recordatorio de cobro" y Detalle `cli:{id}|{nombre}|L. saldo` (de ahí sale "último aviso"; sin tablas nuevas). Usa el permiso Finanzas/Cobros. Botón desde Cobros/Index y acceso rápido en el inicio.
- **Respaldo**: `Helpers/RespaldoEstado` lee `respaldos/*.zip` (>=512 bytes) y avisa si el último tiene más de 36 h (danger a 3+ días). Alerta en el inicio y tarjeta en `Seguridad/Index` (solo con permiso Seguridad). `tools/probar-restauracion.ps1` restaura el último zip en la BD temporal `kirkenta_prueba_restore`, compara tablas y filas clave contra la real y la borra.
- **Sugerencia de compras**: `Helpers/SugerenciaComprasHelper` (sugerido = venta diaria × días a cubrir + mínimo − stock − en camino; proveedor y costo de la última OC) y página `Pages/Compras/Sugerencias` (permiso Compras/Ordenes).
- **Panel del dueño**: `Pages/Reportes/PanelDueno` (permiso Reportes/Index): mes vs mes pasado al mismo día, utilidad bruta (DetalleVenta.Subtotal − CostoTotalLinea), ticket, por cobrar, inventario parado 60 días.
- **Resumen diario por correo**: `Services/ResumenDiarioService` (HostedService, apagado hasta configurar). Config por variables de entorno o user-secrets (NO appsettings): `Correo__Host`, `Correo__Puerto` (587), `Correo__Ssl`, `Correo__Usuario`, `Correo__Clave`, `Correo__Desde`, `Correo__Destinatarios` (coma), `Correo__HoraResumen` (20:00). Contenido en `Helpers/ResumenNegocioHelper`.
- **Asistente**: nuevas acciones en `AsistenteCatalogoNegocio.cs`: recordatorios_cobro, respaldo_estado, sugerencia_compras, panel_dueno y aprobaciones con contraseña (aprobar_vacaciones, aprobar_permiso, aprobar_vale; el vale respeta la doble aprobación de personas distintas). Seguimiento: si el mensaje no pide nada nuevo pero trae otra fecha ("¿y ayer?") o empieza con "y/solo" repite la última consulta (`ContextoCliente.UltimaAccion/UltimaParams`, `RespuestaAsistente.Seguimiento`, `ContinuarConsulta`). Al abrir el chat (una vez por sesión) avisa si hay pendientes; `pendientes` ahora incluye vacaciones, vales y respaldo.
- **Motor**: la tolerancia a errores de escritura ahora exige prefijos de 6+ letras (con 5 "pendientes" coincidía con "vendi*") y una coincidencia exacta gana sobre una aproximada (`PuntuarDetalle`). Arregla empates de "vales pendientes", "vacaciones pendientes", "abre conciliación bancaria", "repartidores", "cuántos empleados hay".
- **Ya existía y no se tocó**: CAI/SAR completo (FacturacionFiscalHelper, NC propia, alertas), auditoría con antes/después (CambiosHelper) y Seguridad/Auditoria, respaldo programado con verificación, Producto.RowVersion.
- **No se hizo (requiere migración y compilar en tu máquina)**: RowVersion en el resto de tablas, conteo por método de pago del cierre en tabla propia, multi-sucursal.
- **Respaldo completo del sistema** (tools/): `respaldo-completo.ps1` (BD verificada + wwwroot/uploads + App_Data + storage + imágenes + código + MANIFEST.json con SHA256, verifica el zip y copia a `-CopiaExterna` con rotación; appsettings solo con `-IncluirConfiguracion`), `restaurar-sistema.ps1` (revisa sin tocar; restaura código/archivos/BD; no pisa una BD con tablas salvo `-Reemplazar` + escribir RESTAURAR), `programar-respaldo-completo.ps1` (tarea semanal) y `LEEME-RECUPERACION.md` (pasos de recuperación en equipo nuevo). Scripts sin probar en la máquina real todavía.

- **Respaldo/restauración dentro del sistema** (Admin): `Services/RespaldoSistemaService` crea un .zip (`manifest.json` + `data/<tabla>.jsonl` con tipos + `archivos/` de wwwroot/uploads, wwwroot/images y App_Data) en `respaldos/` (nombre `kirkenta-datos_*.zip`); verifica el zip. Restaurar: valida versión de migración, crea antes un respaldo `previo-restauracion`, borra e inserta todas las tablas en UNA transacción con FK desactivadas, compara conteos y revierte si algo no cuadra; si la BD está vacía ejecuta MigrateAsync. Página `Pages/Configuracion/Respaldos` (contraseña + escribir RESTAURAR, subir .zip, descargar). `RespaldoAutomaticoService`: diario 02:00, conserva 14 (`Respaldo:Automatico/Hora/Conservar/CopiaExterna`). Sin probar en la máquina real.
- **Conteo por método en tabla propia**: `Models/CierreCajaConteo` (tabla `ConteosCierreCaja`, creada al arrancar por `EsquemaModulosNuevos`; si usas `dotnet ef migrations add` recuerda que la tabla ya existe). `Cierres/Create` guarda una fila por método (Efectivo + lo contado en pantalla: sistema, contado, diferencia) y `Cierres/Details` la muestra. Ya no se mete en Notas.
- **Restaurar por módulo**: `RespaldoSistemaService.Grupos` (Clientes, Productos e inventario, Ventas, Compras, Finanzas, RRHH, Logística, Activos/producción, Usuarios/config) → `RestaurarAsync(ruta, soloTablas, archivos)`. Misma contraseña + escribir RESTAURAR; también hace respaldo previo.
- **Aviso de fallo del respaldo**: `RespaldoEstado.RegistrarError/LimpiarError/LeerError/Avisar` (archivo `respaldos/ultimo-error-respaldo.txt`, se borra con el siguiente respaldo exitoso). Se muestra en el panel de inicio, Seguridad, página de Respaldos y asistente ("pendientes", "respaldo"); si `Correo:*` está configurado también manda correo (`RespaldoAutomaticoService`).
- **Etiquetas de código de barras** (Inventario): `Helpers/CodigoBarrasHelper` (Code 128 propio → SVG, sin librerías; EAN-13 interno `CodigoInterno(id)` = "2"+Id(11 díg)+verificador), `Helpers/EtiquetasHelper` (formatos: rollos 50x25, 40x30, 60x40, 30x20 y hojas A4 de 21/24/65), páginas `Productos/Etiquetas` (elegir productos/cantidades, botón "Asignar códigos internos" a los que no tienen) y `Productos/EtiquetasImprimir` (hoja sin menús lista para imprimir). Si el producto no tiene CodigoBarras se usa el SKU. Link en menú Inventario y en Productos.
- **Escáner con cámara en el POS**: `wwwroot/js/escaner-camara.js` (BarcodeDetector nativo; Chrome/Edge/Android; requiere https o localhost) + botón 📷 en el buscador del POS; reutiliza el Enter del buscador (igual que la pistola USB), escaneo continuo con pitido.
- **Asistente de configuración inicial** (solo Admin): `Pages/Configuracion/Inicio` (5 pasos: empresa → SAR → caja/banco → categorías/unidades → lista de pendientes), `Helpers/ConfiguracionInicialHelper` (estado, parseo). Aviso en el inicio mientras falte empresa/caja/catálogo y link en Configuración. Impuestos y métodos de pago ya los siembran los seeders.

- **Declaración mensual de ISV** (Reportes): `Helpers/DeclaracionISVHelper` (débito de ventas, crédito de compras recibidas, saldo a favor, retenciones), `Pages/Reportes/DeclaracionISV` (CSV incluido) y `LibroCompras`. Asistente: acción `isv_mes`.
- **POS sin internet**: `wwwroot/sw.js` (service worker: caché de estáticos y de /POS y /Movil; no toca POST; borra páginas guardadas al cerrar sesión), `wwwroot/js/kirkenta-offline.js` (cola en IndexedDB `kirkenta-offline`, sincroniza al volver la red, aviso flotante con panel de pendientes/rechazadas), `wwwroot/offline.html`. El POS manda siempre un `offlineId` (GUID); el handler `RegistrarVenta` lo guarda en la tabla `VentasOffline` y si llega repetido devuelve la misma venta (sin duplicar). Requiere https o localhost.
- **Panel móvil / app instalable**: `Pages/Movil/Index` (resumen del dueño, respeta permisos; Layout=null), `wwwroot/manifest.webmanifest` + `wwwroot/images/pwa/icon-*.png`; el manifiesto y el theme-color se enlazan desde `_Layout` y `_LayoutPOS`.
- **Catálogo en línea**: página pública `/Catalogo` (AllowAnonymous en Program.cs; `wwwroot/js/catalogo.js`; solo existe si el Admin lo activa en Configuración → Catálogo en línea `Pages/Configuracion/Catalogo`). El pedido se guarda en `PedidosWeb` (precios recalculados en servidor con `CatalogoWebHelper`, antispam: campo trampa + máx. 5 pedidos/10 min por IP) y abre WhatsApp. Bandeja: `Pages/PedidosWeb/Index` (permiso Ventas/Pedidos). Tablas nuevas: `VentasOffline`, `CatalogoWebConfig`, `PedidosWeb` (creadas al arrancar por EsquemaModulosNuevos y incluidas en el respaldo).

- **Foto de factura → orden de compra con IA**: se sube la foto de la factura del proveedor, la IA (Claude o Gemini; Groq es solo texto y no lee imágenes) extrae proveedor, productos, cantidades y costos y arma la orden de compra para revisar antes de guardar.
- **Proveedores de IA** (`Pages/Configuracion/AsistenteIA`, `Services/Asistente/AsistenteIA.cs`, config en `App_Data/asistente-ia.json`, clave protegida con Data Protection): presets Anthropic, Gemini, Groq, OpenAI/OpenRouter. Botón "Ver modelos disponibles" (`ListarModelosAsync`, GET /models) para elegir un modelo que la clave sí pueda usar (un 404 `model_not_found` es permiso de la cuenta). "Probar conexión" recarga la página y vacía el campo de clave: pegar la clave y pulsar Guardar directo. Guardar exige clave si el proveedor es en la nube. La clave NUNCA se pega en el chat. `RespuestaAsistente.Origen` ("ia" | "ia-sin-respuesta" | "ia-apagada") → insignias en el chat (`asistente.js`).
- **Pedidos web → POS**: en `Pages/PedidosWeb/Index` el botón "🛒 Cobrar en POS" aparece para todo pedido no Cancelado y no cobrado (las Notas contienen "Cobrado en POS" cuando ya se cobró, entonces muestra "✅ Ya cobrado"). `Pages/POS/Index.cshtml.cs` carga el pedido, y al vender lo marca Atendido (si no lo estaba) y lo marca cobrado.
- **Asistente robusto** (`Services/Asistente/AsistenteLenguaje.cs`): antes de las reglas normaliza lenguaje coloquial (plata/pisto/lana→dinero, "al día", "cuánto llevamos", "qué se está acabando", "quién me debe", "a quién le debo", "pendientes"...). Además charla (saludos), "módulos" (`MenuModulos`) y sugerencias (máx. 3) cuando no entiende (`NoEntendi`). Orden: reglas → IA (solo si las reglas no entienden) → sugerencias. `asistente.js` colapsa los chips según el tamaño del panel (3/5/8) con "Ver N más ▾". Pruebas: `tests/Kirkenta.Tests/AsistenteLenguajeTests.cs`; verificación de acciones con `verifica_asistente.py` (scratchpad).
- **Alerta de rango SAR**: `FacturacionFiscalHelper.AlertaRango` (peligro ≤10 números o agotado, aviso ≤50); se muestra en el inicio (`Pages/Index.cshtml.cs`) y en el panel móvil.
- **Respaldo automático configurable**: `Services/RespaldoConfigStore` (`App_Data/respaldo-config.json`: Automatico, Hora, Conservar, CopiaExterna; estado de la última copia en `respaldo-copia-estado.json`; si no existe usa `Respaldo:*` de appsettings). `RespaldoAutomaticoService` lee la config en cada vuelta, evita repetir el respaldo del día, copia a la carpeta externa (USB/red, conserva los N últimos, nunca lanza) y avisa por correo si falla la copia. UI en `Configuracion/Respaldos` (tarjeta "⏰ Respaldo automático" + probar carpeta). Pruebas: `RespaldoConfigTests`.
- **Impresión térmica ESC/POS + gaveta**: `Services/ImpresoraConfigStore` (`App_Data/impresora.json`: Modo ninguna|red|windows, Host/Puerto 9100, Impresora, Columnas 32/42/48, AbrirGaveta, Auto ninguno|venta|factura, Copias, Pie), `Helpers/TicketTermico.cs` (`EscPosBuilder`, CP858, limpia caracteres de control), `Services/ImpresoraTermicaService` (TCP 9100 con timeout 3 s o RAW de Windows vía winspool; prueba, gaveta, factura, venta, auto). Página `Configuracion/Impresora` (solo Admin), endpoint `Impresion/Directa` (POST Factura/Venta/Gaveta, JSON), `wwwroot/js/kirkenta-impresion.js` (`[data-imprimir-directo]`), botón "🖨️ Imprimir directo" en `Facturas/Details`. Impresión automática al cobrar en el POS y al emitir factura desde venta. Sin hardware probado aún. Pruebas: `ImpresoraTermicaTests`.
- **Servidor local / HTTPS / arranque** (`tools/`, guía `tools/LEEME-SERVIDOR-LOCAL.md`): `https-local.ps1` (admin; certificado autofirmado con SAN, `certs/kirkenta-local.pfx/.cer`, `appsettings.Production.json` con Kestrel http://localhost:5114 y https://0.0.0.0:5115, regla de firewall), `instalar-arranque-automatico.ps1` (tarea programada "Kirkenta-Servidor" al iniciar sesión, `-Quitar` la elimina), `iniciar-servidor.cmd` (Production, log en `logs/servidor.log`). HTTPS es necesario porque `Seguridad:CookieSoloHttps=true` y para el POS offline/cámara desde otros equipos de la LAN; en cada equipo cliente hay que instalar el `.cer`.
- **Pendiente / no implementado**: sincronización multi-sucursal offline, tablero del dueño ampliado, alertas sonoras internas. Nada de lo de esta sección se compiló en el entorno de desarrollo (sin dotnet): compilar y correr `dotnet test tests\Kirkenta.Tests`.

- **Licencias de uso (sin internet)** (`Services/Licencia/`): `LicenciaFormato` (licencia `KRK1-<datos>.<firma>`, ECDSA P-256; compartido con la herramienta del emisor), `LicenciaClavePublica` (clave pública; VACÍA = licencias desactivadas, modo desarrollo), `LicenciaService` (singleton: carga/activa, `Evaluar` pura y probada, latido cada minuto, estado firmado con HMAC guardado en 4 lugares: `App_Data/licencia.dat`, ProgramData, LocalAppData y tabla `LicenciaEstado` en la BD) y `LicenciaMiddleware` (sin licencia válida todo redirige a `/Licencia`; peticiones no GET reciben 402). Reglas: licencia ligada al código de la PC (`XXXX-XXXX-XXXX-XXXX`, MachineGuid o `App_Data/instalacion.id` en contenedor) o `*`; la vigencia cuenta desde la activación (o desde `Inicio` si viene fijo); se guarda la fecha más avanzada vista y un retroceso de reloj >15 min bloquea hasta una licencia nueva; cada licencia trae `ActivarAntes` (demo 3 días, normal 30). Pantalla `Pages/Licencia/Index` (anónima solo si está bloqueado; con licencia solo ve/cambia el Admin), aviso de ≤7 días en `_Layout`, menú Configuración → 🔑 Licencia. Emisor: `tools/Licenciador` (+ `tools/licenciador.cmd`, guarda llave privada y licencias en `%USERPROFILE%\KirkentaLicencias`, fuera del proyecto). Planes: demo7/15/30, 6m, 1a, 3a, 5a, perpetua. Pruebas: `LicenciaTests`. Guía: `tools/LEEME-DOCKER-Y-LICENCIAS.md`.
- **Instalación limpia**: al arrancar, si la BD está vacía `EnsureCreated()` crea las tablas del modelo y `InstalacionInicialHelper` crea el admin inicial (`Inicial:AdminUsuario/AdminClave`; si no hay clave se genera y queda en `App_Data/primer-acceso.txt`). Se puede apagar con `Mantenimiento:CrearEsquemaSiVacia=false`.
- **Entrega sin código fuente**: `Dockerfile` (multi-stage; falla si falta la clave pública de licencias; vacía la cadena de conexión de appsettings; zona horaria America/Tegucigalpa; volúmenes App_Data, logs, respaldos, wwwroot/uploads), `docker-compose.yml` (MySQL 8 + sistema + perfil `https` con Caddy), `.env.example`, `Caddyfile`, `.dockerignore`; ejecutable Windows con `tools/publicar-ejecutable.ps1`. `Kirkenta.csproj` excluye `tools\**` de la compilación.
- **Instalador Setup.exe (Windows)**: `instalador/kirkenta.iss` (Inno Setup 6) + `instalador/scripts/` (`instalar.ps1`: MySQL portable propio como servicio `KirkentaMySQL` puerto 3307 con claves aleatorias, crea BD/usuario, escribe `appsettings.Production.json`, tarea programada `Kirkenta-Servicio` como SYSTEM con `Iniciar-Servicio.cmd` que reinicia si se cae, regla de firewall 5114; `desinstalar.ps1` conserva los datos). Se genera con `tools/crear-instalador.ps1 -MySqlZip <zip> -Version x.y.z` (requiere Inno Setup, ZIP de MySQL 8.0 y opcional `vc_redist.x64.exe` en `instalador/recursos`). Sin probar en Windows al momento de escribirlo.
- **Licencias cortas**: `LicenciaDatos.Minutos` (30 min, 1 h, 3 h, 12 h; llevan `Dias=1` para que una versión vieja nunca las tome como ilimitadas). Planes del licenciador: min30, hora1, horas3, horas12 (o `--minutos N`). Tolerancia de retroceso de reloj: 5 min.
