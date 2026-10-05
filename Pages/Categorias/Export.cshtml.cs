using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Export;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Categorias
{
    public class ExportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ExportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(string formato = "excel", bool plantilla = false, bool soloActivas = false)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Categorias/Index");
            }

            var columns = ExportColumns.Categorias();

            if (plantilla)
            {
                var archivoVacio = GenerarArchivo(new List<Categoria>(), columns, formato, esPlantilla: true);
                return File(archivoVacio, ContentType(formato), NombreArchivo("plantilla_categorias", formato));
            }

            var query = _context.Categorias.AsQueryable();
            if (soloActivas)
            {
                query = query.Where(c => c.Activa);
            }

            var categorias = query.OrderBy(c => c.Nombre).ToList();

            var archivo = GenerarArchivo(categorias, columns, formato, esPlantilla: false);
            return File(archivo, ContentType(formato), NombreArchivo("categorias", formato));
        }

        private static byte[] GenerarArchivo<T>(List<T> items, List<ExportColumn<T>> columns, string formato, bool esPlantilla)
        {
            var titulo = esPlantilla ? "Plantilla de importación de categorías" : "Listado de categorías";
            return formato.ToLower() switch
            {
                "csv" => CsvExporter.Export(items, columns),
                "json" => JsonExporter.Export(items),
                _ => ExcelExporter.Export(items, columns, "Categorías", titulo)
            };
        }

        private static string ContentType(string formato) => formato.ToLower() switch
        {
            "csv" => "text/csv",
            "json" => "application/json",
            _ => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        private static string NombreArchivo(string baseNombre, string formato)
        {
            var ext = formato.ToLower() switch { "csv" => "csv", "json" => "json", _ => "xlsx" };
            return $"{baseNombre}_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
        }
    }
}