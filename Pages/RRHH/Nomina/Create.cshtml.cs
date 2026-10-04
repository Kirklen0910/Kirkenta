using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.RRHH.Nomina
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

        public string NumeroPreview { get; set; } = "";

        public class InputModel
        {
            [Required]
            public string Tipo { get; set; } = "Mensual";

            [Required]
            public DateTime FechaInicio { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            [Required]
            public DateTime FechaFin { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));

            [Required]
            public DateTime FechaPago { get; set; } = DateTime.Today;

            [StringLength(200)]
            public string? PeriodoDescripcion { get; set; }

            [StringLength(500)]
            public string? Notas { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "NominaCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Nomina/Index");
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "RRHH", "NominaCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/RRHH/Nomina/Index");
            }

            CargarDatos();

            if (Input.FechaFin < Input.FechaInicio)
            {
                ModelState.AddModelError("Input.FechaFin", "La fecha fin no puede ser anterior al inicio");
                return Page();
            }

            if (!ModelState.IsValid) return Page();

            // Obtener empleados activos según tipo de nómina
            var empleados = _context.Empleados
                .Where(e => e.Estado == "Activo" || e.Estado == "Vacaciones")
                .Where(e => e.FrecuenciaPago == Input.Tipo)
                .ToList();

            if (empleados.Count == 0)
            {
                ModelState.AddModelError(string.Empty, $"No hay empleados activos con frecuencia de pago '{Input.Tipo}'");
                return Page();
            }

            var numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "Nomina");
            var diasPeriodo = NominaHelper.DiasPeriodo(Input.Tipo);
            var periodoDesc = string.IsNullOrWhiteSpace(Input.PeriodoDescripcion)
                ? $"{Input.FechaInicio:dd/MM/yyyy} - {Input.FechaFin:dd/MM/yyyy}"
                : Input.PeriodoDescripcion;

            var nomina = new Models.Nomina
            {
                Numero = numero,
                Tipo = Input.Tipo,
                PeriodoDescripcion = periodoDesc,
                FechaInicio = Input.FechaInicio,
                FechaFin = Input.FechaFin,
                FechaPago = Input.FechaPago,
                Estado = "Calculada",
                CantidadEmpleados = empleados.Count,
                UsuarioCalculoId = currentUser?.Id,
                FechaCalculo = DateTime.Now,
                Notas = Input.Notas,
                FechaCreacion = DateTime.Now,
                EmpresaId = 1
            };

            _context.Nominas.Add(nomina);
            _context.SaveChanges();

            // Calcular detalle de cada empleado
            decimal totalBruto = 0, totalBonos = 0, totalIHSS = 0, totalRAP = 0, totalISR = 0, totalVales = 0, totalNeto = 0;

            foreach (var emp in empleados)
            {
                var detalle = NominaHelper.CalcularDetalleEmpleado(
                    _context,
                    emp,
                    Input.Tipo,
                    diasPeriodo,
                    Input.FechaInicio,
                    Input.FechaFin);

                detalle.NominaId = nomina.Id;
                _context.DetalleNominas.Add(detalle);

                totalBruto += detalle.TotalBruto;
                totalBonos += detalle.BonoTransporte + detalle.BonoAlimentacion + detalle.OtrosBonos;
                totalIHSS += detalle.DeduccionIHSS;
                totalRAP += detalle.DeduccionRAP;
                totalISR += detalle.DeduccionISR;
                totalVales += detalle.DeduccionVales;
                totalNeto += detalle.SalarioNeto;
            }

            nomina.TotalSalariosBrutos = totalBruto;
            nomina.TotalBonos = totalBonos;
            nomina.TotalDeduccionIHSS = totalIHSS;
            nomina.TotalDeduccionRAP = totalRAP;
            nomina.TotalDeduccionISR = totalISR;
            nomina.TotalDeduccionVales = totalVales;
            nomina.TotalNeto = totalNeto;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Generar nómina",
                $"Generó nómina {nomina.Numero} tipo {nomina.Tipo} con {empleados.Count} empleados. Total neto: L. {totalNeto:N2}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Nómina {nomina.Numero} generada. {empleados.Count} empleados. Total: L. {totalNeto:N2}";
            return RedirectToPage("/RRHH/Nomina/Details", new { id = nomina.Id });
        }

        private void CargarDatos()
        {
            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Nomina");

            // Sugerir fechas del período actual
            var (inicio, fin, pago) = NominaHelper.CalcularPeriodo("Mensual", DateTime.Today);
            Input.FechaInicio = inicio;
            Input.FechaFin = fin;
            Input.FechaPago = pago;
        }
    }
}