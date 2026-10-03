using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Export;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Productos
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
                !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "ProductosExport", "ver"))
            {
                TempData["Error"] = "No tienes permiso para exportar productos";
                return RedirectToPage("/Productos/Index");
            }

            var categorias = _context.Categorias
                .AsNoTracking()
                .ToDictionary(c => c.Id, c => c.Nombre);

            var impuestos = _context.Impuestos
                .AsNoTracking()
                .ToDictionary(i => i.Id, i => i.Nombre ?? $"{i.Porcentaje}%");

            var columns = ExportColumns.Productos(categorias, impuestos);

            if (plantilla)
            {
                var archivoVacio = GenerarArchivo(new List<Producto>(), columns, formato, esPlantilla: true);
                return File(archivoVacio, ContentType(formato), NombreArchivo("plantilla_productos", formato));
            }

            var query = _context.Productos.AsNoTracking().AsQueryable();
            if (soloActivos)
            {
                query = query.Where(p => p.Activo);
            }

            var productos = query.OrderBy(p => p.Nombre).ToList();

            var archivo = GenerarArchivo(productos, columns, formato, esPlantilla: false);
            return File(archivo, ContentType(formato), NombreArchivo("productos", formato));
        }

        private static byte[] GenerarArchivo<T>(List<T> items, List<ExportColumn<T>> columns, string formato, bool esPlantilla)
        {
            var titulo = esPlantilla ? "Plantilla de importación de productos" : "Listado de productos";
            return formato.ToLower() switch
            {
                "csv" => CsvExporter.Export(items, columns),
                "json" => JsonExporter.Export(items),
                _ => ExcelExporter.Export(items, columns, "Productos", titulo)
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