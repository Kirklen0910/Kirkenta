using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Configuracion
{
    /// <summary>Busca, descarga e instala versiones nuevas del sistema desde GitHub Releases (firmadas). Con contraseña.</summary>
    public class ActualizacionesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ActualizadorSistema _act;
        private readonly IServiceScopeFactory _scopes;

        public ActualizacionesModel(ApplicationDbContext db, ActualizadorSistema act, IServiceScopeFactory scopes)
        {
            _db = db; _act = act; _scopes = scopes;
        }

        public string VersionActual => ActualizadorSistema.VersionActual.ToString();
        public bool Configurado => _act.Configurado;
        public bool PuedeInstalar => _act.CarpetaDescargas != null;
        public DateTime? UltimaRevision => ActualizadorSistema.UltimaRevision;
        public InfoActualizacion? Disponible { get; set; }
        public string? Mensaje { get; set; }
        public bool MensajeEsError { get; set; }
        /// <summary>true cuando la instalación está en marcha: la página muestra el avance y se recarga sola al terminar.</summary>
        public bool EnCurso { get; set; }
        public string VersionDestino { get; set; } = "";

        [BindProperty] public string? Password { get; set; }

        private async Task<Models.Usuario?> AdminAsync()
        {
            var nombre = User.Identity?.Name?.Trim();
            if (string.IsNullOrEmpty(nombre)) return null;
            var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.Username == nombre);
            return u != null && PermisoHelper.UsuarioTieneAcceso(_db, u.Username, "Configuracion", "Actualizaciones") ? u : null;
        }

        private IActionResult SinPermiso()
        {
            TempData["Error"] = "Solo un administrador puede actualizar el sistema.";
            return Redirect("/");
        }

        private bool VerificarPassword(Models.Usuario u, out string error)
        {
            error = "";
            if (u.BloqueadoHasta != null && u.BloqueadoHasta > DateTime.Now) { error = "Usuario bloqueado temporalmente por intentos fallidos."; return false; }
            bool ok;
            try { ok = !string.IsNullOrEmpty(Password) && BCrypt.Net.BCrypt.Verify(Password, u.PasswordHash); } catch { ok = false; }
            if (ok)
            {
                if (u.IntentosFallidos > 0 || u.BloqueadoHasta != null) { u.IntentosFallidos = 0; u.BloqueadoHasta = null; _db.SaveChanges(); }
                return true;
            }
            u.IntentosFallidos++;
            if (u.IntentosFallidos >= 5) { u.BloqueadoHasta = DateTime.Now.AddMinutes(10); u.IntentosFallidos = 0; }
            _db.SaveChanges();
            error = "Contraseña incorrecta.";
            return false;
        }

        private string Ip() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

        private void MarcarEnCurso()
        {
            var e = EstadoActualizacion.Instantanea();
            EnCurso = e.Activa;
            VersionDestino = e.Version;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (await AdminAsync() == null) return SinPermiso();
            Disponible = ActualizadorSistema.UltimaDisponible;
            MarcarEnCurso();
            return Page();
        }

        /// <summary>Avance de la actualización (lo consulta la pantalla cada segundo).</summary>
        public async Task<IActionResult> OnGetEstadoAsync()
        {
            if (await AdminAsync() == null) return new JsonResult(new { sinPermiso = true }) { StatusCode = 401 };
            var e = EstadoActualizacion.Instantanea();
            Response.Headers["Cache-Control"] = "no-store";
            return new JsonResult(new { activa = e.Activa, fase = e.Fase, porcentaje = e.Porcentaje, mensaje = e.Mensaje, error = e.Error, version = e.Version, actual = VersionActual });
        }

        public async Task<IActionResult> OnPostBuscarAsync()
        {
            if (await AdminAsync() == null) return SinPermiso();
            var r = await _act.BuscarAsync(HttpContext.RequestAborted);
            Disponible = r.Disponible;
            Mensaje = r.Mensaje; MensajeEsError = !r.Ok;
            MarcarEnCurso();
            return Page();
        }

        public async Task<IActionResult> OnPostInstalarAsync()
        {
            var u = await AdminAsync();
            if (u == null) return SinPermiso();

            if (!VerificarPassword(u, out var errPass)) { Mensaje = errPass; MensajeEsError = true; Disponible = ActualizadorSistema.UltimaDisponible; return Page(); }

            // Se vuelve a consultar y verificar la firma: nunca se instala algo que venga del formulario
            var r = await _act.BuscarAsync(HttpContext.RequestAborted);
            Disponible = r.Disponible;
            if (!r.Ok || r.Disponible == null) { Mensaje = r.Ok ? "Ya no hay una versión nueva para instalar." : r.Mensaje; MensajeEsError = !r.Ok; return Page(); }

            if (!PuedeInstalar) { Mensaje = "Este sistema no está instalado con el instalador de Kirkenta; actualízalo manualmente."; MensajeEsError = true; return Page(); }

            // Todo el proceso (descarga, respaldo, instalador) corre en segundo plano; la pantalla muestra el avance
            if (!ActualizadorSistema.IniciarEnSegundoPlano(_scopes, r.Disponible, u.Id, Ip()))
            { Mensaje = "Ya hay una actualización en curso."; MensajeEsError = true; }
            MarcarEnCurso();
            return Page();
        }
    }
}
