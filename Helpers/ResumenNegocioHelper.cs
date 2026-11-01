using System.Net;
using System.Text;
using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    public record ResumenNegocio(
        DateTime Fecha, decimal VentasHoy, int CantidadHoy, decimal VentasAyer,
        decimal VentasMes, decimal VentasMesAnterior, decimal UtilidadMes, decimal IngresosMes,
        decimal PorCobrar, decimal PorCobrarVencido, int ProductosBajoMinimo, string? AvisoRespaldo)
    {
        public decimal VariacionMesPct => VentasMesAnterior > 0 ? Math.Round((VentasMes - VentasMesAnterior) / VentasMesAnterior * 100, 1) : 0;
        public decimal MargenPct => IngresosMes > 0 ? Math.Round(UtilidadMes / IngresosMes * 100, 1) : 0;
    }

    /// <summary>Cifras clave del negocio para el correo diario al dueño.</summary>
    public static class ResumenNegocioHelper
    {
        public static ResumenNegocio Calcular(ApplicationDbContext db, DateTime ahora)
        {
            var hoy = ahora.Date;
            var ini = new DateTime(hoy.Year, hoy.Month, 1);
            var fin = ini.AddMonths(1);
            var iniAnt = ini.AddMonths(-1);
            var corte = iniAnt.AddDays(Math.Min(hoy.Day, DateTime.DaysInMonth(iniAnt.Year, iniAnt.Month)));

            decimal Suma(DateTime d, DateTime h) =>
                db.Ventas.AsNoTracking().Where(v => v.Fecha >= d && v.Fecha < h && v.Estado == "Completada").Sum(v => (decimal?)v.Total) ?? 0m;

            var ventasHoy = db.Ventas.AsNoTracking().Where(v => v.Fecha >= hoy && v.Fecha < hoy.AddDays(1) && v.Estado == "Completada").Select(v => v.Total).ToList();
            var lineas = db.DetalleVentas.AsNoTracking()
                .Join(db.Ventas.AsNoTracking().Where(v => v.Fecha >= ini && v.Fecha < fin && v.Estado == "Completada"),
                      d => d.VentaId, v => v.Id, (d, v) => new { d.Subtotal, d.CostoTotalLinea }).ToList();
            var ingresos = lineas.Sum(x => x.Subtotal);

            var cxc = db.Ventas.AsNoTracking().Where(v => v.EsCredito && v.SaldoPendiente > 0 && v.Estado != "Anulada")
                .Select(v => new { v.SaldoPendiente, v.Fecha, v.FechaVencimientoCredito }).ToList();

            var bajo = db.Productos.AsNoTracking().Count(p => p.Activo && p.Stock <= p.StockMinimo);
            var aviso = RespaldoEstado.Evaluar(RespaldoEstado.Leer().Ultimo, ahora)?.Texto;

            return new ResumenNegocio(
                hoy, ventasHoy.Sum(), ventasHoy.Count, Suma(hoy.AddDays(-1), hoy),
                Suma(ini, fin), Suma(iniAnt, corte), ingresos - lineas.Sum(x => x.CostoTotalLinea), ingresos,
                cxc.Sum(x => x.SaldoPendiente), cxc.Where(x => (x.FechaVencimientoCredito ?? x.Fecha).Date < hoy).Sum(x => x.SaldoPendiente),
                bajo, aviso);
        }

        private static string L(decimal v) => "L. " + v.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);

        public static string Asunto(string empresa, ResumenNegocio r) => $"{empresa}: resumen del {r.Fecha:dd/MM/yyyy}";

        public static string ToHtml(string empresa, ResumenNegocio r)
        {
            string H(string s) => WebUtility.HtmlEncode(s);
            var flecha = r.VariacionMesPct >= 0 ? "▲" : "▼";
            var sb = new StringBuilder();
            sb.Append("<div style=\"font-family:Segoe UI,Arial,sans-serif;max-width:560px;margin:auto;color:#1f2937\">");
            sb.Append($"<h2 style=\"margin:0 0 4px\">{H(empresa)}</h2><p style=\"margin:0 0 16px;color:#6b7280\">Resumen del {r.Fecha:dddd dd 'de' MMMM}</p>");
            sb.Append("<table style=\"width:100%;border-collapse:collapse\">");
            void Fila(string k, string v, string? nota = null)
                => sb.Append($"<tr><td style=\"padding:8px 0;border-bottom:1px solid #e5e7eb\">{H(k)}</td><td style=\"padding:8px 0;border-bottom:1px solid #e5e7eb;text-align:right\"><strong>{H(v)}</strong>{(nota == null ? "" : "<br><span style=\"color:#6b7280;font-size:12px\">" + H(nota) + "</span>")}</td></tr>");
            Fila("Ventas de hoy", L(r.VentasHoy), $"{r.CantidadHoy} venta(s) · ayer {L(r.VentasAyer)}");
            Fila("Ventas del mes", L(r.VentasMes), $"{flecha} {Math.Abs(r.VariacionMesPct)}% vs el mes pasado a la misma fecha ({L(r.VentasMesAnterior)})");
            Fila("Utilidad bruta del mes", L(r.UtilidadMes), $"Margen {r.MargenPct}%");
            Fila("Por cobrar", L(r.PorCobrar), r.PorCobrarVencido > 0 ? $"{L(r.PorCobrarVencido)} vencido" : "Nada vencido");
            Fila("Productos en o bajo el mínimo", r.ProductosBajoMinimo.ToString());
            sb.Append("</table>");
            if (!string.IsNullOrEmpty(r.AvisoRespaldo))
                sb.Append($"<p style=\"margin-top:16px;padding:10px;background:#fef3c7;border-radius:6px\">💾 {H(r.AvisoRespaldo)}</p>");
            sb.Append("<p style=\"margin-top:20px;color:#9ca3af;font-size:12px\">Enviado automáticamente por Kirkenta.</p></div>");
            return sb.ToString();
        }
    }
}
