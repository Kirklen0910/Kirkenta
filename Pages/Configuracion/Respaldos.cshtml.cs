using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Configuracion
{
    /// <summary>Respaldo y restauración completa de los datos del cliente. Solo Admin, con contraseña.</summary>
    [RequestSizeLimit(4L * 1024 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 4L * 1024 * 1024 * 1024)]
    public class RespaldosModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly RespaldoSistemaService _svc;
        private readonly IWebHostEnvironment _env;
        private readonly RespaldoConfigStore _cfgStore;

        public RespaldosModel(ApplicationDbContext db, RespaldoSistemaService svc, IWebHostEnvironment env, RespaldoConfigStore cfgStore)
        {
            _db = db; _svc = svc; _env = env; _cfgStore = cfgStore;
        }

        public record Fila(string Nombre, DateTime Fecha, long Bytes, ManifiestoRespaldo? Info);
        public List<Fila> Respaldos { get; set; } = new();
        public string Carpeta { get; set; } = "";
        public RespaldoConfig Config { get; set; } = new();
        public RespaldoCopiaEstado? UltimaCopia { get; set; }

        // Formulario del respaldo automático
        [BindProperty] public bool CfgAutomatico { get; set; }
        [BindProperty] public string CfgHora { get; set; } = "02:00";
        [BindProperty] public int CfgConservar { get; set; } = 14;
        [BindProperty] public string? CfgCopiaExterna { get; set; }

        [BindProperty] public string? Password { get; set; }
        [BindProperty] public string? Confirmacion { get; set; }
        [BindProperty] public string? Archivo { get; set; }
        [BindProperty] public IFormFile? Subido { get; set; }
        /// <summary>"todo" o "modulos".</summary>
        [BindProperty] public string Alcance { get; set; } = "todo";
        [BindProperty] public List<string> Modulos { get; set; } = new();
        [BindProperty] public bool IncluirArchivos { get; set; }

        private async Task<Models.Usuario?> AdminAsync()
        {
            var nombre = User.Identity?.Name?.Trim();
            if (string.IsNullOrEmpty(nombre)) return null;
            var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.Username == nombre);
            return u != null && PermisoHelper.UsuarioTieneAcceso(_db, u.Username, "Configuracion", "Respaldos") ? u : null;
        }

        private void Cargar()
        {
            Carpeta = _svc.Carpeta;
            Config = _cfgStore.Obtener();
            UltimaCopia = _cfgStore.UltimaCopia();
            Respaldos = _svc.Listar().Select(f => new Fila(f.Name, f.LastWriteTime, f.Length, RespaldoSistemaService.Inspeccionar(f.FullName))).ToList();
        }

        private IActionResult SinPermiso()
        {
            TempData["Error"] = "Solo un administrador puede respaldar o restaurar el sistema.";
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

        public async Task<IActionResult> OnGetAsync()
        {
            if (await AdminAsync() == null) return SinPermiso();
            Cargar();
            return Page();
        }

        public async Task<IActionResult> OnPostCrearAsync()
        {
            var u = await AdminAsync();
            if (u == null) return SinPermiso();
            var r = await _svc.CrearAsync();
            ActividadHelper.Registrar(_db, u.Id, "Respaldo del sistema", r.Mensaje, Ip());
            var extra = "";
            if (r.Ok && r.Ruta != null)
            {
                var cfg = _cfgStore.Obtener();
                if (!string.IsNullOrWhiteSpace(cfg.CopiaExterna))
                {
                    var (okCopia, msgCopia) = RespaldoConfigStore.CopiarAExterna(r.Ruta, cfg.CopiaExterna!, cfg.Conservar);
                    _cfgStore.RegistrarCopia(okCopia, msgCopia);
                    extra = " " + msgCopia + (okCopia ? "." : " (revisa la carpeta externa).");
                }
            }
            TempData[r.Ok ? "Success" : "Error"] = r.Mensaje + extra;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostConfigurarAsync()
        {
            var u = await AdminAsync();
            if (u == null) return SinPermiso();
            var c = new RespaldoConfig { Automatico = CfgAutomatico, Hora = (CfgHora ?? "").Trim(), Conservar = CfgConservar, CopiaExterna = CfgCopiaExterna };
            var error = RespaldoConfigStore.Validar(c);
            if (error != null) { TempData["Error"] = error; return RedirectToPage(); }
            _cfgStore.Guardar(c);
            ActividadHelper.Registrar(_db, u.Id, "Respaldo del sistema",
                $"Respaldo automático {(c.Automatico ? "activo a las " + c.Hora : "apagado")}, conservar {c.Conservar}, copia externa: {(string.IsNullOrWhiteSpace(c.CopiaExterna) ? "ninguna" : c.CopiaExterna)}", Ip());
            TempData["Success"] = "Configuración del respaldo guardada.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostProbarCopiaAsync()
        {
            if (await AdminAsync() == null) return SinPermiso();
            var carpeta = (CfgCopiaExterna ?? "").Trim();
            if (carpeta.Length == 0) { TempData["Error"] = "Escribe primero la carpeta externa (por ejemplo E:\\Respaldos)."; return RedirectToPage(); }
            if (!Path.IsPathRooted(carpeta)) { TempData["Error"] = "La carpeta debe ser una ruta completa, por ejemplo E:\\Respaldos."; return RedirectToPage(); }
            var (ok, msg) = RespaldoConfigStore.ProbarCarpeta(carpeta);
            TempData[ok ? "Success" : "Error"] = msg;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetDescargarAsync(string nombre)
        {
            var u = await AdminAsync();
            if (u == null) return SinPermiso();
            var f = _svc.Listar().FirstOrDefault(x => x.Name == nombre);
            if (f == null) return NotFound();
            ActividadHelper.Registrar(_db, u.Id, "Respaldo del sistema", "Descarga de " + f.Name, Ip());
            return PhysicalFile(f.FullName, "application/zip", f.Name);
        }

        public async Task<IActionResult> OnPostRestaurarAsync()
        {
            var u = await AdminAsync();
            if (u == null) return SinPermiso();

            if (!string.Equals((Confirmacion ?? "").Trim(), "RESTAURAR", StringComparison.Ordinal))
            { TempData["Error"] = "Para restaurar escribe la palabra RESTAURAR en mayúsculas."; return RedirectToPage(); }
            if (!VerificarPassword(u, out var err)) { TempData["Error"] = err; return RedirectToPage(); }

            string? ruta = null;
            string? temporal = null;
            try
            {
                if (Subido != null && Subido.Length > 0)
                {
                    temporal = Path.Combine(Path.GetTempPath(), "kirkenta-subido-" + Guid.NewGuid().ToString("N") + ".zip");
                    await using (var fs = System.IO.File.Create(temporal)) await Subido.CopyToAsync(fs);
                    ruta = temporal;
                }
                else if (!string.IsNullOrWhiteSpace(Archivo))
                {
                    var f = _svc.Listar().FirstOrDefault(x => x.Name == Archivo);
                    if (f == null) { TempData["Error"] = "No se encontró ese respaldo."; return RedirectToPage(); }
                    ruta = f.FullName;
                }
                else { TempData["Error"] = "Elige un respaldo de la lista o sube un archivo .zip."; return RedirectToPage(); }

                ISet<string>? soloTablas = null;
                if (Alcance == "modulos")
                {
                    soloTablas = RespaldoSistemaService.TablasDeGrupos(Modulos ?? new List<string>());
                    if (soloTablas.Count == 0) { TempData["Error"] = "Elige al menos un módulo para restaurar."; return RedirectToPage(); }
                }

                var r = await _svc.RestaurarAsync(ruta, soloTablas, soloTablas == null || IncluirArchivos);
                try { ActividadHelper.Registrar(_db, u.Id, "Restauración del sistema", r.Mensaje + (soloTablas != null ? " Módulos: " + string.Join(", ", Modulos ?? new List<string>()) : ""), Ip()); } catch { }
                TempData[r.Ok ? "Success" : "Error"] = r.Mensaje + (r.Avisos.Count > 0 ? " " + string.Join(" ", r.Avisos) : "");
            }
            finally
            {
                if (temporal != null) { try { System.IO.File.Delete(temporal); } catch { } }
            }
            return RedirectToPage();
        }
    }
}
