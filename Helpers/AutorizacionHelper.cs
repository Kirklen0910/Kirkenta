using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    /// <summary>Verifica usuario + contraseña de quien autoriza una operación sensible (con bloqueo por intentos).</summary>
    public static class AutorizacionHelper
    {
        public const int MaxIntentos = 5;

        public static bool VerificarAdmin(ApplicationDbContext db, string? username, string? password, out Usuario? autor, out string error)
        {
            autor = null; error = "";
            var nombre = (username ?? "").Trim();
            if (nombre.Length == 0 || string.IsNullOrEmpty(password))
            {
                error = "Indica el usuario y la contraseña del administrador que autoriza.";
                return false;
            }

            var u = db.Usuarios.FirstOrDefault(x => x.Username == nombre && x.Activo);
            if (u == null)
            {
                error = "Usuario o contraseña del administrador incorrectos.";
                return false;
            }
            if (u.BloqueadoHasta.HasValue && u.BloqueadoHasta.Value > DateTime.Now)
            {
                error = "Ese usuario está bloqueado temporalmente por intentos fallidos. Intenta más tarde.";
                return false;
            }

            bool ok;
            try { ok = BCrypt.Net.BCrypt.Verify(password, u.PasswordHash); } catch { ok = false; }
            if (!ok)
            {
                u.IntentosFallidos++;
                if (u.IntentosFallidos >= MaxIntentos) { u.BloqueadoHasta = DateTime.Now.AddMinutes(10); u.IntentosFallidos = 0; }
                db.SaveChanges();
                error = "Usuario o contraseña del administrador incorrectos.";
                return false;
            }

            if (u.IntentosFallidos > 0 || u.BloqueadoHasta != null)
            {
                u.IntentosFallidos = 0; u.BloqueadoHasta = null; db.SaveChanges();
            }
            if (u.Rol != "Admin")
            {
                error = "Solo un usuario con rol Administrador puede autorizar un cierre sin conteo.";
                return false;
            }
            autor = u;
            return true;
        }
    }
}
