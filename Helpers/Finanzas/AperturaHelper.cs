using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Maneja la apertura y consulta de cajas.
    /// </summary>
    public static class AperturaHelper
    {
        // Serializa las aperturas dentro del servidor: dos clics simultáneos no abren dos cajas
        private static readonly object _candadoApertura = new();

        /// <summary>
        /// Devuelve la apertura activa de una cuenta, o null si no hay.
        /// </summary>
        public static AperturaCaja? ObtenerAperturaActiva(ApplicationDbContext context, int cuentaId)
        {
            return context.AperturasCaja
                .FirstOrDefault(a => a.CuentaId == cuentaId && a.Activa);
        }

        /// <summary>
        /// Verifica si hay una apertura activa para una cuenta.
        /// </summary>
        public static bool HayAperturaActiva(ApplicationDbContext context, int cuentaId)
        {
            return context.AperturasCaja.Any(a => a.CuentaId == cuentaId && a.Activa);
        }

        /// <summary>
        /// Registra una nueva apertura de caja.
        /// </summary>
        public static (AperturaCaja? apertura, string? error) Abrir(
            ApplicationDbContext context,
            int cuentaId,
            decimal saldoInicial,
            string? notas,
            int usuarioId,
            int? cuentaOrigenFondoId = null)
        {
            try
            {
                if (saldoInicial < 0)
                    return (null, "El saldo inicial no puede ser negativo.");
                saldoInicial = Math.Round(saldoInicial, 2);

                var cuenta = context.CuentasFinancieras.AsNoTracking().FirstOrDefault(c => c.Id == cuentaId);
                if (cuenta == null) return (null, "La cuenta seleccionada no existe.");
                if (!cuenta.Activa) return (null, $"La cuenta '{cuenta.Nombre}' está inactiva.");
                if (cuenta.Tipo != "Caja" || cuenta.Subtipo == "CuentasPorCobrar")
                    return (null, "Solo se puede abrir una caja de efectivo (no un banco ni una cuenta por cobrar).");

                Monitor.Enter(_candadoApertura);

                // El POS vende contra UNA caja abierta: con dos aperturas activas a la vez no hay forma de saber
                // a cuál va el efectivo de cada venta, así que solo se permite una.
                var existente = context.AperturasCaja.FirstOrDefault(a => a.Activa);

                if (existente != null)
                {
                    return (null, existente.CuentaId == cuentaId
                        ? $"Ya existe una apertura activa para esta cuenta ({existente.Numero}). Debes cerrarla antes de abrir una nueva."
                        : $"Ya hay otra caja abierta ({existente.Numero}). Ciérrala antes de abrir una nueva: el POS trabaja con una sola caja a la vez.");
                }

                // El saldo de la cuenta tiene que tener su movimiento antes de compararlo con lo contado
                AperturaCuentaHelper.RegistrarAperturaCuenta(context, cuentaId, usuarioId);
                var saldoSistema = Math.Round(context.CuentasFinancieras.AsNoTracking().Where(c => c.Id == cuentaId).Select(c => c.SaldoActual).First(), 2);
                var diferencia = saldoInicial - saldoSistema;
                if (cuentaOrigenFondoId.HasValue && diferencia > 0)
                {
                    var origen = context.CuentasFinancieras.AsNoTracking().FirstOrDefault(c => c.Id == cuentaOrigenFondoId.Value);
                    if (origen == null || !origen.Activa || origen.Id == cuentaId || origen.Subtipo == "CuentasPorCobrar")
                        return (null, "Elige una cuenta activa (otra caja o un banco) de donde sale el fondo.");
                }

                var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "AperturaCaja");

                var apertura = new AperturaCaja
                {
                    Numero = numero,
                    CuentaId = cuentaId,
                    Fecha = DateTime.Today,
                    FechaApertura = DateTime.Now,
                    UsuarioAbreId = usuarioId,
                    SaldoInicial = saldoInicial,
                    Notas = notas,
                    Activa = true,
                    EmpresaId = EmpresaActual.Id
                };

                context.AperturasCaja.Add(apertura);
                context.SaveChanges();

                // La caja abre con lo que de verdad tiene: si lo contado no es lo que dice el sistema
                // (lo que dejó el último cierre), la diferencia se registra ANTES de la apertura, para que
                // el saldo de la caja y el esperado del próximo cierre sean el mismo número.
                if (Math.Abs(diferencia) >= 0.01m)
                    RegistrarDiferencia(context, cuentaId, apertura, saldoSistema, diferencia, cuentaOrigenFondoId, usuarioId);

                return (apertura, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AperturaHelper] Error: {ex.Message}");
                return (null, ex.Message);
            }
            finally
            {
                if (Monitor.IsEntered(_candadoApertura)) Monitor.Exit(_candadoApertura);
            }
        }

        /// <summary>
        /// Diferencia entre lo contado al abrir y lo que dice el sistema:
        /// fondo traído de otra cuenta (transferencia) o, si no se indica de dónde, sobrante/faltante de caja.
        /// Se fecha un segundo antes de la apertura: entra al saldo de la caja pero no al arqueo del turno.
        /// </summary>
        private static void RegistrarDiferencia(ApplicationDbContext context, int cuentaId, AperturaCaja apertura,
            decimal saldoSistema, decimal diferencia, int? cuentaOrigenFondoId, int usuarioId)
        {
            var moneda = context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;
            var mov = new MovimientoFinanciero
            {
                Numero = NumeroDocumentoHelper.GenerarSiguiente(context, "MovimientoFinanciero"),
                Fecha = apertura.FechaApertura.AddSeconds(-1),
                Monto = Math.Abs(diferencia),
                MonedaId = moneda,
                TipoCambio = 1,
                FormaPago = "Efectivo",
                Referencia = apertura.Numero,
                Origen = "AperturaCaja",
                OrigenId = apertura.Id,
                EsAutomatico = true,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = usuarioId,
                EmpresaId = EmpresaActual.Id
            };
            if (diferencia > 0 && cuentaOrigenFondoId.HasValue)
            {
                mov.Tipo = "Transferencia";
                mov.CuentaId = cuentaOrigenFondoId.Value;
                mov.CuentaDestinoId = cuentaId;
                mov.Concepto = $"Fondo para la apertura {apertura.Numero}";
            }
            else
            {
                var sobrante = diferencia > 0;
                var categoria = sobrante
                    ? MovimientoManualHelper.ObtenerOCrearCategoria(context, "Ingreso", "Sobrante de caja", "4.2.02", "#f59e0b")
                    : MovimientoManualHelper.ObtenerOCrearCategoria(context, "Egreso", "Faltante de caja", "6.3.02", "#f59e0b");
                mov.Tipo = sobrante ? "Ingreso" : "Egreso";
                mov.CuentaId = cuentaId;
                mov.CategoriaId = categoria.Id;
                mov.Concepto = $"{(sobrante ? "Sobrante" : "Faltante")} al abrir {apertura.Numero}: el sistema tenía L. {saldoSistema:N2} y se contaron L. {apertura.SaldoInicial:N2}";
            }
            context.MovimientosFinancieros.Add(mov);
            context.SaveChanges();
            SaldoHelper.Aplicar(context, mov);
            context.SaveChanges();
        }

        /// <summary>
        /// Cierra una apertura (se llama desde el cierre).
        /// </summary>
        public static void Cerrar(ApplicationDbContext context, int aperturaId, int cierreId)
        {
            var apertura = context.AperturasCaja.FirstOrDefault(a => a.Id == aperturaId);
            if (apertura != null)
            {
                apertura.Activa = false;
                apertura.CierreId = cierreId;
                context.SaveChanges();
            }
        }
    }
}