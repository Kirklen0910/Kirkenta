using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Kirkenta.Pages.OrdenesCompra
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

        public List<Proveedor> Proveedores { get; set; } = new();
        public List<Moneda> Monedas { get; set; } = new();
        public string NumeroPreview { get; set; } = "";
        public string ProductosJson { get; set; } = "[]";

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Debes seleccionar un proveedor")]
            public int ProveedorId { get; set; }

            public int MonedaId { get; set; } = 1;

            public DateTime? FechaEntregaEstimada { get; set; }

            [Required]
            public string Estado { get; set; } = "Borrador";

            [StringLength(500)]
            public string? Notas { get; set; }

            public List<ItemInput> Items { get; set; } = new();
        }

        public class ItemInput
        {
            [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
            public int ProductoId { get; set; }

            [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
            public decimal Cantidad { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
            public decimal PrecioUnitario { get; set; }

            [Range(0, double.MaxValue)]
            public decimal Descuento { get; set; }

            [Range(0, 100)]
            public decimal ImpuestoPorcentaje { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "OrdenesCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear órdenes de compra";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "OrdenesCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear órdenes de compra";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            CargarDatos();

            var items = Input.Items ?? new List<ItemInput>();

            if (Input.ProveedorId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Debes seleccionar un proveedor");
            }

            if (items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Debes agregar al menos un producto");
            }
            else
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item.ProductoId <= 0)
                        ModelState.AddModelError(string.Empty, $"Producto #{i + 1}: producto inválido");
                    if (item.Cantidad <= 0)
                        ModelState.AddModelError(string.Empty, $"Producto #{i + 1}: cantidad debe ser mayor a 0");
                    if (item.PrecioUnitario < 0)
                        ModelState.AddModelError(string.Empty, $"Producto #{i + 1}: precio no puede ser negativo");

                    var brutoItem = item.Cantidad * item.PrecioUnitario;
                    if (item.Descuento > brutoItem)
                        ModelState.AddModelError(string.Empty, $"Producto #{i + 1}: descuento no puede superar el subtotal");
                }
            }

            if (!ModelState.IsValid)
                return Page();

            var numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "OrdenCompra");

            decimal subtotal = 0, descuentoTotal = 0, impuestos = 0, total = 0;
            var detalles = new List<DetalleOrdenCompra>();

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

                detalles.Add(new DetalleOrdenCompra
                {
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
            var proveedor = _context.Proveedores.FirstOrDefault(p => p.Id == Input.ProveedorId);
            var nombreProveedor = proveedor?.Nombre ?? "—";

            var orden = new Models.OrdenCompra
            {
                Numero = numero,
                ProveedorId = Input.ProveedorId,
                Fecha = DateTime.Now,
                FechaEntregaEstimada = Input.FechaEntregaEstimada,
                Estado = string.IsNullOrWhiteSpace(Input.Estado) ? "Borrador" : Input.Estado,
                Subtotal = subtotal,
                Descuento = descuentoTotal,
                Impuestos = impuestos,
                Total = total,
                Saldo = total,
                MonedaId = Input.MonedaId,
                TipoCambio = moneda?.TipoCambio ?? 1,
                Notas = Input.Notas,
                UsuarioCreoId = currentUser?.Id,
                EmpresaId = 1
            };

            _context.OrdenesCompra.Add(orden);
            _context.SaveChanges();

            foreach (var d in detalles)
            {
                d.OrdenCompraId = orden.Id;
                _context.DetalleOrdenesCompra.Add(d);
            }
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear orden de compra",
                $"Creó la orden {orden.Numero} por L. {orden.Total:N2} al proveedor {nombreProveedor}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Orden {orden.Numero} creada correctamente";
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

            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "OrdenCompra");

            // 🔧 FIX: Cargar todo a memoria primero, luego hacer el Join en LINQ to Objects
            // EF Core + Pomelo (MySQL) no puede traducir impuestos.Where(...) dentro del Select.

            var impuestosDict = _context.Impuestos
                .AsNoTracking()
                .ToDictionary(i => i.Id, i => i.Porcentaje);

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
                .ToList();  // ⬅️ Aquí ya estamos en memoria

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