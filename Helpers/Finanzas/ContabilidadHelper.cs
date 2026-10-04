using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Genera reportes contables (Estado de Resultados y Balance General)
    /// agrupando MovimientoFinanciero por la cuenta del Plan de Cuentas
    /// asociada a su CategoriaFinanciera.
    /// </summary>
    public static class ContabilidadHelper
    {
        // ============================================================
        // DTO DE SALIDA
        // ============================================================

        /// <summary>
        /// Línea de reporte contable: agrupa los movimientos por cuenta.
        /// </summary>
        public class LineaReporte
        {
            public int? PlanCuentaId { get; set; }
            public string Codigo { get; set; } = "";
            public string Cuenta { get; set; } = "";
            public string Tipo { get; set; } = "";
            public string Naturaleza { get; set; } = "";
            public int Nivel { get; set; }
            public decimal Monto { get; set; }
            public int CantidadMovimientos { get; set; }
        }

        /// <summary>
        /// Resultado completo del Estado de Resultados.
        /// </summary>
        public class EstadoResultados
        {
            public DateTime Desde { get; set; }
            public DateTime Hasta { get; set; }
            public List<LineaReporte> Ingresos { get; set; } = new();
            public List<LineaReporte> Costos { get; set; } = new();
            public List<LineaReporte> Gastos { get; set; } = new();
            public decimal TotalIngresos { get; set; }
            public decimal TotalCostos { get; set; }
            public decimal TotalGastos { get; set; }
            public decimal UtilidadBruta { get; set; }
            public decimal UtilidadOperativa { get; set; }
            public decimal UtilidadNeta { get; set; }
            public decimal MargenBruto { get; set; }
            public decimal MargenNeto { get; set; }
        }

        /// <summary>
        /// Resultado completo del Balance General.
        /// </summary>
        public class BalanceGeneral
        {
            public DateTime FechaCorte { get; set; }
            public List<LineaReporte> Activos { get; set; } = new();
            public List<LineaReporte> Pasivos { get; set; } = new();
            public List<LineaReporte> Patrimonio { get; set; } = new();
            public decimal TotalActivos { get; set; }
            public decimal TotalPasivos { get; set; }
            public decimal TotalPatrimonio { get; set; }
            public decimal TotalPasivoPatrimonio { get; set; }
            public decimal Diferencia { get; set; }
            public decimal ResultadoEjercicio { get; set; }
            public bool Cuadra => Math.Abs(Diferencia) < 1.00m;
        }

        // ============================================================
        // ESTADO DE RESULTADOS
        // ============================================================

        /// <summary>
        /// Genera el Estado de Resultados para un período.
        /// Fórmulas:
        ///   Utilidad Bruta = Ingresos - Costos
        ///   Utilidad Operativa = Utilidad Bruta - Gastos
        ///   Utilidad Neta = Utilidad Operativa (v1: sin otros ingresos/egresos)
        /// </summary>
        public static EstadoResultados GenerarEstadoResultados(
            ApplicationDbContext context,
            DateTime desde,
            DateTime hasta)
        {
            var resultado = new EstadoResultados
            {
                Desde = desde.Date,
                Hasta = hasta.Date
            };

            var desdeFull = desde.Date;
            var hastaFull = hasta.Date.AddDays(1).AddSeconds(-1);

            // Traer solo movimientos activos en el rango, con su categoría y plan
            var movimientos = context.MovimientosFinancieros
                .Where(m => m.Estado == "Activo"
                         && m.Fecha >= desdeFull
                         && m.Fecha <= hastaFull)
                .Select(m => new
                {
                    m.Id,
                    m.Tipo,
                    m.Monto,
                    m.CategoriaId,
                    PlanCuentaId = context.CategoriasFinancieras
                        .Where(c => c.Id == m.CategoriaId)
                        .Select(c => c.PlanCuentaId)
                        .FirstOrDefault(),
                    PlanCodigo = context.CategoriasFinancieras
                        .Where(c => c.Id == m.CategoriaId)
                        .Select(c => context.PlanCuentas
                            .Where(p => p.Id == c.PlanCuentaId)
                            .Select(p => p.Codigo)
                            .FirstOrDefault())
                        .FirstOrDefault(),
                    PlanNombre = context.CategoriasFinancieras
                        .Where(c => c.Id == m.CategoriaId)
                        .Select(c => context.PlanCuentas
                            .Where(p => p.Id == c.PlanCuentaId)
                            .Select(p => p.Nombre)
                            .FirstOrDefault())
                        .FirstOrDefault(),
                    PlanTipo = context.CategoriasFinancieras
                        .Where(c => c.Id == m.CategoriaId)
                        .Select(c => context.PlanCuentas
                            .Where(p => p.Id == c.PlanCuentaId)
                            .Select(p => p.Tipo)
                            .FirstOrDefault())
                        .FirstOrDefault(),
                    PlanNaturaleza = context.CategoriasFinancieras
                        .Where(c => c.Id == m.CategoriaId)
                        .Select(c => context.PlanCuentas
                            .Where(p => p.Id == c.PlanCuentaId)
                            .Select(p => p.Naturaleza)
                            .FirstOrDefault())
                        .FirstOrDefault(),
                    PlanNivel = context.CategoriasFinancieras
                        .Where(c => c.Id == m.CategoriaId)
                        .Select(c => context.PlanCuentas
                            .Where(p => p.Id == c.PlanCuentaId)
                            .Select(p => p.Nivel)
                            .FirstOrDefault())
                        .FirstOrDefault(),
                    CategoriaNombre = context.CategoriasFinancieras
                        .Where(c => c.Id == m.CategoriaId)
                        .Select(c => c.Nombre)
                        .FirstOrDefault()
                })
                .ToList();

            // Agrupar por categoría → plan
            var agrupado = movimientos
                .Where(m => m.PlanCuentaId != null)
                .GroupBy(m => new
                {
                    PlanId = m.PlanCuentaId!.Value,
                    Codigo = m.PlanCodigo ?? "",
                    Nombre = m.PlanNombre ?? "",
                    Tipo = m.PlanTipo ?? "",
                    Naturaleza = m.PlanNaturaleza ?? "",
                    Nivel = m.PlanNivel
                })
                .Select(g => new LineaReporte
                {
                    PlanCuentaId = g.Key.PlanId,
                    Codigo = g.Key.Codigo,
                    Cuenta = g.Key.Nombre,
                    Tipo = g.Key.Tipo,
                    Naturaleza = g.Key.Naturaleza,
                    Nivel = g.Key.Nivel,
                    Monto = g.Sum(x => x.Monto),
                    CantidadMovimientos = g.Count()
                })
                .ToList();

            // Separar por tipo contable
            resultado.Ingresos = agrupado
                .Where(l => l.Tipo == "Ingreso")
                .OrderBy(l => l.Codigo)
                .ToList();

            resultado.Costos = agrupado
                .Where(l => l.Tipo == "Costo")
                .OrderBy(l => l.Codigo)
                .ToList();

            resultado.Gastos = agrupado
                .Where(l => l.Tipo == "Gasto")
                .OrderBy(l => l.Codigo)
                .ToList();

            resultado.TotalIngresos = resultado.Ingresos.Sum(l => l.Monto);
            resultado.TotalCostos = resultado.Costos.Sum(l => l.Monto);
            resultado.TotalGastos = resultado.Gastos.Sum(l => l.Monto);

            resultado.UtilidadBruta = resultado.TotalIngresos - resultado.TotalCostos;
            resultado.UtilidadOperativa = resultado.UtilidadBruta - resultado.TotalGastos;
            resultado.UtilidadNeta = resultado.UtilidadOperativa;

            resultado.MargenBruto = resultado.TotalIngresos > 0
                ? Math.Round(resultado.UtilidadBruta / resultado.TotalIngresos * 100m, 2)
                : 0m;

            resultado.MargenNeto = resultado.TotalIngresos > 0
                ? Math.Round(resultado.UtilidadNeta / resultado.TotalIngresos * 100m, 2)
                : 0m;

            return resultado;
        }

        // ============================================================
        // BALANCE GENERAL
        // ============================================================

        /// <summary>
        /// Genera el Balance General a una fecha de corte.
        /// Trae TODOS los movimientos activos históricos hasta la fecha,
        /// y separa por tipo contable (Activo, Pasivo, Patrimonio).
        /// La utilidad del ejercicio se calcula como diferencia entre
        /// ingresos y gastos del año en curso.
        /// </summary>
        public static BalanceGeneral GenerarBalanceGeneral(
            ApplicationDbContext context,
            DateTime fechaCorte)
        {
            var resultado = new BalanceGeneral
            {
                FechaCorte = fechaCorte.Date
            };

            var fechaFin = fechaCorte.Date.AddDays(1).AddSeconds(-1);

            // Todos los movimientos activos hasta la fecha de corte
            var movimientos = context.MovimientosFinancieros
                .Where(m => m.Estado == "Activo"
                         && m.Fecha <= fechaFin)
                .Select(m => new
                {
                    m.Id,
                    m.Tipo,
                    m.Monto,
                    m.CategoriaId,
                    PlanCuentaId = context.CategoriasFinancieras
                        .Where(c => c.Id == m.CategoriaId)
                        .Select(c => c.PlanCuentaId)
                        .FirstOrDefault(),
                    PlanCodigo = context.CategoriasFinancieras
                        .Where(c => c.Id == m.CategoriaId)
                        .Select(c => context.PlanCuentas
                            .Where(p => p.Id == c.PlanCuentaId)
                            .Select(p => p.Codigo)
                            .FirstOrDefault())
                        .FirstOrDefault(),
                    PlanNombre = context.CategoriasFinancieras
                        .Where(c => c.Id == m.CategoriaId)
                        .Select(c => context.PlanCuentas
                            .Where(p => p.Id == c.PlanCuentaId)
                            .Select(p => p.Nombre)
                            .FirstOrDefault())
                        .FirstOrDefault(),
                    PlanTipo = context.CategoriasFinancieras
                        .Where(c => c.Id == m.CategoriaId)
                        .Select(c => context.PlanCuentas
                            .Where(p => p.Id == c.PlanCuentaId)
                            .Select(p => p.Tipo)
                            .FirstOrDefault())
                        .FirstOrDefault(),
                    PlanNaturaleza = context.CategoriasFinancieras
                        .Where(c => c.Id == m.CategoriaId)
                        .Select(c => context.PlanCuentas
                            .Where(p => p.Id == c.PlanCuentaId)
                            .Select(p => p.Naturaleza)
                            .FirstOrDefault())
                        .FirstOrDefault(),
                    PlanNivel = context.CategoriasFinancieras
                        .Where(c => c.Id == m.CategoriaId)
                        .Select(c => context.PlanCuentas
                            .Where(p => p.Id == c.PlanCuentaId)
                            .Select(p => p.Nivel)
                            .FirstOrDefault())
                        .FirstOrDefault()
                })
                .ToList();

            // Agrupar por plan cuenta
            var agrupado = movimientos
                .Where(m => m.PlanCuentaId != null)
                .GroupBy(m => new
                {
                    PlanId = m.PlanCuentaId!.Value,
                    Codigo = m.PlanCodigo ?? "",
                    Nombre = m.PlanNombre ?? "",
                    Tipo = m.PlanTipo ?? "",
                    Naturaleza = m.PlanNaturaleza ?? "",
                    Nivel = m.PlanNivel
                })
                .Select(g => new LineaReporte
                {
                    PlanCuentaId = g.Key.PlanId,
                    Codigo = g.Key.Codigo,
                    Cuenta = g.Key.Nombre,
                    Tipo = g.Key.Tipo,
                    Naturaleza = g.Key.Naturaleza,
                    Nivel = g.Key.Nivel,
                    Monto = g.Sum(x => x.Monto),
                    CantidadMovimientos = g.Count()
                })
                .ToList();

            resultado.Activos = agrupado
                .Where(l => l.Tipo == "Activo")
                .OrderBy(l => l.Codigo)
                .ToList();

            resultado.Pasivos = agrupado
                .Where(l => l.Tipo == "Pasivo")
                .OrderBy(l => l.Codigo)
                .ToList();

            resultado.Patrimonio = agrupado
                .Where(l => l.Tipo == "Patrimonio")
                .OrderBy(l => l.Codigo)
                .ToList();

            resultado.TotalActivos = resultado.Activos.Sum(l => l.Monto);
            resultado.TotalPasivos = resultado.Pasivos.Sum(l => l.Monto);
            resultado.TotalPatrimonio = resultado.Patrimonio.Sum(l => l.Monto);

            // Utilidad del ejercicio = ingresos - costos - gastos acumulados (histórico hasta la fecha)
            var ingresosHistoricos = agrupado.Where(l => l.Tipo == "Ingreso").Sum(l => l.Monto);
            var costosHistoricos = agrupado.Where(l => l.Tipo == "Costo").Sum(l => l.Monto);
            var gastosHistoricos = agrupado.Where(l => l.Tipo == "Gasto").Sum(l => l.Monto);

            resultado.ResultadoEjercicio = ingresosHistoricos - costosHistoricos - gastosHistoricos;

            // Sumar la utilidad del ejercicio al patrimonio para cuadrar
            resultado.TotalPatrimonio += resultado.ResultadoEjercicio;

            resultado.TotalPasivoPatrimonio = resultado.TotalPasivos + resultado.TotalPatrimonio;
            resultado.Diferencia = resultado.TotalActivos - resultado.TotalPasivoPatrimonio;

            return resultado;
        }

        // ============================================================
        // UTILIDADES
        // ============================================================

        /// <summary>
        /// Cuenta cuántos movimientos NO tienen categoría con PlanCuentaId
        /// en un rango. Sirve para mostrar advertencia en el reporte.
        /// </summary>
        public static int ContarSinPlanCuenta(
            ApplicationDbContext context,
            DateTime desde,
            DateTime hasta)
        {
            var desdeFull = desde.Date;
            var hastaFull = hasta.Date.AddDays(1).AddSeconds(-1);

            return context.MovimientosFinancieros
                .Where(m => m.Estado == "Activo"
                         && m.Fecha >= desdeFull
                         && m.Fecha <= hastaFull)
                .Where(m => m.CategoriaId == null
                         || !context.CategoriasFinancieras
                            .Where(c => c.Id == m.CategoriaId)
                            .Any(c => c.PlanCuentaId != null))
                .Count();
        }
    }
}