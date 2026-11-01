using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Finanzas.Cobros
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CobroPendiente> Cobros { get; set; } = new();
        public decimal TotalPendiente { get; set; }
        public int CobrosVencidos { get; set; }
        public int CobrosProximos { get; set; }

        public class CobroPendiente
        {
            public int VentaId { get; set; }
            public string VentaNumero { get; set; } = "";
            public string? FacturaNumero { get; set; }
            public DateTime FechaVenta { get; set; }
            public string ClienteNombre { get; set; } = "";
            public string ClienteTelefono { get; set; } = "";
            public string ClienteRTN { get; set; } = "";
            public decimal TotalVenta { get; set; }
            public decimal SaldoPendiente { get; set; }
            public int DiasCredito { get; set; }
            public DateTime? FechaVencimiento { get; set; }
            public int DiasParaVencer { get; set; }
            public string EstadoAlerta { get; set; } = "Pendiente";
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "Cobros", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver los cobros pendientes.";
                return RedirectToPage("/Finanzas/Index");
            }

            var ventasCredito = _context.Ventas
                .AsNoTracking()
                .Where(v => v.EsCredito && v.SaldoPendiente > 0 && v.Estado != "Anulada")
                .ToList();

            var hoy = DateTime.Today;
            var idsClientes = ventasCredito.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value).Distinct().ToList();
            var idsVentasCredito = ventasCredito.Select(v => v.Id).ToList();
            var clientes = _context.Clientes.AsNoTracking().Where(c => idsClientes.Contains(c.Id)).ToDictionary(c => c.Id);

            // ===== FIX: puede haber varias facturas para una misma venta.
            // Agrupamos por VentaId y nos quedamos con la más reciente. =====
            var facturas = _context.Facturas
                .AsNoTracking()
                .Where(f => f.VentaId != null && idsVentasCredito.Contains(f.VentaId.Value))
                .GroupBy(f => f.VentaId!.Value)
                .Select(g => g.OrderByDescending(f => f.Id).First())
                .ToList()
                .ToDictionary(f => f.VentaId!.Value);

            foreach (var venta in ventasCredito)
            {
                if (!venta.ClienteId.HasValue) continue;
                var cliente = clientes.GetValueOrDefault(venta.ClienteId.Value);
                if (cliente == null) continue;

                var factura = facturas.GetValueOrDefault(venta.Id);

                var fechaVencimiento = venta.FechaVencimientoCredito
                    ?? factura?.FechaVencimiento
                    ?? (cliente.DiasCredito > 0 ? venta.Fecha.AddDays(cliente.DiasCredito) : (DateTime?)null);

                int diasParaVencer = 0;
                string estadoAlerta = "Pendiente";

                if (fechaVencimiento.HasValue)
                {
                    diasParaVencer = (fechaVencimiento.Value.Date - hoy).Days;
                    if (diasParaVencer < 0) estadoAlerta = "Vencido";
                    else if (diasParaVencer <= 7) estadoAlerta = "PorVencer";
                }

                Cobros.Add(new CobroPendiente
                {
                    VentaId = venta.Id,
                    VentaNumero = venta.Numero,
                    FacturaNumero = factura?.Numero,
                    FechaVenta = venta.Fecha,
                    ClienteNombre = cliente.Nombre,
                    ClienteTelefono = cliente.Telefono ?? "",
                    ClienteRTN = cliente.RTN ?? "",
                    TotalVenta = venta.Total,
                    SaldoPendiente = venta.SaldoPendiente,
                    DiasCredito = cliente.DiasCredito,
                    FechaVencimiento = fechaVencimiento,
                    DiasParaVencer = diasParaVencer,
                    EstadoAlerta = estadoAlerta
                });
            }

            Cobros = Cobros
                .OrderBy(c => c.FechaVencimiento ?? DateTime.MaxValue)
                .ThenBy(c => c.VentaNumero)
                .ToList();

            TotalPendiente = Cobros.Sum(c => c.SaldoPendiente);
            CobrosVencidos = Cobros.Count(c => c.EstadoAlerta == "Vencido");
            CobrosProximos = Cobros.Count(c => c.EstadoAlerta == "PorVencer");

            return Page();
        }
    }
}