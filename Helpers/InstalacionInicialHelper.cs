using System.Security.Cryptography;
using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Primera puesta en marcha en una base de datos nueva (instalación limpia, por ejemplo en Docker):
    /// si no existe ningún usuario crea el administrador inicial para poder entrar al sistema.
    /// </summary>
    public static class InstalacionInicialHelper
    {
        private const string Alfabeto = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string GenerarClave(int largo = 12)
        {
            var c = new char[largo];
            for (int i = 0; i < largo; i++) c[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
            return new string(c);
        }

        /// <summary>
        /// Crea el administrador si la tabla de usuarios está vacía. Si no se indica clave genera una al azar y la deja
        /// en App_Data/primer-acceso.txt (se debe leer, cambiar la clave y borrar el archivo).
        /// Devuelve un mensaje para el log, o null si no hizo nada.
        /// </summary>
        public static string? CrearAdminSiNoHayUsuarios(ApplicationDbContext db, string rutaContenido, string? usuario, string? clave)
        {
            if (db.Usuarios.Any()) return null;

            var nombre = string.IsNullOrWhiteSpace(usuario) ? "admin" : usuario.Trim();
            var generada = string.IsNullOrWhiteSpace(clave);
            var pass = generada ? GenerarClave() : clave!;

            db.Usuarios.Add(new Usuario
            {
                Username = nombre,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(pass),
                Rol = "Admin",
                Email = "admin@local",
                NombreCompleto = "Administrador",
                Activo = true,
                FechaCreacion = DateTime.Now
            });
            db.SaveChanges();

            if (!generada) return $"Se creó el administrador inicial '{nombre}' con la clave indicada en la configuración.";

            var dir = Path.Combine(rutaContenido, "App_Data");
            Directory.CreateDirectory(dir);
            var archivo = Path.Combine(dir, "primer-acceso.txt");
            File.WriteAllText(archivo,
                "PRIMER ACCESO A KIRKENTA\r\n" +
                $"Usuario: {nombre}\r\n" +
                $"Clave:   {pass}\r\n\r\n" +
                "Entra, cambia la clave desde el sistema y BORRA este archivo.\r\n");
            return $"Se creó el administrador inicial '{nombre}'. Su clave está en {archivo} (léela, cámbiala y borra el archivo).";
        }
    }
}
