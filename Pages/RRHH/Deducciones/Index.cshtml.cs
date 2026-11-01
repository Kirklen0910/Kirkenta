using System.ComponentModel.DataAnnotations;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.RRHH.Deducciones
{
    /// <summary>
    /// Deducciones de nómina por año (IHSS, RAP, ISR y su tabla de tramos). Cuando el SAR/IHSS cambian los valores
    /// de un año, se corrigen aquí sin tocar código. Cada año tiene su propia configuración.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) { _context = context; }

        public static readonly string[] Metodos = { "Acumulativo", "MensualSimple" };

        public List<ConfiguracionDeduccion> Configs { get; set; } = new();
        public ConfiguracionDeduccion? Config { get; set; }
        public bool PuedeCrear { get; private set; }
        public bool PuedeEditar { get; private set; }
        public int AnioActual => DateTime.Now.Year;

        [BindProperty] public ConfigInput Input { get; set; } = new();
        [BindProperty] public List<TramoInput> Tramos { get; set; } = new();

        public class ConfigInput
        {
            public bool AplicaIHSS { get; set; } = true;
            [Range(0, 100, ErrorMessage = "El % de IHSS debe estar entre 0 y 100")]
            public decimal PorcentajeIHSS { get; set; }
            [Range(0, 100000000, ErrorMessage = "El tope de IHSS no es válido")]
            public decimal TopeIHSS { get; set; }

            public bool AplicaRAP { get; set; } = true;
            [Range(0, 100, ErrorMessage = "El % de RAP debe estar entre 0 y 100")]
            public decimal PorcentajeRAP { get; set; }
            [Range(0, 100000000, ErrorMessage = "El tope de RAP no es válido")]
            public decimal? TopeRAP { get; set; }

            public bool AplicaISR { get; set; } = true;
            [Range(0, 1000000000, ErrorMessage = "El tope anual exento de ISR no es válido")]
            public decimal TopeAnualExentoISR { get; set; }
            [Required] public string MetodoISR { get; set; } = "Acumulativo";

            [StringLength(500)] public string? Notas { get; set; }
        }

        public class TramoInput
        {
            public decimal? Desde { get; set; }
            public decimal? Hasta { get; set; }
            public decimal? Porcentaje { get; set; }
            public decimal? MontoFijo { get; set; }
            public string? Descripcion { get; set; }
        }

        private const string Modulo = "Configuracion";
        private const string Sub = "Deducciones";

        private bool Cargar()
        {
            var usr = User.Identity?.Name;
            if (!PermisoHelper.UsuarioTieneAcceso(_context, usr, Modulo, Sub)) return false;
            PuedeCrear = PermisoHelper.UsuarioTienePermiso(_context, usr, Modulo, Sub, "crear");
            PuedeEditar = PermisoHelper.UsuarioTienePermiso(_context, usr, Modulo, Sub, "editar");
            Configs = _context.ConfiguracionDeducciones.OrderByDescending(c => c.Anio).ToList();
            return true;
        }

        private IActionResult SinPermiso()
        {
            TempData["Error"] = "No tienes permiso para acceder a esta sección";
            return RedirectToPage("/Index");
        }

        private void Seleccionar(int? anio)
        {
            Config = (anio.HasValue ? Configs.FirstOrDefault(c => c.Anio == anio.Value) : null)
                     ?? Configs.FirstOrDefault(c => c.Anio == AnioActual)
                     ?? Configs.FirstOrDefault();
        }

        private void LlenarFormulario()
        {
            if (Config == null) return;
            Input = new ConfigInput
            {
                AplicaIHSS = Config.AplicaIHSS, PorcentajeIHSS = Config.PorcentajeIHSS, TopeIHSS = Config.TopeIHSS,
                AplicaRAP = Config.AplicaRAP, PorcentajeRAP = Config.PorcentajeRAP, TopeRAP = Config.TopeRAP,
                AplicaISR = Config.AplicaISR, TopeAnualExentoISR = Config.TopeAnualExentoISR,
                MetodoISR = Config.MetodoISR, Notas = Config.Notas
            };
            Tramos = _context.TramosISR.Where(t => t.ConfiguracionDeduccionId == Config.Id)
                .OrderBy(t => t.Orden)
                .Select(t => new TramoInput { Desde = t.Desde, Hasta = t.Hasta, Porcentaje = t.Porcentaje, MontoFijo = t.MontoFijo, Descripcion = t.Descripcion })
                .ToList();
        }

        public IActionResult OnGet(int? anio)
        {
            if (!Cargar()) return SinPermiso();
            Seleccionar(anio);
            LlenarFormulario();
            return Page();
        }

        public IActionResult OnPostNuevoAnio(int anio)
        {
            if (!Cargar() || !PuedeCrear) return SinPermiso();
            if (anio < 2000 || anio > 2100) { TempData["Error"] = "Año no válido"; return RedirectToPage(); }
            if (_context.ConfiguracionDeducciones.Any(c => c.Anio == anio))
            { TempData["Error"] = $"Ya existe la configuración de {anio}"; return RedirectToPage(new { anio }); }

            // Se parte de la del año anterior más reciente: solo hay que corregir lo que cambió
            var origen = _context.ConfiguracionDeducciones.Where(c => c.Anio < anio).OrderByDescending(c => c.Anio).FirstOrDefault();
            if (origen == null)
            {
                NominaHelper_Crear(anio);
            }
            else
            {
                CopiarConfiguracion(_context, origen, anio);
            }
            Registrar("Crear configuración de deducciones", $"Año {anio}");
            TempData["Success"] = $"Configuración de {anio} creada. Revisa los valores y guarda.";
            return RedirectToPage(new { anio });
        }

        private void NominaHelper_Crear(int anio) => Kirkenta.Helpers.RRHH.NominaHelper.ObtenerConfiguracion(_context, anio);

        /// <summary>Copia una configuración (y su tabla de tramos) a otro año.</summary>
        public static ConfiguracionDeduccion CopiarConfiguracion(ApplicationDbContext context, ConfiguracionDeduccion origen, int anio)
        {
            var nueva = new ConfiguracionDeduccion
            {
                Anio = anio, AplicaIHSS = origen.AplicaIHSS, PorcentajeIHSS = origen.PorcentajeIHSS, TopeIHSS = origen.TopeIHSS,
                AplicaRAP = origen.AplicaRAP, PorcentajeRAP = origen.PorcentajeRAP, TopeRAP = origen.TopeRAP,
                AplicaISR = origen.AplicaISR, TopeAnualExentoISR = origen.TopeAnualExentoISR, MetodoISR = origen.MetodoISR,
                Notas = $"Copiada de {origen.Anio}", Activa = true, FechaCreacion = DateTime.Now
            };
            context.ConfiguracionDeducciones.Add(nueva);
            context.SaveChanges();
            var tramos = context.TramosISR.Where(t => t.ConfiguracionDeduccionId == origen.Id).OrderBy(t => t.Orden).ToList();
            foreach (var t in tramos)
                context.TramosISR.Add(new TramoISR
                {
                    ConfiguracionDeduccionId = nueva.Id, Orden = t.Orden, Desde = t.Desde, Hasta = t.Hasta,
                    Porcentaje = t.Porcentaje, MontoFijo = t.MontoFijo, Descripcion = t.Descripcion, FechaCreacion = DateTime.Now
                });
            context.SaveChanges();
            return nueva;
        }

        /// <summary>Valida la tabla de tramos. Devuelve la lista limpia y ordenada, o null con el mensaje de error.</summary>
        /// <summary>Exento mensual (primer tramo) que corresponde a un exento anual del SAR.</summary>
        public static decimal ExentoMensual(decimal topeAnualExento)
            => Math.Round((topeAnualExento + Kirkenta.Helpers.RRHH.NominaHelper.DeduccionGastosMedicosAnual) / 12m, 2);

        /// <summary>Exento anual del SAR que corresponde al "Hasta" del primer tramo mensual.</summary>
        public static decimal ExentoAnualDesdeTramo(decimal hastaPrimerTramo)
            => Math.Round(hastaPrimerTramo * 12m - Kirkenta.Helpers.RRHH.NominaHelper.DeduccionGastosMedicosAnual, 2);

        /// <summary>
        /// Deja la tabla coherente con lo que se escribió: si hay exento anual, el primer tramo termina en su
        /// equivalente mensual y el segundo empieza un centavo después. Los montos fijos se recalculan siempre
        /// (cada uno = el anterior + su % sobre el ancho del tramo anterior), así no quedan descuadrados.
        /// Devuelve un error si el exento no cabe en la tabla.
        /// </summary>
        public static string? AjustarTramosISR(List<TramoInput> tramos, decimal topeAnualExento)
        {
            if (tramos.Count == 0) return null;

            if (topeAnualExento > 0)
            {
                if (tramos.Count < 2) return "La tabla del ISR necesita al menos dos tramos (el exento y uno gravado).";
                var exento = ExentoMensual(topeAnualExento);
                if (tramos[1].Hasta.HasValue && tramos[1].Hasta.Value <= exento + 0.01m)
                    return $"El exento anual de L. {topeAnualExento:N2} equivale a L. {exento:N2} mensuales, y no cabe antes del segundo tramo (que termina en L. {tramos[1].Hasta:N2}). Corrige el exento o la tabla.";
                tramos[0].Hasta = exento;
                tramos[1].Desde = exento + 0.01m;
            }

            tramos[0].MontoFijo = 0m;
            for (int i = 1; i < tramos.Count; i++)
            {
                var ant = tramos[i - 1];
                var inicioAnt = i - 1 == 0 ? 0m : tramos[i - 2].Hasta ?? ant.Desde ?? 0m;
                var ancho = (ant.Hasta ?? 0m) - inicioAnt;
                tramos[i].MontoFijo = Math.Round((ant.MontoFijo ?? 0m) + ancho * (ant.Porcentaje ?? 0m) / 100m, 2);
            }
            return null;
        }

        public static List<TramoInput>? ValidarTramos(IEnumerable<TramoInput>? entrada, bool exigir, out string? error)
        {
            error = null;
            var filas = (entrada ?? Enumerable.Empty<TramoInput>())
                .Where(t => t != null && (t.Desde.HasValue || t.Hasta.HasValue || t.Porcentaje.HasValue || t.MontoFijo.HasValue))
                .Select(t => new TramoInput
                {
                    Desde = t.Desde ?? 0m, Hasta = t.Hasta, Porcentaje = t.Porcentaje ?? 0m, MontoFijo = t.MontoFijo ?? 0m,
                    Descripcion = string.IsNullOrWhiteSpace(t.Descripcion) ? null : t.Descripcion.Trim()
                })
                .OrderBy(t => t.Desde)
                .ToList();

            if (filas.Count == 0)
            {
                if (exigir) { error = "Agrega al menos un tramo de ISR (o desmarca \"Aplica ISR\")."; return null; }
                return filas;
            }

            for (int i = 0; i < filas.Count; i++)
            {
                var t = filas[i];
                if (t.Desde < 0 || t.MontoFijo < 0) { error = $"Tramo {i + 1}: los montos no pueden ser negativos."; return null; }
                if (t.Porcentaje < 0 || t.Porcentaje > 100) { error = $"Tramo {i + 1}: el porcentaje debe estar entre 0 y 100."; return null; }
                if (t.Hasta.HasValue && t.Hasta.Value <= t.Desde) { error = $"Tramo {i + 1}: \"Hasta\" debe ser mayor que \"Desde\"."; return null; }
                if (i < filas.Count - 1)
                {
                    if (!t.Hasta.HasValue) { error = $"Tramo {i + 1}: solo el último tramo puede quedar sin \"Hasta\"."; return null; }
                    if (filas[i + 1].Desde <= t.Hasta.Value) { error = $"Los tramos {i + 1} y {i + 2} se traslapan: el siguiente debe empezar después de \"Hasta\"."; return null; }
                }
            }
            if (filas[0].Desde != 0m) { error = "El primer tramo debe empezar en 0."; return null; }
            if (filas[^1].Hasta.HasValue) { error = "El último tramo debe quedar sin \"Hasta\" (sin límite)."; return null; }
            return filas;
        }

        public IActionResult OnPostGuardar(int id)
        {
            if (!Cargar() || !PuedeEditar) return SinPermiso();
            var config = _context.ConfiguracionDeducciones.FirstOrDefault(c => c.Id == id);
            if (config == null) { TempData["Error"] = "Configuración no encontrada"; return RedirectToPage(); }
            Config = config;

            if (!Metodos.Contains(Input.MetodoISR)) ModelState.AddModelError("", "Método de ISR no válido.");
            var tramos = ValidarTramos(Tramos, Input.AplicaISR, out var errorTramos);
            if (errorTramos == null && tramos != null && Input.AplicaISR)
                errorTramos = AjustarTramosISR(tramos, Input.TopeAnualExentoISR);
            if (errorTramos != null) ModelState.AddModelError("", errorTramos);
            if (!ModelState.IsValid) return Page();

            config.AplicaIHSS = Input.AplicaIHSS; config.PorcentajeIHSS = Input.PorcentajeIHSS; config.TopeIHSS = Input.TopeIHSS;
            config.AplicaRAP = Input.AplicaRAP; config.PorcentajeRAP = Input.PorcentajeRAP; config.TopeRAP = Input.TopeRAP;
            config.AplicaISR = Input.AplicaISR; config.TopeAnualExentoISR = Input.TopeAnualExentoISR; config.MetodoISR = Input.MetodoISR;
            config.Notas = string.IsNullOrWhiteSpace(Input.Notas) ? null : Input.Notas.Trim();
            config.Activa = true;

            var viejos = _context.TramosISR.Where(t => t.ConfiguracionDeduccionId == config.Id).ToList();
            _context.TramosISR.RemoveRange(viejos);
            int orden = 1;
            foreach (var t in tramos!)
                _context.TramosISR.Add(new TramoISR
                {
                    ConfiguracionDeduccionId = config.Id, Orden = orden++, Desde = t.Desde ?? 0m, Hasta = t.Hasta,
                    Porcentaje = t.Porcentaje ?? 0m, MontoFijo = t.MontoFijo ?? 0m, Descripcion = t.Descripcion, FechaCreacion = DateTime.Now
                });
            _context.SaveChanges();

            Registrar("Editar configuración de deducciones", $"Año {config.Anio}: IHSS {config.PorcentajeIHSS}% (tope {config.TopeIHSS:N2}), RAP {config.PorcentajeRAP}%, {tramos.Count} tramo(s) de ISR");
            TempData["Success"] = $"Deducciones de {config.Anio} guardadas. Aplican a las nóminas que se calculen desde ahora.";
            return RedirectToPage(new { anio = config.Anio });
        }

        private void Registrar(string accion, string detalle)
        {
            var u = _context.Usuarios.FirstOrDefault(x => x.Username == User.Identity!.Name);
            ActividadHelper.Registrar(_context, u?.Id ?? 0, accion, detalle, HttpContext.Connection.RemoteIpAddress?.ToString());
        }
    }
}
