using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace Kirkenta.Pages.Reportes
{
    /// <summary>Antigüedad de saldos de proveedores (cuentas por pagar).</summary>
    public class CuentasPorPagarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CuentasPorPagarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class FilaProveedor
        {
            public int ProveedorId { get; set; }
            public string Proveedor { get; set; } = "";
            public decimal Corriente { get; set; }
            public decimal D1a30 { get; set; }
            public decimal D31a60 { get; set; }
            public decimal D61a90 { get; set; }
            public decimal Mas90 { get; set; }
            public decimal Total => Corriente + D1a30 + D31a60 + D61a90 + Mas90;
        }

        public List<FilaProveedor> Filas { get; set; } = new();
        public FilaProveedor Totales { get; set; } = new() { Proveedor = "TOTAL" };

        /// <summary>Una orden de compra con lo que se debía, lo pagado y lo pendiente.</summary>
        public class OrdenEstado
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public int ProveedorId { get; set; }
            public string Proveedor { get; set; } = "";
            public DateTime Vence { get; set; }
            public decimal Total { get; set; }
            public decimal Saldo { get; set; }
            public decimal Pagado => Total - Saldo;
            public int DiasVencida => Math.Max(0, (DateTime.Today - Vence).Days);
            /// <summary>Pagada, Vencida, Abonada (pago parcial al día) o Pendiente.</summary>
            public string EstadoPago => Saldo <= 0 ? "Pagada" : DiasVencida > 0 ? "Vencida" : Saldo < Total ? "Abonada" : "Pendiente";
        }

        public List<OrdenEstado> Ordenes { get; set; } = new();
        public bool VerPagadas { get; set; }
        public decimal TotalVencido => Totales.Total - Totales.Corriente;
        public decimal PagadoUltimos30 { get; set; }

        private bool Cargar()
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Reportes", "CuentasPorPagar", "ver")) return false;

            var estadosExcluidos = new List<string> { "Borrador", "Cancelada", "Anulada" }; // List (no array): evita el bug de EF con spans en .NET 10
            var ordenes = _context.OrdenesCompra
                .Where(o => o.Saldo > 0 && !estadosExcluidos.Contains(o.Estado))
                .ToList();
            var proveedores = _context.Proveedores.ToDictionary(p => p.Id);
            DateTime Vence(Models.OrdenCompra o)
                => (o.FechaRecepcion ?? o.Fecha).Date.AddDays(proveedores.TryGetValue(o.ProveedorId, out var pr) ? pr.DiasCredito : 0);

            // Detalle por orden: las que se deben y, si se pide, las pagadas de los últimos 90 días
            var desdePagadas = DateTime.Today.AddDays(-90);
            var pagadas = VerPagadas
                ? _context.OrdenesCompra.Where(o => o.Saldo <= 0 && o.Total > 0 && !estadosExcluidos.Contains(o.Estado) && o.Fecha >= desdePagadas).ToList()
                : new List<Models.OrdenCompra>();
            Ordenes = ordenes.Concat(pagadas).Select(o => new OrdenEstado
            {
                Id = o.Id, Numero = o.Numero, ProveedorId = o.ProveedorId,
                Proveedor = proveedores.TryGetValue(o.ProveedorId, out var pr) ? pr.Nombre : "Sin proveedor",
                Vence = Vence(o), Total = o.Total, Saldo = Math.Max(0, o.Saldo)
            }).OrderBy(o => o.Saldo <= 0).ThenBy(o => o.Vence).ToList();

            var desde30 = DateTime.Today.AddDays(-30);
            PagadoUltimos30 = _context.PagosProveedor.Where(p => p.Fecha >= desde30).Sum(p => (decimal?)p.Monto) ?? 0m;

            foreach (var g in ordenes.GroupBy(o => o.ProveedorId))
            {
                proveedores.TryGetValue(g.Key, out var prov);
                var fila = new FilaProveedor
                {
                    ProveedorId = g.Key,
                    Proveedor = prov?.Nombre ?? "Sin proveedor"
                };
                foreach (var o in g)
                {
                    var dias = (DateTime.Today - Vence(o)).Days;
                    if (dias <= 0) fila.Corriente += o.Saldo;
                    else if (dias <= 30) fila.D1a30 += o.Saldo;
                    else if (dias <= 60) fila.D31a60 += o.Saldo;
                    else if (dias <= 90) fila.D61a90 += o.Saldo;
                    else fila.Mas90 += o.Saldo;
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

        public IActionResult OnGet(bool pagadas = false)
        {
            VerPagadas = pagadas;
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
            var filas = new List<string?[]> { new[] { "Proveedor", "Corriente", "1-30", "31-60", "61-90", "Más de 90", "Total" } };
            filas.AddRange(Filas.Select(f => new string?[] { f.Proveedor, F(f.Corriente), F(f.D1a30), F(f.D31a60), F(f.D61a90), F(f.Mas90), F(f.Total) }));
            filas.Add(new string?[] { "TOTAL", F(Totales.Corriente), F(Totales.D1a30), F(Totales.D31a60), F(Totales.D61a90), F(Totales.Mas90), F(Totales.Total) });
            return CsvExportHelper.Crear($"cuentas-por-pagar-{DateTime.Today:yyyyMMdd}.csv", filas);
        }
    }
}
