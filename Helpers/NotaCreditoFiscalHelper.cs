using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Numeración fiscal de notas de crédito (devoluciones de clientes) con su propio CAI y rango.
    /// Solo actúa si la facturación fiscal está activa Y la empresa configuró el rango de notas de crédito.
    /// Si no, la devolución usa la numeración interna ("Devolucion").
    /// </summary>
    public static class NotaCreditoFiscalHelper
    {
        public class ResultadoNC
        {
            public bool Activa { get; set; }
            public bool Ok { get; set; } = true;
            public string? Error { get; set; }
            public string? Numero { get; set; }
            public string? CAI { get; set; }
            public string? RangoInicial { get; set; }
            public string? RangoFinal { get; set; }
            public DateTime? FechaLimiteEmision { get; set; }
        }

        /// <summary>True si el usuario escribió algo en cualquiera de los campos de nota de crédito.</summary>
        public static bool HayDatos(string? cai, string? ini, string? fin, DateTime? limite)
            => !string.IsNullOrWhiteSpace(cai) || !string.IsNullOrWhiteSpace(ini)
               || !string.IsNullOrWhiteSpace(fin) || limite != null;

        /// <summary>Reserva (consume) el próximo número de nota de crédito. Llamar dentro de la transacción.</summary>
        public static ResultadoNC Reservar(ApplicationDbContext context)
        {
            var r = new ResultadoNC();
            var empresa = context.ConfiguracionEmpresa.FirstOrDefault();
            if (empresa == null || !empresa.FacturacionFiscalActiva) return r;
            if (!HayDatos(empresa.NotaCreditoCAI, empresa.NotaCreditoRangoInicial,
                          empresa.NotaCreditoRangoFinal, empresa.NotaCreditoFechaLimite))
                return r; // sin rango propio → numeración interna

            // Bloquear la fila de la empresa y releerla: dos devoluciones simultáneas no repiten número
            if (context.Database.IsRelational())
            {
                var idEmpresa = empresa.Id;
                context.ConfiguracionEmpresa.Where(c => c.Id == idEmpresa)
                    .ExecuteUpdate(u => u.SetProperty(c => c.SiguienteNumeroNotaCredito, c => c.SiguienteNumeroNotaCredito));
                context.Entry(empresa).Reload();
            }

            r.Activa = true;
            r.CAI = empresa.NotaCreditoCAI;
            r.RangoInicial = empresa.NotaCreditoRangoInicial;
            r.RangoFinal = empresa.NotaCreditoRangoFinal;
            r.FechaLimiteEmision = empresa.NotaCreditoFechaLimite;

            var errorCfg = FacturacionFiscalHelper.ValidarConfiguracion(
                empresa.NotaCreditoCAI, empresa.NotaCreditoRangoInicial,
                empresa.NotaCreditoRangoFinal, empresa.NotaCreditoFechaLimite);
            if (errorCfg != null)
            {
                r.Ok = false;
                r.Error = "Rango de notas de crédito incompleto: " + errorCfg.Replace("la facturación fiscal", "las notas de crédito fiscales");
                return r;
            }

            if (empresa.NotaCreditoFechaLimite!.Value.Date < DateTime.Today)
            {
                r.Ok = false;
                r.Error = $"El CAI de notas de crédito venció el {empresa.NotaCreditoFechaLimite:dd/MM/yyyy}. Actualiza el rango en Configuración.";
                return r;
            }

            FacturacionFiscalHelper.TryParsear(empresa.NotaCreditoRangoInicial, out var prefijo, out var inicio, out var ancho);
            FacturacionFiscalHelper.TryParsear(empresa.NotaCreditoRangoFinal, out _, out var fin, out _);

            var siguiente = Math.Max(empresa.SiguienteNumeroNotaCredito, inicio);
            var emitidos = context.Devoluciones
                .Where(d => d.CAI == empresa.NotaCreditoCAI && d.Numero.StartsWith(prefijo))
                .Select(d => d.Numero)
                .ToList();
            foreach (var n in emitidos)
            {
                if (FacturacionFiscalHelper.TryParsear(n, out var pf, out var num, out _) && pf == prefijo && num >= siguiente)
                    siguiente = num + 1;
            }

            if (siguiente > fin)
            {
                r.Ok = false;
                r.Error = "Se agotó el rango autorizado de notas de crédito. Actualiza el rango en Configuración.";
                return r;
            }

            r.Numero = prefijo + siguiente.ToString().PadLeft(ancho, '0');
            empresa.SiguienteNumeroNotaCredito = siguiente + 1;
            context.SaveChanges();
            return r;
        }
    }
}
