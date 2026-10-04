using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.PagosProveedor
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<Models.OrdenCompra> OrdenesPorPagar { get; set; } = new();
        public List<Proveedor> Proveedores { get; set; } = new();
        public List<MetodoPago> MetodosPago { get; set; } = new();
        public List<Moneda> Monedas { get; set; } = new();

        public string NumeroPreview { get; set; } = "";

        public class InputModel
        {
            [Required(ErrorMessage = "Debes seleccionar una orden de compra")]
            public int OrdenCompraId { get; set; }

            public int ProveedorId { get; set; }

            [Required(ErrorMessage = "El monto es obligatorio")]
            [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
            public decimal Monto { get; set; }

            [Required(ErrorMessage = "El método de pago es obligatorio")]
            public int MetodoPagoId { get; set; }

            [StringLength(100)]
            public string? Referencia { get; set; }

            [Required]
            public int MonedaId { get; set; } = 1;

            [StringLength(500)]
            public string? Notas { get; set; }

            public bool RegistrarEnFinanzas { get; set; } = true;
        }

        public IActionResult OnGet(int? ordenId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "PagosCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para registrar pagos";
                return RedirectToPage("/PagosProveedor/Index");
            }

            CargarDatos();

            if (ordenId.HasValue)
            {
                Input.OrdenCompraId = ordenId.Value;
                var orden = _context.OrdenesCompra.FirstOrDefault(o => o.Id == ordenId.Value);
                if (orden != null)
                {
                    Input.ProveedorId = orden.ProveedorId;
                    Input.Monto = orden.Saldo;
                    Input.MonedaId = orden.MonedaId;
                }
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "PagosCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para registrar pagos";
                return RedirectToPage("/PagosProveedor/Index");
            }

            CargarDatos();

            if (!ModelState.IsValid)
                return Page();

            var orden = _context.OrdenesCompra.FirstOrDefault(o => o.Id == Input.OrdenCompraId);
            if (orden == null)
            {
                ModelState.AddModelError("Input.OrdenCompraId", "Orden no encontrada");
                return Page();
            }

            if (orden.Estado != "Recibida" && orden.Estado != "RecibidaParcial")
            {
                ModelState.AddModelError(string.Empty,
                    $"Solo se pueden pagar órdenes en estado Recibida o Recibida Parcial (estado actual: {orden.Estado})");
                return Page();
            }

            if (orden.Saldo <= 0)
            {
                ModelState.AddModelError(string.Empty, "Esta orden ya está completamente pagada");
                return Page();
            }

            if (Input.Monto > orden.Saldo)
            {
                ModelState.AddModelError("Input.Monto",
                    $"El monto no puede ser mayor al saldo pendiente (L. {orden.Saldo:N2})");
                return Page();
            }

            // ===== VALIDACIÓN: período contable cerrado =====
            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, DateTime.Today);
            if (!periodoOk)
            {
                ModelState.AddModelError(string.Empty, periodoError!);
                return Page();
            }

            var numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "PagoProveedor");

            var pago = new Models.PagoProveedor
            {
                Numero = numero,
                ProveedorId = orden.ProveedorId,
                OrdenCompraId = orden.Id,
                Fecha = DateTime.Now,
                Monto = Input.Monto,
                MetodoPagoId = Input.MetodoPagoId,
                Referencia = Input.Referencia,
                MonedaId = Input.MonedaId,
                TipoCambio = 1,
                Notas = Input.Notas,
                UsuarioCreoId = currentUser?.Id,
                EmpresaId = 1
            };

            _context.PagosProveedor.Add(pago);

            orden.Saldo -= Input.Monto;
            if (orden.Saldo <= 0)
            {
                orden.Saldo = 0;
                if (orden.Estado == "Recibida")
                {
                    orden.Estado = "Pagada";
                }
            }

            _context.SaveChanges();

            // REGISTRO AUTOMÁTICO EN FINANZAS
            string? movimientoError = null;

            if (Input.RegistrarEnFinanzas)
            {
                try
                {
                    var proveedor = _context.Proveedores.FirstOrDefault(p => p.Id == orden.ProveedorId);
                    var metodoPago = _context.MetodosPago.FirstOrDefault(m => m.Id == Input.MetodoPagoId);

                    var mov = MovimientoAutomaticoHelper.RegistrarEgresoPagoProveedor(
                        _context,
                        pago.Id,
                        pago.Numero ?? "—",
                        pago.Monto,
                        currentUser?.Id ?? 0,
                        nombreProveedor: proveedor?.Nombre ?? "—",
                        formaPago: metodoPago?.Nombre
                    );

                    if (mov == null)
                    {
                        movimientoError = "No se pudo registrar el movimiento financiero (revisa la configuración de cuentas y categorías)";
                    }
                }
                catch (Exception ex)
                {
                    movimientoError = ex.Message;
                    Console.WriteLine($"[PagosProveedor] Error al registrar movimiento: {ex.Message}");
                }
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Registrar pago a proveedor",
                $"Registró pago {pago.Numero} por L. {pago.Monto:N2} a la orden {orden.Numero}. Saldo restante: L. {orden.Saldo:N2}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            if (movimientoError != null)
            {
                TempData["Warning"] = $"Pago registrado. {movimientoError}";
            }
            else
            {
                TempData["Success"] = $"Pago {pago.Numero} registrado correctamente. Saldo restante: L. {orden.Saldo:N2}";
            }

            return RedirectToPage("/PagosProveedor/Index");
        }

        private void CargarDatos()
        {
            OrdenesPorPagar = _context.OrdenesCompra
                .Where(o => (o.Estado == "Recibida" || o.Estado == "RecibidaParcial") && o.Saldo > 0)
                .OrderBy(o => o.Fecha)
                .ToList();

            Proveedores = _context.Proveedores.ToList();
            MetodosPago = _context.MetodosPago.Where(m => m.Activo).ToList();
            Monedas = _context.Monedas.Where(m => m.Activa).OrderBy(m => m.Codigo).ToList();

            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "PagoProveedor");
        }
    }
}