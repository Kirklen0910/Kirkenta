using Kirkenta.Helpers.Import;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Import
{
    /// <summary>Descarga el CSV con las filas que fallaron en la última importación.</summary>
    public class ErroresModel : PageModel
    {
        public IActionResult OnGet()
        {
            var datos = ImportReportHelper.Recuperar(HttpContext.Session);
            if (datos == null)
            {
                TempData["Error"] = "No hay un reporte de importación disponible (la sesión expiró).";
                return RedirectToPage("/Index");
            }

            var nombre = $"errores_importacion_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(ImportReportHelper.ErroresCsv(datos), "text/csv; charset=utf-8", nombre);
        }
    }
}
