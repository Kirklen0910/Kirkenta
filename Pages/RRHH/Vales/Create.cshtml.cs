using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Vales
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

        public List<Empleado> Empleados { get; set; } = new();
        public string NumeroPreview { get; set; } = "";

        public class InputModel
        {
            [Required(ErrorMessage = "Debes seleccionar un empleado")]
            public int EmpleadoId { get; set; }

            [Required(ErrorMessage = "El monto es obligatorio")]
            [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
            public decimal Monto { get; set; }

            [Required(ErrorMessage = "El motivo es obligatorio")]
            [StringLength(300)]
            public string Motivo { get; set; } = string.Empty;

            [Range(1, 24, ErrorMessage = "Las cuotas deben estar entre 1 y 24")]
            public int Cuotas { get; set; } = 1;

            public DateTime? FechaPrimerDescuento { get; set; }

            [StringLength(500)]
            public string? Notas { get; set; }
        }

        public IActionResult OnGet(int? empleadoId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ValesCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Vales/Index");
            }

            CargarDatos();

            if (empleadoId.HasValue)
            {
                Input.EmpleadoId = empleadoId.Value;
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "ValesCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Vales/Index");
            }

            CargarDatos();

            var empleado = _context.Empleados.FirstOrDefault(e => e.Id == Input.EmpleadoId);
            if (empleado == null)
            {
                ModelState.AddModelError("Input.EmpleadoId", "Empleado no encontrado");
                return Page();
            }

            if (empleado.Estado == "Baja")
            {
                ModelState.AddModelError("Input.EmpleadoId", "No se pueden crear vales para empleados dados de baja");
                return Page();
            }

            // Validar que no tenga vales pendientes excesivos
            var saldoTotalVales = _context.ValesEmpleado
                .Where(v => v.EmpleadoId == Input.EmpleadoId && v.SaldoPendiente > 0)
                .Sum(v => (decimal?)v.SaldoPendiente) ?? 0;

            // Regla: no puede tener más del 50% de un mes de salario en vales
            var limiteVales = empleado.SalarioBase * 0.5m;
            if ((saldoTotalVales + Input.Monto) > limiteVales)
            {
                ModelState.AddModelError(string.Empty,
                    $"El empleado ya tiene L. {saldoTotalVales:N2} en vales. Con este, superaría el 50% de su salario mensual (L. {limiteVales:N2}).");
                return Page();
            }

            if (!ModelState.IsValid) return Page();

            var numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "ValeEmpleado");
            var montoCuota = Math.Round(Input.Monto / Input.Cuotas, 2);

            var vale = new ValeEmpleado
            {
                Numero = numero,
                EmpleadoId = Input.EmpleadoId,
                FechaSolicitud = DateTime.Now,
                Monto = Input.Monto,
                Motivo = Input.Motivo,
                Estado = "Solicitado",
                Cuotas = Input.Cuotas,
                MontoCuota = montoCuota,
                FechaPrimerDescuento = Input.FechaPrimerDescuento,
                SaldoPendiente = Input.Monto,
                Notas = Input.Notas,
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = currentUser?.Id,
                EmpresaId = 1
            };

            _context.ValesEmpleado.Add(vale);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear vale",
                $"Creó vale {vale.Numero} por L. {vale.Monto:N2} para {empleado.Nombres} {empleado.Apellidos}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Vale {vale.Numero} creado correctamente. Pendiente de aprobación.";
            return RedirectToPage("/RRHH/Vales/Index");
        }

        private void CargarDatos()
        {
            Empleados = _context.Empleados
                .Where(e => e.Estado == "Activo" || e.Estado == "Vacaciones")
                .OrderBy(e => e.Apellidos)
                .ThenBy(e => e.Nombres)
                .ToList();

            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "ValeEmpleado");
        }
    }
}