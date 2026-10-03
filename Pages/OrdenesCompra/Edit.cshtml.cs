using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kirkenta.Pages.OrdenesCompra
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<Proveedor> Proveedores { get; set; } = new();
        public List<Moneda> Monedas { get; set; } = new();
        public string Numero { get; set; } = "";
        public string ProductosJson { get; set; } = "[]";
        public string ItemsJson { get; set; } = "[]";

        public class InputModel
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public int ProveedorId { get; set; }
            public int MonedaId { get; set; } = 1;
            public DateTime? FechaEntregaEstimada { get; set; }
            public string Estado { get; set; } = "Borrador";
            public string? Notas { get; set; }
            public List<ItemInput> Items { get; set; } = new();
        }

        public class ItemInput
        {
            public int ProductoId { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Descuento { get; set; }
            public decimal ImpuestoPorcentaje { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "OrdenesEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar órdenes";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            var orden = _context.OrdenesCompra.FirstOrDefault(o => o.Id == id);
            if (orden == null)
            {
                TempData["Error"] = "Orden no encontrada";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            if (orden.Estado != "Borrador" && orden.Estado != "Enviada")
            {
                TempData["Error"] = "Solo se pueden editar órdenes en estado Borrador o Enviada";
                return RedirectToPage("/OrdenesCompra/Details", new { id });
            }

            var detalles = _context.DetalleOrdenesCompra.Where(d => d.OrdenCompraId == id).ToList();
            if (detalles.Any(d => d.CantidadRecibida > 0))
            {
                TempData["Error"] = "No se puede editar: la orden ya tiene recepciones registradas";
                return RedirectToPage("/OrdenesCompra/Details", new { id });
            }

            if (_context.PagosProveedor.Any(p => p.OrdenCompraId == id))
            {
                TempData["Error"] = "No se puede editar: la orden ya tiene pagos registrados";
                return RedirectToPage("/OrdenesCompra/Details", new { id });
            }

            CargarDatos();
            Numero = orden.Numero;

            Input = new InputModel
            {
                Id = orden.Id,
                Numero = orden.Numero,
                ProveedorId = orden.ProveedorId,
                MonedaId = orden.MonedaId,
                FechaEntregaEstimada = orden.FechaEntregaEstimada,
                Estado = orden.Estado,
                Notas = orden.Notas
            };

            var productos = _context.Productos
                .AsNoTracking()
                .ToDictionary(p => p.Id, p => p.Nombre);

            var itemsList = detalles.Select(d => new
            {
                productoId = d.ProductoId,
                nombre = productos.GetValueOrDefault(d.ProductoId, "—"),
                cantidad = d.Cantidad,
                precio = d.PrecioUnitario,
                descuento = d.Descuento,
                impuesto = d.ImpuestoPorcentaje
            }).ToList();

            ItemsJson = JsonSerializer.Serialize(itemsList);
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "OrdenesEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar órdenes";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            CargarDatos();

            var orden = _context.OrdenesCompra.FirstOrDefault(o => o.Id == Input.Id);
            if (orden == null)
            {
                TempData["Error"] = "Orden no encontrada";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            if (orden.Estado != "Borrador" && orden.Estado != "Enviada")
            {
                TempData["Error"] = "Solo se pueden editar órdenes en Borrador o Enviada";
                return RedirectToPage("/OrdenesCompra/Details", new { id = orden.Id });
            }

            var detallesViejos = _context.DetalleOrdenesCompra
                .Where(d => d.OrdenCompraId == orden.Id)
                .ToList();

            if (detallesViejos.Any(d => d.CantidadRecibida > 0))
            {
                TempData["Error"] = "No se puede editar: la orden ya tiene recepciones";
                return RedirectToPage("/OrdenesCompra/Details", new { id = orden.Id });
            }

            if (_context.PagosProveedor.Any(p => p.OrdenCompraId == orden.Id))
            {
                TempData["Error"] = "No se puede editar: la orden ya tiene pagos";
                return RedirectToPage("/OrdenesCompra/Details", new { id = orden.Id });
            }

            if (!ModelState.IsValid)
                return Page();

            var items = Input.Items ?? new List<ItemInput>();
            if (items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Debes agregar al menos un producto");
                return Page();
            }

            _context.DetalleOrdenesCompra.RemoveRange(detallesViejos);

            decimal subtotal = 0, descuentoTotal = 0, impuestos = 0, total = 0;
            var nuevosDetalles = new List<DetalleOrdenCompra>();

            foreach (var item in items)
            {
                var bruto = item.Cantidad * item.PrecioUnitario;
                var conDescuento = bruto - item.Descuento;
                var iva = Math.Round(conDescuento * (item.ImpuestoPorcentaje / 100m), 2);
                var totalLinea = conDescuento + iva;

                subtotal += bruto;
                descuentoTotal += item.Descuento;
                impuestos += iva;
                total += totalLinea;

                nuevosDetalles.Add(new DetalleOrdenCompra
                {
                    OrdenCompraId = orden.Id,
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    CantidadRecibida = 0,
                    PrecioUnitario = item.PrecioUnitario,
                    Descuento = item.Descuento,
                    ImpuestoPorcentaje = item.ImpuestoPorcentaje,
                    Subtotal = bruto,
                    Total = totalLinea
                });
            }

            var moneda = _context.Monedas.FirstOrDefault(m => m.Id == Input.MonedaId);

            orden.ProveedorId = Input.ProveedorId;
            orden.MonedaId = Input.MonedaId;
            orden.TipoCambio = moneda?.TipoCambio ?? 1;
            orden.FechaEntregaEstimada = Input.FechaEntregaEstimada;
            orden.Estado = Input.Estado;
            orden.Notas = Input.Notas;
            orden.Subtotal = subtotal;
            orden.Descuento = descuentoTotal;
            orden.Impuestos = impuestos;
            orden.Total = total;

            var pagosAplicados = _context.PagosProveedor
                .Where(p => p.OrdenCompraId == orden.Id)
                .Sum(p => (decimal?)p.Monto) ?? 0;

            orden.Saldo = orden.Total - pagosAplicados;
            if (orden.Saldo < 0) orden.Saldo = 0;

            foreach (var d in nuevosDetalles)
                _context.DetalleOrdenesCompra.Add(d);

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar orden de compra",
                $"Editó la orden {orden.Numero} (Total: L. {orden.Total:N2})",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Orden {orden.Numero} actualizada";
            return RedirectToPage("/OrdenesCompra/Details", new { id = orden.Id });
        }

        private void CargarDatos()
        {
            Proveedores = _context.Proveedores
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToList();

            Monedas = _context.Monedas
                .Where(m => m.Activa)
                .OrderBy(m => m.Codigo)
                .ToList();

            // 🔧 FIX: Separar la query en dos para evitar error de traducción de Pomelo.
            // 1) Cargar impuestos en un Diccionario (lookup O(1))
            var impuestosDict = _context.Impuestos
                .AsNoTracking()
                .ToDictionary(i => i.Id, i => i.Porcentaje);

            // 2) Cargar productos base a memoria
            var productosBase = _context.Productos
                .AsNoTracking()
                .Where(p => p.Activo)
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.SKU,
                    p.PrecioCompra,
                    p.ImpuestoId
                })
                .ToList();

            // 3) En memoria, unir con impuestos
            var productos = productosBase.Select(p => new
            {
                id = p.Id,
                nombre = p.Nombre,
                sku = p.SKU ?? "",
                precioCompra = p.PrecioCompra,
                impuesto = p.ImpuestoId.HasValue && impuestosDict.ContainsKey(p.ImpuestoId.Value)
                    ? impuestosDict[p.ImpuestoId.Value]
                    : 0m
            })
            .ToList();

            ProductosJson = JsonSerializer.Serialize(productos);
        }
    }
}