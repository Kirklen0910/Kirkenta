using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Kirkenta.Pages.Cotizaciones
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

        public List<Cliente> Clientes { get; set; } = new();
        public string Numero { get; set; } = "";
        public string ProductosJson { get; set; } = "[]";
        public string ItemsJson { get; set; } = "[]";

        public class InputModel
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public int ClienteId { get; set; }
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
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Cotizaciones", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Cotizaciones/Index");
            }

            var cotizacion = _context.Cotizaciones.FirstOrDefault(c => c.Id == id);
            if (cotizacion == null)
            {
                TempData["Error"] = "Cotización no encontrada";
                return RedirectToPage("/Cotizaciones/Index");
            }

            if (cotizacion.Estado == "Convertida")
            {
                TempData["Error"] = "No se puede editar una cotización convertida";
                return RedirectToPage("/Cotizaciones/Details", new { id });
            }

            Clientes = _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
            Numero = cotizacion.Numero;

            Input = new InputModel
            {
                Id = cotizacion.Id,
                Numero = cotizacion.Numero,
                ClienteId = cotizacion.ClienteId,
                Estado = cotizacion.Estado,
                Notas = cotizacion.Notas
            };

            // Productos para el buscador
            ProductosJson = ObtenerProductosJson();

            // Items existentes
            var detalles = _context.DetalleCotizaciones.Where(d => d.CotizacionId == id).ToList();
            var productos = _context.Productos.ToList();

            var itemsList = detalles.Select(d => new
            {
                productoId = d.ProductoId,
                nombre = productos.FirstOrDefault(p => p.Id == d.ProductoId)?.Nombre ?? "—",
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
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Cotizaciones", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Cotizaciones/Index");
            }

            Clientes = _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();

            if (!ModelState.IsValid)
            {
                Numero = Input.Numero;
                ProductosJson = ObtenerProductosJson();
                ItemsJson = "[]";
                return Page();
            }

            var cotizacion = _context.Cotizaciones.FirstOrDefault(c => c.Id == Input.Id);
            if (cotizacion == null)
            {
                TempData["Error"] = "Cotización no encontrada";
                return RedirectToPage("/Cotizaciones/Index");
            }

            // Borrar items antiguos
            var itemsViejos = _context.DetalleCotizaciones.Where(d => d.CotizacionId == cotizacion.Id);
            _context.DetalleCotizaciones.RemoveRange(itemsViejos);

            // Calcular totales nuevos
            decimal subtotal = 0, descuento = 0, impuestos = 0, total = 0;
            var nuevosDetalles = new List<DetalleCotizacion>();

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

                nuevosDetalles.Add(new DetalleCotizacion
                {
                    CotizacionId = cotizacion.Id,
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Descuento = item.Descuento,
                    ImpuestoPorcentaje = item.ImpuestoPorcentaje,
                    Subtotal = stConDesc,
                    Total = tot
                });
            }

            cotizacion.ClienteId = Input.ClienteId;
            cotizacion.Estado = Input.Estado;
            cotizacion.Notas = Input.Notas;
            cotizacion.Subtotal = subtotal;
            cotizacion.Descuento = descuento;
            cotizacion.Impuestos = impuestos;
            cotizacion.Total = total;

            foreach (var d in nuevosDetalles)
                _context.DetalleCotizaciones.Add(d);

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar cotización",
                $"Editó la cotización {cotizacion.Numero}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cotización {cotizacion.Numero} actualizada";
            return RedirectToPage("/Cotizaciones/Details", new { id = cotizacion.Id });
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