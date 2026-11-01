using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Facturación fiscal (CAI / rango autorizado SAR).
    /// Solo actúa cuando ConfiguracionEmpresa.FacturacionFiscalActiva = true.
    /// Si está inactiva, el sistema usa la numeración interna y no muestra avisos de CAI.
    /// </summary>
    public static class FacturacionFiscalHelper
    {
        public class ResultadoFiscal
        {
            public bool Activa { get; set; }
            public bool Ok { get; set; } = true;
            public string? Error { get; set; }
            public string? Advertencia { get; set; }
            public string? Numero { get; set; }
            public string? CAI { get; set; }
            public string? RangoInicial { get; set; }
            public string? RangoFinal { get; set; }
            public DateTime? FechaLimiteEmision { get; set; }
            public long Restantes { get; set; }
        }

        public static ConfiguracionEmpresa? ObtenerEmpresa(ApplicationDbContext context)
            => context.ConfiguracionEmpresa.FirstOrDefault();

        public static bool EstaActiva(ApplicationDbContext context)
            => ObtenerEmpresa(context)?.FacturacionFiscalActiva == true;

        /// <summary>Separa "000-001-01-00000001" en prefijo "000-001-01-" y número 1 (ancho 8).</summary>
        public static bool TryParsear(string? texto, out string prefijo, out long numero, out int ancho)
        {
            prefijo = ""; numero = 0; ancho = 0;
            if (string.IsNullOrWhiteSpace(texto)) return false;
            var m = Regex.Match(texto.Trim(), @"^(.*?)(\d+)$");
            if (!m.Success) return false;
            prefijo = m.Groups[1].Value;
            ancho = m.Groups[2].Value.Length;
            return long.TryParse(m.Groups[2].Value, out numero);
        }

        /// <summary>Valida los datos fiscales de la configuración. Devuelve null si todo está bien.</summary>
        public static string? ValidarConfiguracion(string? cai, string? rangoInicial, string? rangoFinal, DateTime? fechaLimite)
        {
            if (string.IsNullOrWhiteSpace(cai))
                return "Para activar la facturación fiscal debes ingresar el CAI.";
            if (!TryParsear(rangoInicial, out var p1, out var n1, out _))
                return "El rango inicial no es válido (ej: 000-001-01-00000001).";
            if (!TryParsear(rangoFinal, out var p2, out var n2, out _))
                return "El rango final no es válido (ej: 000-001-01-00000100).";
            if (!string.Equals(p1, p2, StringComparison.Ordinal))
                return "El rango inicial y el final deben tener el mismo prefijo (establecimiento, punto de emisión y tipo de documento).";
            if (n2 < n1)
                return "El rango final no puede ser menor que el rango inicial.";
            if (fechaLimite == null)
                return "Debes indicar la fecha límite de emisión.";
            return null;
        }

        /// <summary>
        /// Calcula el próximo número fiscal sin consumirlo. Si la facturación fiscal está inactiva
        /// devuelve Activa=false y el llamador debe usar la numeración interna.
        /// </summary>
        public static ResultadoFiscal Previsualizar(ApplicationDbContext context)
            => Calcular(context, consumir: false);

        /// <summary>
        /// Reserva (consume) el próximo número fiscal. Debe llamarse dentro de la misma transacción
        /// que crea la factura. Guarda el contador en BD.
        /// </summary>
        public static ResultadoFiscal Reservar(ApplicationDbContext context)
            => Calcular(context, consumir: true);

        private static ResultadoFiscal Calcular(ApplicationDbContext context, bool consumir)
        {
            var empresa = ObtenerEmpresa(context);
            var r = new ResultadoFiscal();
            if (empresa == null || !empresa.FacturacionFiscalActiva) return r;

            // Al consumir: bloquear la fila de la empresa y releerla, así dos cajeros
            // facturando a la vez nunca reciben el mismo número fiscal.
            if (consumir && context.Database.IsRelational())
            {
                var idEmpresa = empresa.Id;
                context.ConfiguracionEmpresa.Where(c => c.Id == idEmpresa)
                    .ExecuteUpdate(u => u.SetProperty(c => c.SiguienteNumeroFiscal, c => c.SiguienteNumeroFiscal));
                context.Entry(empresa).Reload();
            }

            r.Activa = true;
            r.CAI = empresa.CAI;
            r.RangoInicial = empresa.RangoInicial;
            r.RangoFinal = empresa.RangoFinal;
            r.FechaLimiteEmision = empresa.FechaLimiteEmision;

            var errorCfg = ValidarConfiguracion(empresa.CAI, empresa.RangoInicial, empresa.RangoFinal, empresa.FechaLimiteEmision);
            if (errorCfg != null)
            {
                r.Ok = false;
                r.Error = "Facturación fiscal activa pero incompleta: " + errorCfg;
                return r;
            }

            if (empresa.FechaLimiteEmision!.Value.Date < DateTime.Today)
            {
                r.Ok = false;
                r.Error = $"El CAI venció el {empresa.FechaLimiteEmision:dd/MM/yyyy}. Solicita un nuevo rango al SAR y actualízalo en Configuración, o desactiva la facturación fiscal.";
                return r;
            }

            TryParsear(empresa.RangoInicial, out var prefijo, out var inicio, out var ancho);
            TryParsear(empresa.RangoFinal, out _, out var fin, out _);

            // Siguiente = máx(contador guardado, rango inicial, último emitido con este CAI + 1)
            var siguiente = Math.Max(empresa.SiguienteNumeroFiscal, inicio);
            var emitidos = context.Facturas
                .Where(f => f.CAI == empresa.CAI && f.Numero.StartsWith(prefijo))
                .Select(f => f.Numero)
                .ToList();
            foreach (var n in emitidos)
            {
                if (TryParsear(n, out var pf, out var num, out _) && pf == prefijo && num >= siguiente)
                    siguiente = num + 1;
            }

            if (siguiente > fin)
            {
                r.Ok = false;
                r.Error = "Se agotó el rango de facturación autorizado por el SAR. Solicita un nuevo rango y actualízalo en Configuración.";
                return r;
            }

            r.Numero = prefijo + siguiente.ToString().PadLeft(ancho, '0');
            r.Restantes = fin - siguiente; // después de emitir esta

            var diasRestantes = (empresa.FechaLimiteEmision.Value.Date - DateTime.Today).Days;
            if (diasRestantes <= 15)
                r.Advertencia = $"El CAI vence en {diasRestantes} día(s) ({empresa.FechaLimiteEmision:dd/MM/yyyy}).";
            else if (r.Restantes <= 20)
                r.Advertencia = $"Quedan solo {r.Restantes + 1} número(s) disponibles en el rango autorizado.";

            if (consumir)
            {
                empresa.SiguienteNumeroFiscal = siguiente + 1;
                context.SaveChanges();
            }

            return r;
        }

        /// <summary>
        /// Aviso para el tablero y el panel móvil: rango autorizado por el SAR agotado o a punto de agotarse.
        /// null si la facturación fiscal está apagada, no hay problema o no se pudo calcular.
        /// (El vencimiento del CAI por fecha ya lo avisan las pantallas por su cuenta.)
        /// </summary>
        public static (string Texto, string Nivel)? AlertaRango(ApplicationDbContext context)
        {
            try
            {
                var f = Previsualizar(context);
                if (!f.Activa) return null;
                if (!f.Ok)
                    return f.Error != null && f.Error.StartsWith("Se agotó")
                        ? ("Se agotó el rango de facturación del SAR: no se pueden emitir facturas hasta registrar un rango nuevo.", "danger")
                        : null;
                var disponibles = f.Restantes + 1;
                if (disponibles <= 10) return ($"Quedan solo {disponibles} número(s) en el rango de facturación del SAR. Solicita un rango nuevo.", "danger");
                if (disponibles <= 50) return ($"Quedan {disponibles} números en el rango de facturación del SAR. Ve pidiendo un rango nuevo.", "warning");
                return null;
            }
            catch { return null; }
        }

        /// <summary>Copia el número y el snapshot fiscal a la factura (o numeración interna si está inactiva).</summary>
        public static void Aplicar(Factura factura, ResultadoFiscal fiscal)
        {
            if (!fiscal.Activa) return;
            factura.Numero = fiscal.Numero!;
            factura.CAI = fiscal.CAI;
            factura.RangoInicial = fiscal.RangoInicial;
            factura.RangoFinal = fiscal.RangoFinal;
            factura.FechaLimiteEmision = fiscal.FechaLimiteEmision;
        }

        /// <summary>
        /// Atajo para los puntos de emisión: devuelve (numero, error) y aplica el snapshot.
        /// Si no hay facturación fiscal usa la serie interna "Factura".
        /// </summary>
        public static (string? numero, string? error, string? advertencia) AsignarNumero(ApplicationDbContext context, Factura factura)
        {
            var f = Reservar(context);
            if (!f.Activa)
            {
                var n = NumeroDocumentoHelper.GenerarSiguiente(context, "Factura");
                factura.Numero = n;
                return (n, null, null);
            }
            if (!f.Ok) return (null, f.Error, null);
            Aplicar(factura, f);
            return (f.Numero, null, f.Advertencia);
        }
    }
}
