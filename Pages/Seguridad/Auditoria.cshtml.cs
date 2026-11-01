using System.Text;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Seguridad
{
    public class AuditoriaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AuditoriaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public int? UsuarioId { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }
        [BindProperty(SupportsGet = true)] public bool SoloFallos { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        public List<ActividadUsuario> Registros { get; set; } = new();
        public List<Usuario> Usuarios { get; set; } = new();
        public Dictionary<int, string> Nombres { get; set; } = new();

        private IQueryable<ActividadUsuario> Filtrar()
        {
            var query = _context.Actividades.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                query = query.Where(a => a.Accion.Contains(s) || (a.Detalle != null && a.Detalle.Contains(s)) || (a.Ip != null && a.Ip.Contains(s)));
            }
            if (UsuarioId.HasValue) query = query.Where(a => a.UsuarioId == UsuarioId.Value);
            if (Desde.HasValue) query = query.Where(a => a.Fecha >= Desde.Value.Date);
            if (Hasta.HasValue) query = query.Where(a => a.Fecha < Hasta.Value.Date.AddDays(1));
            if (SoloFallos) query = query.Where(a => a.Accion.Contains("Login fallido"));
            return query;
        }

        private bool TieneAcceso() =>
            PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Seguridad", "Auditoria", "ver");

        public IActionResult OnGet()
        {
            if (!TieneAcceso())
            {
                TempData["Error"] = "No tienes permiso para ver la auditoría";
                return RedirectToPage("/Index");
            }

            Usuarios = _context.Usuarios.OrderBy(u => u.Username).ToList();
            Nombres = Usuarios.ToDictionary(u => u.Id, u => u.Username);

            var query = Filtrar();
            Pager = new PagerInfo(Pagina, 50, query.Count());
            Registros = query.OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id)
                .Skip(Pager.Skip).Take(Pager.PageSize).ToList();
            return Page();
        }

        public IActionResult OnGetExportar()
        {
            if (!TieneAcceso())
            {
                TempData["Error"] = "No tienes permiso para exportar la auditoría";
                return RedirectToPage("/Index");
            }

            var nombres = _context.Usuarios.ToDictionary(u => u.Id, u => u.Username);
            var filas = Filtrar().OrderByDescending(a => a.Fecha).Take(10000).ToList();

            static string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"") + "\"";

            var sb = new StringBuilder();
            sb.AppendLine("sep=,"); // Excel en español: separa en columnas al abrir con doble clic
            sb.AppendLine("Fecha,Usuario,Accion,Detalle,IP");
            foreach (var a in filas)
            {
                var usuario = nombres.TryGetValue(a.UsuarioId, out var n) ? n : a.UsuarioId.ToString();
                sb.AppendLine(string.Join(",", C(a.Fecha.ToString("yyyy-MM-dd HH:mm:ss")), C(usuario), C(a.Accion), C(a.Detalle), C(a.Ip)));
            }

            var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"auditoria_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
    }
}
