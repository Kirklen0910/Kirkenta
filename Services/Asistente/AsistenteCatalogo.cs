using System.Globalization;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Services.Asistente
{
    /// <summary>
    /// Todo lo que el asistente sabe hacer. Para agregar una acción nueva basta con añadir un AccionDef a la lista:
    /// el motor, los permisos, la confirmación con contraseña y la bitácora ya están resueltos.
    /// </summary>
    public static partial class AsistenteCatalogo
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static string L(decimal v) => "L. " + v.ToString("N2", Inv);
        private static string F(DateTime d) => d.ToString("dd/MM/yyyy", Inv);
        private static List<string> Fila(params string[] c) => c.ToList();
        private const int Limite = 200;

        public static IReadOnlyList<AccionDef> Todas { get; } = Construir();

        public static AccionDef? Buscar(string? id) => Todas.FirstOrDefault(a => a.Id == id);

        private static string Nombre(Empleado e) => $"{e.Nombres} {e.Apellidos}".Trim();

        private static bool Acceso(ContextoAccion c, string modulo, string sub)
            => PermisoHelper.UsuarioTieneAcceso(c.Db, c.Usuario.Username, modulo, sub);

        private static readonly string[] MesesEs =
            { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };
        private static string MesNombre(int mes) => MesesEs[Math.Clamp(mes, 1, 12) - 1];

        private static string Nota(int total) => total > Limite ? $" (se muestran los primeros {Limite})" : "";

        // ════════════════════════════════════════════════════════════════
        private static List<AccionDef> Construir()
        {
            var lista = new List<AccionDef>
            {
                // ───────── General ─────────
                new() { Id = "ayuda", Modulo = "General", Titulo = "Ayuda", Descripcion = "Lista lo que puedo hacer por ti",
                    Ejemplo = "ayuda", Frases = new[] { "ayud*", "puedes hacer", "comando*", "opcion*" }, Ejecutar = Ayuda },
                new() { Id = "resumen_dia", Modulo = "General", Titulo = "Resumen del negocio", Descripcion = "Cómo va el negocio: ventas, caja y pendientes",
                    Ejemplo = "cómo vamos hoy", UsaFechas = true,
                    Frases = new[] { "como vamos", "como va negocio", "resumen dia", "resumen negocio", "panorama" }, Ejecutar = ResumenDia },

                // ───────── Ventas ─────────
                new() { Id = "ventas_resumen", Modulo = "Ventas", Titulo = "Ventas del período", Descripcion = "Lista y total de ventas (hoy, ayer, este mes, fechas...)",
                    Ejemplo = "ventas de hoy", UsaFechas = true, PermModulo = "Ventas", PermSub = "Index",
                    Frases = new[] { "venta*", "vendi*", "vendimos", "vendio" }, Ejecutar = VentasResumen },
                new() { Id = "ventas_top", Modulo = "Ventas", Titulo = "Productos más vendidos", Descripcion = "Ranking de productos por cantidad vendida",
                    Ejemplo = "productos más vendidos del mes", UsaFechas = true, PermModulo = "Reportes", PermSub = "Ventas",
                    Frases = new[] { "mas vendid*", "producto* mas vendid*", "top producto*", "mejor* producto*", "venta* producto*" }, Ejecutar = VentasTop },
                new() { Id = "cxc", Modulo = "Ventas", Titulo = "Cuentas por cobrar", Descripcion = "Clientes que nos deben y cuánto está vencido",
                    Ejemplo = "cuentas por cobrar", PermModulo = "Reportes", PermSub = "CuentasPorCobrar",
                    Frases = new[] { "cuenta* cobrar", "por cobrar", "cartera", "antiguedad saldo*", "moroso*", "cobranza", "quien* deb*", "deuda* client*", "client* moroso*", "quien* pagar*", "client* atrasado*", "client* deben", "pendiente* cobro*" }, Ejecutar = Cxc },
                new() { Id = "saldo_cliente", Modulo = "Ventas", Titulo = "Saldo de un cliente", Descripcion = "Estado de cuenta resumido, con botón de WhatsApp para cobrar",
                    Ejemplo = "saldo de Alfredo Nahun", PermModulo = "Ventas", PermSub = "Clientes",
                    Frases = new[] { "saldo", "saldo client*", "estado cuenta", "cuanto deb*", "cuanto me deb*", "saldo pendiente" }, Ejecutar = SaldoCliente },
                new() { Id = "cotizaciones_pendientes", Modulo = "Ventas", Titulo = "Cotizaciones abiertas", Descripcion = "Cotizaciones en borrador o enviadas sin respuesta",
                    Ejemplo = "cotizaciones pendientes", PermModulo = "Ventas", PermSub = "Cotizaciones",
                    Frases = new[] { "cotizacion*", "cotizacion* pendiente*", "cotizacion* abierta*" }, Ejecutar = CotizacionesPendientes },
                new() { Id = "pedidos_pendientes", Modulo = "Ventas", Titulo = "Pedidos pendientes", Descripcion = "Pedidos por preparar, enviar o entregar",
                    Ejemplo = "pedidos pendientes", PermModulo = "Ventas", PermSub = "Pedidos",
                    Frases = new[] { "pedido*", "pedido* pendiente*", "pedido* por entregar" }, Ejecutar = PedidosPendientes },
                new() { Id = "facturas_pendientes", Modulo = "Ventas", Titulo = "Facturas con saldo", Descripcion = "Facturas emitidas que aún no están pagadas",
                    Ejemplo = "facturas pendientes de pago", PermModulo = "Ventas", PermSub = "Facturas",
                    Frases = new[] { "factura* pendiente*", "factura* vencida*", "factura* sin pagar", "factura* por cobrar", "factura* abierta*" }, Ejecutar = FacturasPendientes },
                new() { Id = "anular_factura", Modulo = "Ventas", Titulo = "Anular factura", Descripcion = "Anula una factura (devuelve inventario y revierte pagos). Pide contraseña",
                    Ejemplo = "anular la factura 000123", Tipo = TipoAccion.Operacion, RequierePassword = true,
                    PermModulo = "Ventas", PermSub = "Facturas", PermAccion = "eliminar",
                    Frases = new[] { "anul* factura*", "cancel* factura*", "invalid* factura*" }, Preparar = PrepararAnularFactura, Ejecutar = EjecutarAnularFactura },

                // ───────── Inventario ─────────
                new() { Id = "stock_bajo", Modulo = "Inventario", Titulo = "Productos con stock bajo", Descripcion = "Productos en o por debajo del mínimo",
                    Ejemplo = "productos con stock bajo", PermModulo = "Inventario", PermSub = "Productos",
                    Frases = new[] { "stock bajo", "bajo minimo", "bajo stock", "poco stock", "por agotar*", "agotad*", "sin stock", "stock minimo", "reponer", "reabastec*", "inventario bajo", "producto* agot*", "que comprar" }, Ejecutar = StockBajo },
                new() { Id = "buscar_producto", Modulo = "Inventario", Titulo = "Buscar producto", Descripcion = "Precio y existencias de un producto",
                    Ejemplo = "precio de cable", PermModulo = "Inventario", PermSub = "Productos",
                    Frases = new[] { "precio*", "buscar producto*", "existencia*", "cuantos hay", "tenemos", "stock de" }, Ejecutar = BuscarProducto },
                new() { Id = "valor_inventario", Modulo = "Inventario", Titulo = "Valor del inventario", Descripcion = "Cuánto vale la mercadería (costo y venta)",
                    Ejemplo = "valor del inventario", PermModulo = "Inventario", PermSub = "Valoracion",
                    Frases = new[] { "valor inventario", "valoracion", "cuanto vale inventario", "costo inventario", "inventario valorado" }, Ejecutar = ValorInventario },

                // ───────── Compras ─────────
                new() { Id = "cxp", Modulo = "Compras", Titulo = "Cuentas por pagar", Descripcion = "Lo que debemos a proveedores",
                    Ejemplo = "cuentas por pagar", PermModulo = "Compras", PermSub = "CuentasPorPagar",
                    Frases = new[] { "cuenta* pagar", "por pagar", "deuda* proveedor*", "cuanto debemos", "debemos proveedor*", "debo proveedor*" }, Ejecutar = Cxp },

                // ───────── Finanzas ─────────
                new() { Id = "saldos_cuentas", Modulo = "Finanzas", Titulo = "Saldos de cuentas", Descripcion = "Dinero en cajas y bancos",
                    Ejemplo = "saldos de cuentas", PermModulo = "Finanzas", PermSub = "Cuentas",
                    Frases = new[] { "saldo* cuenta*", "saldo* banco*", "saldo* caja*", "cuanto dinero", "dinero tenemos", "dinero hay", "saldo* disponible*", "cuentas bancaria*", "cuenta*", "banco*" }, Ejecutar = SaldosCuentas },
                new() { Id = "movimientos", Modulo = "Finanzas", Titulo = "Movimientos financieros", Descripcion = "Ingresos y egresos del período",
                    Ejemplo = "gastos de este mes", UsaFechas = true, PermModulo = "Finanzas", PermSub = "Movimientos",
                    Frases = new[] { "movimiento*", "gasto*", "egreso*", "ingreso*", "salida* dinero", "entrada* dinero", "en que gastamos" }, Ejecutar = Movimientos },
                new() { Id = "resumen_financiero", Modulo = "Finanzas", Titulo = "Resultado del período", Descripcion = "Ingresos, egresos y utilidad",
                    Ejemplo = "cuánto ganamos este mes", UsaFechas = true, PermModulo = "Finanzas", PermSub = "Reportes",
                    Frases = new[] { "utilidad", "ganancia*", "cuanto gan*", "estado resultado*", "flujo caja", "flujo efectivo", "ingreso* egreso*", "resultado*" }, Ejecutar = ResumenFinanciero },
                new() { Id = "estado_caja", Modulo = "Finanzas", Titulo = "Estado de la caja", Descripcion = "Cajas abiertas y efectivo esperado",
                    Ejemplo = "estado de la caja", PermModulo = "Finanzas", PermSub = "Aperturas",
                    Frases = new[] { "estado caja", "caja abierta*", "cuanto hay caja", "cuanto efectivo", "efectivo esperado", "arqueo", "como esta caja", "caja como va", "dinero caja" }, Ejecutar = EstadoCaja },
                new() { Id = "ultimos_cierres", Modulo = "Finanzas", Titulo = "Últimos cierres de caja", Descripcion = "Historial de cierres con diferencias",
                    Ejemplo = "últimos cierres de caja", PermModulo = "Finanzas", PermSub = "Cierres",
                    Frases = new[] { "ultim* cierr*", "ultim* cierr* caja", "historial cierr*", "cierres anteriores", "cierres", "cierres caja" }, Ejecutar = UltimosCierres },
                new() { Id = "cierre_caja", Modulo = "Finanzas", Titulo = "Cerrar caja", Descripcion = "Hace el cierre de caja del día con arqueo y distribución. Pide contraseña",
                    Ejemplo = "cierra la caja de hoy", Tipo = TipoAccion.Operacion, RequierePassword = true,
                    PermModulo = "Finanzas", PermSub = "CierresCreate", PermAccion = "crear",
                    Frases = new[] { "cerr* caja", "cierra* caja", "cierre caja", "hacer cierre", "procesa* cierre", "cuadre caja", "cuadrar caja" }, Preparar = PrepararCierreCaja, Ejecutar = EjecutarCierreCaja },

                // ───────── RRHH ─────────
                new() { Id = "empleados", Modulo = "RRHH", Titulo = "Empleados activos", Descripcion = "Cuántos empleados hay y en qué departamentos",
                    Ejemplo = "cuántos empleados hay", PermModulo = "RRHH", PermSub = "Empleados",
                    Frases = new[] { "empleado*", "cuantos empleado* hay", "personal", "plantilla", "trabajador*" }, Ejecutar = Empleados },
                new() { Id = "cumpleanos", Modulo = "RRHH", Titulo = "Cumpleaños del mes", Descripcion = "Empleados que cumplen años este mes",
                    Ejemplo = "cumpleaños de este mes", PermModulo = "RRHH", PermSub = "Empleados",
                    Frases = new[] { "cumple*" }, Ejecutar = Cumpleanos },
                new() { Id = "vales_pendientes", Modulo = "RRHH", Titulo = "Vales en trámite", Descripcion = "Vales o adelantos pendientes de aprobar o entregar",
                    Ejemplo = "vales pendientes", PermModulo = "RRHH", PermSub = "Vales",
                    Frases = new[] { "vale*", "adelanto*", "prestamo* empleado*" }, Ejecutar = ValesPendientes },
                new() { Id = "vacaciones", Modulo = "RRHH", Titulo = "Vacaciones", Descripcion = "Solicitudes y vacaciones próximas",
                    Ejemplo = "vacaciones pendientes", PermModulo = "RRHH", PermSub = "Vacaciones",
                    Frases = new[] { "vacacion*" }, Ejecutar = Vacaciones },
                new() { Id = "nomina_resumen", Modulo = "RRHH", Titulo = "Resumen de nómina", Descripcion = "Últimas nóminas y totales. Información sensible: pide contraseña",
                    Ejemplo = "resumen de nómina", RequierePassword = true, PermModulo = "RRHH", PermSub = "Nomina",
                    Frases = new[] { "nomina*", "planilla*", "sueldo*", "salario*" }, Ejecutar = NominaResumen },

                // ───────── Logística ─────────
                new() { Id = "envios_pendientes", Modulo = "Logística", Titulo = "Envíos pendientes", Descripcion = "Envíos por despachar o en camino",
                    Ejemplo = "envíos pendientes", PermModulo = "Logistica", PermSub = "Envios",
                    Frases = new[] { "envio*", "envio* pendiente*", "entrega* pendiente*", "paquete*" }, Ejecutar = EnviosPendientes },
                new() { Id = "rutas", Modulo = "Logística", Titulo = "Rutas de reparto", Descripcion = "Rutas del período y avance de entregas",
                    Ejemplo = "rutas de hoy", UsaFechas = true, PermModulo = "Logistica", PermSub = "Rutas",
                    Frases = new[] { "ruta*", "reparto*" }, Ejecutar = Rutas },

                // ───────── Seguridad ─────────
                new() { Id = "actividad", Modulo = "Seguridad", Titulo = "Actividad reciente", Descripcion = "Qué han hecho los usuarios en el sistema",
                    Ejemplo = "actividad reciente", PermModulo = "Seguridad", PermSub = "Auditoria",
                    Frases = new[] { "actividad*", "bitacora", "auditoria", "quien hizo", "quien* conect*", "quien entro" }, Ejecutar = Actividad },
            };

            lista.AddRange(Extra());
            lista.AddRange(Listados());
            lista.AddRange(Negocio());
            lista.AddRange(Navegacion());
            return lista;
        }

        // ════════════════════════════════════════════════════════════════
        //  NAVEGACIÓN: "abre clientes", "llévame a nómina"... cubre todo el sistema
        // ════════════════════════════════════════════════════════════════
        private static IEnumerable<AccionDef> Navegacion()
        {
            // (id, título, url, módulo, submódulo, palabras clave)
            var destinos = new (string id, string titulo, string url, string? mod, string? sub, string[] claves)[]
            {
                ("clientes", "Clientes", "/Clientes", "Ventas", "Clientes", new[] { "client*" }),
                ("pos", "Punto de venta", "/POS", "Ventas", "POS", new[] { "pos", "punto venta", "caja registradora" }),
                ("ventas", "Ventas", "/Ventas", "Ventas", "Index", new[] { "venta*" }),
                ("cotizaciones", "Cotizaciones", "/Cotizaciones", "Ventas", "Cotizaciones", new[] { "cotizacion*" }),
                ("pedidos", "Pedidos", "/Pedidos", "Ventas", "Pedidos", new[] { "pedido*" }),
                ("facturas", "Facturas", "/Facturas", "Ventas", "Facturas", new[] { "factura*" }),
                ("devoluciones", "Devoluciones", "/Devoluciones", "Ventas", "Devoluciones", new[] { "devolucion*" }),
                ("productos", "Productos", "/Productos", "Inventario", "Productos", new[] { "producto*" }),
                ("categorias", "Categorías", "/Categorias", "Inventario", "Categorias", new[] { "categoria*" }),
                ("bajas", "Bajas de inventario", "/Bajas", "Inventario", "Bajas", new[] { "baja*" }),
                ("proveedores", "Proveedores", "/Proveedores", "Compras", "Proveedores", new[] { "proveedor*" }),
                ("ordenes_compra", "Órdenes de compra", "/OrdenesCompra", "Compras", "Ordenes", new[] { "orden* compra*", "compra*" }),
                ("pagos_proveedor", "Pagos a proveedores", "/PagosProveedor", "Compras", "Pagos", new[] { "pago* proveedor*" }),
                ("finanzas", "Finanzas", "/Finanzas", "Finanzas", "Index", new[] { "finanza*" }),
                ("cuentas", "Cuentas financieras", "/Finanzas/Cuentas", "Finanzas", "Cuentas", new[] { "cuenta* financiera*", "cuenta* bancaria*", "banco*" }),
                ("movimientos_pag", "Movimientos financieros", "/Finanzas/Movimientos", "Finanzas", "Movimientos", new[] { "movimiento*" }),
                ("aperturas", "Apertura de caja", "/Finanzas/Aperturas", "Finanzas", "Aperturas", new[] { "apertura*", "abrir caja" }),
                ("cierres", "Cierres de caja", "/Finanzas/Cierres", "Finanzas", "Cierres", new[] { "cierre*" }),
                ("asientos", "Libro diario y asientos", "/Finanzas/Asientos", "Finanzas", "Asientos", new[] { "asiento*", "libro diario", "contabilidad" }),
                ("cierres_contables", "Cierres contables", "/Finanzas/CierresContables", "Finanzas", "CierresContables", new[] { "cierre* contable*", "periodo* contable*" }),
                ("conciliacion", "Conciliación bancaria", "/Finanzas/Conciliacion", "Finanzas", "Conciliacion", new[] { "conciliacion*" }),
                ("reportes_fin", "Reportes financieros", "/Finanzas/Reportes", "Finanzas", "Reportes", new[] { "reporte* financiero*" }),
                ("rrhh", "Recursos humanos", "/RRHH", "RRHH", "Index", new[] { "rrhh", "recursos humanos" }),
                ("empleados_pag", "Empleados", "/RRHH/Empleados", "RRHH", "Empleados", new[] { "empleado*" }),
                ("nomina_pag", "Nómina", "/RRHH/Nomina", "RRHH", "Nomina", new[] { "nomina*", "planilla*" }),
                ("vacaciones_pag", "Vacaciones", "/RRHH/Vacaciones", "RRHH", "Vacaciones", new[] { "vacacion*" }),
                ("vales_pag", "Vales", "/RRHH/Vales", "RRHH", "Vales", new[] { "vale*" }),
                ("permisos_pag", "Permisos de empleados", "/RRHH/Permisos", "RRHH", "Permisos", new[] { "permiso* empleado*" }),
                ("logistica", "Logística", "/Logistica", "Logistica", "Index", new[] { "logistica" }),
                ("envios_pag", "Envíos", "/Logistica/Envios", "Logistica", "Envios", new[] { "envio*" }),
                ("rutas_pag", "Rutas", "/Logistica/Rutas", "Logistica", "Rutas", new[] { "ruta*" }),
                ("reportes", "Reportes", "/Reportes", "Reportes", "Index", new[] { "reporte*" }),
                ("usuarios", "Usuarios", "/Usuarios", "Usuarios", "Index", new[] { "usuario*" }),
                ("seguridad", "Seguridad y auditoría", "/Seguridad", "Seguridad", "Index", new[] { "seguridad" }),
                ("configuracion", "Configuración", "/Configuracion", null, null, new[] { "configuracion", "ajuste*" }),
                ("apariencia", "Apariencia (tema claro/oscuro)", "/Configuracion/Apariencia", null, null, new[] { "apariencia", "tema*", "modo oscuro", "modo claro" }),
            };

            var verbos = new[] { "abr*", "ir", "ve", "llev*", "pantalla", "modulo", "pagina" };
            foreach (var d in destinos)
            {
                var frases = new List<string>();
                foreach (var k in d.claves)
                    foreach (var v in verbos) frases.Add($"{v} {k}");
                var url = d.url;
                yield return new AccionDef
                {
                    Id = "ir_" + d.id,
                    Modulo = "Ir a…",
                    Titulo = "Abrir " + d.titulo,
                    Descripcion = "Te llevo a la pantalla de " + d.titulo,
                    Ejemplo = "abre " + d.titulo.ToLowerInvariant(),
                    Tipo = TipoAccion.Navegar,
                    PermModulo = d.mod,
                    PermSub = d.sub,
                    Url = url,
                    Frases = frases.ToArray(),
                    Ejecutar = c => new RespuestaAsistente
                    {
                        Tipo = "texto",
                        Mensaje = $"Listo, aquí tienes {d.titulo}.",
                        Enlaces = new List<EnlaceAsistente> { new() { Texto = "Abrir " + d.titulo, Url = url } }
                    }
                };
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  GENERAL
        // ════════════════════════════════════════════════════════════════
        private static RespuestaAsistente Ayuda(ContextoAccion c)
        {
            var filas = new List<List<string>>();
            foreach (var a in Todas.Where(x => x.Tipo != TipoAccion.Navegar)
                         .Where(x => x.PermModulo == null || Acceso(c, x.PermModulo, x.PermSub ?? "Index"))
                         .OrderBy(x => x.Modulo).ThenBy(x => x.Titulo))
            {
                filas.Add(Fila(a.Modulo, a.Titulo + (a.RequierePassword ? " 🔒" : ""), "“" + a.Ejemplo + "”"));
            }
            var r = RespuestaAsistente.Tabla("Lo que puedo hacer por ti",
                "Escríbeme en español, como se lo pedirías a un compañero. 🔒 = te pido contraseña. " +
                "También puedo llevarte a cualquier pantalla: “abre clientes”, “llévame a nómina”. " +
                "Puedes agregar fechas: “ventas de ayer”, “gastos del mes pasado”, “ventas del 01/10/2026 al 15/10/2026”.",
                new[] { "Módulo", "Acción", "Ejemplo" }, filas);
            return r;
        }

        private static RespuestaAsistente ResumenDia(ContextoAccion c)
        {
            var d = c.Desde.Date; var h = c.Hasta.Date.AddDays(1);
            var lineas = new List<string>();

            if (Acceso(c, "Ventas", "Index"))
            {
                var ventas = c.Db.Ventas.AsNoTracking().Where(v => v.Fecha >= d && v.Fecha < h && v.Estado != "Anulada").Select(v => v.Total).ToList();
                lineas.Add($"🛒 Ventas {c.RangoTexto}: {ventas.Count} por {L(ventas.Sum())}");
            }
            if (Acceso(c, "Finanzas", "Movimientos"))
            {
                var movs = c.Db.MovimientosFinancieros.AsNoTracking()
                    .Where(m => m.Estado == "Activo" && m.Fecha >= d && m.Fecha < h && (m.Tipo == "Ingreso" || m.Tipo == "Egreso"))
                    .Select(m => new { m.Tipo, m.Monto }).ToList();
                var ing = movs.Where(m => m.Tipo == "Ingreso").Sum(m => m.Monto);
                var egr = movs.Where(m => m.Tipo == "Egreso").Sum(m => m.Monto);
                lineas.Add($"💰 Ingresos {L(ing)} · Egresos {L(egr)} · Neto {L(ing - egr)}");
            }
            if (Acceso(c, "Ventas", "Facturas"))
            {
                var n = c.Db.Facturas.AsNoTracking().Count(f => f.Saldo > 0 && f.Estado != "Anulada" && f.Estado != "Pagada");
                lineas.Add($"🧾 Facturas con saldo pendiente: {n}");
            }
            if (Acceso(c, "Ventas", "Cotizaciones"))
            {
                var n = c.Db.Cotizaciones.AsNoTracking().Count(q => q.Estado == "Borrador" || q.Estado == "Enviada");
                lineas.Add($"📄 Cotizaciones abiertas: {n}");
            }
            if (Acceso(c, "Inventario", "Productos"))
            {
                var n = c.Db.Productos.AsNoTracking().Count(p => p.Activo && p.Stock <= p.StockMinimo);
                lineas.Add($"📦 Productos en o bajo el mínimo: {n}");
            }
            if (lineas.Count == 0) return RespuestaAsistente.Texto("Tu rol no tiene acceso a los datos del resumen.");
            return RespuestaAsistente.Texto(string.Join("\n", lineas), "Resumen del negocio");
        }

        // ════════════════════════════════════════════════════════════════
        //  VENTAS
        // ════════════════════════════════════════════════════════════════
        private static RespuestaAsistente VentasResumen(ContextoAccion c)
        {
            var d = c.Desde.Date; var h = c.Hasta.Date.AddDays(1);
            var ventas = c.Db.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= d && v.Fecha < h && v.Estado != "Anulada")
                .OrderBy(v => v.Fecha).ToList();

            var ids = ventas.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value).Distinct().ToList();
            var clientes = c.Db.Clientes.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nombre);

            var unDia = d == c.Hasta.Date;
            var filas = ventas.Take(Limite).Select(v => Fila(
                v.Numero,
                unDia ? v.Fecha.ToString("HH:mm", Inv) : v.Fecha.ToString("dd/MM HH:mm", Inv),
                v.ClienteId.HasValue && clientes.TryGetValue(v.ClienteId.Value, out var n) ? n : "Consumidor final",
                v.EsCredito ? "Crédito" : "Contado",
                L(v.Total))).ToList();

            var total = ventas.Sum(v => v.Total);
            var credito = ventas.Where(v => v.EsCredito).Sum(v => v.Total);
            var prom = ventas.Count > 0 ? total / ventas.Count : 0;
            var msg = ventas.Count == 0
                ? $"No hay ventas {c.RangoTexto}."
                : $"{ventas.Count} venta(s) {c.RangoTexto} por {L(total)} (promedio {L(prom)}; a crédito {L(credito)}){Nota(ventas.Count)}.";
            return RespuestaAsistente.Tabla($"Ventas {c.RangoTexto}", msg,
                new[] { "Venta", "Fecha", "Cliente", "Tipo", "Total" }, filas,
                ventas.Count > 0 ? Fila("TOTAL", "", "", ventas.Count + " ventas", L(total)) : null, new[] { 4 });
        }

        private static RespuestaAsistente VentasTop(ContextoAccion c)
        {
            var d = c.Desde.Date; var h = c.Hasta.Date.AddDays(1);
            var lineas = (from dv in c.Db.DetalleVentas.AsNoTracking()
                          join v in c.Db.Ventas.AsNoTracking() on dv.VentaId equals v.Id
                          where v.Fecha >= d && v.Fecha < h && v.Estado != "Anulada"
                          select new { dv.ProductoId, dv.Cantidad, dv.Total }).ToList();

            var top = lineas.GroupBy(x => x.ProductoId)
                .Select(g => new { Id = g.Key, Cant = g.Sum(x => x.Cantidad), Monto = g.Sum(x => x.Total) })
                .OrderByDescending(x => x.Cant).Take(15).ToList();
            var ids = top.Select(x => x.Id).ToList();
            var nombres = c.Db.Productos.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Nombre);

            var filas = top.Select((x, i) => Fila((i + 1).ToString(), nombres.GetValueOrDefault(x.Id, "Producto " + x.Id),
                x.Cant.ToString("N2", Inv), L(x.Monto))).ToList();
            return RespuestaAsistente.Tabla($"Productos más vendidos {c.RangoTexto}",
                filas.Count == 0 ? $"No hubo ventas {c.RangoTexto}." : $"Top {filas.Count} productos por cantidad {c.RangoTexto}.",
                new[] { "#", "Producto", "Cantidad", "Monto" }, filas, null, new[] { 2, 3 });
        }

        private static RespuestaAsistente Cxc(ContextoAccion c)
        {
            var ventas = c.Db.Ventas.AsNoTracking().Where(v => v.EsCredito && v.SaldoPendiente > 0 && v.Estado != "Anulada").ToList();
            var ids = ventas.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value).Distinct().ToList();
            var clientes = c.Db.Clientes.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nombre);

            var filas = ventas.GroupBy(v => v.ClienteId ?? 0).Select(g =>
            {
                var venc = g.Where(v => (c.Hoy - (v.FechaVencimientoCredito ?? v.Fecha).Date).Days > 0).Sum(v => v.SaldoPendiente);
                return new { Nombre = clientes.GetValueOrDefault(g.Key, "Sin cliente"), Total = g.Sum(v => v.SaldoPendiente), Vencido = venc, Docs = g.Count() };
            }).OrderByDescending(x => x.Total).ToList();

            var tabla = filas.Select(x => Fila(x.Nombre, x.Docs.ToString(), L(x.Vencido), L(x.Total))).ToList();
            var total = filas.Sum(x => x.Total); var venci = filas.Sum(x => x.Vencido);
            return RespuestaAsistente.Tabla("Cuentas por cobrar",
                filas.Count == 0 ? "No hay saldos pendientes de clientes. 🎉" : $"Nos deben {L(total)}, de los cuales {L(venci)} están vencidos.",
                new[] { "Cliente", "Ventas", "Vencido", "Saldo total" }, tabla,
                filas.Count > 0 ? Fila("TOTAL", "", L(venci), L(total)) : null, new[] { 1, 2, 3 });
        }

        private static IQueryable<Cliente> FiltrarClientes(ContextoAccion c, string texto)
        {
            var q = c.Db.Clientes.AsNoTracking().AsQueryable();
            foreach (var tk in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var t = tk;
                q = q.Where(x => x.Nombre.Contains(t) || (x.RazonSocial != null && x.RazonSocial.Contains(t)) || (x.RTN != null && x.RTN.Contains(t)));
            }
            return q;
        }

        private static RespuestaAsistente SaldoCliente(ContextoAccion c)
        {
            if (c.Texto == null)
                return RespuestaAsistente.Faltante("¿De qué cliente? Escribe su nombre o RTN.", "saldo_cliente", c.P, "texto");

            var encontrados = FiltrarClientes(c, c.Texto).OrderBy(x => x.Nombre).Take(7).ToList();
            if (encontrados.Count == 0) return RespuestaAsistente.Texto($"No encontré clientes que coincidan con “{c.Texto}”.");
            if (encontrados.Count > 1)
                return RespuestaAsistente.Elegir($"Encontré varios clientes con “{c.Texto}”. ¿Cuál?",
                    encontrados.Select(x => new OpcionAsistente { Texto = x.Nombre, Envio = "saldo de " + x.Nombre }));

            var cli = encontrados[0];
            var ventas = c.Db.Ventas.AsNoTracking()
                .Where(v => v.ClienteId == cli.Id && v.EsCredito && v.SaldoPendiente > 0 && v.Estado != "Anulada")
                .OrderBy(v => v.FechaVencimientoCredito ?? v.Fecha).ToList();

            var filas = ventas.Select(v =>
            {
                var venc = (v.FechaVencimientoCredito ?? v.Fecha).Date;
                var dias = Math.Max(0, (c.Hoy - venc).Days);
                return Fila(v.Numero, F(v.Fecha), F(venc), L(v.Total), L(v.SaldoPendiente), dias > 0 ? dias + " días" : "Al día");
            }).ToList();

            var saldo = ventas.Sum(v => v.SaldoPendiente);
            var vencido = ventas.Where(v => (c.Hoy - (v.FechaVencimientoCredito ?? v.Fecha).Date).Days > 0).Sum(v => v.SaldoPendiente);
            var empresa = c.Db.ConfiguracionEmpresa.AsNoTracking().Select(e => e.Nombre).FirstOrDefault() ?? "Mi Empresa";

            var r = RespuestaAsistente.Tabla($"Saldo de {cli.Nombre}",
                ventas.Count == 0 ? $"{cli.Nombre} no tiene saldo pendiente. ✅"
                    : $"{cli.Nombre} debe {L(saldo)} (vencido {L(vencido)})." + (cli.LimiteCredito > 0 ? $" Límite de crédito {L(cli.LimiteCredito)}." : ""),
                new[] { "Venta", "Fecha", "Vence", "Total", "Saldo", "Atraso" }, filas,
                ventas.Count > 0 ? Fila("TOTAL", "", "", "", L(saldo), "") : null, new[] { 3, 4 });
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Abrir estado de cuenta", Url = "/Clientes/EstadoCuenta/" + cli.Id } };
            if (saldo > 0)
            {
                var wa = WhatsAppHelper.Enlace(cli.Telefono, WhatsAppHelper.MensajeCobranza(empresa, cli.Nombre, saldo, vencido));
                if (wa != null) r.Enlaces.Add(new EnlaceAsistente { Texto = "Cobrar por WhatsApp", Url = wa, Externo = true });
            }
            return r;
        }

        private static RespuestaAsistente CotizacionesPendientes(ContextoAccion c)
        {
            var cot = c.Db.Cotizaciones.AsNoTracking().Where(q => q.Estado == "Borrador" || q.Estado == "Enviada").OrderBy(q => q.FechaVencimiento).ToList();
            var ids = cot.Select(q => q.ClienteId).Distinct().ToList();
            var cli = c.Db.Clientes.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nombre);
            var filas = cot.Take(Limite).Select(q => Fila(q.Numero, F(q.Fecha), cli.GetValueOrDefault(q.ClienteId, "—"), q.Estado,
                q.FechaVencimiento.HasValue ? F(q.FechaVencimiento.Value) : "—", L(q.Total))).ToList();
            return RespuestaAsistente.Tabla("Cotizaciones abiertas",
                cot.Count == 0 ? "No hay cotizaciones abiertas." : $"{cot.Count} cotización(es) por {L(cot.Sum(q => q.Total))}{Nota(cot.Count)}.",
                new[] { "Cotización", "Fecha", "Cliente", "Estado", "Vence", "Total" }, filas, null, new[] { 5 });
        }

        private static RespuestaAsistente PedidosPendientes(ContextoAccion c)
        {
            var estados = new List<string> { "Pendiente", "En preparacion", "Enviado" };
            var ped = c.Db.Pedidos.AsNoTracking().Where(p => estados.Contains(p.Estado)).OrderBy(p => p.FechaEntrega ?? p.Fecha).ToList();
            var ids = ped.Select(p => p.ClienteId).Distinct().ToList();
            var cli = c.Db.Clientes.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nombre);
            var filas = ped.Take(Limite).Select(p => Fila(p.Numero, F(p.Fecha), cli.GetValueOrDefault(p.ClienteId, "—"), p.Estado,
                p.FechaEntrega.HasValue ? F(p.FechaEntrega.Value) : "—", L(p.Total))).ToList();
            return RespuestaAsistente.Tabla("Pedidos pendientes",
                ped.Count == 0 ? "No hay pedidos pendientes." : $"{ped.Count} pedido(s) por {L(ped.Sum(p => p.Total))}{Nota(ped.Count)}.",
                new[] { "Pedido", "Fecha", "Cliente", "Estado", "Entrega", "Total" }, filas, null, new[] { 5 });
        }

        private static RespuestaAsistente FacturasPendientes(ContextoAccion c)
        {
            var fac = c.Db.Facturas.AsNoTracking().Where(f => f.Saldo > 0 && f.Estado != "Anulada" && f.Estado != "Pagada")
                .OrderBy(f => f.FechaVencimiento ?? f.Fecha).ToList();
            var ids = fac.Select(f => f.ClienteId).Distinct().ToList();
            var cli = c.Db.Clientes.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nombre);
            var filas = fac.Take(Limite).Select(f => Fila(f.Numero, cli.GetValueOrDefault(f.ClienteId, "—"), F(f.Fecha),
                f.FechaVencimiento.HasValue ? F(f.FechaVencimiento.Value) : "—", L(f.Total), L(f.Saldo))).ToList();
            var saldo = fac.Sum(f => f.Saldo);
            return RespuestaAsistente.Tabla("Facturas con saldo",
                fac.Count == 0 ? "No hay facturas con saldo pendiente. ✅" : $"{fac.Count} factura(s) con saldo total de {L(saldo)}{Nota(fac.Count)}.",
                new[] { "Factura", "Cliente", "Fecha", "Vence", "Total", "Saldo" }, filas,
                fac.Count > 0 ? Fila("TOTAL", "", "", "", "", L(saldo)) : null, new[] { 4, 5 });
        }

        // ── Anular factura (Operación con contraseña) ──
        private static RespuestaAsistente PrepararAnularFactura(ContextoAccion c)
        {
            Factura? f = null;
            if (c.P.TryGetValue("facturaId", out var fid) && int.TryParse(fid, out var id))
                f = c.Db.Facturas.AsNoTracking().FirstOrDefault(x => x.Id == id);

            if (f == null)
            {
                var raw = c.P.GetValueOrDefault("raw", "") ?? "";
                var tokens = AsistenteMotor.Tokens(AsistenteMotor.Normalizar(raw))
                    .Where(t => t.Any(char.IsDigit) && t.Length >= 3).ToList();
                if (tokens.Count == 0)
                    return RespuestaAsistente.Faltante("¿Cuál es el número de la factura que quieres anular?", "anular_factura", c.P, "raw");

                var tk = tokens[0];
                var halladas = c.Db.Facturas.AsNoTracking().Where(x => x.Numero.Contains(tk)).OrderByDescending(x => x.Fecha).Take(6).ToList();
                if (halladas.Count == 0) return RespuestaAsistente.Texto($"No encontré ninguna factura con “{tk}”.");
                if (halladas.Count > 1)
                    return RespuestaAsistente.Elegir("Encontré varias facturas. ¿Cuál quieres anular?",
                        halladas.Select(x => new OpcionAsistente { Texto = $"{x.Numero} · {L(x.Total)}", Envio = "anular factura " + x.Numero }));
                f = halladas[0];
            }

            if (f.Estado == "Anulada") return RespuestaAsistente.Error($"La factura {f.Numero} ya está anulada.");
            if (f.VentaId.HasValue) return RespuestaAsistente.Error($"La factura {f.Numero} viene de una venta del POS: se anula desde la venta.");
            c.P["facturaId"] = f.Id.ToString();

            var motivo = (c.P.GetValueOrDefault("motivo", "") ?? "").Trim();
            if (motivo.Length < 5)
                return RespuestaAsistente.Faltante($"Factura {f.Numero} por {L(f.Total)}. ¿Cuál es el motivo de la anulación? (mínimo 5 caracteres)", "anular_factura", c.P, "motivo");

            var cliente = c.Db.Clientes.AsNoTracking().Where(x => x.Id == f.ClienteId).Select(x => x.Nombre).FirstOrDefault() ?? "—";
            return new RespuestaAsistente
            {
                Tipo = "confirmar",
                Titulo = "Confirmar anulación de factura",
                Mensaje = "Esta acción no se puede deshacer. Confirma con tu contraseña.",
                RequierePassword = true,
                Resumen = new List<string>
                {
                    $"Factura: {f.Numero}", $"Cliente: {cliente}", $"Total: {L(f.Total)} · Estado: {f.Estado}", $"Motivo: {motivo}",
                    "Se devolverá el inventario y se revertirán los pagos y el costo en Finanzas."
                }
            };
        }

        private static RespuestaAsistente EjecutarAnularFactura(ContextoAccion c)
        {
            if (!int.TryParse(c.P.GetValueOrDefault("facturaId"), out var id)) return RespuestaAsistente.Error("Falta la factura.");
            var motivo = (c.P.GetValueOrDefault("motivo", "") ?? "").Trim();
            var num = c.Db.Facturas.AsNoTracking().Where(x => x.Id == id).Select(x => x.Numero).FirstOrDefault() ?? id.ToString();
            var (ok, error) = AnulacionFacturaHelper.Anular(c.Db, id, $"{motivo} (vía asistente)", c.Usuario.Id);
            if (!ok) return RespuestaAsistente.Error(error ?? "No se pudo anular la factura.");
            ActividadHelper.Registrar(c.Db, c.Usuario.Id, "Asistente: anular factura",
                $"Anuló la factura {num}. Motivo: {motivo}. Autorizó: {c.Autorizo}", c.Ip);
            var r = RespuestaAsistente.Texto($"✅ Factura {num} anulada. Se devolvió el inventario y se revirtieron los pagos.");
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Ver factura", Url = "/Facturas/Details/" + id } };
            return r;
        }

        // ════════════════════════════════════════════════════════════════
        //  INVENTARIO
        // ════════════════════════════════════════════════════════════════
        private static RespuestaAsistente StockBajo(ContextoAccion c)
        {
            var prods = c.Db.Productos.AsNoTracking().Where(p => p.Activo && p.Stock <= p.StockMinimo)
                .OrderBy(p => p.Stock - p.StockMinimo).ToList();
            var filas = prods.Take(Limite).Select(p => Fila(p.Nombre, p.SKU ?? "—", p.Stock.ToString("N2", Inv), p.StockMinimo.ToString("N2", Inv),
                Math.Max(0, p.StockMinimo * 2 - p.Stock).ToString("N2", Inv))).ToList();
            return RespuestaAsistente.Tabla("Productos con stock bajo",
                prods.Count == 0 ? "Todo el inventario está sobre el mínimo. ✅" : $"{prods.Count} producto(s) en o bajo el mínimo{Nota(prods.Count)}. “Sugerido” repone hasta el doble del mínimo.",
                new[] { "Producto", "SKU", "Stock", "Mínimo", "Sugerido" }, filas, null, new[] { 2, 3, 4 });
        }

        private static RespuestaAsistente BuscarProducto(ContextoAccion c)
        {
            if (c.Texto == null) return RespuestaAsistente.Faltante("¿Qué producto buscas? Escribe nombre, SKU o código de barras.", "buscar_producto", c.P, "texto");
            var q = c.Db.Productos.AsNoTracking().Where(p => p.Activo);
            foreach (var tk in c.Texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var t = tk;
                q = q.Where(p => p.Nombre.Contains(t) || (p.SKU != null && p.SKU.Contains(t)) || (p.CodigoBarras != null && p.CodigoBarras.Contains(t)));
            }
            var lista = q.OrderBy(p => p.Nombre).Take(15).ToList();
            var filas = lista.Select(p => Fila(p.Nombre, p.SKU ?? "—", p.Stock.ToString("N2", Inv), L(p.PrecioVenta))).ToList();
            return RespuestaAsistente.Tabla($"Productos: {c.Texto}",
                lista.Count == 0 ? $"No encontré productos con “{c.Texto}”." : $"{lista.Count} resultado(s).",
                new[] { "Producto", "SKU", "Stock", "Precio" }, filas, null, new[] { 2, 3 });
        }

        private static RespuestaAsistente ValorInventario(ContextoAccion c)
        {
            var p = c.Db.Productos.AsNoTracking().Where(x => x.Activo && x.Stock > 0).Select(x => new { x.Stock, x.PrecioCompra, x.PrecioVenta }).ToList();
            var costo = p.Sum(x => x.Stock * x.PrecioCompra);
            var venta = p.Sum(x => x.Stock * x.PrecioVenta);
            return RespuestaAsistente.Texto(
                $"📦 {p.Count} productos con existencia.\nValor a costo: {L(costo)}\nValor a precio de venta: {L(venta)}\nUtilidad potencial: {L(venta - costo)}",
                "Valor del inventario");
        }

        // ════════════════════════════════════════════════════════════════
        //  COMPRAS
        // ════════════════════════════════════════════════════════════════
        private static RespuestaAsistente Cxp(ContextoAccion c)
        {
            var ordenes = c.Db.OrdenesCompra.AsNoTracking().Where(o => o.Saldo > 0 && o.Estado != "Cancelada" && o.Estado != "Borrador").ToList();
            var ids = ordenes.Select(o => o.ProveedorId).Distinct().ToList();
            var prov = c.Db.Proveedores.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Nombre);
            var filas = ordenes.GroupBy(o => o.ProveedorId)
                .Select(g => new { N = prov.GetValueOrDefault(g.Key, "Proveedor " + g.Key), Docs = g.Count(), Saldo = g.Sum(o => o.Saldo) })
                .OrderByDescending(x => x.Saldo).ToList();
            var total = filas.Sum(x => x.Saldo);
            return RespuestaAsistente.Tabla("Cuentas por pagar",
                filas.Count == 0 ? "No debemos nada a proveedores. ✅" : $"Debemos {L(total)} a {filas.Count} proveedor(es).",
                new[] { "Proveedor", "Órdenes", "Saldo" }, filas.Select(x => Fila(x.N, x.Docs.ToString(), L(x.Saldo))).ToList(),
                filas.Count > 0 ? Fila("TOTAL", "", L(total)) : null, new[] { 1, 2 });
        }

        // ════════════════════════════════════════════════════════════════
        //  FINANZAS
        // ════════════════════════════════════════════════════════════════
        private static RespuestaAsistente SaldosCuentas(ContextoAccion c)
        {
            var cuentas = c.Db.CuentasFinancieras.AsNoTracking().Where(x => x.Activa).OrderBy(x => x.Tipo).ThenBy(x => x.Nombre).ToList();
            var filas = cuentas.Select(x => Fila(x.Nombre, x.Tipo, x.Banco ?? "—", L(x.SaldoActual))).ToList();
            var liquido = cuentas.Where(x => x.Tipo == "Caja" || x.Tipo == "Banco").Sum(x => x.SaldoActual);
            return RespuestaAsistente.Tabla("Saldos de cuentas",
                $"Disponible en cajas y bancos: {L(liquido)}.", new[] { "Cuenta", "Tipo", "Banco", "Saldo" }, filas,
                null, new[] { 3 });
        }

        private static RespuestaAsistente Movimientos(ContextoAccion c)
        {
            var d = c.Desde.Date; var h = c.Hasta.Date.AddDays(1);
            var tipos = new List<string> { "Ingreso", "Egreso", "Transferencia" };
            var movs = c.Db.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Estado == "Activo" && m.Fecha >= d && m.Fecha < h && tipos.Contains(m.Tipo))
                .OrderBy(m => m.Fecha).ToList();
            var ing = movs.Where(m => m.Tipo == "Ingreso").Sum(m => m.Monto);
            var egr = movs.Where(m => m.Tipo == "Egreso").Sum(m => m.Monto);
            var filas = movs.Take(Limite).Select(m => Fila(m.Numero, m.Fecha.ToString("dd/MM HH:mm", Inv), m.Tipo, m.Concepto, L(m.Monto))).ToList();
            return RespuestaAsistente.Tabla($"Movimientos {c.RangoTexto}",
                movs.Count == 0 ? $"No hay movimientos {c.RangoTexto}." : $"{movs.Count} movimiento(s) {c.RangoTexto}: ingresos {L(ing)}, egresos {L(egr)}{Nota(movs.Count)}.",
                new[] { "Número", "Fecha", "Tipo", "Concepto", "Monto" }, filas,
                movs.Count > 0 ? Fila("NETO", "", "", "Ingresos − egresos", L(ing - egr)) : null, new[] { 4 });
        }

        private static RespuestaAsistente ResumenFinanciero(ContextoAccion c)
        {
            var d = c.Desde.Date; var h = c.Hasta.Date.AddDays(1);
            var movs = c.Db.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Estado == "Activo" && m.Fecha >= d && m.Fecha < h && (m.Tipo == "Ingreso" || m.Tipo == "Egreso"))
                .Select(m => new { m.Tipo, m.Monto }).ToList();
            var ing = movs.Where(m => m.Tipo == "Ingreso").Sum(m => m.Monto);
            var egr = movs.Where(m => m.Tipo == "Egreso").Sum(m => m.Monto);
            var margen = ing > 0 ? (ing - egr) / ing * 100 : 0;
            return RespuestaAsistente.Tabla($"Resultado {c.RangoTexto}", $"Entre {F(d)} y {F(c.Hasta)}.",
                new[] { "Concepto", "Monto" },
                new List<List<string>> { Fila("Ingresos", L(ing)), Fila("Egresos", L(egr)), Fila("Resultado neto", L(ing - egr)), Fila("Margen", margen.ToString("N1", Inv) + " %") },
                null, new[] { 1 });
        }

        private static RespuestaAsistente EstadoCaja(ContextoAccion c)
        {
            var aperturas = c.Db.AperturasCaja.AsNoTracking().Where(a => a.Activa).ToList();
            if (aperturas.Count == 0) return RespuestaAsistente.Texto("No hay ninguna caja abierta en este momento.");
            var ids = aperturas.Select(a => a.CuentaId).ToList();
            var cuentas = c.Db.CuentasFinancieras.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nombre);
            var filas = new List<List<string>>();
            foreach (var a in aperturas)
            {
                var calc = CierreHelper.Calcular(c.Db, a.CuentaId, c.Hoy);
                filas.Add(Fila(cuentas.GetValueOrDefault(a.CuentaId, "Caja " + a.CuentaId), a.Numero, a.FechaApertura.ToString("dd/MM HH:mm", Inv),
                    L(a.SaldoInicial), calc.Error == null ? L(calc.EfectivoEsperado) : "—"));
            }
            return RespuestaAsistente.Tabla("Estado de la caja", $"{aperturas.Count} caja(s) abierta(s).",
                new[] { "Caja", "Apertura", "Desde", "Fondo inicial", "Efectivo esperado" }, filas, null, new[] { 3, 4 });
        }

        private static RespuestaAsistente UltimosCierres(ContextoAccion c)
        {
            var cierres = c.Db.CierresCaja.AsNoTracking().OrderByDescending(x => x.FechaCierre).Take(10).ToList();
            var ids = cierres.Select(x => x.CuentaId).Distinct().ToList();
            var cuentas = c.Db.CuentasFinancieras.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nombre);
            var filas = cierres.Select(x => Fila(x.Numero, F(x.Fecha), cuentas.GetValueOrDefault(x.CuentaId, "—"),
                L(x.EfectivoEsperado), L(x.EfectivoContado), L(x.Diferencia), x.Resultado)).ToList();
            return RespuestaAsistente.Tabla("Últimos cierres de caja",
                filas.Count == 0 ? "Todavía no hay cierres de caja." : $"{filas.Count} cierre(s) más recientes.",
                new[] { "Cierre", "Fecha", "Caja", "Esperado", "Contado", "Diferencia", "Resultado" }, filas, null, new[] { 3, 4, 5 });
        }

        // ── Cierre de caja (Operación con contraseña) ──
        private static RespuestaAsistente PrepararCierreCaja(ContextoAccion c)
        {
            var cajas = c.Db.CuentasFinancieras.AsNoTracking().Where(x => x.Tipo == "Caja" && x.Activa).OrderBy(x => x.Nombre).ToList();
            var abiertas = cajas.Where(x => c.Db.AperturasCaja.Any(a => a.CuentaId == x.Id && a.Activa)).ToList();
            if (abiertas.Count == 0)
                return RespuestaAsistente.Error("No hay ninguna caja abierta para cerrar. Primero hay que abrir la caja.");

            CuentaFinanciera? cuenta = null;
            if (int.TryParse(c.P.GetValueOrDefault("cuentaId"), out var ci)) cuenta = abiertas.FirstOrDefault(x => x.Id == ci);
            if (cuenta == null && c.Texto != null)
            {
                var tks = c.Texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                cuenta = abiertas.FirstOrDefault(x => tks.All(tk => AsistenteMotor.Normalizar(x.Nombre).Contains(tk)));
            }
            if (cuenta == null && abiertas.Count == 1) cuenta = abiertas[0];
            if (cuenta == null)
                return RespuestaAsistente.Elegir("¿Qué caja quieres cerrar?",
                    abiertas.Select(x => new OpcionAsistente { Texto = x.Nombre, Envio = "cerrar caja " + x.Nombre }));

            c.P["cuentaId"] = cuenta.Id.ToString();
            var calc = CierreHelper.Calcular(c.Db, cuenta.Id, c.Hoy);
            if (calc.Error != null) return RespuestaAsistente.Error(calc.Error);
            var esperado = calc.EfectivoEsperado;
            c.P["esperado"] = esperado.ToString(Inv);

            var contado = c.Numero("contado");
            if (contado == null)
                return RespuestaAsistente.Faltante($"{cuenta.Nombre}: el sistema espera {L(esperado)} en efectivo. ¿Cuánto efectivo contaste?", "cierre_caja", c.P, "contado");
            if (contado < 0) { c.P.Remove("contado"); return RespuestaAsistente.Faltante("El monto no puede ser negativo. ¿Cuánto efectivo contaste?", "cierre_caja", c.P, "contado"); }

            var fondo = c.Numero("fondo");
            if (fondo == null)
                return RespuestaAsistente.Faltante($"Contaste {L(contado.Value)}. ¿Cuánto dejas como fondo para mañana? (escribe 0 si no dejas)", "cierre_caja", c.P, "fondo");
            if (fondo < 0 || fondo > contado)
            {
                c.P.Remove("fondo");
                return RespuestaAsistente.Faltante($"El fondo debe estar entre 0 y {L(contado.Value)}. ¿Cuánto dejas de fondo?", "cierre_caja", c.P, "fondo");
            }

            var entrega = contado.Value - fondo.Value;
            var dif = contado.Value - esperado;
            var resultado = CierreHelper.DeterminarResultado(dif, 20m);
            return new RespuestaAsistente
            {
                Tipo = "confirmar",
                Titulo = "Confirmar cierre de caja",
                Mensaje = "Revisa los datos. Para cerrar la caja confirma con tu contraseña.",
                RequierePassword = true,
                Resumen = new List<string>
                {
                    $"Caja: {cuenta.Nombre} · {F(c.Hoy)}",
                    $"Efectivo esperado: {L(esperado)}",
                    $"Efectivo contado: {L(contado.Value)}",
                    $"Diferencia: {L(dif)} → {resultado}",
                    $"Fondo para mañana: {L(fondo.Value)}",
                    $"Entrega a administración: {L(entrega)}"
                }
            };
        }

        private static RespuestaAsistente EjecutarCierreCaja(ContextoAccion c)
        {
            if (!int.TryParse(c.P.GetValueOrDefault("cuentaId"), out var cuentaId)) return RespuestaAsistente.Error("Falta la caja.");
            var contado = c.Numero("contado"); var fondo = c.Numero("fondo") ?? 0;
            if (contado == null || contado < 0 || fondo < 0 || fondo > contado) return RespuestaAsistente.Error("Los montos del cierre no son válidos.");

            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(c.Db, c.Hoy);
            if (!periodoOk) return RespuestaAsistente.Error(periodoError ?? "El período contable está cerrado.");

            var (cierre, error) = CierreHelper.Registrar(c.Db, cuentaId, c.Hoy, contado.Value,
                $"Registrado desde el asistente. Autorizó: {c.Autorizo}", 20.00m, c.Usuario.Id);
            if (error != null) return RespuestaAsistente.Error(error);

            var entrega = contado.Value - fondo;
            var dist = new List<DistribucionInput>();
            if (fondo > 0) dist.Add(new DistribucionInput { Tipo = "FondoCaja", Monto = fondo, DestinoDescripcion = "Fondo para el próximo día" });
            if (entrega > 0)
            {
                var recibeId = c.AutorizoId ?? c.Usuario.Id;
                var recibeNombre = c.Db.Usuarios.AsNoTracking().Where(u => u.Id == recibeId).Select(u => u.Username).FirstOrDefault();
                dist.Add(new DistribucionInput { Tipo = "EntregaAdmin", Monto = entrega, UsuarioRecibeId = recibeId, NombreRecibe = recibeNombre });
            }

            var (ok, distError) = DistribucionHelper.RegistrarDistribuciones(c.Db, cierre.Id, cuentaId, contado.Value, dist, c.Usuario.Id);

            ActividadHelper.Registrar(c.Db, c.Usuario.Id, "Cierre de caja",
                $"Cerró caja {cierre.Numero} desde el asistente. Esperado: {L(cierre.EfectivoEsperado)}, Contado: {L(cierre.EfectivoContado)}, " +
                $"Diferencia: {L(cierre.Diferencia)}. Autorizó: {c.Autorizo}", c.Ip);

            var msg = $"✅ Cierre {cierre.Numero} registrado. Resultado: {cierre.Resultado} (diferencia {L(cierre.Diferencia)}).";
            if (!ok) msg += $"\n⚠️ El cierre quedó registrado pero hubo un problema con las distribuciones: {distError}";
            var r = RespuestaAsistente.Texto(msg);
            r.Enlaces = new List<EnlaceAsistente>
            {
                new() { Texto = "Ver cierre", Url = "/Finanzas/Cierres/Details/" + cierre.Id },
                new() { Texto = "Informe por método de pago", Url = "/Finanzas/Cierres/Informe/" + cierre.Id }
            };
            return r;
        }

        // ════════════════════════════════════════════════════════════════
        //  RRHH
        // ════════════════════════════════════════════════════════════════
        private static RespuestaAsistente Empleados(ContextoAccion c)
        {
            var q = c.Db.Empleados.AsNoTracking().Where(e => e.Estado == "Activo");
            var todos = q.ToList();
            var lista = todos.AsEnumerable();
            if (c.Texto != null)
            {
                foreach (var tk in c.Texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = tk;
                    lista = lista.Where(e => InformeNorm(Nombre(e)).Contains(t) || InformeNorm(e.Codigo).Contains(t)
                                             || InformeNorm(e.Departamento).Contains(t) || InformeNorm(e.PuestoNombre).Contains(t));
                }
            }
            var sel = lista.OrderBy(e => e.Nombres).ThenBy(e => e.Apellidos).ToList();
            var filas = sel.Take(Limite).Select(e => Fila(e.Codigo, Nombre(e), Pal(e.PuestoNombre), Pal(e.Departamento), Pal(e.Telefono))).ToList();

            var porDepto = todos.GroupBy(e => string.IsNullOrWhiteSpace(e.Departamento) ? "Sin departamento" : e.Departamento!)
                .OrderByDescending(g => g.Count()).Select(g => $"{g.Key}: {g.Count()}").ToList();
            var msg = c.Texto == null
                ? $"Hay {todos.Count} empleado(s) activo(s). Por departamento → {string.Join(" • ", porDepto)}."
                : (sel.Count == 0 ? $"No encontré empleados con “{c.Texto}”." : $"{sel.Count} empleado(s) con “{c.Texto}”.");
            var r = RespuestaAsistente.Tabla(c.Texto == null ? "Empleados activos" : $"Empleados: {c.Texto}", msg,
                new[] { "Código", "Nombre", "Puesto", "Departamento", "Teléfono" }, filas);
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Abrir Empleados", Url = "/RRHH/Empleados" } };
            return r;
        }

        private static string InformeNorm(string? s) => Kirkenta.Helpers.Finanzas.InformeMetodosPagoHelper.Norm(s);

        private static RespuestaAsistente Cumpleanos(ContextoAccion c)
        {
            var emp = c.Db.Empleados.AsNoTracking().Where(e => e.Estado == "Activo" && e.FechaNacimiento != null && e.FechaNacimiento.Value.Month == c.Hoy.Month)
                .ToList().OrderBy(e => e.FechaNacimiento!.Value.Day).ToList();
            var filas = emp.Select(e => Fila(Nombre(e), e.FechaNacimiento!.Value.Day + " de " + MesNombre(c.Hoy.Month), (c.Hoy.Year - e.FechaNacimiento!.Value.Year).ToString() + " años")).ToList();
            return RespuestaAsistente.Tabla("Cumpleaños del mes", emp.Count == 0 ? "Nadie cumple años este mes." : $"{emp.Count} cumpleañero(s) este mes. 🎂",
                new[] { "Empleado", "Día", "Cumple" }, filas);
        }

        private static Dictionary<int, string> NombresEmpleados(ContextoAccion c, List<int> ids)
            => c.Db.Empleados.AsNoTracking().Where(e => ids.Contains(e.Id)).ToList().ToDictionary(e => e.Id, e => Nombre(e));

        private static RespuestaAsistente ValesPendientes(ContextoAccion c)
        {
            var estados = new List<string> { "Solicitado", "AprobadoGerente", "AprobadoRRHH" };
            var vales = c.Db.ValesEmpleado.AsNoTracking().Where(v => estados.Contains(v.Estado)).OrderBy(v => v.FechaSolicitud).ToList();
            var n = NombresEmpleados(c, vales.Select(v => v.EmpleadoId).Distinct().ToList());
            var filas = vales.Select(v => Fila(v.Numero, n.GetValueOrDefault(v.EmpleadoId, "—"), F(v.FechaSolicitud), v.Estado, L(v.Monto))).ToList();
            return RespuestaAsistente.Tabla("Vales en trámite", vales.Count == 0 ? "No hay vales pendientes." : $"{vales.Count} vale(s) por {L(vales.Sum(v => v.Monto))}.",
                new[] { "Vale", "Empleado", "Solicitado", "Estado", "Monto" }, filas, null, new[] { 4 });
        }

        private static RespuestaAsistente Vacaciones(ContextoAccion c)
        {
            var vac = c.Db.VacacionesEmpleado.AsNoTracking()
                .Where(v => v.Estado == "Solicitado" || (v.Estado == "Aprobado" && v.FechaFin >= c.Hoy)).OrderBy(v => v.FechaInicio).ToList();
            var n = NombresEmpleados(c, vac.Select(v => v.EmpleadoId).Distinct().ToList());
            var filas = vac.Select(v => Fila(v.Numero, n.GetValueOrDefault(v.EmpleadoId, "—"), F(v.FechaInicio), F(v.FechaFin),
                v.DiasSolicitados.ToString("N1", Inv), v.Estado)).ToList();
            return RespuestaAsistente.Tabla("Vacaciones", vac.Count == 0 ? "No hay solicitudes ni vacaciones próximas." : $"{vac.Count} registro(s).",
                new[] { "Solicitud", "Empleado", "Inicio", "Fin", "Días", "Estado" }, filas, null, new[] { 4 });
        }

        private static RespuestaAsistente NominaResumen(ContextoAccion c)
        {
            var noms = c.Db.Nominas.AsNoTracking().Where(n => n.Estado != "Anulada").OrderByDescending(n => n.FechaPago).Take(6).ToList();
            var filas = noms.Select(n => Fila(n.Numero, n.PeriodoDescripcion, n.Estado, n.CantidadEmpleados.ToString(), L(n.TotalSalariosBrutos), L(n.TotalNeto))).ToList();
            return RespuestaAsistente.Tabla("Resumen de nómina", filas.Count == 0 ? "Todavía no hay nóminas." : "Últimas nóminas.",
                new[] { "Nómina", "Período", "Estado", "Empleados", "Bruto", "Neto" }, filas, null, new[] { 3, 4, 5 });
        }

        // ════════════════════════════════════════════════════════════════
        //  LOGÍSTICA
        // ════════════════════════════════════════════════════════════════
        private static RespuestaAsistente EnviosPendientes(ContextoAccion c)
        {
            var estados = new List<string> { "Pendiente", "EnRuta", "Reagendado" };
            var env = c.Db.Envios.AsNoTracking().Where(e => estados.Contains(e.Estado)).OrderBy(e => e.FechaEntregaEstimada ?? e.FechaCreacion).ToList();
            var filas = env.Take(Limite).Select(e => Fila(e.Numero, e.ClienteNombre, e.Ciudad ?? e.ZonaNombre ?? "—", e.Estado,
                e.FechaEntregaEstimada.HasValue ? F(e.FechaEntregaEstimada.Value) : "—")).ToList();
            return RespuestaAsistente.Tabla("Envíos pendientes", env.Count == 0 ? "No hay envíos pendientes." : $"{env.Count} envío(s) por entregar{Nota(env.Count)}.",
                new[] { "Envío", "Cliente", "Destino", "Estado", "Entrega estimada" }, filas);
        }

        private static RespuestaAsistente Rutas(ContextoAccion c)
        {
            var d = c.Desde.Date; var h = c.Hasta.Date.AddDays(1);
            var rutas = c.Db.Rutas.AsNoTracking().Where(r => r.Fecha >= d && r.Fecha < h).OrderBy(r => r.Fecha).ToList();
            var filas = rutas.Select(r => Fila(r.Numero, F(r.Fecha), r.RepartidorNombre, r.Estado, $"{r.ParadasEntregadas}/{r.TotalParadas}")).ToList();
            return RespuestaAsistente.Tabla($"Rutas {c.RangoTexto}", rutas.Count == 0 ? $"No hay rutas {c.RangoTexto}." : $"{rutas.Count} ruta(s) {c.RangoTexto}.",
                new[] { "Ruta", "Fecha", "Repartidor", "Estado", "Entregadas" }, filas);
        }

        // ════════════════════════════════════════════════════════════════
        //  SEGURIDAD
        // ════════════════════════════════════════════════════════════════
        private static RespuestaAsistente Actividad(ContextoAccion c)
        {
            var act = c.Db.Actividades.AsNoTracking().OrderByDescending(a => a.Fecha).Take(25).ToList();
            var ids = act.Select(a => a.UsuarioId).Distinct().ToList();
            var us = c.Db.Usuarios.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionary(u => u.Id, u => u.Username);
            var filas = act.Select(a => Fila(a.Fecha.ToString("dd/MM HH:mm", Inv), us.GetValueOrDefault(a.UsuarioId, "—"), a.Accion, a.Detalle ?? "")).ToList();
            return RespuestaAsistente.Tabla("Actividad reciente", "Últimas 25 acciones registradas.", new[] { "Fecha", "Usuario", "Acción", "Detalle" }, filas);
        }
    }
}
