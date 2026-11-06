using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Kirkenta.Services;
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
        public bool PuedeAnular { get; set; }
        public bool Anulado => Pago.Estado == PagoProveedorService.EstadoAnulado;
        public string? AnuladoPor { get; set; }

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

            PuedeAnular = !Anulado && TienePermisoAnular(currentRol);
            AnuladoPor = pago.UsuarioAnuloId.HasValue
                ? _context.Usuarios.FirstOrDefault(u => u.Id == pago.UsuarioAnuloId.Value)?.Username
                : null;

            return Page();
        }

        public IActionResult OnPostAnular(int id, string? motivo)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!TienePermisoAnular(currentRol))
            {
                TempData["Error"] = "No tienes permiso para anular pagos a proveedores";
                return RedirectToPage("/PagosProveedor/Details", new { id });
            }

            PagoProveedorService.Resultado r;
            try
            {
                r = PagoProveedorService.Anular(_context, id, motivo, currentUser?.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PagosProveedor/Details] Error al anular: {ex.Message}");
                TempData["Error"] = "No se pudo anular el pago. Intenta de nuevo.";
                return RedirectToPage("/PagosProveedor/Details", new { id });
            }

            if (!r.Ok)
            {
                TempData["Error"] = r.Error;
                return RedirectToPage("/PagosProveedor/Details", new { id });
            }

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Anular pago a proveedor",
                $"Anuló el pago {r.Pago!.Numero} por L. {r.Pago.Monto:N2}"
                    + (r.Orden != null ? $" de la orden {r.Orden.Numero}" : "")
                    + $". Motivo: {r.Pago.MotivoAnulacion}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Pago {r.Pago.Numero} anulado";
            return RedirectToPage("/PagosProveedor/Details", new { id });
        }

        private bool TienePermisoAnular(string rol) =>
            rol == "Admin" || PermisoHelper.TienePermiso(_context, rol, "Compras", "PagosCreate", "eliminar");
    }
}