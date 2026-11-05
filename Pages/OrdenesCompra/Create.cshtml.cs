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
        /// <summary>Productos que ya le compramos al proveedor (con su último precio) para agregarlos con un clic.</summary>
        public string HabitualesJson { get; set; } = "[]";

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

            /// <summary>La compra ya ocurrió (por llamada, etc.): se registra directo como recibida y entra al inventario.</summary>
            public bool YaRecibida { get; set; }

            [StringLength(40)]
            public string? NumeroFacturaProveedor { get; set; }

            public DateTime? FechaFacturaProveedor { get; set; }

            /// <summary>"Promedio" (costo promedio ponderado) o "Ultimo".</summary>
            public string MetodoCosto { get; set; } = "Promedio";

            public bool ActualizarPrecioVenta { get; set; }

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

        public IActionResult OnGet(int? proveedorId)
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
            if (proveedorId.HasValue)
            {
                var prov = Proveedores.FirstOrDefault(p => p.Id == proveedorId.Value);
                if (prov != null)
                {
                    Input.ProveedorId = prov.Id;
                    if (prov.MonedaId > 0 && Monedas.Any(m => m.Id == prov.MonedaId)) Input.MonedaId = prov.MonedaId;
                }
            }
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

            if (Input.YaRecibida)
            {
                Input.Estado = "Recibida";
                if (string.IsNullOrWhiteSpace(Input.NumeroFacturaProveedor))
                    ModelState.AddModelError(string.Empty, "Indica el número de factura del proveedor de la compra ya recibida");
                else if (Input.ProveedorId > 0)
                {
                    var repetida = FacturaProveedorRepetida.OrdenConLaFactura(_context, Input.ProveedorId, Input.NumeroFacturaProveedor);
                    if (repetida != null)
                        ModelState.AddModelError(string.Empty, $"Esa factura ya fue cargada en la orden {repetida}");
                }
            }
            else if (Input.Estado != "Borrador" && Input.Estado != "Enviada")
                ModelState.AddModelError(string.Empty, "El estado inicial solo puede ser Borrador o Enviada");

            if (Input.ProveedorId > 0 && !_context.Proveedores.Any(p => p.Id == Input.ProveedorId && p.Activo))
                ModelState.AddModelError(string.Empty, "El proveedor no existe o está inactivo");

            if (items.Count > 0)
            {
                var errLineas = LineasDocumentoHelper.Validar(_context, items
                    .Select(i => new LineasDocumentoHelper.Linea(i.ProductoId, i.Cantidad, i.PrecioUnitario, i.Descuento, i.ImpuestoPorcentaje))
                    .ToList(), soloActivos: false);
                if (errLineas != null) ModelState.AddModelError(string.Empty, errLineas);
            }

            if (!ModelState.IsValid)
                return Page();

            using var tx = _context.Database.BeginTransaction();
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
                    CantidadRecibida = Input.YaRecibida ? item.Cantidad : 0,
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
                Notas = ConstruirNotas(),
                FechaRecepcion = Input.YaRecibida ? DateTime.Now : null,
                UsuarioRecibioId = Input.YaRecibida ? currentUser?.Id : null,
                UsuarioCreoId = currentUser?.Id,
                EmpresaId = EmpresaActual.Id
            };

            _context.OrdenesCompra.Add(orden);
            _context.SaveChanges();

            foreach (var d in detalles)
            {
                d.OrdenCompraId = orden.Id;
                _context.DetalleOrdenesCompra.Add(d);
            }
            _context.SaveChanges();

            var cambiosCosto = new List<string>();
            if (Input.YaRecibida)
            {
                // Igual que una recepción normal: kardex de entrada + costo promedio ponderado (o último costo)
                foreach (var d in detalles)
                {
                    var producto = _context.Productos.FirstOrDefault(p => p.Id == d.ProductoId);
                    if (producto == null) continue;
                    var costoNeto = d.Cantidad > 0 ? Math.Max(0m, (d.Cantidad * d.PrecioUnitario - d.Descuento) / d.Cantidad) : d.PrecioUnitario;
                    var costoEntrada = costoNeto * orden.TipoCambio;
                    var stockPrevio = producto.Stock;
                    var costoPrevio = producto.PrecioCompra;
                    var precioVentaPrevio = producto.PrecioVenta;
                    var precioMayoristaPrevio = producto.PrecioMayorista;

                    InventarioHelper.Registrar(_context, producto, d.Cantidad, "Compra", orden.Numero, orden.Id,
                        currentUser?.Id, costoEntrada, "Compra registrada como ya recibida");

                    producto.PrecioCompra = Input.MetodoCosto == "Ultimo"
                        ? Math.Round(costoEntrada, 4)
                        : InventarioHelper.CostoPromedio(stockPrevio, costoPrevio, d.Cantidad, costoEntrada);

                    if (Input.ActualizarPrecioVenta && costoPrevio > 0 && precioVentaPrevio > 0 && producto.PrecioCompra != costoPrevio)
                    {
                        var factor = producto.PrecioCompra / costoPrevio;
                        producto.PrecioVenta = Math.Round(precioVentaPrevio * factor, 2);
                        if (precioMayoristaPrevio > 0) producto.PrecioMayorista = Math.Round(precioMayoristaPrevio * factor, 2);
                    }
                    if (producto.PrecioCompra != costoPrevio)
                        cambiosCosto.Add($"{producto.Nombre}: costo L. {costoPrevio:N2} → L. {producto.PrecioCompra:N2}");
                }
                _context.SaveChanges();
            }
            tx.Commit();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear orden de compra",
                $"Creó la orden {orden.Numero} por L. {orden.Total:N2} al proveedor {nombreProveedor}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = Input.YaRecibida
                ? $"Orden {orden.Numero} registrada como recibida: el inventario y el costo ya se actualizaron."
                    + (cambiosCosto.Count > 0 ? " Cambios: " + string.Join("; ", cambiosCosto) : "")
                : $"Orden {orden.Numero} creada correctamente";
            return RedirectToPage("/OrdenesCompra/Details", new { id = orden.Id });
        }

        private string? ConstruirNotas()
        {
            var partes = new List<string>();
            if (Input.YaRecibida)
            {
                var f = $"Factura del proveedor N° {Input.NumeroFacturaProveedor?.Trim()}";
                if (Input.FechaFacturaProveedor.HasValue) f += $" (fecha {Input.FechaFacturaProveedor.Value:dd/MM/yyyy})";
                partes.Add(f + ". Compra registrada como ya recibida.");
            }
            if (!string.IsNullOrWhiteSpace(Input.Notas)) partes.Add(Input.Notas.Trim());
            var s = string.Join("\n", partes);
            if (s.Length == 0) return null;
            return s.Length > 500 ? s.Substring(0, 500) : s;
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

            // Productos que ya se le compraron a cada proveedor, con el último precio pagado
            var hab = (from d in _context.DetalleOrdenesCompra.AsNoTracking()
                       join o in _context.OrdenesCompra.AsNoTracking() on d.OrdenCompraId equals o.Id
                       where o.Estado != "Cancelada" && o.Estado != "Borrador"
                       select new { o.ProveedorId, d.ProductoId, d.PrecioUnitario, o.Fecha, d.Id }).ToList()
                .GroupBy(x => new { x.ProveedorId, x.ProductoId })
                .Select(g => g.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id).First())
                .GroupBy(x => x.ProveedorId)
                .SelectMany(g => g.OrderByDescending(x => x.Fecha).Take(25))
                .Select(x => new { proveedorId = x.ProveedorId, productoId = x.ProductoId, precio = x.PrecioUnitario })
                .ToList();
            HabitualesJson = JsonSerializer.Serialize(hab);
        }
    }
}