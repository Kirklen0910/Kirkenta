using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>Una factura de venta o una compra recibida, separada por tipo de impuesto.</summary>
    public class LineaFiscal
    {
        public DateTime Fecha { get; set; }
        public string Documento { get; set; } = "";
        public string Tercero { get; set; } = "";
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
        public decimal Total { get; set; }

        public decimal IsvTotal => Isv15 + Isv18 + OtroIsv;
        public decimal GravadoTotal => Gravado15 + Gravado18 + OtroGravado;

        /// <summary>Suma una línea de documento a la clasificación (según el porcentaje de ISV).</summary>
        public void Agregar(decimal porcentaje, decimal baseLinea, decimal isv, bool exonerada)
        {
            if (porcentaje == 0) { if (exonerada) Exonerado += baseLinea; else Exento += baseLinea; }
            else if (porcentaje == 15m) { Gravado15 += baseLinea; Isv15 += isv; }
            else if (porcentaje == 18m) { Gravado18 += baseLinea; Isv18 += isv; }
            else { OtroGravado += baseLinea; OtroIsv += isv; }
        }
    }

    public class ResumenISV
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }

        // Ventas (débito fiscal)
        public decimal VentasExentas { get; set; }
        public decimal VentasExoneradas { get; set; }
        public decimal VentasGravadas15 { get; set; }
        public decimal Isv15 { get; set; }
        public decimal VentasGravadas18 { get; set; }
        public decimal Isv18 { get; set; }
        public decimal VentasOtroGravado { get; set; }
        public decimal IsvOtro { get; set; }
        public decimal IsvDebito => Isv15 + Isv18 + IsvOtro;
        public int FacturasEmitidas { get; set; }
        public int FacturasAnuladas { get; set; }

        // Compras (crédito fiscal)
        public decimal ComprasExentas { get; set; }
        public decimal ComprasGravadas { get; set; }
        public decimal IsvCredito { get; set; }
        public int Compras { get; set; }

        // Ajustes que escribe el usuario
        public decimal SaldoFavorAnterior { get; set; }
        public decimal Retenciones { get; set; }

        // Devoluciones / notas de crédito del mes
        public decimal Devoluciones { get; set; }
        /// <summary>ISV contenido en las devoluciones del mes: reduce el débito fiscal.</summary>
        public decimal IsvDevoluciones { get; set; }

        /// <summary>Positivo = ISV a pagar; negativo = saldo a favor.</summary>
        public decimal Resultado => IsvDebito - IsvDevoluciones - IsvCredito - SaldoFavorAnterior - Retenciones;
        public decimal IsvAPagar => Resultado > 0 ? Resultado : 0;
        public decimal SaldoFavorSiguiente => Resultado < 0 ? -Resultado : 0;
    }

    /// <summary>Arma los libros de ventas y compras y el resumen mensual del ISV a partir de los datos del sistema.</summary>
    public static class DeclaracionISVHelper
    {
        public static List<LineaFiscal> Ventas(ApplicationDbContext db, DateTime desde, DateTime hastaExclusivo)
        {
            var facturas = db.Facturas.AsNoTracking()
                .Where(f => f.Fecha >= desde && f.Fecha < hastaExclusivo)
                .OrderBy(f => f.Fecha).ThenBy(f => f.Numero).ToList();
            var ids = facturas.Select(f => f.Id).ToList();
            var detalles = db.DetalleFacturas.AsNoTracking().Where(d => ids.Contains(d.FacturaId)).ToList()
                .GroupBy(d => d.FacturaId).ToDictionary(g => g.Key, g => g.ToList());
            var clientes = db.Clientes.AsNoTracking().ToDictionary(c => c.Id, c => c);

            var res = new List<LineaFiscal>();
            foreach (var f in facturas)
            {
                clientes.TryGetValue(f.ClienteId, out var cli);
                var l = new LineaFiscal
                {
                    Fecha = f.Fecha, Documento = f.Numero, Tercero = cli?.Nombre ?? "Consumidor final", RTN = cli?.RTN,
                    Anulada = f.Estado == "Anulada"
                };
                if (!l.Anulada)
                {
                    // El descuento general se prorratea: las lineas lo traen SIN descontar y el ISV real se calculo despues.
                    decimal factor = 1m;
                    if (detalles.TryGetValue(f.Id, out var dets))
                    {
                        var sumaBase = dets.Sum(d => d.Subtotal);
                        if (f.Descuento > 0 && sumaBase + f.MontoEnvio > 0)
                            factor = Math.Max(0m, (sumaBase + f.MontoEnvio - f.Descuento) / (sumaBase + f.MontoEnvio));
                        foreach (var d in dets)
                            l.Agregar(d.ImpuestoPorcentaje, Math.Round(d.Subtotal * factor, 2),
                                      Math.Round((d.Total - d.Subtotal) * factor, 2), f.EsExonerada);
                    }
                    // ISV del envio (u otro cargo sin linea): lo que falta para igualar el ISV real de la factura
                    var faltante = Math.Round(f.Impuestos - l.IsvTotal, 2);
                    if (faltante > 0.01m && f.MontoEnvio > 0 && !f.EsExonerada)
                    {
                        var baseEnvio = Math.Round(f.MontoEnvio * factor, 2);
                        var tasa = baseEnvio > 0 ? Math.Round(faltante / baseEnvio * 100m, 0) : 0m;
                        l.Agregar(baseEnvio > 0 ? tasa : 15m, baseEnvio, faltante, false);
                    }
                    l.Total = f.Total;
                }
                res.Add(l);
            }
            return res;
        }

        /// <summary>Compras recibidas en el período (por fecha de recepción). En recepciones parciales solo cuenta lo recibido.</summary>
        public static List<LineaFiscal> Compras(ApplicationDbContext db, DateTime desde, DateTime hastaExclusivo)
        {
            var ordenes = db.OrdenesCompra.AsNoTracking()
                .Where(o => (o.Estado == "Recibida" || o.Estado == "RecibidaParcial")
                            && (o.FechaRecepcion ?? o.Fecha) >= desde && (o.FechaRecepcion ?? o.Fecha) < hastaExclusivo)
                .OrderBy(o => o.FechaRecepcion ?? o.Fecha).ThenBy(o => o.Numero).ToList();
            var ids = ordenes.Select(o => o.Id).ToList();
            var detalles = db.DetalleOrdenesCompra.AsNoTracking().Where(d => ids.Contains(d.OrdenCompraId)).ToList()
                .GroupBy(d => d.OrdenCompraId).ToDictionary(g => g.Key, g => g.ToList());
            var provs = db.Proveedores.AsNoTracking().ToDictionary(p => p.Id, p => p);

            var res = new List<LineaFiscal>();
            foreach (var o in ordenes)
            {
                provs.TryGetValue(o.ProveedorId, out var p);
                var l = new LineaFiscal { Fecha = o.FechaRecepcion ?? o.Fecha, Documento = o.Numero, Tercero = p?.Nombre ?? "Proveedor", RTN = p?.RTN };
                if (detalles.TryGetValue(o.Id, out var dets))
                {
                    foreach (var d in dets)
                    {
                        var factor = o.Estado == "Recibida" ? 1m : (d.Cantidad > 0 ? Math.Min(1m, d.CantidadRecibida / d.Cantidad) : 0m);
                        if (factor <= 0) continue;
                        var baseLinea = Math.Round(d.Subtotal * factor, 2);
                        var total = Math.Round(d.Total * factor, 2);
                        l.Agregar(d.ImpuestoPorcentaje, baseLinea, total - baseLinea, false);
                        l.Total += total;
                    }
                }
                res.Add(l);
            }
            return res;
        }

        /// <summary>Resumen del ISV del mes. Puro: no toca la base de datos.</summary>
        public static ResumenISV Resumir(DateTime desde, DateTime hasta, IEnumerable<LineaFiscal> ventas, IEnumerable<LineaFiscal> compras,
            decimal saldoFavorAnterior = 0, decimal retenciones = 0, decimal devoluciones = 0, decimal isvDevoluciones = 0)
        {
            var emitidas = ventas.Where(v => !v.Anulada).ToList();
            var comp = compras.ToList();
            return new ResumenISV
            {
                Desde = desde, Hasta = hasta,
                VentasExentas = emitidas.Sum(v => v.Exento),
                VentasExoneradas = emitidas.Sum(v => v.Exonerado),
                VentasGravadas15 = emitidas.Sum(v => v.Gravado15), Isv15 = emitidas.Sum(v => v.Isv15),
                VentasGravadas18 = emitidas.Sum(v => v.Gravado18), Isv18 = emitidas.Sum(v => v.Isv18),
                VentasOtroGravado = emitidas.Sum(v => v.OtroGravado), IsvOtro = emitidas.Sum(v => v.OtroIsv),
                FacturasEmitidas = emitidas.Count, FacturasAnuladas = ventas.Count(v => v.Anulada),
                ComprasExentas = comp.Sum(c => c.Exento + c.Exonerado),
                ComprasGravadas = comp.Sum(c => c.GravadoTotal), IsvCredito = comp.Sum(c => c.IsvTotal), Compras = comp.Count,
                SaldoFavorAnterior = Math.Max(0, saldoFavorAnterior), Retenciones = Math.Max(0, retenciones),
                Devoluciones = devoluciones,
                IsvDevoluciones = Math.Max(0, isvDevoluciones)
            };
        }

        /// <summary>
        /// ISV contenido en las devoluciones aplicadas del periodo (proporcion ISV/Total de la venta o factura de origen).
        /// Las notas de credito reducen el debito fiscal.
        /// </summary>
        public static decimal IsvDevolucionesDelPeriodo(ApplicationDbContext db, DateTime desde, DateTime hastaExclusivo)
        {
            var devs = db.Devoluciones.AsNoTracking()
                .Where(d => d.Estado == "Aplicada" && d.Fecha >= desde && d.Fecha < hastaExclusivo).ToList();
            if (devs.Count == 0) return 0m;
            var ventaIds = devs.Where(d => d.VentaId.HasValue).Select(d => d.VentaId!.Value).Distinct().ToList();
            var facturaIds = devs.Where(d => !d.VentaId.HasValue && d.FacturaId.HasValue).Select(d => d.FacturaId!.Value).Distinct().ToList();
            var ventas = db.Ventas.AsNoTracking().Where(v => ventaIds.Contains(v.Id)).ToDictionary(v => v.Id, v => (v.Impuestos, v.Total));
            var facturas = db.Facturas.AsNoTracking().Where(f => facturaIds.Contains(f.Id)).ToDictionary(f => f.Id, f => (f.Impuestos, f.Total));

            decimal total = 0;
            foreach (var d in devs)
            {
                (decimal imp, decimal tot) origen = (0m, 0m);
                if (d.VentaId.HasValue && ventas.TryGetValue(d.VentaId.Value, out var v)) origen = (v.Impuestos, v.Total);
                else if (d.FacturaId.HasValue && facturas.TryGetValue(d.FacturaId.Value, out var f)) origen = (f.Impuestos, f.Total);
                if (origen.tot > 0) total += Math.Round(d.Total * origen.imp / origen.tot, 2);
            }
            return total;
        }

        public static decimal DevolucionesDelPeriodo(ApplicationDbContext db, DateTime desde, DateTime hastaExclusivo)
            => db.Devoluciones.AsNoTracking().Where(d => d.Estado == "Aplicada" && d.Fecha >= desde && d.Fecha < hastaExclusivo).Sum(d => (decimal?)d.Total) ?? 0m;
    }
}
