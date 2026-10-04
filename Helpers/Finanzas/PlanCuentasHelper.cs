using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Gestiona el Plan de Cuentas contable: seed inicial y utilidades.
    /// </summary>
    public static class PlanCuentasHelper
    {
        /// <summary>
        /// Seed del plan de cuentas estándar de Honduras.
        /// Es idempotente: no duplica si ya existen.
        /// </summary>
        public static void Seed(ApplicationDbContext context)
        {
            var cuentasEstandar = new List<(string Codigo, string Nombre, string Tipo, string CodigoPadre, bool EsMovimiento)>
            {
                // ===== ACTIVO =====
                ("1", "ACTIVO", "Activo", null!, false),
                ("1.1", "Activo Corriente", "Activo", "1", false),
                ("1.1.01", "Caja y Bancos", "Activo", "1.1", false),
                ("1.1.01.001", "Caja General", "Activo", "1.1.01", true),
                ("1.1.01.002", "Caja Chica", "Activo", "1.1.01", true),
                ("1.1.01.003", "Caja POS", "Activo", "1.1.01", true),
                ("1.1.01.004", "Bancos", "Activo", "1.1.01", true),
                ("1.1.02", "Cuentas por Cobrar", "Activo", "1.1", false),
                ("1.1.02.001", "Clientes", "Activo", "1.1.02", true),
                ("1.1.02.002", "Documentos por Cobrar", "Activo", "1.1.02", true),
                ("1.1.03", "Inventarios", "Activo", "1.1", false),
                ("1.1.03.001", "Mercadería para la Venta", "Activo", "1.1.03", true),
                ("1.1.03.002", "Materia Prima", "Activo", "1.1.03", true),
                ("1.1.04", "Pagos Anticipados", "Activo", "1.1", true),
                ("1.2", "Activo No Corriente", "Activo", "1", false),
                ("1.2.01", "Propiedad, Planta y Equipo", "Activo", "1.2", false),
                ("1.2.01.001", "Mobiliario y Equipo", "Activo", "1.2.01", true),
                ("1.2.01.002", "Equipo de Cómputo", "Activo", "1.2.01", true),
                ("1.2.01.003", "Vehículos", "Activo", "1.2.01", true),
                ("1.2.01.004", "Maquinaria", "Activo", "1.2.01", true),
                ("1.2.02", "Depreciación Acumulada", "Activo", "1.2", false),
                ("1.2.02.001", "Depreciación Mobiliario", "Activo", "1.2.02", true),
                ("1.2.02.002", "Depreciación Equipo", "Activo", "1.2.02", true),
                ("1.2.03", "Activos Intangibles", "Activo", "1.2", true),

                // ===== PASIVO =====
                ("2", "PASIVO", "Pasivo", null!, false),
                ("2.1", "Pasivo Corriente", "Pasivo", "2", false),
                ("2.1.01", "Cuentas por Pagar", "Pasivo", "2.1", false),
                ("2.1.01.001", "Proveedores", "Pasivo", "2.1.01", true),
                ("2.1.01.002", "Documentos por Pagar", "Pasivo", "2.1.01", true),
                ("2.1.02", "Impuestos por Pagar", "Pasivo", "2.1", false),
                ("2.1.02.001", "ISV por Pagar", "Pasivo", "2.1.02", true),
                ("2.1.02.002", "ISR por Pagar", "Pasivo", "2.1.02", true),
                ("2.1.02.003", "IHSS por Pagar", "Pasivo", "2.1.02", true),
                ("2.1.02.004", "RAP por Pagar", "Pasivo", "2.1.02", true),
                ("2.1.03", "Sueldos y Salarios por Pagar", "Pasivo", "2.1", true),
                ("2.1.04", "Retenciones", "Pasivo", "2.1", true),
                ("2.1.05", "Préstamos a Corto Plazo", "Pasivo", "2.1", true),
                ("2.2", "Pasivo No Corriente", "Pasivo", "2", false),
                ("2.2.01", "Préstamos a Largo Plazo", "Pasivo", "2.2", true),
                ("2.2.02", "Provisiones Laborales", "Pasivo", "2.2", true),

                // ===== PATRIMONIO =====
                ("3", "PATRIMONIO", "Patrimonio", null!, false),
                ("3.1", "Capital", "Patrimonio", "3", false),
                ("3.1.01", "Capital Social", "Patrimonio", "3.1", true),
                ("3.1.02", "Aportes de Socios", "Patrimonio", "3.1", true),
                ("3.2", "Reservas y Resultados", "Patrimonio", "3", false),
                ("3.2.01", "Reserva Legal", "Patrimonio", "3.2", true),
                ("3.2.02", "Utilidades Retenidas", "Patrimonio", "3.2", true),
                ("3.2.03", "Pérdidas Acumuladas", "Patrimonio", "3.2", true),
                ("3.2.04", "Resultado del Ejercicio", "Patrimonio", "3.2", true),

                // ===== INGRESOS =====
                ("4", "INGRESOS", "Ingreso", null!, false),
                ("4.1", "Ingresos Operativos", "Ingreso", "4", false),
                ("4.1.01", "Ventas de Mercadería", "Ingreso", "4.1", true),
                ("4.1.02", "Ventas de Servicios", "Ingreso", "4.1", true),
                ("4.1.03", "Devoluciones en Ventas", "Ingreso", "4.1", true),
                ("4.1.04", "Descuentos en Ventas", "Ingreso", "4.1", true),
                ("4.2", "Ingresos No Operativos", "Ingreso", "4", false),
                ("4.2.01", "Intereses Ganados", "Ingreso", "4.2", true),
                ("4.2.02", "Otros Ingresos", "Ingreso", "4.2", true),

                // ===== COSTOS =====
                ("5", "COSTOS", "Costo", null!, false),
                ("5.1", "Costo de Ventas", "Costo", "5", false),
                ("5.1.01", "Costo de Mercadería Vendida", "Costo", "5.1", true),
                ("5.1.02", "Costo de Servicios Prestados", "Costo", "5.1", true),

                // ===== GASTOS =====
                ("6", "GASTOS", "Gasto", null!, false),
                ("6.1", "Gastos de Venta", "Gasto", "6", false),
                ("6.1.01", "Publicidad", "Gasto", "6.1", true),
                ("6.1.02", "Comisiones", "Gasto", "6.1", true),
                ("6.2", "Gastos de Administración", "Gasto", "6", false),
                ("6.2.01", "Sueldos y Salarios", "Gasto", "6.2", true),
                ("6.2.02", "Prestaciones Laborales", "Gasto", "6.2", true),
                ("6.2.03", "Alquileres", "Gasto", "6.2", true),
                ("6.2.04", "Servicios Básicos", "Gasto", "6.2", true),
                ("6.2.05", "Honorarios Profesionales", "Gasto", "6.2", true),
                ("6.2.06", "Mantenimiento", "Gasto", "6.2", true),
                ("6.2.07", "Papelería y Útiles", "Gasto", "6.2", true),
                ("6.2.08", "Depreciaciones", "Gasto", "6.2", true),
                ("6.2.09", "Servicios Bancarios", "Gasto", "6.2", true),
                ("6.2.10", "Impuestos y Tasas", "Gasto", "6.2", true),
                ("6.2.11", "Gastos no Deducibles", "Gasto", "6.2", true),
                ("6.3", "Otros Gastos", "Gasto", "6", false),
                ("6.3.01", "Gastos Financieros", "Gasto", "6.3", true),
                ("6.3.02", "Gastos Diversos", "Gasto", "6.3", true),
            };

            foreach (var (codigo, nombre, tipo, codigoPadre, esMovimiento) in cuentasEstandar)
            {
                if (context.PlanCuentas.Any(c => c.Codigo == codigo)) continue;

                var naturaleza = (tipo == "Activo" || tipo == "Costo" || tipo == "Gasto")
                    ? "Deudora"
                    : "Acreedora";

                var nivel = codigo.Split('.').Length;

                context.PlanCuentas.Add(new PlanCuenta
                {
                    Codigo = codigo,
                    Nombre = nombre,
                    Tipo = tipo,
                    CodigoPadre = codigoPadre,
                    Nivel = nivel,
                    Naturaleza = naturaleza,
                    EsMovimiento = esMovimiento,
                    Activa = true,
                    FechaCreacion = DateTime.Now,
                    EmpresaId = 1
                });
            }

            context.SaveChanges();
        }

        /// <summary>
        /// Obtiene todas las cuentas activas ordenadas jerárquicamente.
        /// </summary>
        public static List<PlanCuenta> ObtenerTodas(ApplicationDbContext context)
        {
            return context.PlanCuentas
                .Where(c => c.Activa)
                .OrderBy(c => c.Codigo)
                .ToList();
        }

        /// <summary>
        /// Obtiene solo las cuentas que permiten movimientos (hojas del árbol).
        /// </summary>
        public static List<PlanCuenta> ObtenerCuentasMovimiento(ApplicationDbContext context)
        {
            return context.PlanCuentas
                .Where(c => c.Activa && c.EsMovimiento)
                .OrderBy(c => c.Codigo)
                .ToList();
        }

        /// <summary>
        /// Recalcula el nivel y la naturaleza de todas las cuentas basado en su código.
        /// </summary>
        public static int RecalcularJerarquia(ApplicationDbContext context)
        {
            var cuentas = context.PlanCuentas.ToList();
            int cambios = 0;

            foreach (var cuenta in cuentas)
            {
                var nivel = cuenta.Codigo.Split('.').Length;
                var naturaleza = (cuenta.Tipo == "Activo" || cuenta.Tipo == "Costo" || cuenta.Tipo == "Gasto")
                    ? "Deudora"
                    : "Acreedora";

                if (cuenta.Nivel != nivel) { cuenta.Nivel = nivel; cambios++; }
                if (cuenta.Naturaleza != naturaleza) { cuenta.Naturaleza = naturaleza; cambios++; }
            }

            if (cambios > 0) context.SaveChanges();
            return cambios;
        }
    }
}