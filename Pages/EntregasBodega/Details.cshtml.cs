using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.EntregasBodega
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public EntregaBodega Entrega { get; set; } = new();
        public Venta Venta { get; set; } = new();
        public Cliente? Cliente { get; set; }
        public Factura? Factura { get; set; }
        public string? Usuario { get; set; }
        public bool PuedeAnular { get; set; }
        public List<Fila> Filas { get; set; } = new();
        public decimal PendienteVenta { get; set; }

        public class Fila
        {
            public int ProductoId { get; set; }
            public string Producto { get; set; } = "";
            public decimal Cantidad { get; set; }
            public decimal Vendida { get; set; }
            public decimal PendienteAhora { get; set; }
        }

        private bool PuedeVer()
            => PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "EntregasBodega", "ver");

        public IActionResult OnGet(int id)
        {
            if (!PuedeVer())
            {
                TempData["Error"] = "No tienes permiso para ver entregas";
                return RedirectToPage("/Index");
            }
            if (!Cargar(id))
            {
                TempData["Error"] = "Comprobante no encontrado";
                return RedirectToPage("/EntregasBodega/Index");
            }
            return Page();
        }

        public IActionResult OnPostAnular(int id, string? motivo)
        {
            var puede = PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "EntregasBodega", "eliminar");
            if (!puede)
            {
                TempData["Error"] = "No tienes permiso para anular comprobantes";
                return RedirectToPage(new { id });
            }
            var user = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (user == null) return RedirectToPage("/Auth/Login");

            var error = EntregaBodegaHelper.Anular(_context, id, motivo, user.Id);
            if (error != null) { TempData["Error"] = error; return RedirectToPage(new { id }); }

            ActividadHelper.Registrar(_context, user.Id, "Anular entrega en bodega",
                $"Anuló el comprobante de entrega #{id}. Motivo: {motivo}", HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = "Comprobante anulado. Lo que contenía vuelve a quedar pendiente por entregar.";
            return RedirectToPage(new { id });
        }

        private bool Cargar(int id)
        {
            var e = _context.EntregasBodega.AsNoTracking().FirstOrDefault(x => x.Id == id);
            if (e == null) return false;
            Entrega = e;
            Venta = _context.Ventas.AsNoTracking().First(v => v.Id == e.VentaId);
            Cliente = Venta.ClienteId.HasValue ? _context.Clientes.AsNoTracking().FirstOrDefault(c => c.Id == Venta.ClienteId.Value) : null;
            Factura = _context.Facturas.AsNoTracking()
                .Where(f => f.VentaId == Venta.Id && f.Estado != "Anulada")
                .OrderByDescending(f => f.Id).FirstOrDefault();
            Usuario = e.UsuarioId.HasValue ? _context.Usuarios.AsNoTracking().Where(u => u.Id == e.UsuarioId.Value).Select(u => u.Username).FirstOrDefault() : null;
            PuedeAnular = PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "EntregasBodega", "eliminar");

            var lineas = EntregaBodegaHelper.Lineas(_context, e.VentaId);
            var detalles = _context.DetallesEntregaBodega.AsNoTracking().Where(d => d.EntregaBodegaId == id).ToList();
            Filas = detalles.Select(d =>
            {
                var l = lineas.FirstOrDefault(x => x.DetalleVentaId == d.DetalleVentaId);
                return new Fila
                {
                    ProductoId = d.ProductoId,
                    Producto = l?.Producto ?? "—",
                    Cantidad = d.Cantidad,
                    Vendida = l?.Vendida ?? 0,
                    PendienteAhora = l?.Pendiente ?? 0
                };
            }).ToList();
            PendienteVenta = lineas.Sum(l => l.Pendiente);
            return true;
        }
    }
}
