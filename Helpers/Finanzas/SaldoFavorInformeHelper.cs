using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    public class SaldoFavorLinea
    {
        public DateTime Fecha { get; set; }
        public string Cliente { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Concepto { get; set; } = "";
        public int? VentaId { get; set; }
        public string? Venta { get; set; }
        public string? Factura { get; set; }
        public int? FacturaId { get; set; }
        public decimal Monto { get; set; }
    }

    /// <summary>
    /// Referencia (no es dinero que entra ni sale de caja): saldo a favor acreditado por devoluciones y aplicado en ventas
    /// durante el período. Explica por qué una factura grande puede cobrarse en efectivo por menos.
    /// </summary>
    public class SaldoFavorInforme
    {
        public decimal Acreditado { get; set; }
        public decimal Aplicado { get; set; }
        public List<SaldoFavorLinea> Lineas { get; set; } = new();
    }

    public static class SaldoFavorInformeHelper
    {
        public static SaldoFavorInforme Generar(ApplicationDbContext db, DateTime desde, DateTime hasta)
        {
            var inf = new SaldoFavorInforme();
            var movs = db.SaldoFavorMovimientos.AsNoTracking()
                .Where(m => m.Fecha >= desde && m.Fecha < hasta)
                .OrderBy(m => m.Fecha).ToList();
            if (movs.Count == 0) return inf;

            var cids = movs.Select(m => m.ClienteId).Distinct().ToList();
            var clientes = db.Clientes.AsNoTracking().Where(c => cids.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Nombre);
            var refs = VentaReferenciaHelper.Mapa(db, movs.Where(m => m.VentaId.HasValue).Select(m => m.VentaId!.Value));

            foreach (var m in movs)
            {
                refs.TryGetValue(m.VentaId ?? 0, out var r);
                inf.Lineas.Add(new SaldoFavorLinea
                {
                    Fecha = m.Fecha,
                    Cliente = clientes.GetValueOrDefault(m.ClienteId, "—"),
                    Tipo = m.Tipo switch { "Credito" => "Acreditado", "Uso" => "Aplicado en venta", "Reverso" => "Reverso", _ => m.Tipo },
                    Concepto = m.Concepto ?? "",
                    VentaId = m.VentaId, Venta = r?.VentaNumero, Factura = r?.FacturaNumero, FacturaId = r?.FacturaId,
                    Monto = m.Monto
                });
            }
            inf.Acreditado = movs.Where(m => m.Monto > 0).Sum(m => m.Monto);
            inf.Aplicado = -movs.Where(m => m.Monto < 0).Sum(m => m.Monto);
            return inf;
        }
    }
}
