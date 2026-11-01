using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Services.Licencia;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.LicenciaPagina
{
    /// <summary>
    /// Pantalla de licencia. Sin licencia válida la puede abrir cualquiera (es la única pantalla disponible);
    /// con licencia válida solo la ven los usuarios con sesión y solo el Admin puede cambiarla.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly LicenciaService _lic;
        private readonly ApplicationDbContext _db;

        public IndexModel(LicenciaService lic, ApplicationDbContext db) { _lic = lic; _db = db; }

        [BindProperty] public string? Texto { get; set; }

        public LicenciaVista Vista { get; set; } = null!;
        public bool EsAdmin { get; set; }
        public bool PuedeActivar { get; set; }
        public string? Mensaje { get; set; }
        public bool MensajeOk { get; set; }

        private async Task<bool> PrepararAsync()
        {
            Vista = _lic.Estado();
            var autenticado = User.Identity?.IsAuthenticated == true;
            if (Vista.Permitido)
            {
                if (!autenticado) return false;
                var nombre = User.Identity!.Name?.Trim();
                EsAdmin = !string.IsNullOrEmpty(nombre)
                    && PermisoHelper.UsuarioTieneAcceso(_db, nombre, "Configuracion", "Licencia");
                PuedeActivar = EsAdmin && Vista.Aplica;
            }
            else
            {
                PuedeActivar = Vista.Aplica; // bloqueado: hay que poder activar una licencia sin iniciar sesión
            }
            return true;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!await PrepararAsync()) return RedirectToPage("/Auth/Login");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!await PrepararAsync()) return RedirectToPage("/Auth/Login");
            if (!PuedeActivar) return Forbid();

            if (string.IsNullOrWhiteSpace(Texto))
            {
                Mensaje = "Pega la licencia completa.";
                return Page();
            }

            var (ok, msg) = _lic.Activar(Texto);
            if (ok)
            {
                TempData["Success"] = msg;
                return RedirectToPage("/Index");
            }
            Mensaje = msg;
            MensajeOk = false;
            Vista = _lic.Estado();
            return Page();
        }
    }
}
