using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Export;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Proveedores
{
    public class ExportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ExportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(string formato = "excel", bool plantilla = false, bool soloActivos = false)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "ProveedoresExport", "ver"))
            {
                TempData["Error"] = "No tienes permiso para exportar proveedores";
                return RedirectToPage("/Proveedores/Index");
            }

            var columns = ExportColumns.Proveedores();

            if (plantilla)
            {
                var archivoVacio = GenerarArchivo(new List<Proveedor>(), columns, formato, esPlantilla: true);
                return File(archivoVacio, ContentType(formato), NombreArchivo("plantilla_proveedores", formato));
            }

            var query = _context.Proveedores.AsNoTracking().AsQueryable();
            if (soloActivos)
            {
                query = query.Where(p => p.Activo);
            }

            var proveedores = query.OrderBy(p => p.Nombre).ToList();

            var archivo = GenerarArchivo(proveedores, columns, formato, esPlantilla: false);
            return File(archivo, ContentType(formato), NombreArchivo("proveedores", formato));
        }

        private static byte[] GenerarArchivo<T>(List<T> items, List<ExportColumn<T>> columns, string formato, bool esPlantilla)
        {
            var titulo = esPlantilla ? "Plantilla de importación de proveedores" : "Listado de proveedores";
            return formato.ToLower() switch
            {
                "csv" => CsvExporter.Export(items, columns),
                "json" => JsonExporter.Export(items),
                _ => ExcelExporter.Export(items, columns, "Proveedores", titulo)
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