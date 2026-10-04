using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.PlanCuentas
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

        public List<PlanCuenta> CuentasPadre { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El código es obligatorio")]
            [StringLength(20)]
            public string Codigo { get; set; } = string.Empty;

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(150)]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "El tipo es obligatorio")]
            public string Tipo { get; set; } = "Activo";

            [StringLength(20)]
            public string? CodigoPadre { get; set; }

            public bool EsMovimiento { get; set; } = true;

            [StringLength(300)]
            public string? Descripcion { get; set; }

            public bool Activa { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "PlanCuentas", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/PlanCuentas/Index");
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "PlanCuentas", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/PlanCuentas/Index");
            }

            CargarDatos();

            if (!ModelState.IsValid) return Page();

            // Validar código único
            if (_context.PlanCuentas.Any(c => c.Codigo == Input.Codigo))
            {
                ModelState.AddModelError("Input.Codigo", "Ya existe una cuenta con este código");
                return Page();
            }

            // Validar que el código padre exista si se especifica
            if (!string.IsNullOrWhiteSpace(Input.CodigoPadre))
            {
                var padreExiste = _context.PlanCuentas.Any(c => c.Codigo == Input.CodigoPadre);
                if (!padreExiste)
                {
                    ModelState.AddModelError("Input.CodigoPadre", "El código de cuenta padre no existe");
                    return Page();
                }
            }

            var nivel = Input.Codigo.Split('.').Length;
            var naturaleza = (Input.Tipo == "Activo" || Input.Tipo == "Costo" || Input.Tipo == "Gasto")
                ? "Deudora"
                : "Acreedora";

            var cuenta = new PlanCuenta
            {
                Codigo = Input.Codigo.Trim(),
                Nombre = Input.Nombre.Trim(),
                Tipo = Input.Tipo,
                CodigoPadre = string.IsNullOrWhiteSpace(Input.CodigoPadre) ? null : Input.CodigoPadre.Trim(),
                Nivel = nivel,
                Naturaleza = naturaleza,
                EsMovimiento = Input.EsMovimiento,
                Descripcion = Input.Descripcion,
                Activa = Input.Activa,
                FechaCreacion = DateTime.Now,
                EmpresaId = 1
            };

            _context.PlanCuentas.Add(cuenta);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear cuenta contable",
                $"Creó la cuenta '{cuenta.Codigo} - {cuenta.Nombre}' ({cuenta.Tipo})",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cuenta '{cuenta.Codigo} - {cuenta.Nombre}' creada correctamente";
            return RedirectToPage("/Finanzas/PlanCuentas/Index");
        }

        private void CargarDatos()
        {
            CuentasPadre = _context.PlanCuentas
                .Where(c => c.Activa && !c.EsMovimiento)
                .OrderBy(c => c.Codigo)
                .ToList();
        }
    }
}