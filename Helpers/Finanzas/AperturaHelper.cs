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
            int usuarioId)
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

                // Aseguramos que el saldo inicial de la cuenta tenga su
                // movimiento contable (para que el Balance refleje el Activo).
                // Si ya existe (porque se creó la cuenta con saldo inicial),
                // el helper es idempotente y no duplica.
                AperturaCuentaHelper.RegistrarAperturaCuenta(context, cuentaId, usuarioId);

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