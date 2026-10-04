using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Gestiona el cierre contable mensual.
    /// Al cerrar un mes, se bloquean todos los movimientos con fecha dentro de ese mes.
    /// Se puede reabrir (con motivo) por un usuario con permiso.
    /// </summary>
    public static class CierreContableHelper
    {
        /// <summary>
        /// Verifica si una fecha está dentro de un mes cerrado.
        /// Devuelve true si el mes está cerrado (bloqueado).
        /// </summary>
        public static bool EstaCerrado(ApplicationDbContext context, DateTime fecha)
        {
            var anio = fecha.Year;
            var mes = fecha.Month;

            var cierre = context.CierresContables
                .FirstOrDefault(c => c.Anio == anio && c.Mes == mes);

            return cierre != null && cierre.Estado == "Cerrado";
        }

        /// <summary>
        /// Valida si una fecha se puede usar para registrar movimientos.
        /// Devuelve (ok, error). Si el mes está cerrado, ok=false.
        /// </summary>
        public static (bool ok, string? error) ValidarFecha(ApplicationDbContext context, DateTime fecha)
        {
            var cierre = context.CierresContables
                .FirstOrDefault(c => c.Anio == fecha.Year && c.Mes == fecha.Month);

            if (cierre == null || cierre.Estado == "Abierto")
                return (true, null);

            return (false,
                $"El período {cierre.PeriodoTexto} está CERRADO contablemente. " +
                $"Debes reabrirlo antes de registrar movimientos con esa fecha.");
        }

        /// <summary>
        /// Obtiene el estado de un mes específico. Devuelve null si no hay registro (mes abierto por defecto).
        /// </summary>
        public static CierreContable? Obtener(ApplicationDbContext context, int anio, int mes)
        {
            return context.CierresContables
                .FirstOrDefault(c => c.Anio == anio && c.Mes == mes);
        }

        /// <summary>
        /// Cierra un mes. Registra totales de ingresos, egresos y cantidad de movimientos
        /// para auditoría. No permite cerrar un mes ya cerrado.
        /// </summary>
        public static (bool ok, string? error, CierreContable? cierre) Cerrar(
            ApplicationDbContext context,
            int anio,
            int mes,
            int usuarioId,
            string? notas = null)
        {
            try
            {
                if (mes < 1 || mes > 12)
                    return (false, "Mes inválido", null);

                // Verificar que no exista ya cerrado
                var existente = context.CierresContables
                    .FirstOrDefault(c => c.Anio == anio && c.Mes == mes);

                if (existente != null && existente.Estado == "Cerrado")
                    return (false, $"El período {existente.PeriodoTexto} ya está cerrado", null);

                // Calcular totales del mes
                var inicioMes = new DateTime(anio, mes, 1);
                var finMes = inicioMes.AddMonths(1);

                var movimientos = context.MovimientosFinancieros
                    .Where(m => m.Estado == "Activo"
                             && m.Fecha >= inicioMes
                             && m.Fecha < finMes)
                    .ToList();

                var ingresos = movimientos.Where(m => m.Tipo == "Ingreso").Sum(m => m.Monto);
                var egresos = movimientos.Where(m => m.Tipo == "Egreso").Sum(m => m.Monto);
                var balance = ingresos - egresos;

                CierreContable cierre;
                if (existente != null)
                {
                    // Reutilizar la fila existente (estaba Abierto)
                    cierre = existente;
                }
                else
                {
                    cierre = new CierreContable
                    {
                        Anio = anio,
                        Mes = mes,
                        FechaCreacion = DateTime.Now,
                        EmpresaId = 1
                    };
                    context.CierresContables.Add(cierre);
                }

                cierre.Estado = "Cerrado";
                cierre.FechaCierre = DateTime.Now;
                cierre.UsuarioCierreId = usuarioId;
                cierre.TotalIngresos = ingresos;
                cierre.TotalEgresos = egresos;
                cierre.Balance = balance;
                cierre.CantidadMovimientos = movimientos.Count;
                cierre.Notas = notas;

                // Limpiar datos de reapertura si existían (por si se cerró, reabrió y volvió a cerrar)
                cierre.FechaReapertura = null;
                cierre.UsuarioReaperturaId = null;
                cierre.MotivoReapertura = null;

                context.SaveChanges();

                return (true, null, cierre);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CierreContableHelper] Error al cerrar: {ex.Message}");
                return (false, ex.Message, null);
            }
        }

        /// <summary>
        /// Reabre un mes previamente cerrado. Requiere motivo.
        /// </summary>
        public static (bool ok, string? error) Reabrir(
            ApplicationDbContext context,
            int anio,
            int mes,
            int usuarioId,
            string motivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                    return (false, "Debes indicar el motivo de la reapertura");

                var cierre = context.CierresContables
                    .FirstOrDefault(c => c.Anio == anio && c.Mes == mes);

                if (cierre == null)
                    return (false, "El período no tiene cierre registrado");

                if (cierre.Estado != "Cerrado")
                    return (false, $"El período {cierre.PeriodoTexto} no está cerrado");

                cierre.Estado = "Abierto";
                cierre.FechaReapertura = DateTime.Now;
                cierre.UsuarioReaperturaId = usuarioId;
                cierre.MotivoReapertura = motivo;

                context.SaveChanges();

                return (true, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CierreContableHelper] Error al reabrir: {ex.Message}");
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Devuelve todos los meses de un año con su estado.
        /// Los meses sin registro se consideran "Abierto".
        /// </summary>
        public static List<CierreContable> ObtenerEstadoAnual(ApplicationDbContext context, int anio)
        {
            var cierres = context.CierresContables
                .Where(c => c.Anio == anio)
                .ToList();

            var resultado = new List<CierreContable>();

            for (int mes = 1; mes <= 12; mes++)
            {
                var existente = cierres.FirstOrDefault(c => c.Mes == mes);

                if (existente != null)
                {
                    resultado.Add(existente);
                }
                else
                {
                    resultado.Add(new CierreContable
                    {
                        Anio = anio,
                        Mes = mes,
                        Estado = "Abierto",
                        EmpresaId = 1
                    });
                }
            }

            return resultado;
        }
    }
}