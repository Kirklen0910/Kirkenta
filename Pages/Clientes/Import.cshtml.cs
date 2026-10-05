using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Import;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Clientes
{
    public class ImportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public IFormFile? Archivo { get; set; }

        public ImportWizardConfig WizardConfig { get; set; } = new();

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            WizardConfig = BuildConfig();

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, WizardConfig.ModuloPermiso, WizardConfig.SubmoduloPermiso, "crear"))
            {
                TempData["Error"] = $"No tienes permiso para importar {WizardConfig.NombrePlural}";
                return Redirect(WizardConfig.UrlIndex);
            }

            // Limpiar datos previos del wizard
            ImportWizardHelper.LimpiarSesion(HttpContext.Session, WizardConfig);

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            WizardConfig = BuildConfig();

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, WizardConfig.ModuloPermiso, WizardConfig.SubmoduloPermiso, "crear"))
            {
                TempData["Error"] = $"No tienes permiso para importar {WizardConfig.NombrePlural}";
                return Redirect(WizardConfig.UrlIndex);
            }

            var resultado = ImportWizardHelper.ProcesarArchivo(Archivo);

            if (!resultado.Ok)
            {
                ViewData["ImportError"] = resultado.Error;
                return Page();
            }

            ImportWizardHelper.GuardarEnSesion(HttpContext.Session, WizardConfig, resultado.Headers, resultado.Rows);

            return Redirect(WizardConfig.UrlImportMap);
        }

        /// <summary>
        /// Configuración específica del módulo Clientes.
        /// </summary>
        private static ImportWizardConfig BuildConfig()
        {
            return new ImportWizardConfig
            {
                NombrePlural = "clientes",
                NombreSingular = "cliente",
                ModuloPermiso = "Ventas",
                SubmoduloPermiso = "ClientesImport",
                SessionKey = "Clientes",
                UrlIndex = "/Clientes/Index",
                UrlImport = "/Clientes/Import",
                UrlImportMap = "/Clientes/ImportMap",
                CamposRequeridos = new[] { "Nombre" }
            };
        }
    }
}