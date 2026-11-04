using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Efectivo que sale de la caja al cerrar pero todavía no llega al banco (el cierre es a las 6 pm y el banco
    /// ya cerró). Vive en una cuenta puente «Efectivo por depositar»: la caja ya no lo tiene (la próxima apertura
    /// no lo cuenta) y el banco todavía no (la conciliación no se descuadra). Cuando se deposita, se registra la
    /// boleta y pasa al banco.
    /// </summary>
    public static class EfectivoPorDepositarHelper
    {
        public const string Tipo = "PorDepositar";            // tipo de distribución del cierre
        public const string TipoCuenta = "Transito";          // no es caja (no se abre ni se vende con ella) ni banco
        public const string CodigoCuenta = "POR-DEPOSITAR";
        public const string CodigoContable = "1.1.01.005";
        public const string OrigenDeposito = "DepositoCierre";

        /// <summary>La cuenta puente (se crea la primera vez que se usa).</summary>
        public static CuentaFinanciera Cuenta(ApplicationDbContext ctx)
        {
            var c = ctx.CuentasFinancieras.FirstOrDefault(x => x.Codigo == CodigoCuenta);
            if (c != null)
            {
                if (!c.Activa) { c.Activa = true; ctx.SaveChanges(); }
                return c;
            }
            // su cuenta del catálogo contable, dentro de «Caja y Bancos»
            if (!ctx.PlanCuentas.Any(p => p.Codigo == CodigoContable) && ctx.PlanCuentas.Any(p => p.Codigo == "1.1.01"))
            {
                ctx.PlanCuentas.Add(new PlanCuenta
                {
                    Codigo = CodigoContable, Nombre = "Efectivo por depositar", Tipo = "Activo", Naturaleza = "Deudora",
                    Nivel = 4, CodigoPadre = "1.1.01", EsMovimiento = true, Activa = true
                });
                ctx.SaveChanges();
            }
            c = new CuentaFinanciera
            {
                Codigo = CodigoCuenta, Nombre = "Efectivo por depositar", Tipo = TipoCuenta, Subtipo = "PorDepositar",
                CuentaContable = CodigoContable, Activa = true, SaldoActual = 0, SaldoInicial = 0,
                MonedaId = ctx.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1,
                Descripcion = "Efectivo de los cierres de caja que todavía no se ha depositado en el banco.",
                FechaCreacion = DateTime.Now, EmpresaId = EmpresaActual.Id
            };
            ctx.CuentasFinancieras.Add(c);
            ctx.SaveChanges();
            return c;
        }

        public record Pendiente(int DistribucionId, int CierreId, string CierreNumero, string Caja, DateTime Fecha, decimal Monto);

        /// <summary>Lo que salió de una caja «para depositar» y todavía no tiene su depósito registrado.</summary>
        public static List<Pendiente> Pendientes(ApplicationDbContext ctx, int? cierreId = null)
        {
            var depositadas = ctx.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Origen == OrigenDeposito && m.Estado == "Activo" && m.OrigenId != null)
                .Select(m => m.OrigenId!.Value).ToHashSet();
            var q = ctx.DistribucionesCierre.AsNoTracking().Where(d => d.Tipo == Tipo && d.MovimientoId != null);
            if (cierreId.HasValue) q = q.Where(d => d.CierreCajaId == cierreId.Value);
            var dist = q.ToList().Where(d => !depositadas.Contains(d.Id)).ToList();
            if (dist.Count == 0) return new();
            var ids = dist.Select(d => d.CierreCajaId).Distinct().ToList();
            var cierres = ctx.CierresCaja.AsNoTracking().Where(c => ids.Contains(c.Id)).ToDictionary(c => c.Id);
            var cuentas = ctx.CuentasFinancieras.AsNoTracking().ToDictionary(c => c.Id, c => c.Nombre);
            return dist.Where(d => cierres.ContainsKey(d.CierreCajaId)).Select(d =>
            {
                var c = cierres[d.CierreCajaId];
                return new Pendiente(d.Id, c.Id, c.Numero, cuentas.GetValueOrDefault(c.CuentaId) ?? "Caja", c.FechaCierre, d.Monto);
            }).OrderBy(p => p.Fecha).ToList();
        }

        /// <summary>
        /// El efectivo llegó al banco: pasa de «Efectivo por depositar» al banco con la fecha y la boleta del depósito.
        /// </summary>
        public static (MovimientoFinanciero? mov, string? error) RegistrarDeposito(ApplicationDbContext ctx, int distribucionId,
            int bancoId, DateTime fecha, string? referencia, int usuarioId)
        {
            var pend = Pendientes(ctx).FirstOrDefault(p => p.DistribucionId == distribucionId);
            if (pend == null) return (null, "Ese efectivo ya tiene su depósito registrado (o no existe).");
            var banco = ctx.CuentasFinancieras.FirstOrDefault(c => c.Id == bancoId);
            if (banco == null || !banco.Activa || banco.Tipo != "Banco") return (null, "Elige una cuenta de banco activa.");
            if (string.IsNullOrWhiteSpace(referencia)) return (null, "Escribe el número de la boleta o de la transferencia del depósito.");
            if (fecha.Date > DateTime.Today) return (null, "La fecha del depósito no puede ser futura.");
            if (fecha.Date < pend.Fecha.Date) return (null, "El depósito no puede ser antes del cierre de caja.");
            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(ctx, fecha);
            if (!periodoOk) return (null, periodoError);

            var transito = Cuenta(ctx);
            if (transito.SaldoActual < pend.Monto - 0.005m)
                return (null, $"En «Efectivo por depositar» solo hay L. {transito.SaldoActual:N2}.");

            var mov = new MovimientoFinanciero
            {
                Numero = NumeroDocumentoHelper.GenerarSiguiente(ctx, "MovimientoFinanciero"),
                Tipo = "Transferencia",
                Fecha = fecha.Date == DateTime.Today ? DateTime.Now : fecha.Date.AddHours(12),
                CuentaId = transito.Id,
                CuentaDestinoId = banco.Id,
                Monto = pend.Monto,
                MonedaId = ctx.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1,
                TipoCambio = 1,
                Concepto = $"Depósito del cierre {pend.CierreNumero} ({pend.Caja})",
                Referencia = referencia.Trim().Length > 100 ? referencia.Trim()[..100] : referencia.Trim(),
                FormaPago = "Depósito",
                Origen = OrigenDeposito,
                OrigenId = distribucionId,
                EsAutomatico = true,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = usuarioId,
                EmpresaId = EmpresaActual.Id
            };
            ctx.MovimientosFinancieros.Add(mov);
            ctx.SaveChanges();
            SaldoHelper.Aplicar(ctx, mov);

            // la distribución del cierre queda con la boleta y el banco
            var d = ctx.DistribucionesCierre.First(x => x.Id == distribucionId);
            d.Referencia = mov.Referencia;
            d.CuentaDestinoId = banco.Id;
            ctx.SaveChanges();
            return (mov, null);
        }
    }
}
