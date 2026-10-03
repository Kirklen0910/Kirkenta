using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Export;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Clientes
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
                !PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "ClientesExport", "ver"))
            {
                TempData["Error"] = "No tienes permiso para exportar clientes";
                return RedirectToPage("/Clientes/Index");
            }

            var columns = ExportColumns.Clientes();

            if (plantilla)
            {
                var archivoVacio = GenerarArchivo(new List<Cliente>(), columns, formato, esPlantilla: true);
                return File(archivoVacio, ContentType(formato), NombreArchivo("plantilla_clientes", formato));
            }

            var query = _context.Clientes.AsNoTracking().AsQueryable();
            if (soloActivos)
            {
                query = query.Where(c => c.Activo);
            }

            var clientes = query.OrderBy(c => c.Nombre).ToList();

            var archivo = GenerarArchivo(clientes, columns, formato, esPlantilla: false);
            return File(archivo, ContentType(formato), NombreArchivo("clientes", formato));
        }

        private static byte[] GenerarArchivo<T>(List<T> items, List<ExportColumn<T>> columns, string formato, bool esPlantilla)
        {
            var titulo = esPlantilla ? "Plantilla de importación de clientes" : "Listado de clientes";
            return formato.ToLower() switch
            {
                "csv" => CsvExporter.Export(items, columns),
                "json" => JsonExporter.Export(items),
                _ => ExcelExporter.Export(items, columns, "Clientes", titulo)
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