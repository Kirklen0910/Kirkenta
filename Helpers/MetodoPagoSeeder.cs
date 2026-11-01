using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Seeder de métodos de pago básicos.
    /// Es idempotente: no duplica si ya existen.
    /// </summary>
    public static class MetodoPagoSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            var metodos = new List<MetodoPago>
            {
                new() { Nombre = "Efectivo",        Tipo = "Efectivo",      RequiereReferencia = false, Activo = true },
                new() { Nombre = "Tarjeta",         Tipo = "Tarjeta",       RequiereReferencia = true,  Activo = true },
                new() { Nombre = "Transferencia",   Tipo = "Transferencia", RequiereReferencia = true,  Activo = true },
                new() { Nombre = "Crédito",         Tipo = "Credito",       RequiereReferencia = false, Activo = true },
            };

            foreach (var metodo in metodos)
            {
                if (!context.MetodosPago.Any(m => m.Nombre == metodo.Nombre))
                {
                    context.MetodosPago.Add(metodo);
                }
            }

            context.SaveChanges();
        }
    }
}