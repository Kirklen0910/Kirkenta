using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Kirkenta.Pages.Pedidos
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<Cliente> Clientes { get; set; } = new();
        public string NumeroPreview { get; set; } = "";
        public string ProductosJson { get; set; } = "[]";

        public class InputModel
        {
            public int ClienteId { get; set; }
            public DateTime? FechaEntrega { get; set; }
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

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Pedidos", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Pedidos/Index");
            }

            Clientes = _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Pedido");
            ProductosJson = ObtenerProductosJson();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Pedidos", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Pedidos/Index");
            }

            Clientes = _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();

            if (Input.ClienteId <= 0 || Input.Items == null || Input.Items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Selecciona un cliente y al menos un producto");
                NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Pedido");
                ProductosJson = ObtenerProductosJson();
                return Page();
            }

            var numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "Pedido");

            decimal subtotal = 0, descuento = 0, impuestos = 0, total = 0;
            var detalles = new List<DetallePedido>();

            foreach (var item in Input.Items)
            {
                var st = item.Cantidad * item.PrecioUnitario;
                var stConDesc = st - item.Descuento;
                var iv = stConDesc * (item.ImpuestoPorcentaje / 100);
                var tot = stConDesc + iv;

                subtotal += st;
                descuento += item.Descuento;
                impuestos += iv;
                total += tot;

                detalles.Add(new DetallePedido
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Descuento = item.Descuento,
                    ImpuestoPorcentaje = item.ImpuestoPorcentaje,
                    Subtotal = stConDesc,
                    Total = tot
                });
            }

            var pedido = new Pedido
            {
                Numero = numero,
                ClienteId = Input.ClienteId,
                Fecha = DateTime.Now,
                FechaEntrega = Input.FechaEntrega,
                Subtotal = subtotal,
                Descuento = descuento,
                Impuestos = impuestos,
                Total = total,
                Estado = "Pendiente",
                Notas = Input.Notas,
                UsuarioCreoId = currentUser?.Id
            };

            _context.Pedidos.Add(pedido);
            _context.SaveChanges();

            foreach (var d in detalles)
            {
                d.PedidoId = pedido.Id;
                _context.DetallePedidos.Add(d);
            }
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear pedido",
                $"Creó el pedido {pedido.Numero} por L. {pedido.Total:N2}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Pedido {pedido.Numero} creado correctamente";
            return RedirectToPage("/Pedidos/Index");
        }

        private string ObtenerProductosJson()
        {
            var productos = _context.Productos
                .Where(p => p.Activo)
                .Select(p => new { p.Id, p.Nombre, p.SKU, p.Stock, p.PrecioVenta, p.ImpuestoId })
                .ToList();
            var impuestosDict = _context.Impuestos.ToDictionary(i => i.Id, i => i.Porcentaje);
            var list = productos.Select(p => new
            {
                id = p.Id,
                nombre = p.Nombre,
                sku = p.SKU,
                stock = p.Stock,
                precio = p.PrecioVenta,
                impuesto = p.ImpuestoId.HasValue && impuestosDict.ContainsKey(p.ImpuestoId.Value) ? impuestosDict[p.ImpuestoId.Value] : 0m
            }).ToList();
            return JsonSerializer.Serialize(list);
        }
    }
}