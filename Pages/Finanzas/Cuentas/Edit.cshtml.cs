using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Cuentas
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

        public List<Moneda> Monedas { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required]
            [StringLength(20)]
            public string Codigo { get; set; } = string.Empty;

            [Required]
            [StringLength(100)]
            public string Nombre { get; set; } = string.Empty;

            [Required]
            public string Tipo { get; set; } = "Caja";

            public string? Subtipo { get; set; }

            [StringLength(50)]
            public string? NumeroCuenta { get; set; }

            [StringLength(100)]
            public string? Banco { get; set; }

            public int MonedaId { get; set; } = 1;

            [StringLength(30)]
            public string? CuentaContable { get; set; }

            [StringLength(300)]
            public string? Descripcion { get; set; }

            [StringLength(150)]
            public string? Responsable { get; set; }

            public bool Activa { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CuentasEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar cuentas";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }

            Monedas = _context.Monedas.Where(m => m.Activa).OrderBy(m => m.Codigo).ToList();

            var c = _context.CuentasFinancieras.FirstOrDefault(x => x.Id == id);
            if (c?.Codigo == Kirkenta.Helpers.Finanzas.EfectivoPorDepositarHelper.CodigoCuenta)
            {
                TempData["Error"] = "«Efectivo por depositar» la maneja el sistema con los cierres de caja: no se edita ni se elimina a mano.";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }
            if (c == null)
            {
                TempData["Error"] = "Cuenta no encontrada";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }

            Input = new InputModel
            {
                Id = c.Id,
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Tipo = c.Tipo,
                Subtipo = c.Subtipo,
                NumeroCuenta = c.NumeroCuenta,
                Banco = c.Banco,
                MonedaId = c.MonedaId,
                CuentaContable = c.CuentaContable,
                Descripcion = c.Descripcion,
                Responsable = c.Responsable,
                Activa = c.Activa
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CuentasEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar cuentas";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }

            Monedas = _context.Monedas.Where(m => m.Activa).OrderBy(m => m.Codigo).ToList();

            if (!ModelState.IsValid) return Page();

            var c = _context.CuentasFinancieras.FirstOrDefault(x => x.Id == Input.Id);
            if (c?.Codigo == Kirkenta.Helpers.Finanzas.EfectivoPorDepositarHelper.CodigoCuenta)
            {
                TempData["Error"] = "«Efectivo por depositar» la maneja el sistema con los cierres de caja: no se edita ni se elimina a mano.";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }
            if (c == null)
            {
                TempData["Error"] = "Cuenta no encontrada";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }

            if (_context.CuentasFinancieras.Any(x => x.Codigo == Input.Codigo && x.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Codigo", "Ya existe otra cuenta con este código");
                return Page();
            }

            // Una caja con apertura activa no se puede desactivar: el POS y el cierre dependen de ella
            if (c.Activa && !Input.Activa && _context.AperturasCaja.Any(a => a.CuentaId == c.Id && a.Activa))
            {
                ModelState.AddModelError("Input.Activa", "No puedes desactivar una cuenta con una apertura de caja activa. Cierra la caja primero.");
                return Page();
            }

            // Cambiar el tipo o convertirla en/desde Cuentas por Cobrar descuadra el efectivo, los cierres y el balance
            var nuevoSubtipo = Input.Tipo == "Caja" ? Input.Subtipo : null;
            bool cambiaNaturaleza = c.Tipo != Input.Tipo
                || ((c.Subtipo == "CuentasPorCobrar") != (nuevoSubtipo == "CuentasPorCobrar"));
            if (cambiaNaturaleza && _context.MovimientosFinancieros.Any(m => m.CuentaId == c.Id || m.CuentaDestinoId == c.Id))
            {
                ModelState.AddModelError("Input.Tipo", "No puedes cambiar el tipo de una cuenta que ya tiene movimientos. Crea otra cuenta y transfiere el saldo.");
                return Page();
            }

            c.Codigo = Input.Codigo;
            c.Nombre = Input.Nombre;
            c.Tipo = Input.Tipo;
            c.Subtipo = Input.Tipo == "Caja" ? Input.Subtipo : null;
            c.NumeroCuenta = Input.NumeroCuenta;
            c.Banco = Input.Banco;
            c.MonedaId = Input.MonedaId;
            c.CuentaContable = Input.CuentaContable;
            c.Descripcion = Input.Descripcion;
            c.Responsable = Input.Responsable;
            c.Activa = Input.Activa;

            _context.SaveChanges();

            // ✅ Si cambió el nombre, actualizar el movimiento de apertura
            var movApertura = _context.MovimientosFinancieros
                .FirstOrDefault(m => m.Origen == "Apertura" && m.OrigenId == c.Id && m.Estado == "Activo");

            if (movApertura != null)
            {
                movApertura.Concepto = $"Saldo inicial de {c.Nombre}";
                _context.SaveChanges();
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar cuenta financiera",
                $"Editó la cuenta '{c.Nombre}' ({c.Codigo})",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cuenta '{c.Nombre}' actualizada";
            return RedirectToPage("/Finanzas/Cuentas/Index");
        }
    }
}