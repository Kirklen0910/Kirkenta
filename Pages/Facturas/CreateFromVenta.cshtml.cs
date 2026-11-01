using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Facturas
{
    public class CreateFromVentaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateFromVentaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Venta Venta { get; set; } = new();
        public Cliente? Cliente { get; set; }
        public string NumeroPreview { get; set; } = "";
        public bool FiscalActiva { get; set; }
        public string? FiscalError { get; set; }
        public string? FiscalAdvertencia { get; set; }

        public bool VentaYaFacturada { get; set; }
        public int? FacturaExistenteId { get; set; }
        public string? FacturaExistenteNumero { get; set; }

        public Pago? PagoVenta { get; set; }
        public MetodoPago? MetodoPagoVenta { get; set; }
        public bool EsEfectivo { get; set; }
        public bool EsCredito { get; set; }
        public decimal MontoRecibido { get; set; }
        public decimal Cambio { get; set; }

        public class InputModel
        {
            public int VentaId { get; set; }
            public DateTime? FechaVencimiento { get; set; }
        }

        public IActionResult OnGet(int ventaId)
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Facturas", "crear")))
            {
                TempData["Error"] = "No tienes permiso para realizar esta acción";
                return RedirectToPage("/Index");
            }

            var venta = _context.Ventas.FirstOrDefault(v => v.Id == ventaId);
            if (venta == null)
            {
                TempData["Error"] = "Venta no encontrada";
                return RedirectToPage("/Ventas/Index");
            }

            var facturaExistente = _context.Facturas.FirstOrDefault(f => f.VentaId == ventaId);
            if (facturaExistente != null)
            {
                TempData["Error"] = $"Esta venta ya fue facturada como {facturaExistente.Numero}";
                return RedirectToPage("/Facturas/Details", new { id = facturaExistente.Id });
            }

            if (venta.Estado == "Anulada")
            {
                TempData["Error"] = "No se puede facturar una venta anulada";
                return RedirectToPage("/Ventas/Details", new { id = venta.Id });
            }

            Venta = venta;
            Cliente = venta.ClienteId.HasValue
                ? _context.Clientes.FirstOrDefault(c => c.Id == venta.ClienteId.Value)
                : null;
            var fiscal = FacturacionFiscalHelper.Previsualizar(_context);
            FiscalActiva = fiscal.Activa;
            FiscalError = fiscal.Ok ? null : fiscal.Error;
            FiscalAdvertencia = fiscal.Advertencia;
            NumeroPreview = fiscal.Activa
                ? (fiscal.Numero ?? "— sin número disponible —")
                : NumeroDocumentoHelper.PreviewSiguiente(_context, "Factura");

            CargarInfoPago(venta);

            Input = new InputModel
            {
                VentaId = venta.Id,
                FechaVencimiento = DateTime.Now.AddDays(30)
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Facturas", "crear")))
            {
                TempData["Error"] = "No tienes permiso para realizar esta acción";
                return RedirectToPage("/Index");
            }

            var venta = _context.Ventas.FirstOrDefault(v => v.Id == Input.VentaId);
            if (venta == null)
            {
                TempData["Error"] = "Venta no encontrada";
                return RedirectToPage("/Ventas/Index");
            }

            if (_context.Facturas.Any(f => f.VentaId == venta.Id))
            {
                TempData["Error"] = "Esta venta ya fue facturada";
                return RedirectToPage("/Ventas/Details", new { id = venta.Id });
            }

            if (venta.Estado == "Anulada")
            {
                TempData["Error"] = "No se puede facturar una venta anulada";
                return RedirectToPage("/Ventas/Details", new { id = venta.Id });
            }

            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, venta.Fecha);
            if (!periodoOk)
            {
                TempData["Error"] = periodoError;
                return RedirectToPage("/Ventas/Details", new { id = venta.Id });
            }

            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);

            CargarInfoPago(venta);

            using var transaction = _context.Database.BeginTransaction();

            // Bloqueo de la venta: dos personas facturando la misma venta a la vez no la duplican
            if (_context.Database.IsRelational())
            {
                var ventaIdBloqueo = venta.Id;
                _context.Ventas.Where(v => v.Id == ventaIdBloqueo)
                    .ExecuteUpdate(u => u.SetProperty(v => v.Estado, v => v.Estado));
                if (_context.Facturas.AsNoTracking().Any(f => f.VentaId == ventaIdBloqueo))
                {
                    transaction.Rollback();
                    TempData["Error"] = "Esta venta ya fue facturada";
                    return RedirectToPage("/Ventas/Details", new { id = ventaIdBloqueo });
                }
            }

            try
            {
                // ===== 1. CREAR FACTURA =====
                // 🔒 FIX: Si la venta es al crédito, la factura nace EMITIDA con Saldo = Total.
                // Si la venta es al contado, nace PAGADA con Saldo = 0.
                bool ventaACredito = EsCredito || venta.EsCredito;

                var clienteExo = (venta.EsExonerada && venta.ClienteId.HasValue)
                    ? _context.Clientes.AsNoTracking().FirstOrDefault(c => c.Id == venta.ClienteId.Value)
                    : null;

                // Descuento total de la venta: el general en monto + el de cada línea (el POS manda el descuento por línea,
                // incluido el porcentaje general repartido). Sin esto la factura salía sin descuento aunque el total sí lo tenía.
                var descuentoLineas = _context.DetalleVentas.Where(d => d.VentaId == venta.Id).Sum(d => (decimal?)d.Descuento) ?? 0m;

                var factura = new Factura
                {
                    Numero = "",
                    ClienteId = venta.ClienteId ?? 1,
                    VentaId = venta.Id,
                    Fecha = DateTime.Now,
                    FechaVencimiento = venta.FechaVencimientoCredito ?? Input.FechaVencimiento,
                    Subtotal = venta.Subtotal,
                    Descuento = venta.Descuento + descuentoLineas,
                    Impuestos = venta.Impuestos,
                    Total = venta.Total,
                    // Si ya se cobró algo (o hubo devoluciones) antes de facturar, la factura nace con el saldo real
                    Saldo = ventaACredito ? Math.Max(venta.SaldoPendiente, 0) : 0,
                    Estado = !ventaACredito || venta.SaldoPendiente <= 0 ? "Pagada"
                           : venta.SaldoPendiente < venta.Total ? "PagadaParcial"
                           : "Emitida",
                    Notas = $"Generada desde venta {venta.Numero}",
                    UsuarioCreoId = currentUser?.Id,
                    RequiereEnvio = venta.RequiereEnvio,
                    MontoEnvio = venta.MontoEnvio,
                    EsExonerada = venta.EsExonerada,
                    ExoneradoConstancia = clienteExo?.ExoneradoConstancia,
                    ExoneradoOrdenCompraExenta = clienteExo?.ExoneradoOrdenCompraExenta,
                    ExoneradoRegistroSag = clienteExo?.ExoneradoRegistroSag
                };

                // Numeración: fiscal (CAI/rango) si está activa; si no, serie interna.
                var (numeroAsignado, errorNumero, _) = FacturacionFiscalHelper.AsignarNumero(_context, factura);
                if (numeroAsignado == null)
                {
                    transaction.Rollback();
                    TempData["Error"] = errorNumero ?? "No se pudo asignar el número de factura.";
                    return RedirectToPage("/Facturas/CreateFromVenta", new { ventaId = venta.Id });
                }

                _context.Facturas.Add(factura);
                _context.SaveChanges();

                // ===== 2. COPIAR ITEMS =====
                var items = _context.DetalleVentas
                    .Where(d => d.VentaId == venta.Id)
                    .ToList();

                foreach (var item in items)
                {
                    _context.DetalleFacturas.Add(new DetalleFactura
                    {
                        FacturaId = factura.Id,
                        ProductoId = item.ProductoId,
                        Descripcion = item.Descripcion,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.PrecioUnitario,
                        Descuento = item.Descuento,
                        ImpuestoPorcentaje = item.ImpuestoPorcentaje,
                        Subtotal = item.Subtotal,
                        Total = item.Total
                    });
                }

                // ===== 3. LIGAR PAGO EXISTENTE A LA FACTURA =====
                if (PagoVenta != null)
                {
                    PagoVenta.FacturaId = factura.Id;
                }
                // También los cobros posteriores de una venta a crédito que aún no tenían factura
                foreach (var pagoSinFactura in _context.Pagos.Where(p => p.VentaId == venta.Id && p.FacturaId == null).ToList())
                {
                    pagoSinFactura.FacturaId = factura.Id;
                }

                // ===== 4. LIGAR ENVÍO EXISTENTE A LA FACTURA =====
                if (venta.RequiereEnvio)
                {
                    var envio = _context.Envios.FirstOrDefault(e => e.VentaId == venta.Id && e.FacturaId == null);
                    if (envio != null)
                    {
                        envio.FacturaId = factura.Id;
                    }
                }

                _context.SaveChanges();

                // ============================================================
                // 5. MOVIMIENTOS FINANCIEROS
                // ============================================================
                if (!venta.MovimientoRegistrado)
                {
                    if (ventaACredito)
                    {
                        // 🔒 OPCIÓN A: la venta a crédito NO genera ingreso al facturar.
                        // El ingreso se registra SOLO al cobrar (Finanzas > Cobros).
                    }
                    else
                    {
                        var apertura = _context.AperturasCaja.FirstOrDefault(a => a.Activa);
                        int? cuentaId = apertura?.CuentaId;

                        if (!cuentaId.HasValue)
                        {
                            var cuentaCaja = _context.CuentasFinancieras
                                .Where(c => c.Tipo == "Caja" && c.Activa)
                                .OrderBy(c => c.Id)
                                .FirstOrDefault();
                            cuentaId = cuentaCaja?.Id;
                        }

                        if (!cuentaId.HasValue)
                        {
                            transaction.Rollback();
                            TempData["Error"] = "No hay una cuenta de caja activa para registrar los movimientos financieros";
                            return RedirectToPage("/Facturas/CreateFromVenta", new { ventaId = venta.Id });
                        }

                        decimal montoIngreso = EsEfectivo ? MontoRecibido : venta.Total;
                        decimal montoCambio = EsEfectivo ? Cambio : 0;

                        var (movIngreso, errorIngreso) = MovimientoAutomaticoHelper.RegistrarIngresoVenta(
                            _context, venta.Id, venta.Numero,
                            monto: montoIngreso,
                            usuarioId: currentUser?.Id ?? 0,
                            formaPago: MetodoPagoVenta?.Nombre,
                            cuentaId: cuentaId);

                        if (movIngreso == null)
                        {
                            transaction.Rollback();
                            TempData["Error"] = $"No se pudo registrar el ingreso en Finanzas. {errorIngreso ?? "Revisa la configuración de cuentas y categorías."}";
                            return RedirectToPage("/Facturas/CreateFromVenta", new { ventaId = venta.Id });
                        }

                        venta.MovimientoIngresoId = movIngreso.Id;

                        if (montoCambio > 0)
                        {
                            var (movCambio, errorCambio) = MovimientoAutomaticoHelper.RegistrarEgresoCambio(
                                _context, venta.Id, venta.Numero,
                                montoCambio: montoCambio,
                                usuarioId: currentUser?.Id ?? 0,
                                formaPago: "Efectivo",
                                cuentaId: cuentaId,
                                origen: "Venta");

                            if (movCambio == null)
                            {
                                transaction.Rollback();
                                TempData["Error"] = $"No se pudo registrar el egreso por cambio en Finanzas. {errorCambio ?? "Los movimientos fueron revertidos."}";
                                return RedirectToPage("/Facturas/CreateFromVenta", new { ventaId = venta.Id });
                            }

                            venta.MovimientoCambioId = movCambio.Id;
                        }

                        venta.MovimientoRegistrado = true;
                        venta.FechaMovimiento = DateTime.Now;
                    }
                }

                // ============================================================
                // 6. COGS: registrar si la venta aún no lo tiene
                // ============================================================
                if (!venta.CostoRegistrado && venta.CostoTotalVendido > 0)
                {
                    var apertura = _context.AperturasCaja.FirstOrDefault(a => a.Activa);
                    int? cuentaCogsId = apertura?.CuentaId;

                    if (!cuentaCogsId.HasValue)
                    {
                        var cuentaCaja = _context.CuentasFinancieras
                            .Where(c => c.Tipo == "Caja" && c.Activa)
                            .OrderBy(c => c.Id)
                            .FirstOrDefault();
                        cuentaCogsId = cuentaCaja?.Id;
                    }

                    if (cuentaCogsId.HasValue)
                    {
                        var (movCosto, errorCosto) = MovimientoAutomaticoHelper.RegistrarEgresoCostoVenta(
                            _context,
                            venta.Id,
                            venta.Numero,
                            venta.CostoTotalVendido,
                            currentUser?.Id ?? 0,
                            cuentaCogsId);

                        if (movCosto != null)
                        {
                            venta.MovimientoCostoId = movCosto.Id;
                            venta.CostoRegistrado = true;
                        }
                        else
                        {
                            Console.WriteLine($"[Facturas] Advertencia: no se pudo registrar el COGS. {errorCosto}");
                        }
                    }
                }

                _context.SaveChanges();
                transaction.Commit();

                ActividadHelper.Registrar(
                    _context,
                    currentUser?.Id ?? 0,
                    "Facturar venta POS",
                    $"Factura {factura.Numero} generada desde venta {venta.Numero}. COGS: L. {venta.CostoTotalVendido:N2}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["Success"] = ventaACredito
                    ? $"Factura {factura.Numero} generada (a crédito). Saldo pendiente: L. {factura.Saldo:N2}. Registra el cobro en Finanzas > Cobros."
                    : $"Factura {factura.Numero} generada correctamente.";

                // Impresión directa en la térmica si está configurada "al emitir la factura" (nunca rompe la facturación)
                HttpContext.RequestServices.GetService<Kirkenta.Services.ImpresoraTermicaService>()?.ImprimirFacturaAuto(factura.Id);

                return RedirectToPage("/Facturas/Details", new { id = factura.Id });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"ERROR al facturar venta {venta.Id}: {ex.Message}");
                TempData["Error"] = $"Error al facturar la venta: {ex.Message}";
                return RedirectToPage("/Ventas/Details", new { id = venta.Id });
            }
        }

        private void CargarInfoPago(Venta venta)
        {
            PagoVenta = _context.Pagos
                .Where(p => p.VentaId == venta.Id)
                .OrderByDescending(p => p.Id)
                .FirstOrDefault();

            if (PagoVenta != null)
            {
                MetodoPagoVenta = _context.MetodosPago.FirstOrDefault(m => m.Id == PagoVenta.MetodoPagoId);

                var tipo = (MetodoPagoVenta?.Tipo ?? "").Trim().ToLower();
                EsEfectivo = tipo.Contains("efectivo") || tipo.Contains("cash") || tipo.Contains("contado") || tipo == "";
                EsCredito = tipo.Contains("credito") || venta.EsCredito;

                MontoRecibido = PagoVenta.MontoRecibido ?? venta.Total;
                Cambio = PagoVenta.Cambio ?? 0;
            }
            else
            {
                EsEfectivo = false;
                EsCredito = venta.EsCredito;
                MontoRecibido = venta.Total;
                Cambio = 0;
            }
        }
    }
}