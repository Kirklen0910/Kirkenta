using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Crea los datos iniciales de Finanzas: cajas, bancos comunes de Honduras,
    /// categorías de ingreso/egreso más usadas, y asocia categorías al plan de cuentas.
    /// Es idempotente: no duplica si ya existen.
    /// </summary>
    public static class FinanzasSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            SeedCuentas(context);
            SeedCategorias(context);
            context.SaveChanges();

            AsociarCategoriasAPlan(context);
            CorregirMapeos(context);
        }

        private static void SeedCuentas(ApplicationDbContext context)
        {
            var cuentas = new List<CuentaFinanciera>
            {
                new() { Codigo = "CAJA-CHICA", Nombre = "Caja Chica", Tipo = "Caja", Subtipo = "Chica", SaldoInicial = 0, SaldoActual = 0, Activa = true },
                new() { Codigo = "CAJA-GRAL", Nombre = "Caja General", Tipo = "Caja", Subtipo = "General", SaldoInicial = 0, SaldoActual = 0, Activa = true },
                new() { Codigo = "CAJA-POS", Nombre = "Caja POS", Tipo = "Caja", Subtipo = "POS", SaldoInicial = 0, SaldoActual = 0, Activa = true },
                new() { Codigo = "CXC", Nombre = "Cuentas por Cobrar", Tipo = "Caja", Subtipo = "CuentasPorCobrar", SaldoInicial = 0, SaldoActual = 0, Activa = true }
            };

            foreach (var cuenta in cuentas)
            {
                if (!context.CuentasFinancieras.Any(c => c.Codigo == cuenta.Codigo))
                {
                    context.CuentasFinancieras.Add(cuenta);
                }
            }
        }

        private static void SeedCategorias(ApplicationDbContext context)
        {
            // Ingresos
            var ingresos = new[]
            {
                ("Ventas al contado", "#10b981"),
                ("Ventas al crédito (cobros)", "#06b6d4"),
                ("Devoluciones de clientes", "#8b5cf6"),
                ("Préstamos recibidos", "#f59e0b"),
                ("Aportes de socios", "#6366f1"),
                ("Intereses ganados", "#14b8a6"),
                ("Apertura de cuenta", "#14b8a6"),
                ("Otros ingresos", "#6b7280"),
            };

            foreach (var (nombre, color) in ingresos)
            {
                if (!context.CategoriasFinancieras.Any(c => c.Tipo == "Ingreso" && c.Nombre == nombre))
                {
                    context.CategoriasFinancieras.Add(new CategoriaFinanciera
                    {
                        Tipo = "Ingreso",
                        Nombre = nombre,
                        Color = color,
                        EsSistema = true,
                        Activa = true
                    });
                }
            }

            // Egresos
            var egresos = new[]
            {
                ("Alquiler", "#ef4444"),
                ("Energía eléctrica", "#f97316"),
                ("Agua potable", "#06b6d4"),
                ("Internet y telefonía", "#3b82f6"),
                ("Combustible", "#f59e0b"),
                ("Mantenimiento y reparaciones", "#8b5cf6"),
                ("Papelería y útiles", "#ec4899"),
                ("Publicidad y marketing", "#a855f7"),
                ("Servicios profesionales", "#14b8a6"),
                ("Nómina", "#dc2626"),
                ("Adelantos/Vales a empleados", "#b91c1c"),
                ("Pago a proveedores", "#7c3aed"),
                ("Impuestos (ISV/IVA)", "#991b1b"),
                ("Servicios bancarios", "#0f766e"),
                ("Fletes y transporte", "#ea580c"),
                ("Fletes de envíos a clientes", "#ea580c"),
                ("Seguros", "#0891b2"),
                ("Préstamos (cuotas)", "#b45309"),
                ("Costo de mercadería vendida", "#b91c1c"),
                ("Cambio entregado", "#f59e0b"),
                ("Otros gastos", "#6b7280"),
            };

            foreach (var (nombre, color) in egresos)
            {
                if (!context.CategoriasFinancieras.Any(c => c.Tipo == "Egreso" && c.Nombre == nombre))
                {
                    context.CategoriasFinancieras.Add(new CategoriaFinanciera
                    {
                        Tipo = "Egreso",
                        Nombre = nombre,
                        Color = color,
                        EsSistema = true,
                        Activa = true
                    });
                }
            }
        }

        /// <summary>
        /// Asocia las categorías financieras del sistema al Plan de Cuentas contable.
        /// Es idempotente: solo actualiza las que tengan PlanCuentaId = null.
        /// </summary>
        private static void AsociarCategoriasAPlan(ApplicationDbContext context)
        {
            // Mapa: nombre de categoría → código de cuenta del plan
            var mapa = new Dictionary<string, (string Tipo, string CodigoPlan)>
            {
                // ===== INGRESOS =====
                { "Ventas al contado",              ("Ingreso", "4.1.01") },
                { "Ventas al crédito (cobros)",     ("Ingreso", "4.1.01") },
                { "Devoluciones de clientes",       ("Ingreso", "4.1.03") },
                { "Préstamos recibidos",            ("Ingreso", "2.1.05") },
                { "Aportes de socios",              ("Ingreso", "3.1.02") },
                { "Intereses ganados",              ("Ingreso", "4.2.01") },
                { "Apertura de cuenta",             ("Ingreso", "1.1.01.001") },
                { "Otros ingresos",                 ("Ingreso", "4.2.02") },

                // ===== EGRESOS: COSTOS =====
                { "Costo de mercadería vendida",    ("Egreso", "5.1.01") },

                // El cambio entregado reduce la venta cobrada (se recibió más de lo que costó)
                { "Cambio entregado",               ("Egreso", "4.1.01") },

                // ===== EGRESOS: GASTOS =====
                { "Alquiler",                        ("Egreso", "6.2.03") },
                { "Energía eléctrica",               ("Egreso", "6.2.04") },
                { "Agua potable",                    ("Egreso", "6.2.04") },
                { "Internet y telefonía",            ("Egreso", "6.2.04") },
                { "Combustible",                     ("Egreso", "6.2.06") },
                { "Mantenimiento y reparaciones",    ("Egreso", "6.2.06") },
                { "Papelería y útiles",              ("Egreso", "6.2.07") },
                { "Publicidad y marketing",          ("Egreso", "6.1.01") },
                { "Servicios profesionales",         ("Egreso", "6.2.05") },
                { "Nómina",                          ("Egreso", "6.2.01") },
                // Un vale es dinero que el empleado devuelve en la nómina: es un activo, no un gasto
                { "Adelantos/Vales a empleados",     ("Egreso", "1.1.02.003") },
                // Lo cobrado al cliente por envío se aplica aquí: paga el flete sin tocar la utilidad
                { "Fletes de envíos a clientes",     ("Egreso", "2.1.99") },
                { "Pago a proveedores",              ("Egreso", "1.1.03.001") },
                { "Impuestos (ISV/IVA)",             ("Egreso", "6.2.10") },
                { "Servicios bancarios",             ("Egreso", "6.2.09") },
                { "Fletes y transporte",             ("Egreso", "6.3.02") },
                { "Seguros",                         ("Egreso", "6.2.10") },
                { "Préstamos (cuotas)",              ("Egreso", "6.3.01") },
                { "Otros gastos",                    ("Egreso", "6.3.02") },
            };

            // Diccionario de plan de cuentas para búsquedas rápidas
            var planDict = context.PlanCuentas
                .ToDictionary(p => p.Codigo, p => p);

            var categoriasSinPlan = context.CategoriasFinancieras
                .Where(c => c.PlanCuentaId == null)
                .ToList();

            bool cambios = false;

            foreach (var categoria in categoriasSinPlan)
            {
                if (!mapa.TryGetValue(categoria.Nombre, out var info)) continue;
                if (info.Tipo != categoria.Tipo) continue; // coherencia de tipo

                if (planDict.TryGetValue(info.CodigoPlan, out var planCuenta))
                {
                    categoria.PlanCuentaId = planCuenta.Id;
                    categoria.CuentaContable = planCuenta.Codigo;
                    cambios = true;
                }
            }

            if (cambios) context.SaveChanges();
        }

        /// <summary>
        /// Corrige mapeos antiguos que quedaron mal en bases ya sembradas.
        /// Solo cambia la categoría si todavía apunta al código incorrecto original
        /// (no pisa personalizaciones del usuario). Es idempotente.
        ///  - "Pago a proveedores": era 6.2.10 (gasto) → ahora 1.1.03.001 (Inventario).
        ///    Como gasto duplicaba el costo (el COGS ya lo descuenta al vender).
        ///  - "Préstamos recibidos": era 4.2.02 (ingreso) → ahora 2.1.05 (Pasivo).
        /// </summary>
        private static void CorregirMapeos(ApplicationDbContext context)
        {
            var correcciones = new (string Tipo, string Nombre, string CodigoActual, string CodigoNuevo)[]
            {
                ("Egreso",  "Pago a proveedores", "6.2.10", "1.1.03.001"),
                ("Ingreso", "Préstamos recibidos", "4.2.02", "2.1.05"),
                ("Egreso",  "Adelantos/Vales a empleados", "6.2.02", "1.1.02.003"),
                // El vuelto no es un gasto: reduce lo que entró por la venta (el ingreso se registra por lo recibido)
                ("Egreso",  "Cambio entregado", "6.3.02", "4.1.01"),
            };

            bool cambios = false;

            foreach (var (tipo, nombre, codigoActual, codigoNuevo) in correcciones)
            {
                var planNuevo = context.PlanCuentas.FirstOrDefault(p => p.Codigo == codigoNuevo);
                if (planNuevo == null) continue;

                var categoria = context.CategoriasFinancieras
                    .FirstOrDefault(c => c.Tipo == tipo && c.Nombre == nombre);
                if (categoria == null || categoria.PlanCuentaId == null) continue;

                var planActual = context.PlanCuentas.FirstOrDefault(p => p.Id == categoria.PlanCuentaId);
                if (planActual == null || planActual.Codigo != codigoActual) continue;

                categoria.PlanCuentaId = planNuevo.Id;
                categoria.CuentaContable = planNuevo.Codigo;
                cambios = true;
            }

            if (cambios) context.SaveChanges();
        }
    }
}