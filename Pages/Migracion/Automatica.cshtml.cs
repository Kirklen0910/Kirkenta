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
        private readonly RespaldoSistemaService _respaldo;

        public AutomaticaModel(ApplicationDbContext context, RespaldoSistemaService respaldo)
        {
            _context = context;
            _respaldo = respaldo;
        }

        [BindProperty(SupportsGet = true)] public string Token { get; set; } = "";
        [BindProperty] public Dictionary<int, string> Destino { get; set; } = new();
        [BindProperty] public bool Confirmo { get; set; }

        public List<TablaOrigen> Tablas { get; set; } = new();
        public List<PasoMigracion> Plan { get; set; } = new();
        public ResultadoAutomatico? Resultado { get; set; }
        public int RegistrosExistentes { get; set; }

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
            RegistrosExistentes = _context.Productos.Count() + _context.Clientes.Count() + _context.Proveedores.Count() + _context.Ventas.Count();
            return null;
        }

        public IActionResult OnGet() => Cargar(false) ?? Page();

        public IActionResult OnPostRevisar()
        {
            var r = Cargar(true);
            if (r != null) return r;
            Resultado = MigracionAutomaticaHelper.Ejecutar(_context, Tablas, Plan, Usuario()?.Id, false);
            return Page();
        }

        public async Task<IActionResult> OnPostMigrarAsync()
        {
            var r = Cargar(true);
            if (r != null) return r;

            if (!Confirmo)
            {
                TempData["Error"] = "Marca la casilla de confirmación para migrar.";
                return Page();
            }

            // Red de seguridad: respaldo completo ANTES de tocar nada
            var resp = await _respaldo.CrearAsync("antes-de-migracion");
            if (!resp.Ok)
            {
                TempData["Error"] = "No se migró nada: no se pudo crear el respaldo previo. " + resp.Mensaje;
                return Page();
            }

            Resultado = MigracionAutomaticaHelper.Ejecutar(_context, Tablas, Plan, Usuario()?.Id, true);
            Resultado.Respaldo = resp.Ruta == null ? null : Path.GetFileName(resp.Ruta);

            var user = Usuario();
            if (Resultado.Aplicado)
            {
                var resumen = string.Join("; ", Resultado.Pasos.Select(p => $"{p.Paso.Titulo}: {p.Res?.Creados ?? 0}"));
                ActividadHelper.Registrar(_context, user?.Id ?? 0, "Migración automática", resumen,
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            else
            {
                ActividadHelper.Registrar(_context, user?.Id ?? 0, "Migración automática (cancelada)",
                    Resultado.ErrorFatal ?? "", HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            return Page();
        }
    }
}
