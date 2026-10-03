using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Proveedores
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Proveedor Proveedor { get; set; } = new();
        public decimal TotalComprado { get; set; }
        public int CantidadCompras { get; set; }
        public decimal SaldoPendiente { get; set; }
        public DateTime? UltimaCompra { get; set; }
        public List<OrdenCompra> UltimasOrdenes { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var p = _context.Proveedores.FirstOrDefault(x => x.Id == id);
            if (p == null)
            {
                TempData["Error"] = "Proveedor no encontrado";
                return RedirectToPage("/Proveedores/Index");
            }

            Proveedor = p;

            var ordenes = _context.OrdenesCompra
                .Where(o => o.ProveedorId == id && o.Estado != "Cancelada")
                .OrderByDescending(o => o.Fecha)
                .ToList();

            CantidadCompras = ordenes.Count;
            TotalComprado = ordenes.Sum(o => o.Total);
            SaldoPendiente = ordenes.Sum(o => o.Saldo);
            UltimaCompra = ordenes.FirstOrDefault()?.Fecha;
            UltimasOrdenes = ordenes.Take(10).ToList();

            return Page();
        }
    }
}