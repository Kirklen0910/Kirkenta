using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Movimientos
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<CuentaFinanciera> Cuentas { get; set; } = new();
        public List<CategoriaFinanciera> Categorias { get; set; } = new();
        public string Numero { get; set; } = "";

        public class InputModel
        {
            public int Id { get; set; }
            public string Tipo { get; set; } = "Egreso";
            public DateTime Fecha { get; set; }
            public int CuentaId { get; set; }
            public int? CategoriaId { get; set; }
            public decimal Monto { get; set; }
            public string Concepto { get; set; } = "";
            public string? Referencia { get; set; }
            public string? FormaPago { get; set; }
            public string? Notas { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "MovimientosEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            var mov = _context.MovimientosFinancieros.FirstOrDefault(m => m.Id == id);
            if (mov == null)
            {
                TempData["Error"] = "Movimiento no encontrado";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            if (mov.Estado != "Activo")
            {
                TempData["Error"] = "No se puede editar un movimiento anulado";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            if (mov.Origen != "Manual")
            {
                TempData["Error"] = "Solo se pueden editar movimientos manuales";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            if (mov.Tipo == "Transferencia")
            {
                TempData["Error"] = "Las transferencias no se pueden editar. Anula y crea una nueva.";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            CargarDatos();
            Numero = mov.Numero;

            Input = new InputModel
            {
                Id = mov.Id,
                Tipo = mov.Tipo,
                Fecha = mov.Fecha,
                CuentaId = mov.CuentaId,
                CategoriaId = mov.CategoriaId,
                Monto = mov.Monto,
                Concepto = mov.Concepto,
                Referencia = mov.Referencia,
                FormaPago = mov.FormaPago,
                Notas = mov.Notas
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "MovimientosEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            CargarDatos();

            // ===== VALIDACIÓN: período contable cerrado =====
            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, Input.Fecha);
            if (!periodoOk)
            {
                ModelState.AddModelError(string.Empty, periodoError!);
            }

            Input.Monto = Math.Round(Input.Monto, 2, MidpointRounding.AwayFromZero);
            if (Input.Monto <= 0)
                ModelState.AddModelError("Input.Monto", "El monto debe ser mayor a 0");
            if (Input.Fecha.Date > DateTime.Today)
                ModelState.AddModelError("Input.Fecha", "No se pueden registrar movimientos con fecha futura");
            if (string.IsNullOrWhiteSpace(Input.Concepto))
                ModelState.AddModelError("Input.Concepto", "El concepto es obligatorio");
            if (!Input.CategoriaId.HasValue || !_context.CategoriasFinancieras.Any(c => c.Id == Input.CategoriaId.Value && c.Activa))
                ModelState.AddModelError("Input.CategoriaId", "Selecciona una categoría activa");
            if (!_context.CuentasFinancieras.Any(c => c.Id == Input.CuentaId && c.Activa))
                ModelState.AddModelError("Input.CuentaId", "Selecciona una cuenta activa");

            if (!ModelState.IsValid) return Page();

            var mov = _context.MovimientosFinancieros.FirstOrDefault(m => m.Id == Input.Id);
            if (mov == null || mov.Estado != "Activo" || mov.Origen != "Manual")
            {
                TempData["Error"] = "No se puede editar este movimiento";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            var catEdit = _context.CategoriasFinancieras.FirstOrDefault(c => c.Id == Input.CategoriaId!.Value);
            if (catEdit == null || catEdit.Tipo != mov.Tipo)
            {
                TempData["Error"] = $"La categoría debe ser de tipo {mov.Tipo}";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            // Una categoría solo contable no mueve saldo: el dinero del movimiento desaparecería de la cuenta
            if (SaldoHelper.EsSoloContable(_context, new MovimientoFinanciero { Tipo = mov.Tipo, CategoriaId = catEdit.Id }))
            {
                TempData["Error"] = "Esa categoría es solo contable (costo de ventas): no se puede usar en un movimiento manual";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            // Validar que no se intente editar un movimiento de un mes cerrado
            var (periodoOkViejo, periodoErrorViejo) = CierreContableHelper.ValidarFecha(_context, mov.Fecha);
            if (!periodoOkViejo)
            {
                TempData["Error"] = $"No puedes editar este movimiento porque pertenece a un período cerrado. {periodoErrorViejo}";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            // Todo o nada: movimiento + saldo + documento origen en una sola transacción
            using var tx = _context.Database.BeginTransaction();

            // Revertir() solo actúa sobre movimientos no activos: se marca temporalmente,
            // se quita el efecto VIEJO (cuenta, monto y categoría de antes) y se reactiva.
            mov.Estado = "Anulado";
            SaldoHelper.Revertir(_context, mov);
            mov.Estado = "Activo";

            // Aplicar cambios
            // El formulario solo trae el día: si no cambia, se conserva la hora (define a qué turno de caja pertenece)
            mov.Fecha = Input.Fecha.Date == mov.Fecha.Date ? mov.Fecha : FormaPagoHelper.FechaReal(Input.Fecha);
            mov.CuentaId = Input.CuentaId;
            mov.CategoriaId = Input.CategoriaId;
            mov.Monto = Input.Monto;
            mov.Concepto = Input.Concepto;
            mov.Referencia = Input.Referencia;
            mov.FormaPago = Input.FormaPago;
            mov.Notas = Input.Notas;

            _context.SaveChanges();

            // Aplicar nuevo saldo
            SaldoHelper.Aplicar(_context, mov);
            _context.SaveChanges();
            tx.Commit();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar movimiento financiero",
                $"Editó el movimiento {mov.Numero}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Movimiento {mov.Numero} actualizado";
            return RedirectToPage("/Finanzas/Movimientos/Index");
        }

        private void CargarDatos()
        {
            Cuentas = _context.CuentasFinancieras.Where(c => c.Activa).OrderBy(c => c.Nombre).ToList();
            Categorias = _context.CategoriasFinancieras.Where(c => c.Activa).OrderBy(c => c.Nombre).ToList();
        }
    }
}