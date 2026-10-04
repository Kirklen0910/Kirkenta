using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Lógica de conciliación bancaria: matching automático, cierre, cálculo de diferencias.
    /// </summary>
    public static class ConciliacionHelper
    {
        /// <summary>
        /// Rango de días para considerar una fecha "parecida" en el matching automático.
        /// </summary>
        private const int DiasTolerancia = 3;

        /// <summary>
        /// Rango de diferencia de monto para considerar "parecido" (redondeos).
        /// </summary>
        private const decimal ToleranciaMonto = 0.01m;

        /// <summary>
        /// Crea una nueva conciliación con sus líneas del sistema cargadas automáticamente
        /// (movimientos activos de la cuenta en el período).
        /// </summary>
        public static (ConciliacionBancaria? conciliacion, string? error) Crear(
            ApplicationDbContext context,
            int cuentaId,
            DateTime fechaInicio,
            DateTime fechaFin,
            decimal saldoBanco,
            string? notas,
            int usuarioId)
        {
            try
            {
                if (fechaFin < fechaInicio)
                    return (null, "La fecha fin no puede ser anterior a la fecha inicio");

                var cuenta = context.CuentasFinancieras.FirstOrDefault(c => c.Id == cuentaId);
                if (cuenta == null)
                    return (null, "Cuenta no encontrada");

                if (cuenta.Tipo != "Banco")
                    return (null, "Solo se pueden conciliar cuentas bancarias");

                if (!cuenta.Activa)
                    return (null, $"La cuenta '{cuenta.Nombre}' está inactiva");

                // Verificar que no exista una conciliación abierta para la misma cuenta y período
                var existente = context.ConciliacionesBancarias
                    .Any(c => c.CuentaId == cuentaId
                           && c.FechaInicio == fechaInicio.Date
                           && c.FechaFin == fechaFin.Date
                           && (c.Estado == "Abierta" || c.Estado == "EnRevision"));

                if (existente)
                    return (null, "Ya existe una conciliación abierta para esta cuenta y período");

                // Calcular saldo del sistema al final del período
                // Saldo sistema = saldo inicial del período + movimientos del período
                // Simplificación: usamos el saldo actual y restamos los movimientos posteriores al período.
                // Una implementación más robusta requeriría un registro histórico de saldos.
                var movimientosPosteriores = context.MovimientosFinancieros
                    .Where(m => m.Estado == "Activo"
                             && m.Fecha > fechaFin.Date.AddDays(1).AddSeconds(-1)
                             && (m.CuentaId == cuentaId || m.CuentaDestinoId == cuentaId))
                    .ToList();

                decimal efectoPosterior = 0;
                foreach (var m in movimientosPosteriores)
                {
                    if (m.CuentaId == cuentaId)
                    {
                        if (m.Tipo == "Ingreso") efectoPosterior += m.Monto;
                        else if (m.Tipo == "Egreso") efectoPosterior -= m.Monto;
                        else if (m.Tipo == "Transferencia") efectoPosterior -= m.Monto;
                    }
                    if (m.CuentaDestinoId == cuentaId && m.Tipo == "Transferencia")
                    {
                        efectoPosterior += m.Monto;
                    }
                }

                var saldoSistema = cuenta.SaldoActual - efectoPosterior;

                var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "ConciliacionBancaria");

                var conciliacion = new ConciliacionBancaria
                {
                    Numero = numero,
                    CuentaId = cuentaId,
                    FechaInicio = fechaInicio.Date,
                    FechaFin = fechaFin.Date,
                    SaldoBanco = saldoBanco,
                    SaldoSistema = saldoSistema,
                    Diferencia = saldoBanco - saldoSistema,
                    Estado = "Abierta",
                    Notas = notas,
                    FechaCreacion = DateTime.Now,
                    UsuarioCreoId = usuarioId,
                    EmpresaId = 1
                };

                context.ConciliacionesBancarias.Add(conciliacion);
                context.SaveChanges();

                // Cargar líneas del sistema (movimientos del período)
                CargarLineasSistema(context, conciliacion);

                // Recalcular totales
                RecalcularTotales(context, conciliacion);

                return (conciliacion, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConciliacionHelper] Error al crear: {ex.Message}");
                return (null, ex.Message);
            }
        }

        /// <summary>
        /// Carga las líneas del sistema (movimientos del período) como detalles de la conciliación.
        /// </summary>
        public static void CargarLineasSistema(ApplicationDbContext context, ConciliacionBancaria conciliacion)
        {
            // Eliminar líneas del sistema existentes (para recargar)
            var existentes = context.ConciliacionesDetalle
                .Where(d => d.ConciliacionBancariaId == conciliacion.Id && d.Origen == "Sistema")
                .ToList();

            if (existentes.Any())
            {
                context.ConciliacionesDetalle.RemoveRange(existentes);
                context.SaveChanges();
            }

            var movimientos = context.MovimientosFinancieros
                .Where(m => m.Estado == "Activo"
                         && m.Fecha >= conciliacion.FechaInicio
                         && m.Fecha < conciliacion.FechaFin.AddDays(1)
                         && (m.CuentaId == conciliacion.CuentaId || m.CuentaDestinoId == conciliacion.CuentaId))
                .OrderBy(m => m.Fecha)
                .ToList();

            foreach (var m in movimientos)
            {
                decimal montoFirmado = 0;
                if (m.CuentaId == conciliacion.CuentaId)
                {
                    if (m.Tipo == "Ingreso") montoFirmado = m.Monto;
                    else if (m.Tipo == "Egreso") montoFirmado = -m.Monto;
                    else if (m.Tipo == "Transferencia") montoFirmado = -m.Monto;
                }
                if (m.CuentaDestinoId == conciliacion.CuentaId && m.Tipo == "Transferencia")
                {
                    montoFirmado = m.Monto;
                }

                context.ConciliacionesDetalle.Add(new ConciliacionDetalle
                {
                    ConciliacionBancariaId = conciliacion.Id,
                    Origen = "Sistema",
                    MovimientoId = m.Id,
                    Fecha = m.Fecha,
                    Descripcion = m.Concepto,
                    Referencia = m.Referencia,
                    Monto = montoFirmado,
                    Matcheada = false,
                    FechaCreacion = DateTime.Now
                });
            }

            context.SaveChanges();
        }

        /// <summary>
        /// Ejecuta matching automático entre las líneas del sistema y las del banco.
        /// Criterios: mismo monto (±tolerancia), fecha dentro de ±DiasTolerancia, referencia similar.
        /// </summary>
        public static int MatchingAutomatico(ApplicationDbContext context, int conciliacionId)
        {
            var detalles = context.ConciliacionesDetalle
                .Where(d => d.ConciliacionBancariaId == conciliacionId)
                .ToList();

            var sistema = detalles.Where(d => d.Origen == "Sistema" && !d.Matcheada).ToList();
            var banco = detalles.Where(d => d.Origen == "Banco" && !d.Matcheada).ToList();

            int matches = 0;

            foreach (var s in sistema)
            {
                // Buscar la línea del banco que mejor matchea
                var candidatos = banco
                    .Where(b => !b.Matcheada
                             && Math.Abs(b.Monto - s.Monto) <= ToleranciaMonto
                             && Math.Abs((b.Fecha - s.Fecha).TotalDays) <= DiasTolerancia)
                    .OrderBy(b => Math.Abs((b.Fecha - s.Fecha).TotalDays))
                    .ToList();

                if (candidatos.Count == 0) continue;

                // Si hay varios, preferir el que coincida en referencia
                ConciliacionDetalle? mejor = null;
                if (!string.IsNullOrWhiteSpace(s.Referencia))
                {
                    mejor = candidatos.FirstOrDefault(c =>
                        !string.IsNullOrWhiteSpace(c.Referencia) &&
                        c.Referencia.Trim().ToLower() == s.Referencia.Trim().ToLower());
                }

                mejor ??= candidatos.First();

                // Marcar ambos
                s.Matcheada = true;
                s.MatcheadaConDetalleId = mejor.Id;
                s.TipoMatch = "Automatico";

                mejor.Matcheada = true;
                mejor.MatcheadaConDetalleId = s.Id;
                mejor.TipoMatch = "Automatico";

                matches++;
            }

            context.SaveChanges();
            return matches;
        }

        /// <summary>
        /// Marca manualmente dos líneas como matcheadas.
        /// </summary>
        public static (bool ok, string? error) MatchingManual(
            ApplicationDbContext context,
            int detalleSistemaId,
            int detalleBancoId)
        {
            try
            {
                var sistema = context.ConciliacionesDetalle.FirstOrDefault(d => d.Id == detalleSistemaId);
                var banco = context.ConciliacionesDetalle.FirstOrDefault(d => d.Id == detalleBancoId);

                if (sistema == null || banco == null)
                    return (false, "Detalle no encontrado");

                if (sistema.Origen != "Sistema" || banco.Origen != "Banco")
                    return (false, "Debes emparejar una línea del sistema con una del banco");

                if (sistema.ConciliacionBancariaId != banco.ConciliacionBancariaId)
                    return (false, "Las líneas pertenecen a conciliaciones diferentes");

                if (sistema.Matcheada || banco.Matcheada)
                    return (false, "Una de las líneas ya está matcheada");

                if (Math.Abs(sistema.Monto - banco.Monto) > ToleranciaMonto)
                    return (false, $"Los montos no coinciden: sistema L. {sistema.Monto:N2}, banco L. {banco.Monto:N2}");

                sistema.Matcheada = true;
                sistema.MatcheadaConDetalleId = banco.Id;
                sistema.TipoMatch = "Manual";

                banco.Matcheada = true;
                banco.MatcheadaConDetalleId = sistema.Id;
                banco.TipoMatch = "Manual";

                context.SaveChanges();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Deshace un match (manual o automático).
        /// </summary>
        public static (bool ok, string? error) DeshacerMatch(
            ApplicationDbContext context,
            int detalleId)
        {
            try
            {
                var detalle = context.ConciliacionesDetalle.FirstOrDefault(d => d.Id == detalleId);
                if (detalle == null) return (false, "Detalle no encontrado");
                if (!detalle.Matcheada) return (false, "Esta línea no está matcheada");

                var contraparte = detalle.MatcheadaConDetalleId.HasValue
                    ? context.ConciliacionesDetalle.FirstOrDefault(d => d.Id == detalle.MatcheadaConDetalleId.Value)
                    : null;

                detalle.Matcheada = false;
                detalle.MatcheadaConDetalleId = null;
                detalle.TipoMatch = null;

                if (contraparte != null)
                {
                    contraparte.Matcheada = false;
                    contraparte.MatcheadaConDetalleId = null;
                    contraparte.TipoMatch = null;
                }

                context.SaveChanges();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Recalcula los totales de la conciliación (líneas y matcheadas).
        /// </summary>
        public static void RecalcularTotales(ApplicationDbContext context, ConciliacionBancaria conciliacion)
        {
            var detalles = context.ConciliacionesDetalle
                .Where(d => d.ConciliacionBancariaId == conciliacion.Id)
                .ToList();

            conciliacion.TotalLineasSistema = detalles.Count(d => d.Origen == "Sistema");
            conciliacion.TotalLineasBanco = detalles.Count(d => d.Origen == "Banco");
            conciliacion.TotalMatcheadas = detalles.Count(d => d.Matcheada);
            conciliacion.TotalNoMatcheadas = detalles.Count(d => !d.Matcheada);

            context.SaveChanges();
        }

        /// <summary>
        /// Marca la conciliación como "Conciliada" (cerrada).
        /// Requiere que la diferencia sea 0.
        /// </summary>
        public static (bool ok, string? error) Cerrar(
            ApplicationDbContext context,
            int conciliacionId,
            int usuarioId,
            string? notasCierre)
        {
            try
            {
                var conciliacion = context.ConciliacionesBancarias
                    .FirstOrDefault(c => c.Id == conciliacionId);

                if (conciliacion == null) return (false, "Conciliación no encontrada");
                if (conciliacion.Estado != "Abierta" && conciliacion.Estado != "EnRevision")
                    return (false, "La conciliación no está abierta");

                // Recalcular diferencia final
                var detalles = context.ConciliacionesDetalle
                    .Where(d => d.ConciliacionBancariaId == conciliacionId)
                    .ToList();

                var noMatcheadasSistema = detalles
                    .Where(d => d.Origen == "Sistema" && !d.Matcheada)
                    .Sum(d => d.Monto);

                var noMatcheadasBanco = detalles
                    .Where(d => d.Origen == "Banco" && !d.Matcheada)
                    .Sum(d => d.Monto);

                // Diferencia final = lo que quedó pendiente del sistema - lo que quedó pendiente del banco
                var diferenciaFinal = noMatcheadasSistema - noMatcheadasBanco;

                if (Math.Abs(diferenciaFinal) > ToleranciaMonto)
                {
                    return (false,
                        $"No se puede cerrar: quedan diferencias por L. {Math.Abs(diferenciaFinal):N2}. " +
                        $"Empareja o marca como revisadas las líneas pendientes.");
                }

                conciliacion.Estado = "Conciliada";
                conciliacion.UsuarioCierraId = usuarioId;
                conciliacion.FechaCierre = DateTime.Now;
                conciliacion.NotasCierre = notasCierre;

                context.SaveChanges();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Cancela una conciliación (no se puede cancelar si está Conciliada).
        /// </summary>
        public static (bool ok, string? error) Cancelar(
            ApplicationDbContext context,
            int conciliacionId,
            string motivo)
        {
            try
            {
                var conciliacion = context.ConciliacionesBancarias
                    .FirstOrDefault(c => c.Id == conciliacionId);

                if (conciliacion == null) return (false, "Conciliación no encontrada");
                if (conciliacion.Estado == "Conciliada")
                    return (false, "No se puede cancelar una conciliación ya cerrada");

                conciliacion.Estado = "Cancelada";
                conciliacion.Notas = (conciliacion.Notas ?? "") + $"\n[Cancelada] {motivo}";

                context.SaveChanges();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}