using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Reportes
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool VerVentas { get; private set; }
        public bool VerCompras { get; private set; }
        public bool VerCxC { get; private set; }
        public bool VerCxP { get; private set; }
        public bool VerISV { get; private set; }
        public bool VerInventario { get; private set; }
        public bool VerRentabilidad { get; private set; }

        private bool Ver(string sub) => PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Reportes", sub, "ver");

        public IActionResult OnGet()
        {
            VerVentas = Ver("Ventas");
            VerCompras = Ver("Compras");
            VerCxC = Ver("CuentasPorCobrar");
            VerCxP = Ver("CuentasPorPagar");
            VerISV = Ver("ISV");
            VerInventario = Ver("Inventario");
            VerRentabilidad = Ver("Rentabilidad");

            if (!(Ver("Index") || VerVentas || VerCompras || VerCxC || VerCxP || VerISV || VerInventario || VerRentabilidad))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            return Page();
        }
    }
}