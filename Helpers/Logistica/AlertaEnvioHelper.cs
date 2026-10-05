using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Logistica
{
    /// <summary>
    /// Helper de alertas de envíos y rutas.
    /// Calcula en tiempo real el estado de cada envío según su fecha estimada
    /// y devuelve semáforos (Verde/Amarillo/Rojo) + contadores para el dashboard.
    /// NO persiste nada en BD: todo se calcula al vuelo.
    /// </summary>
    public static class AlertaEnvioHelper
    {
        // ============================================================
        // SEMÁFORO POR ENVÍO
        // ============================================================

        /// <summary>
        /// Devuelve el semáforo de un envío:
        ///   "Verde"    → completado o a tiempo
        ///   "Amarillo" → vence hoy
        ///   "Rojo"     → atrasado o fallido
        /// </summary>
        public static string CalcularSemaforo(Envio envio)
        {
            // Ya entregado: verde (completado)
            if (envio.Estado == "Entregado") return "Verde";

            // Fallido: requiere acción → rojo
            if (envio.Estado == "Fallido") return "Rojo";

            // Cancelado: gris (lo mapeamos a Verde para no alertar)
            if (envio.Estado == "Cancelado") return "Verde";

            // Pendiente o EnRuta: comparamos contra la fecha estimada
            if (!envio.FechaEntregaEstimada.HasValue) return "Verde";

            var hoy = DateTime.Today;
            var fechaEst = envio.FechaEntregaEstimada.Value.Date;

            if (fechaEst < hoy) return "Rojo";      // ya pasó
            if (fechaEst == hoy) return "Amarillo"; // vence hoy
            return "Verde";                          // a tiempo
        }

        /// <summary>
        /// Devuelve la etiqueta legible del semáforo para mostrar en la UI.
        /// </summary>
        public static string EtiquetaSemaforo(string semaforo)
        {
            return semaforo switch
            {
                "Rojo" => "🔴 Atrasado",
                "Amarillo" => "🟡 Vence hoy",
                "Verde" => "🟢 A tiempo",
                _ => "⚪ Sin estado"
            };
        }

        /// <summary>
        /// Calcula los días de atraso (positivo) o los días que faltan (negativo).
        /// Si no tiene fecha estimada, devuelve null.
        /// </summary>
        public static int? DiasDiferencia(Envio envio)
        {
            if (!envio.FechaEntregaEstimada.HasValue) return null;

            var hoy = DateTime.Today;
            var fechaEst = envio.FechaEntregaEstimada.Value.Date;

            return (hoy - fechaEst).Days;
        }

        // ============================================================
        // CONSULTAS DE ENVÍOS
        // ============================================================

        /// <summary>
        /// Envíos ATRASADOS: pendientes o en ruta, con fecha estimada ya vencida.
        /// </summary>
        public static List<Envio> ObtenerEnviosAtrasados(ApplicationDbContext context)
        {
            var hoy = DateTime.Today;

            return context.Envios
                .AsNoTracking()
                .Where(e => (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                         && e.FechaEntregaEstimada.HasValue
                         && e.FechaEntregaEstimada.Value.Date < hoy)
                .OrderBy(e => e.FechaEntregaEstimada)
                .ToList();
        }

        /// <summary>
        /// Envíos que VENCEN HOY: pendientes o en ruta, con fecha estimada = hoy.
        /// </summary>
        public static List<Envio> ObtenerEnviosVencenHoy(ApplicationDbContext context)
        {
            var hoy = DateTime.Today;

            return context.Envios
                .AsNoTracking()
                .Where(e => (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                         && e.FechaEntregaEstimada.HasValue
                         && e.FechaEntregaEstimada.Value.Date == hoy)
                .OrderBy(e => e.Id)
                .ToList();
        }

        /// <summary>
        /// Envíos que vencen en las próximas N horas.
        /// Nota: como solo guardamos fecha (no hora) en FechaEntregaEstimada,
        /// esto se interpreta como "vencen en los próximos N días".
        /// </summary>
        public static List<Envio> ObtenerEnviosProximosAVencer(ApplicationDbContext context, int dias = 2)
        {
            var hoy = DateTime.Today;
            var limite = hoy.AddDays(dias);

            return context.Envios
                .AsNoTracking()
                .Where(e => (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                         && e.FechaEntregaEstimada.HasValue
                         && e.FechaEntregaEstimada.Value.Date > hoy
                         && e.FechaEntregaEstimada.Value.Date <= limite)
                .OrderBy(e => e.FechaEntregaEstimada)
                .ToList();
        }

        /// <summary>
        /// Envíos FALLIDOS que aún no se han reagendado.
        /// </summary>
        public static List<Envio> ObtenerEnviosFallidos(ApplicationDbContext context)
        {
            return context.Envios
                .AsNoTracking()
                .Where(e => e.Estado == "Fallido")
                .OrderByDescending(e => e.FechaEntregaReal)
                .ToList();
        }

        /// <summary>
        /// Envíos entregados hoy.
        /// </summary>
        public static List<Envio> ObtenerEnviosEntregadosHoy(ApplicationDbContext context)
        {
            var hoy = DateTime.Today;
            var manana = hoy.AddDays(1);

            return context.Envios
                .AsNoTracking()
                .Where(e => e.Estado == "Entregado"
                         && e.FechaEntregaReal.HasValue
                         && e.FechaEntregaReal.Value >= hoy
                         && e.FechaEntregaReal.Value < manana)
                .OrderByDescending(e => e.FechaEntregaReal)
                .ToList();
        }

        // ============================================================
        // CONSULTAS DE RUTAS
        // ============================================================

        /// <summary>
        /// Rutas EN RIESGO: están en reparto y tienen paradas cuya fecha estimada
        /// ya pasó (deberían haberse entregado antes).
        /// </summary>
        public static List<Ruta> ObtenerRutasAtrasadas(ApplicationDbContext context)
        {
            var hoy = DateTime.Today;

            // Rutas que están actualmente en reparto
            var rutasActivas = context.Rutas
                .AsNoTracking()
                .Where(r => r.Estado == "EnReparto")
                .ToList();

            var rutasAtrasadas = new List<Ruta>();

            foreach (var ruta in rutasActivas)
            {
                var tieneParadasAtrasadas = context.Envios
                    .AsNoTracking()
                    .Any(e => e.RutaId == ruta.Id
                           && (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                           && e.FechaEntregaEstimada.HasValue
                           && e.FechaEntregaEstimada.Value.Date < hoy);

                if (tieneParadasAtrasadas)
                {
                    rutasAtrasadas.Add(ruta);
                }
            }

            return rutasAtrasadas.OrderBy(r => r.FechaSalida).ToList();
        }

        /// <summary>
        /// Rutas que salieron hoy (para el dashboard).
        /// </summary>
        public static List<Ruta> ObtenerRutasDespachadasHoy(ApplicationDbContext context)
        {
            var hoy = DateTime.Today;
            var manana = hoy.AddDays(1);

            return context.Rutas
                .AsNoTracking()
                .Where(r => r.FechaSalida.HasValue
                         && r.FechaSalida.Value >= hoy
                         && r.FechaSalida.Value < manana)
                .OrderByDescending(r => r.FechaSalida)
                .ToList();
        }

        // ============================================================
        // RESUMEN PARA DASHBOARD
        // ============================================================

        /// <summary>
        /// DTO con contadores de alertas para el dashboard.
        /// </summary>
        public class ResumenAlertas
        {
            public int EnviosAtrasados { get; set; }
            public int EnviosVencenHoy { get; set; }
            public int EnviosProximosAVencer { get; set; }
            public int EnviosFallidos { get; set; }
            public int EnviosEntregadosHoy { get; set; }

            public int RutasActivas { get; set; }
            public int RutasAtrasadas { get; set; }
            public int RutasDespachadasHoy { get; set; }

            public bool HayAlertas => EnviosAtrasados > 0
                                   || EnviosFallidos > 0
                                   || RutasAtrasadas > 0;
        }

        /// <summary>
        /// Calcula todos los contadores del dashboard en una sola llamada.
        /// </summary>
        public static ResumenAlertas ObtenerResumenAlertas(ApplicationDbContext context)
        {
            var hoy = DateTime.Today;

            var resumen = new ResumenAlertas
            {
                EnviosAtrasados = context.Envios
                    .AsNoTracking()
                    .Count(e => (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                             && e.FechaEntregaEstimada.HasValue
                             && e.FechaEntregaEstimada.Value.Date < hoy),

                EnviosVencenHoy = context.Envios
                    .AsNoTracking()
                    .Count(e => (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                             && e.FechaEntregaEstimada.HasValue
                             && e.FechaEntregaEstimada.Value.Date == hoy),

                EnviosProximosAVencer = context.Envios
                    .AsNoTracking()
                    .Count(e => (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                             && e.FechaEntregaEstimada.HasValue
                             && e.FechaEntregaEstimada.Value.Date > hoy
                             && e.FechaEntregaEstimada.Value.Date <= hoy.AddDays(2)),

                EnviosFallidos = context.Envios
                    .AsNoTracking()
                    .Count(e => e.Estado == "Fallido"),

                EnviosEntregadosHoy = context.Envios
                    .AsNoTracking()
                    .Count(e => e.Estado == "Entregado"
                             && e.FechaEntregaReal.HasValue
                             && e.FechaEntregaReal.Value.Date == hoy),

                RutasActivas = context.Rutas
                    .AsNoTracking()
                    .Count(r => r.Estado == "EnReparto"),

                RutasDespachadasHoy = context.Rutas
                    .AsNoTracking()
                    .Count(r => r.FechaSalida.HasValue
                             && r.FechaSalida.Value.Date == hoy)
            };

            // Rutas atrasadas: las que tienen paradas vencidas
            var rutasActivasIds = context.Rutas
                .AsNoTracking()
                .Where(r => r.Estado == "EnReparto")
                .Select(r => r.Id)
                .ToList();

            if (rutasActivasIds.Count > 0)
            {
                var rutasConAtraso = context.Envios
                    .AsNoTracking()
                    .Where(e => e.RutaId.HasValue
                             && rutasActivasIds.Contains(e.RutaId.Value)
                             && (e.Estado == "Pendiente" || e.Estado == "EnRuta")
                             && e.FechaEntregaEstimada.HasValue
                             && e.FechaEntregaEstimada.Value.Date < hoy)
                    .Select(e => e.RutaId!.Value)
                    .Distinct()
                    .ToList();

                resumen.RutasAtrasadas = rutasConAtraso.Count;
            }

            return resumen;
        }
    }
}