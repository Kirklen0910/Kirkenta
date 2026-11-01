using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.Integridad
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }

        public string Empresa { get; set; } = "";
        public int TotalPasos => IntegridadHelper.TotalPasos;
        public string[] Modulos => IntegridadHelper.Modulos;

        private bool Permitido() => PermisoHelper.UsuarioTienePermiso(_db, User.Identity?.Name, "Finanzas", "Index", "ver");

        public IActionResult OnGet()
        {
            if (!Permitido())
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }
            Desde ??= DateTime.Today;
            Hasta ??= DateTime.Today;
            Empresa = _db.ConfiguracionEmpresa.FirstOrDefault()?.Nombre ?? "Mi Empresa";
            return Page();
        }

        /// <summary>Ejecuta un paso de la revisión y devuelve su resultado (la pantalla los pide uno a uno para mostrar el avance).</summary>
        public IActionResult OnGetPaso(int i, DateTime? desde, DateTime? hasta)
        {
            if (!Permitido()) return new JsonResult(new { error = "Sin permiso" }) { StatusCode = 403 };
            if (i < 0 || i >= IntegridadHelper.TotalPasos) return new JsonResult(new { error = "Paso inválido" }) { StatusCode = 400 };
            var d = (desde ?? DateTime.Today).Date;
            var h = (hasta ?? desde ?? DateTime.Today).Date.AddDays(1);
            if (h <= d) h = d.AddDays(1);

            var r = IntegridadHelper.EjecutarPaso(_db, i, d, h);
            return new JsonResult(new
            {
                area = r.Area, titulo = r.Titulo, explicacion = r.Explicacion, revisados = r.Revisados, total = r.Total, global = r.Global,
                ejemplos = r.Ejemplos.Select(e => new
                {
                    documento = e.Documento, detalle = e.Detalle,
                    url = e.Enlace != null ? Url.Content("~" + e.Enlace)
                        : e.Pagina != null ? (e.Id.HasValue ? Url.Page(e.Pagina, new { id = e.Id }) : Url.Page(e.Pagina)) : null
                })
            });
        }
    }
}
