using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Movil
{
    /// <summary>
    /// Panel del dueño pensado para el celular (instalable como app): ventas de hoy, caja,
    /// por cobrar, por pagar, stock bajo y alertas. Cada bloque respeta los permisos del rol.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context) { _context = context; }

        public class Tarjeta
        {
            public string Icono { get; set; } = "";
            public string Etiqueta { get; set; } = "";
            public string Valor { get; set; } = "";
            public string Detalle { get; set; } = "";
            public string Clase { get; set; } = "";   // up | down | ""
            public string? Pagina { get; set; }
        }

        public class AlertaMovil
        {
            public string Texto { get; set; } = "";
            public string Nivel { get; set; } = "warning";
            public string? Pagina { get; set; }
        }

        public class VentaMovil
        {
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public decimal Total { get; set; }
            public string Cliente { get; set; } = "Consumidor final";
        }

        public string Negocio { get; set; } = "Kirkenta";
        public string Usuario { get; set; } = "";
        public bool MostrarVentas { get; set; }
        public decimal VentasHoy { get; set; }
        public int CantidadVentasHoy { get; set; }
        public decimal VentasAyer { get; set; }
        public decimal VentasMes { get; set; }
        public decimal TicketPromedio => CantidadVentasHoy > 0 ? Math.Round(VentasHoy / CantidadVentasHoy, 2) : 0;
        public List<string> Dias7 { get; set; } = new();
        public List<decimal> Montos7 { get; set; } = new();
        public List<Tarjeta> Tarjetas { get; set; } = new();
        public List<AlertaMovil> Alertas { get; set; } = new();
        public List<VentaMovil> UltimasVentas { get; set; } = new();
        public bool PuedeVender { get; set; }
        public bool SinAcceso => !MostrarVentas && !Tarjetas.Any() && !Alertas.Any();

        private bool Puede(string modulo, string sub, string accion = "ver")
            => PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, modulo, sub, accion);

        private static string L(decimal v) => "L. " + v.ToString("N2");

        public void OnGet()
        {
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            Usuario = User.Identity?.Name ?? "";
            Negocio = _context.ConfiguracionEmpresa.Select(e => e.Nombre).FirstOrDefault() ?? "Kirkenta";
            PuedeVender = Puede("Ventas", "Create", "crear");

            if (Puede("Ventas", "Index"))
            {
                MostrarVentas = true;
                var desde7 = hoy.AddDays(-6);
                var ventas = _context.Ventas
                    .Where(v => v.Fecha >= inicioMes.AddMonths(-1) && v.Estado == "Completada")
                    .Select(v => new { v.Fecha, v.Total }).ToList();

                VentasHoy = ventas.Where(v => v.Fecha.Date == hoy).Sum(v => v.Total);
                CantidadVentasHoy = ventas.Count(v => v.Fecha.Date == hoy);
                VentasAyer = ventas.Where(v => v.Fecha.Date == hoy.AddDays(-1)).Sum(v => v.Total);
                VentasMes = ventas.Where(v => v.Fecha >= inicioMes).Sum(v => v.Total);

                var cultura = new System.Globalization.CultureInfo("es-HN");
                for (int i = 0; i < 7; i++)
                {
                    var d = desde7.AddDays(i);
                    Dias7.Add(d.ToString("ddd", cultura));
                    Montos7.Add(ventas.Where(v => v.Fecha.Date == d).Sum(v => v.Total));
                }

                var ultimas = _context.Ventas.Where(v => v.Estado == "Completada")
                    .OrderByDescending(v => v.Fecha).Take(6).ToList();
                var ids = ultimas.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value).Distinct().ToList();
                var nombres = _context.Clientes.Where(c => ids.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Nombre);
                UltimasVentas = ultimas.Select(v => new VentaMovil
                {
                    Numero = v.Numero,
                    Fecha = v.Fecha,
                    Total = v.Total,
                    Cliente = v.ClienteId.HasValue && nombres.TryGetValue(v.ClienteId.Value, out var n) ? n : "Consumidor final"
                }).ToList();
            }

            if (Puede("Finanzas", "Index"))
            {
                var saldo = _context.CuentasFinancieras.Where(c => c.Activa).Sum(c => (decimal?)c.SaldoActual) ?? 0;
                Tarjetas.Add(new Tarjeta { Icono = "🏦", Etiqueta = "Caja y bancos", Valor = L(saldo), Detalle = "Saldo de cuentas activas", Pagina = "/Finanzas/Index" });
            }

            if (Puede("Ventas", "Facturas") || Puede("Finanzas", "Cobros"))
            {
                var credito = _context.Ventas.Where(v => v.EsCredito && v.SaldoPendiente > 0 && v.Estado != "Anulada").ToList();
                var facturas = _context.Facturas.Where(f => f.VentaId == null && f.Saldo > 0 && f.Estado != "Anulada").ToList();
                var porCobrar = credito.Sum(v => v.SaldoPendiente) + facturas.Sum(f => f.Saldo);
                var vencido = credito.Where(v => (v.FechaVencimientoCredito ?? v.Fecha).Date < hoy).Sum(v => v.SaldoPendiente)
                            + facturas.Where(f => (f.FechaVencimiento ?? f.Fecha).Date < hoy).Sum(f => f.Saldo);
                Tarjetas.Add(new Tarjeta
                {
                    Icono = "💰", Etiqueta = "Por cobrar", Valor = L(porCobrar),
                    Detalle = vencido > 0 ? $"{L(vencido)} vencido" : "Sin vencidos",
                    Clase = vencido > 0 ? "down" : "up", Pagina = "/Reportes/CuentasPorCobrar"
                });
                if (vencido > 0)
                    Alertas.Add(new AlertaMovil { Texto = $"Te deben {L(vencido)} vencido", Nivel = "danger", Pagina = "/Reportes/CuentasPorCobrar" });
            }

            if (Puede("Compras", "CuentasPorPagar") || Puede("Compras", "Ordenes"))
            {
                var excluidos = new List<string> { "Borrador", "Cancelada", "Anulada" };
                var ordenes = _context.OrdenesCompra.Where(o => o.Saldo > 0 && !excluidos.Contains(o.Estado)).ToList();
                var dias = _context.Proveedores.ToDictionary(p => p.Id, p => p.DiasCredito);
                var porPagar = ordenes.Sum(o => o.Saldo);
                var vencido = ordenes
                    .Where(o => (o.FechaRecepcion ?? o.Fecha).Date.AddDays(dias.TryGetValue(o.ProveedorId, out var d) ? d : 0) < hoy)
                    .Sum(o => o.Saldo);
                Tarjetas.Add(new Tarjeta
                {
                    Icono = "🧾", Etiqueta = "Por pagar", Valor = L(porPagar),
                    Detalle = vencido > 0 ? $"{L(vencido)} vencido" : "Sin vencidos",
                    Clase = vencido > 0 ? "down" : "up", Pagina = "/Reportes/CuentasPorPagar"
                });
                if (vencido > 0)
                    Alertas.Add(new AlertaMovil { Texto = $"Debes {L(vencido)} vencido a proveedores", Nivel = "danger", Pagina = "/Reportes/CuentasPorPagar" });
            }

            if (Puede("Inventario", "Productos"))
            {
                var bajos = _context.Productos.Count(p => p.Activo && p.Stock <= p.StockMinimo);
                Tarjetas.Add(new Tarjeta
                {
                    Icono = "📦", Etiqueta = "Stock bajo", Valor = bajos.ToString(),
                    Detalle = bajos > 0 ? "Productos por reponer" : "Todo en orden",
                    Clase = bajos > 0 ? "down" : "up", Pagina = "/Productos/Index"
                });
                if (bajos > 0)
                    Alertas.Add(new AlertaMovil { Texto = $"{bajos} producto(s) con stock bajo", Nivel = "warning", Pagina = "/Productos/Index" });
            }

            if (Puede("Ventas", "Facturas"))
            {
                var emp = _context.ConfiguracionEmpresa.FirstOrDefault();
                if (emp != null && emp.FacturacionFiscalActiva && emp.FechaLimiteEmision.HasValue)
                {
                    var diasCai = (emp.FechaLimiteEmision.Value.Date - hoy).Days;
                    if (diasCai < 0)
                        Alertas.Add(new AlertaMovil { Texto = "El CAI está vencido: no se pueden emitir facturas", Nivel = "danger", Pagina = "/Configuracion/Index" });
                    else if (diasCai <= 15)
                        Alertas.Add(new AlertaMovil { Texto = $"El CAI vence en {diasCai} día(s)", Nivel = "warning", Pagina = "/Configuracion/Index" });
                }
                var avisoRango = FacturacionFiscalHelper.AlertaRango(_context);
                if (avisoRango != null)
                    Alertas.Add(new AlertaMovil { Texto = avisoRango.Value.Texto, Nivel = avisoRango.Value.Nivel, Pagina = "/Configuracion/Index" });
            }

            if (Puede("Seguridad", "Index"))
            {
                try
                {
                    var aviso = RespaldoEstado.Avisar(hoy);
                    if (aviso != null)
                        Alertas.Add(new AlertaMovil { Texto = aviso.Texto, Nivel = aviso.Nivel, Pagina = "/Seguridad/Index" });
                }
                catch { }
            }

            if (PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Pedidos", "ver"))
            {
                try
                {
                    var web = _context.PedidosWeb.Count(p => p.Estado == "Nuevo");
                    if (web > 0)
                        Alertas.Add(new AlertaMovil { Texto = $"{web} pedido(s) web nuevo(s)", Nivel = "warning", Pagina = "/PedidosWeb/Index" });
                }
                catch { }
                var pend = _context.Pedidos.Count(p => p.Estado == "Pendiente" || p.Estado == "En preparacion");
                if (pend > 0)
                    Alertas.Add(new AlertaMovil { Texto = $"{pend} pedido(s) pendientes", Nivel = "warning", Pagina = "/Pedidos/Index" });
            }
        }
    }
}
