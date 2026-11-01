using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.PlanCuentas
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

        public List<PlanCuenta> CuentasPadre { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

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

            public bool EsMovimiento { get; set; }

            [StringLength(300)]
            public string? Descripcion { get; set; }

            public bool Activa { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "PlanCuentas", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/PlanCuentas/Index");
            }

            var cuenta = _context.PlanCuentas.FirstOrDefault(c => c.Id == id);
            if (cuenta == null)
            {
                TempData["Error"] = "Cuenta no encontrada";
                return RedirectToPage("/Finanzas/PlanCuentas/Index");
            }

            CargarDatos(id);

            Input = new InputModel
            {
                Id = cuenta.Id,
                Codigo = cuenta.Codigo,
                Nombre = cuenta.Nombre,
                Tipo = cuenta.Tipo,
                CodigoPadre = cuenta.CodigoPadre,
                EsMovimiento = cuenta.EsMovimiento,
                Descripcion = cuenta.Descripcion,
                Activa = cuenta.Activa
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "PlanCuentas", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/PlanCuentas/Index");
            }

            CargarDatos(Input.Id);

            if (!ModelState.IsValid) return Page();

            var cuenta = _context.PlanCuentas.FirstOrDefault(c => c.Id == Input.Id);
            if (cuenta == null)
            {
                TempData["Error"] = "Cuenta no encontrada";
                return RedirectToPage("/Finanzas/PlanCuentas/Index");
            }

            // Validar código único (excluyendo la misma cuenta)
            if (_context.PlanCuentas.Any(c => c.Codigo == Input.Codigo && c.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Codigo", "Ya existe otra cuenta con este código");
                return Page();
            }

            // Validar que el código padre exista y no sea ella misma
            if (!string.IsNullOrWhiteSpace(Input.CodigoPadre))
            {
                if (Input.CodigoPadre == cuenta.Codigo)
                {
                    ModelState.AddModelError("Input.CodigoPadre", "Una cuenta no puede ser su propio padre");
                    return Page();
                }

                var padreExiste = _context.PlanCuentas.Any(c => c.Codigo == Input.CodigoPadre);
                if (!padreExiste)
                {
                    ModelState.AddModelError("Input.CodigoPadre", "El código de cuenta padre no existe");
                    return Page();
                }
            }

            // No se puede cambiar el codigo ni el tipo de una cuenta en uso (rompe asientos y reportes)
            var codigoNuevo = Input.Codigo.Trim();
            if (codigoNuevo != cuenta.Codigo || Input.Tipo != cuenta.Tipo)
            {
                var bloqueo = PlanCuentaProteccion.MotivoBloqueoCodigo(_context, cuenta);
                if (bloqueo != null)
                {
                    ModelState.AddModelError("Input.Codigo",
                        $"No se puede cambiar el código ni el tipo de esta cuenta porque {bloqueo}.");
                    return Page();
                }
            }

            // Una cuenta con asientos no puede dejar de ser de movimiento
            if (!Input.EsMovimiento && cuenta.EsMovimiento &&
                _context.DetallesAsiento.Any(d => d.PlanCuentaId == cuenta.Id))
            {
                ModelState.AddModelError("Input.EsMovimiento", "Esta cuenta ya tiene asientos: debe seguir siendo de movimiento.");
                return Page();
            }

            var nivel = Input.Codigo.Split('.').Length;
            var naturaleza = (Input.Tipo == "Activo" || Input.Tipo == "Costo" || Input.Tipo == "Gasto")
                ? "Deudora"
                : "Acreedora";

            var cambiosCuenta = CambiosHelper.Describir(
                ("Código", cuenta.Codigo, Input.Codigo.Trim()),
                ("Nombre", cuenta.Nombre, Input.Nombre.Trim()),
                ("Tipo", cuenta.Tipo, Input.Tipo),
                ("Padre", cuenta.CodigoPadre, string.IsNullOrWhiteSpace(Input.CodigoPadre) ? null : Input.CodigoPadre.Trim()),
                ("Movimiento", cuenta.EsMovimiento, Input.EsMovimiento),
                ("Activa", cuenta.Activa, Input.Activa));

            cuenta.Codigo = Input.Codigo.Trim();
            cuenta.Nombre = Input.Nombre.Trim();
            cuenta.Tipo = Input.Tipo;
            cuenta.CodigoPadre = string.IsNullOrWhiteSpace(Input.CodigoPadre) ? null : Input.CodigoPadre.Trim();
            cuenta.Nivel = nivel;
            cuenta.Naturaleza = naturaleza;
            cuenta.EsMovimiento = Input.EsMovimiento;
            cuenta.Descripcion = Input.Descripcion;
            cuenta.Activa = Input.Activa;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar cuenta contable",
                $"Editó la cuenta '{cuenta.Codigo} - {cuenta.Nombre}'. " + cambiosCuenta,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cuenta '{cuenta.Codigo} - {cuenta.Nombre}' actualizada correctamente";
            return RedirectToPage("/Finanzas/PlanCuentas/Index");
        }

        private void CargarDatos(int cuentaIdActual)
        {
            CuentasPadre = _context.PlanCuentas
                .Where(c => c.Activa && c.Id != cuentaIdActual)
                .OrderBy(c => c.Codigo)
                .ToList();
        }
    }
}