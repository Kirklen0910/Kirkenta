using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.RRHH
{
    public static class NominaHelper
    {
        /// <summary>
        /// Deducción anual de gastos médicos sin comprobante (Ley ISR, L. 40,000). El SAR publica la tabla
        /// anual sobre la renta neta; la tabla mensual que usa la nómina es (anual + esta deducción) / 12.
        /// </summary>
        public const decimal DeduccionGastosMedicosAnual = 40000m;

        // ============================================================
        // CONFIGURACIÓN DE DEDUCCIONES POR AÑO
        // ============================================================

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
            // Si ya hay un año anterior configurado, se parte de ese (con sus tasas, topes y tramos ya corregidos)
            var anterior = context.ConfiguracionDeducciones.Where(c => c.Anio < anio).OrderByDescending(c => c.Anio).FirstOrDefault();
            if (anterior != null)
                return Kirkenta.Pages.RRHH.Deducciones.IndexModel.CopiarConfiguracion(context, anterior, anio);

            var config = new ConfiguracionDeduccion
            {
                Anio = anio,
                AplicaIHSS = true,
                PorcentajeIHSS = 5.0m,      // 2.5% EM + 2.5% IVM
                TopeIHSS = 11903.13m,       // techo IHSS 2026 (también piso del RAP)
                AplicaRAP = true,
                PorcentajeRAP = 1.5m,
                AplicaISR = true,
                TopeAnualExentoISR = 228324.32m,   // SAR 2026 → L. 22,360.36 mensuales
                MetodoISR = "Acumulativo",
                Activa = true,
                FechaCreacion = DateTime.Now,
                Notas = "Configuración por defecto (Honduras)"
            };

            context.ConfiguracionDeducciones.Add(config);
            context.SaveChanges();

            // Tramos ISR MENSUALES 2026 (comunicado SAR 02-2026). Son la tabla anual más la deducción de
            // L. 40,000 de gastos médicos, divididas entre 12; por eso se aplican directo sobre el bruto mensual:
            //   Hasta L. 22,360.36 → exento
            //   L. 22,360.37 a L. 32,346.18 → 15% sobre excedente
            //   L. 32,346.19 a L. 70,805.06 → L. 1,497.87 + 20% sobre excedente
            //   Más de L. 70,805.06 → L. 9,189.65 + 25% sobre excedente
            var tramos = new List<TramoISR>
            {
                new() { ConfiguracionDeduccionId = config.Id, Orden = 1, Desde = 0, Hasta = 22360.36m, Porcentaje = 0, MontoFijo = 0, Descripcion = "Exento" },
                new() { ConfiguracionDeduccionId = config.Id, Orden = 2, Desde = 22360.37m, Hasta = 32346.18m, Porcentaje = 15, MontoFijo = 0, Descripcion = "15% sobre excedente" },
                new() { ConfiguracionDeduccionId = config.Id, Orden = 3, Desde = 32346.19m, Hasta = 70805.06m, Porcentaje = 20, MontoFijo = 1497.87m, Descripcion = "L. 1,497.87 + 20% sobre excedente" },
                new() { ConfiguracionDeduccionId = config.Id, Orden = 4, Desde = 70805.07m, Hasta = null, Porcentaje = 25, MontoFijo = 9189.65m, Descripcion = "L. 9,189.65 + 25% sobre excedente" }
            };

            context.TramosISR.AddRange(tramos);
            context.SaveChanges();

            return config;
        }

        // ============================================================
        // CÁLCULO DE ISR
        // ============================================================

        /// <summary>
        /// Calcula el ISR MENSUAL sobre una base MENSUAL usando la tabla de tramos mensuales.
        /// </summary>
        public static decimal CalcularISRMensual(decimal salarioBrutoMensual, List<TramoISR> tramos)
        {
            if (tramos == null || tramos.Count == 0)
                return 0;

            // Tramo con el "Desde" más alto que no supere el salario. Así un salario que cae entre dos tramos
            // (por ejemplo 15,692.255 entre "hasta 15,692.25" y "desde 15,692.26") no se queda sin ISR.
            var tramo = tramos
                .Where(t => salarioBrutoMensual >= t.Desde)
                .OrderByDescending(t => t.Desde)
                .FirstOrDefault();

            if (tramo == null) return 0;

            // El excedente se mide desde el "Hasta" del tramo anterior (22,360.36), no desde el "Desde" del
            // tramo (22,360.37): así no se pierde el centavo de diferencia entre tramos.
            var limiteAnterior = tramos
                .Where(t => t.Hasta.HasValue && t.Hasta.Value < tramo.Desde)
                .Select(t => (decimal?)t.Hasta!.Value)
                .Max();
            var excedente = salarioBrutoMensual - (limiteAnterior ?? tramo.Desde);
            return Math.Round(tramo.MontoFijo + (excedente * (tramo.Porcentaje / 100m)), 2);
        }

        /// <summary>
        /// Calcula el ISR a retener en el mes actual, usando el método ACUMULATIVO ANUAL.
        /// 🔒 FIX: La tabla ISR es MENSUAL. Para comparar contra el acumulado anual
        /// se debe convertir el acumulado a un "promedio mensual" antes de aplicar la tabla,
        /// o bien anualizar los tramos. Usamos el enfoque SAR: calcular el ISR anual
        /// aplicando la tabla mensual sobre el promedio mensual del acumulado, y luego
        /// restar lo ya retenido.
        ///
        /// Fórmula SAR:
        ///   ISR_anual_proyectado = ISR_mensual(promedio_mensual) × meses_trabajados
        ///   ISR_mes = ISR_anual_proyectado − ISR_retenido_antes
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

            var inicioAnio = new DateTime(anio, 1, 1);
            var inicioMesActual = new DateTime(anio, mesActual, 1);

            // Solo cuentan nóminas vigentes: una nómina anulada/rechazada NO retuvo ISR de verdad
            var nominasDelAnio = context.Nominas
                .Where(n => n.FechaInicio >= inicioAnio && n.FechaInicio < inicioMesActual
                         && n.Estado != "Anulada")
                .Select(n => new { n.Id, n.FechaInicio })
                .ToList();
            var idsNominas = nominasDelAnio.Select(n => n.Id).ToList();

            var detallesAnteriores = context.DetalleNominas
                .Where(d => d.EmpleadoId == empleadoId && idsNominas.Contains(d.NominaId))
                .ToList();

            decimal acumuladoBrutoAntes = detallesAnteriores.Sum(d => d.TotalBruto);
            decimal isrRetenidoAntes = detallesAnteriores.Sum(d => d.DeduccionISR);

            // Meses realmente pagados a este empleado (más el actual): si entró a mitad de año
            // no se diluye el promedio con meses en los que no trabajaba.
            var mesesConPago = detallesAnteriores
                .Select(d => nominasDelAnio.First(n => n.Id == d.NominaId).FechaInicio.Month)
                .Distinct()
                .Count();
            int mesesProcesados = mesesConPago + 1;

            decimal acumuladoBrutoConMes = acumuladoBrutoAntes + salarioBrutoMes;

            // Promedio mensual del acumulado (base para aplicar la tabla mensual)
            decimal promedioMensual = acumuladoBrutoConMes / mesesProcesados;

            var tramos = context.TramosISR
                .Where(t => t.ConfiguracionDeduccionId == config.Id)
                .OrderBy(t => t.Orden)
                .ToList();

            // ISR mensual teórico según la tabla
            decimal isrMensualTeorico = CalcularISRMensual(promedioMensual, tramos);

            // ISR anual proyectado = mensual × meses procesados
            decimal isrAnualProyectado = isrMensualTeorico * mesesProcesados;

            // Retención del mes = ISR proyectado − ISR ya retenido
            decimal retencionMes = isrAnualProyectado - isrRetenidoAntes;

            if (retencionMes < 0) retencionMes = 0;

            return Math.Round(retencionMes, 2);
        }

        // ============================================================
        // IHSS Y RAP
        // ============================================================

        /// <summary>
        /// IHSS del trabajador: PorcentajeIHSS (2.5% Enfermedad y Maternidad + 2.5% IVM = 5%)
        /// sobre el salario mensual, topado al techo de cotización (L. 11,903.13 en 2026).
        /// </summary>
        public static decimal CalcularIHSS(decimal salarioMensual, ConfiguracionDeduccion config)
        {
            if (!config.AplicaIHSS) return 0;
            var baseCalculo = Math.Min(salarioMensual, config.TopeIHSS);
            return Math.Round(baseCalculo * (config.PorcentajeIHSS / 100m), 2);
        }

        /// <summary>
        /// RAP / FOVIIF del trabajador (Decreto 47-2024): PorcentajeRAP (1.5%) solo sobre la parte del
        /// salario que EXCEDE el techo IVM del IHSS (TopeIHSS, que funciona como piso del RAP).
        /// Un salario igual o menor al techo del IHSS no paga RAP. TopeRAP, si se define, limita la base.
        /// </summary>
        public static decimal CalcularRAP(decimal salarioMensual, ConfiguracionDeduccion config)
        {
            if (!config.AplicaRAP) return 0;

            var salarioTopado = config.TopeRAP.HasValue
                ? Math.Min(salarioMensual, config.TopeRAP.Value)
                : salarioMensual;
            var excedente = Math.Max(0m, salarioTopado - config.TopeIHSS);

            return Math.Round(excedente * (config.PorcentajeRAP / 100m), 2);
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
            // Se calcula sobre el salario base mensual (los bonos de transporte/alimentación no cotizan),
            // se topa al techo y se prorratea al período.
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
                // El método acumulativo trabaja con meses completos; para nóminas semanales,
                // catorcenales o quincenales se usa el equivalente mensual prorrateado.
                if (config.MetodoISR == "Acumulativo" && diasPeriodo == 30)
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
                    var tramos = context.TramosISR
                        .Where(t => t.ConfiguracionDeduccionId == config.Id)
                        .OrderBy(t => t.Orden)
                        .ToList();

                    // Base: bruto mensual equivalente (el salario más bonos), no solo el salario base
                    var brutoMensualEquivalente = totalBruto * 30m / Math.Max(diasPeriodo, 1);
                    var isrMensualCompleto = CalcularISRMensual(brutoMensualEquivalente, tramos);
                    deduccionISR = Math.Round(isrMensualCompleto * (diasPeriodo / 30m), 2);
                }
            }

            // ===== VALES =====
            var (deduccionVales, _) = CalcularDescuentoVales(context, empleado.Id);

            // Los vales no pueden descontar más de lo que queda después de las deducciones de ley
            var disponibleParaVales = Math.Max(0m, totalBruto - deduccionIHSS - deduccionRAP - deduccionISR);
            if (deduccionVales > disponibleParaVales) deduccionVales = disponibleParaVales;

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