using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Migracion;
using Kirkenta.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Migracion
{
    public class AutomaticaModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceScopeFactory _scopes;

        public AutomaticaModel(ApplicationDbContext context, IServiceScopeFactory scopes)
        {
            _context = context;
            _scopes = scopes;
        }

        [BindProperty(SupportsGet = true)] public string Token { get; set; } = "";
        [BindProperty] public Dictionary<int, string> Destino { get; set; } = new();
        [BindProperty] public bool Confirmo { get; set; }
        /// <summary>Cómo leer las fechas donde ningún valor aclara el orden: «dmy» o «mdy».</summary>
        [BindProperty] public string FormatoFecha { get; set; } = "dmy";
        [BindProperty(SupportsGet = true)] public string? Tarea { get; set; }

        public List<TablaOrigen> Tablas { get; set; } = new();
        public List<PasoMigracion> Plan { get; set; } = new();
        public ResultadoAutomatico? Resultado { get; set; }
        public int RegistrosExistentes { get; set; }
        public List<FormatoColumna> Formatos { get; set; } = new();
        public string EmpresaNombre => _context.ConfiguracionEmpresa.Select(e => e.Nombre).FirstOrDefault() ?? "";
        /// <summary>Revisión o migración corriendo en segundo plano (se muestra la barra de avance).</summary>
        public TareaMigracion? EnCurso { get; set; }

        private Kirkenta.Models.Usuario? Usuario() => _context.Usuarios.FirstOrDefault(x => x.Username == User.Identity!.Name);

        private IActionResult? Cargar(bool usarElegidos)
        {
            if (Usuario()?.Rol != "Admin") { TempData["Error"] = "Solo el administrador puede usar la migración."; return RedirectToPage("/Index"); }
            var tablas = MigracionAlmacen.Cargar(Token);
            if (tablas == null)
            {
                TempData["Error"] = "La migración expiró. Vuelve a subir el archivo.";
                return RedirectToPage("/Migracion/Index");
            }
            HttpContext.Session.SetString(MigracionAlmacen.SessionToken, Token);
            Tablas = tablas;
            Plan = MigracionAutomaticaHelper.Planificar(tablas, usarElegidos && Destino.Count > 0 ? Destino : null);
            Formatos = MigracionFormatos.Analizar(tablas, Plan);
            RegistrosExistentes = _context.Productos.Count() + _context.Clientes.Count() + _context.Proveedores.Count() + _context.Ventas.Count();
            return null;
        }

        public IActionResult OnGet()
        {
            var t = MigracionTareas.Obtener(Tarea);
            if (t != null && t.Token == Token) { Destino = t.Destino; FormatoFecha = t.FormatoFecha; }
            var r = Cargar(t != null);
            if (r != null) return r;
            if (t == null) return Page();
            if (t.Terminada) Resultado = t.Resultado;
            else EnCurso = t;
            return Page();
        }

        /// <summary>Avance de la tarea (la página lo consulta cada segundo).</summary>
        public IActionResult OnGetEstado()
        {
            var t = Usuario()?.Rol == "Admin" ? MigracionTareas.Obtener(Tarea) : null;
            if (t == null) return new JsonResult(new { p = 0, etapa = "No se encontró la tarea", listo = true });
            return new JsonResult(new { p = t.Porcentaje, etapa = t.Etapa, listo = t.Terminada });
        }

        public IActionResult OnPostRevisar() => Iniciar(false);

        public IActionResult OnPostMigrar()
        {
            if (!Confirmo)
            {
                var r = Cargar(true);
                if (r != null) return r;
                TempData["Error"] = "Marca la casilla de confirmación para migrar.";
                return Page();
            }
            return Iniciar(true);
        }

        private IActionResult Iniciar(bool aplicar)
        {
            var r = Cargar(true);
            if (r != null) return r;
            var usuario = Usuario();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var tablas = Tablas;
            var plan = Plan;
            var formato = FormatoFecha == "mdy" ? "mdy" : "dmy";

            var t = MigracionTareas.Iniciar(new TareaMigracion { Token = Token, Aplicar = aplicar, Destino = new(Destino), FormatoFecha = formato }, tarea =>
            {
                using var scope = _scopes.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                string? respaldo = null;
                if (aplicar)
                {
                    // Red de seguridad: respaldo completo ANTES de tocar nada
                    tarea.Avanzar(0, "Respaldo previo");
                    var resp = scope.ServiceProvider.GetRequiredService<RespaldoSistemaService>().CrearAsync("antes-de-migracion").GetAwaiter().GetResult();
                    if (!resp.Ok) return new ResultadoAutomatico { ErrorFatal = "No se migró nada: no se pudo crear el respaldo previo. " + resp.Mensaje };
                    respaldo = resp.Ruta == null ? null : Path.GetFileName(resp.Ruta);
                }

                var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, plan, usuario?.Id, aplicar, tarea.Avanzar, formato);
                res.Respaldo = respaldo;
                if (aplicar)
                {
                    if (res.Aplicado)
                        ActividadHelper.Registrar(ctx, usuario?.Id ?? 0, "Migración automática",
                            string.Join("; ", res.Pasos.Select(p => $"{p.Paso.Titulo}: {p.Res?.Creados ?? 0}")), ip);
                    else
                        ActividadHelper.Registrar(ctx, usuario?.Id ?? 0, "Migración automática (cancelada)", res.ErrorFatal ?? "", ip);
                }
                return res;
            });

            if (t == null)
            {
                TempData["Error"] = "Ya hay una revisión o migración en curso. Espera a que termine.";
                return Page();
            }
            return RedirectToPage(new { Token, Tarea = t.Id });
        }
    }
}
