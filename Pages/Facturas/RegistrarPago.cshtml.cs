using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Facturas
{
    public class RegistrarPagoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegistrarPagoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Factura Factura { get; set; } = new();
        public List<MetodoPago> MetodosPago { get; set; } = new();

        public class InputModel
        {
            public int FacturaId { get; set; }

            [Required(ErrorMessage = "El método de pago es obligatorio")]
            public int MetodoPagoId { get; set; }

            [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
            public decimal Monto { get; set; }

            public string? Referencia { get; set; }
            public string? Notas { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Facturas", "editar")))
            {
                TempData["Error"] = "No tienes permiso para realizar esta acción";
                return RedirectToPage("/Index");
            }

            var factura = _context.Facturas.FirstOrDefault(f => f.Id == id);
            if (factura == null)
            {
                TempData["Error"] = "Factura no encontrada";
                return RedirectToPage("/Facturas/Index");
            }

            if (factura.Estado == "Pagada" || factura.Estado == "Anulada")
            {
                TempData["Error"] = "Esta factura no admite más pagos";
                return RedirectToPage("/Facturas/Details", new { id });
            }

            // 🔒 Las facturas que vienen de una venta a crédito se cobran en Finanzas > Cobros,
            // para que el dinero entre a la caja/banco y el saldo de la venta se mantenga igual.
            if (factura.VentaId.HasValue)
            {
                var ventaOrigen = _context.Ventas.FirstOrDefault(v => v.Id == factura.VentaId.Value);
                if (ventaOrigen != null && ventaOrigen.EsCredito)
                {
                    TempData["Error"] = "Esta factura es de una venta a crédito: regístrala desde Finanzas > Cobros para que el ingreso entre a caja.";
                    return RedirectToPage("/Finanzas/Cobros/Cobrar", new { ventaId = ventaOrigen.Id });
                }
            }

            Factura = factura;
            MetodosPago = _context.MetodosPago.Where(m => m.Activo).ToList();

            Input = new InputModel
            {
                FacturaId = factura.Id,
                Monto = factura.Saldo
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Facturas", "editar")))
            {
                TempData["Error"] = "No tienes permiso para realizar esta acción";
                return RedirectToPage("/Index");
            }

            var factura = _context.Facturas.FirstOrDefault(f => f.Id == Input.FacturaId);
            if (factura == null)
            {
                TempData["Error"] = "Factura no encontrada";
                return RedirectToPage("/Facturas/Index");
            }

            if (factura.Estado == "Pagada" || factura.Estado == "Anulada" || factura.Saldo <= 0)
            {
                TempData["Error"] = "Esta factura está anulada o ya no tiene saldo pendiente";
                return RedirectToPage("/Facturas/Details", new { id = factura.Id });
            }

            // 🔒 Las facturas que vienen de una venta a crédito se cobran en Finanzas > Cobros,
            // para que el dinero entre a la caja/banco y el saldo de la venta se mantenga igual.
            if (factura.VentaId.HasValue)
            {
                var ventaOrigen = _context.Ventas.FirstOrDefault(v => v.Id == factura.VentaId.Value);
                if (ventaOrigen != null && ventaOrigen.EsCredito)
                {
                    TempData["Error"] = "Esta factura es de una venta a crédito: regístrala desde Finanzas > Cobros para que el ingreso entre a caja.";
                    return RedirectToPage("/Finanzas/Cobros/Cobrar", new { ventaId = ventaOrigen.Id });
                }
            }

            MetodosPago = _context.MetodosPago.Where(m => m.Activo).ToList();

            if (!ModelState.IsValid)
            {
                Factura = factura;
                return Page();
            }

            Input.Monto = Math.Round(Input.Monto, 2);

            if (Input.Monto > factura.Saldo)
            {
                ModelState.AddModelError("Input.Monto", "El monto no puede ser mayor al saldo pendiente");
                Factura = factura;
                return Page();
            }

            // 🔒 FIX: validar que el período contable no esté cerrado
            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, DateTime.Today);
            if (!periodoOk)
            {
                ModelState.AddModelError(string.Empty, periodoError!);
                Factura = factura;
                return Page();
            }

            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);

            var metodo = _context.MetodosPago.FirstOrDefault(m => m.Id == Input.MetodoPagoId && m.Activo);
            if (metodo == null)
            {
                ModelState.AddModelError("Input.MetodoPagoId", "Método de pago no válido");
                Factura = factura;
                return Page();
            }

            using var tx = _context.Database.BeginTransaction();

            // Descuento atómico del saldo: dos pagos simultáneos sobre la misma factura no pueden cobrar de más
            if (_context.Database.IsRelational())
            {
                var montoPago = Input.Monto;
                var filas = _context.Facturas
                    .Where(f => f.Id == factura.Id && f.Saldo >= montoPago && f.Estado != "Anulada" && f.Estado != "Pagada")
                    .ExecuteUpdate(u => u.SetProperty(f => f.Saldo, f => f.Saldo - montoPago));
                if (filas == 0)
                {
                    tx.Rollback();
                    TempData["Error"] = "El saldo de la factura cambió mientras registrabas el pago. Revísalo e inténtalo de nuevo.";
                    return RedirectToPage("/Facturas/Details", new { id = factura.Id });
                }
                _context.Entry(factura).Reload();
            }
            else
            {
                factura.Saldo -= Input.Monto;
            }

            var pago = new Pago
            {
                FacturaId = factura.Id,
                ClienteId = factura.ClienteId,
                Fecha = DateTime.Now,
                Monto = Input.Monto,
                MetodoPagoId = Input.MetodoPagoId,
                Referencia = Input.Referencia,
                Notas = Input.Notas,
                UsuarioCreoId = currentUser?.Id
            };

            _context.Pagos.Add(pago);

            if (factura.Saldo <= 0)
            {
                factura.Saldo = 0;
                factura.Estado = "Pagada";
            }
            else
            {
                factura.Estado = "PagadaParcial";
            }

            _context.SaveChanges();

            // Facturas sin venta (de cotización o pedido): el dinero debe entrar a caja/banco en Finanzas.
            // Las facturas de una venta ya registraron su ingreso en el POS / Cobros.
            if (!factura.VentaId.HasValue)
            {
                var (mov, errorFin) = MovimientoAutomaticoHelper.RegistrarIngresoPagoFactura(
                    _context, factura.Id, factura.Numero, Input.Monto, currentUser?.Id ?? 0,
                    metodo.Nombre, metodo.Tipo);
                if (mov == null)
                {
                    tx.Rollback();
                    ModelState.AddModelError(string.Empty, $"No se pudo registrar el ingreso en Finanzas. {errorFin}");
                    _context.ChangeTracker.Clear();
                    Factura = _context.Facturas.First(f => f.Id == Input.FacturaId);
                    return Page();
                }
            }

            tx.Commit();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Registrar pago",
                $"Registró pago de L. {pago.Monto:N2} en factura {factura.Numero}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Pago de L. {pago.Monto:N2} registrado correctamente";
            return RedirectToPage("/Facturas/Details", new { id = factura.Id });
        }
    }
}