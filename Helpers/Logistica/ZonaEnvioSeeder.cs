using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Logistica
{
    /// <summary>
    /// Seeder de zonas de envío típicas de Honduras.
    /// Es idempotente: no duplica si ya existen.
    /// </summary>
    public static class ZonaEnvioSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            var zonas = new List<ZonaEnvio>
            {
                new()
                {
                    Nombre = "Centro / Casco Urbano",
                    Descripcion = "Zona céntrica de la ciudad, entregas rápidas",
                    PrecioSugerido = 50.00m,
                    Color = "#10b981",
                    Activa = true,
                    Orden = 1
                },
                new()
                {
                    Nombre = "Zona Cercana",
                    Descripcion = "Colonias cercanas al centro, hasta 5 km",
                    PrecioSugerido = 80.00m,
                    Color = "#3b82f6",
                    Activa = true,
                    Orden = 2
                },
                new()
                {
                    Nombre = "Zona Media",
                    Descripcion = "Colonias alejadas, entre 5 y 15 km",
                    PrecioSugerido = 150.00m,
                    Color = "#f59e0b",
                    Activa = true,
                    Orden = 3
                },
                new()
                {
                    Nombre = "Zona Lejana",
                    Descripcion = "Municipios aledaños, más de 15 km",
                    PrecioSugerido = 300.00m,
                    Color = "#ef4444",
                    Activa = true,
                    Orden = 4
                },
                new()
                {
                    Nombre = "Envío Gratis",
                    Descripcion = "Promociones o clientes VIP con envío sin costo",
                    PrecioSugerido = 0.00m,
                    Color = "#8b5cf6",
                    Activa = true,
                    Orden = 5
                }
            };

            foreach (var zona in zonas)
            {
                if (!context.ZonasEnvio.Any(z => z.Nombre == zona.Nombre))
                {
                    zona.FechaCreacion = DateTime.Now;
                    zona.EmpresaId = 1;
                    context.ZonasEnvio.Add(zona);
                }
            }

            context.SaveChanges();
        }
    }
}