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

            if (!ModelState.IsValid) return Page();

            var mov = _context.MovimientosFinancieros.FirstOrDefault(m => m.Id == Input.Id);
            if (mov == null || mov.Estado != "Activo" || mov.Origen != "Manual")
            {
                TempData["Error"] = "No se puede editar este movimiento";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            // Validar que no se intente editar un movimiento de un mes cerrado
            var (periodoOkViejo, periodoErrorViejo) = CierreContableHelper.ValidarFecha(_context, mov.Fecha);
            if (!periodoOkViejo)
            {
                TempData["Error"] = $"No puedes editar este movimiento porque pertenece a un período cerrado. {periodoErrorViejo}";
                return RedirectToPage("/Finanzas/Movimientos/Index");
            }

            // Revertir saldo anterior
            SaldoHelper.Revertir(_context, mov);

            // Aplicar cambios
            mov.Fecha = Input.Fecha;
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