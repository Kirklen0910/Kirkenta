using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    public class ResumenMetodo
    {
        public string Clave { get; set; } = "";      // lo que se usa como filtro
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public decimal Total { get; set; }
        public int Cantidad { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class TransaccionInforme
    {
        public DateTime Fecha { get; set; }
        public string Documento { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string Metodo { get; set; } = "";
        public string? Referencia { get; set; }
        public decimal Monto { get; set; }
        public string Clave { get; set; } = "";     // método normalizado ("credito" para ventas a crédito)
        public string Tipo { get; set; } = "cobro"; // cobro | credito
    }

    public class EfectivoCaja
    {
        public decimal SaldoInicial { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Egresos { get; set; }
        public decimal Esperado { get; set; }
    }

    public class InformeMetodosPago
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }          // exclusivo
        public string Filtro { get; set; } = "todos";
        public string FiltroEtiqueta { get; set; } = "Todos los métodos";

        public List<ResumenMetodo> Metodos { get; set; } = new();       // cobros por método (siempre todos)
        public decimal TotalCobrado { get; set; }
        public int TotalTransacciones { get; set; }

        public decimal VentasContado { get; set; }
        public decimal VentasCredito { get; set; }
        public int CantidadCredito { get; set; }
        public decimal SaldoCreditoPendiente { get; set; }

        public List<ResumenMetodo> Egresos { get; set; } = new();       // salidas por forma de pago
        public decimal TotalEgresos { get; set; }

        public List<TransaccionInforme> Todas { get; set; } = new();    // cobros + ventas a crédito (para filtrar en pantalla)
        public EfectivoCaja? Efectivo { get; set; }                     // solo cuando se conoce la caja (cierre / previo)
        public SaldoFavorInforme SaldoFavor { get; set; } = new();       // referencia: saldo a favor acreditado / aplicado
        public DescuentosInforme Descuentos { get; set; } = new();       // descuentos otorgados en el período
        public List<TransaccionInforme> Detalle { get; set; } = new();  // según el filtro
        public decimal TotalDetalle { get; set; }
    }

    /// <summary>
    /// Informe de cómo se mueve el negocio por método de pago: efectivo, tarjeta, transferencia, crédito y cualquier
    /// otro método configurado. Se usa en el informe del cierre de caja y en el asistente.
    /// </summary>
    public static class InformeMetodosPagoHelper
    {
        public static string Norm(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var d = s.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in d)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(ch);
            return sb.ToString();
        }

        /// <param name="desde">Inicio (inclusive).</param>
        /// <param name="hasta">Fin (exclusivo).</param>
        /// <param name="filtro">"todos", "credito" o parte del nombre/tipo de un método (efectivo, tarjeta, transferencia...).</param>
        public static InformeMetodosPago Generar(ApplicationDbContext db, DateTime desde, DateTime hasta, string? filtro = null)
        {
            var inf = new InformeMetodosPago { Desde = desde, Hasta = hasta };
            var f = Norm(filtro);
            if (string.IsNullOrEmpty(f)) f = "todos";
            inf.Filtro = f;

            var metodos = db.MetodosPago.AsNoTracking().ToList();
            var metodosId = metodos.ToDictionary(m => m.Id);

            // ── Cobros reales (pagos con monto) ──
            var pagos = db.Pagos.AsNoTracking()
                .Where(p => p.Fecha >= desde && p.Fecha < hasta && p.Monto > 0)
                .OrderBy(p => p.Fecha).ToList();

            // Un pago de una venta anulada o de una factura anulada no cuenta
            var ventaIds = pagos.Where(p => p.VentaId.HasValue).Select(p => p.VentaId!.Value).Distinct().ToList();
            var facturaIds = pagos.Where(p => p.FacturaId.HasValue).Select(p => p.FacturaId!.Value).Distinct().ToList();
            var ventas = db.Ventas.AsNoTracking().Where(v => ventaIds.Contains(v.Id)).ToDictionary(v => v.Id);
            var facturas = db.Facturas.AsNoTracking().Where(x => facturaIds.Contains(x.Id)).ToDictionary(x => x.Id);
            pagos = pagos.Where(p =>
                !(p.VentaId.HasValue && ventas.TryGetValue(p.VentaId.Value, out var v) && v.Estado == "Anulada") &&
                !(p.FacturaId.HasValue && facturas.TryGetValue(p.FacturaId.Value, out var fa) && fa.Estado == "Anulada")).ToList();

            var clienteIds = pagos.Select(p => p.ClienteId).Distinct().ToList();
            var clientes = db.Clientes.AsNoTracking().Where(c => clienteIds.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Nombre);

            var total = pagos.Sum(p => p.Monto);
            inf.TotalCobrado = total;
            inf.TotalTransacciones = pagos.Count;

            inf.Metodos = pagos.GroupBy(p => p.MetodoPagoId)
                .Select(g =>
                {
                    metodosId.TryGetValue(g.Key, out var m);
                    var nombre = m?.Nombre ?? "Otro";
                    return new ResumenMetodo
                    {
                        Clave = Norm(nombre), Nombre = nombre, Tipo = m?.Tipo ?? "",
                        Total = g.Sum(x => x.Monto), Cantidad = g.Count(),
                        Porcentaje = total > 0 ? Math.Round(g.Sum(x => x.Monto) / total * 100m, 1) : 0
                    };
                }).OrderByDescending(x => x.Total).ToList();

            // Todos los métodos activos aparecen (aunque no hayan tenido cobros) para poder filtrarlos
            foreach (var m in metodos.Where(x => x.Activo))
            {
                var clave = Norm(m.Nombre);
                if (clave.Contains("credito") && !clave.Contains("tarjeta")) continue; // el crédito tiene su propia vista
                if (inf.Metodos.Any(x => x.Clave == clave)) continue;
                inf.Metodos.Add(new ResumenMetodo { Clave = clave, Nombre = m.Nombre, Tipo = m.Tipo ?? "" });
            }

            // ── Ventas a crédito emitidas ──
            var cred = db.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= desde && v.Fecha < hasta && v.Estado != "Anulada" && v.EsCredito).ToList();
            inf.VentasCredito = cred.Sum(v => v.Total);
            inf.CantidadCredito = cred.Count;
            inf.SaldoCreditoPendiente = cred.Sum(v => v.SaldoPendiente);
            inf.VentasContado = db.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= desde && v.Fecha < hasta && v.Estado != "Anulada" && !v.EsCredito).Sum(v => (decimal?)v.Total) ?? 0;

            // ── Salidas por forma de pago ──
            var egr = db.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Estado == "Activo" && m.Tipo == "Egreso" && m.Fecha >= desde && m.Fecha < hasta && m.Origen != "Apertura"
                         && (m.Notas == null || !m.Notas.Contains(SaldoHelper.MarcaSoloContable)))
                .Select(m => new { m.FormaPago, m.Monto }).ToList();
            inf.TotalEgresos = egr.Sum(x => x.Monto);
            inf.Egresos = egr.GroupBy(x => string.IsNullOrWhiteSpace(x.FormaPago) ? "Sin forma de pago" : x.FormaPago!)
                .Select(g => new ResumenMetodo
                {
                    Clave = Norm(g.Key), Nombre = g.Key, Total = g.Sum(x => x.Monto), Cantidad = g.Count(),
                    Porcentaje = inf.TotalEgresos > 0 ? Math.Round(g.Sum(x => x.Monto) / inf.TotalEgresos * 100m, 1) : 0
                }).OrderByDescending(x => x.Total).ToList();

            // ── Todas las transacciones (cobros + ventas a crédito) para filtrar en pantalla ──
            var credCliIds = cred.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value).Distinct().ToList();
            var credNombres = db.Clientes.AsNoTracking()
                .Where(c => credCliIds.Contains(c.Id))
                .ToDictionary(c => c.Id, c => c.Nombre);
            foreach (var p in pagos)
            {
                metodosId.TryGetValue(p.MetodoPagoId, out var mm);
                var doc = p.VentaId.HasValue && ventas.TryGetValue(p.VentaId.Value, out var vv) ? vv.Numero
                        : p.FacturaId.HasValue && facturas.TryGetValue(p.FacturaId.Value, out var ff) ? ff.Numero : "—";
                inf.Todas.Add(new TransaccionInforme
                {
                    Fecha = p.Fecha, Documento = doc,
                    Cliente = clientes.TryGetValue(p.ClienteId, out var cn) ? cn : "Consumidor final",
                    Metodo = mm?.Nombre ?? "Otro", Clave = Norm(mm?.Nombre ?? "Otro"), Referencia = p.Referencia, Monto = p.Monto, Tipo = "cobro"
                });
            }
            foreach (var v in cred.OrderBy(x => x.Fecha))
            {
                inf.Todas.Add(new TransaccionInforme
                {
                    Fecha = v.Fecha, Documento = v.Numero,
                    Cliente = v.ClienteId.HasValue && credNombres.TryGetValue(v.ClienteId.Value, out var cc) ? cc : "Consumidor final",
                    Metodo = "Crédito", Clave = "credito", Monto = v.Total, Tipo = "credito"
                });
            }
            inf.Todas = inf.Todas.OrderBy(x => x.Fecha).ToList();

            // ── Detalle según filtro ──
            if (f == "credito" || f == "a credito")
            {
                inf.Filtro = "credito";
                inf.FiltroEtiqueta = "Ventas a crédito";
                var cid = cred.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value).Distinct().ToList();
                var nombres = db.Clientes.AsNoTracking().Where(c => cid.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Nombre);
                inf.Detalle = cred.OrderBy(v => v.Fecha).Select(v => new TransaccionInforme
                {
                    Fecha = v.Fecha, Documento = v.Numero,
                    Cliente = v.ClienteId.HasValue && nombres.TryGetValue(v.ClienteId.Value, out var n) ? n : "Consumidor final",
                    Metodo = "Crédito", Monto = v.Total
                }).ToList();
            }
            else
            {
                IEnumerable<Pago> sel = pagos;
                if (f != "todos")
                {
                    sel = pagos.Where(p =>
                    {
                        if (!metodosId.TryGetValue(p.MetodoPagoId, out var m)) return false;
                        return Norm(m.Nombre).Contains(f) || Norm(m.Tipo).Contains(f);
                    });
                    var m0 = metodos.FirstOrDefault(m => Norm(m.Nombre).Contains(f) || Norm(m.Tipo).Contains(f));
                    inf.FiltroEtiqueta = m0?.Nombre ?? filtro ?? f;
                }
                inf.Detalle = sel.Select(p =>
                {
                    metodosId.TryGetValue(p.MetodoPagoId, out var m);
                    var doc = p.VentaId.HasValue && ventas.TryGetValue(p.VentaId.Value, out var v) ? v.Numero
                            : p.FacturaId.HasValue && facturas.TryGetValue(p.FacturaId.Value, out var fa) ? fa.Numero : "—";
                    return new TransaccionInforme
                    {
                        Fecha = p.Fecha, Documento = doc,
                        Cliente = clientes.TryGetValue(p.ClienteId, out var n) ? n : "Consumidor final",
                        Metodo = m?.Nombre ?? "Otro", Referencia = p.Referencia, Monto = p.Monto
                    };
                }).ToList();
            }
            inf.TotalDetalle = inf.Detalle.Sum(x => x.Monto);
            inf.Descuentos = DescuentosInformeHelper.Generar(db, desde, hasta);
            inf.SaldoFavor = SaldoFavorInformeHelper.Generar(db, desde, hasta);
            return inf;
        }

        /// <summary>Rango del informe de un cierre ya hecho: desde la apertura hasta el cierre.</summary>
        public static (DateTime desde, DateTime hasta) RangoDeCierre(ApplicationDbContext db, CierreCaja cierre)
        {
            var ap = cierre.AperturaId.HasValue ? db.AperturasCaja.AsNoTracking().FirstOrDefault(a => a.Id == cierre.AperturaId.Value) : null;
            var d = ap != null ? ap.FechaApertura : cierre.Fecha.Date;
            var h = cierre.FechaCierre > d ? cierre.FechaCierre.AddSeconds(1) : cierre.Fecha.Date.AddDays(1);
            return (d, h);
        }

        /// <summary>Informe previo (antes de cerrar): desde la apertura activa de la caja hasta ahora.</summary>
        public static InformeMetodosPago GenerarPrevio(ApplicationDbContext db, int cuentaId, DateTime fecha, string? filtro = null)
        {
            var ap = db.AperturasCaja.AsNoTracking().Where(a => a.CuentaId == cuentaId && a.Activa)
                .OrderByDescending(a => a.FechaApertura).FirstOrDefault();
            var d = ap != null ? ap.FechaApertura : fecha.Date;
            var h = DateTime.Now > fecha.Date.AddDays(1) ? DateTime.Now.AddSeconds(1) : fecha.Date.AddDays(1);
            var inf = Generar(db, d, h, filtro);
            var calc = CierreHelper.Calcular(db, cuentaId, fecha);
            if (calc.Error == null)
                inf.Efectivo = new EfectivoCaja { SaldoInicial = calc.SaldoInicial, Ingresos = calc.TotalIngresos, Egresos = calc.TotalEgresos, Esperado = calc.EfectivoEsperado };
            return inf;
        }
    }
}
