using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Configuracion
{
    /// <summary>
    /// Apariencia del sistema (tema claro / oscuro / según el equipo).
    /// Disponible para cualquier usuario con sesión iniciada, sin importar su rol:
    /// la preferencia se guarda en una cookie propia de cada usuario (ver _Layout.cshtml).
    /// </summary>
    public class AparienciaModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
