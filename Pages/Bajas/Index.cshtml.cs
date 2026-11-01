using Microsoft.AspNetCore.Mvc;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Bajas
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
        [BindProperty(SupportsGet = true)] public string? Tipo { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        public List<BajaInventario> Bajas { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public bool EsAdmin { get; set; }
        public bool PuedeCrear { get; set; }
        public bool PuedeAprobar { get; set; }
        public bool PuedeRevertir { get; set; }

        public int BajasMes { get; set; }
        public decimal BajasMesValor { get; set; }
        public int Pendientes { get; set; }
        public int TotalHistorico { get; set; }
        public decimal TotalHistoricoValor { get; set; }
        public int Aprobadas { get; set; }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Bajas", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver las bajas";
                return RedirectToPage("/Index");
            }

            EsAdmin = currentRol == "Admin";
            PuedeCrear = PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Bajas", "crear");
            PuedeAprobar = PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Bajas", "editar");
            PuedeRevertir = PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Bajas", "eliminar");

            var query = _context.BajasInventario.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                var prodIds = _context.Productos
                    .Where(p => p.Nombre.Contains(s))
                    .Select(p => p.Id)
                    .ToList();
                query = query.Where(b => b.Numero.Contains(s) || b.Motivo.Contains(s) || prodIds.Contains(b.ProductoId));
            }
            if (!string.IsNullOrWhiteSpace(Estado)) query = query.Where(b => b.Estado == Estado);
            if (!string.IsNullOrWhiteSpace(Tipo)) query = query.Where(b => b.TipoBaja == Tipo);

            Pager = new PagerInfo(Pagina, 50, query.Count());
            Bajas = query.OrderByDescending(b => b.Fecha).ThenByDescending(b => b.Id)
                .Skip(Pager.Skip).Take(Pager.PageSize).ToList();

            var idsPagina = Bajas.Select(b => b.ProductoId).Distinct().ToList();
            Productos = _context.Productos.Where(p => idsPagina.Contains(p.Id)).ToList();

            // KPIs globales: las pérdidas reales son las bajas APROBADAS y no revertidas
            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var todas = _context.BajasInventario.AsNoTracking();
            var reales = todas.Where(b => b.Estado == "Aprobada" && !b.Revertida);

            BajasMes = todas.Count(b => b.Fecha >= inicioMes);
            BajasMesValor = reales.Where(b => b.Fecha >= inicioMes).Sum(b => (decimal?)b.ValorTotal) ?? 0;
            Pendientes = todas.Count(b => b.Estado == "Pendiente");
            TotalHistorico = todas.Count();
            TotalHistoricoValor = reales.Sum(b => (decimal?)b.ValorTotal) ?? 0;
            Aprobadas = todas.Count(b => b.Estado == "Aprobada");

            return Page();
        }
    }
}