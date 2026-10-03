using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Movimientos
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

        public List<CuentaFinanciera> Cuentas { get; set; } = new();
        public List<CategoriaFinanciera> Categorias { get; set; } = new();
        public string NumeroPreview { get; set; } = "";

        public class InputModel
        {
            [Required]
            public string Tipo { get; set; } = "Egreso";

            public DateTime Fecha { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "Debes seleccionar una cuenta")]
            public int CuentaId { get; set; }

            public int? CuentaDestinoId { get; set; }

            public int? CategoriaId { get; set; }

            [Required(ErrorMessage = "El monto es obligatorio")]
            [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
            public decimal Monto { get; set; }

            [Required(ErrorMessage = "El concepto es obligatorio")]
            [StringLength(300)]
            public string Concepto { get; set; } = "";

            [StringLength(100)]
            public string? Referencia { get; set; }

            [StringLength(30)]
            public string? FormaPago { get; set; }

            [StringLength(500)]
            public string? Notas { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "MovimientosCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "MovimientosCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            CargarDatos();

            // Validaciones específicas por tipo
            if (Input.Tipo != "Transferencia" && !Input.CategoriaId.HasValue)
            {
                ModelState.AddModelError("Input.CategoriaId", "Debes seleccionar una categoría");
            }

            if (Input.Tipo == "Transferencia")
            {
                if (!Input.CuentaDestinoId.HasValue)
                {
                    ModelState.AddModelError("Input.CuentaDestinoId", "Debes seleccionar la cuenta destino");
                }
                else if (Input.CuentaDestinoId == Input.CuentaId)
                {
                    ModelState.AddModelError("Input.CuentaDestinoId", "La cuenta destino debe ser diferente a la origen");
                }
            }

            if (!ModelState.IsValid) return Page();

            // Verificar saldo suficiente para egresos y transferencias
            if (Input.Tipo == "Egreso" || Input.Tipo == "Transferencia")
            {
                var cuentaOrigen = _context.CuentasFinancieras.FirstOrDefault(c => c.Id == Input.CuentaId);
                if (cuentaOrigen != null && cuentaOrigen.SaldoActual < Input.Monto)
                {
                    // Advertencia, no bloqueo. Algunas empresas permiten sobregiros.
                    TempData["Warning"] = $"Atención: el saldo de '{cuentaOrigen.Nombre}' quedará en negativo (L. {(cuentaOrigen.SaldoActual - Input.Monto):N2})";
                }
            }

            var numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "MovimientoFinanciero");
            var monedaDefecto = _context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;

            var mov = new MovimientoFinanciero
            {
                Numero = numero,
                Tipo = Input.Tipo,
                Fecha = Input.Fecha,
                CuentaId = Input.CuentaId,
                CuentaDestinoId = Input.Tipo == "Transferencia" ? Input.CuentaDestinoId : null,
                CategoriaId = Input.Tipo == "Transferencia" ? null : Input.CategoriaId,
                Monto = Input.Monto,
                MonedaId = monedaDefecto,
                TipoCambio = 1,
                Concepto = Input.Concepto,
                Referencia = Input.Referencia,
                FormaPago = Input.FormaPago,
                Origen = "Manual",
                EsAutomatico = false,
                Notas = Input.Notas,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = currentUser?.Id,
                EmpresaId = 1
            };

            _context.MovimientosFinancieros.Add(mov);
            _context.SaveChanges();

            // Aplicar saldo
            SaldoHelper.Aplicar(_context, mov);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear movimiento financiero",
                $"Registró {mov.Tipo} {mov.Numero} por L. {mov.Monto:N2} — {mov.Concepto}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Movimiento {mov.Numero} registrado";
            return RedirectToPage("/Finanzas/Movimientos/Index");
        }

        private void CargarDatos()
        {
            Cuentas = _context.CuentasFinancieras
                .Where(c => c.Activa)
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToList();

            Categorias = _context.CategoriasFinancieras
                .Where(c => c.Activa)
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToList();

            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "MovimientoFinanciero");
        }
    }
}