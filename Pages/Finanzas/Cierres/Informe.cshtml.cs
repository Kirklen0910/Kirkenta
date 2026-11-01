using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Finanzas.Cierres
{
    /// <summary>Informe por método de pago de un cierre de caja o de cualquier período.</summary>
    public class InformeModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public InformeModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public int? Id { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }
        [BindProperty(SupportsGet = true)] public string? Metodo { get; set; }

        public InformeMetodosPago Informe { get; set; } = new();
        public CierreCaja? Cierre { get; set; }
        public string Empresa { get; set; } = "Mi Empresa";
        public string Titulo { get; set; } = "";
        public string PeriodoTexto { get; set; } = "";

        public IActionResult OnGet()
        {
            var u = _db.Usuarios.FirstOrDefault(x => x.Username == User.Identity!.Name);
            var rol = u?.Rol ?? "Pendiente";
            if (rol != "Admin" && !PermisoHelper.TienePermiso(_db, rol, "Finanzas", "Cierres", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Cierres/Index");
            }

            Empresa = _db.ConfiguracionEmpresa.FirstOrDefault()?.Nombre ?? "Mi Empresa";

            DateTime d, h;
            if (Id.HasValue)
            {
                Cierre = _db.CierresCaja.FirstOrDefault(c => c.Id == Id.Value);
                if (Cierre == null)
                {
                    TempData["Error"] = "Cierre no encontrado";
                    return RedirectToPage("/Finanzas/Cierres/Index");
                }
                (d, h) = InformeMetodosPagoHelper.RangoDeCierre(_db, Cierre);
                Titulo = $"Informe del cierre {Cierre.Numero}";
                PeriodoTexto = $"{d:dd/MM/yyyy HH:mm} a {Cierre.FechaCierre:dd/MM/yyyy HH:mm}";
            }
            else
            {
                var hoy = DateTime.Today;
                var ini = (Desde ?? hoy).Date;
                var fin = (Hasta ?? Desde ?? hoy).Date;
                if (fin < ini) (ini, fin) = (fin, ini);
                d = ini; h = fin.AddDays(1);
                Desde = ini; Hasta = fin;
                Titulo = "Informe por método de pago";
                PeriodoTexto = ini == fin ? ini.ToString("dd/MM/yyyy") : $"{ini:dd/MM/yyyy} al {fin:dd/MM/yyyy}";
            }

            Informe = InformeMetodosPagoHelper.Generar(_db, d, h, Metodo);
            return Page();
        }

        /// <summary>Enlace de la misma pantalla filtrando por un método de pago (null = todos).</summary>
        public string EnlaceMetodo(string? metodo)
        {
            var q = new List<string>();
            if (Id.HasValue) q.Add("id=" + Id.Value);
            else
            {
                if (Desde.HasValue) q.Add("desde=" + Desde.Value.ToString("yyyy-MM-dd"));
                if (Hasta.HasValue) q.Add("hasta=" + Hasta.Value.ToString("yyyy-MM-dd"));
            }
            if (!string.IsNullOrWhiteSpace(metodo)) q.Add("metodo=" + Uri.EscapeDataString(metodo));
            return "/Finanzas/Cierres/Informe" + (q.Count > 0 ? "?" + string.Join("&", q) : "");
        }
    }
}
