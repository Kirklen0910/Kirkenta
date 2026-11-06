using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    public static class CierreHelper
    {
        public static CalculoCierre Calcular(
            ApplicationDbContext context,
            int cuentaId,
            DateTime fecha)
        {
            var inicioDia = fecha.Date;
            var finDia = inicioDia.AddDays(1);

            var cuenta = context.CuentasFinancieras
                .AsNoTracking()
                .FirstOrDefault(c => c.Id == cuentaId);

            if (cuenta == null)
            {
                return new CalculoCierre { Error = "Cuenta no encontrada" };
            }

            var apertura = context.AperturasCaja
                .AsNoTracking()
                .Where(a => a.CuentaId == cuentaId && a.Activa)
                .OrderByDescending(a => a.FechaApertura)
                .FirstOrDefault();

            if (apertura == null)
            {
                // Solo se cierra una apertura ACTIVA: así una misma apertura no puede cerrarse dos veces.
                return new CalculoCierre
                {
                    Error = "No hay una apertura de caja activa para esta cuenta. Debes abrir la caja antes de cerrarla.",
                    RequiereApertura = true
                };
            }

            decimal saldoInicial = apertura.SaldoInicial;

            // 🔒 FIX: Solo se cuentan movimientos en EFECTIVO para el esperado del arqueo
            // (la regla está en FormaPagoHelper.EsEfectivo). Tarjeta y transferencias de clientes no son billetes,
            // pero una transferencia entre cuentas sí saca o mete dinero físico de la caja.
            // El período del arqueo va desde que se abrió la caja (no desde medianoche): si la caja
            // se abrió ayer y se cierra hoy, o se abrió dos veces el mismo día, el esperado sigue cuadrando.
            var desde = apertura.FechaApertura;
            var hasta = DateTime.Now > finDia ? DateTime.Now : finDia;
            var movimientosDia = context.MovimientosFinancieros
                .AsNoTracking()
                .Where(m => m.Estado == "Activo"
                         && m.Origen != "Apertura"
                         && m.Fecha >= desde
                         && m.Fecha <= hasta)
                .Where(m => (m.CuentaId == cuentaId || m.CuentaDestinoId == cuentaId))
                .ToList()
                .Where(FormaPagoHelper.EsEfectivo)
                .ToList();

            decimal ingresos = 0;
            decimal egresos = 0;

            foreach (var m in movimientosDia)
            {
                if (m.CuentaId == cuentaId)
                {
                    if (m.Tipo == "Ingreso") ingresos += m.Monto;
                    else if (m.Tipo == "Egreso") egresos += m.Monto;
                    else if (m.Tipo == "Transferencia") egresos += m.Monto;
                }
                if (m.CuentaDestinoId == cuentaId && m.Tipo == "Transferencia")
                {
                    ingresos += m.Monto;
                }
            }

            var esperado = saldoInicial + ingresos - egresos;

            return new CalculoCierre
            {
                AperturaId = apertura.Id,
                AperturaNumero = apertura.Numero,
                AperturaFechaHora = apertura.FechaApertura,
                SaldoInicial = saldoInicial,
                TotalIngresos = ingresos,
                TotalEgresos = egresos,
                EfectivoEsperado = esperado
            };
        }

        public static string DeterminarResultado(decimal diferencia, decimal tolerancia)
        {
            if (Math.Abs(diferencia) <= tolerancia)
                return "Cuadrado";
            return diferencia > 0 ? "Sobrante" : "Faltante";
        }

        /// <summary>
        /// Registra el cierre de caja. Cierre + cierre de la apertura + ajuste por sobrante/faltante
        /// se guardan juntos en una transacción: si algo falla no queda un cierre a medias.
        /// </summary>
        public static (CierreCaja cierre, string? error) Registrar(
            ApplicationDbContext context,
            int cuentaId,
            DateTime fecha,
            decimal efectivoContado,
            string? notas,
            decimal tolerancia,
            int usuarioId)
        {
            var abrimosTx = context.Database.CurrentTransaction == null;
            var tx = abrimosTx ? context.Database.BeginTransaction() : null;
            try
            {
                var resultado = RegistrarInterno(context, cuentaId, fecha, efectivoContado, notas, tolerancia, usuarioId);
                if (resultado.error == null) tx?.Commit();
                else tx?.Rollback();
                return resultado;
            }
            catch
            {
                tx?.Rollback();
                throw;
            }
            finally
            {
                tx?.Dispose();
            }
        }

        private static (CierreCaja cierre, string? error) RegistrarInterno(
            ApplicationDbContext context,
            int cuentaId,
            DateTime fecha,
            decimal efectivoContado,
            string? notas,
            decimal tolerancia,
            int usuarioId)
        {
            try
            {
                var calculo = Calcular(context, cuentaId, fecha);
                if (calculo.Error != null)
                    return (new CierreCaja(), calculo.Error);

                var diferencia = efectivoContado - calculo.EfectivoEsperado;
                var resultado = DeterminarResultado(diferencia, tolerancia);

                var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "CierreCaja");

                var cierre = new CierreCaja
                {
                    Numero = numero,
                    CuentaId = cuentaId,
                    AperturaId = calculo.AperturaId,
                    Fecha = fecha.Date,
                    FechaCierre = DateTime.Now,
                    UsuarioCierraId = usuarioId,
                    SaldoInicialSistema = calculo.SaldoInicial,
                    TotalIngresosEfectivo = calculo.TotalIngresos,
                    TotalEgresosEfectivo = calculo.TotalEgresos,
                    EfectivoEsperado = calculo.EfectivoEsperado,
                    EfectivoContado = efectivoContado,
                    Diferencia = diferencia,
                    Resultado = resultado,
                    Tolerancia = tolerancia,
                    Notas = notas,
                    EstadoActa = "Cerrado",
                    EmpresaId = EmpresaActual.Id
                };

                context.CierresCaja.Add(cierre);
                context.SaveChanges();

                if (calculo.AperturaId.HasValue)
                {
                    AperturaHelper.Cerrar(context, calculo.AperturaId.Value, cierre.Id);
                }

                // La tolerancia define si el cierre se ve «Cuadrado», pero todo centavo de diferencia se registra:
                // si no, el saldo del sistema y el dinero contado quedaban distintos y la diferencia reaparecía
                // en la siguiente apertura como un faltante o sobrante sin explicación.
                if (Math.Abs(diferencia) >= 0.01m)
                {
                    var tipoAjuste = diferencia > 0 ? "Ingreso" : "Egreso";
                    GenerarAjuste(context, cuentaId, tipoAjuste, Math.Abs(diferencia), numero, cierre.Id, usuarioId);
                }

                TrasladarNoEfectivo(context, cuentaId, calculo.AperturaFechaHora, cierre, usuarioId);

                return (cierre, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CierreHelper] Error: {ex.Message}");
                return (new CierreCaja(), ex.Message);
            }
        }

        /// <summary>
        /// Lo cobrado en el turno con tarjeta o transferencia entra a la cuenta de la caja, pero no son billetes:
        /// ese dinero está en el banco. Al cerrar se traslada, para que el saldo de la caja sea solo su efectivo.
        /// Antes se quedaba ahí y la siguiente apertura lo registraba como «Faltante de caja» (un gasto falso).
        /// Sin cuenta de banco va a la cuenta puente «por depositar». Si el neto es negativo (reembolsos por
        /// tarjeta mayores a lo cobrado) el traslado va al revés.
        /// </summary>
        private static void TrasladarNoEfectivo(ApplicationDbContext context, int cuentaId, DateTime? desde, CierreCaja cierre, int usuarioId)
        {
            if (desde == null) return;
            var cuenta = context.CuentasFinancieras.FirstOrDefault(c => c.Id == cuentaId);
            if (cuenta == null || cuenta.Tipo != "Caja") return;

            var inicio = desde.Value;
            var noEfectivo = context.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Estado == "Activo" && m.CuentaId == cuentaId && m.Origen != "Apertura"
                         && m.Fecha >= inicio && (m.Tipo == "Ingreso" || m.Tipo == "Egreso"))
                .ToList()
                .Where(m => !FormaPagoHelper.EsEfectivo(m) && !SaldoHelper.EsSoloContable(context, m))
                .ToList();
            var neto = noEfectivo.Sum(m => m.Tipo == "Ingreso" ? m.Monto : -m.Monto);
            if (Math.Abs(neto) < 0.01m) return;

            var destinoId = context.CuentasFinancieras.Where(c => c.Tipo == "Banco" && c.Activa)
                .OrderBy(c => c.Id).Select(c => (int?)c.Id).FirstOrDefault()
                ?? EfectivoPorDepositarHelper.Cuenta(context).Id;

            var mov = new MovimientoFinanciero
            {
                Numero = NumeroDocumentoHelper.GenerarSiguiente(context, "MovimientoFinanciero"),
                Tipo = "Transferencia",
                Fecha = DateTime.Now,
                CuentaId = neto > 0 ? cuentaId : destinoId,
                CuentaDestinoId = neto > 0 ? destinoId : cuentaId,
                Monto = Math.Abs(neto),
                MonedaId = context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1,
                TipoCambio = 1,
                Concepto = $"Tarjetas y transferencias del turno {cierre.Numero}",
                Referencia = cierre.Numero,
                FormaPago = "Transferencia",
                Origen = "CierreCaja",
                OrigenId = cierre.Id,
                EsAutomatico = true,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = usuarioId,
                EmpresaId = EmpresaActual.Id
            };
            context.MovimientosFinancieros.Add(mov);
            context.SaveChanges();
            SaldoHelper.Aplicar(context, mov);
            context.SaveChanges();
        }

        private static void GenerarAjuste(
            ApplicationDbContext context,
            int cuentaId,
            string tipoAjuste,
            decimal montoAjuste,
            string numeroCierre,
            int cierreId,
            int usuarioId)
        {
            // El sobrante es un ingreso no operativo (4.2.02) y el faltante un gasto diverso (6.3.02):
            // así salen clasificados en el Estado de Resultados y no quedan "sin clasificar".
            var categoria = tipoAjuste == "Ingreso"
                ? MovimientoManualHelper.ObtenerOCrearCategoria(context, "Ingreso", "Sobrante de caja", "4.2.02", "#f59e0b")
                : MovimientoManualHelper.ObtenerOCrearCategoria(context, "Egreso", "Faltante de caja", "6.3.02", "#f59e0b");

            var numeroAjuste = NumeroDocumentoHelper.GenerarSiguiente(context, "MovimientoFinanciero");
            var monedaDefecto = context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;

            // 🔒 FIX: usar la forma de pago real de la cuenta (efectivo si es caja, transferencia si es banco)
            var cuenta = context.CuentasFinancieras.FirstOrDefault(c => c.Id == cuentaId);
            var formaPago = cuenta?.Tipo == "Banco" ? "Transferencia" : "Efectivo";

            var ajuste = new MovimientoFinanciero
            {
                Numero = numeroAjuste,
                Tipo = tipoAjuste,
                Fecha = DateTime.Now,
                CuentaId = cuentaId,
                CategoriaId = categoria.Id,
                Monto = montoAjuste,
                MonedaId = monedaDefecto,
                TipoCambio = 1,
                Concepto = $"Ajuste por {tipoAjuste.ToLower()} en cierre {numeroCierre}",
                Referencia = numeroCierre,
                FormaPago = formaPago,
                Origen = "CierreCaja",
                OrigenId = cierreId,
                EsAutomatico = true,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = usuarioId,
                EmpresaId = EmpresaActual.Id
            };

            context.MovimientosFinancieros.Add(ajuste);
            context.SaveChanges();

            SaldoHelper.Aplicar(context, ajuste);
            context.SaveChanges();

            var cierre = context.CierresCaja.FirstOrDefault(c => c.Id == cierreId);
            if (cierre != null)
            {
                cierre.MovimientoAjusteId = ajuste.Id;
                context.SaveChanges();
            }
        }
    }

    public class CalculoCierre
    {
        public string? Error { get; set; }
        public bool RequiereApertura { get; set; }
        public int? AperturaId { get; set; }
        public string? AperturaNumero { get; set; }
        public DateTime? AperturaFechaHora { get; set; }
        public decimal SaldoInicial { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal EfectivoEsperado { get; set; }
    }
}