using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace Kirkenta.Pages.Reportes
{
    /// <summary>Antigüedad de saldos de clientes (cuentas por cobrar).</summary>
    public class CuentasPorCobrarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CuentasPorCobrarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class FilaCliente
        {
            public int ClienteId { get; set; }
            public string Cliente { get; set; } = "";
            public string? Telefono { get; set; }
            public decimal Corriente { get; set; }
            public decimal D1a30 { get; set; }
            public decimal D31a60 { get; set; }
            public decimal D61a90 { get; set; }
            public decimal Mas90 { get; set; }
            public decimal Total => Corriente + D1a30 + D31a60 + D61a90 + Mas90;
            /// <summary>Lo que ya paso su fecha de vencimiento.</summary>
            public decimal Vencido => D1a30 + D31a60 + D61a90 + Mas90;
        }

        public string EmpresaNombre { get; set; } = "";
        public List<FilaCliente> Filas { get; set; } = new();
        public FilaCliente Totales { get; set; } = new() { Cliente = "TOTAL" };

        private bool Cargar()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Reportes", "CuentasPorCobrar", "ver")) return false;

            var ventas = _context.Ventas
                .Where(v => v.EsCredito && v.SaldoPendiente > 0 && v.Estado != "Anulada")
                .ToList();
            var nombres = _context.Clientes.ToDictionary(c => c.Id, c => c.Nombre);
            var telefonos = _context.Clientes.ToDictionary(c => c.Id, c => c.Telefono);
            EmpresaNombre = _context.ConfiguracionEmpresa.Select(e => e.Nombre).FirstOrDefault() ?? "";

            foreach (var g in ventas.GroupBy(v => v.ClienteId ?? 0))
            {
                var fila = new FilaCliente
                {
                    ClienteId = g.Key,
                    Cliente = nombres.TryGetValue(g.Key, out var n) ? n : "Sin cliente",
                    Telefono = telefonos.TryGetValue(g.Key, out var tel) ? tel : null
                };
                foreach (var v in g)
                {
                    var venc = (v.FechaVencimientoCredito ?? v.Fecha).Date;
                    var dias = (DateTime.Today - venc).Days;
                    if (dias <= 0) fila.Corriente += v.SaldoPendiente;
                    else if (dias <= 30) fila.D1a30 += v.SaldoPendiente;
                    else if (dias <= 60) fila.D31a60 += v.SaldoPendiente;
                    else if (dias <= 90) fila.D61a90 += v.SaldoPendiente;
                    else fila.Mas90 += v.SaldoPendiente;
                }
                Filas.Add(fila);
            }

            Filas = Filas.OrderByDescending(f => f.Total).ToList();
            Totales.Corriente = Filas.Sum(f => f.Corriente);
            Totales.D1a30 = Filas.Sum(f => f.D1a30);
            Totales.D31a60 = Filas.Sum(f => f.D31a60);
            Totales.D61a90 = Filas.Sum(f => f.D61a90);
            Totales.Mas90 = Filas.Sum(f => f.Mas90);
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
            var filas = new List<string?[]> { new[] { "Cliente", "Corriente", "1-30", "31-60", "61-90", "Más de 90", "Total" } };
            filas.AddRange(Filas.Select(f => new string?[] { f.Cliente, F(f.Corriente), F(f.D1a30), F(f.D31a60), F(f.D61a90), F(f.Mas90), F(f.Total) }));
            filas.Add(new string?[] { "TOTAL", F(Totales.Corriente), F(Totales.D1a30), F(Totales.D31a60), F(Totales.D61a90), F(Totales.Mas90), F(Totales.Total) });
            return CsvExportHelper.Crear($"antiguedad-saldos-{DateTime.Today:yyyyMMdd}.csv", filas);
        }
    }
}
