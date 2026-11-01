using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Ventas
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Venta Venta { get; set; } = new();
        public Cliente? Cliente { get; set; }
        public List<DetalleVenta> Items { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public List<Pago> Pagos { get; set; } = new();
        public List<MetodoPago> MetodosPago { get; set; } = new();
        public MovimientoFinanciero? MovimientoIngreso { get; set; }
        public MovimientoFinanciero? MovimientoCambio { get; set; }

        public Factura? FacturaAsociada { get; set; }
        public bool VentaTieneFactura { get; set; }
        public bool PuedeAnular { get; set; }
        public bool PuedeDevolver { get; set; }
        public List<EntregaBodegaHelper.LineaPendiente> LineasEntrega { get; set; } = new();
        public List<EntregaBodega> Entregas { get; set; } = new();
        public decimal PendienteEntrega { get; set; }
        public bool PuedeEntregar { get; set; }

        public IActionResult OnGet(int id)
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Index", "ver")))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var venta = _context.Ventas.FirstOrDefault(v => v.Id == id);
            if (venta == null)
            {
                TempData["Error"] = "Venta no encontrada";
                return RedirectToPage("/Ventas/Index");
            }

            Venta = venta;
            Cliente = venta.ClienteId.HasValue
                ? _context.Clientes.FirstOrDefault(c => c.Id == venta.ClienteId.Value)
                : null;
            Items = _context.DetalleVentas.Where(d => d.VentaId == id).ToList();
            Productos = _context.Productos.ToList();
            Pagos = _context.Pagos.Where(p => p.VentaId == id).ToList();
            MetodosPago = _context.MetodosPago.ToList();

            FacturaAsociada = _context.Facturas.FirstOrDefault(f => f.VentaId == id);
            VentaTieneFactura = FacturaAsociada != null;
            PuedeAnular = PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Delete", "eliminar");
            PuedeDevolver = PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Devoluciones", "crear");

            if (venta.MovimientoIngresoId.HasValue)
            {
                MovimientoIngreso = _context.MovimientosFinancieros
                    .FirstOrDefault(m => m.Id == venta.MovimientoIngresoId.Value);
            }
            if (venta.MovimientoCambioId.HasValue)
            {
                MovimientoCambio = _context.MovimientosFinancieros
                    .FirstOrDefault(m => m.Id == venta.MovimientoCambioId.Value);
            }

            LineasEntrega = EntregaBodegaHelper.Lineas(_context, id);
            Entregas = _context.EntregasBodega.AsNoTracking().Where(e => e.VentaId == id).OrderBy(e => e.Fecha).ToList();
            PendienteEntrega = LineasEntrega.Sum(l => l.Pendiente);
            PuedeEntregar = PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Create", "crear")
                         || PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "Salidas", "crear");

            return Page();
        }

        public IActionResult OnPostControlEntrega(int id)
        {
            var puede = PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Create", "crear")
                     || PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "Salidas", "crear");
            if (!puede)
            {
                TempData["Error"] = "No tienes permiso para controlar entregas";
                return RedirectToPage("/Ventas/Details", new { id });
            }
            var venta = _context.Ventas.FirstOrDefault(v => v.Id == id);
            if (venta == null || venta.Estado == "Anulada")
            {
                TempData["Error"] = "Venta no disponible";
                return RedirectToPage("/Ventas/Details", new { id });
            }
            venta.ControlEntrega = true;
            _context.SaveChanges();
            TempData["Success"] = "Listo: esta venta ahora controla sus entregas en bodega. Registra la primera entrega cuando salga mercadería.";
            return RedirectToPage("/Ventas/Details", new { id });
        }

        public IActionResult OnPostAnular(int id, string? motivo)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Delete", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para anular ventas";
                return RedirectToPage("/Ventas/Details", new { id });
            }

            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null) return RedirectToPage("/Auth/Login");

            var (ok, error) = AnulacionVentaHelper.Anular(_context, id, motivo ?? "", currentUser.Id);
            if (!ok)
            {
                TempData["Error"] = error;
                return RedirectToPage("/Ventas/Details", new { id });
            }

            var venta = _context.Ventas.FirstOrDefault(v => v.Id == id);
            ActividadHelper.Registrar(
                _context,
                currentUser.Id,
                "Anular venta",
                $"Anuló la venta {venta?.Numero}. Motivo: {motivo}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Venta {venta?.Numero} anulada. Inventario y finanzas fueron revertidos.";
            return RedirectToPage("/Ventas/Details", new { id });
        }

        public IActionResult OnPostRegistrarEnFinanzas(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "MovimientosCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para registrar movimientos financieros";
                return RedirectToPage("/Ventas/Details", new { id });
            }

            var venta = _context.Ventas.FirstOrDefault(v => v.Id == id);
            if (venta == null)
            {
                TempData["Error"] = "Venta no encontrada";
                return RedirectToPage("/Ventas/Index");
            }

            if (_context.Facturas.Any(f => f.VentaId == id))
            {
                TempData["Error"] = "Esta venta ya tiene una factura asociada. Los movimientos ya fueron registrados al facturar.";
                return RedirectToPage("/Ventas/Details", new { id });
            }

            if (venta.MovimientoRegistrado)
            {
                TempData["Error"] = "Esta venta ya fue registrada en Finanzas";
                return RedirectToPage("/Ventas/Details", new { id });
            }

            if (venta.Estado == "Anulada")
            {
                TempData["Error"] = "No se puede registrar una venta anulada";
                return RedirectToPage("/Ventas/Details", new { id });
            }

            // 🔒 OPCIÓN A: Las ventas al crédito NO se registran aquí.
            // Se registran SOLO al cobrar desde Finanzas > Cobros.
            if (venta.EsCredito)
            {
                TempData["Error"] = "Esta es una venta al crédito. Los ingresos se registran automáticamente al cobrar en Finanzas > Cuentas por Cobrar.";
                return RedirectToPage("/Ventas/Details", new { id });
            }

            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, venta.Fecha);
            if (!periodoOk)
            {
                TempData["Error"] = $"No se puede registrar el movimiento: {periodoError}";
                return RedirectToPage("/Ventas/Details", new { id });
            }

            // Detectar método de pago
            var pago = _context.Pagos
                .Where(p => p.VentaId == id)
                .OrderByDescending(p => p.Id)
                .FirstOrDefault();

            var metodoPago = pago != null
                ? _context.MetodosPago.FirstOrDefault(m => m.Id == pago.MetodoPagoId)
                : null;

            var tipoMetodo = (metodoPago?.Tipo ?? "").Trim().ToLower();
            bool esEfectivo = tipoMetodo.Contains("efectivo")
                           || tipoMetodo.Contains("cash")
                           || tipoMetodo.Contains("contado")
                           || tipoMetodo == "";

            decimal montoRecibido = pago?.MontoRecibido ?? venta.Total;
            decimal cambio = pago?.Cambio ?? 0;

            using var transaction = _context.Database.BeginTransaction();

            try
            {
                var apertura = _context.AperturasCaja.FirstOrDefault(a => a.Activa);
                int? cuentaId = apertura?.CuentaId;

                if (!cuentaId.HasValue)
                {
                    var cuentaCaja = _context.CuentasFinancieras
                        .Where(c => c.Tipo == "Caja" && c.Activa
                                 && (c.Subtipo == null || c.Subtipo != "CuentasPorCobrar"))
                        .OrderBy(c => c.Id)
                        .FirstOrDefault();
                    cuentaId = cuentaCaja?.Id;
                }

                if (!cuentaId.HasValue)
                {
                    transaction.Rollback();
                    TempData["Error"] = "No hay una cuenta de caja activa para registrar el movimiento";
                    return RedirectToPage("/Ventas/Details", new { id });
                }

                var montoIngreso = esEfectivo ? montoRecibido : venta.Total;

                var (movIngreso, errorIngreso) = MovimientoAutomaticoHelper.RegistrarIngresoVenta(
                    _context,
                    venta.Id,
                    venta.Numero,
                    monto: montoIngreso,
                    usuarioId: currentUser?.Id ?? 0,
                    formaPago: metodoPago?.Nombre,
                    cuentaId: cuentaId
                );

                if (movIngreso == null)
                {
                    transaction.Rollback();
                    TempData["Error"] = $"No se pudo registrar el ingreso. {errorIngreso ?? "Revisa la configuración de cuentas y categorías."}";
                    return RedirectToPage("/Ventas/Details", new { id });
                }

                venta.MovimientoIngresoId = movIngreso.Id;

                MovimientoFinanciero? movCambio = null;
                if (esEfectivo && cambio > 0)
                {
                    var (mov, errorCambio) = MovimientoAutomaticoHelper.RegistrarEgresoCambio(
                        _context,
                        venta.Id,
                        venta.Numero,
                        montoCambio: cambio,
                        usuarioId: currentUser?.Id ?? 0,
                        formaPago: "Efectivo",
                        cuentaId: cuentaId,
                        origen: "Venta"
                    );

                    if (mov == null)
                    {
                        transaction.Rollback();
                        TempData["Error"] = $"No se pudo registrar el egreso por cambio. {errorCambio ?? "Los movimientos fueron revertidos."}";
                        return RedirectToPage("/Ventas/Details", new { id });
                    }

                    movCambio = mov;
                    venta.MovimientoCambioId = movCambio.Id;
                }

                // COGS: si la venta aún no tiene su costo registrado
                if (!venta.CostoRegistrado && venta.CostoTotalVendido > 0)
                {
                    var (movCosto, errorCosto) = MovimientoAutomaticoHelper.RegistrarEgresoCostoVenta(
                        _context,
                        venta.Id,
                        venta.Numero,
                        venta.CostoTotalVendido,
                        currentUser?.Id ?? 0,
                        cuentaId);

                    if (movCosto != null)
                    {
                        venta.MovimientoCostoId = movCosto.Id;
                        venta.CostoRegistrado = true;
                    }
                    else
                    {
                        Console.WriteLine($"[Ventas/Details] Advertencia: no se pudo registrar el COGS. {errorCosto}");
                    }
                }

                venta.MovimientoRegistrado = true;
                venta.FechaMovimiento = DateTime.Now;
                _context.SaveChanges();

                transaction.Commit();

                ActividadHelper.Registrar(
                    _context,
                    currentUser?.Id ?? 0,
                    "Registrar venta en Finanzas",
                    $"Registró la venta {venta.Numero} en Finanzas (sin factura). Ingreso: L. {montoIngreso:N2}" +
                    (cambio > 0 ? $", Cambio: L. {cambio:N2}" : ""),
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["Success"] = $"Venta {venta.Numero} registrada en Finanzas correctamente" +
                    (cambio > 0 ? $" (Ingreso: L. {montoIngreso:N2}, Cambio: L. {cambio:N2})" : $" (Ingreso: L. {montoIngreso:N2})");

                return RedirectToPage("/Ventas/Details", new { id });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"ERROR al registrar venta en Finanzas: {ex.Message}");
                TempData["Error"] = $"Error al registrar: {ex.Message}";
                return RedirectToPage("/Ventas/Details", new { id });
            }
        }
    }
}