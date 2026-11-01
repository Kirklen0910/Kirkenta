using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Configuracion
{
    /// <summary>Impresora térmica de tickets y gaveta de dinero. Solo Admin.</summary>
    public class ImpresoraModel : PageModel
    {
        private readonly ImpresoraConfigStore _store;
        private readonly ImpresoraTermicaService _svc;
        private readonly ApplicationDbContext _db;

        public ImpresoraModel(ImpresoraConfigStore store, ImpresoraTermicaService svc, ApplicationDbContext db)
        {
            _store = store; _svc = svc; _db = db;
        }

        [BindProperty] public string Modo { get; set; } = "ninguna";
        [BindProperty] public string Host { get; set; } = "";
        [BindProperty] public int Puerto { get; set; } = 9100;
        [BindProperty] public string Impresora { get; set; } = "";
        [BindProperty] public int Columnas { get; set; } = 42;
        [BindProperty] public bool AbrirGaveta { get; set; }
        [BindProperty] public string Auto { get; set; } = "ninguno";
        [BindProperty] public int Copias { get; set; } = 1;
        [BindProperty] public string? Pie { get; set; }

        public List<string> ImpresorasWindows { get; set; } = new();
        public string? Resultado { get; set; }
        public bool ResultadoOk { get; set; }

        private async Task<bool> EsAdminAsync()
        {
            var nombre = User.Identity?.Name?.Trim();
            if (string.IsNullOrEmpty(nombre)) return false;
            return PermisoHelper.UsuarioTieneAcceso(_db, nombre, "Configuracion", "Impresora");
        }

        private IActionResult SinPermiso()
        {
            TempData["Error"] = "Solo un administrador puede configurar la impresora.";
            return Redirect("/");
        }

        private void Cargar()
        {
            var c = _store.Obtener();
            Modo = c.Modo; Host = c.Host; Puerto = c.Puerto; Impresora = c.Impresora; Columnas = c.Columnas;
            AbrirGaveta = c.AbrirGaveta; Auto = c.Auto; Copias = c.Copias; Pie = c.Pie;
        }

        private ImpresoraConfig Armar() => ImpresoraConfigStore.Normalizar(new ImpresoraConfig
        {
            Modo = Modo, Host = Host, Puerto = Puerto, Impresora = Impresora, Columnas = Columnas,
            AbrirGaveta = AbrirGaveta, Auto = Auto, Copias = Copias, Pie = Pie ?? ""
        });

        private void CargarImpresoras() => ImpresorasWindows = ImpresoraTermicaService.ListarImpresorasWindows();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!await EsAdminAsync()) return SinPermiso();
            Cargar();
            CargarImpresoras();
            return Page();
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            if (!await EsAdminAsync()) return SinPermiso();
            var cfg = Armar();
            var error = cfg.Modo == "ninguna" ? null : ImpresoraConfigStore.Validar(cfg);
            if (error != null)
            {
                Resultado = error; ResultadoOk = false; CargarImpresoras();
                return Page();
            }
            _store.Guardar(cfg);
            TempData["Success"] = cfg.Modo == "ninguna" ? "Guardado. La impresión directa quedó apagada." : "Configuración de la impresora guardada.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostPruebaAsync()
        {
            if (!await EsAdminAsync()) return SinPermiso();
            var r = await _svc.PruebaAsync(Armar());
            Resultado = r.Mensaje; ResultadoOk = r.Ok; CargarImpresoras();
            return Page();
        }

        public async Task<IActionResult> OnPostGavetaAsync()
        {
            if (!await EsAdminAsync()) return SinPermiso();
            var r = await _svc.AbrirGavetaAsync(Armar());
            Resultado = r.Mensaje; ResultadoOk = r.Ok; CargarImpresoras();
            return Page();
        }
    }
}
