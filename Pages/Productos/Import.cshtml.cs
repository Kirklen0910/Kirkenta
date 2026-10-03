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

            if (Archivo == null || Archivo.Length == 0)
            {
                Error = "Debes seleccionar un archivo";
                return Page();
            }

            if (Archivo.Length > 20 * 1024 * 1024)
            {
                Error = "El archivo no puede pesar más de 20 MB";
                return Page();
            }

            var extension = Path.GetExtension(Archivo.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".csv")
            {
                Error = "Solo se permiten archivos Excel (.xlsx) o CSV (.csv)";
                return Page();
            }

            Helpers.Import.ImportFile archivoLeido;
            using (var stream = Archivo.OpenReadStream())
            {
                archivoLeido = extension == ".xlsx"
                    ? Helpers.Import.ExcelImporter.Read(stream)
                    : Helpers.Import.CsvImporter.Read(stream);
            }

            if (!archivoLeido.Ok)
            {
                Error = archivoLeido.Error ?? "No se pudo leer el archivo";
                return Page();
            }

            if (archivoLeido.Rows.Count == 0)
            {
                Error = "El archivo no contiene filas de datos";
                return Page();
            }

            if (archivoLeido.Rows.Count > 5000)
            {
                Error = $"El archivo tiene {archivoLeido.Rows.Count} filas. El máximo es 5,000 filas por importación.";
                return Page();
            }

            var jsonHeaders = System.Text.Json.JsonSerializer.Serialize(archivoLeido.Headers);
            var jsonRows = System.Text.Json.JsonSerializer.Serialize(archivoLeido.Rows);
            HttpContext.Session.SetString("ImportProductos_Headers", jsonHeaders);
            HttpContext.Session.SetString("ImportProductos_Rows", jsonRows);

            return RedirectToPage("/Productos/ImportMap");
        }
    }
}