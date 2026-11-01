using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.EntregasBodega
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<VentaPendiente> Pendientes { get; set; } = new();
        public List<EntregaBodega> Recientes { get; set; } = new();
        public Dictionary<int, string> Ventas { get; set; } = new();
        public bool PuedeCrear { get; set; }
        public Dictionary<int, VentaReferenciaHelper.RefVenta> Refs { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        public class VentaPendiente
        {
            public int VentaId { get; set; }
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public string Cliente { get; set; } = "Consumidor final";
            public int? ClienteId { get; set; }
            public decimal Vendido { get; set; }
            public decimal Entregado { get; set; }
            public decimal Pendiente { get; set; }
        }

        public IActionResult OnGet()
        {
            var u = User.Identity?.Name;
            var puedeVer = PermisoHelper.UsuarioTienePermiso(_context, u, "Ventas", "Index", "ver")
                        || PermisoHelper.UsuarioTienePermiso(_context, u, "Inventario", "Salidas", "ver");
            if (!puedeVer)
            {
                TempData["Error"] = "No tienes permiso para ver entregas";
                return RedirectToPage("/Index");
            }
            PuedeCrear = PermisoHelper.UsuarioTienePermiso(_context, u, "Ventas", "Create", "crear")
                      || PermisoHelper.UsuarioTienePermiso(_context, u, "Inventario", "Salidas", "crear");

            var ventas = _context.Ventas.AsNoTracking()
                .Where(v => v.ControlEntrega && v.Estado != "Anulada")
                .OrderByDescending(v => v.Fecha).Take(300).ToList();
            var clienteIds = ventas.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value).Distinct().ToList();
            var clientes = _context.Clientes.AsNoTracking().Where(c => clienteIds.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Nombre);

            var refsPend = VentaReferenciaHelper.Mapa(_context, ventas.Select(v => v.Id));
            foreach (var v in ventas)
            {
                if (!string.IsNullOrWhiteSpace(Q) && refsPend.TryGetValue(v.Id, out var rp) && !VentaReferenciaHelper.Coincide(rp, Q)) continue;
                var lineas = EntregaBodegaHelper.Lineas(_context, v.Id);
                var pend = lineas.Sum(l => l.Pendiente);
                if (pend <= 0) continue;
                Pendientes.Add(new VentaPendiente
                {
                    VentaId = v.Id,
                    Numero = v.Numero,
                    Fecha = v.Fecha,
                    ClienteId = v.ClienteId,
                    Cliente = v.ClienteId.HasValue ? clientes.GetValueOrDefault(v.ClienteId.Value, "—") : "Consumidor final",
                    Vendido = lineas.Sum(l => l.Vendida - l.Devuelta),
                    Entregado = lineas.Sum(l => l.Entregada),
                    Pendiente = pend
                });
            }

            var todas = _context.EntregasBodega.AsNoTracking().OrderByDescending(e => e.Fecha).ThenByDescending(e => e.Id).Take(string.IsNullOrWhiteSpace(Q) ? 30 : 300).ToList();
            Refs = VentaReferenciaHelper.Mapa(_context, todas.Select(e => e.VentaId).Concat(ventas.Select(v => v.Id)));
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var t = Q.Trim();
                todas = todas.Where(e => e.Numero.Contains(t, StringComparison.OrdinalIgnoreCase)
                    || (Refs.TryGetValue(e.VentaId, out var r) && VentaReferenciaHelper.Coincide(r, t))).Take(50).ToList();
            }
            Recientes = todas;
            Ventas = Refs.ToDictionary(k => k.Key, k => k.Value.VentaNumero);
            return Page();
        }
    }
}
