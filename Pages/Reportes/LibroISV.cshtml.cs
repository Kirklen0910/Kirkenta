using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace Kirkenta.Pages.Reportes
{
    /// <summary>Libro de ventas / ISV por período (base para la declaración mensual).</summary>
    public class LibroISVModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LibroISVModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? Desde { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? Hasta { get; set; }

        public class FilaLibro
        {
            public DateTime Fecha { get; set; }
            public string Numero { get; set; } = "";
            public string Cliente { get; set; } = "";
            public string? RTN { get; set; }
            public bool Anulada { get; set; }
            public decimal Exento { get; set; }
            public decimal Exonerado { get; set; }
            public decimal Gravado15 { get; set; }
            public decimal Isv15 { get; set; }
            public decimal Gravado18 { get; set; }
            public decimal Isv18 { get; set; }
            public decimal OtroGravado { get; set; }
            public decimal OtroIsv { get; set; }
            public decimal Ajustes { get; set; }
            public decimal Total { get; set; }
        }

        public List<FilaLibro> Filas { get; set; } = new();
        public FilaLibro Totales { get; set; } = new();
        public List<Devolucion> Devoluciones { get; set; } = new();
        public decimal TotalDevoluciones { get; set; }

        private bool Cargar()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Reportes", "ISV", "ver")) return false;

            var hoy = DateTime.Today;
            var desde = (Desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
            var hasta = (Hasta ?? hoy).Date;
            Desde = desde;
            Hasta = hasta;
            var hastaExcl = hasta.AddDays(1);

            // Mismo cálculo que la declaración mensual (DeclaracionISVHelper): libro y declaración siempre cuadran
            foreach (var l in DeclaracionISVHelper.Ventas(_context, desde, hastaExcl))
            {
                var fila = new FilaLibro
                {
                    Fecha = l.Fecha, Numero = l.Documento, Cliente = l.Tercero, RTN = l.RTN, Anulada = l.Anulada,
                    Exento = l.Exento, Exonerado = l.Exonerado,
                    Gravado15 = l.Gravado15, Isv15 = l.Isv15, Gravado18 = l.Gravado18, Isv18 = l.Isv18,
                    OtroGravado = l.OtroGravado, OtroIsv = l.OtroIsv, Total = l.Total
                };
                if (!fila.Anulada)
                {
                    var partes = fila.Exento + fila.Exonerado + fila.Gravado15 + fila.Isv15 + fila.Gravado18 + fila.Isv18 + fila.OtroGravado + fila.OtroIsv;
                    fila.Ajustes = fila.Total - partes; // redondeos de centavos
                }
                Filas.Add(fila);
            }

            Totales = new FilaLibro
            {
                Numero = "TOTAL",
                Exento = Filas.Sum(x => x.Exento),
                Exonerado = Filas.Sum(x => x.Exonerado),
                Gravado15 = Filas.Sum(x => x.Gravado15),
                Isv15 = Filas.Sum(x => x.Isv15),
                Gravado18 = Filas.Sum(x => x.Gravado18),
                Isv18 = Filas.Sum(x => x.Isv18),
                OtroGravado = Filas.Sum(x => x.OtroGravado),
                OtroIsv = Filas.Sum(x => x.OtroIsv),
                Ajustes = Filas.Sum(x => x.Ajustes),
                Total = Filas.Sum(x => x.Total)
            };

            Devoluciones = _context.Devoluciones
                .Where(d => d.Estado == "Aplicada" && d.Fecha >= desde && d.Fecha < hastaExcl)
                .OrderBy(d => d.Fecha).ToList();
            TotalDevoluciones = Devoluciones.Sum(d => d.Total);
            return true;
        }

        public IActionResult OnGet()
        {
            if (!Cargar())
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public IActionResult OnGetCsv()
        {
            if (!Cargar()) return RedirectToPage("/Index");

            string F(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);
            var filas = new List<string?[]>
            {
                new[] { "Fecha", "Factura", "Cliente", "RTN", "Estado", "Exento", "Exonerado", "Gravado 15%", "ISV 15%", "Gravado 18%", "ISV 18%", "Otro gravado", "Otro ISV", "Ajustes", "Total" }
            };
            filas.AddRange(Filas.Select(x => new string?[]
            {
                x.Fecha.ToString("dd/MM/yyyy"), x.Numero, x.Cliente, x.RTN, x.Anulada ? "ANULADA" : "Emitida",
                F(x.Exento), F(x.Exonerado), F(x.Gravado15), F(x.Isv15), F(x.Gravado18), F(x.Isv18), F(x.OtroGravado), F(x.OtroIsv), F(x.Ajustes), F(x.Total)
            }));
            filas.Add(new string?[]
            {
                "", "TOTAL", "", "", "", F(Totales.Exento), F(Totales.Exonerado), F(Totales.Gravado15), F(Totales.Isv15), F(Totales.Gravado18),
                F(Totales.Isv18), F(Totales.OtroGravado), F(Totales.OtroIsv), F(Totales.Ajustes), F(Totales.Total)
            });
            filas.Add(new string?[] { "", "Devoluciones/notas de crédito del período", "", "", "", F(TotalDevoluciones) });
            return CsvExportHelper.Crear($"libro-isv-{Desde:yyyyMMdd}-{Hasta:yyyyMMdd}.csv", filas);
        }
    }
}
