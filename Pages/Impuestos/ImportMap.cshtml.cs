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

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            WizardConfig = BuildConfig();
            if (currentRol != "Admin") return Redirect(WizardConfig.UrlIndex);

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
            if (currentRol != "Admin") return Redirect(WizardConfig.UrlIndex);

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

            TempData["Success"] = $"Importación completada: {resultado.Resumen()}";
            return Redirect(WizardConfig.UrlIndex);
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

        private ImportResult ImportarImpuestos(
            List<Dictionary<string, string>> filas,
            Dictionary<string, string> mapa,
            int? usuarioId)
        {
            var resultado = new ImportResult { TotalFilas = filas.Count };

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

                    var porcentaje = ParserHelper.ParsearDecimal(Obtener("Porcentaje"));
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

                    var existente = _context.Impuestos
                        .FirstOrDefault(i => i.Nombre.ToLower() == nombre.ToLower());

                    if (existente != null)
                    {
                        existente.Nombre = nombre;
                        existente.Porcentaje = porcentaje.Value;
                        existente.Descripcion = Obtener("Descripcion") ?? existente.Descripcion;
                        existente.Activo = ParserHelper.ParsearBool(Obtener("Activo")) ?? existente.Activo;
                        resultado.Actualizados++;
                    }
                    else
                    {
                        var imp = new Impuesto
                        {
                            Nombre = nombre,
                            Porcentaje = porcentaje.Value,
                            Descripcion = Obtener("Descripcion"),
                            EsPredeterminado = ParserHelper.ParsearBool(Obtener("EsPredeterminado")) ?? false,
                            Activo = ParserHelper.ParsearBool(Obtener("Activo")) ?? true
                        };
                        _context.Impuestos.Add(imp);
                        resultado.Creados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.AgregarError(filaNum, $"Error inesperado: {ex.Message}");
                }
            }

            _context.SaveChanges();
            return resultado;
        }
    }
}