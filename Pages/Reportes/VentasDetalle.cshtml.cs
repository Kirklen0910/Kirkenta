using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Kirkenta.Pages.Reportes
{
    /// <summary>
    /// Ventas por período, dinámico: por día, método de pago, cajero, cliente o listado
    /// venta por venta. Muestra descuentos, envío, ISV, costo y utilidad.
    /// </summary>
    public class VentasDetalleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public VentasDetalleModel(ApplicationDbContext context) { _context = context; }

        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }
        /// <summary>0 = todos | -1 = crédito (sin pago) | id de método</summary>
        [BindProperty(SupportsGet = true)] public int? MetodoPagoId { get; set; }
        [BindProperty(SupportsGet = true)] public int? CajeroId { get; set; }
        [BindProperty(SupportsGet = true)] public int? ClienteId { get; set; }
        [BindProperty(SupportsGet = true)] public int? ProductoId { get; set; }
        [BindProperty(SupportsGet = true)] public string? Buscar { get; set; }
        /// <summary>"" = todas | contado | credito</summary>
        [BindProperty(SupportsGet = true)] public string? Tipo { get; set; }
        /// <summary>vigentes (default) | anuladas | todas</summary>
        [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
        /// <summary>venta | dia | metodo | cajero | cliente</summary>
        [BindProperty(SupportsGet = true)] public string? Agrupar { get; set; }

        public List<SelectListItem> Metodos { get; set; } = new();
        public List<SelectListItem> Cajeros { get; set; } = new();
        public string? ClienteNombre { get; set; }
        public string? ProductoNombre { get; set; }

        public List<VentaFila> Ventas { get; set; } = new();
        public List<Grupo> Grupos { get; set; } = new();

        public int CantidadVentas { get; set; }
        public decimal TotalBruto { get; set; }
        public decimal TotalDescuento { get; set; }
        public decimal TotalNeto { get; set; }
        public decimal TotalEnvio { get; set; }
        public decimal TotalIsv { get; set; }
        public decimal TotalCobrado { get; set; }
        public decimal TotalCosto { get; set; }
        public decimal TotalUtilidad { get; set; }
        public decimal TicketPromedio => CantidadVentas > 0 ? TotalCobrado / CantidadVentas : 0;
        public decimal SaldoCredito { get; set; }

        public class VentaFila
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public int? ClienteId { get; set; }
            public string Cliente { get; set; } = "Consumidor final";
            public string Cajero { get; set; } = "—";
            public int? CajeroId { get; set; }
            public string Metodo { get; set; } = "";
            public bool EsCredito { get; set; }
            public string Estado { get; set; } = "";
            public decimal Bruto { get; set; }
            public decimal Descuento { get; set; }
            public decimal Neto { get; set; }
            public decimal Envio { get; set; }
            public decimal Isv { get; set; }
            public decimal Total { get; set; }
            public decimal Costo { get; set; }
            public decimal Utilidad { get; set; }
        }

        public class Grupo
        {
            public string Clave { get; set; } = "";
            public string Etiqueta { get; set; } = "";
            public int Ventas { get; set; }
            public decimal Bruto { get; set; }
            public decimal Descuento { get; set; }
            public decimal Neto { get; set; }
            public decimal Envio { get; set; }
            public decimal Isv { get; set; }
            public decimal Total { get; set; }
            public decimal Utilidad { get; set; }
            public decimal Cobrado { get; set; }
        }

        private async Task<bool> CargarAsync()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Reportes", "Ventas", "ver"))
                return false;

            var hasta = (Hasta ?? DateTime.Today).Date;
            var desde = (Desde ?? new DateTime(hasta.Year, hasta.Month, 1)).Date;
            if (desde > hasta) (desde, hasta) = (hasta, desde);
            Desde = desde; Hasta = hasta;
            var hastaExcl = hasta.AddDays(1);
            if (string.IsNullOrEmpty(Agrupar)) Agrupar = "dia";
            if (string.IsNullOrEmpty(Estado)) Estado = "vigentes";

            Metodos = await _context.MetodosPago.OrderBy(m => m.Nombre)
                .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Nombre }).ToListAsync();
            Cajeros = await _context.Usuarios.OrderBy(u => u.Username)
                .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Username }).ToListAsync();
            var metodoNombre = await _context.MetodosPago.ToDictionaryAsync(m => m.Id, m => m.Nombre);
            var cajeroNombre = await _context.Usuarios.ToDictionaryAsync(u => u.Id, u => u.Username);

            var q = _context.Ventas.AsNoTracking().Where(v => v.Fecha >= desde && v.Fecha < hastaExcl);
            q = Estado switch
            {
                "anuladas" => q.Where(v => v.Estado == "Anulada" || v.Estado == "Cancelada"),
                "todas" => q,
                _ => q.Where(v => v.Estado != "Anulada" && v.Estado != "Cancelada")
            };
            if (Tipo == "contado") q = q.Where(v => !v.EsCredito);
            else if (Tipo == "credito") q = q.Where(v => v.EsCredito);
            if (CajeroId.HasValue) q = q.Where(v => v.UsuarioCreoId == CajeroId);
            if (ClienteId.HasValue)
            {
                q = q.Where(v => v.ClienteId == ClienteId);
                ClienteNombre = await _context.Clientes.Where(c => c.Id == ClienteId).Select(c => c.Nombre).FirstOrDefaultAsync();
            }
            if (ProductoId.HasValue)
            {
                q = q.Where(v => _context.DetalleVentas.Any(d => d.VentaId == v.Id && d.ProductoId == ProductoId));
                ProductoNombre = await _context.Productos.Where(p => p.Id == ProductoId).Select(p => p.Nombre).FirstOrDefaultAsync();
            }
            if (MetodoPagoId == -1) q = q.Where(v => v.EsCredito);
            else if (MetodoPagoId > 0) q = q.Where(v => _context.Pagos.Any(p => p.VentaId == v.Id && p.MetodoPagoId == MetodoPagoId));
            if (!string.IsNullOrWhiteSpace(Buscar))
            {
                var t = Buscar.Trim();
                q = q.Where(v => v.Numero.Contains(t)
                    || (v.ClienteId != null && _context.Clientes.Any(c => c.Id == v.ClienteId && (c.Nombre.Contains(t) || (c.RTN != null && c.RTN.Contains(t))))));
            }

            var ventas = await q.OrderByDescending(v => v.Fecha).ThenByDescending(v => v.Id).Take(5000).ToListAsync();
            var ids = ventas.Select(v => v.Id).ToList();

            var lineas = new Dictionary<int, (decimal bruto, decimal desc, decimal neto, decimal costo)>();
            var pagos = new List<(int VentaId, int MetodoPagoId, decimal Monto)>();
            if (ids.Count > 0)
            {
                var det = await _context.DetalleVentas.AsNoTracking().Where(d => ids.Contains(d.VentaId))
                    .Select(d => new { d.VentaId, d.Cantidad, d.PrecioUnitario, d.Descuento, d.Subtotal, d.CostoTotalLinea })
                    .ToListAsync();
                foreach (var g in det.GroupBy(d => d.VentaId))
                    lineas[g.Key] = (g.Sum(x => x.Cantidad * x.PrecioUnitario), g.Sum(x => x.Descuento),
                                     g.Sum(x => x.Subtotal), g.Sum(x => x.CostoTotalLinea));

                var pg = await _context.Pagos.AsNoTracking().Where(p => p.VentaId != null && ids.Contains(p.VentaId.Value))
                    .Select(p => new { p.VentaId, p.MetodoPagoId, p.Monto }).ToListAsync();
                foreach (var p in pg) pagos.Add((p.VentaId!.Value, p.MetodoPagoId, p.Monto));
            }
            var pagosPorVenta = pagos.GroupBy(p => p.VentaId).ToDictionary(g => g.Key, g => g.ToList());

            var clienteIds = ventas.Where(v => v.ClienteId != null).Select(v => v.ClienteId!.Value).Distinct().ToList();
            var clientes = await _context.Clientes.AsNoTracking().Where(c => clienteIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Nombre);

            // Cobrado por método (para el desglose); crédito se acumula aparte
            var porMetodo = new Dictionary<string, Grupo>();
            Grupo G(Dictionary<string, Grupo> d, string clave, string etq)
            {
                if (!d.TryGetValue(clave, out var g)) d[clave] = g = new Grupo { Clave = clave, Etiqueta = etq };
                return g;
            }

            foreach (var v in ventas)
            {
                lineas.TryGetValue(v.Id, out var l);
                var descVenta = l.desc + v.Descuento;
                var fila = new VentaFila
                {
                    Id = v.Id, Numero = v.Numero, Fecha = v.Fecha, ClienteId = v.ClienteId,
                    Cliente = v.ClienteId.HasValue && clientes.TryGetValue(v.ClienteId.Value, out var cn) ? cn : "Consumidor final",
                    CajeroId = v.UsuarioCreoId,
                    Cajero = v.UsuarioCreoId.HasValue && cajeroNombre.TryGetValue(v.UsuarioCreoId.Value, out var un) ? un : "—",
                    EsCredito = v.EsCredito, Estado = v.Estado,
                    Bruto = l.bruto, Descuento = descVenta, Neto = l.neto - v.Descuento,
                    Envio = v.MontoEnvio, Isv = v.Impuestos, Total = v.Total,
                    Costo = l.costo
                };
                fila.Utilidad = fila.Neto - fila.Costo;
                pagosPorVenta.TryGetValue(v.Id, out var pv);
                var nombres = (pv ?? new()).Select(p => metodoNombre.TryGetValue(p.MetodoPagoId, out var mn) ? mn : "?").Distinct().ToList();
                if (v.EsCredito && nombres.Count == 0) nombres.Add("Crédito");
                fila.Metodo = nombres.Count == 0 ? "—" : string.Join(" + ", nombres);
                Ventas.Add(fila);

                if (v.EsCredito) { G(porMetodo, "-1", "Crédito").Cobrado += v.Total; SaldoCredito += v.SaldoPendiente; }
                foreach (var p in pv ?? new())
                {
                    var n = metodoNombre.TryGetValue(p.MetodoPagoId, out var mn) ? mn : "?";
                    var g = G(porMetodo, p.MetodoPagoId.ToString(), n);
                    g.Cobrado += p.Monto;
                }
            }

            CantidadVentas = Ventas.Count;
            TotalBruto = Ventas.Sum(v => v.Bruto);
            TotalDescuento = Ventas.Sum(v => v.Descuento);
            TotalNeto = Ventas.Sum(v => v.Neto);
            TotalEnvio = Ventas.Sum(v => v.Envio);
            TotalIsv = Ventas.Sum(v => v.Isv);
            TotalCobrado = Ventas.Sum(v => v.Total);
            TotalCosto = Ventas.Sum(v => v.Costo);
            TotalUtilidad = Ventas.Sum(v => v.Utilidad);

            if (Agrupar == "metodo")
            {
                foreach (var g in porMetodo.Values)
                {
                    g.Ventas = Ventas.Count(v => v.Metodo.Split(" + ").Contains(g.Etiqueta));
                    g.Total = g.Cobrado;
                }
                Grupos = porMetodo.Values.OrderByDescending(g => g.Cobrado).ToList();
            }
            else if (Agrupar != "venta")
            {
                Func<VentaFila, (string, string)> clave = Agrupar switch
                {
                    "cajero" => v => (v.CajeroId?.ToString() ?? "0", v.Cajero),
                    "cliente" => v => (v.ClienteId?.ToString() ?? "0", v.Cliente),
                    _ => v => (v.Fecha.ToString("yyyy-MM-dd"), v.Fecha.ToString("dd/MM/yyyy ddd", new CultureInfo("es-HN")))
                };
                Grupos = Ventas.GroupBy(v => clave(v).Item1).Select(g => new Grupo
                {
                    Clave = g.Key, Etiqueta = clave(g.First()).Item2, Ventas = g.Count(),
                    Bruto = g.Sum(x => x.Bruto), Descuento = g.Sum(x => x.Descuento), Neto = g.Sum(x => x.Neto),
                    Envio = g.Sum(x => x.Envio), Isv = g.Sum(x => x.Isv), Total = g.Sum(x => x.Total),
                    Utilidad = g.Sum(x => x.Utilidad)
                }).ToList();
                Grupos = Agrupar == "dia" ? Grupos.OrderBy(g => g.Clave).ToList() : Grupos.OrderByDescending(g => g.Total).ToList();
            }
            // Desglose por método siempre disponible en la vista
            MetodosResumen = porMetodo.Values.OrderByDescending(g => g.Cobrado).ToList();
            return true;
        }

        public List<Grupo> MetodosResumen { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!await CargarAsync())
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnGetCsvAsync()
        {
            if (!await CargarAsync()) return RedirectToPage("/Index");
            string F(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);
            var filas = new List<string?[]>
            {
                new[] { "Fecha", "Venta", "Cliente", "Cajero", "Método de pago", "Estado", "Bruto", "Descuento", "Neto", "Envío", "ISV", "Total", "Costo", "Utilidad" }
            };
            filas.AddRange(Ventas.Select(v => new string?[]
            {
                v.Fecha.ToString("yyyy-MM-dd HH:mm"), v.Numero, v.Cliente, v.Cajero, v.Metodo, v.Estado,
                F(v.Bruto), F(v.Descuento), F(v.Neto), F(v.Envio), F(v.Isv), F(v.Total), F(v.Costo), F(v.Utilidad)
            }));
            return CsvExportHelper.Crear($"ventas-periodo-{Desde:yyyyMMdd}-{Hasta:yyyyMMdd}.csv", filas);
        }
    }
}
