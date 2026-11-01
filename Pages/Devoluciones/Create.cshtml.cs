using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Devoluciones
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Venta? Venta { get; set; }
        public Cliente? Cliente { get; set; }
        public List<LineaVista> Lineas { get; set; } = new();
        public List<MetodoPago> MetodosPago { get; set; } = new();

        [BindProperty]
        public string? NumeroBuscar { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class LineaVista
        {
            public int DetalleVentaId { get; set; }
            public string Producto { get; set; } = "";
            public decimal Vendida { get; set; }
            public decimal Devuelta { get; set; }
            public decimal Disponible => Vendida - Devuelta;
            public decimal PrecioDevolucion { get; set; }
        }

        public class InputModel
        {
            public int VentaId { get; set; }
            public string? Motivo { get; set; }
            public bool ReingresaInventario { get; set; } = true;
            public string? FormaReembolso { get; set; } = "Efectivo";
            public List<ItemInput> Items { get; set; } = new();
        }

        public class ItemInput
        {
            public int DetalleVentaId { get; set; }
            public decimal Cantidad { get; set; }
        }

        private bool TienePermiso()
            => PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Devoluciones", "crear");

        public IActionResult OnGet(int? ventaId)
        {
            if (!TienePermiso())
            {
                TempData["Error"] = "No tienes permiso para registrar devoluciones";
                return RedirectToPage("/Index");
            }

            if (ventaId.HasValue && !Cargar(ventaId.Value))
            {
                TempData["Error"] = "Venta no encontrada";
                return RedirectToPage("/Devoluciones/Index");
            }

            return Page();
        }

        public IActionResult OnPostBuscar()
        {
            if (!TienePermiso()) return RedirectToPage("/Index");

            var num = (NumeroBuscar ?? "").Trim();
            var venta = string.IsNullOrEmpty(num) ? null : _context.Ventas.FirstOrDefault(v => v.Numero == num);
            if (venta == null)
            {
                TempData["Error"] = "No se encontró una venta con ese número";
                return RedirectToPage("/Devoluciones/Create");
            }
            return RedirectToPage("/Devoluciones/Create", new { ventaId = venta.Id });
        }

        public IActionResult OnPost()
        {
            if (!TienePermiso())
            {
                TempData["Error"] = "No tienes permiso para registrar devoluciones";
                return RedirectToPage("/Index");
            }

            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null) return RedirectToPage("/Auth/Login");

            var (dev, error) = DevolucionVentaHelper.Crear(_context, new DevolucionVentaHelper.Solicitud
            {
                VentaId = Input.VentaId,
                Motivo = Input.Motivo,
                ReingresaInventario = Input.ReingresaInventario,
                FormaReembolso = Input.FormaReembolso,
                UsuarioId = currentUser.Id,
                Items = Input.Items
                    .Select(i => new DevolucionVentaHelper.ItemSolicitud { DetalleVentaId = i.DetalleVentaId, Cantidad = i.Cantidad })
                    .ToList()
            });

            if (dev == null)
            {
                ModelState.AddModelError(string.Empty, error ?? "No se pudo registrar la devolución");
                Cargar(Input.VentaId, conservarInput: true);
                return Page();
            }

            ActividadHelper.Registrar(
                _context,
                currentUser.Id,
                "Registrar devolución",
                $"Devolución {dev.Numero} por L. {dev.Total:N2} (venta {Venta?.Numero}). Reembolso: L. {dev.MontoReembolsado:N2}, aplicado a saldo: L. {dev.MontoAplicadoSaldo:N2}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Devolución {dev.Numero} registrada por L. {dev.Total:N2}.";
            return RedirectToPage("/Devoluciones/Details", new { id = dev.Id });
        }

        private bool Cargar(int ventaId, bool conservarInput = false)
        {
            var venta = _context.Ventas.FirstOrDefault(v => v.Id == ventaId);
            if (venta == null) return false;

            Venta = venta;
            Cliente = venta.ClienteId.HasValue ? _context.Clientes.FirstOrDefault(c => c.Id == venta.ClienteId.Value) : null;
            MetodosPago = _context.MetodosPago.Where(m => m.Activo).ToList();

            var detalles = _context.DetalleVentas.Where(d => d.VentaId == ventaId).ToList();
            var idsProductos = detalles.Select(d => d.ProductoId).ToList();
            var productos = _context.Productos
                .Where(p => idsProductos.Contains(p.Id))
                .ToDictionary(p => p.Id, p => p.Nombre);
            var devuelto = DevolucionVentaHelper.YaDevueltoPorDetalle(_context, ventaId);
            var factor = DevolucionVentaHelper.FactorProrrateo(venta, detalles);

            Lineas = detalles.Select(d => new LineaVista
            {
                DetalleVentaId = d.Id,
                Producto = productos.TryGetValue(d.ProductoId, out var n) ? n : (d.Descripcion ?? $"Producto {d.ProductoId}"),
                Vendida = d.Cantidad,
                Devuelta = devuelto.TryGetValue(d.Id, out var dv) ? dv : 0,
                PrecioDevolucion = DevolucionVentaHelper.PrecioUnitarioDevolucion(d, factor)
            }).ToList();

            if (!conservarInput)
            {
                Input = new InputModel
                {
                    VentaId = ventaId,
                    Items = Lineas.Select(l => new ItemInput { DetalleVentaId = l.DetalleVentaId, Cantidad = 0 }).ToList()
                };
            }

            return true;
        }
    }
}
