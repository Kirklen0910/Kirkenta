using System.Globalization;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Reportes
{
    /// <summary>Resumen mensual del ISV (débito de ventas − crédito de compras) para llevarle al contador o declarar.</summary>
    public class DeclaracionISVModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeclaracionISVModel(ApplicationDbContext context) { _context = context; }

        [BindProperty(SupportsGet = true)] public int? Anio { get; set; }
        [BindProperty(SupportsGet = true)] public int? Mes { get; set; }
        [BindProperty(SupportsGet = true)] public decimal? SaldoFavor { get; set; }
        [BindProperty(SupportsGet = true)] public decimal? Retenciones { get; set; }

        public ResumenISV Resumen { get; set; } = new();
        public string Empresa { get; set; } = "";
        public string? RTN { get; set; }
        public static readonly string[] Meses = { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };
        public string NombrePeriodo => $"{Meses[Resumen.Desde.Month - 1]} de {Resumen.Desde.Year}";

        private bool Cargar()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Reportes", "ISV", "ver")) return false;

            // Por defecto, el mes anterior (el que normalmente se declara)
            var baseMes = DateTime.Today.AddMonths(-1);
            var anio = Anio is >= 2000 and <= 2100 ? Anio.Value : baseMes.Year;
            var mes = Mes is >= 1 and <= 12 ? Mes.Value : baseMes.Month;
            Anio = anio; Mes = mes;

            var desde = new DateTime(anio, mes, 1);
            var hastaExcl = desde.AddMonths(1);
            var ventas = DeclaracionISVHelper.Ventas(_context, desde, hastaExcl);
            var compras = DeclaracionISVHelper.Compras(_context, desde, hastaExcl);
            Resumen = DeclaracionISVHelper.Resumir(desde, hastaExcl.AddDays(-1), ventas, compras,
                SaldoFavor ?? 0, Retenciones ?? 0, DeclaracionISVHelper.DevolucionesDelPeriodo(_context, desde, hastaExcl),
                DeclaracionISVHelper.IsvDevolucionesDelPeriodo(_context, desde, hastaExcl));

            var emp = _context.ConfiguracionEmpresa.FirstOrDefault();
            Empresa = emp?.Nombre ?? "";
            RTN = emp?.RTN;
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
            var r = Resumen;
            var filas = new List<string?[]>
            {
                new[] { "Resumen de ISV", NombrePeriodo },
                new[] { "Empresa", Empresa, "RTN", RTN },
                new string?[] { },
                new[] { "VENTAS (débito fiscal)", "Base", "ISV" },
                new[] { "Ventas gravadas 15%", F(r.VentasGravadas15), F(r.Isv15) },
                new[] { "Ventas gravadas 18%", F(r.VentasGravadas18), F(r.Isv18) },
                new[] { "Otras tasas", F(r.VentasOtroGravado), F(r.IsvOtro) },
                new[] { "Ventas exentas", F(r.VentasExentas), "0.00" },
                new[] { "Ventas exoneradas", F(r.VentasExoneradas), "0.00" },
                new[] { "Total ISV de ventas", "", F(r.IsvDebito) },
                new[] { "(-) ISV de devoluciones / notas de crédito", "", F(r.IsvDevoluciones) },
                new string?[] { },
                new[] { "COMPRAS (crédito fiscal)", "Base", "ISV" },
                new[] { "Compras gravadas", F(r.ComprasGravadas), F(r.IsvCredito) },
                new[] { "Compras exentas", F(r.ComprasExentas), "0.00" },
                new string?[] { },
                new[] { "Saldo a favor del mes anterior", "", F(r.SaldoFavorAnterior) },
                new[] { "Retenciones de ISV que te hicieron", "", F(r.Retenciones) },
                new[] { r.Resultado >= 0 ? "ISV A PAGAR" : "SALDO A FAVOR PARA EL PRÓXIMO MES", "", F(Math.Abs(r.Resultado)) },
            };
            return CsvExportHelper.Crear($"resumen-isv-{r.Desde:yyyy-MM}.csv", filas);
        }
    }
}
