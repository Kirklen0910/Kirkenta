using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Import;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Categorias
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

        [BindProperty]
        public string ModoImportacion { get; set; } = "upsert";

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

            if (!Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "Categorias", "crear"))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return Redirect(WizardConfig.UrlIndex);
            }

            if (!CargarDesdeSesion())
            {
                TempData["Error"] = "La sesión expiró. Vuelve a subir el archivo.";
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

            if (!Kirkenta.Helpers.PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "Categorias", "crear"))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return Redirect(WizardConfig.UrlIndex);
            }

            if (!CargarDesdeSesion())
            {
                TempData["Error"] = "La sesión expiró. Vuelve a subir el archivo.";
                return Redirect(WizardConfig.UrlImport);
            }

            var faltantes = WizardConfig.CamposRequeridos
                .Where(campo => string.IsNullOrEmpty(Mapeo.GetValueOrDefault(campo)))
                .ToList();

            if (faltantes.Count > 0)
            {
                var labelsFaltantes = WizardConfig.Campos
                    .Where(c => faltantes.Contains(c.Key))
                    .Select(c => c.Label);

                TempData["Error"] = $"Debes mapear: {string.Join(", ", labelsFaltantes)}";
                return Redirect(WizardConfig.UrlImportMap);
            }

            var resultado = ImportarCategorias(FilasArchivo, Mapeo, ModoImportacion, currentUser?.Id);

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Importar categorías",
                $"Importó categorías: {resultado.Resumen()}",
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
                NombrePlural = "categorías",
                NombreSingular = "categoría",
                ModuloPermiso = "Inventario",
                SubmoduloPermiso = "Categorias",
                SessionKey = "Categorias",
                UrlIndex = "/Categorias/Index",
                UrlImport = "/Categorias/Import",
                UrlImportMap = "/Categorias/ImportMap",
                CamposRequeridos = new[] { "Nombre" },
                Campos = new List<CampoMapeo>
                {
                    new() { Key = "Nombre",      Label = "Nombre",      Requerido = true },
                    new() { Key = "Descripcion", Label = "Descripción", Requerido = false },
                    new() { Key = "Color",       Label = "Color",       Requerido = false, Ayuda = "Hexadecimal, ej: #4f46e5" },
                    new() { Key = "Activa",      Label = "Activa (Sí/No)", Requerido = false },
                },
                AliasCampos = new Dictionary<string, string[]>
                {
                    { "Nombre",      new[] { "nombre", "name", "categoria", "rubro" } },
                    { "Descripcion", new[] { "descripcion", "description", "detalle" } },
                    { "Color",       new[] { "color", "hex", "codigo_color" } },
                    { "Activa",      new[] { "activa", "activo", "active", "estado", "status" } },
                }
            };
        }

        private string EtiquetaDe(string campo) =>
            ImportWizardConfig.EtiquetaLimpia(WizardConfig.Campos.FirstOrDefault(c => c.Key == campo)?.Label ?? campo);

        private ImportResult ImportarCategorias(
            List<Dictionary<string, string>> filas,
            Dictionary<string, string> mapa,
            string modo,
            int? usuarioId)
        {
            var resultado = new ImportResult { TotalFilas = filas.Count };

            // Categorías creadas en este mismo archivo (aún sin guardar), para detectar nombres repetidos
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

                    if (nombre.Length > 100)
                    {
                        resultado.AgregarError(filaNum, "El nombre excede 100 caracteres");
                        continue;
                    }

                    if (creadasEnArchivo.Contains(nombre))
                    {
                        resultado.AgregarAdvertencia(filaNum, $"La categoría '{nombre}' está repetida en el archivo; se ignora esta fila.");
                        resultado.Ignorados++;
                        continue;
                    }

                    var existente = _context.Categorias
                        .FirstOrDefault(c => c.Nombre.ToLower() == nombre.ToLower());

                    if (existente != null && modo == "crear")
                    {
                        resultado.Ignorados++;
                        continue;
                    }

                    if (existente == null && modo == "actualizar")
                    {
                        resultado.AgregarAdvertencia(filaNum, $"Categoría '{nombre}' no existe, se ignora");
                        resultado.Ignorados++;
                        continue;
                    }

                    // Color: solo si viene y es un hexadecimal válido. Si no viene, una categoría
                    // existente conserva su color (antes se le pisaba con gris en cada importación).
                    string? color = null;
                    var colorTexto = Obtener("Color");
                    if (!string.IsNullOrWhiteSpace(colorTexto))
                    {
                        var hex = colorTexto.Trim().TrimStart('#');
                        if (System.Text.RegularExpressions.Regex.IsMatch(hex, "^([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$"))
                            color = "#" + hex.ToLowerInvariant();
                        else
                            resultado.AgregarAdvertencia(filaNum, $"'Color': '{colorTexto}' no es un hexadecimal válido (ej: #4f46e5); se ignoró.");
                    }

                    var descripcion = T("Descripcion", 300);

                    var activa = ImportHelper.Booleano(resultado, filaNum, EtiquetaDe("Activa"), Obtener("Activa"));

                    if (existente != null)
                    {
                        existente.Nombre = nombre;
                        existente.Descripcion = descripcion ?? existente.Descripcion;
                        existente.Color = color ?? existente.Color;
                        existente.Activa = activa ?? existente.Activa;
                        resultado.Actualizados++;
                    }
                    else
                    {
                        var cat = new Categoria
                        {
                            Nombre = nombre,
                            Descripcion = descripcion,
                            Color = color ?? "#6b7280",
                            Activa = activa ?? true,
                            FechaCreacion = DateTime.Now
                        };
                        _context.Categorias.Add(cat);
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