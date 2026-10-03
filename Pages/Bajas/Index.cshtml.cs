using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Bajas
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<BajaInventario> Bajas { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public bool EsAdmin { get; set; }

        public int BajasMes { get; set; }
        public decimal BajasMesValor { get; set; }
        public int Pendientes { get; set; }
        public int TotalHistorico { get; set; }
        public decimal TotalHistoricoValor { get; set; }
        public int Aprobadas { get; set; }

        public void OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            EsAdmin = currentRol == "Admin";

            Bajas = _context.BajasInventario.OrderByDescending(b => b.Fecha).ToList();
            Productos = _context.Productos.ToList();

            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var bajasMes = Bajas.Where(b => b.Fecha >= inicioMes).ToList();

            BajasMes = bajasMes.Count;
            BajasMesValor = bajasMes.Sum(b => b.ValorTotal);
            Pendientes = Bajas.Count(b => b.Estado == "Pendiente");
            TotalHistorico = Bajas.Count;
            TotalHistoricoValor = Bajas.Sum(b => b.ValorTotal);
            Aprobadas = Bajas.Count(b => b.Estado == "Aprobada");
        }
    }
}