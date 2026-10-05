using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Helpers.Logistica;
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
        public List<ZonaEnvio> ZonasEnvio { get; set; } = new();
        public Dictionary<int, decimal> ImpuestosDict { get; set; } = new();
        public string ClientesJson { get; set; } = "[]";
        public string ItemsInicialesJson { get; set; } = "[]";
        public string? CotizacionInfo { get; set; }
        public int? CotizacionId { get; set; }
        public int? ClienteInicialId { get; set; }
        public string ClienteInicialNombre { get; set; } = "";

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

            // VALIDAR APERTURA DE CAJA
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
            ZonasEnvio = _context.ZonasEnvio.Where(z => z.Activa).OrderBy(z => z.Orden).ThenBy(z => z.Nombre).ToList();
            ImpuestosDict = _context.Impuestos.Where(i => i.Activo).ToDictionary(i => i.Id, i => i.Porcentaje);

            var clientes = _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
            var clientesList = clientes.Select(c => new
            {
                id = c.Id,
                nombre = c.Nombre,
                rtn = c.RTN ?? "",
                codigo = c.Codigo ?? "",
                direccion = c.Direccion ?? "",
                telefono = c.Telefono ?? ""
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
                        .ToDictionary(p => p.Id, p => new { p.Nombre, p.Stock, p.ImpuestoId });

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
                            stock = prod?.Stock ?? 0
                        };
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

            // ===== LOGÍSTICA =====
            public bool RequiereEnvio { get; set; } = false;
            public decimal MontoEnvio { get; set; } = 0;
            public string? EnvioDireccion { get; set; }
            public string? EnvioReferencia { get; set; }
            public string? EnvioContactoNombre { get; set; }
            public string? EnvioContactoTelefono { get; set; }
            public string? EnvioCiudad { get; set; }
            public int? EnvioZonaId { get; set; }
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

                // ===== VALIDACIÓN: período contable cerrado =====
                var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, DateTime.Today);
                if (!periodoOk)
                    return new JsonResult(new { success = false, error = periodoError });

                if (request == null || request.Items == null || request.Items.Count == 0)
                    return new JsonResult(new { success = false, error = "Carrito vacío" });

                // ===== VALIDAR ENVÍO =====
                if (request.RequiereEnvio)
                {
                    if (string.IsNullOrWhiteSpace(request.EnvioDireccion))
                        return new JsonResult(new { success = false, error = "Debes indicar la dirección de envío" });

                    if (string.IsNullOrWhiteSpace(request.EnvioContactoNombre))
                        return new JsonResult(new { success = false, error = "Debes indicar quién recibe el envío" });

                    if (string.IsNullOrWhiteSpace(request.EnvioContactoTelefono))
                        return new JsonResult(new { success = false, error = "Debes indicar el teléfono de contacto del envío" });

                    if (request.MontoEnvio < 0)
                        return new JsonResult(new { success = false, error = "El monto de envío no puede ser negativo" });
                }

                // ===== VALIDAR Y RECALCULAR IMPUESTOS CON ISVHelper =====
                var productoIds = request.Items.Select(i => i.ProductoId).Distinct().ToList();
                var productosDict = _context.Productos
                    .Where(p => productoIds.Contains(p.Id))
                    .ToDictionary(p => p.Id, p => new { p.Nombre, p.ImpuestoId, p.Stock });

                var impuestosActivos = _context.Impuestos
                    .Where(i => i.Activo)
                    .ToDictionary(i => i.Id, i => i.Porcentaje);

                var impuestoPredeterminadoId = _context.Impuestos
                    .FirstOrDefault(i => i.EsPredeterminado && i.Activo)?.Id;

                var itemsValidados = new List<ItemParaISV>();
                var itemsConTasa = new List<(ItemRequest original, int productoId, decimal tasa)>();

                foreach (var item in request.Items)
                {
                    if (!productosDict.ContainsKey(item.ProductoId))
                        return new JsonResult(new { success = false, error = $"Producto {item.ProductoId} no encontrado o inactivo" });

                    if (item.Cantidad <= 0)
                        return new JsonResult(new { success = false, error = "Cantidad inválida" });

                    var prod = productosDict[item.ProductoId];
                    if (prod.Stock < item.Cantidad)
                        return new JsonResult(new { success = false, error = $"Stock insuficiente para '{prod.Nombre}'. Disponible: {prod.Stock}" });

                    int? impuestoId = prod.ImpuestoId ?? impuestoPredeterminadoId;
                    decimal tasaCorrecta = impuestoId.HasValue && impuestosActivos.ContainsKey(impuestoId.Value)
                        ? impuestosActivos[impuestoId.Value] : 0m;

                    itemsConTasa.Add((item, item.ProductoId, tasaCorrecta));

                    itemsValidados.Add(new ItemParaISV
                    {
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.PrecioUnitario,
                        Descuento = item.Descuento,
                        ImpuestoId = impuestoId
                    });
                }

                // ===== CALCULAR CON ISVHelper (incluye envío) =====
                decimal montoEnvioCalculo = request.RequiereEnvio ? request.MontoEnvio : 0;
                var isvResult = ISVHelper.Calcular(_context, itemsValidados, request.Descuento, montoEnvioCalculo);

                // ===== CREAR DETALLES CON TASA CORRECTA =====
                decimal subtotalItems = 0, impuestosTotal = 0, total = 0;
                var detalles = new List<DetalleVenta>();

                foreach (var (itemOrig, productoId, tasa) in itemsConTasa)
                {
                    var st = itemOrig.Cantidad * itemOrig.PrecioUnitario;
                    var stConDesc = st - itemOrig.Descuento;
                    var iv = Math.Round(stConDesc * (tasa / 100m), 2);
                    var tot = stConDesc + iv;

                    subtotalItems += st;
                    impuestosTotal += iv;
                    total += tot;

                    detalles.Add(new DetalleVenta
                    {
                        ProductoId = productoId,
                        Cantidad = itemOrig.Cantidad,
                        PrecioUnitario = itemOrig.PrecioUnitario,
                        Descuento = itemOrig.Descuento,
                        ImpuestoPorcentaje = tasa,
                        Subtotal = stConDesc,
                        Total = tot
                    });
                }

                // Ajustar totales finales desde ISVHelper (ya incluye envío)
                subtotalItems += montoEnvioCalculo;
                impuestosTotal = isvResult.ISVTotal;
                total = isvResult.Total;

                // ===== CREAR VENTA =====
                var numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "Venta");

                var venta = new Venta
                {
                    Numero = numero,
                    ClienteId = request.ClienteId,
                    CotizacionId = request.CotizacionId,
                    Fecha = DateTime.Now,
                    Subtotal = subtotalItems,
                    Descuento = request.Descuento,
                    Impuestos = impuestosTotal,
                    Total = total,
                    Estado = "Completada",
                    UsuarioCreoId = currentUser.Id,
                    RequiereEnvio = request.RequiereEnvio,
                    MontoEnvio = montoEnvioCalculo
                };

                _context.Ventas.Add(venta);
                _context.SaveChanges();

                // Guardar detalles + descontar stock
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

                _context.SaveChanges();  // 🔑 Guardamos el pago ANTES de crear el envío

                // ===== CREAR ENVÍO SI APLICA =====
                // REGLA DE NEGOCIO: solo se crea el envío si hay pago registrado (ya se cobró).
                // El pago se registró arriba, así que validamos que exista en BD.
                Envio? envio = null;
                if (request.RequiereEnvio)
                {
                    // Doble check: verificar que el pago esté en BD antes de crear el envío
                    var pagoExiste = _context.Pagos.Any(p => p.VentaId == venta.Id);

                    if (!pagoExiste)
                    {
                        Console.WriteLine($"[POS] ⚠️ ADVERTENCIA: Se intentó crear envío para venta {venta.Numero} sin pago registrado. Envío NO creado.");
                    }
                    else
                    {
                        envio = EnvioHelper.CrearParaVenta(
                            _context,
                            venta,
                            request.EnvioDireccion!,
                            request.EnvioReferencia,
                            request.EnvioContactoNombre!,
                            request.EnvioContactoTelefono!,
                            request.EnvioCiudad,
                            request.EnvioZonaId,
                            montoEnvioCalculo,
                            currentUser.Id
                        );
                    }
                }

                // ===== REGISTRO AUTOMÁTICO EN FINANZAS =====
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
                            cuentaId: apertura.CuentaId
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
                            Subtotal = subtotalItems,
                            Descuento = request.Descuento,
                            Impuestos = impuestosTotal,
                            Total = total,
                            Saldo = 0,
                            Estado = "Pagada",
                            Notas = $"Facturada desde cotización {cot.Numero}",
                            UsuarioCreoId = currentUser.Id,
                            RequiereEnvio = request.RequiereEnvio,
                            MontoEnvio = montoEnvioCalculo
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

                        // Trasladar el envío a la factura también
                        if (envio != null)
                        {
                            envio.FacturaId = factura.Id;
                        }

                        _context.SaveChanges();

                        ActividadHelper.Registrar(
                            _context,
                            currentUser.Id,
                            "Cotización facturada",
                            $"Facturó la cotización {cot.Numero} como {factura.Numero} por L. {total:N2}" +
                            (request.RequiereEnvio ? $" (con envío de L. {montoEnvioCalculo:N2})" : "") +
                            (movimientoRegistrado ? " (movimiento financiero registrado)" : ""),
                            HttpContext.Connection.RemoteIpAddress?.ToString());

                        return new JsonResult(new
                        {
                            success = true,
                            numero = factura.Numero,
                            total = total,
                            tipo = "factura",
                            requiereEnvio = request.RequiereEnvio,
                            envioNumero = envio?.Numero,
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
                    (request.RequiereEnvio ? $" (envío L. {montoEnvioCalculo:N2})" : "") +
                    (movimientoRegistrado ? " (movimiento financiero registrado)" : ""),
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return new JsonResult(new
                {
                    success = true,
                    numero = venta.Numero,
                    total = venta.Total,
                    tipo = "venta",
                    requiereEnvio = request.RequiereEnvio,
                    envioNumero = envio?.Numero,
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