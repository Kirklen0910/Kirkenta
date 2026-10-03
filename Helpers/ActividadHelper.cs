using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    public static class ActividadHelper
    {
        /// <summary>
        /// Registra una actividad de usuario en la base de datos.
        /// Nunca lanza excepción (silencioso si falla).
        /// </summary>
        public static void Registrar(
            ApplicationDbContext context,
            int usuarioId,
            string accion,
            string? detalle = null,
            string? ip = null,
            string? userAgent = null)
        {
            try
            {
                if (usuarioId <= 0) return;

                var actividad = new ActividadUsuario
                {
                    UsuarioId = usuarioId,
                    Accion = accion,
                    Detalle = detalle,
                    Ip = string.IsNullOrEmpty(ip) ? "N/A" : ip,
                    UserAgent = userAgent,
                    Fecha = DateTime.Now
                };

                context.Actividades.Add(actividad);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log silencioso: no rompemos la app si falla el log
                Console.WriteLine($"Error al registrar actividad: {ex.Message}");
            }
        }
    }
}