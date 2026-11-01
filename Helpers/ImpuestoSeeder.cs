using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Asegura que existan los impuestos básicos de Honduras para poder facturar:
    ///  - Si no hay ningún impuesto: crea ISV 15% (predeterminado), ISV 18% y Exento 0%.
    ///  - Si ya hay impuestos pero ninguno al 0%: agrega "Exento" 0% para poder marcar
    ///    productos exentos. No modifica ni borra los impuestos existentes.
    /// Idempotente.
    /// </summary>
    public static class ImpuestoSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (!context.Impuestos.Any())
            {
                context.Impuestos.AddRange(
                    new Impuesto { Nombre = "ISV 15%", Porcentaje = 15m, Descripcion = "Impuesto sobre ventas (tasa general)", EsPredeterminado = true, Activo = true },
                    new Impuesto { Nombre = "ISV 18%", Porcentaje = 18m, Descripcion = "Impuesto sobre ventas (tasa especial)", EsPredeterminado = false, Activo = true },
                    new Impuesto { Nombre = "Exento", Porcentaje = 0m, Descripcion = "Producto exento de ISV", EsPredeterminado = false, Activo = true });
                context.SaveChanges();
                return;
            }

            if (!context.Impuestos.Any(i => i.Activo && i.Porcentaje == 0m))
            {
                context.Impuestos.Add(new Impuesto
                {
                    Nombre = "Exento",
                    Porcentaje = 0m,
                    Descripcion = "Producto exento de ISV",
                    EsPredeterminado = false,
                    Activo = true
                });
                context.SaveChanges();
            }
        }
    }
}
