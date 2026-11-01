using Kirkenta.Helpers.Import;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Import
{
    public class ResultadoModel : PageModel
    {
        public const int MaxFilasEnPantalla = 200;

        public ImportResumenSesion Datos { get; set; } = new();

        public IActionResult OnGet()
        {
            var datos = ImportReportHelper.Recuperar(HttpContext.Session);
            if (datos == null)
                return RedirectToPage("/Index");

            Datos = datos;
            return Page();
        }
    }
}
