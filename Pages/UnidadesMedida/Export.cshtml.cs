using Kirkenta.Data;
using Kirkenta.Helpers.Export;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.UnidadesMedida
{
    public class ExportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ExportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(string formato = "excel", bool plantilla = false)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin") return RedirectToPage("/UnidadesMedida/Index");

            var columns = ExportColumns.UnidadesMedida();

            if (plantilla)
            {
                var vacio = GenerarArchivo(new List<UnidadMedida>(), columns, formato, esPlantilla: true);
                return File(vacio, ContentType(formato), NombreArchivo("plantilla_unidades", formato));
            }

            var lista = _context.UnidadesMedida.OrderBy(u => u.Nombre).ToList();
            var archivo = GenerarArchivo(lista, columns, formato, esPlantilla: false);
            return File(archivo, ContentType(formato), NombreArchivo("unidades", formato));
        }

        private static byte[] GenerarArchivo<T>(List<T> items, List<ExportColumn<T>> columns, string formato, bool esPlantilla)
        {
            var titulo = esPlantilla ? "Plantilla de importación de unidades" : "Listado de unidades";
            return formato.ToLower() switch
            {
                "csv" => CsvExporter.Export(items, columns),
                "json" => JsonExporter.Export(items),
                _ => ExcelExporter.Export(items, columns, "Unidades", titulo)
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