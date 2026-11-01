using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.UnidadesMedida
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
            if (!Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "UnidadesMedida", "crear")) return Redirect(WizardConfig.UrlIndex);

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
            if (!Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "UnidadesMedida", "crear")) return Redirect(WizardConfig.UrlIndex);

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

            var resultado = ImportarUnidades(FilasArchivo, Mapeo, currentUser?.Id);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Importar unidades de medida",
                $"Importó unidades: {resultado.Resumen()}",
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
                NombrePlural = "unidades de medida",
                NombreSingular = "unidad",
                ModuloPermiso = "Configuracion",
                SubmoduloPermiso = "UnidadesMedida",
                SessionKey = "UnidadesMedida",
                UrlIndex = "/UnidadesMedida/Index",
                UrlImport = "/UnidadesMedida/Import",
                UrlImportMap = "/UnidadesMedida/ImportMap",
                CamposRequeridos = new[] { "Nombre", "Abreviatura" },
                Campos = new List<CampoMapeo>
                {
                    new() { Key = "Nombre",      Label = "Nombre",       Requerido = true },
                    new() { Key = "Abreviatura", Label = "Abreviatura",  Requerido = true, Ayuda = "Máx 10 caracteres" },
                    new() { Key = "Descripcion", Label = "Descripción",  Requerido = false },
                    new() { Key = "Activa",      Label = "Activa (Sí/No)", Requerido = false },
                },
                AliasCampos = new Dictionary<string, string[]>
                {
                    { "Nombre",      new[] { "nombre", "name", "unidad", "medida" } },
                    { "Abreviatura", new[] { "abreviatura", "abrev", "abbr", "sigla", "short" } },
                    { "Descripcion", new[] { "descripcion", "description", "detalle" } },
                    { "Activa",      new[] { "activa", "activo", "active", "estado" } },
                }
            };
        }

        private string EtiquetaDe(string campo) =>
            ImportWizardConfig.EtiquetaLimpia(WizardConfig.Campos.FirstOrDefault(c => c.Key == campo)?.Label ?? campo);

        private ImportResult ImportarUnidades(
            List<Dictionary<string, string>> filas,
            Dictionary<string, string> mapa,
            int? usuarioId)
        {
            var resultado = new ImportResult { TotalFilas = filas.Count };

            var creadasEnArchivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

                    var abreviatura = Obtener("Abreviatura");
                    if (string.IsNullOrWhiteSpace(abreviatura))
                    {
                        resultado.AgregarError(filaNum, "La abreviatura es obligatoria");
                        continue;
                    }
                    if (abreviatura.Length > 10)
                    {
                        resultado.AgregarAdvertencia(filaNum, $"'Abreviatura' excede 10 caracteres; se recortó '{abreviatura}'.");
                        abreviatura = abreviatura.Substring(0, 10);
                    }

                    if (creadasEnArchivo.Contains(nombre))
                    {
                        resultado.AgregarAdvertencia(filaNum, $"La unidad '{nombre}' está repetida en el archivo; se ignora esta fila.");
                        resultado.Ignorados++;
                        continue;
                    }

                    var descripcion = T("Descripcion", 200);

                    var existente = _context.UnidadesMedida
                        .FirstOrDefault(u => u.Nombre.ToLower() == nombre.ToLower());

                    var activa = ImportHelper.Booleano(resultado, filaNum, EtiquetaDe("Activa"), Obtener("Activa"));

                    if (existente != null)
                    {
                        existente.Nombre = nombre;
                        existente.Abreviatura = abreviatura;
                        existente.Descripcion = descripcion ?? existente.Descripcion;
                        existente.Activa = activa ?? existente.Activa;
                        resultado.Actualizados++;
                    }
                    else
                    {
                        var unidad = new UnidadMedida
                        {
                            Nombre = nombre,
                            Abreviatura = abreviatura,
                            Descripcion = descripcion,
                            Activa = activa ?? true,
                            FechaCreacion = DateTime.Now
                        };
                        _context.UnidadesMedida.Add(unidad);
                        creadasEnArchivo.Add(nombre);
                        resultado.Creados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.AgregarError(filaNum, $"Error inesperado: {ex.Message}");
                }
            }

            ImportHelper.GuardarSeguro(_context, resultado, () => _context.SaveChanges());
            return resultado;
        }
    }
}