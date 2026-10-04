using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.RRHH
{
    /// <summary>
    /// Lógica para cálculo y gestión de vacaciones.
    /// </summary>
    public static class VacacionHelper
    {
        /// <summary>
        /// Calcula los días hábiles entre dos fechas (excluyendo fines de semana
        /// y feriados del país de la empresa).
        /// </summary>
        public static (decimal diasHabiles, int diasFeriados, List<Feriado> feriadosEnRango) CalcularDias(
            ApplicationDbContext context,
            DateTime fechaInicio,
            DateTime fechaFin)
        {
            var diasHabiles = 0;
            var feriadosEnRango = new List<Feriado>();

            // Obtener feriados del rango
            var feriados = EmpleadoHelper.ObtenerFeriados(context, fechaInicio, fechaFin);
            var feriadosDict = feriados.Select(f => f.Fecha.Date).ToHashSet();

            var cursor = fechaInicio.Date;
            while (cursor <= fechaFin.Date)
            {
                // Excluir sábados y domingos
                if (cursor.DayOfWeek != DayOfWeek.Saturday && cursor.DayOfWeek != DayOfWeek.Sunday)
                {
                    // Excluir feriados
                    if (feriadosDict.Contains(cursor))
                    {
                        var feriado = feriados.FirstOrDefault(f => f.Fecha.Date == cursor);
                        if (feriado != null) feriadosEnRango.Add(feriado);
                    }
                    else
                    {
                        diasHabiles++;
                    }
                }
                cursor = cursor.AddDays(1);
            }

            return (diasHabiles, feriadosEnRango.Count, feriadosEnRango);
        }

        /// <summary>
        /// Registra una nueva solicitud de vacaciones.
        /// </summary>
        public static (VacacionEmpleado? vacacion, string? error) Solicitar(
            ApplicationDbContext context,
            int empleadoId,
            DateTime fechaInicio,
            DateTime fechaFin,
            string? motivo,
            int? usuarioSolicitaId)
        {
            try
            {
                var empleado = context.Empleados.FirstOrDefault(e => e.Id == empleadoId);
                if (empleado == null) return (null, "Empleado no encontrado");

                if (fechaFin < fechaInicio) return (null, "La fecha fin no puede ser anterior a la fecha inicio");

                if (fechaInicio.Date < DateTime.Today) return (null, "La fecha de inicio no puede ser en el pasado");

                // Calcular días
                var (dias, diasFeriados, feriados) = CalcularDias(context, fechaInicio, fechaFin);

                if (dias <= 0) return (null, "El período no contiene días hábiles");

                // Validar saldo
                if (dias > empleado.DiasVacacionesDisponibles)
                {
                    return (null, $"Saldo insuficiente. Disponible: {empleado.DiasVacacionesDisponibles} días, solicitado: {dias} días");
                }

                // Validar solapamiento con otras vacaciones
                var solapa = context.VacacionesEmpleado.Any(v =>
                    v.EmpleadoId == empleadoId &&
                    v.Estado != "Rechazado" &&
                    v.Estado != "Cancelado" &&
                    ((fechaInicio >= v.FechaInicio && fechaInicio <= v.FechaFin) ||
                     (fechaFin >= v.FechaInicio && fechaFin <= v.FechaFin) ||
                     (fechaInicio <= v.FechaInicio && fechaFin >= v.FechaFin)));

                if (solapa) return (null, "Se solapa con otra solicitud de vacaciones");

                var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "Vacacion");

                var vacacion = new VacacionEmpleado
                {
                    Numero = numero,
                    EmpleadoId = empleadoId,
                    Estado = "Solicitado",
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                    DiasSolicitados = dias,
                    DiasFeriados = diasFeriados,
                    DiasADescontar = dias,
                    FechaSolicitud = DateTime.Now,
                    UsuarioSolicitaId = usuarioSolicitaId,
                    Motivo = motivo,
                    EmpresaId = 1
                };

                context.VacacionesEmpleado.Add(vacacion);
                context.SaveChanges();

                return (vacacion, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VacacionHelper] Error: {ex.Message}");
                return (null, ex.Message);
            }
        }

        /// <summary>
        /// Aprueba una solicitud de vacaciones. Descuenta los días del saldo.
        /// </summary>
        public static (bool ok, string? error) Aprobar(
            ApplicationDbContext context,
            int vacacionId,
            int usuarioApruebaId)
        {
            try
            {
                var vacacion = context.VacacionesEmpleado.FirstOrDefault(v => v.Id == vacacionId);
                if (vacacion == null) return (false, "Vacación no encontrada");
                if (vacacion.Estado != "Solicitado") return (false, "La solicitud ya fue procesada");

                var empleado = context.Empleados.FirstOrDefault(e => e.Id == vacacion.EmpleadoId);
                if (empleado == null) return (false, "Empleado no encontrado");

                if (vacacion.DiasADescontar > empleado.DiasVacacionesDisponibles)
                    return (false, "Saldo insuficiente");

                // Descontar
                empleado.DiasVacacionesDisponibles -= vacacion.DiasADescontar;
                empleado.DiasVacacionesTomados += vacacion.DiasADescontar;

                vacacion.Estado = "Aprobado";
                vacacion.UsuarioApruebaId = usuarioApruebaId;
                vacacion.FechaAprobacion = DateTime.Now;

                context.SaveChanges();
                return (true, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VacacionHelper] Error al aprobar: {ex.Message}");
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Rechaza una solicitud de vacaciones.
        /// </summary>
        public static (bool ok, string? error) Rechazar(
            ApplicationDbContext context,
            int vacacionId,
            int usuarioApruebaId,
            string motivoRechazo)
        {
            try
            {
                var vacacion = context.VacacionesEmpleado.FirstOrDefault(v => v.Id == vacacionId);
                if (vacacion == null) return (false, "Vacación no encontrada");
                if (vacacion.Estado != "Solicitado") return (false, "La solicitud ya fue procesada");

                vacacion.Estado = "Rechazado";
                vacacion.UsuarioApruebaId = usuarioApruebaId;
                vacacion.FechaAprobacion = DateTime.Now;
                vacacion.MotivoRechazo = motivoRechazo;

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