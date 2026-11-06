using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Finanzas.Movimientos
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }
        [BindProperty(SupportsGet = true)] public string? Tipo { get; set; }
        [BindProperty(SupportsGet = true)] public string? FormaPago { get; set; }
        [BindProperty(SupportsGet = true)] public int? CuentaId { get; set; }
        [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;

        public PagerInfo Pager { get; set; } = new();
        public List<MovimientoItem> Movimientos { get; set; } = new();
        public List<CuentaFinanciera> Cuentas { get; set; } = new();
        public List<CategoriaFinanciera> Categorias { get; set; } = new();

        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal Balance { get; set; }
        public int CantidadMovimientos { get; set; }

        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeAnular { get; set; }

        public class MovimientoItem
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public string Tipo { get; set; } = "";
            public string Concepto { get; set; } = "";
            public string? Referencia { get; set; }
            public decimal Monto { get; set; }
            public int CuentaId { get; set; }
            public int? CuentaDestinoId { get; set; }
            public string CuentaNombre { get; set; } = "";
            public string? CuentaDestinoNombre { get; set; }
            public string? CategoriaNombre { get; set; }
            public string? CategoriaColor { get; set; }
            public string Estado { get; set; } = "Activo";
            public string Origen { get; set; } = "Manual";
            public string? FormaPago { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "Movimientos", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Index");
            }

            PuedeCrear = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "MovimientosCreate", "crear");
            PuedeEditar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "MovimientosEdit", "editar");
            PuedeAnular = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "MovimientosAnular", "eliminar");

            var cuentas = _context.CuentasFinancieras.AsNoTracking().ToList();
            var categorias = _context.CategoriasFinancieras.AsNoTracking().ToList();

            Cuentas = cuentas;
            Categorias = categorias;

            var cuentasDict = cuentas.ToDictionary(c => c.Id, c => c.Nombre);
            var categoriasDict = categorias.ToDictionary(c => c.Id, c => new { c.Nombre, c.Color });

            var query = _context.MovimientosFinancieros.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                query = query.Where(m => m.Numero.Contains(s) || m.Concepto.Contains(s)
                                         || (m.Referencia != null && m.Referencia.Contains(s)));
            }
            if (Desde.HasValue) { var d = Desde.Value.Date; query = query.Where(m => m.Fecha >= d); }
            if (Hasta.HasValue) { var h = Hasta.Value.Date.AddDays(1); query = query.Where(m => m.Fecha < h); }
            if (!string.IsNullOrWhiteSpace(Tipo)) query = query.Where(m => m.Tipo == Tipo);
            if (!string.IsNullOrWhiteSpace(FormaPago))
            {
                var fp = FormaPago.Trim().ToLower();
                query = query.Where(m => m.FormaPago != null && m.FormaPago.ToLower() == fp);
            }
            if (CuentaId.HasValue)
            {
                var cid = CuentaId.Value;
                query = query.Where(m => m.CuentaId == cid || m.CuentaDestinoId == cid);
            }
            if (!string.IsNullOrWhiteSpace(Estado)) query = query.Where(m => m.Estado == Estado);

            // KPIs sobre el conjunto filtrado (solo movimientos activos), calculados en la BD
            var activos = query.Where(m => m.Estado == "Activo");
            // Los totales son dinero real: los movimientos solo contables (costo de ventas, retenciones) no lo mueven
            var marca = Kirkenta.Helpers.Finanzas.SaldoHelper.MarcaSoloContable;
            var dinero = activos.Where(m => m.Notas == null || !m.Notas.Contains(marca));
            TotalIngresos = dinero.Where(m => m.Tipo == "Ingreso").Sum(m => (decimal?)m.Monto) ?? 0;
            TotalEgresos = dinero.Where(m => m.Tipo == "Egreso").Sum(m => (decimal?)m.Monto) ?? 0;
            Balance = TotalIngresos - TotalEgresos;
            CantidadMovimientos = activos.Count();

            Pager = new PagerInfo(Pagina, 50, query.Count());
            var lista = query
                .OrderByDescending(m => m.Fecha)
                .ThenByDescending(m => m.Id)
                .Skip(Pager.Skip).Take(Pager.PageSize)
                .ToList();

            Movimientos = lista.Select(m => new MovimientoItem
            {
                Id = m.Id,
                Numero = m.Numero,
                Fecha = m.Fecha,
                Tipo = m.Tipo,
                Concepto = m.Concepto,
                Referencia = m.Referencia,
                Monto = m.Monto,
                CuentaId = m.CuentaId,
                CuentaDestinoId = m.CuentaDestinoId,
                CuentaNombre = cuentasDict.GetValueOrDefault(m.CuentaId, "—"),
                CuentaDestinoNombre = m.CuentaDestinoId.HasValue
                    ? cuentasDict.GetValueOrDefault(m.CuentaDestinoId.Value)
                    : null,
                CategoriaNombre = m.CategoriaId.HasValue
                    ? categoriasDict.GetValueOrDefault(m.CategoriaId.Value)?.Nombre
                    : null,
                CategoriaColor = m.CategoriaId.HasValue
                    ? categoriasDict.GetValueOrDefault(m.CategoriaId.Value)?.Color
                    : null,
                Estado = m.Estado,
                Origen = m.Origen,
                FormaPago = m.FormaPago
            }).ToList();

            return Page();
        }
    }
}