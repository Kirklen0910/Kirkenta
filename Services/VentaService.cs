using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Helpers.Logistica;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Services
{
    public interface IVentaService
    {
        /// <summary>
        /// Registra una venta del POS completa: valida, calcula impuestos y costo (COGS),
        /// descuenta stock, crea pago, envío y movimientos financieros, todo en una transacción.
        /// Nunca lanza excepciones: cualquier problema vuelve en ResultadoVenta.
        /// </summary>
        /// <param name="username">Usuario autenticado (User.Identity.Name).</param>
        /// <param name="ip">IP del cliente, solo para el registro de actividad.</param>
        Task<ResultadoVenta> RegistrarVentaAsync(VentaRequest request, string? username, string? ip);
    }

    public class VentaService : IVentaService
    {
        private const int MaxReintentosConcurrencia = 3;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<VentaService> _logger;

        public VentaService(ApplicationDbContext context, ILogger<VentaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoVenta> RegistrarVentaAsync(VentaRequest request, string? username, string? ip)
        {
            try
            {
                var usuarioId = await _context.Usuarios
                    .AsNoTracking()
                    .Where(u => u.Username == username)
                    .Select(u => (int?)u.Id)
                    .FirstOrDefaultAsync();

                if (usuarioId == null)
                    return ResultadoVenta.Falla("Sesión no válida");

                var errorSolicitud = await ValidarSolicitudAsync(request);
                if (errorSolicitud != null)
                    return ResultadoVenta.Falla(errorSolicitud);

                for (var intento = 1; intento <= MaxReintentosConcurrencia; intento++)
                {
                    try
                    {
                        return await IntentarRegistrarAsync(request, usuarioId.Value, ip);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // Otro cajero modificó el stock al mismo tiempo: se descarta todo lo
                        // cargado y se vuelve a intentar leyendo los datos frescos.
                        _context.ChangeTracker.Clear();

                        if (intento >= MaxReintentosConcurrencia)
                        {
                            _logger.LogWarning("[POS] Venta rechazada por concurrencia tras {Intentos} intentos.", intento);
                            return ResultadoVenta.Falla(
                                "El stock de uno o más productos cambió mientras procesábamos tu venta. " +
                                "Vuelve a intentarlo; probablemente ya no hay suficientes unidades.");
                        }

                        await Task.Delay(50 * intento);
                    }
                }

                return ResultadoVenta.Falla("No se pudo completar la venta por conflictos de concurrencia. Intenta de nuevo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POS] Error inesperado al registrar la venta.");
                return ResultadoVenta.Falla(
                    "Ocurrió un error inesperado al registrar la venta. No se guardó nada. " +
                    "Intenta de nuevo; si se repite, avisa al administrador (el detalle quedó en el log del sistema).");
            }
        }

        // ============================================================
        // VALIDACIONES PREVIAS (no tocan datos)
        // ============================================================
        private async Task<string?> ValidarSolicitudAsync(VentaRequest? request)
        {
            var hayCaja = await _context.AperturasCaja.AsNoTracking().AnyAsync(a => a.Activa);
            if (!hayCaja)
                return "No hay caja abierta. Contacta al administrador.";

            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, DateTime.Today);
            if (!periodoOk)
                return periodoError ?? "El período contable de hoy está cerrado.";

            if (request == null || request.Items == null || request.Items.Count == 0)
                return "Carrito vacío";

            if (request.Descuento < 0)
                return "El descuento no puede ser negativo";
            if (request.DescuentoGeneralPorcentaje < 0 || request.DescuentoGeneralPorcentaje > 100)
                return "El descuento general debe estar entre 0% y 100%";
            if (request.DescuentoGeneralPorcentaje > 0 && request.Descuento > 0)
                return "Usa el descuento en porcentaje o en monto, no los dos a la vez";

            foreach (var item in request.Items)
            {
                if (item.Cantidad <= 0)
                    return "Cantidad inválida";
                if (item.PrecioUnitario < 0)
                    return "El precio de un producto no puede ser negativo";
                if (item.Descuento < 0)
                    return "El descuento de un producto no puede ser negativo";
            }

            if (request.RequiereEnvio)
            {
                if (string.IsNullOrWhiteSpace(request.EnvioDireccion))
                    return "Debes indicar la dirección de envío";
                if (string.IsNullOrWhiteSpace(request.EnvioContactoNombre))
                    return "Debes indicar quién recibe el envío";
                if (string.IsNullOrWhiteSpace(request.EnvioContactoTelefono))
                    return "Debes indicar el teléfono de contacto del envío";
                if (request.MontoEnvio < 0)
                    return "El monto de envío no puede ser negativo";
            }

            return null;
        }

        // ============================================================
        // UN INTENTO DE REGISTRO (si hay conflicto de concurrencia, lanza
        // DbUpdateConcurrencyException y RegistrarVentaAsync lo reintenta)
        // ============================================================
        /// <summary>Valida usuario y contraseña del supervisor. Devuelve su usuario si puede autorizar, o null.</summary>
        private async Task<string?> ValidarAutorizadorAsync(VentaRequest r)
        {
            var nombre = (r.AutorizadorUsuario ?? "").Trim();
            if (nombre.Length == 0 || string.IsNullOrEmpty(r.AutorizadorClave)) return null;

            var u = await _context.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Username == nombre && x.Activo);
            if (u == null) return null;

            bool ok;
            try { ok = BCrypt.Net.BCrypt.Verify(r.AutorizadorClave, u.PasswordHash); } catch { ok = false; }
            if (!ok) return null;

            // Administrador, o un rol con permiso de eliminar ventas (nivel supervisor)
            if (u.Rol == "Admin" || PermisoHelper.UsuarioTienePermiso(_context, u.Username, "Ventas", "Delete", "eliminar"))
                return u.Username;
            return null;
        }

        private async Task<ResultadoVenta> IntentarRegistrarAsync(VentaRequest request, int usuarioId, string? ip)
        {
            var apertura = await _context.AperturasCaja.AsNoTracking().FirstOrDefaultAsync(a => a.Activa);
            if (apertura == null)
                return ResultadoVenta.Falla("No hay caja abierta. Contacta al administrador.");

            var productoIds = request.Items.Select(i => i.ProductoId).Distinct().ToList();

            var productosDict = await _context.Productos
                .Where(p => productoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            var impuestosActivos = await _context.Impuestos
                .Where(i => i.Activo)
                .ToDictionaryAsync(i => i.Id, i => i.Porcentaje);

            var impuestoPredeterminadoId = (await _context.Impuestos
                .FirstOrDefaultAsync(i => i.EsPredeterminado && i.Activo))?.Id;

            // Cliente exonerado de ISV: todas las líneas (y el envío) van a 0%
            var clienteExonerado = request.ClienteId.HasValue
                ? await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ClienteId.Value && c.EsExonerado)
                : null;
            bool exonerada = clienteExonerado != null;

            // Si el mismo producto viene en dos líneas del carrito, el stock se valida
            // con la suma de ambas (antes cada línea se validaba por separado y se podía
            // vender más de lo disponible).
            var cantidadPorProducto = request.Items
                .GroupBy(i => i.ProductoId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Cantidad));

            // Precios que la cotización fijó para cada producto (si la venta viene de una).
            // El POS no deja editar precios, así que solo se aceptan: el precio de lista
            // actual del producto o el precio acordado en la cotización.
            var preciosCotizados = new Dictionary<int, List<decimal>>();
            if (request.CotizacionId.HasValue)
            {
                var cot = await _context.Cotizaciones.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == request.CotizacionId.Value);
                if (cot == null)
                    return ResultadoVenta.Falla("La cotización indicada no existe.");
                if (cot.Estado != "Aceptada")
                    return ResultadoVenta.Falla($"La cotización {cot.Numero} no está aceptada (estado: {cot.Estado}).");

                var detCot = await _context.DetalleCotizaciones.AsNoTracking()
                    .Where(d => d.CotizacionId == cot.Id)
                    .ToListAsync();
                preciosCotizados = detCot
                    .GroupBy(d => d.ProductoId)
                    .ToDictionary(g => g.Key, g => g.Select(d => d.PrecioUnitario).ToList());
            }

            var itemsValidados = new List<ItemParaISV>();
            string? autorizadoPor = null; // supervisor que autorizó vender sin existencia
            var itemsConTasa = new List<(ItemVentaRequest original, int productoId, decimal tasa, decimal costoUnitario)>();

            var descuentosFinales = new List<decimal>(); // descuento total de cada línea (propio + parte del general)
            var pctGeneral = Math.Round(request.DescuentoGeneralPorcentaje, 2);

            foreach (var item in request.Items)
            {
                if (!productosDict.TryGetValue(item.ProductoId, out var prod))
                    return ResultadoVenta.Falla($"Producto {item.ProductoId} no encontrado o inactivo");

                if (!prod.Activo)
                    return ResultadoVenta.Falla($"El producto '{prod.Nombre}' está inactivo y no se puede vender.");

                // 🔒 El precio lo manda el navegador: se compara contra el del servidor
                bool precioValido = Math.Abs(item.PrecioUnitario - prod.PrecioVenta) <= 0.01m
                    || (preciosCotizados.TryGetValue(item.ProductoId, out var cotizados)
                        && cotizados.Any(c => Math.Abs(item.PrecioUnitario - c) <= 0.01m));
                if (!precioValido)
                {
                    _logger.LogWarning("[POS] Precio rechazado para producto {ProductoId}: enviado {Enviado}, vigente {Vigente}.",
                        prod.Id, item.PrecioUnitario, prod.PrecioVenta);
                    return ResultadoVenta.Falla(
                        $"El precio de '{prod.Nombre}' cambió (ahora L. {prod.PrecioVenta:N2}). Recarga el POS e intenta de nuevo.");
                }

                if (item.Descuento > item.Cantidad * item.PrecioUnitario)
                    return ResultadoVenta.Falla($"El descuento de '{prod.Nombre}' no puede ser mayor que su subtotal.");

                // Productos marcados "no admite descuento": ni descuento propio ni parte del general
                if (!prod.PermiteDescuento && item.Descuento > 0)
                    return ResultadoVenta.Falla($"'{prod.Nombre}' no admite descuento. Quítaselo e intenta de nuevo.");

                var netoLinea = item.Cantidad * item.PrecioUnitario - item.Descuento;
                var parteGeneral = (pctGeneral > 0 && prod.PermiteDescuento && netoLinea > 0)
                    ? Math.Round(netoLinea * pctGeneral / 100m, 2, MidpointRounding.AwayFromZero)
                    : 0m;
                descuentosFinales.Add(item.Descuento + parteGeneral);

                var cantidadTotal = cantidadPorProducto[item.ProductoId];
                if (prod.Stock < cantidadTotal)
                {
                    // El prefijo STOCK_INSUFICIENTE| le indica al POS que pida la clave de un supervisor.
                    var msgStock = $"Stock insuficiente para '{prod.Nombre}'. Disponible: {prod.Stock:N2}, solicitado: {cantidadTotal:N2}.";
                    if (string.IsNullOrWhiteSpace(request.AutorizadorUsuario))
                        return ResultadoVenta.Falla("STOCK_INSUFICIENTE|" + msgStock);

                    autorizadoPor ??= await ValidarAutorizadorAsync(request);
                    if (autorizadoPor == null)
                        return ResultadoVenta.Falla("STOCK_INSUFICIENTE|" + msgStock +
                            " La autorización no es válida: revisa usuario y contraseña (debe ser un supervisor o administrador).");
                }

                int? impuestoId = prod.ImpuestoId ?? impuestoPredeterminadoId;
                // Misma regla que ISVHelper: si el impuesto del producto no está activo se usa el predeterminado
                // (antes la línea quedaba en 0% mientras el total sí cobraba ISV: detalle y total no cuadraban)
                decimal tasaCorrecta = exonerada ? 0m
                    : (impuestoId.HasValue && impuestosActivos.ContainsKey(impuestoId.Value)
                        ? impuestosActivos[impuestoId.Value]
                        : (impuestoPredeterminadoId.HasValue && impuestosActivos.ContainsKey(impuestoPredeterminadoId.Value)
                            ? impuestosActivos[impuestoPredeterminadoId.Value] : 0m));

                // 🔑 COGS: capturamos el costo unitario actual como snapshot
                decimal costoUnitario = prod.PrecioCompra;

                itemsConTasa.Add((item, item.ProductoId, tasaCorrecta, costoUnitario));

                itemsValidados.Add(new ItemParaISV
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Descuento = descuentosFinales[^1],
                    ImpuestoId = impuestoId
                });
            }

            var baseNeta = request.Items.Sum(i => i.Cantidad * i.PrecioUnitario - i.Descuento);
            if (request.Descuento > baseNeta)
                return ResultadoVenta.Falla("El descuento general no puede ser mayor que el total de los productos.");

            decimal montoEnvioCalculo = request.RequiereEnvio ? request.MontoEnvio : 0;
            var isvResult = ISVHelper.Calcular(_context, itemsValidados, request.Descuento, montoEnvioCalculo, exonerada);

            decimal subtotalItems = 0;
            decimal costoTotalVendido = 0;
            var detalles = new List<DetalleVenta>();

            for (var idxItem = 0; idxItem < itemsConTasa.Count; idxItem++)
            {
                var (itemOrig, productoId, tasa, costoUnitario) = itemsConTasa[idxItem];
                var descLinea = descuentosFinales[idxItem];
                var st = itemOrig.Cantidad * itemOrig.PrecioUnitario;
                var stConDesc = st - descLinea;
                var iv = Math.Round(stConDesc * (tasa / 100m), 2);
                var tot = stConDesc + iv;

                var costoLinea = costoUnitario * itemOrig.Cantidad;

                subtotalItems += st;
                costoTotalVendido += costoLinea;

                detalles.Add(new DetalleVenta
                {
                    ProductoId = productoId,
                    Cantidad = itemOrig.Cantidad,
                    PrecioUnitario = itemOrig.PrecioUnitario,
                    Descuento = descLinea,
                    ImpuestoPorcentaje = tasa,
                    Subtotal = stConDesc,
                    Total = tot,
                    // 🔑 COGS: snapshot del costo
                    CostoUnitarioSnapshot = costoUnitario,
                    CostoTotalLinea = costoLinea
                });
            }

            subtotalItems += montoEnvioCalculo;
            var impuestosTotal = isvResult.ISVTotal;
            var total = isvResult.Total;

            // Saldo a favor del cliente: se paga primero con el crédito y solo el resto se cobra
            decimal credito = request.SaldoFavorAplicar > 0 ? Math.Round(Math.Min(request.SaldoFavorAplicar, total), 2) : 0m;
            if (credito > 0 && !request.ClienteId.HasValue)
                return ResultadoVenta.Falla("El saldo a favor solo se puede usar con un cliente seleccionado.");
            decimal porCobrar = total - credito;

            var metodoPago = await _context.MetodosPago.FirstOrDefaultAsync(m => m.Id == request.MetodoPagoId);
            var tipoMetodo = (metodoPago?.Tipo ?? "").Trim().ToLower();

            bool esEfectivo = tipoMetodo.Contains("efectivo")
                           || tipoMetodo.Contains("cash")
                           || tipoMetodo.Contains("contado")
                           || tipoMetodo == "";

            bool esCredito = tipoMetodo.Contains("credito");

            if (request.Monto < porCobrar && !esCredito)
                return ResultadoVenta.Falla($"Falta dinero para completar la venta. Faltan L. {(porCobrar - request.Monto):N2}");

            decimal montoRecibido = esEfectivo ? request.Monto : porCobrar;
            decimal cambioCalculado = esEfectivo ? (request.Monto - porCobrar) : 0;
            if (cambioCalculado < 0) cambioCalculado = 0;

            if (esCredito && !request.ClienteId.HasValue)
                return ResultadoVenta.Falla("Las ventas al crédito requieren un cliente seleccionado.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 🔒 Saldo a favor: se revisa dentro de la transacción y con el cliente bloqueado,
                // para que dos cajas no gasten el mismo crédito a la vez.
                if (credito > 0)
                {
                    if (_context.Database.IsRelational())
                    {
                        var tablaCli = _context.Model.FindEntityType(typeof(Cliente))?.GetTableName() ?? "Clientes";
#pragma warning disable EF1002 // el nombre de tabla sale del modelo de EF, el Id va como parametro
                        await _context.Database.ExecuteSqlRawAsync($"SELECT `Id` FROM `{tablaCli}` WHERE `Id` = {{0}} FOR UPDATE", request.ClienteId!.Value);
#pragma warning restore EF1002
                    }
                    var saldoDisponible = await SaldoFavorHelper.SaldoAsync(_context, request.ClienteId!.Value);
                    if (credito > saldoDisponible + 0.005m)
                        return ResultadoVenta.Falla($"El cliente solo tiene L. {saldoDisponible:N2} de saldo a favor disponible.");
                }

                // 🔒 Control de límite de crédito (solo si el cliente tiene un límite configurado > 0).
                // Se revisa DENTRO de la transacción y con la fila del cliente bloqueada: dos ventas a crédito
                // simultáneas al mismo cliente se atienden una tras otra y ya no pueden pasarse del límite juntas.
                if (esCredito && request.ClienteId.HasValue)
                {
                    if (_context.Database.IsRelational())
                    {
                        var tablaClientes = _context.Model.FindEntityType(typeof(Cliente))?.GetTableName() ?? "Clientes";
#pragma warning disable EF1002 // el nombre de tabla sale del modelo de EF, el Id va como parametro
                        await _context.Database.ExecuteSqlRawAsync($"SELECT `Id` FROM `{tablaClientes}` WHERE `Id` = {{0}} FOR UPDATE", request.ClienteId.Value);
#pragma warning restore EF1002
                    }

                    var clienteCredito = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == request.ClienteId.Value);
                    if (clienteCredito != null && clienteCredito.LimiteCredito > 0)
                    {
                        var saldoActual = await _context.Ventas
                            .Where(v => v.ClienteId == clienteCredito.Id && v.EsCredito && v.Estado != "Anulada")
                            .SumAsync(v => (decimal?)v.SaldoPendiente) ?? 0m;

                        if (saldoActual + porCobrar > clienteCredito.LimiteCredito)
                        {
                            return ResultadoVenta.Falla(
                                $"El cliente '{clienteCredito.Nombre}' excede su límite de crédito. " +
                                $"Límite: L. {clienteCredito.LimiteCredito:N2}, saldo actual: L. {saldoActual:N2}, " +
                                $"esta venta: L. {porCobrar:N2}.");
                        }
                    }
                }

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
                    UsuarioCreoId = usuarioId,
                    RequiereEnvio = request.RequiereEnvio,
                    MontoEnvio = montoEnvioCalculo,
                    EsExonerada = exonerada,
                    EsCredito = esCredito,
                    SaldoPendiente = esCredito ? porCobrar : 0,
                    ControlEntrega = request.ControlEntrega,
                    FechaVencimientoCredito = null,
                    MovimientoRegistrado = false,
                    // 🔑 COGS
                    CostoTotalVendido = costoTotalVendido,
                    CostoRegistrado = false
                };

                if (esCredito && request.ClienteId.HasValue)
                {
                    var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == request.ClienteId.Value);
                    if (cliente != null && cliente.DiasCredito > 0)
                    {
                        venta.FechaVencimientoCredito = DateTime.Today.AddDays(cliente.DiasCredito);
                    }
                }

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                foreach (var d in detalles)
                {
                    d.VentaId = venta.Id;
                    _context.DetalleVentas.Add(d);

                    var prod = productosDict[d.ProductoId];
                    var notaKardex = (autorizadoPor != null && prod.Stock < d.Cantidad)
                        ? $"Venta sin existencia autorizada por {autorizadoPor}: el stock queda negativo hasta reabastecer"
                        : null;
                    InventarioHelper.Registrar(_context, prod, -d.Cantidad, "Venta", venta.Numero, venta.Id, usuarioId, d.CostoUnitarioSnapshot, notaKardex);
                }

                int clienteIdPago = request.ClienteId ?? 1;
                var pago = new Pago
                {
                    VentaId = venta.Id,
                    ClienteId = clienteIdPago,
                    Fecha = DateTime.Now,
                    Monto = esCredito ? 0 : porCobrar,
                    MontoRecibido = esEfectivo ? montoRecibido : (decimal?)null,
                    Cambio = esEfectivo ? cambioCalculado : (decimal?)null,
                    MetodoPagoId = request.MetodoPagoId,
                    Referencia = request.Referencia,
                    UsuarioCreoId = usuarioId
                };
                // Si todo se pagó con saldo a favor no hay cobro en el método elegido
                if (!(porCobrar <= 0 && credito > 0)) _context.Pagos.Add(pago);
                await _context.SaveChangesAsync();

                Envio? envio = null;
                if (request.RequiereEnvio)
                {
                    envio = EnvioHelper.CrearParaVenta(
                        _context, venta,
                        request.EnvioDireccion!,
                        request.EnvioReferencia,
                        request.EnvioContactoNombre!,
                        request.EnvioContactoTelefono!,
                        request.EnvioCiudad,
                        request.EnvioZonaId,
                        montoEnvioCalculo,
                        usuarioId
                    );
                }

                if (request.CotizacionId.HasValue)
                {
                    // Reclamo atómico: una cotización solo se convierte en UNA venta
                    if (_context.Database.IsRelational())
                    {
                        var cid = request.CotizacionId.Value;
                        var reclamada = await _context.Cotizaciones
                            .Where(c => c.Id == cid && c.Estado == "Aceptada")
                            .ExecuteUpdateAsync(u => u.SetProperty(c => c.Estado, "Aceptada"));
                        if (reclamada == 0)
                        {
                            await transaction.RollbackAsync();
                            _context.ChangeTracker.Clear();
                            return ResultadoVenta.Falla("La cotización ya fue convertida en otra venta.");
                        }
                    }
                    var cotizacion = await _context.Cotizaciones.FirstOrDefaultAsync(c => c.Id == request.CotizacionId.Value);
                    if (cotizacion != null && cotizacion.Estado == "Aceptada")
                    {
                        cotizacion.Estado = "Convertida";
                        cotizacion.VentaId = venta.Id;
                        cotizacion.FechaConversion = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();

                // ================================================================
                // MOVIMIENTOS FINANCIEROS
                // - Contado: Ingreso (venta) + Egreso (cambio) + Egreso (COGS)
                // - Crédito: NO se registra ingreso (se registra al cobrar),
                //            pero SÍ se registra el COGS para el Estado de Resultados.
                // ================================================================
                if (!esCredito)
                {
                    var hayIngreso = esEfectivo ? montoRecibido > 0 : porCobrar > 0;
                    var (movIngreso, errorIngreso) = !hayIngreso ? ((MovimientoFinanciero?)null, (string?)null) : MovimientoAutomaticoHelper.RegistrarIngresoVenta(
                        _context, venta.Id, venta.Numero,
                        // Efectivo: entra lo RECIBIDO; el cambio sale aparte como egreso (neto = total).
                        monto: esEfectivo ? montoRecibido : porCobrar,
                        usuarioId: usuarioId,
                        // Si es efectivo pero el metodo se llama distinto (p.ej. "Contado"), se rotula "Efectivo" para que el arqueo de caja lo cuente.
                        formaPago: (esEfectivo && !(metodoPago?.Nombre ?? "").Contains("efectivo", StringComparison.OrdinalIgnoreCase)) ? "Efectivo" : metodoPago?.Nombre,
                        cuentaId: apertura.CuentaId
                    );

                    if (hayIngreso && movIngreso == null)
                    {
                        await transaction.RollbackAsync();
                        _context.ChangeTracker.Clear();
                        return ResultadoVenta.Falla(
                            $"No se pudo registrar el ingreso en Finanzas. {errorIngreso ?? "Revisa la configuración de cuentas y categorías."}");
                    }
                    if (movIngreso != null) venta.MovimientoIngresoId = movIngreso.Id;

                    if (esEfectivo && cambioCalculado > 0)
                    {
                        var (movCambio, errorCambio) = MovimientoAutomaticoHelper.RegistrarEgresoCambio(
                            _context, venta.Id, venta.Numero,
                            montoCambio: cambioCalculado,
                            usuarioId: usuarioId,
                            formaPago: "Efectivo",
                            cuentaId: apertura.CuentaId,
                            origen: "Venta"
                        );

                        if (movCambio == null)
                        {
                            await transaction.RollbackAsync();
                            _context.ChangeTracker.Clear();
                            return ResultadoVenta.Falla(
                                $"No se pudo registrar el egreso por cambio en Finanzas. {errorCambio ?? "Los movimientos fueron revertidos."}");
                        }
                        venta.MovimientoCambioId = movCambio.Id;
                    }

                    venta.MovimientoRegistrado = true;
                    venta.FechaMovimiento = DateTime.Now;
                }
                else
                {
                    venta.MovimientoRegistrado = false;
                    venta.FechaMovimiento = null;
                }

                // Parte pagada con saldo a favor: ingreso no efectivo (no entra a la caja física) + descuento del crédito
                if (credito > 0)
                {
                    var (movCredito, errorCredito) = MovimientoAutomaticoHelper.RegistrarIngresoVenta(
                        _context, venta.Id, venta.Numero, credito, usuarioId,
                        formaPago: SaldoFavorHelper.NombreMetodo, cuentaId: apertura.CuentaId);
                    if (movCredito == null)
                    {
                        await transaction.RollbackAsync();
                        _context.ChangeTracker.Clear();
                        return ResultadoVenta.Falla(
                            $"No se pudo registrar el pago con saldo a favor en Finanzas. {errorCredito ?? "Los movimientos fueron revertidos."}");
                    }
                    venta.SaldoFavorAplicado = credito;
                    SaldoFavorHelper.Usar(_context, request.ClienteId!.Value, credito, $"Pago de la venta {venta.Numero}", venta.Id, usuarioId);
                    var metodoSaldo = SaldoFavorHelper.AsegurarMetodoPago(_context);
                    _context.Pagos.Add(new Pago
                    {
                        VentaId = venta.Id,
                        ClienteId = request.ClienteId!.Value,
                        Fecha = DateTime.Now,
                        Monto = credito,
                        MetodoPagoId = metodoSaldo.Id,
                        Referencia = "Saldo a favor",
                        UsuarioCreoId = usuarioId
                    });
                }

                // 🔑 COGS: registrar el costo de venta siempre (contado o crédito)
                if (costoTotalVendido > 0)
                {
                    var (movCosto, errorCosto) = MovimientoAutomaticoHelper.RegistrarEgresoCostoVenta(
                        _context,
                        venta.Id,
                        venta.Numero,
                        costoTotalVendido,
                        usuarioId,
                        apertura.CuentaId
                    );

                    if (movCosto == null)
                    {
                        // El costo no es bloqueante: la venta se mantiene y el costo se puede
                        // regenerar más adelante si es necesario.
                        _logger.LogWarning("[POS] No se pudo registrar el COGS de la venta {Numero}. {Error}", venta.Numero, errorCosto);
                    }
                    else
                    {
                        venta.MovimientoCostoId = movCosto.Id;
                        venta.CostoRegistrado = true;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ActividadHelper.Registrar(_context, usuarioId, "Venta POS",
                    $"Registró la venta {venta.Numero} por L. {total:N2}" +
                    (esCredito ? " (a crédito)" : $" ({(esEfectivo ? $"Efectivo, cambio: L. {cambioCalculado:N2}" : "Tarjeta/Transferencia")})") +
                    $" | COGS: L. {costoTotalVendido:N2}" +
                    (autorizadoPor != null ? $" | Sin existencia autorizada por {autorizadoPor}" : "") +
                    (credito > 0 ? $" | Saldo a favor aplicado: L. {credito:N2}" : ""),
                    ip);

                return new ResultadoVenta
                {
                    Exito = true,
                    Numero = venta.Numero,
                    VentaId = venta.Id,
                    Total = total,
                    MontoRecibido = esEfectivo ? montoRecibido : (decimal?)null,
                    Cambio = cambioCalculado,
                    EsCredito = esCredito,
                    RequiereEnvio = request.RequiereEnvio,
                    EnvioNumero = envio?.Numero,
                    MovimientoRegistrado = venta.MovimientoRegistrado,
                    CostoRegistrado = venta.CostoRegistrado,
                    CostoTotalVendido = costoTotalVendido
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                throw; // RegistrarVentaAsync lo reintenta
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                _logger.LogError(ex, "[POS] Error al registrar la venta (transacción revertida).");
                return ResultadoVenta.Falla(
                    "Ocurrió un error al registrar la venta. No se guardó nada (la operación fue revertida). " +
                    "Si se repite, avisa al administrador: el detalle quedó en el log del sistema.");
            }
        }
    }
}
