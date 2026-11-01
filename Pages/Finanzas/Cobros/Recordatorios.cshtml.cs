using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Finanzas.Cobros
{
    /// <summary>
    /// Recordatorios de cobro por WhatsApp: un renglón por cliente con saldo (no por factura),
    /// mensaje formal ya escrito, y bitácora de a quién se le recordó y cuándo.
    /// No requiere API de Meta: abre wa.me y el usuario solo presiona enviar.
    /// </summary>
    public class RecordatoriosModel : PageModel
    {
        private const string AccionRecordatorio = "Recordatorio de cobro";
        private readonly ApplicationDbContext _context;

        public RecordatoriosModel(ApplicationDbContext context) => _context = context;

        public class FilaCliente
        {
            public int ClienteId { get; set; }
            public string Nombre { get; set; } = "";
            public string Telefono { get; set; } = "";
            public int Documentos { get; set; }
            public decimal Saldo { get; set; }
            public decimal Vencido { get; set; }
            public int DiasAtraso { get; set; }
            public DateTime? UltimoRecordatorio { get; set; }
            public string? Enlace { get; set; }
        }

        public List<FilaCliente> Filas { get; set; } = new();
        public decimal TotalSaldo { get; set; }
        public decimal TotalVencido { get; set; }
        public int SinTelefono { get; set; }

        [BindProperty(SupportsGet = true)] public string Filtro { get; set; } = "vencidos"; // vencidos | todos | sinaviso
        [BindProperty(SupportsGet = true)] public int DiasMinimos { get; set; } = 0;

        private bool Autorizado(string accion)
        {
            var u = _context.Usuarios.AsNoTracking().FirstOrDefault(x => x.Username == User.Identity!.Name);
            var rol = u?.Rol ?? "Pendiente";
            return rol == "Admin" || PermisoHelper.TienePermiso(_context, rol, "Finanzas", "Cobros", accion);
        }

        public IActionResult OnGet()
        {
            if (!Autorizado("ver"))
            {
                TempData["Error"] = "No tienes permiso para ver los cobros pendientes.";
                return RedirectToPage("/Finanzas/Index");
            }

            var hoy = DateTime.Today;
            var empresa = _context.ConfiguracionEmpresa.AsNoTracking().Select(e => e.Nombre).FirstOrDefault() ?? "Mi Empresa";

            var ventas = _context.Ventas.AsNoTracking()
                .Where(v => v.EsCredito && v.SaldoPendiente > 0 && v.Estado != "Anulada" && v.ClienteId != null)
                .ToList();
            var ids = ventas.Select(v => v.ClienteId!.Value).Distinct().ToList();
            var clientes = _context.Clientes.AsNoTracking().Where(c => ids.Contains(c.Id)).ToDictionary(c => c.Id);
            var ultimos = UltimosRecordatorios();

            foreach (var g in ventas.GroupBy(v => v.ClienteId!.Value))
            {
                if (!clientes.TryGetValue(g.Key, out var cli)) continue;

                decimal saldo = 0, vencido = 0; int maxAtraso = 0;
                foreach (var v in g)
                {
                    saldo += v.SaldoPendiente;
                    var venc = (v.FechaVencimientoCredito ?? (cli.DiasCredito > 0 ? v.Fecha.AddDays(cli.DiasCredito) : v.Fecha)).Date;
                    var atraso = (hoy - venc).Days;
                    if (atraso > 0) { vencido += v.SaldoPendiente; maxAtraso = Math.Max(maxAtraso, atraso); }
                }

                ultimos.TryGetValue(cli.Id, out var ult);
                Filas.Add(new FilaCliente
                {
                    ClienteId = cli.Id,
                    Nombre = cli.Nombre,
                    Telefono = cli.Telefono ?? "",
                    Documentos = g.Count(),
                    Saldo = saldo,
                    Vencido = vencido,
                    DiasAtraso = maxAtraso,
                    UltimoRecordatorio = ult == default ? null : ult,
                    Enlace = WhatsAppHelper.Enlace(cli.Telefono, WhatsAppHelper.MensajeCobranza(empresa, cli.Nombre, saldo, vencido))
                });
            }

            IEnumerable<FilaCliente> q = Filas;
            if (Filtro == "vencidos") q = q.Where(f => f.Vencido > 0);
            else if (Filtro == "sinaviso") q = q.Where(f => f.Vencido > 0 && (f.UltimoRecordatorio == null || (hoy - f.UltimoRecordatorio.Value.Date).Days >= 7));
            if (DiasMinimos > 0) q = q.Where(f => f.DiasAtraso >= DiasMinimos);

            Filas = q.OrderByDescending(f => f.DiasAtraso).ThenByDescending(f => f.Vencido).ToList();
            TotalSaldo = Filas.Sum(f => f.Saldo);
            TotalVencido = Filas.Sum(f => f.Vencido);
            SinTelefono = Filas.Count(f => f.Enlace == null);
            return Page();
        }

        /// <summary>Deja constancia de que se abrió el WhatsApp de cobro de un cliente.</summary>
        public IActionResult OnPostRegistrar(int clienteId)
        {
            if (!Autorizado("ver")) return Forbid();

            var cli = _context.Clientes.AsNoTracking().FirstOrDefault(c => c.Id == clienteId);
            if (cli == null) return NotFound();

            var saldo = _context.Ventas.AsNoTracking()
                .Where(v => v.ClienteId == clienteId && v.EsCredito && v.SaldoPendiente > 0 && v.Estado != "Anulada")
                .Sum(v => (decimal?)v.SaldoPendiente) ?? 0m;

            var usuario = _context.Usuarios.AsNoTracking().FirstOrDefault(u => u.Username == User.Identity!.Name);
            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, AccionRecordatorio,
                $"cli:{cli.Id}|{cli.Nombre}|L. {saldo:N2}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return new JsonResult(new { ok = true, fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm") });
        }

        /// <summary>Fecha del último recordatorio por cliente, leída de la bitácora de actividad.</summary>
        private Dictionary<int, DateTime> UltimosRecordatorios()
        {
            var desde = DateTime.Today.AddDays(-120);
            var filas = _context.Actividades.AsNoTracking()
                .Where(a => a.Accion == AccionRecordatorio && a.Fecha >= desde)
                .Select(a => new { a.Detalle, a.Fecha })
                .ToList();

            var r = new Dictionary<int, DateTime>();
            foreach (var f in filas)
            {
                var d = f.Detalle ?? "";
                if (!d.StartsWith("cli:")) continue;
                var corte = d.IndexOf('|');
                if (corte < 5 || !int.TryParse(d.AsSpan(4, corte - 4), out var id)) continue;
                if (!r.TryGetValue(id, out var prev) || f.Fecha > prev) r[id] = f.Fecha;
            }
            return r;
        }
    }
}
