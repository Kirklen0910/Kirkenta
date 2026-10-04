using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Categorias
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

        public List<PlanCuenta> CuentasIngreso { get; set; } = new();
        public List<PlanCuenta> CuentasEgreso { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El tipo es obligatorio")]
            public string Tipo { get; set; } = "Egreso";

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(100)]
            public string Nombre { get; set; } = string.Empty;

            [StringLength(300)]
            public string? Descripcion { get; set; }

            [StringLength(30)]
            public string? CuentaContable { get; set; }

            public int? PlanCuentaId { get; set; }

            [StringLength(20)]
            public string Color { get; set; } = "#6b7280";

            public bool Activa { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CategoriasCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Categorias/Index");
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CategoriasCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Categorias/Index");
            }

            CargarDatos();

            if (!ModelState.IsValid) return Page();

            if (_context.CategoriasFinancieras.Any(c => c.Tipo == Input.Tipo && c.Nombre == Input.Nombre))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe una categoría con este nombre para este tipo");
                return Page();
            }

            // Validar que la cuenta del plan pertenezca al tipo correcto
            if (Input.PlanCuentaId.HasValue)
            {
                var cuenta = _context.PlanCuentas.FirstOrDefault(p => p.Id == Input.PlanCuentaId.Value);
                if (cuenta == null)
                {
                    ModelState.AddModelError("Input.PlanCuentaId", "La cuenta contable seleccionada no existe");
                    return Page();
                }

                if (!cuenta.Activa)
                {
                    ModelState.AddModelError("Input.PlanCuentaId", "La cuenta contable seleccionada está inactiva");
                    return Page();
                }

                if (!cuenta.EsMovimiento)
                {
                    ModelState.AddModelError("Input.PlanCuentaId", "La cuenta contable seleccionada no permite movimientos directos");
                    return Page();
                }

                // Validar coherencia tipo Categoria vs tipo PlanCuenta
                var tiposValidos = Input.Tipo == "Ingreso"
                    ? new[] { "Ingreso" }
                    : new[] { "Costo", "Gasto" };

                if (!tiposValidos.Contains(cuenta.Tipo))
                {
                    ModelState.AddModelError("Input.PlanCuentaId",
                        $"El tipo de la categoría ({Input.Tipo}) no coincide con el tipo de la cuenta ({cuenta.Tipo})");
                    return Page();
                }
            }

            var categoria = new CategoriaFinanciera
            {
                Tipo = Input.Tipo,
                Nombre = Input.Nombre,
                Descripcion = Input.Descripcion,
                CuentaContable = Input.CuentaContable,
                PlanCuentaId = Input.PlanCuentaId,
                Color = Input.Color,
                EsSistema = false,
                Activa = Input.Activa,
                FechaCreacion = DateTime.Now,
                EmpresaId = 1
            };

            _context.CategoriasFinancieras.Add(categoria);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear categoría financiera",
                $"Creó la categoría '{categoria.Nombre}' ({categoria.Tipo})",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Categoría '{categoria.Nombre}' creada";
            return RedirectToPage("/Finanzas/Categorias/Index");
        }

        private void CargarDatos()
        {
            CuentasIngreso = _context.PlanCuentas
                .Where(c => c.Activa && c.EsMovimiento && c.Tipo == "Ingreso")
                .OrderBy(c => c.Codigo)
                .ToList();

            CuentasEgreso = _context.PlanCuentas
                .Where(c => c.Activa && c.EsMovimiento && (c.Tipo == "Costo" || c.Tipo == "Gasto"))
                .OrderBy(c => c.Codigo)
                .ToList();
        }
    }
}