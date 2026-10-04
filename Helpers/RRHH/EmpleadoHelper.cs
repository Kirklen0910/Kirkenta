using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.RRHH
{
    /// <summary>
    /// Lógica de negocio para empleados: código autogenerado, antigüedad,
    /// acumulación de vacaciones, etc.
    /// </summary>
    public static class EmpleadoHelper
    {
        /// <summary>
        /// Genera el código de un nuevo empleado usando NumeroDocumentoHelper.
        /// </summary>
        public static string GenerarCodigo(ApplicationDbContext context)
        {
            return NumeroDocumentoHelper.GenerarSiguiente(context, "Empleado");
        }

        /// <summary>
        /// Preview del próximo código sin generarlo.
        /// </summary>
        public static string PreviewCodigo(ApplicationDbContext context)
        {
            return NumeroDocumentoHelper.PreviewSiguiente(context, "Empleado");
        }

        /// <summary>
        /// Calcula la antigüedad en años cumplidos.
        /// </summary>
        public static int CalcularAniosAntiguedad(Empleado empleado, DateTime? fechaReferencia = null)
        {
            var fecha = fechaReferencia ?? DateTime.Today;
            var anios = fecha.Year - empleado.FechaIngreso.Year;

            if (empleado.FechaIngreso.Date > fecha.AddYears(-anios).Date)
                anios--;

            return Math.Max(0, anios);
        }

        /// <summary>
        /// Calcula meses totales de antigüedad.
        /// </summary>
        public static int CalcularMesesAntiguedad(Empleado empleado, DateTime? fechaReferencia = null)
        {
            var fecha = fechaReferencia ?? DateTime.Today;
            return ((fecha.Year - empleado.FechaIngreso.Year) * 12) + fecha.Month - empleado.FechaIngreso.Month;
        }

        /// <summary>
        /// Determina cuántos días de vacaciones le corresponden a un empleado
        /// según su antigüedad y la tabla configurada.
        /// </summary>
        public static decimal CalcularDiasVacacionesPorAntiguedad(
            ApplicationDbContext context,
            Empleado empleado,
            int anioAntiguedad)
        {
            var config = context.ConfiguracionEmpresa.FirstOrDefault();
            var tablaStr = config?.RHTablaVacaciones ?? "1:10,2:12,3:15,4:20,5:20";
            var maxAnio = config?.RHAntiguedadMaxTabla ?? 5;

            // Parsear "1:10,2:12,3:15,4:20,5:20"
            var tabla = new Dictionary<int, decimal>();
            foreach (var par in tablaStr.Split(','))
            {
                var partes = par.Split(':');
                if (partes.Length == 2 && int.TryParse(partes[0], out var anio) &&
                    decimal.TryParse(partes[1], out var dias))
                {
                    tabla[anio] = dias;
                }
            }

            // Si supera el máximo, usar el último valor
            if (anioAntiguedad >= maxAnio)
            {
                var ultimo = tabla.OrderByDescending(k => k.Key).FirstOrDefault();
                return ultimo.Value;
            }

            return tabla.GetValueOrDefault(anioAntiguedad, 0);
        }

        /// <summary>
        /// Procesa la acumulación anual de vacaciones para un empleado.
        /// Se llama cuando el empleado cumple un aniversario.
        /// </summary>
        public static (decimal diasAgregados, string mensaje) ProcesarAcumulacionVacaciones(
            ApplicationDbContext context,
            Empleado empleado,
            DateTime fechaReferencia)
        {
            try
            {
                var aniosAntiguedad = CalcularAniosAntiguedad(empleado, fechaReferencia);

                // Si ya se procesó este año, no hacer nada
                if (empleado.UltimoAnioVacacionesProcesado >= aniosAntiguedad)
                    return (0, "Ya se procesó este año");

                // Calcular cuántos años hay que procesar (por si saltó años)
                decimal totalDiasAgregados = 0;
                int anioInicio = empleado.UltimoAnioVacacionesProcesado + 1;

                for (int anio = anioInicio; anio <= aniosAntiguedad; anio++)
                {
                    var dias = CalcularDiasVacacionesPorAntiguedad(context, empleado, anio);
                    totalDiasAgregados += dias;
                }

                empleado.DiasVacacionesGanados += totalDiasAgregados;
                empleado.DiasVacacionesDisponibles += totalDiasAgregados;
                empleado.UltimoAnioVacacionesProcesado = aniosAntiguedad;

                context.SaveChanges();

                return (totalDiasAgregados, $"Se agregaron {totalDiasAgregados} días de vacaciones");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmpleadoHelper] Error acumulando vacaciones: {ex.Message}");
                return (0, ex.Message);
            }
        }

        /// <summary>
        /// Procesa la acumulación de vacaciones para TODOS los empleados activos.
        /// Se llama una vez al día o al arrancar la app.
        /// </summary>
        public static int ProcesarAcumulacionGlobal(ApplicationDbContext context)
        {
            try
            {
                var hoy = DateTime.Today;
                var empleados = context.Empleados
                    .Where(e => e.Estado == "Activo" || e.Estado == "Vacaciones")
                    .ToList();

                int procesados = 0;

                foreach (var emp in empleados)
                {
                    // Solo procesar si ya cumplió al menos 1 año
                    var anios = CalcularAniosAntiguedad(emp, hoy);
                    if (anios >= 1 && emp.UltimoAnioVacacionesProcesado < anios)
                    {
                        var (dias, _) = ProcesarAcumulacionVacaciones(context, emp, hoy);
                        if (dias > 0) procesados++;
                    }
                }

                return procesados;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmpleadoHelper] Error acumulación global: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Obtiene los feriados del país de la empresa en un rango de fechas.
        /// </summary>
        public static List<Feriado> ObtenerFeriados(
            ApplicationDbContext context,
            DateTime desde,
            DateTime hasta)
        {
            var config = context.ConfiguracionEmpresa.FirstOrDefault();
            var paisCodigo = config?.PaisCodigo ?? "HN";

            return context.Feriados
                .AsNoTracking()
                .Where(f => f.Activo
                         && f.PaisCodigo == paisCodigo
                         && f.Fecha >= desde.Date
                         && f.Fecha <= hasta.Date)
                .OrderBy(f => f.Fecha)
                .ToList();
        }
    }
}