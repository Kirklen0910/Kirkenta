using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Crea los datos iniciales de Finanzas: cajas, bancos comunes de Honduras,
    /// categorías de ingreso/egreso más usadas.
    /// Es idempotente: no duplica si ya existen.
    /// </summary>
    public static class FinanzasSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            SeedCuentas(context);
            SeedCategorias(context);
            context.SaveChanges();
        }

        private static void SeedCuentas(ApplicationDbContext context)
        {
            var cuentas = new List<CuentaFinanciera>
            {
                new() { Codigo = "CAJA-CHICA", Nombre = "Caja Chica", Tipo = "Caja", Subtipo = "Chica", SaldoInicial = 0, SaldoActual = 0, Activa = true },
                new() { Codigo = "CAJA-GRAL", Nombre = "Caja General", Tipo = "Caja", Subtipo = "General", SaldoInicial = 0, SaldoActual = 0, Activa = true },
                new() { Codigo = "CAJA-POS", Nombre = "Caja POS", Tipo = "Caja", Subtipo = "POS", SaldoInicial = 0, SaldoActual = 0, Activa = true },
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
                ("Seguros", "#0891b2"),
                ("Préstamos (cuotas)", "#b45309"),
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
    }
}