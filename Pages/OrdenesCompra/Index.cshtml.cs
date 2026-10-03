using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
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

            Ordenes = _context.OrdenesCompra
                .AsNoTracking()
                .OrderByDescending(o => o.Fecha)
                .ToList();

            Proveedores = _context.Proveedores
                .AsNoTracking()
                .ToList();

            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            // Excluir Borrador del KPI "Total comprado (mes)"
            var estadosValidos = new[] { "Enviada", "RecibidaParcial", "Recibida", "Pagada" };

            var ordenesMes = Ordenes
                .Where(o => o.Fecha >= inicioMes && estadosValidos.Contains(o.Estado))
                .ToList();

            TotalMes = ordenesMes.Sum(o => o.Total);
            CantidadMes = ordenesMes.Count;

            SaldoTotal = Ordenes
                .Where(o => o.Estado != "Cancelada")
                .Sum(o => o.Saldo);

            OrdenesPendientesPago = Ordenes
                .Count(o => o.Saldo > 0 && (o.Estado == "Recibida" || o.Estado == "RecibidaParcial"));

            Recibidas = Ordenes.Count(o => o.Estado == "Recibida" || o.Estado == "Pagada");
            Pendientes = Ordenes.Count(o => o.Estado == "Borrador" || o.Estado == "Enviada");
        }
    }
}