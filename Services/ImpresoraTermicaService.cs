using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Services
{
    /// <summary>
    /// Impresión directa de tickets en impresoras térmicas ESC/POS (sin el cuadro de impresión del navegador),
    /// con apertura de la gaveta de dinero. Funciona con impresoras de red (IP, puerto 9100) o instaladas en Windows.
    /// Como el sistema corre en el servidor del negocio, la impresora es la que está conectada a ese servidor o a su red.
    /// </summary>
    public class ImpresoraTermicaService
    {
        private readonly ApplicationDbContext _db;
        private readonly ImpresoraConfigStore _store;
        private readonly ILogger<ImpresoraTermicaService> _log;

        public ImpresoraTermicaService(ApplicationDbContext db, ImpresoraConfigStore store, ILogger<ImpresoraTermicaService> log)
        {
            _db = db; _store = store; _log = log;
        }

        public ImpresoraConfig Config => _store.Obtener();

        // ───────────────────────── Envío a la impresora ─────────────────────────

        public async Task<(bool Ok, string Mensaje)> EnviarAsync(byte[] datos, ImpresoraConfig? cfg = null)
        {
            cfg ??= _store.Obtener();
            var error = ImpresoraConfigStore.Validar(cfg);
            if (cfg.Modo == "ninguna") return (false, "No hay impresora configurada. Ve a Configuración → Impresora de tickets.");
            if (error != null) return (false, error);

            try
            {
                if (cfg.Modo == "red")
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    using var tcp = new TcpClient();
                    await tcp.ConnectAsync(cfg.Host, cfg.Puerto, cts.Token);
                    await using var stream = tcp.GetStream();
                    await stream.WriteAsync(datos, cts.Token);
                    await stream.FlushAsync(cts.Token);
                    return (true, $"Enviado a la impresora {cfg.Host}:{cfg.Puerto}.");
                }

                if (!OperatingSystem.IsWindows())
                    return (false, "La impresión por nombre de Windows solo funciona si el sistema corre en Windows. Usa el modo de red (IP).");

                var (ok, msg) = ImpresoraWindows.Enviar(cfg.Impresora, datos);
                return (ok, msg);
            }
            catch (OperationCanceledException)
            {
                return (false, $"La impresora {cfg.Host}:{cfg.Puerto} no respondió en 3 segundos. ¿Está encendida y en la misma red?");
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "No se pudo imprimir el ticket");
                return (false, "No se pudo imprimir: " + ex.Message);
            }
        }

        public Task<(bool Ok, string Mensaje)> PruebaAsync(ImpresoraConfig? cfg = null)
        {
            cfg ??= _store.Obtener();
            return EnviarAsync(TicketTermico.Prueba(cfg), cfg);
        }

        public Task<(bool Ok, string Mensaje)> AbrirGavetaAsync(ImpresoraConfig? cfg = null)
        {
            cfg ??= _store.Obtener();
            var b = new EscPosBuilder(cfg.Columnas).Gaveta();
            return EnviarAsync(b.ToArray(), cfg);
        }

        // ───────────────────────── Tickets de la base de datos ─────────────────────────

        private async Task<ConfiguracionEmpresa> EmpresaAsync()
            => await _db.ConfiguracionEmpresa.AsNoTracking().FirstOrDefaultAsync() ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };

        private static List<string> CabeceraEmpresa(ConfiguracionEmpresa e)
        {
            var l = new List<string>();
            if (!string.IsNullOrWhiteSpace(e.RazonSocial)) l.Add(e.RazonSocial!);
            if (!string.IsNullOrWhiteSpace(e.RTN)) l.Add("RTN: " + e.RTN);
            if (!string.IsNullOrWhiteSpace(e.Direccion)) l.Add(e.Direccion!);
            if (!string.IsNullOrWhiteSpace(e.Telefono)) l.Add("Tel: " + e.Telefono);
            return l;
        }

        private List<string> PieTicket(ImpresoraConfig cfg)
            => string.IsNullOrWhiteSpace(cfg.Pie) ? new List<string>() : new List<string> { cfg.Pie };

        public async Task<(bool Ok, string Mensaje)> ImprimirFacturaAsync(int facturaId)
        {
            var cfg = _store.Obtener();
            var f = await _db.Facturas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == facturaId);
            if (f == null) return (false, "Factura no encontrada.");
            var emp = await EmpresaAsync();
            var cli = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == f.ClienteId);
            var nombres = await _db.Productos.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nombre);
            var dets = await _db.DetalleFacturas.AsNoTracking().Where(d => d.FacturaId == facturaId).ToListAsync();

            var d = new TicketDatos { Empresa = emp.Nombre, Cabecera = CabeceraEmpresa(emp), Titulo = "FACTURA " + f.Numero };
            if (f.Estado == "Anulada") d.Titulo = "*** ANULADA *** " + f.Numero;
            if (!string.IsNullOrWhiteSpace(f.CAI))
            {
                d.Datos.Add("CAI: " + f.CAI);
                d.Datos.Add("Rango: " + f.RangoInicial);
                d.Datos.Add("al: " + f.RangoFinal);
                if (f.FechaLimiteEmision.HasValue) d.Datos.Add("Fecha limite emision: " + f.FechaLimiteEmision.Value.ToString("dd/MM/yyyy"));
            }
            d.Datos.Add("Fecha: " + f.Fecha.ToString("dd/MM/yyyy HH:mm"));
            d.Datos.Add("Cliente: " + (cli?.Nombre ?? "Consumidor final"));
            if (!string.IsNullOrWhiteSpace(cli?.RTN)) d.Datos.Add("RTN: " + cli!.RTN);

            foreach (var det in dets)
            {
                var nombre = !string.IsNullOrWhiteSpace(det.Descripcion) ? det.Descripcion! : (nombres.TryGetValue(det.ProductoId, out var n) ? n : $"Producto {det.ProductoId}");
                d.Lineas.Add(new TicketLinea(nombre, det.Cantidad, det.PrecioUnitario, det.Total));
            }
            var desgloseIsv = Kirkenta.Helpers.Finanzas.ISVHelper.DesglosarFactura(_db, f, dets);
            d.Totales.Add(("Subtotal productos", TicketTermico.M(desgloseIsv.SubtotalProductos)));
            if (f.Descuento > 0) d.Totales.Add(("Descuento", "-" + TicketTermico.M(f.Descuento)));
            if (f.MontoEnvio > 0) d.Totales.Add(("Envio", TicketTermico.M(f.MontoEnvio)));
            d.Totales.Add(f.EsExonerada ? ("ISV (exonerado)", TicketTermico.M(0m)) : ("ISV", TicketTermico.M(f.Impuestos)));
            if (desgloseIsv.EnvioISV > 0) d.Totales.Add(("  incluye ISV envio", TicketTermico.M(desgloseIsv.EnvioISV)));
            d.TotalValor = TicketTermico.M(f.Total);
            d.Pie = PieTicket(cfg);

            return await EnviarAsync(TicketTermico.Construir(d, cfg), cfg);
        }

        public async Task<(bool Ok, string Mensaje)> ImprimirVentaAsync(int ventaId)
        {
            var cfg = _store.Obtener();
            var v = await _db.Ventas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ventaId);
            if (v == null) return (false, "Venta no encontrada.");
            var emp = await EmpresaAsync();
            var cli = v.ClienteId.HasValue ? await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == v.ClienteId) : null;
            var nombres = await _db.Productos.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nombre);
            var dets = await _db.DetalleVentas.AsNoTracking().Where(d => d.VentaId == ventaId).ToListAsync();

            var d = new TicketDatos { Empresa = emp.Nombre, Cabecera = CabeceraEmpresa(emp), Titulo = "TICKET " + v.Numero };
            d.Datos.Add("Fecha: " + v.Fecha.ToString("dd/MM/yyyy HH:mm"));
            d.Datos.Add("Cliente: " + (cli?.Nombre ?? "Consumidor final"));
            foreach (var det in dets)
            {
                var nombre = !string.IsNullOrWhiteSpace(det.Descripcion) ? det.Descripcion! : (nombres.TryGetValue(det.ProductoId, out var n) ? n : $"Producto {det.ProductoId}");
                d.Lineas.Add(new TicketLinea(nombre, det.Cantidad, det.PrecioUnitario, det.Total));
            }
            d.Totales.Add(("Subtotal", TicketTermico.M(v.Subtotal)));
            var descuentoVenta = v.Descuento + dets.Sum(x => x.Descuento);
            if (descuentoVenta > 0) d.Totales.Add(("Descuento", "-" + TicketTermico.M(descuentoVenta)));
            if (v.MontoEnvio > 0) d.Totales.Add(("Envio", TicketTermico.M(v.MontoEnvio)));
            d.Totales.Add(v.EsExonerada ? ("ISV (exonerado)", TicketTermico.M(0m)) : ("ISV", TicketTermico.M(v.Impuestos)));
            d.TotalValor = TicketTermico.M(v.Total);
            if (v.EsCredito) d.Datos.Add("Venta al credito. Saldo: L. " + TicketTermico.M(v.SaldoPendiente));
            d.Pie = PieTicket(cfg);
            d.Pie.Add("Este ticket no es factura fiscal.");

            return await EnviarAsync(TicketTermico.Construir(d, cfg), cfg);
        }

        // ───────────────────────── Impresión automática (nunca rompe el flujo de venta) ─────────────────────────

        /// <summary>Si está configurado "imprimir al cobrar", imprime el ticket de la venta. Ignora cualquier error.</summary>
        public void ImprimirVentaAuto(int ventaId)
        {
            try
            {
                var cfg = _store.Obtener();
                if (cfg.Modo == "ninguna" || cfg.Auto != "venta") return;
                var r = ImprimirVentaAsync(ventaId).GetAwaiter().GetResult();
                if (!r.Ok) _log.LogWarning("Impresión automática de la venta {id}: {m}", ventaId, r.Mensaje);
            }
            catch (Exception ex) { _log.LogWarning(ex, "Impresión automática de la venta {id} falló", ventaId); }
        }

        /// <summary>Si está configurado "imprimir al emitir la factura", imprime la factura. Ignora cualquier error.</summary>
        public void ImprimirFacturaAuto(int facturaId)
        {
            try
            {
                var cfg = _store.Obtener();
                if (cfg.Modo == "ninguna" || cfg.Auto != "factura") return;
                var r = ImprimirFacturaAsync(facturaId).GetAwaiter().GetResult();
                if (!r.Ok) _log.LogWarning("Impresión automática de la factura {id}: {m}", facturaId, r.Mensaje);
            }
            catch (Exception ex) { _log.LogWarning(ex, "Impresión automática de la factura {id} falló", facturaId); }
        }

        /// <summary>Nombres de las impresoras instaladas en Windows (para elegir en pantalla). Vacío si no se pueden leer.</summary>
        public static List<string> ListarImpresorasWindows()
        {
            var res = new List<string>();
            if (!OperatingSystem.IsWindows()) return res;
            try
            {
                var psi = new ProcessStartInfo("powershell", "-NoProfile -NonInteractive -Command \"Get-Printer | Select-Object -ExpandProperty Name\"")
                {
                    RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p == null) return res;
                var salida = p.StandardOutput.ReadToEndAsync();
                if (!p.WaitForExit(5000)) { try { p.Kill(); } catch { } return res; }
                foreach (var l in salida.GetAwaiter().GetResult().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    res.Add(l);
            }
            catch { /* sin PowerShell: se escribe el nombre a mano */ }
            return res;
        }
    }

    /// <summary>Envío de bytes "crudos" (RAW) a una impresora de Windows por su nombre, sin controlador gráfico.</summary>
    [SupportedOSPlatform("windows")]
    internal static class ImpresoraWindows
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class DOCINFOW
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pDocName = "Ticket Kirkenta";
            [MarshalAs(UnmanagedType.LPWStr)] public string? pOutputFile = null;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDataType = "RAW";
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool OpenPrinter(string src, out IntPtr hPrinter, IntPtr pd);
        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);
        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOW di);
        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);
        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);
        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);
        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static (bool Ok, string Mensaje) Enviar(string impresora, byte[] datos)
        {
            if (!OpenPrinter(impresora.Normalize(), out var h, IntPtr.Zero))
                return (false, $"Windows no encontró la impresora “{impresora}”. Revisa el nombre exacto en Configuración → Impresoras.");
            var ptr = IntPtr.Zero;
            try
            {
                if (!StartDocPrinter(h, 1, new DOCINFOW()))
                    return (false, "Windows rechazó el trabajo de impresión (código " + Marshal.GetLastWin32Error() + ").");
                try
                {
                    if (!StartPagePrinter(h)) return (false, "No se pudo iniciar la página (código " + Marshal.GetLastWin32Error() + ").");
                    ptr = Marshal.AllocCoTaskMem(datos.Length);
                    Marshal.Copy(datos, 0, ptr, datos.Length);
                    var ok = WritePrinter(h, ptr, datos.Length, out var escritos);
                    EndPagePrinter(h);
                    return ok && escritos == datos.Length
                        ? (true, $"Enviado a la impresora “{impresora}”.")
                        : (false, "La impresora no recibió todo el ticket (código " + Marshal.GetLastWin32Error() + ").");
                }
                finally { EndDocPrinter(h); }
            }
            finally
            {
                if (ptr != IntPtr.Zero) Marshal.FreeCoTaskMem(ptr);
                ClosePrinter(h);
            }
        }
    }
}
