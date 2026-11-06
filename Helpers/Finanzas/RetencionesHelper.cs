using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Retenciones y aportes de la nómina: lo que se le descuenta al empleado (ISR, IHSS, RAP) y lo que aporta el
    /// patrono (IHSS, RAP, INFOP) no es de la empresa: es una deuda con cada entidad hasta que se le paga.
    ///
    /// Al pagarle a un empleado el dinero que sale es el neto. Lo demás del gasto entra con movimientos SOLO
    /// CONTABLES (no tocan caja ni banco), uno por concepto, enlazados al pago por OrigenId:
    ///   Debe 6.2.01 Sueldos (bruto) / 6.2.02 Aportes patronales
    ///   Haber Banco (neto), 2.1.02.00x ISR, IHSS, RAP e INFOP por pagar, 1.1.02.003 Anticipos (vales descontados)
    /// Al pagarle a la entidad sale dinero real y baja la deuda: Debe 2.1.02.00x / Haber caja o banco.
    /// El pendiente siempre se calcula de esos movimientos activos, así un pago anulado vuelve a quedar pendiente.
    /// </summary>
    public static class RetencionesHelper
    {
        public const string OrigenNomina = "NominaRetencion";
        public const string OrigenPago = "PagoRetencion";
        public const string MarcaPatronal = "[PATRONAL]";
        public const string ClaveVale = "VALE";
        public const string CodigoAnticipos = "1.1.02.003";

        public record Concepto(string Clave, string Nombre, string Entidad, string CodigoPlan, string CategoriaPago);

        public static readonly List<Concepto> Conceptos = new()
        {
            new("ISR", "ISR", "SAR", "2.1.02.002", "Pago de ISR retenido"),
            new("IHSS", "IHSS", "Instituto Hondureño de Seguridad Social", "2.1.02.003", "Pago de IHSS"),
            new("RAP", "RAP", "Régimen de Aportaciones Privadas", "2.1.02.004", "Pago de RAP"),
            new("INFOP", "INFOP", "Instituto Nacional de Formación Profesional", "2.1.02.005", "Pago de INFOP"),
        };

        public static string Marca(string clave) => $"[RET:{clave}]";

        public static string? ClaveDe(string? notas)
        {
            if (string.IsNullOrEmpty(notas)) return null;
            var i = notas.IndexOf("[RET:", StringComparison.Ordinal);
            if (i < 0) return null;
            var fin = notas.IndexOf(']', i);
            return fin < 0 ? null : notas.Substring(i + 5, fin - i - 5);
        }

        /// <summary>Cuenta del plan contra la que va un movimiento de nómina (la contrapartida del gasto).</summary>
        public static string? CodigoDe(string? notas)
        {
            var clave = ClaveDe(notas);
            if (clave == ClaveVale) return CodigoAnticipos;
            return Conceptos.FirstOrDefault(c => c.Clave == clave)?.CodigoPlan;
        }

        // ============================================================
        // AL PAGAR LA NÓMINA
        // ============================================================

        /// <summary>
        /// Registra la parte del gasto que no sale en dinero al pagarle a un empleado. Idempotente por pago.
        /// Debe llamarse dentro de la transacción del pago.
        /// </summary>
        public static string? RegistrarDeNomina(ApplicationDbContext context, PagoNominaEmpleado pago, DetalleNomina d,
            string numeroNomina, string empleado, int? usuarioId, bool validarPeriodo = true)
        {
            if (context.MovimientosFinancieros.Any(m => m.Origen == OrigenNomina && m.OrigenId == pago.Id)) return null;

            var cuentaId = pago.CuentaId ?? context.CuentasFinancieras.Where(c => c.Activa).OrderBy(c => c.Id).Select(c => c.Id).FirstOrDefault();
            if (cuentaId == 0) return "No hay una cuenta activa para registrar las retenciones.";

            var sueldos = MovimientoManualHelper.ObtenerOCrearCategoria(context, "Egreso", "Sueldos: retenciones y vales", "6.2.01", "#dc2626");
            var aportes = MovimientoManualHelper.ObtenerOCrearCategoria(context, "Egreso", "Aportes patronales", "6.2.02", "#b91c1c");

            var lineas = new (string Clave, decimal Monto, bool Patronal, string Texto)[]
            {
                ("ISR", d.DeduccionISR, false, "ISR retenido"),
                ("IHSS", d.DeduccionIHSS, false, "IHSS retenido"),
                ("RAP", d.DeduccionRAP, false, "RAP retenido"),
                (ClaveVale, d.DeduccionVales, false, "Vales descontados"),
                ("IHSS", d.AporteIHSSPatronal, true, "Aporte patronal IHSS"),
                ("RAP", d.AporteRAPPatronal, true, "Aporte patronal RAP"),
                ("INFOP", d.AporteINFOP, true, "Aporte patronal INFOP"),
            };

            foreach (var l in lineas.Where(x => x.Monto > 0))
            {
                var (mov, error) = MovimientoManualHelper.Crear(context, "Egreso", pago.FechaPago, cuentaId, null,
                    l.Patronal ? aportes.Id : sueldos.Id, l.Monto, $"{l.Texto} — Nómina {numeroNomina}, {empleado}",
                    numeroNomina, OrigenNomina, pago.Id, usuarioId, soloContable: true,
                    notas: Marca(l.Clave) + (l.Patronal ? MarcaPatronal : ""), validarPeriodo: validarPeriodo);
                if (mov == null) return error;
            }
            return null;
        }

        /// <summary>
        /// Pagos de nómina hechos antes de existir este registro: se les crea su parte contable, para que el gasto
        /// y la deuda queden completos. Los aportes patronales de esas nóminas no se calcularon, por eso no entran.
        /// </summary>
        public static int Reparar(ApplicationDbContext context)
        {
            var hechos = context.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Origen == OrigenNomina && m.OrigenId != null).Select(m => m.OrigenId!.Value).Distinct().ToHashSet();
            var pagos = context.PagosNominaEmpleado.Where(p => p.Estado == "Pagado").ToList().Where(p => !hechos.Contains(p.Id)).ToList();
            var n = 0;
            foreach (var p in pagos)
            {
                var d = context.DetalleNominas.FirstOrDefault(x => x.Id == p.DetalleNominaId);
                if (d == null) continue;
                var nomina = context.Nominas.AsNoTracking().Where(x => x.Id == p.NominaId).Select(x => x.Numero).FirstOrDefault() ?? "";
                var emp = context.Empleados.AsNoTracking().FirstOrDefault(e => e.Id == p.EmpleadoId);
                if (RegistrarDeNomina(context, p, d, nomina, $"{emp?.Nombres} {emp?.Apellidos}", null, validarPeriodo: false) == null) n++;
            }
            return n;
        }

        // ============================================================
        // CUÁNTO SE DEBE
        // ============================================================

        public class FilaResumen
        {
            public Concepto Concepto { get; set; } = null!;
            public decimal Retenido { get; set; }
            public decimal AportePatronal { get; set; }
            public decimal Pagado { get; set; }
            public decimal Pendiente => Retenido + AportePatronal - Pagado;
        }

        /// <summary>Lo generado por las nóminas hasta una fecha, por clave (incluye VALE: anticipos ya descontados).</summary>
        public static Dictionary<string, decimal> Generado(ApplicationDbContext context, DateTime hastaExcl)
        {
            return context.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Origen == OrigenNomina && m.Estado == "Activo" && m.Fecha < hastaExcl)
                .Select(m => new { m.Notas, m.Monto }).ToList()
                .GroupBy(m => ClaveDe(m.Notas) ?? "")
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));
        }

        public static List<FilaResumen> Resumen(ApplicationDbContext context)
        {
            var movs = context.MovimientosFinancieros.AsNoTracking()
                .Where(m => (m.Origen == OrigenNomina || m.Origen == OrigenPago) && m.Estado == "Activo")
                .Select(m => new { m.Origen, m.Notas, m.Monto }).ToList();

            return Conceptos.Select(c =>
            {
                var propios = movs.Where(m => ClaveDe(m.Notas) == c.Clave).ToList();
                return new FilaResumen
                {
                    Concepto = c,
                    Retenido = propios.Where(m => m.Origen == OrigenNomina && !m.Notas!.Contains(MarcaPatronal)).Sum(m => m.Monto),
                    AportePatronal = propios.Where(m => m.Origen == OrigenNomina && m.Notas!.Contains(MarcaPatronal)).Sum(m => m.Monto),
                    Pagado = propios.Where(m => m.Origen == OrigenPago).Sum(m => m.Monto)
                };
            }).ToList();
        }

        public record FilaMes(int Anio, int Mes, Dictionary<string, decimal> Montos);

        /// <summary>Lo generado por mes y por concepto: es lo que se declara a cada entidad.</summary>
        public static List<FilaMes> PorMes(ApplicationDbContext context, int meses = 12)
        {
            var desde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(meses - 1));
            var movs = context.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Origen == OrigenNomina && m.Estado == "Activo" && m.Fecha >= desde)
                .Select(m => new { m.Fecha, m.Notas, m.Monto }).ToList()
                .Where(m => ClaveDe(m.Notas) != ClaveVale).ToList();
            return movs.GroupBy(m => new { m.Fecha.Year, m.Fecha.Month })
                .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
                .Select(g => new FilaMes(g.Key.Year, g.Key.Month,
                    g.GroupBy(x => ClaveDe(x.Notas) ?? "").ToDictionary(x => x.Key, x => x.Sum(y => y.Monto))))
                .ToList();
        }

        // ============================================================
        // PAGO A LA ENTIDAD
        // ============================================================

        /// <summary>Paga a una entidad. Es dinero real: baja la caja o el banco y baja la deuda, no toca la utilidad.</summary>
        public static (MovimientoFinanciero? mov, string? error) Pagar(ApplicationDbContext context, string clave, decimal monto,
            int cuentaId, DateTime fecha, string? referencia, int? usuarioId)
        {
            var concepto = Conceptos.FirstOrDefault(c => c.Clave == clave);
            if (concepto == null) return (null, "Concepto no válido.");
            monto = Math.Round(monto, 2, MidpointRounding.AwayFromZero);
            if (monto <= 0) return (null, "El monto debe ser mayor a 0.");
            if (fecha.Date > DateTime.Today) return (null, "La fecha del pago no puede ser futura.");
            if (string.IsNullOrWhiteSpace(referencia)) return (null, "Escribe el número de recibo o de la transacción.");

            var pendiente = Resumen(context).First(f => f.Concepto.Clave == clave).Pendiente;
            if (monto > pendiente + 0.005m)
                return (null, $"Solo hay L. {pendiente:N2} pendientes de {concepto.Nombre}.");

            var cuenta = context.CuentasFinancieras.FirstOrDefault(c => c.Id == cuentaId && c.Activa);
            if (cuenta == null) return (null, "Elige una cuenta activa.");
            if (cuenta.SaldoActual < monto)
                return (null, $"Saldo insuficiente en '{cuenta.Nombre}'. Disponible: L. {cuenta.SaldoActual:N2}.");

            var categoria = MovimientoManualHelper.ObtenerOCrearCategoria(context, "Egreso", concepto.CategoriaPago, concepto.CodigoPlan, "#7c3aed");
            var (mov, error) = MovimientoManualHelper.Crear(context, "Egreso", fecha, cuentaId, null, categoria.Id, monto,
                $"Pago de {concepto.Nombre} a {concepto.Entidad}", referencia.Trim(), OrigenPago, null, usuarioId, notas: Marca(clave));
            return (mov, error);
        }

        public static List<MovimientoFinanciero> Pagos(ApplicationDbContext context, int cantidad = 30) =>
            context.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Origen == OrigenPago)
                .OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id).Take(cantidad).ToList();
    }
}
