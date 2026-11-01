using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.EntregasBodega
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Venta Venta { get; set; } = new();
        public Cliente? Cliente { get; set; }
        public List<EntregaBodegaHelper.LineaPendiente> Lineas { get; set; } = new();

        [BindProperty] public int VentaId { get; set; }
        [BindProperty] public string? RecibidoPor { get; set; }
        [BindProperty] public string? Notas { get; set; }
        [BindProperty] public List<ItemInput> Items { get; set; } = new();

        public class ItemInput
        {
            public int DetalleVentaId { get; set; }
            public decimal Cantidad { get; set; }
        }

        private bool Puede()
            => PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Create", "crear")
            || PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "Salidas", "crear");

        private bool Cargar(int ventaId)
        {
            var venta = _context.Ventas.AsNoTracking().FirstOrDefault(v => v.Id == ventaId);
            if (venta == null) return false;
            Venta = venta;
            Cliente = venta.ClienteId.HasValue ? _context.Clientes.AsNoTracking().FirstOrDefault(c => c.Id == venta.ClienteId.Value) : null;
            Lineas = EntregaBodegaHelper.Lineas(_context, ventaId);
            return true;
        }

        public IActionResult OnGet(int ventaId)
        {
            if (!Puede())
            {
                TempData["Error"] = "No tienes permiso para registrar entregas";
                return RedirectToPage("/Index");
            }
            if (!Cargar(ventaId))
            {
                TempData["Error"] = "Venta no encontrada";
                return RedirectToPage("/EntregasBodega/Index");
            }
            VentaId = ventaId;
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!Puede())
            {
                TempData["Error"] = "No tienes permiso para registrar entregas";
                return RedirectToPage("/Index");
            }
            var user = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (user == null) return RedirectToPage("/Auth/Login");

            var (entrega, error) = EntregaBodegaHelper.Crear(_context, VentaId,
                Items.Select(i => new EntregaBodegaHelper.LineaEntrega { DetalleVentaId = i.DetalleVentaId, Cantidad = i.Cantidad }).ToList(),
                RecibidoPor, Notas, user.Id);

            if (entrega == null)
            {
                _context.ChangeTracker.Clear();
                Cargar(VentaId);
                ModelState.AddModelError(string.Empty, error ?? "No se pudo registrar la entrega");
                return Page();
            }

            ActividadHelper.Registrar(_context, user.Id, "Entrega en bodega",
                $"Comprobante {entrega.Numero} de la venta {Venta.Numero}", HttpContext.Connection.RemoteIpAddress?.ToString());

            var pendiente = EntregaBodegaHelper.TotalPendiente(_context, VentaId);
            TempData["Success"] = pendiente > 0
                ? $"Comprobante {entrega.Numero} registrado. Aún falta por entregar de esta venta."
                : $"Comprobante {entrega.Numero} registrado. La venta quedó entregada por completo.";
            return RedirectToPage("/EntregasBodega/Details", new { id = entrega.Id });
        }
    }
}
