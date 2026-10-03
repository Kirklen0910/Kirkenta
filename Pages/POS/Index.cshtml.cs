using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Kirkenta.Pages.POS
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Producto> Productos { get; set; } = new();
        public List<Categoria> Categorias { get; set; } = new();
        public List<MetodoPago> MetodosPago { get; set; } = new();
        public Dictionary<int, decimal> ImpuestosDict { get; set; } = new();
        public string ClientesJson { get; set; } = "[]";
        public string ItemsInicialesJson { get; set; } = "[]";
        public string? CotizacionInfo { get; set; }
        public int? CotizacionId { get; set; }
        public int? ClienteInicialId { get; set; }
        public string ClienteInicialNombre { get; set; } = "";

        // ⬇️ NUEVO: información de la caja abierta
        public bool HayApertura { get; set; }
        public string CuentaCajaNombre { get; set; } = "";
        public decimal SaldoApertura { get; set; }
        public int CuentaCajaId { get; set; }

        public IActionResult OnGet(int? cotizacionId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Create", "crear"))
            {
                TempData["Error"] = "No tienes permiso para usar el POS";
                return RedirectToPage("/Index");
            }

            // ⬇️ VALIDAR APERTURA DE CAJA
            var apertura = _context.AperturasCaja
                .FirstOrDefault(a => a.Activa);

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
            ImpuestosDict = _context.Impuestos.ToDictionary(i => i.Id, i => i.Porcentaje);

            var clientes = _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
            var clientesList = clientes.Select(c => new
            {
                id = c.Id,
                nombre = c.Nombre,
                rtn = c.RTN ?? "",
                codigo = c.Codigo ?? ""
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
                    var productos = _context.Productos.ToList();

                    var itemsIniciales = detalles.Select(d => new
                    {
                        productoId = d.ProductoId,
                        nombre = productos.FirstOrDefault(p => p.Id == d.ProductoId)?.Nombre ?? "—",
                        precio = d.PrecioUnitario,
                        cantidad = d.Cantidad,
                        descuento = d.Descuento,
                        impuesto = d.ImpuestoPorcentaje,
                        stock = productos.FirstOrDefault(p => p.Id == d.ProductoId)?.Stock ?? 0
                    }).ToList();

                    ItemsInicialesJson = JsonSerializer.Serialize(itemsIniciales);
                }
            }

            return Page();
        }

        public class VentaRequest
        {
            public int? ClienteId { get; set; }
            public int MetodoPagoId { get; set; }
            public decimal Monto { get; set; }
            public string? Referencia { get; set; }
            public decimal Descuento { get; set; }
            public int? CotizacionId { get; set; }
            public bool RegistrarEnFinanzas { get; set; } = true;
            public List<ItemRequest> Items { get; set; } = new();
        }

        public class ItemRequest
        {
            public int ProductoId { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal ImpuestoPorcentaje { get; set; }
            public decimal Descuento { get; set; }
        }

        public IActionResult OnPostRegistrarVenta([FromBody] VentaRequest request)
        {
            try
            {
                var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
                if (currentUser == null)
                    return new JsonResult(new { success = false, error = "Sesión no válida" });

                // Verificar apertura activa
                var apertura = _context.AperturasCaja.FirstOrDefault(a => a.Activa);
                if (apertura == null)
                    return new JsonResult(new { success = false, error = "No hay caja abierta. Contacta al administrador." });

                if (request == null || request.Items == null || request.Items.Count == 0)
                    return new JsonResult(new { success = false, error = "Carrito vacío" });

                decimal subtotal = 0, impuestos = 0, total = 0;
                var detalles = new List<DetalleVenta>();

                foreach (var item in request.Items)
                {
                    var st = item.Cantidad * item.PrecioUnitario;
                    var stConDesc = st - item.Descuento;
                    var iv = stConDesc * (item.ImpuestoPorcentaje / 100);
                    var tot = stConDesc + iv;

                    subtotal += st;
                    impuestos += iv;
                    total += tot;

                    detalles.Add(new DetalleVenta
                    {
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.PrecioUnitario,
                        Descuento = item.Descuento,
                        ImpuestoPorcentaje = item.ImpuestoPorcentaje,
                        Subtotal = stConDesc,
                        Total = tot
                    });
                }

                total -= request.Descuento;
                if (total < 0) total = 0;

                var numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "Venta");

                var venta = new Venta
                {
                    Numero = numero,
                    ClienteId = request.ClienteId,
                    CotizacionId = request.CotizacionId,
                    Fecha = DateTime.Now,
                    Subtotal = subtotal,
                    Descuento = request.Descuento,
                    Impuestos = impuestos,
                    Total = total,
                    Estado = "Completada",
                    UsuarioCreoId = currentUser.Id
                };

                _context.Ventas.Add(venta);
                _context.SaveChanges();

                foreach (var d in detalles)
                {
                    d.VentaId = venta.Id;
                    _context.DetalleVentas.Add(d);

                    var prod = _context.Productos.FirstOrDefault(p => p.Id == d.ProductoId);
                    if (prod != null)
                    {
                        prod.Stock -= d.Cantidad;
                    }
                }

                int clienteIdPago = request.ClienteId ?? 1;
                var pago = new Pago
                {
                    VentaId = venta.Id,
                    ClienteId = clienteIdPago,
                    Fecha = DateTime.Now,
                    Monto = total,
                    MetodoPagoId = request.MetodoPagoId,
                    Referencia = request.Referencia,
                    UsuarioCreoId = currentUser.Id
                };
                _context.Pagos.Add(pago);

                // Registro automático en finanzas con la cuenta de la apertura
                bool movimientoRegistrado = false;
                string? movimientoError = null;

                if (request.RegistrarEnFinanzas)
                {
                    try
                    {
                        var mov = MovimientoAutomaticoHelper.RegistrarIngresoVenta(
                            _context,
                            venta.Id,
                            venta.Numero,
                            total,
                            currentUser.Id,
                            formaPago: request.MetodoPagoId > 0
                                ? _context.MetodosPago.FirstOrDefault(m => m.Id == request.MetodoPagoId)?.Nombre
                                : "Efectivo",
                            cuentaId: apertura.CuentaId  // ⬅️ Usa la cuenta de la apertura
                        );

                        if (mov != null)
                        {
                            movimientoRegistrado = true;
                        }
                        else
                        {
                            movimientoError = "No se pudo registrar el movimiento financiero (revisa la configuración de cuentas y categorías)";
                        }
                    }
                    catch (Exception ex)
                    {
                        movimientoError = ex.Message;
                        Console.WriteLine($"[POS] Error al registrar movimiento: {ex.Message}");
                    }
                }

                if (request.CotizacionId.HasValue)
                {
                    var cot = _context.Cotizaciones.FirstOrDefault(c => c.Id == request.CotizacionId.Value);
                    if (cot != null)
                    {
                        var numeroFactura = NumeroDocumentoHelper.GenerarSiguiente(_context, "Factura");

                        var factura = new Factura
                        {
                            Numero = numeroFactura,
                            ClienteId = cot.ClienteId,
                            CotizacionId = cot.Id,
                            VentaId = venta.Id,
                            Fecha = DateTime.Now,
                            FechaVencimiento = DateTime.Now.AddDays(30),
                            Subtotal = subtotal,
                            Descuento = request.Descuento,
                            Impuestos = impuestos,
                            Total = total,
                            Saldo = 0,
                            Estado = "Pagada",
                            Notas = $"Facturada desde cotización {cot.Numero}",
                            UsuarioCreoId = currentUser.Id
                        };

                        _context.Facturas.Add(factura);
                        _context.SaveChanges();

                        foreach (var d in detalles)
                        {
                            _context.DetalleFacturas.Add(new DetalleFactura
                            {
                                FacturaId = factura.Id,
                                ProductoId = d.ProductoId,
                                Cantidad = d.Cantidad,
                                PrecioUnitario = d.PrecioUnitario,
                                Descuento = d.Descuento,
                                ImpuestoPorcentaje = d.ImpuestoPorcentaje,
                                Subtotal = d.Subtotal,
                                Total = d.Total
                            });
                        }

                        pago.FacturaId = factura.Id;

                        cot.Estado = "Convertida";
                        cot.FacturaId = factura.Id;
                        cot.FechaConversion = DateTime.Now;

                        _context.SaveChanges();

                        ActividadHelper.Registrar(
                            _context,
                            currentUser.Id,
                            "Cotización facturada",
                            $"Facturó la cotización {cot.Numero} como {factura.Numero} por L. {total:N2}" +
                            (movimientoRegistrado ? " (movimiento financiero registrado)" : ""),
                            HttpContext.Connection.RemoteIpAddress?.ToString());

                        return new JsonResult(new
                        {
                            success = true,
                            numero = factura.Numero,
                            total = total,
                            tipo = "factura",
                            movimientoRegistrado = movimientoRegistrado,
                            movimientoError = movimientoError
                        });
                    }
                }

                _context.SaveChanges();

                ActividadHelper.Registrar(
                    _context,
                    currentUser.Id,
                    "Venta POS",
                    $"Registró la venta {venta.Numero} por L. {venta.Total:N2}" +
                    (movimientoRegistrado ? " (movimiento financiero registrado)" : ""),
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return new JsonResult(new
                {
                    success = true,
                    numero = venta.Numero,
                    total = venta.Total,
                    tipo = "venta",
                    movimientoRegistrado = movimientoRegistrado,
                    movimientoError = movimientoError
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR POS: {ex.Message}");
                Console.WriteLine($"STACK: {ex.StackTrace}");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }
    }
}