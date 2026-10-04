using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.RRHH
{
    public static class NominaHelper
    {
        // ============================================================
        // CONFIGURACIÓN DE DEDUCCIONES POR AÑO
        // ============================================================

        /// <summary>
        /// Obtiene la configuración de deducciones vigente para un año.
        /// Si no existe, la crea con valores por defecto de Honduras.
        /// </summary>
        public static ConfiguracionDeduccion ObtenerConfiguracion(ApplicationDbContext context, int anio)
        {
            var config = context.ConfiguracionDeducciones
                .FirstOrDefault(c => c.Anio == anio && c.Activa);

            if (config == null)
            {
                config = CrearConfiguracionPorDefecto(context, anio);
            }

            return config;
        }

        private static ConfiguracionDeduccion CrearConfiguracionPorDefecto(ApplicationDbContext context, int anio)
        {
            var config = new ConfiguracionDeduccion
            {
                Anio = anio,
                AplicaIHSS = true,
                PorcentajeIHSS = 2.5m,
                TopeIHSS = 11995.00m,
                AplicaRAP = true,
                PorcentajeRAP = 1.5m,
                AplicaISR = true,
                TopeAnualExentoISR = 250000.00m,
                MetodoISR = "Acumulativo",
                Activa = true,
                FechaCreacion = DateTime.Now,
                Notas = "Configuración por defecto (Honduras)"
            };

            context.ConfiguracionDeducciones.Add(config);
            context.SaveChanges();

            // Tramos ISR por defecto (Honduras 2024)
            var tramos = new List<TramoISR>
            {
                new() { ConfiguracionDeduccionId = config.Id, Orden = 1, Desde = 0, Hasta = 15692.25m, Porcentaje = 0, MontoFijo = 0, Descripcion = "Exento" },
                new() { ConfiguracionDeduccionId = config.Id, Orden = 2, Desde = 15692.26m, Hasta = 23246.20m, Porcentaje = 15, MontoFijo = 0, Descripcion = "15% sobre excedente" },
                new() { ConfiguracionDeduccionId = config.Id, Orden = 3, Desde = 23246.21m, Hasta = 55170.68m, Porcentaje = 20, MontoFijo = 1133.09m, Descripcion = "L. 1,133.09 + 20% sobre excedente" },
                new() { ConfiguracionDeduccionId = config.Id, Orden = 4, Desde = 55170.69m, Hasta = null, Porcentaje = 25, MontoFijo = 7517.98m, Descripcion = "L. 7,517.98 + 25% sobre excedente" }
            };

            context.TramosISR.AddRange(tramos);
            context.SaveChanges();

            return config;
        }

        // ============================================================
        // CÁLCULO DE ISR
        // ============================================================

        /// <summary>
        /// Calcula el ISR mensual según la tabla de tramos.
        /// </summary>
        public static decimal CalcularISRMensual(decimal salarioBrutoMensual, List<TramoISR> tramos)
        {
            if (tramos == null || tramos.Count == 0)
                return 0;

            var tramo = tramos
                .OrderBy(t => t.Orden)
                .FirstOrDefault(t =>
                    salarioBrutoMensual >= t.Desde &&
                    (!t.Hasta.HasValue || salarioBrutoMensual <= t.Hasta.Value));

            if (tramo == null) return 0;

            var excedente = salarioBrutoMensual - tramo.Desde;
            return Math.Round(tramo.MontoFijo + (excedente * (tramo.Porcentaje / 100m)), 2);
        }

        /// <summary>
        /// Calcula el ISR a retener en el mes actual, usando el método ACUMULATIVO ANUAL.
        /// Según el SAR de Honduras:
        ///   - Se acumula el salario bruto del año
        ///   - Si el acumulado supera el tope anual exento, se cobra sobre el excedente
        ///   - Se resta lo ya retenido en meses anteriores
        /// </summary>
        public static decimal CalcularISRAcumulativo(
            ApplicationDbContext context,
            int empleadoId,
            decimal salarioBrutoMes,
            int anio,
            int mesActual,
            ConfiguracionDeduccion config)
        {
            if (!config.AplicaISR)
                return 0;

            // 1. Calcular acumulado del año ANTES del mes actual
            var inicioAnio = new DateTime(anio, 1, 1);
            var inicioMesActual = new DateTime(anio, mesActual, 1);

            var nominasDelAnio = context.Nominas
                .Where(n => n.FechaInicio >= inicioAnio && n.FechaInicio < inicioMesActual)
                .Select(n => n.Id)
                .ToList();

            var detallesAnteriores = context.DetalleNominas
                .Where(d => d.EmpleadoId == empleadoId && nominasDelAnio.Contains(d.NominaId))
                .ToList();

            decimal acumuladoBrutoAntes = detallesAnteriores.Sum(d => d.TotalBruto);
            decimal isrRetenidoAntes = detallesAnteriores.Sum(d => d.DeduccionISR);

            // 2. Acumulado incluyendo el mes actual
            decimal acumuladoBrutoConMes = acumuladoBrutoAntes + salarioBrutoMes;

            // 3. Aplicar tabla ISR sobre el ACUMULADO (para calcular el impuesto total anual)
            var tramos = context.TramosISR
                .Where(t => t.ConfiguracionDeduccionId == config.Id)
                .OrderBy(t => t.Orden)
                .ToList();

            // ISR anual que le corresponde por el acumulado
            decimal isrAnualCalculado = CalcularISRMensual(acumuladoBrutoConMes, tramos);

            // 4. Retención del mes = ISR anual calculado - ISR ya retenido
            decimal retencionMes = isrAnualCalculado - isrRetenidoAntes;

            // Si es negativa, no devolvemos, retenemos 0
            if (retencionMes < 0) retencionMes = 0;

            return Math.Round(retencionMes, 2);
        }

        // ============================================================
        // IHSS Y RAP
        // ============================================================

        public static decimal CalcularIHSS(decimal salarioBase, ConfiguracionDeduccion config)
        {
            if (!config.AplicaIHSS) return 0;
            var baseCalculo = Math.Min(salarioBase, config.TopeIHSS);
            return Math.Round(baseCalculo * (config.PorcentajeIHSS / 100m), 2);
        }

        public static decimal CalcularRAP(decimal salarioBase, ConfiguracionDeduccion config)
        {
            if (!config.AplicaRAP) return 0;

            var baseCalculo = config.TopeRAP.HasValue
                ? Math.Min(salarioBase, config.TopeRAP.Value)
                : salarioBase;

            return Math.Round(baseCalculo * (config.PorcentajeRAP / 100m), 2);
        }

        // ============================================================
        // SALARIO PROPORCIONAL Y PERÍODOS
        // ============================================================

        public static decimal CalcularSalarioProporcional(decimal salarioBase, decimal diasTrabajados, decimal diasPeriodo)
        {
            if (diasPeriodo <= 0) return 0;
            return Math.Round((salarioBase / diasPeriodo) * diasTrabajados, 2);
        }

        public static (DateTime inicio, DateTime fin, DateTime pago) CalcularPeriodo(string frecuencia, DateTime referencia)
        {
            var hoy = referencia.Date;

            return frecuencia switch
            {
                "Semanal" => (
                    hoy.AddDays(-(int)hoy.DayOfWeek + (hoy.DayOfWeek == DayOfWeek.Sunday ? -6 : 1)),
                    hoy.AddDays(-(int)hoy.DayOfWeek + (hoy.DayOfWeek == DayOfWeek.Sunday ? 0 : 7)),
                    hoy.AddDays(-(int)hoy.DayOfWeek + (hoy.DayOfWeek == DayOfWeek.Sunday ? 0 : 7)).AddDays(4)
                ),
                "Catorcenal" => (hoy.AddDays(-13), hoy, hoy.AddDays(2)),
                "Quincenal" => hoy.Day <= 15
                    ? (new DateTime(hoy.Year, hoy.Month, 1), new DateTime(hoy.Year, hoy.Month, 15), new DateTime(hoy.Year, hoy.Month, 15))
                    : (new DateTime(hoy.Year, hoy.Month, 16), new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month)), new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month))),
                _ => (new DateTime(hoy.Year, hoy.Month, 1), new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month)), new DateTime(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month)))
            };
        }

        public static int DiasPeriodo(string frecuencia)
        {
            return frecuencia switch
            {
                "Semanal" => 7,
                "Catorcenal" => 14,
                "Quincenal" => 15,
                _ => 30
            };
        }

        // ============================================================
        // VALES
        // ============================================================

        public static List<ValeEmpleado> ObtenerValesActivos(ApplicationDbContext context, int empleadoId)
        {
            return context.ValesEmpleado
                .Where(v => v.EmpleadoId == empleadoId
                         && v.SaldoPendiente > 0
                         && (v.Estado == "Entregado" || v.Estado == "Descontado"))
                .OrderBy(v => v.FechaEntrega)
                .ToList();
        }

        public static (decimal totalDescuento, List<ValeEmpleado> valesAfectados) CalcularDescuentoVales(
            ApplicationDbContext context,
            int empleadoId)
        {
            var vales = ObtenerValesActivos(context, empleadoId);
            decimal total = 0;
            var afectados = new List<ValeEmpleado>();

            foreach (var vale in vales)
            {
                var montoDescuento = Math.Min(vale.MontoCuota, vale.SaldoPendiente);
                if (montoDescuento > 0)
                {
                    total += montoDescuento;
                    afectados.Add(vale);
                }
            }

            return (total, afectados);
        }

        // ============================================================
        // CÁLCULO COMPLETO DEL DETALLE DE UN EMPLEADO
        // ============================================================

        public static DetalleNomina CalcularDetalleEmpleado(
            ApplicationDbContext context,
            Empleado empleado,
            string tipoNomina,
            int diasPeriodo,
            DateTime fechaInicio,
            DateTime fechaFin)
        {
            var config = ObtenerConfiguracion(context, fechaInicio.Year);

            // ===== DEVENGADO =====
            var salarioBaseProporcional = CalcularSalarioProporcional(empleado.SalarioBase, diasPeriodo, 30);
            var bonoTransporte = (empleado.BonoTransporte ?? 0) * (diasPeriodo / 30m);
            var bonoAlimentacion = (empleado.BonoAlimentacion ?? 0) * (diasPeriodo / 30m);
            var otrosBonos = (empleado.OtrosBonos ?? 0) * (diasPeriodo / 30m);

            var totalBruto = Math.Round(salarioBaseProporcional + bonoTransporte + bonoAlimentacion + otrosBonos, 2);

            // ===== IHSS =====
            decimal deduccionIHSS = 0;
            if (empleado.CotizaIHSS)
            {
                var ihssMensual = CalcularIHSS(empleado.SalarioBase, config);
                deduccionIHSS = Math.Round(ihssMensual * (diasPeriodo / 30m), 2);
            }

            // ===== RAP =====
            decimal deduccionRAP = 0;
            if (empleado.CotizaRAP)
            {
                var rapMensual = CalcularRAP(empleado.SalarioBase, config);
                deduccionRAP = Math.Round(rapMensual * (diasPeriodo / 30m), 2);
            }

            // ===== ISR =====
            decimal deduccionISR = 0;
            if (empleado.AplicaISR && config.AplicaISR)
            {
                if (config.MetodoISR == "Acumulativo")
                {
                    deduccionISR = CalcularISRAcumulativo(
                        context,
                        empleado.Id,
                        totalBruto,
                        fechaInicio.Year,
                        fechaInicio.Month,
                        config);
                }
                else
                {
                    // Método mensual simple
                    var tramos = context.TramosISR
                        .Where(t => t.ConfiguracionDeduccionId == config.Id)
                        .OrderBy(t => t.Orden)
                        .ToList();

                    var isrMensualCompleto = CalcularISRMensual(empleado.SalarioBase, tramos);
                    deduccionISR = Math.Round(isrMensualCompleto * (diasPeriodo / 30m), 2);
                }
            }

            // ===== VALES =====
            var (deduccionVales, _) = CalcularDescuentoVales(context, empleado.Id);

            // ===== OTRAS DEDUCCIONES =====
            decimal otrasDeducciones = 0;

            var totalDeducciones = Math.Round(deduccionIHSS + deduccionRAP + deduccionISR + deduccionVales + otrasDeducciones, 2);
            var salarioNeto = Math.Round(totalBruto - totalDeducciones, 2);
            if (salarioNeto < 0) salarioNeto = 0;

            return new DetalleNomina
            {
                EmpleadoId = empleado.Id,
                SalarioBase = Math.Round(salarioBaseProporcional, 2),
                BonoTransporte = Math.Round(bonoTransporte, 2),
                BonoAlimentacion = Math.Round(bonoAlimentacion, 2),
                OtrosBonos = Math.Round(otrosBonos, 2),
                HorasExtra = 0,
                MontoHorasExtra = 0,
                Comisiones = 0,
                TotalBruto = totalBruto,

                DeduccionIHSS = deduccionIHSS,
                DeduccionRAP = deduccionRAP,
                DeduccionISR = deduccionISR,
                DeduccionVales = deduccionVales,
                OtrasDeducciones = otrasDeducciones,
                TotalDeducciones = totalDeducciones,

                SalarioNeto = salarioNeto,

                DiasTrabajados = diasPeriodo,
                DiasAusencia = 0,
                DiasVacaciones = 0,
                DiasPermiso = 0
            };
        }
    }
}