using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Compras
{
    public class SugerenciasModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public SugerenciasModel(ApplicationDbContext context) => _context = context;

        [BindProperty(SupportsGet = true)] public int Historial { get; set; } = 30;
        [BindProperty(SupportsGet = true)] public int Cobertura { get; set; } = 30;

        public List<SugerenciaCompra> Items { get; set; } = new();
        public decimal CostoTotal { get; set; }

        public IActionResult OnGet()
        {
            var u = _context.Usuarios.AsNoTracking().FirstOrDefault(x => x.Username == User.Identity!.Name);
            var rol = u?.Rol ?? "Pendiente";
            if (rol != "Admin" && !PermisoHelper.TienePermiso(_context, rol, "Compras", "Ordenes", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver las compras.";
                return RedirectToPage("/Index");
            }

            Items = SugerenciaComprasHelper.Generar(_context, Historial, Cobertura);
            CostoTotal = Items.Sum(i => i.CostoTotal);
            return Page();
        }
    }
}
