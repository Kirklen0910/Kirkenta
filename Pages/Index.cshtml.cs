using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Kirkenta.Pages
{
    /// <summary>
    /// Dashboard dinámico: cada bloque (indicadores, gráfico, paneles, alertas y accesos rápidos)
    /// se muestra solo si el rol del usuario tiene el permiso correspondiente. Los permisos se
    /// leen en cada visita, así que al cambiar un rol el dashboard cambia de inmediato.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class Kpi
        {
            public string Etiqueta { get; set; } = "";
            public string Valor { get; set; } = "";
            public string Detalle { get; set; } = "";
            public string Clase { get; set; } = ""; // "up" | "down" | ""
            public string? Pagina { get; set; }
        }

        public class Acceso
        {
            public string Pagina { get; set; } = "";
            public string Icono { get; set; } = "";
            public string Etiqueta { get; set; } = "";
        }

        public class Alerta
        {
            public string Texto { get; set; } = "";
            public string Nivel { get; set; } = "warning"; // warning | danger
            public string? Pagina { get; set; }
        }

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

        public class ProductoBajo
        {
            public string Nombre { get; set; } = "";
            public decimal Stock { get; set; }
            public decimal Minimo { get; set; }
        }

        public List<Kpi> Kpis { get; set; } = new();
        public List<Acceso> Accesos { get; set; } = new();
        public List<Alerta> Alertas { get; set; } = new();

        public bool MostrarVentas { get; set; }
        public bool MostrarStockBajo { get; set; }
        public string Ventas7DiasJson { get; set; } = "{}";
        public List<TopProductoMes> TopProductosMes { get; set; } = new();
        public List<VentaReciente> UltimasVentas { get; set; } = new();
        public List<ProductoBajo> ProductosBajos { get; set; } = new();

        public bool SinAcceso => !Kpis.Any() && !Accesos.Any() && !MostrarVentas && !MostrarStockBajo && !Alertas.Any();

        private bool Puede(string modulo, string sub, string accion = "ver")
            => PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, modulo, sub, accion);

        private static string L(decimal v) => "L. " + v.ToString("N2");

        private static (string detalle, string clase) Tendencia(decimal actual, decimal anterior, string contra)
        {
            var flecha = actual >= anterior ? "↑" : "↓";
            var pct = anterior > 0 ? Math.Abs((actual - anterior) / anterior * 100).ToString("N1") : "0";
            return ($"{flecha} {pct}% {contra}", actual >= anterior ? "up" : "down");
        }

        public void OnGet()
        {
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            // ===== VENTAS =====
            if (Puede("Ventas", "Index"))
            {
                MostrarVentas = true;
                var ayer = hoy.AddDays(-1);
                var inicioMesAnt = inicioMes.AddMonths(-1);

                decimal Suma(DateTime d, DateTime h) => _context.Ventas
                    .Where(v => v.Fecha >= d && v.Fecha < h && v.Estado == "Completada")
                    .Sum(v => (decimal?)v.Total) ?? 0;

                var vHoy = Suma(hoy, hoy.AddDays(1));
                var vAyer = Suma(ayer, hoy);
                var vMes = Suma(inicioMes, finMes);
                var vMesAnt = Suma(inicioMesAnt, inicioMes);

                var (dHoy, cHoy) = Tendencia(vHoy, vAyer, "vs ayer");
                var (dMes, cMes) = Tendencia(vMes, vMesAnt, "vs mes anterior");
                Kpis.Add(new Kpi { Etiqueta = "Ventas de hoy", Valor = L(vHoy), Detalle = dHoy, Clase = cHoy, Pagina = "/Ventas/Index" });
                Kpis.Add(new Kpi { Etiqueta = "Ventas del mes", Valor = L(vMes), Detalle = dMes, Clase = cMes, Pagina = "/Ventas/Index" });

                // Ventas últimos 7 días
                var hace7 = hoy.AddDays(-6);
                var ventas7 = _context.Ventas.Where(v => v.Fecha >= hace7 && v.Estado == "Completada").ToList();
                var labels = new List<string>();
                var data = new List<decimal>();
                for (int i = 0; i < 7; i++)
                {
                    var dia = hace7.AddDays(i);
                    labels.Add(dia.ToString("ddd dd/MM"));
                    data.Add(ventas7.Where(v => v.Fecha.Date == dia.Date).Sum(v => v.Total));
                }
                Ventas7DiasJson = JsonSerializer.Serialize(new { labels, data });

                // Top 5 productos del mes
                var ventaIdsMes = _context.Ventas
                    .Where(v => v.Fecha >= inicioMes && v.Fecha < finMes && v.Estado == "Completada")
                    .Select(v => v.Id).ToList();
                var detallesMes = _context.DetalleVentas.Where(d => ventaIdsMes.Contains(d.VentaId)).ToList();
                var nombresProd = _context.Productos.ToDictionary(p => p.Id, p => p.Nombre);
                TopProductosMes = detallesMes
                    .GroupBy(d => d.ProductoId)
                    .Select(g => new TopProductoMes
                    {
                        Nombre = nombresProd.TryGetValue(g.Key, out var n) ? n : "—",
                        Cantidad = g.Sum(d => d.Cantidad),
                        Total = g.Sum(d => d.Total)
                    })
                    .OrderByDescending(x => x.Total).Take(5).ToList();

                // Últimas 5 ventas
                var ultimas = _context.Ventas.Where(v => v.Estado == "Completada")
                    .OrderByDescending(v => v.Fecha).Take(5).ToList();
                var idsCli = ultimas.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value).Distinct().ToList();
                var nombresCli = _context.Clientes.Where(c => idsCli.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Nombre);
                UltimasVentas = ultimas.Select(v => new VentaReciente
                {
                    Numero = v.Numero,
                    Fecha = v.Fecha,
                    Total = v.Total,
                    ClienteNombre = v.ClienteId.HasValue && nombresCli.TryGetValue(v.ClienteId.Value, out var cn) ? cn : "Consumidor final"
                }).ToList();
            }

            // ===== CLIENTES =====
            if (Puede("Ventas", "Clientes"))
            {
                Kpis.Add(new Kpi
                {
                    Etiqueta = "Clientes activos",
                    Valor = _context.Clientes.Count(c => c.Activo).ToString(),
                    Detalle = $"Total registrados: {_context.Clientes.Count()}",
                    Pagina = "/Clientes/Index"
                });
            }

            // ===== COTIZACIONES / PEDIDOS =====
            if (Puede("Ventas", "Cotizaciones"))
            {
                var abiertas = _context.Cotizaciones.Count(c => c.Estado == "Borrador" || c.Estado == "Enviada" || c.Estado == "Aceptada");
                Kpis.Add(new Kpi { Etiqueta = "Cotizaciones abiertas", Valor = abiertas.ToString(), Detalle = "Borrador, enviadas o aceptadas", Pagina = "/Cotizaciones/Index" });
            }
            if (Puede("Ventas", "Pedidos"))
            {
                var pendientes = _context.Pedidos.Count(p => p.Estado == "Pendiente" || p.Estado == "En preparacion");
                Kpis.Add(new Kpi
                {
                    Etiqueta = "Pedidos pendientes",
                    Valor = pendientes.ToString(),
                    Detalle = pendientes > 0 ? "Por preparar o entregar" : "Sin pendientes",
                    Clase = pendientes > 0 ? "down" : "up",
                    Pagina = "/Pedidos/Index"
                });
            }

            // ===== INVENTARIO =====
            if (Puede("Inventario", "Productos"))
            {
                var total = _context.Productos.Count(p => p.Activo);
                var bajos = _context.Productos.Count(p => p.Activo && p.Stock <= p.StockMinimo);
                Kpis.Add(new Kpi
                {
                    Etiqueta = "Productos en stock",
                    Valor = total.ToString(),
                    Detalle = bajos > 0 ? $"⚠️ {bajos} con stock bajo" : "✅ Stock saludable",
                    Clase = bajos > 0 ? "down" : "up",
                    Pagina = "/Productos/Index"
                });

                if (bajos > 0)
                {
                    MostrarStockBajo = true;
                    ProductosBajos = _context.Productos
                        .Where(p => p.Activo && p.Stock <= p.StockMinimo)
                        .OrderBy(p => p.Stock).Take(6)
                        .Select(p => new ProductoBajo { Nombre = p.Nombre, Stock = p.Stock, Minimo = p.StockMinimo })
                        .ToList();
                }
            }

            // ===== POR COBRAR =====
            if (Puede("Ventas", "Facturas") || Puede("Finanzas", "Cobros"))
            {
                var ventasCredito = _context.Ventas
                    .Where(v => v.EsCredito && v.SaldoPendiente > 0 && v.Estado != "Anulada").ToList();
                var facturasSinVenta = _context.Facturas
                    .Where(f => f.VentaId == null && f.Saldo > 0 && f.Estado != "Anulada").ToList();
                var porCobrar = ventasCredito.Sum(v => v.SaldoPendiente) + facturasSinVenta.Sum(f => f.Saldo);
                var vencido = ventasCredito.Where(v => (v.FechaVencimientoCredito ?? v.Fecha).Date < hoy).Sum(v => v.SaldoPendiente)
                            + facturasSinVenta.Where(f => (f.FechaVencimiento ?? f.Fecha).Date < hoy).Sum(f => f.Saldo);
                Kpis.Add(new Kpi
                {
                    Etiqueta = "Por cobrar",
                    Valor = L(porCobrar),
                    Detalle = vencido > 0 ? $"⚠️ {L(vencido)} vencido" : "✅ Sin vencidos",
                    Clase = vencido > 0 ? "down" : "up",
                    Pagina = "/Reportes/CuentasPorCobrar"
                });
                if (vencido > 0)
                    Alertas.Add(new Alerta { Texto = $"Cuentas por cobrar vencidas: {L(vencido)}", Nivel = "danger", Pagina = "/Reportes/CuentasPorCobrar" });
            }

            // ===== POR PAGAR / ÓRDENES =====
            if (Puede("Compras", "CuentasPorPagar") || Puede("Compras", "Ordenes"))
            {
                var excluidos = new List<string> { "Borrador", "Cancelada", "Anulada" }; // List (no array): evita el bug de EF con spans en .NET 10
                var ordenes = _context.OrdenesCompra.Where(o => o.Saldo > 0 && !excluidos.Contains(o.Estado)).ToList();
                var dias = _context.Proveedores.ToDictionary(p => p.Id, p => p.DiasCredito);
                var porPagar = ordenes.Sum(o => o.Saldo);
                var vencidoPagar = ordenes
                    .Where(o => (o.FechaRecepcion ?? o.Fecha).Date.AddDays(dias.TryGetValue(o.ProveedorId, out var d) ? d : 0) < hoy)
                    .Sum(o => o.Saldo);
                Kpis.Add(new Kpi
                {
                    Etiqueta = "Por pagar a proveedores",
                    Valor = L(porPagar),
                    Detalle = vencidoPagar > 0 ? $"⚠️ {L(vencidoPagar)} vencido" : "✅ Sin vencidos",
                    Clase = vencidoPagar > 0 ? "down" : "up",
                    Pagina = "/Reportes/CuentasPorPagar"
                });
                if (vencidoPagar > 0)
                    Alertas.Add(new Alerta { Texto = $"Cuentas por pagar vencidas: {L(vencidoPagar)}", Nivel = "danger", Pagina = "/Reportes/CuentasPorPagar" });
            }

            // ===== FINANZAS (efectivo real: se excluyen los movimientos solo contables) =====
            if (Puede("Finanzas", "Index"))
            {
                var saldo = _context.CuentasFinancieras.Where(c => c.Activa).Sum(c => (decimal?)c.SaldoActual) ?? 0;
                Kpis.Add(new Kpi { Etiqueta = "Caja y bancos", Valor = L(saldo), Detalle = "Saldo actual de cuentas activas", Pagina = "/Finanzas/Index" });

                var idsPlanCosto = _context.PlanCuentas.Where(p => p.Codigo == "5.1.01").Select(p => (int?)p.Id).ToList();
                var catsContables = _context.CategoriasFinancieras
                    .Where(c => c.PlanCuentaId != null && idsPlanCosto.Contains(c.PlanCuentaId))
                    .Select(c => c.Id).ToHashSet();
                var movs = _context.MovimientosFinancieros
                    .Where(m => m.Estado == "Activo" && m.Fecha >= inicioMes && m.Fecha < finMes
                                && (m.Tipo == "Ingreso" || m.Tipo == "Egreso"))
                    .ToList()
                    .Where(m => !(m.Notas != null && m.Notas.Contains(SaldoHelper.MarcaSoloContable))
                                && !(m.Tipo == "Egreso" && m.CategoriaId.HasValue && catsContables.Contains(m.CategoriaId.Value)))
                    .ToList();
                var ing = movs.Where(m => m.Tipo == "Ingreso").Sum(m => m.Monto);
                var egr = movs.Where(m => m.Tipo == "Egreso").Sum(m => m.Monto);
                Kpis.Add(new Kpi
                {
                    Etiqueta = "Flujo de caja del mes",
                    Valor = L(ing - egr),
                    Detalle = $"Entradas {L(ing)} · Salidas {L(egr)}",
                    Clase = ing - egr >= 0 ? "up" : "down",
                    Pagina = "/Finanzas/Reportes/Index"
                });
            }

            // ===== LOGÍSTICA =====
            if (Puede("Logistica", "Envios"))
            {
                var envios = _context.Envios.Count(e => e.Estado != "Entregado" && e.Estado != "Cancelado");
                Kpis.Add(new Kpi
                {
                    Etiqueta = "Envíos pendientes",
                    Valor = envios.ToString(),
                    Detalle = envios > 0 ? "Por despachar o en ruta" : "Sin pendientes",
                    Clase = envios > 0 ? "down" : "up"
                });
            }

            // ===== PEDIDOS DEL CATÁLOGO EN LÍNEA =====
            if (Puede("Ventas", "Pedidos"))
            {
                try
                {
                    var nuevosWeb = _context.PedidosWeb.Count(p => p.Estado == "Nuevo");
                    if (nuevosWeb > 0)
                        Alertas.Add(new Alerta { Texto = $"Tienes {nuevosWeb} pedido(s) web nuevo(s) del catálogo en línea.", Nivel = "warning", Pagina = "/PedidosWeb/Index" });
                }
                catch { /* la tabla se crea al arrancar; el aviso nunca debe romper el inicio */ }
            }

            // ===== ALERTA DE CAI =====
            if (Puede("Ventas", "Facturas"))
            {
                var emp = _context.ConfiguracionEmpresa.FirstOrDefault();
                if (emp != null && emp.FacturacionFiscalActiva && emp.FechaLimiteEmision.HasValue)
                {
                    var diasCai = (emp.FechaLimiteEmision.Value.Date - hoy).Days;
                    if (diasCai < 0)
                        Alertas.Add(new Alerta { Texto = $"El CAI venció el {emp.FechaLimiteEmision:dd/MM/yyyy}. No se pueden emitir facturas.", Nivel = "danger", Pagina = "/Configuracion/Index" });
                    else if (diasCai <= 15)
                        Alertas.Add(new Alerta { Texto = $"El CAI vence en {diasCai} día(s).", Nivel = "warning", Pagina = "/Configuracion/Index" });
                }
                var avisoRango = FacturacionFiscalHelper.AlertaRango(_context);
                if (avisoRango != null)
                    Alertas.Add(new Alerta { Texto = avisoRango.Value.Texto, Nivel = avisoRango.Value.Nivel, Pagina = "/Configuracion/Index" });
            }

            // ===== CONFIGURACIÓN INICIAL (solo administradores, mientras falte algo obligatorio) =====
            try
            {
                var nombreUsuario = User.Identity?.Name;
                var esAdminInicio = !string.IsNullOrEmpty(nombreUsuario) && _context.Usuarios.Any(x => x.Username == nombreUsuario && x.Rol == "Admin");
                if (esAdminInicio && ConfiguracionInicialHelper.FaltaConfigurar(_context))
                    Alertas.Add(new Alerta { Texto = "Falta terminar la configuración inicial del negocio (empresa, caja o catálogo). El asistente te guía paso a paso.", Nivel = "warning", Pagina = "/Configuracion/Inicio" });
            }
            catch { /* el aviso nunca debe romper el inicio */ }

            // ===== ALERTA DE RESPALDO (solo quien administra la seguridad) =====
            if (Puede("Seguridad", "Index"))
            {
                var aviso = RespaldoEstado.Avisar(DateTime.Now);
                if (aviso != null)
                    Alertas.Add(new Alerta { Texto = aviso.Texto, Nivel = aviso.Nivel, Pagina = "/Seguridad/Index" });
            }

            // ===== ACCESOS RÁPIDOS (solo lo que el rol puede hacer) =====
            void Add(bool ok, string pagina, string icono, string etiqueta)
            {
                if (ok) Accesos.Add(new Acceso { Pagina = pagina, Icono = icono, Etiqueta = etiqueta });
            }
            Add(Puede("Ventas", "POS"), "/POS/Index", "🛒", "Punto de venta");
            Add(Puede("Ventas", "Cotizaciones", "crear"), "/Cotizaciones/Create", "📝", "Nueva cotización");
            Add(Puede("Ventas", "Pedidos", "crear"), "/Pedidos/Create", "📋", "Nuevo pedido");
            Add(Puede("Ventas", "Facturas"), "/Facturas/Index", "🧾", "Facturas");
            Add(Puede("Ventas", "Clientes", "crear"), "/Clientes/Create", "👤", "Nuevo cliente");
            Add(Puede("Inventario", "Productos", "crear"), "/Productos/Create", "📦", "Nuevo producto");
            Add(Puede("Compras", "Ordenes", "crear"), "/OrdenesCompra/Create", "🚚", "Nueva orden de compra");
            Add(Puede("Finanzas", "Index"), "/Finanzas/Index", "💰", "Finanzas");
            Add(Puede("Reportes", "Index"), "/Reportes/Index", "📊", "Reportes");
            Add(Puede("Reportes", "PanelDueno"), "/Reportes/PanelDueno", "🧭", "Panel del dueño");
            Add(Puede("Finanzas", "Cobros"), "/Finanzas/Cobros/Recordatorios", "💬", "Recordatorios de cobro");
        }
    }
}
