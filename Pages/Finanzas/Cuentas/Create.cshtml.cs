using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Cuentas
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

        public List<Moneda> Monedas { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El código es obligatorio")]
            [StringLength(20)]
            public string Codigo { get; set; } = string.Empty;

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(100)]
            public string Nombre { get; set; } = string.Empty;

            [Required]
            public string Tipo { get; set; } = "Caja";

            public string? Subtipo { get; set; }

            [StringLength(50)]
            public string? NumeroCuenta { get; set; }

            [StringLength(100)]
            public string? Banco { get; set; }

            [Required]
            public int MonedaId { get; set; } = 1;

            public decimal SaldoInicial { get; set; } = 0;

            [StringLength(30)]
            public string? CuentaContable { get; set; }

            [StringLength(300)]
            public string? Descripcion { get; set; }

            [StringLength(150)]
            public string? Responsable { get; set; }

            public bool Activa { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CuentasCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear cuentas";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }

            Monedas = _context.Monedas.Where(m => m.Activa).OrderBy(m => m.Codigo).ToList();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CuentasCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear cuentas";
                return RedirectToPage("/Finanzas/Cuentas/Index");
            }

            Monedas = _context.Monedas.Where(m => m.Activa).OrderBy(m => m.Codigo).ToList();

            if (!ModelState.IsValid) return Page();

            if (_context.CuentasFinancieras.Any(c => c.Codigo == Input.Codigo))
            {
                ModelState.AddModelError("Input.Codigo", "Ya existe una cuenta con este código");
                return Page();
            }

            if (Input.Tipo == "Banco" && string.IsNullOrWhiteSpace(Input.Banco))
            {
                ModelState.AddModelError("Input.Banco", "El nombre del banco es obligatorio");
                return Page();
            }

            var cuenta = new CuentaFinanciera
            {
                Codigo = Input.Codigo,
                Nombre = Input.Nombre,
                Tipo = Input.Tipo,
                Subtipo = Input.Tipo == "Caja" ? Input.Subtipo : null,
                NumeroCuenta = Input.NumeroCuenta,
                Banco = Input.Banco,
                MonedaId = Input.MonedaId,
                SaldoInicial = Input.SaldoInicial,
                SaldoActual = Input.SaldoInicial,
                CuentaContable = Input.CuentaContable,
                Descripcion = Input.Descripcion,
                Responsable = Input.Responsable,
                Activa = Input.Activa,
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = currentUser?.Id,
                EmpresaId = 1
            };

            _context.CuentasFinancieras.Add(cuenta);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear cuenta financiera",
                $"Creó la cuenta '{cuenta.Nombre}' ({cuenta.Codigo}) con saldo inicial L. {cuenta.SaldoInicial:N2}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cuenta '{cuenta.Nombre}' creada correctamente";
            return RedirectToPage("/Finanzas/Cuentas/Index");
        }
    }
}