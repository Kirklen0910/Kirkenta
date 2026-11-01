using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Productos
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

            HttpContext.Session.Remove("ImportProductos_Headers");
            HttpContext.Session.Remove("ImportProductos_Rows");

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

            HttpContext.Session.SetString("ImportProductos_Headers",
                System.Text.Json.JsonSerializer.Serialize(lectura.Headers));
            HttpContext.Session.SetString("ImportProductos_Rows",
                System.Text.Json.JsonSerializer.Serialize(lectura.Rows));

            return RedirectToPage("/Productos/ImportMap");
        }
    }
}