using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Migracion;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static Kirkenta.Helpers.Migracion.MigracionHistorialHelper;

namespace Kirkenta.Pages.Migracion
{
    public class HistorialModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public HistorialModel(ApplicationDbContext context) { _context = context; }

        [BindProperty(SupportsGet = true)] public string Token { get; set; } = "";
        [BindProperty(SupportsGet = true)] public int Tabla { get; set; }
        [BindProperty(SupportsGet = true)] public string Tipo { get; set; } = TipoCxC;
        [BindProperty(SupportsGet = true)] public int? Detalle { get; set; }

        [BindProperty] public Dictionary<string, string> Mapa { get; set; } = new();
        [BindProperty] public Dictionary<string, string> MapaDet { get; set; } = new();
        [BindProperty] public string TerceroNoExiste { get; set; } = "crear";

        public List<TablaOrigen> Tablas { get; set; } = new();
        public TablaOrigen Principal { get; set; } = new();
        public TablaOrigen? DetalleTabla { get; set; }
        public List<CampoDef> Campos { get; set; } = new();
        public List<CampoDef> CamposDet { get; set; } = new();
        public Dictionary<string, string?> Sugerido { get; set; } = new();
        public Dictionary<string, string?> SugeridoDet { get; set; } = new();
        public Resultado? Res { get; set; }
        public string NombreTipo => MigracionHistorialHelper.NombreTipo(Tipo);
        public bool EsVentas => Tipo == TipoVentas;

        private Kirkenta.Models.Usuario? Usuario() => _context.Usuarios.FirstOrDefault(x => x.Username == User.Identity!.Name);

        private IActionResult? Preparar()
        {
            if (Usuario()?.Rol != "Admin") { TempData["Error"] = "Solo el administrador puede usar la migración."; return RedirectToPage("/Index"); }
            if (!Tipos.Contains(Tipo)) return RedirectToPage("/Migracion/Index", new { continuar = 1 });

            var tablas = MigracionAlmacen.Cargar(Token);
            if (tablas == null || Tabla < 0 || Tabla >= tablas.Count)
            {
                TempData["Error"] = "La migración expiró. Vuelve a subir el archivo.";
                return RedirectToPage("/Migracion/Index");
            }
            HttpContext.Session.SetString(MigracionAlmacen.SessionToken, Token);

            Tablas = tablas;
            Principal = tablas[Tabla];
            Campos = MigracionHistorialHelper.Campos(Tipo);
            Sugerido = Detectar(Principal.Headers, Campos);

            if (EsVentas)
            {
                if (Detalle.HasValue && (Detalle < 0 || Detalle >= tablas.Count || Detalle == Tabla)) Detalle = null;
                if (Detalle.HasValue)
                {
                    DetalleTabla = tablas[Detalle.Value];
                    CamposDet = CamposDetalle();
                    SugeridoDet = Detectar(DetalleTabla.Headers, CamposDet);
                }
            }
            return null;
        }

        public IActionResult OnGet()
        {
            if (EsVentas && !Request.Query.ContainsKey("detalle") && !Request.Query.ContainsKey("sinDetalle"))
            {
                var tablas = MigracionAlmacen.Cargar(Token);
                if (tablas != null && Tabla >= 0 && Tabla < tablas.Count)
                    Detalle = SugerirTablaDetalle(tablas, Tabla);
            }
            return Preparar() ?? Page();
        }

        public IActionResult OnPostPrevisualizar() => Ejecutar(false);
        public IActionResult OnPostImportar() => Ejecutar(true);

        private IActionResult Ejecutar(bool aplicar)
        {
            var redir = Preparar();
            if (redir != null) return redir;

            var op = new Opciones
            {
                Tipo = Tipo,
                Mapa = Mapa.Where(p => !string.IsNullOrEmpty(p.Value)).ToDictionary(p => p.Key, p => p.Value),
                MapaDetalle = MapaDet.Where(p => !string.IsNullOrEmpty(p.Value)).ToDictionary(p => p.Key, p => p.Value),
                Detalle = DetalleTabla,
                TerceroNoExiste = TerceroNoExiste == "sin" ? "sin" : "crear"
            };

            Res = MigracionHistorialHelper.Procesar(_context, Principal, op, aplicar, Usuario()?.Id);

            if (aplicar && Res.Aplicado)
            {
                ActividadHelper.Registrar(_context, Usuario()?.Id ?? 0, "Migración: " + NombreTipo,
                    $"Lote {Res.Lote}: {Res.Creados} registros, {Res.LineasCreadas} líneas, {Res.TercerosCreados} terceros nuevos, monto L. {Res.Monto:N2}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            return Page();
        }

        public IActionResult OnPostDeshacer(string lote)
        {
            if (Usuario()?.Rol != "Admin") return RedirectToPage("/Index");
            var (borrados, conservados) = MigracionHistorialHelper.DeshacerLote(_context, lote);
            ActividadHelper.Registrar(_context, Usuario()?.Id ?? 0, "Migración: deshacer lote",
                $"Lote {lote}: {borrados} registros borrados, {conservados.Count} conservados",
                HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Lote {lote} deshecho: {borrados} registros eliminados." +
                (conservados.Count > 0 ? $" Se conservaron {conservados.Count} que ya tienen movimientos (ej.: {conservados[0]})" : "");
            return RedirectToPage("/Migracion/Index", new { continuar = 1 });
        }
    }
}
