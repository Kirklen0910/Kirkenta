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
                .Where(a => a.CuentaId == cuentaId)
                .OrderByDescending(a => a.FechaApertura)
                .FirstOrDefault(a => a.Fecha == inicioDia || a.Activa);

            if (apertura == null)
            {
                return new CalculoCierre
                {
                    Error = "No hay apertura de caja para esta fecha. Debes abrir la caja antes de cerrarla.",
                    RequiereApertura = true
                };
            }

            decimal saldoInicial = apertura.SaldoInicial;

            var movimientosDia = context.MovimientosFinancieros
                .AsNoTracking()
                .Where(m => m.Estado == "Activo"
                         && m.Fecha >= inicioDia
                         && m.Fecha < finDia)
                .Where(m => (m.CuentaId == cuentaId || m.CuentaDestinoId == cuentaId))
                .Where(m => m.FormaPago != null && m.FormaPago.ToLower().Contains("efectivo"))
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
        /// Registra el cierre. Genera ajuste por diferencia si aplica.
        /// NO registra distribución todavía — eso lo hace el caller después.
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
                    EmpresaId = 1
                };

                context.CierresCaja.Add(cierre);
                context.SaveChanges();

                if (calculo.AperturaId.HasValue)
                {
                    AperturaHelper.Cerrar(context, calculo.AperturaId.Value, cierre.Id);
                }

                if (resultado != "Cuadrado")
                {
                    var tipoAjuste = resultado == "Sobrante" ? "Ingreso" : "Egreso";
                    var montoAjuste = Math.Abs(diferencia);
                    GenerarAjuste(context, cuentaId, tipoAjuste, montoAjuste, numero, cierre.Id, usuarioId);

                    // El ajuste actualiza el saldo, para que las distribuciones cuadren
                }

                return (cierre, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CierreHelper] Error: {ex.Message}");
                return (new CierreCaja(), ex.Message);
            }
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
            var categoria = context.CategoriasFinancieras
                .FirstOrDefault(c => c.Tipo == tipoAjuste && c.Nombre.Contains("Ajuste"));

            if (categoria == null)
            {
                categoria = new CategoriaFinanciera
                {
                    Tipo = tipoAjuste,
                    Nombre = "Ajuste de caja",
                    Color = "#f59e0b",
                    EsSistema = true,
                    Activa = true,
                    FechaCreacion = DateTime.Now,
                    EmpresaId = 1
                };
                context.CategoriasFinancieras.Add(categoria);
                context.SaveChanges();
            }

            var numeroAjuste = NumeroDocumentoHelper.GenerarSiguiente(context, "MovimientoFinanciero");
            var monedaDefecto = context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;

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
                FormaPago = "Efectivo",
                Origen = "CierreCaja",
                OrigenId = cierreId,
                EsAutomatico = true,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = usuarioId,
                EmpresaId = 1
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