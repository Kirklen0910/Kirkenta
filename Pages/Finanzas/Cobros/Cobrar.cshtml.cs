using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Cobros
{
    public class CobrarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CobrarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Venta Venta { get; set; } = new();
        public Factura? Factura { get; set; }
        public Cliente Cliente { get; set; } = new();
        public decimal SaldoPendiente { get; set; }
        public List<MetodoPago> MetodosPago { get; set; } = new();
        public List<CuentaFinanciera> CuentasCaja { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El monto es obligatorio")]
            [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
            public decimal Monto { get; set; }

            [Required(ErrorMessage = "El método de pago es obligatorio")]
            public int MetodoPagoId { get; set; }

            [Required(ErrorMessage = "Debes seleccionar una cuenta")]
            public int CuentaId { get; set; }

            public string? Referencia { get; set; }
        }

        public IActionResult OnGet(int ventaId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "Cobros", "editar"))
            {
                TempData["Error"] = "No tienes permiso para cobrar.";
                return RedirectToPage("/Finanzas/Cobros/Index");
            }

            var venta = _context.Ventas.FirstOrDefault(v => v.Id == ventaId);
            if (venta == null || !venta.EsCredito || venta.SaldoPendiente <= 0 || venta.Estado == "Anulada")
            {
                TempData["Error"] = "Venta no encontrada, anulada o sin saldo pendiente.";
                return RedirectToPage("/Finanzas/Cobros/Index");
            }

            // 🔒 PUNTO 2: Validación de doble conteo
            if (venta.MovimientoRegistrado)
            {
                TempData["Error"] = "Esta venta ya fue registrada en Finanzas. No se puede cobrar de nuevo.";
                return RedirectToPage("/Finanzas/Cobros/Index");
            }

            Venta = venta;
            // Solo la factura vigente: un cobro no puede tocar una anulada (la dejaba «Pagada»)
            Factura = _context.Facturas
                .Where(f => f.VentaId == ventaId && f.Estado != "Anulada")
                .OrderByDescending(f => f.Id)
                .FirstOrDefault();
            Cliente = venta.ClienteId.HasValue
                ? _context.Clientes.FirstOrDefault(c => c.Id == venta.ClienteId.Value) ?? new Cliente()
                : new Cliente();
            SaldoPendiente = venta.SaldoPendiente;

            CargarCatalogos();

            Input = new InputModel
            {
                Monto = venta.SaldoPendiente,
                MetodoPagoId = MetodosPago.FirstOrDefault()?.Id ?? 1,
                CuentaId = CuentasCaja.FirstOrDefault()?.Id ?? 0
            };

            return Page();
        }

        public IActionResult OnPost(int ventaId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "Cobros", "editar"))
            {
                TempData["Error"] = "No tienes permiso.";
                return RedirectToPage("/Finanzas/Cobros/Index");
            }

            var venta = _context.Ventas.FirstOrDefault(v => v.Id == ventaId);
            if (venta == null || !venta.EsCredito || venta.SaldoPendiente <= 0 || venta.Estado == "Anulada" || !venta.ClienteId.HasValue)
            {
                TempData["Error"] = "Venta no encontrada, anulada, sin cliente o ya fue cobrada.";
                return RedirectToPage("/Finanzas/Cobros/Index");
            }

            // 🔒 PUNTO 2: Validación de doble conteo (POST)
            if (venta.MovimientoRegistrado)
            {
                TempData["Error"] = "Esta venta ya fue registrada en Finanzas. No se puede cobrar de nuevo.";
                return RedirectToPage("/Finanzas/Cobros/Index");
            }

            Venta = venta;
            // Solo la factura vigente: un cobro no puede tocar una anulada (la dejaba «Pagada»)
            Factura = _context.Facturas
                .Where(f => f.VentaId == ventaId && f.Estado != "Anulada")
                .OrderByDescending(f => f.Id)
                .FirstOrDefault();
            Cliente = venta.ClienteId.HasValue
                ? _context.Clientes.FirstOrDefault(c => c.Id == venta.ClienteId.Value) ?? new Cliente()
                : new Cliente();
            SaldoPendiente = venta.SaldoPendiente;

            CargarCatalogos();

            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, DateTime.Today);
            if (!periodoOk)
            {
                ModelState.AddModelError(string.Empty, periodoError!);
                return Page();
            }

            if (!ModelState.IsValid) return Page();

            Input.Monto = Math.Round(Input.Monto, 2, MidpointRounding.AwayFromZero);

            if (Input.Monto <= 0)
            {
                ModelState.AddModelError("Input.Monto", "El monto debe ser mayor a 0.");
                return Page();
            }

            if (Input.Monto > venta.SaldoPendiente)
            {
                ModelState.AddModelError("Input.Monto", "El monto no puede ser mayor al saldo pendiente.");
                return Page();
            }

            var cuenta = _context.CuentasFinancieras.FirstOrDefault(c => c.Id == Input.CuentaId);
            if (cuenta == null || !cuenta.Activa)
            {
                ModelState.AddModelError("Input.CuentaId", "Cuenta no encontrada o inactiva.");
                return Page();
            }

            // 🔒 Defensa: no permitir cobrar en CxC (debe ser caja o banco real)
            if (cuenta.Tipo == "Caja" && cuenta.Subtipo == "CuentasPorCobrar")
            {
                ModelState.AddModelError("Input.CuentaId", "No puedes cobrar en la cuenta de Cuentas por Cobrar. Selecciona una caja o banco real.");
                return Page();
            }

            var metodoPago = _context.MetodosPago.FirstOrDefault(m => m.Id == Input.MetodoPagoId && m.Activo);
            if (metodoPago == null)
            {
                ModelState.AddModelError("Input.MetodoPagoId", "Método de pago no válido.");
                return Page();
            }

            // Si es efectivo pero el método se llama distinto (p.ej. "Contado"), se rotula "Efectivo" para el arqueo de caja
            var formaPago = FormaPagoHelper.Rotulo(metodoPago);

            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // 0. Descuento atómico del saldo de la venta: dos cobros simultáneos no pueden cobrar de más
                if (_context.Database.IsRelational())
                {
                    var montoCobro = Input.Monto;
                    var filas = _context.Ventas
                        .Where(v => v.Id == venta.Id && v.Estado != "Anulada" && v.SaldoPendiente >= montoCobro)
                        .ExecuteUpdate(u => u.SetProperty(v => v.SaldoPendiente, v => v.SaldoPendiente - montoCobro));
                    if (filas == 0)
                    {
                        transaction.Rollback();
                        TempData["Error"] = "El saldo de la venta cambió mientras registrabas el cobro. Revisa el saldo e inténtalo de nuevo.";
                        return RedirectToPage("/Finanzas/Cobros/Index");
                    }
                    _context.Entry(venta).Reload();
                }
                else
                {
                    venta.SaldoPendiente -= Input.Monto;
                }

                // 1. Movimiento de ingreso real
                var (mov, error) = MovimientoAutomaticoHelper.RegistrarIngresoCobro(
                    _context,
                    venta.Id,
                    venta.Numero,
                    Input.Monto,
                    currentUser!.Id,
                    formaPago: formaPago,
                    cuentaId: Input.CuentaId
                );

                if (mov == null)
                {
                    transaction.Rollback();
                    TempData["Error"] = error ?? "No se pudo registrar el cobro.";
                    return RedirectToPage("/Finanzas/Cobros/Index");
                }

                // 2. Registrar pago
                var pago = new Pago
                {
                    VentaId = venta.Id,
                    FacturaId = Factura?.Id,
                    ClienteId = venta.ClienteId!.Value,
                    Fecha = DateTime.Now,
                    Monto = Input.Monto,
                    MetodoPagoId = Input.MetodoPagoId,
                    Referencia = Input.Referencia,
                    UsuarioCreoId = currentUser.Id
                };
                _context.Pagos.Add(pago);

                // 3. Actualizar saldo de la venta
                if (venta.SaldoPendiente < 0) venta.SaldoPendiente = 0;

                // 4. Actualizar factura si existe
                if (Factura != null)
                {
                    Factura.Saldo -= Input.Monto;
                    if (Factura.Saldo < 0) Factura.Saldo = 0;
                    Factura.Estado = Factura.Saldo == 0 ? "Pagada" : "PagadaParcial";
                }

                _context.SaveChanges();
                transaction.Commit();

                ActividadHelper.Registrar(
                    _context,
                    currentUser.Id,
                    "Cobro de venta al crédito",
                    $"Cobró L. {Input.Monto:N2} de la venta {venta.Numero}. Saldo restante: L. {venta.SaldoPendiente:N2}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["Success"] = $"Cobro de L. {Input.Monto:N2} registrado. Saldo restante: L. {venta.SaldoPendiente:N2}";
                return RedirectToPage("/Finanzas/Cobros/Index");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["Error"] = $"Error al registrar el cobro: {ex.Message}";
                return RedirectToPage("/Finanzas/Cobros/Index");
            }
        }

        private void CargarCatalogos()
        {
            MetodosPago = _context.MetodosPago.Where(m => m.Activo).OrderBy(m => m.Nombre).ToList();
            if (MetodosPago.Count == 0)
            {
                MetodosPago = new List<MetodoPago>
                {
                    new MetodoPago { Id = 1, Nombre = "Efectivo", Tipo = "Efectivo", Activo = true }
                };
            }

            CuentasCaja = _context.CuentasFinancieras
                .Where(c => c.Activa
                         && (c.Tipo == "Caja" || c.Tipo == "Banco")
                         && (c.Tipo != "Caja" || c.Subtipo == null || c.Subtipo != "CuentasPorCobrar"))
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToList();

            var apertura = _context.AperturasCaja.FirstOrDefault(a => a.Activa);
            if (apertura != null)
            {
                var cuentaApertura = CuentasCaja.FirstOrDefault(c => c.Id == apertura.CuentaId);
                if (cuentaApertura != null)
                {
                    CuentasCaja.Remove(cuentaApertura);
                    CuentasCaja.Insert(0, cuentaApertura);
                }
            }
        }
    }
}