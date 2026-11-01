using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Kirkenta.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kirkenta.Pages.POS
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IVentaService _ventaService;

        public IndexModel(ApplicationDbContext context, IVentaService ventaService)
        {
            _context = context;
            _ventaService = ventaService;
        }

        public List<Producto> Productos { get; set; } = new();
        public List<Categoria> Categorias { get; set; } = new();
        public List<MetodoPago> MetodosPago { get; set; } = new();
        public List<ZonaEnvio> ZonasEnvio { get; set; } = new();
        public Dictionary<int, decimal> ImpuestosDict { get; set; } = new();
        /// <summary>Impuesto que se aplica a los productos que no tienen uno asignado (y al envío).</summary>
        public int? ImpuestoPredeterminadoId { get; set; }
        public decimal TasaPredeterminada { get; set; }
        public string ClientesJson { get; set; } = "[]";
        public string ItemsInicialesJson { get; set; } = "[]";
        public string? CotizacionInfo { get; set; }
        public int? CotizacionId { get; set; }
        public int? PedidoWebId { get; set; }
        public int? ClienteInicialId { get; set; }
        public string ClienteInicialNombre { get; set; } = "";

        public bool HayApertura { get; set; }
        public string CuentaCajaNombre { get; set; } = "";
        public decimal SaldoApertura { get; set; }
        public int CuentaCajaId { get; set; }

        public IActionResult OnGet(int? cotizacionId, int? pedidoWebId)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Create", "crear"))
            {
                TempData["Error"] = "No tienes permiso para usar el POS";
                return RedirectToPage("/Index");
            }

            var apertura = _context.AperturasCaja.FirstOrDefault(a => a.Activa);
            if (apertura == null)
            {
                TempData["Error"] = "No hay ninguna caja abierta. Debes registrar una apertura antes de vender.";
                return RedirectToPage("/Finanzas/Aperturas/Create");
            }

            HayApertura = true;
            SaldoApertura = apertura.SaldoInicial;
            CuentaCajaId = apertura.CuentaId;
            CuentaCajaNombre = _context.CuentasFinancieras
                .FirstOrDefault(c => c.Id == apertura.CuentaId)?.Nombre ?? "—";

            Productos = _context.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToList();
            Categorias = _context.Categorias.Where(c => c.Activa).OrderBy(c => c.Nombre).ToList();
            MetodosPago = _context.MetodosPago.Where(m => m.Activo).OrderBy(m => m.Nombre).ToList();

            if (MetodosPago.Count == 0)
            {
                MetodosPago = new List<MetodoPago>
                {
                    new MetodoPago { Id = 1, Nombre = "Efectivo", Tipo = "Efectivo", RequiereReferencia = false, Activo = true },
                    new MetodoPago { Id = 2, Nombre = "Tarjeta", Tipo = "Tarjeta", RequiereReferencia = true, Activo = true },
                    new MetodoPago { Id = 3, Nombre = "Transferencia", Tipo = "Transferencia", RequiereReferencia = true, Activo = true },
                    new MetodoPago { Id = 4, Nombre = "Crédito", Tipo = "Credito", RequiereReferencia = false, Activo = true },
                };
            }

            ZonasEnvio = _context.ZonasEnvio.Where(z => z.Activa).OrderBy(z => z.Orden).ThenBy(z => z.Nombre).ToList();
            ImpuestosDict = _context.Impuestos.Where(i => i.Activo).ToDictionary(i => i.Id, i => i.Porcentaje);
            var impuestoDefault = _context.Impuestos.FirstOrDefault(i => i.Activo && i.EsPredeterminado);
            ImpuestoPredeterminadoId = impuestoDefault?.Id;
            TasaPredeterminada = impuestoDefault?.Porcentaje ?? 0m;

            var clientes = _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
            var clientesList = clientes.Select(c => new
            {
                id = c.Id,
                nombre = c.Nombre,
                rtn = c.RTN ?? "",
                codigo = c.Codigo ?? "",
                direccion = c.Direccion ?? "",
                telefono = c.Telefono ?? "",
                exonerado = c.EsExonerado
            }).ToList();
            ClientesJson = JsonSerializer.Serialize(clientesList);

            if (cotizacionId.HasValue)
            {
                var cot = _context.Cotizaciones.FirstOrDefault(c => c.Id == cotizacionId.Value);
                if (cot != null && cot.Estado != "Convertida")
                {
                    CotizacionId = cot.Id;
                    CotizacionInfo = $"Cargando cotización {cot.Numero}";
                    ClienteInicialId = cot.ClienteId;
                    ClienteInicialNombre = _context.Clientes.FirstOrDefault(c => c.Id == cot.ClienteId)?.Nombre ?? "";

                    var detalles = _context.DetalleCotizaciones.Where(d => d.CotizacionId == cot.Id).ToList();
                    var productosIds = detalles.Select(d => d.ProductoId).Distinct().ToList();
                    var productosDict = _context.Productos
                        .Where(p => productosIds.Contains(p.Id))
                        .ToDictionary(p => p.Id, p => new { p.Nombre, p.Stock, p.ImpuestoId, p.PermiteDescuento });

                    var impuestoPredeterminadoId = _context.Impuestos
                        .FirstOrDefault(i => i.EsPredeterminado && i.Activo)?.Id;

                    var itemsIniciales = detalles.Select(d =>
                    {
                        var prod = productosDict.GetValueOrDefault(d.ProductoId);
                        int? impuestoId = prod?.ImpuestoId ?? impuestoPredeterminadoId;
                        decimal tasa = impuestoId.HasValue && ImpuestosDict.ContainsKey(impuestoId.Value)
                            ? ImpuestosDict[impuestoId.Value] : 0m;

                        return new
                        {
                            productoId = d.ProductoId,
                            nombre = prod?.Nombre ?? "—",
                            precio = d.PrecioUnitario,
                            cantidad = d.Cantidad,
                            descuento = d.Descuento,
                            impuestoId = impuestoId,
                            impuesto = tasa,
                            stock = prod?.Stock ?? 0,
                            permiteDescuento = prod?.PermiteDescuento ?? true
                        };
                    }).ToList();

                    ItemsInicialesJson = JsonSerializer.Serialize(itemsIniciales);
                }
            }

            // ===== Pedido del catálogo en línea → carrito =====
            if (pedidoWebId.HasValue && !cotizacionId.HasValue)
            {
                var pw = _context.PedidosWeb.AsNoTracking().FirstOrDefault(p => p.Id == pedidoWebId.Value);
                if (pw == null || pw.Estado == "Cancelado" || (pw.Notas ?? "").Contains("Cobrado en POS"))
                {
                    TempData["Error"] = "Ese pedido web ya no está disponible para cobrar.";
                    return RedirectToPage("/PedidosWeb/Index");
                }

                var lineasWeb = new List<(int id, decimal cant)>();
                try
                {
                    using var doc = JsonDocument.Parse(pw.ItemsJson);
                    foreach (var e in doc.RootElement.EnumerateArray())
                        lineasWeb.Add((e.GetProperty("id").GetInt32(), e.GetProperty("cantidad").GetDecimal()));
                }
                catch { /* pedido ilegible: se abre vacío */ }

                var ids = lineasWeb.Select(l => l.id).Distinct().ToList();
                var prods = _context.Productos.Where(p => ids.Contains(p.Id) && p.Activo).ToDictionary(p => p.Id);
                var avisos = new List<string>();
                var itemsWeb = new List<object>();
                foreach (var l in lineasWeb)
                {
                    if (!prods.TryGetValue(l.id, out var prod)) { avisos.Add("un producto ya no existe o está inactivo"); continue; }
                    var cant = l.cant;
                    if (cant > prod.Stock)
                    {
                        avisos.Add($"«{prod.Nombre}»: pidió {cant:0.##} y hay {prod.Stock:0.##}");
                        cant = prod.Stock;
                    }
                    if (cant <= 0) continue;
                    int? impId = prod.ImpuestoId ?? ImpuestoPredeterminadoId;
                    decimal tasa = impId.HasValue && ImpuestosDict.ContainsKey(impId.Value) ? ImpuestosDict[impId.Value] : 0m;
                    itemsWeb.Add(new
                    {
                        productoId = prod.Id, nombre = prod.Nombre, precio = prod.PrecioVenta, cantidad = cant,
                        descuento = 0m, impuestoId = impId, impuesto = tasa, stock = prod.Stock,
                        permiteDescuento = prod.PermiteDescuento
                    });
                }
                ItemsInicialesJson = JsonSerializer.Serialize(itemsWeb);
                PedidoWebId = pw.Id;

                // Cliente: se busca por teléfono (solo dígitos, últimos 8)
                string Ult8(string? t) { var d = new string((t ?? "").Where(char.IsDigit).ToArray()); return d.Length > 8 ? d[^8..] : d; }
                var tel = Ult8(pw.Telefono);
                if (tel.Length >= 8)
                {
                    var cli = clientes.FirstOrDefault(c => Ult8(c.Telefono) == tel);
                    if (cli != null) { ClienteInicialId = cli.Id; ClienteInicialNombre = cli.Nombre; }
                }
                CotizacionInfo = $"Pedido web {pw.Numero} · {pw.Nombre} · {pw.Telefono}" +
                    (ClienteInicialId == null ? " · (cliente no registrado: elige uno o vende como consumidor final)" : "") +
                    (avisos.Count > 0 ? " · Revisa: " + string.Join("; ", avisos) : "");
            }

            return Page();
        }

        /// <summary>Resumen rápido del cliente para el POS: saldos, últimas compras y productos que más compra.</summary>
        public IActionResult OnGetHistorialCliente(int clienteId)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Create", "crear"))
                return new JsonResult(new { ok = false });

            var cliente = _context.Clientes.AsNoTracking().FirstOrDefault(c => c.Id == clienteId);
            if (cliente == null) return new JsonResult(new { ok = false });

            var vigentes = _context.Ventas.AsNoTracking()
                .Where(v => v.ClienteId == clienteId && v.Estado != "Anulada" && v.Estado != "Cancelada");

            var cantidadVentas = vigentes.Count();
            var totalComprado = vigentes.Sum(v => (decimal?)v.Total) ?? 0m;
            var saldoPendiente = vigentes.Where(v => v.EsCredito).Sum(v => (decimal?)v.SaldoPendiente) ?? 0m;

            var ultimas = vigentes.OrderByDescending(v => v.Fecha).Take(8)
                .Select(v => new { v.Id, v.Numero, v.Fecha, v.Total, v.EsCredito, v.SaldoPendiente })
                .ToList()
                .Select(v => new { id = v.Id, numero = v.Numero, fecha = v.Fecha.ToString("dd/MM/yyyy"), total = v.Total, saldo = v.EsCredito ? v.SaldoPendiente : 0m })
                .ToList();

            var top = (from d in _context.DetalleVentas.AsNoTracking()
                       join v in vigentes on d.VentaId equals v.Id
                       group d by d.ProductoId into g
                       select new { ProductoId = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                      .OrderByDescending(x => x.Cantidad).Take(8).ToList();
            var ids = top.Select(x => x.ProductoId).ToList();
            var nombres = _context.Productos.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Nombre);
            var productos = top.Select(x => new { id = x.ProductoId, nombre = nombres.GetValueOrDefault(x.ProductoId, "—"), cantidad = x.Cantidad }).ToList();

            return new JsonResult(new
            {
                ok = true,
                nombre = cliente.Nombre,
                cantidadVentas,
                totalComprado,
                saldoPendiente,
                saldoFavor = SaldoFavorHelper.Saldo(_context, clienteId),
                ventas = ultimas,
                productos
            });
        }

        /// <summary>Saldo a favor disponible del cliente (por devoluciones o cambios), para usarlo al cobrar.</summary>
        public IActionResult OnGetSaldoFavor(int clienteId)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Create", "crear"))
                return new JsonResult(new { saldo = 0m });
            return new JsonResult(new { saldo = SaldoFavorHelper.Saldo(_context, clienteId) });
        }

        /// <summary>
        /// Handler que llama el POS por fetch('?handler=RegistrarVenta').
        /// Solo revisa el permiso y traduce el resultado a JSON; toda la lógica
        /// de la venta vive en Services/VentaService.cs.
        /// </summary>
        // Un candado por OfflineId: si el POS reenvia la misma venta dos veces a la vez (senal inestable),
        // la segunda espera a la primera y recibe la venta ya registrada en vez de duplicarla.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _candadosOffline = new();

        public async Task<IActionResult> OnPostRegistrarVentaAsync([FromBody] VentaRequest request)
        {
            var clave = string.IsNullOrWhiteSpace(request?.OfflineId) ? null : request!.OfflineId!.Trim();
            if (clave == null) return await RegistrarVentaInternoAsync(request!);

            if (_candadosOffline.Count > 5000) _candadosOffline.Clear();
            var candado = _candadosOffline.GetOrAdd(clave, _ => new SemaphoreSlim(1, 1));
            await candado.WaitAsync();
            try { return await RegistrarVentaInternoAsync(request!); }
            finally { candado.Release(); }
        }

        private async Task<IActionResult> RegistrarVentaInternoAsync(VentaRequest request)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Create", "crear"))
                return new JsonResult(new { success = false, error = "No tienes permiso para registrar ventas" });

            // Venta hecha sin internet: si ya se sincronizó antes (se cortó la señal al recibir la
            // respuesta), se devuelve la misma venta en vez de registrarla otra vez.
            var offlineId = string.IsNullOrWhiteSpace(request?.OfflineId) ? null : request!.OfflineId!.Trim();
            if (offlineId != null && offlineId.Length > 64) offlineId = offlineId.Substring(0, 64);
            if (offlineId != null)
            {
                var previa = _context.VentasOffline.AsNoTracking().FirstOrDefault(v => v.OfflineId == offlineId);
                if (previa != null)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        numero = previa.Numero,
                        ventaId = previa.VentaId,
                        total = previa.Total,
                        duplicada = true
                    });
                }
            }

            var resultado = await _ventaService.RegistrarVentaAsync(
                request!,
                User.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            if (!resultado.Exito)
                return new JsonResult(new { success = false, error = resultado.Error });

            if (request?.PedidoWebId is int pedidoWebId)
            {
                try
                {
                    var pw = _context.PedidosWeb.FirstOrDefault(p => p.Id == pedidoWebId && p.Estado != "Cancelado");
                    if (pw != null && !(pw.Notas ?? "").Contains("Cobrado en POS"))
                    {
                        pw.Estado = "Atendido";
                        var nota = (pw.Notas == null ? "" : pw.Notas + " | ") + $"Cobrado en POS: venta {resultado.Numero}";
                        pw.Notas = nota.Length > 500 ? nota.Substring(0, 500) : nota;
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception) { _context.ChangeTracker.Clear(); }
            }

            if (offlineId != null)
            {
                try
                {
                    _context.VentasOffline.Add(new VentaOffline
                    {
                        OfflineId = offlineId,
                        VentaId = resultado.VentaId,
                        Numero = resultado.Numero,
                        Total = resultado.Total
                    });
                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    // Si no se pudo anotar, la venta igual ya quedó registrada.
                    _context.ChangeTracker.Clear();
                }
            }

            // Impresión directa en la térmica si está configurada "al cobrar" (nunca rompe la venta)
            HttpContext.RequestServices.GetService<Kirkenta.Services.ImpresoraTermicaService>()?.ImprimirVentaAuto(resultado.VentaId);

            return new JsonResult(new
            {
                success = true,
                numero = resultado.Numero,
                ventaId = resultado.VentaId,
                total = resultado.Total,
                montoRecibido = resultado.MontoRecibido,
                cambio = resultado.Cambio,
                tipo = "venta",
                esCredito = resultado.EsCredito,
                requiereEnvio = resultado.RequiereEnvio,
                envioNumero = resultado.EnvioNumero,
                movimientoRegistrado = resultado.MovimientoRegistrado,
                costoRegistrado = resultado.CostoRegistrado,
                costoTotalVendido = resultado.CostoTotalVendido,
                movimientoError = (string?)null
            });
        }
    }
}
