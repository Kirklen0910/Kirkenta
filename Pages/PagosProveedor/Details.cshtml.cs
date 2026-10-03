using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.PagosProveedor
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Models.PagoProveedor Pago { get; set; } = new();
        public Proveedor Proveedor { get; set; } = new();
        public Models.OrdenCompra? Orden { get; set; }
        public MetodoPago? MetodoPago { get; set; }
        public ConfiguracionEmpresa Empresa { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "Pagos", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver este pago";
                return RedirectToPage("/PagosProveedor/Index");
            }

            var pago = _context.PagosProveedor.FirstOrDefault(p => p.Id == id);
            if (pago == null)
            {
                TempData["Error"] = "Pago no encontrado";
                return RedirectToPage("/PagosProveedor/Index");
            }

            Pago = pago;
            Proveedor = _context.Proveedores.FirstOrDefault(p => p.Id == pago.ProveedorId) ?? new Proveedor();
            Orden = pago.OrdenCompraId.HasValue
                ? _context.OrdenesCompra.FirstOrDefault(o => o.Id == pago.OrdenCompraId.Value)
                : null;
            MetodoPago = _context.MetodosPago.FirstOrDefault(m => m.Id == pago.MetodoPagoId);
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault()
                ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };

            return Page();
        }
    }
}