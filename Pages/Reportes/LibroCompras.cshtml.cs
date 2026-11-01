using System.Globalization;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Reportes
{
    /// <summary>Libro de compras (crédito fiscal): compras recibidas por período con su ISV.</summary>
    public class LibroComprasModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LibroComprasModel(ApplicationDbContext context) { _context = context; }

        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }

        public List<LineaFiscal> Filas { get; set; } = new();
        public LineaFiscal Totales { get; set; } = new();

        private bool Cargar()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Reportes", "ISV", "ver")) return false;
            var hoy = DateTime.Today;
            var desde = (Desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
            var hasta = (Hasta ?? hoy).Date;
            Desde = desde; Hasta = hasta;

            Filas = DeclaracionISVHelper.Compras(_context, desde, hasta.AddDays(1));
            Totales = new LineaFiscal
            {
                Documento = "TOTAL",
                Exento = Filas.Sum(x => x.Exento), Exonerado = Filas.Sum(x => x.Exonerado),
                Gravado15 = Filas.Sum(x => x.Gravado15), Isv15 = Filas.Sum(x => x.Isv15),
                Gravado18 = Filas.Sum(x => x.Gravado18), Isv18 = Filas.Sum(x => x.Isv18),
                OtroGravado = Filas.Sum(x => x.OtroGravado), OtroIsv = Filas.Sum(x => x.OtroIsv),
                Total = Filas.Sum(x => x.Total)
            };
            return true;
        }

        public IActionResult OnGet()
        {
            if (!Cargar()) { TempData["Error"] = "No tienes permiso para acceder a esta sección"; return RedirectToPage("/Index"); }
            return Page();
        }

        public IActionResult OnGetCsv()
        {
            if (!Cargar()) return RedirectToPage("/Index");
            string F(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);
            var filas = new List<string?[]>
            {
                new[] { "Fecha", "Orden de compra", "Proveedor", "RTN", "Exento", "Gravado 15%", "ISV 15%", "Gravado 18%", "ISV 18%", "Otro gravado", "Otro ISV", "Total" }
            };
            filas.AddRange(Filas.Select(x => new string?[]
            {
                x.Fecha.ToString("dd/MM/yyyy"), x.Documento, x.Tercero, x.RTN, F(x.Exento + x.Exonerado),
                F(x.Gravado15), F(x.Isv15), F(x.Gravado18), F(x.Isv18), F(x.OtroGravado), F(x.OtroIsv), F(x.Total)
            }));
            filas.Add(new string?[]
            {
                "", "TOTAL", "", "", F(Totales.Exento + Totales.Exonerado), F(Totales.Gravado15), F(Totales.Isv15),
                F(Totales.Gravado18), F(Totales.Isv18), F(Totales.OtroGravado), F(Totales.OtroIsv), F(Totales.Total)
            });
            return CsvExportHelper.Crear($"libro-compras-{Desde:yyyyMMdd}-{Hasta:yyyyMMdd}.csv", filas);
        }
    }
}
