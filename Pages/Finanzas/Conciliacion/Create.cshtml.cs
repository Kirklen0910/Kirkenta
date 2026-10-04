using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Conciliacion
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

        public List<CuentaFinanciera> CuentasBancarias { get; set; } = new();
        public string NumeroPreview { get; set; } = "";

        public class InputModel
        {
            [Required(ErrorMessage = "Debes seleccionar una cuenta bancaria")]
            public int CuentaId { get; set; }

            [Required]
            public DateTime FechaInicio { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            [Required]
            public DateTime FechaFin { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "Debes ingresar el saldo del banco")]
            public decimal SaldoBanco { get; set; }

            [StringLength(500)]
            public string? Notas { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Conciliacion/Index");
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "ConciliacionCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Conciliacion/Index");
            }

            CargarDatos();

            if (!ModelState.IsValid) return Page();

            var (conciliacion, error) = ConciliacionHelper.Crear(
                _context,
                Input.CuentaId,
                Input.FechaInicio,
                Input.FechaFin,
                Input.SaldoBanco,
                Input.Notas,
                currentUser!.Id);

            if (error != null || conciliacion == null)
            {
                ModelState.AddModelError(string.Empty, error ?? "Error al crear la conciliación");
                return Page();
            }

            ActividadHelper.Registrar(
                _context,
                currentUser.Id,
                "Crear conciliación bancaria",
                $"Creó conciliación {conciliacion.Numero} para cuenta #{conciliacion.CuentaId}, " +
                $"período {conciliacion.FechaInicio:dd/MM/yyyy} - {conciliacion.FechaFin:dd/MM/yyyy}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Conciliación {conciliacion.Numero} creada. Las líneas del sistema ya están cargadas.";
            return RedirectToPage("/Finanzas/Conciliacion/Details", new { id = conciliacion.Id });
        }

        private void CargarDatos()
        {
            CuentasBancarias = _context.CuentasFinancieras
                .Where(c => c.Tipo == "Banco" && c.Activa)
                .OrderBy(c => c.Nombre)
                .ToList();

            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "ConciliacionBancaria");
        }
    }
}