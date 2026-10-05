using Kirkenta.Data;
using Kirkenta.Helpers.Import;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.UnidadesMedida
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
            if (currentRol != "Admin") return Redirect(WizardConfig.UrlIndex);

            ImportWizardHelper.LimpiarSesion(HttpContext.Session, WizardConfig);
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            WizardConfig = BuildConfig();
            if (currentRol != "Admin") return Redirect(WizardConfig.UrlIndex);

            var resultado = ImportWizardHelper.ProcesarArchivo(Archivo);
            if (!resultado.Ok)
            {
                ViewData["ImportError"] = resultado.Error;
                return Page();
            }

            ImportWizardHelper.GuardarEnSesion(HttpContext.Session, WizardConfig, resultado.Headers, resultado.Rows);
            return Redirect(WizardConfig.UrlImportMap);
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
                CamposRequeridos = new[] { "Nombre", "Abreviatura" }
            };
        }
    }
}