using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Referencias cruzadas de una venta (número de factura y cliente) para mostrarlas en cualquier listado
    /// y poder buscar por ellas.
    /// </summary>
    public static class VentaReferenciaHelper
    {
        public record RefVenta(int VentaId, string VentaNumero, int? FacturaId, string? FacturaNumero, int? ClienteId, string ClienteNombre);

        public const string ConsumidorFinal = "Consumidor final";

        public static Dictionary<int, RefVenta> Mapa(ApplicationDbContext context, IEnumerable<int> ventaIds)
        {
            var ids = ventaIds.Distinct().ToList();
            var res = new Dictionary<int, RefVenta>();
            if (ids.Count == 0) return res;

            var ventas = context.Ventas.AsNoTracking().Where(v => ids.Contains(v.Id))
                .Select(v => new { v.Id, v.Numero, v.ClienteId }).ToList();
            var clienteIds = ventas.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value).Distinct().ToList();
            var clientes = context.Clientes.AsNoTracking().Where(c => clienteIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Nombre }).ToDictionary(c => c.Id, c => c.Nombre);
            var facturas = context.Facturas.AsNoTracking()
                .Where(f => f.VentaId != null && ids.Contains(f.VentaId.Value) && f.Estado != "Anulada")
                .Select(f => new { f.Id, f.Numero, VentaId = f.VentaId!.Value }).ToList()
                .GroupBy(f => f.VentaId).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First());

            foreach (var v in ventas)
            {
                facturas.TryGetValue(v.Id, out var f);
                res[v.Id] = new RefVenta(v.Id, v.Numero, f?.Id, f?.Numero, v.ClienteId,
                    v.ClienteId.HasValue ? clientes.GetValueOrDefault(v.ClienteId.Value, ConsumidorFinal) : ConsumidorFinal);
            }
            return res;
        }

        /// <summary>¿La venta coincide con el texto buscado (número de venta, de factura o cliente)?</summary>
        public static bool Coincide(RefVenta r, string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return true;
            var t = texto.Trim();
            return r.VentaNumero.Contains(t, StringComparison.OrdinalIgnoreCase)
                || (r.FacturaNumero?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false)
                || r.ClienteNombre.Contains(t, StringComparison.OrdinalIgnoreCase);
        }
    }
}
