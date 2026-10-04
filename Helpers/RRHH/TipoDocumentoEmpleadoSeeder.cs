using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.RRHH
{
    /// <summary>
    /// Seeder de tipos de documentos por defecto para empleados.
    /// </summary>
    public static class TipoDocumentoEmpleadoSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            var tipos = new (string Nombre, string Categoria, bool Obligatorio, bool Vence, string Icono)[]
            {
                // ===== PERSONALES =====
                ("DNI / Cédula de identidad", "Personales", true, false, "id-card"),
                ("RTN", "Personales", false, false, "file-text"),
                ("Partida de nacimiento", "Personales", false, false, "file"),
                ("Foto de perfil", "Personales", false, false, "camera"),
                ("Constancia de estudios", "Personales", false, false, "graduation-cap"),

                // ===== ANTECEDENTES =====
                ("Antecedentes policiales", "Antecedentes", true, true, "shield"),
                ("Antecedentes penales", "Antecedentes", true, true, "shield-alert"),

                // ===== REFERENCIAS =====
                ("Referencia laboral 1", "Referencias", false, false, "user-check"),
                ("Referencia laboral 2", "Referencias", false, false, "user-check"),
                ("Referencia personal", "Referencias", false, false, "user"),

                // ===== ACADÉMICOS =====
                ("Título académico", "Academicos", false, false, "award"),
                ("Diploma", "Academicos", false, false, "award"),
                ("Certificaciones", "Academicos", false, false, "certificate"),

                // ===== CONTRATO =====
                ("Contrato laboral firmado", "Contrato", true, false, "file-signature"),
                ("Adendum de contrato", "Contrato", false, false, "file-plus"),

                // ===== MÉDICOS =====
                ("Examen médico de aptitud", "Medicos", true, true, "heart-pulse"),
                ("Certificado de salud", "Medicos", false, true, "heart"),

                // ===== OTROS =====
                ("Otro documento", "Otros", false, false, "file")
            };

            int orden = 1;
            foreach (var (nombre, categoria, obligatorio, vence, icono) in tipos)
            {
                var existe = context.TiposDocumentoEmpleado.Any(t => t.Nombre == nombre);
                if (existe) { orden++; continue; }

                context.TiposDocumentoEmpleado.Add(new TipoDocumentoEmpleado
                {
                    Nombre = nombre,
                    Categoria = categoria,
                    EsObligatorio = obligatorio,
                    RequiereVencimiento = vence,
                    DiasAlertaVencimiento = vence ? 30 : null,
                    Icono = icono,
                    EsSistema = true,
                    Activo = true,
                    Orden = orden,
                    FechaCreacion = DateTime.Now
                });
                orden++;
            }

            context.SaveChanges();
        }
    }
}