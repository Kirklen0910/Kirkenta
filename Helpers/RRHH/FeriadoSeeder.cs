using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.RRHH
{
    /// <summary>
    /// Seeder de feriados por país. Se ejecuta al arrancar la app.
    /// Carga feriados de: Honduras, Guatemala, El Salvador, Costa Rica,
    /// Nicaragua, Panamá, México y USA.
    /// </summary>
    public static class FeriadoSeeder
    {
        public static void Seed(ApplicationDbContext context, int anio)
        {
            try
            {
                SeedHonduras(context, anio);
                SeedGuatemala(context, anio);
                SeedElSalvador(context, anio);
                SeedCostaRica(context, anio);
                SeedNicaragua(context, anio);
                SeedPanama(context, anio);
                SeedMexico(context, anio);
                SeedUSA(context, anio);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FeriadoSeeder] Error: {ex.Message}");
            }
        }

        // ============================================================
        // HONDURAS
        // ============================================================
        private static void SeedHonduras(ApplicationDbContext context, int anio)
        {
            var feriadosFijos = new (int Mes, int Dia, string Nombre, string Tipo)[]
            {
                (1, 1, "Año Nuevo", "Nacional"),
                (4, 14, "Día de las Américas", "Civico"),
                (5, 1, "Día del Trabajo", "Nacional"),
                (9, 15, "Día de la Independencia", "Nacional"),
                (10, 3, "Día del Soldado", "Civico"),
                (10, 12, "Día de la Raza", "Civico"),
                (10, 21, "Día de las Fuerzas Armadas", "Civico"),
                (12, 25, "Navidad", "Religioso"),
            };

            foreach (var f in feriadosFijos)
            {
                AgregarFeriado(context, f.Nombre, new DateTime(anio, f.Mes, f.Dia), "HN", f.Tipo, true, false, null);
            }

            // Feriados móviles (Semana Santa)
            AgregarFeriadoMovil(context, anio, "HN");
        }

        // ============================================================
        // GUATEMALA
        // ============================================================
        private static void SeedGuatemala(ApplicationDbContext context, int anio)
        {
            var feriadosFijos = new (int Mes, int Dia, string Nombre, string Tipo)[]
            {
                (1, 1, "Año Nuevo", "Nacional"),
                (5, 1, "Día del Trabajo", "Nacional"),
                (6, 30, "Día del Ejército", "Civico"),
                (9, 15, "Día de la Independencia", "Nacional"),
                (10, 20, "Revolución de 1944", "Civico"),
                (11, 1, "Día de Todos los Santos", "Religioso"),
                (12, 24, "Nochebuena", "Religioso"),
                (12, 25, "Navidad", "Religioso"),
                (12, 31, "Fin de Año", "Nacional"),
            };

            foreach (var f in feriadosFijos)
            {
                AgregarFeriado(context, f.Nombre, new DateTime(anio, f.Mes, f.Dia), "GT", f.Tipo, true, false, null);
            }

            AgregarFeriadoMovil(context, anio, "GT");
        }

        // ============================================================
        // EL SALVADOR
        // ============================================================
        private static void SeedElSalvador(ApplicationDbContext context, int anio)
        {
            var feriadosFijos = new (int Mes, int Dia, string Nombre, string Tipo)[]
            {
                (1, 1, "Año Nuevo", "Nacional"),
                (5, 1, "Día del Trabajo", "Nacional"),
                (5, 10, "Día de la Madre", "Civico"),
                (6, 17, "Día del Padre", "Civico"),
                (8, 6, "Día del Divino Salvador del Mundo", "Religioso"),
                (9, 15, "Día de la Independencia", "Nacional"),
                (11, 2, "Día de los Difuntos", "Religioso"),
                (12, 25, "Navidad", "Religioso"),
            };

            foreach (var f in feriadosFijos)
            {
                AgregarFeriado(context, f.Nombre, new DateTime(anio, f.Mes, f.Dia), "SV", f.Tipo, true, false, null);
            }

            AgregarFeriadoMovil(context, anio, "SV");
        }

        // ============================================================
        // COSTA RICA
        // ============================================================
        private static void SeedCostaRica(ApplicationDbContext context, int anio)
        {
            var feriadosFijos = new (int Mes, int Dia, string Nombre, string Tipo)[]
            {
                (1, 1, "Año Nuevo", "Nacional"),
                (4, 11, "Batalla de Rivas", "Civico"),
                (5, 1, "Día del Trabajo", "Nacional"),
                (7, 25, "Anexión del Partido de Nicoya", "Civico"),
                (8, 2, "Virgen de los Ángeles", "Religioso"),
                (8, 15, "Día de la Madre", "Civico"),
                (9, 15, "Día de la Independencia", "Nacional"),
                (12, 25, "Navidad", "Religioso"),
            };

            foreach (var f in feriadosFijos)
            {
                AgregarFeriado(context, f.Nombre, new DateTime(anio, f.Mes, f.Dia), "CR", f.Tipo, true, false, null);
            }

            AgregarFeriadoMovil(context, anio, "CR");
        }

        // ============================================================
        // NICARAGUA
        // ============================================================
        private static void SeedNicaragua(ApplicationDbContext context, int anio)
        {
            var feriadosFijos = new (int Mes, int Dia, string Nombre, string Tipo)[]
            {
                (1, 1, "Año Nuevo", "Nacional"),
                (5, 1, "Día del Trabajo", "Nacional"),
                (5, 30, "Día de la Madre", "Civico"),
                (7, 19, "Revolución Sandinista", "Civico"),
                (9, 14, "Batalla de San Jacinto", "Civico"),
                (9, 15, "Día de la Independencia", "Nacional"),
                (12, 8, "La Purísima", "Religioso"),
                (12, 25, "Navidad", "Religioso"),
            };

            foreach (var f in feriadosFijos)
            {
                AgregarFeriado(context, f.Nombre, new DateTime(anio, f.Mes, f.Dia), "NI", f.Tipo, true, false, null);
            }

            AgregarFeriadoMovil(context, anio, "NI");
        }

        // ============================================================
        // PANAMÁ
        // ============================================================
        private static void SeedPanama(ApplicationDbContext context, int anio)
        {
            var feriadosFijos = new (int Mes, int Dia, string Nombre, string Tipo)[]
            {
                (1, 1, "Año Nuevo", "Nacional"),
                (1, 9, "Día de los Mártires", "Civico"),
                (5, 1, "Día del Trabajo", "Nacional"),
                (11, 3, "Separación de Colombia", "Nacional"),
                (11, 4, "Día de la Bandera", "Civico"),
                (11, 5, "Día de Colón", "Civico"),
                (11, 10, "Primer Grito de Independencia", "Civico"),
                (11, 28, "Independencia de España", "Nacional"),
                (12, 8, "Día de la Madre", "Religioso"),
                (12, 25, "Navidad", "Religioso"),
            };

            foreach (var f in feriadosFijos)
            {
                AgregarFeriado(context, f.Nombre, new DateTime(anio, f.Mes, f.Dia), "PA", f.Tipo, true, false, null);
            }

            AgregarFeriadoMovil(context, anio, "PA");
        }

        // ============================================================
        // MÉXICO
        // ============================================================
        private static void SeedMexico(ApplicationDbContext context, int anio)
        {
            var feriadosFijos = new (int Mes, int Dia, string Nombre, string Tipo)[]
            {
                (1, 1, "Año Nuevo", "Nacional"),
                (2, 5, "Día de la Constitución", "Civico"),
                (3, 21, "Natalicio de Benito Juárez", "Civico"),
                (5, 1, "Día del Trabajo", "Nacional"),
                (9, 16, "Día de la Independencia", "Nacional"),
                (11, 2, "Día de Muertos", "Religioso"),
                (11, 20, "Revolución Mexicana", "Civico"),
                (12, 25, "Navidad", "Religioso"),
            };

            foreach (var f in feriadosFijos)
            {
                AgregarFeriado(context, f.Nombre, new DateTime(anio, f.Mes, f.Dia), "MX", f.Tipo, true, false, null);
            }

            AgregarFeriadoMovil(context, anio, "MX");
        }

        // ============================================================
        // USA
        // ============================================================
        private static void SeedUSA(ApplicationDbContext context, int anio)
        {
            var feriadosFijos = new (int Mes, int Dia, string Nombre, string Tipo)[]
            {
                (1, 1, "New Year's Day", "Nacional"),
                (7, 4, "Independence Day", "Nacional"),
                (11, 11, "Veterans Day", "Nacional"),
                (12, 25, "Christmas Day", "Religioso"),
            };

            foreach (var f in feriadosFijos)
            {
                AgregarFeriado(context, f.Nombre, new DateTime(anio, f.Mes, f.Dia), "US", f.Tipo, true, false, null);
            }
        }

        // ============================================================
        // HELPERS INTERNOS
        // ============================================================

        private static void AgregarFeriado(
            ApplicationDbContext context,
            string nombre,
            DateTime fecha,
            string paisCodigo,
            string tipo,
            bool esRecurrente,
            bool esMovil,
            int? anio)
        {
            var existe = context.Feriados.Any(f =>
                f.Nombre == nombre &&
                f.Fecha.Date == fecha.Date &&
                f.PaisCodigo == paisCodigo);

            if (existe) return;

            context.Feriados.Add(new Feriado
            {
                Nombre = nombre,
                Fecha = fecha,
                PaisCodigo = paisCodigo,
                Tipo = tipo,
                EsRecurrente = esRecurrente,
                EsMovil = esMovil,
                Anio = anio,
                Activo = true,
                FechaCreacion = DateTime.Now
            });
        }

        private static void AgregarFeriadoMovil(ApplicationDbContext context, int anio, string paisCodigo)
        {
            // Cálculo de Semana Santa (algoritmo de Gauss)
            var domingoPascua = CalcularDomingoPascua(anio);

            var feriadosMoviles = new (DateTime Fecha, string Nombre)[]
            {
                (domingoPascua.AddDays(-3), "Jueves Santo"),
                (domingoPascua.AddDays(-2), "Viernes Santo"),
                (domingoPascua.AddDays(-1), "Sábado de Gloria"),
                (domingoPascua, "Domingo de Resurrección"),
            };

            foreach (var f in feriadosMoviles)
            {
                AgregarFeriado(context, f.Nombre, f.Fecha, paisCodigo, "Religioso", false, true, anio);
            }
        }

        /// <summary>
        /// Calcula la fecha del Domingo de Pascua para un año dado (algoritmo de Gauss/Meeus).
        /// </summary>
        private static DateTime CalcularDomingoPascua(int anio)
        {
            int a = anio % 19;
            int b = anio / 100;
            int c = anio % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int mes = (h + l - 7 * m + 114) / 31;
            int dia = ((h + l - 7 * m + 114) % 31) + 1;

            return new DateTime(anio, mes, dia);
        }
    }
}