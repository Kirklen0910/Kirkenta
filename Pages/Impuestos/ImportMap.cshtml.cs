using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Impuestos
{
    public class ImportMapModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportMapModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public ImportWizardConfig WizardConfig { get; set; } = new();
        public List<string> HeadersArchivo { get; set; } = new();
        public List<Dictionary<string, string>> FilasArchivo { get; set; } = new();
        public Dictionary<string, string?> MapaSugerido { get; set; } = new();

        [BindProperty]
        public Dictionary<string, string> Mapeo { get; set; } = new();

        public IActionResult OnGetPlantilla()
        {
            WizardConfig = BuildConfig();
            return File(WizardConfig.PlantillaXlsx(), ImportReportHelper.TipoXlsx,
                $"plantilla_{ColumnMapper.Normalizar(WizardConfig.NombrePlural)}.xlsx");
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            WizardConfig = BuildConfig();
            if (!Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Configuracion", "Impuestos", "crear")) return Redirect(WizardConfig.UrlIndex);

            if (!CargarDesdeSesion())
            {
                TempData["Error"] = "La sesión expiró.";
                return Redirect(WizardConfig.UrlImport);
            }

            MapaSugerido = ColumnMapper.Detectar(HeadersArchivo, WizardConfig.AliasCampos);
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            WizardConfig = BuildConfig();
            if (!Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Configuracion", "Impuestos", "crear")) return Redirect(WizardConfig.UrlIndex);

            if (!CargarDesdeSesion())
            {
                TempData["Error"] = "La sesión expiró.";
                return Redirect(WizardConfig.UrlImport);
            }

            var faltantes = WizardConfig.CamposRequeridos
                .Where(campo => string.IsNullOrEmpty(Mapeo.GetValueOrDefault(campo)))
                .ToList();

            if (faltantes.Count > 0)
            {
                var labelsFaltantes = WizardConfig.Campos.Where(c => faltantes.Contains(c.Key)).Select(c => c.Label);
                TempData["Error"] = $"Debes mapear: {string.Join(", ", labelsFaltantes)}";
                return Redirect(WizardConfig.UrlImportMap);
            }

            var resultado = ImportarImpuestos(FilasArchivo, Mapeo, currentUser?.Id);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Importar impuestos",
                $"Importó impuestos: {resultado.Resumen()}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            ImportWizardHelper.LimpiarSesion(HttpContext.Session, WizardConfig);

            // Éxito limpio => mensaje y listado. Con errores/advertencias => pantalla de resultado
            // con el motivo de cada fila y CSV descargable para corregir y reintentar.
            return ImportReportHelper.Finalizar(this, WizardConfig.UrlIndex, resultado, HeadersArchivo, FilasArchivo);
        }

        private bool CargarDesdeSesion()
        {
            var data = ImportWizardHelper.RecuperarDeSesion(HttpContext.Session, WizardConfig);
            if (data == null) return false;
            HeadersArchivo = data.Value.Headers;
            FilasArchivo = data.Value.Rows;
            return true;
        }

        private static ImportWizardConfig BuildConfig()
        {
            return new ImportWizardConfig
            {
                NombrePlural = "impuestos",
                NombreSingular = "impuesto",
                ModuloPermiso = "Configuracion",
                SubmoduloPermiso = "Impuestos",
                SessionKey = "Impuestos",
                UrlIndex = "/Impuestos/Index",
                UrlImport = "/Impuestos/Import",
                UrlImportMap = "/Impuestos/ImportMap",
                CamposRequeridos = new[] { "Nombre", "Porcentaje" },
                Campos = new List<CampoMapeo>
                {
                    new() { Key = "Nombre",          Label = "Nombre",        Requerido = true },
                    new() { Key = "Porcentaje",      Label = "Porcentaje (%)",Requerido = true },
                    new() { Key = "Descripcion",     Label = "Descripción",   Requerido = false },
                    new() { Key = "EsPredeterminado",Label = "Predeterminado (Sí/No)", Requerido = false },
                    new() { Key = "Activo",          Label = "Activo (Sí/No)", Requerido = false },
                },
                AliasCampos = new Dictionary<string, string[]>
                {
                    { "Nombre",          new[] { "nombre", "name", "impuesto", "tax" } },
                    { "Porcentaje",      new[] { "porcentaje", "tasa", "rate", "iva", "isv", "porcent" } },
                    { "Descripcion",     new[] { "descripcion", "description", "detalle" } },
                    { "EsPredeterminado",new[] { "predeterminado", "default", "por_defecto" } },
                    { "Activo",          new[] { "activo", "active", "estado" } },
                }
            };
        }

        private string EtiquetaDe(string campo) =>
            ImportWizardConfig.EtiquetaLimpia(WizardConfig.Campos.FirstOrDefault(c => c.Key == campo)?.Label ?? campo);

        private ImportResult ImportarImpuestos(
            List<Dictionary<string, string>> filas,
            Dictionary<string, string> mapa,
            int? usuarioId)
        {
            var resultado = new ImportResult { TotalFilas = filas.Count };

            var creadosEnArchivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Impuesto? predeterminadoEnArchivo = null;

            int filaNum = 1;
            foreach (var fila in filas)
            {
                filaNum++;
                try
                {
                    string? Obtener(string campo)
                    {
                        var header = mapa.GetValueOrDefault(campo);
                        if (string.IsNullOrEmpty(header)) return null;
                        return fila.GetValueOrDefault(header)?.Trim();
                    }

                    string? T(string campo, int max) =>
                        ImportHelper.Texto(resultado, filaNum, EtiquetaDe(campo), Obtener(campo), max);

                    var nombre = Obtener("Nombre");
                    if (string.IsNullOrWhiteSpace(nombre))
                    {
                        resultado.AgregarError(filaNum, "El campo 'Nombre' es obligatorio");
                        continue;
                    }
                    if (nombre.Length > 50)
                    {
                        resultado.AgregarError(filaNum, "El nombre excede 50 caracteres");
                        continue;
                    }

                    var porcentajeTexto = Obtener("Porcentaje");
                    var porcentaje = ParserHelper.ParsearDecimal(porcentajeTexto);
                    if (porcentaje == null)
                    {
                        resultado.AgregarError(filaNum, "El porcentaje es obligatorio y debe ser numérico");
                        continue;
                    }
                    if (porcentaje < 0 || porcentaje > 100)
                    {
                        resultado.AgregarError(filaNum, "El porcentaje debe estar entre 0 y 100");
                        continue;
                    }

                    if (creadosEnArchivo.Contains(nombre))
                    {
                        resultado.AgregarAdvertencia(filaNum, $"El impuesto '{nombre}' está repetido en el archivo; se ignora esta fila.");
                        resultado.Ignorados++;
                        continue;
                    }

                    var descripcion = T("Descripcion", 200);
                    var activo = ImportHelper.Booleano(resultado, filaNum, "Activo", Obtener("Activo"));
                    var predeterminado = ImportHelper.Booleano(resultado, filaNum, "Predeterminado", Obtener("EsPredeterminado"));

                    var existente = _context.Impuestos
                        .FirstOrDefault(i => i.Nombre.ToLower() == nombre.ToLower());

                    Impuesto destino;
                    if (existente != null)
                    {
                        existente.Nombre = nombre;
                        existente.Porcentaje = porcentaje.Value;
                        existente.Descripcion = descripcion ?? existente.Descripcion;
                        existente.Activo = activo ?? existente.Activo;
                        if (predeterminado.HasValue) existente.EsPredeterminado = predeterminado.Value;
                        destino = existente;
                        resultado.Actualizados++;
                    }
                    else
                    {
                        destino = new Impuesto
                        {
                            Nombre = nombre,
                            Porcentaje = porcentaje.Value,
                            Descripcion = descripcion,
                            EsPredeterminado = predeterminado ?? false,
                            Activo = activo ?? true
                        };
                        _context.Impuestos.Add(destino);
                        resultado.Creados++;
                    }

                    creadosEnArchivo.Add(nombre);
                    if (predeterminado == true) predeterminadoEnArchivo = destino;
                }
                catch (Exception ex)
                {
                    resultado.AgregarError(filaNum, $"Error inesperado: {ex.Message}");
                }
            }

            ImportHelper.GuardarSeguro(_context, resultado, () =>
            {
                // Solo puede haber un impuesto predeterminado: el último marcado en el archivo
                if (predeterminadoEnArchivo != null)
                {
                    foreach (var otro in _context.Impuestos.Where(i => i.EsPredeterminado).ToList())
                        if (!ReferenceEquals(otro, predeterminadoEnArchivo)) otro.EsPredeterminado = false;
                    predeterminadoEnArchivo.EsPredeterminado = true;
                }
                _context.SaveChanges();
            });
            return resultado;
        }
    }
}