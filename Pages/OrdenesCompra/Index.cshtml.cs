using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.OrdenesCompra
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
        [BindProperty(SupportsGet = true)] public int? ProveedorId { get; set; }
        [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;
        public PagerInfo Pager { get; set; } = new();

        public List<Models.OrdenCompra> Ordenes { get; set; } = new();
        public List<Proveedor> Proveedores { get; set; } = new();

        public decimal TotalMes { get; set; }
        public int CantidadMes { get; set; }
        public decimal SaldoTotal { get; set; }
        public int OrdenesPendientesPago { get; set; }
        public int Recibidas { get; set; }
        public int Pendientes { get; set; }

        public void OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "Ordenes", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return;
            }

            Proveedores = _context.Proveedores
                .AsNoTracking()
                .OrderBy(p => p.Nombre)
                .ToList();

            // ===== Listado filtrado y paginado en la BD =====
            var query = _context.OrdenesCompra.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                var provIds = Proveedores.Where(p => p.Nombre.Contains(s, StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.Id).ToList();
                query = query.Where(o => o.Numero.Contains(s) || provIds.Contains(o.ProveedorId));
            }
            if (!string.IsNullOrWhiteSpace(Estado)) query = query.Where(o => o.Estado == Estado);
            if (ProveedorId.HasValue) query = query.Where(o => o.ProveedorId == ProveedorId.Value);

            Pager = new PagerInfo(Pagina, 50, query.Count());
            Ordenes = query.OrderByDescending(o => o.Fecha).ThenByDescending(o => o.Id)
                .Skip(Pager.Skip).Take(Pager.PageSize).ToList();

            // ===== KPIs globales (agregados en la BD) =====
            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var estadosValidos = new List<string> { "Enviada", "RecibidaParcial", "Recibida", "Pagada" };
            var todas = _context.OrdenesCompra.AsNoTracking();

            var mes = todas.Where(o => o.Fecha >= inicioMes && estadosValidos.Contains(o.Estado));
            TotalMes = mes.Sum(o => (decimal?)o.Total) ?? 0;
            CantidadMes = mes.Count();

            SaldoTotal = todas.Where(o => o.Estado != "Cancelada").Sum(o => (decimal?)o.Saldo) ?? 0;
            OrdenesPendientesPago = todas.Count(o => o.Saldo > 0 && (o.Estado == "Recibida" || o.Estado == "RecibidaParcial"));
            Recibidas = todas.Count(o => o.Estado == "Recibida" || o.Estado == "Pagada");
            Pendientes = todas.Count(o => o.Estado == "Borrador" || o.Estado == "Enviada");
        }
    }
}