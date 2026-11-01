using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.DevolucionesProveedor
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public OrdenCompra? Orden { get; set; }
        public Proveedor? Proveedor { get; set; }
        public List<LineaVista> Lineas { get; set; } = new();
        public List<OrdenCompra> OrdenesDisponibles { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class LineaVista
        {
            public int DetalleOrdenId { get; set; }
            public string Producto { get; set; } = "";
            public decimal Recibida { get; set; }
            public decimal Devuelta { get; set; }
            public decimal Disponible => Recibida - Devuelta;
            public decimal CostoUnitario { get; set; }
        }

        public class InputModel
        {
            public int OrdenCompraId { get; set; }
            public string? Motivo { get; set; }
            public List<ItemInput> Items { get; set; } = new();
        }

        public class ItemInput
        {
            public int DetalleOrdenId { get; set; }
            public decimal Cantidad { get; set; }
        }

        private bool TienePermiso()
            => PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Compras", "DevolucionesProveedorCreate", "crear");

        public IActionResult OnGet(int? ordenId)
        {
            if (!TienePermiso())
            {
                TempData["Error"] = "No tienes permiso para registrar devoluciones a proveedores";
                return RedirectToPage("/Index");
            }

            if (ordenId.HasValue)
            {
                if (!Cargar(ordenId.Value))
                {
                    TempData["Error"] = "Orden no encontrada";
                    return RedirectToPage("/DevolucionesProveedor/Index");
                }
            }
            else
            {
                OrdenesDisponibles = _context.OrdenesCompra
                    .Where(o => o.Estado == "Recibida" || o.Estado == "RecibidaParcial" || o.Estado == "Pagada")
                    .OrderByDescending(o => o.Fecha).Take(200).ToList();
                Proveedores = _context.Proveedores.ToDictionary(p => p.Id, p => p.Nombre);
            }

            return Page();
        }

        public Dictionary<int, string> Proveedores { get; set; } = new();

        public IActionResult OnPost()
        {
            if (!TienePermiso())
            {
                TempData["Error"] = "No tienes permiso para registrar devoluciones a proveedores";
                return RedirectToPage("/Index");
            }

            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null) return RedirectToPage("/Auth/Login");

            var (dev, error) = DevolucionProveedorHelper.Crear(_context, new DevolucionProveedorHelper.Solicitud
            {
                OrdenCompraId = Input.OrdenCompraId,
                Motivo = Input.Motivo,
                UsuarioId = currentUser.Id,
                Items = Input.Items
                    .Select(i => new DevolucionProveedorHelper.ItemSolicitud { DetalleOrdenId = i.DetalleOrdenId, Cantidad = i.Cantidad })
                    .ToList()
            });

            if (dev == null)
            {
                ModelState.AddModelError(string.Empty, error ?? "No se pudo registrar la devolución");
                Cargar(Input.OrdenCompraId, conservarInput: true);
                return Page();
            }

            ActividadHelper.Registrar(
                _context,
                currentUser.Id,
                "Devolución a proveedor",
                $"Devolución {dev.Numero} por L. {dev.Total:N2} de la orden {Orden?.Numero}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Devolución {dev.Numero} registrada por L. {dev.Total:N2}.";
            return RedirectToPage("/DevolucionesProveedor/Details", new { id = dev.Id });
        }

        private bool Cargar(int ordenId, bool conservarInput = false)
        {
            var orden = _context.OrdenesCompra.FirstOrDefault(o => o.Id == ordenId);
            if (orden == null) return false;

            Orden = orden;
            Proveedor = _context.Proveedores.FirstOrDefault(p => p.Id == orden.ProveedorId);

            var detalles = _context.DetalleOrdenesCompra.Where(d => d.OrdenCompraId == ordenId).ToList();
            var ids = detalles.Select(d => d.ProductoId).ToList();
            var productos = _context.Productos.Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Nombre);
            var devuelto = DevolucionProveedorHelper.YaDevueltoPorDetalle(_context, ordenId);

            Lineas = detalles.Select(d => new LineaVista
            {
                DetalleOrdenId = d.Id,
                Producto = productos.TryGetValue(d.ProductoId, out var n) ? n : $"Producto {d.ProductoId}",
                Recibida = d.CantidadRecibida,
                Devuelta = devuelto.TryGetValue(d.Id, out var dv) ? dv : 0,
                CostoUnitario = DevolucionProveedorHelper.CostoUnitarioLocal(d, orden.TipoCambio)
            }).ToList();

            if (!conservarInput)
            {
                Input = new InputModel
                {
                    OrdenCompraId = ordenId,
                    Items = Lineas.Select(l => new ItemInput { DetalleOrdenId = l.DetalleOrdenId, Cantidad = 0 }).ToList()
                };
            }
            return true;
        }
    }
}
