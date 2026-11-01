using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Productos
{
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public class ImportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public IFormFile? Archivo { get; set; }

        public string? Error { get; set; }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "ProductosImport", "crear"))
            {
                TempData["Error"] = "No tienes permiso para importar productos";
                return RedirectToPage("/Productos/Index");
            }

            Helpers.Import.ImportWizardHelper.LimpiarSesion(HttpContext.Session, new Helpers.Import.ImportWizardConfig { SessionKey = "Productos" });

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "ProductosImport", "crear"))
            {
                TempData["Error"] = "No tienes permiso para importar productos";
                return RedirectToPage("/Productos/Index");
            }

            var lectura = Helpers.Import.ImportWizardHelper.ProcesarArchivo(Archivo);
            if (!lectura.Ok)
            {
                Error = lectura.Error;
                return Page();
            }

            Helpers.Import.ImportWizardHelper.GuardarEnSesion(HttpContext.Session,
                new Helpers.Import.ImportWizardConfig { SessionKey = "Productos" }, lectura.Headers, lectura.Rows);

            return RedirectToPage("/Productos/ImportMap");
        }
    }
}