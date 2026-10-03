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
        /// <summary>
        /// Devuelve la apertura activa de una cuenta, o null si no hay.
        /// </summary>
        public static AperturaCaja? ObtenerAperturaActiva(ApplicationDbContext context, int cuentaId)
        {
            return context.AperturasCaja
                .FirstOrDefault(a => a.CuentaId == cuentaId && a.Activa);
        }

        /// <summary>
        /// Verifica si el usuario tiene permiso para usar el POS (necesita apertura activa).
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
                // Verificar que no haya otra apertura activa para esa cuenta
                var existente = context.AperturasCaja
                    .FirstOrDefault(a => a.CuentaId == cuentaId && a.Activa);

                if (existente != null)
                {
                    return (null, $"Ya existe una apertura activa para esta cuenta ({existente.Numero}). Debes cerrarla antes de abrir una nueva.");
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
                    EmpresaId = 1
                };

                context.AperturasCaja.Add(apertura);
                context.SaveChanges();

                return (apertura, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AperturaHelper] Error: {ex.Message}");
                return (null, ex.Message);
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