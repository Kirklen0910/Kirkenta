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

            /// <summary>Normal, DecimoTercer o DecimoCuarto.</summary>
            public string Clase { get; set; } = "Normal";
            /// <summary>Año del décimo.</summary>
            public int? Anio { get; set; }
            public string? AutorizaUsuario { get; set; }
            public string? AutorizaClave { get; set; }
        }

        /// <summary>El período ya existe: la pantalla pide la autorización de un administrador.</summary>
        public bool PideAutorizacion { get; set; }

        /// <summary>Fechas sugeridas según el tipo: la pantalla las cambia al elegir la planilla.</summary>
        public IActionResult OnGetSugerir(string? clase, string? tipo, int? anio)
        {
            if (NominaHelper.EsDecimo(clase))
            {
                var a = anio ?? (clase == NominaHelper.TipoDecimoCuarto && DateTime.Today.Month > 6 ? DateTime.Today.Year + 1 : DateTime.Today.Year);
                var (di, df) = NominaHelper.PeriodoDecimo(clase!, a);
                var limite = clase == NominaHelper.TipoDecimoCuarto ? new DateTime(a, 6, 30) : new DateTime(a, 12, 15);
                return new JsonResult(new { inicio = di.ToString("yyyy-MM-dd"), fin = df.ToString("yyyy-MM-dd"), pago = limite.ToString("yyyy-MM-dd"), descripcion = $"{NominaHelper.NombreTipo(clase)} {a}", anio = a });
            }

            var frecuencia = string.IsNullOrWhiteSpace(tipo) ? "Mensual" : tipo!;
            var ultima = _context.Nominas.Where(n => n.Tipo == frecuencia && n.Estado != "Anulada")
                .OrderByDescending(n => n.FechaFin).Select(n => (DateTime?)n.FechaFin).FirstOrDefault();
            var desde = ultima?.AddDays(1) ?? (frecuencia is "Semanal" or "Catorcenal" ? DateTime.Today : new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day <= 15 ? 1 : 16));
            var (inicio, fin) = NominaHelper.PeriodoDesde(frecuencia, desde);
            return new JsonResult(new { inicio = inicio.ToString("yyyy-MM-dd"), fin = fin.ToString("yyyy-MM-dd"), pago = fin.ToString("yyyy-MM-dd"), descripcion = NominaHelper.DescribirPeriodo(frecuencia, inicio, fin), anio = (int?)null });
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

            CargarDatos(sugerirFechas: true);
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

            // OJO: en el POST NO se sugieren fechas (antes se pisaban las fechas que el usuario eligió)
            CargarDatos(sugerirFechas: false);

            // Planilla de décimo: el período lo define la ley y entran todos los empleados, sea cual sea su frecuencia
            var esDecimo = NominaHelper.EsDecimo(Input.Clase);
            if (esDecimo)
            {
                Input.Tipo = Input.Clase;
                var anioDecimo = Input.Anio ?? Input.FechaFin.Year;
                (Input.FechaInicio, Input.FechaFin) = NominaHelper.PeriodoDecimo(Input.Clase, anioDecimo);
                if (string.IsNullOrWhiteSpace(Input.PeriodoDescripcion)) Input.PeriodoDescripcion = $"{NominaHelper.NombreTipo(Input.Clase)} {anioDecimo}";
            }

            if (Input.FechaFin < Input.FechaInicio)
            {
                ModelState.AddModelError("Input.FechaFin", "La fecha fin no puede ser anterior al inicio");
                return Page();
            }

            if (!ModelState.IsValid) return Page();

            if (!esDecimo)
            {
                var errorPeriodo = NominaHelper.ValidarPeriodo(Input.Tipo, Input.FechaInicio, Input.FechaFin);
                if (errorPeriodo != null)
                {
                    ModelState.AddModelError("Input.FechaInicio", errorPeriodo);
                    return Page();
                }
                if (string.IsNullOrWhiteSpace(Input.PeriodoDescripcion))
                    Input.PeriodoDescripcion = NominaHelper.DescribirPeriodo(Input.Tipo, Input.FechaInicio, Input.FechaFin);
            }

            // Obtener empleados activos según tipo de nómina
            var empleados = _context.Empleados
                .Where(e => e.Estado == "Activo" || e.Estado == "Vacaciones")
                .Where(e => esDecimo || e.FrecuenciaPago == Input.Tipo)
                .ToList();

            if (empleados.Count == 0)
            {
                ModelState.AddModelError(string.Empty, esDecimo ? "No hay empleados activos." : $"No hay empleados activos con frecuencia de pago '{Input.Tipo}'");
                return Page();
            }

            // Evita pagar dos veces el mismo período: no puede traslaparse con otra nómina vigente del mismo tipo
            var traslape = _context.Nominas.FirstOrDefault(n =>
                n.Tipo == Input.Tipo && n.Estado != "Anulada" &&
                n.FechaInicio <= Input.FechaFin && n.FechaFin >= Input.FechaInicio);
            Usuario? autorizoRepetida = null;
            if (traslape != null)
            {
                // Pagar dos veces el mismo período solo con la clave de un administrador
                PideAutorizacion = true;
                var texto = $"Ya existe la nómina {traslape.Numero} ({traslape.FechaInicio:dd/MM/yyyy} - {traslape.FechaFin:dd/MM/yyyy}, {traslape.Estado}) para este período.";
                if (string.IsNullOrWhiteSpace(Input.AutorizaUsuario))
                {
                    ModelState.AddModelError(string.Empty, texto + " Para generarla de nuevo debe autorizar un administrador.");
                    return Page();
                }
                if (!AutorizacionHelper.VerificarAdmin(_context, Input.AutorizaUsuario, Input.AutorizaClave, out autorizoRepetida, out var errorAut))
                {
                    ActividadHelper.Registrar(_context, currentUser?.Id ?? 0, "Nómina repetida: autorización rechazada",
                        $"Intento de repetir {traslape.Numero} con autorizador «{Input.AutorizaUsuario}»", HttpContext.Connection.RemoteIpAddress?.ToString());
                    ModelState.AddModelError(string.Empty, errorAut);
                    return Page();
                }
            }

            using var tx = _context.Database.BeginTransaction();
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
                EmpresaId = EmpresaActual.Id
            };

            _context.Nominas.Add(nomina);
            _context.SaveChanges();

            // Calcular detalle de cada empleado
            foreach (var emp in empleados)
            {
                var detalle = esDecimo
                    ? NominaHelper.CalcularDecimo(_context, emp, Input.Tipo, Input.FechaInicio, Input.FechaFin)
                    : NominaHelper.CalcularDetalleEmpleado(_context, emp, Input.Tipo, diasPeriodo, Input.FechaInicio, Input.FechaFin);
                if (detalle == null) continue;   // no trabajó en el período del décimo

                detalle.NominaId = nomina.Id;
                _context.DetalleNominas.Add(detalle);
            }

            _context.SaveChanges();
            NominaHelper.RecalcularTotales(_context, nomina);
            nomina.CantidadEmpleados = _context.DetalleNominas.Count(d => d.NominaId == nomina.Id);
            if (autorizoRepetida != null)
                nomina.Notas = $"[Período repetido de {traslape!.Numero}, autorizó {autorizoRepetida.Username}] {nomina.Notas}".Trim();
            var totalNeto = nomina.TotalNeto;

            _context.SaveChanges();
            tx.Commit();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Generar nómina",
                $"Generó nómina {nomina.Numero} tipo {NominaHelper.NombreTipo(nomina.Tipo)} con {nomina.CantidadEmpleados} empleados. Total neto: L. {totalNeto:N2}"
                    + (autorizoRepetida != null ? $". Período repetido autorizado por {autorizoRepetida.Username}" : ""),
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Nómina {nomina.Numero} generada. {nomina.CantidadEmpleados} empleados. Total: L. {totalNeto:N2}";
            return RedirectToPage("/RRHH/Nomina/Details", new { id = nomina.Id });
        }

        private void CargarDatos(bool sugerirFechas)
        {
            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Nomina");

            if (!sugerirFechas) return;

            // Sugerir fechas del período que sigue (la pantalla las actualiza al cambiar el tipo)
            var (inicio, fin) = NominaHelper.PeriodoDesde("Mensual", DateTime.Today);
            Input.FechaInicio = inicio;
            Input.FechaFin = fin;
            Input.FechaPago = fin;
        }
    }
}