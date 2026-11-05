using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Configuracion
{
    /// <summary>Dirección para entrar desde el celular u otro equipo de la misma red.</summary>
    public class CelularModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IServer _server;

        public CelularModel(ApplicationDbContext db, IServer server) { _db = db; _server = server; }

        public RedLocalHelper.Estado Estado { get; set; } = new(new(), true, false, 5114);

        public IActionResult OnGet()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_db, User.Identity?.Name, "Configuracion", "Celular", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver esta opción";
                return RedirectToPage("/Index");
            }
            var escucha = _server.Features.Get<IServerAddressesFeature>()?.Addresses ?? (ICollection<string>)new List<string>();
            Estado = RedLocalHelper.Calcular(escucha, RedLocalHelper.CandidatasDelEquipo());
            return Page();
        }
    }
}
