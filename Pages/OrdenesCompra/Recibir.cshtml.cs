using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.OrdenesCompra
{
    public class RecibirModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RecibirModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Models.OrdenCompra Orden { get; set; } = new();
        public Proveedor Proveedor { get; set; } = new();
        public List<DetalleOrdenCompra> Items { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();

        public class InputModel
        {
            public int OrdenId { get; set; }
            public string? Notas { get; set; }
            public bool RecalcularCosto { get; set; } = true;
            /// <summary>"Promedio" (costo promedio ponderado) o "Ultimo" (el costo de esta compra).</summary>
            public string MetodoCosto { get; set; } = "Promedio";
            /// <summary>Si es true, el precio de venta se reajusta manteniendo el margen que tenía sobre el costo anterior.</summary>
            public bool ActualizarPrecioVenta { get; set; }
            public List<ItemInput> Items { get; set; } = new();
        }

        public class ItemInput
        {
            public int DetalleId { get; set; }
            public int ProductoId { get; set; }
            public bool Recibir { get; set; }
            public decimal CantidadRecibir { get; set; }
            /// <summary>Precio unitario realmente facturado por el proveedor (puede ser menor o mayor al pedido).</summary>
            public decimal PrecioUnitario { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "OrdenesRecibir", "editar"))
            {
                TempData["Error"] = "No tienes permiso para recibir mercancía";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            var orden = _context.OrdenesCompra.FirstOrDefault(o => o.Id == id);
            if (orden == null)
            {
                TempData["Error"] = "Orden no encontrada";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            if (orden.Estado != "Enviada" && orden.Estado != "RecibidaParcial")
            {
                TempData["Error"] = "Solo se pueden recibir órdenes en estado Enviada o Recibida Parcial";
                return RedirectToPage("/OrdenesCompra/Details", new { id });
            }

            CargarDatos(orden);
            Input.OrdenId = orden.Id;

            // Pre-poblar items con cantidad pendiente
            Input.Items = Items.Select(d => new ItemInput
            {
                DetalleId = d.Id,
                ProductoId = d.ProductoId,
                Recibir = d.Cantidad - d.CantidadRecibida > 0,
                CantidadRecibir = d.Cantidad - d.CantidadRecibida,
                PrecioUnitario = d.PrecioUnitario
            }).ToList();

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "OrdenesRecibir", "editar"))
            {
                TempData["Error"] = "No tienes permiso para recibir mercancía";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            var orden = _context.OrdenesCompra.FirstOrDefault(o => o.Id == Input.OrdenId);
            if (orden == null)
            {
                TempData["Error"] = "Orden no encontrada";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            if (orden.Estado != "Enviada" && orden.Estado != "RecibidaParcial")
            {
                TempData["Error"] = "Esta orden ya no puede recibir mercancía";
                return RedirectToPage("/OrdenesCompra/Details", new { id = orden.Id });
            }

            CargarDatos(orden);

            // ==== VALIDAR CADA ITEM ====
            var itemsValidos = new List<(DetalleOrdenCompra detalle, ItemInput input, decimal cantidad)>();

            for (int i = 0; i < Input.Items.Count; i++)
            {
                var itemInput = Input.Items[i];
                if (!itemInput.Recibir) continue;

                var detalle = Items.FirstOrDefault(d => d.Id == itemInput.DetalleId);
                if (detalle == null)
                {
                    ModelState.AddModelError(string.Empty, $"Item #{i + 1}: no encontrado");
                    continue;
                }

                var pendiente = detalle.Cantidad - detalle.CantidadRecibida;

                if (itemInput.CantidadRecibir <= 0)
                {
                    ModelState.AddModelError(string.Empty, $"Item #{i + 1}: la cantidad a recibir debe ser mayor a 0");
                    continue;
                }

                if (itemInput.PrecioUnitario < 0)
                {
                    ModelState.AddModelError(string.Empty, $"Item #{i + 1}: el precio unitario no puede ser negativo");
                    continue;
                }

                if (itemInput.CantidadRecibir > pendiente)
                {
                    ModelState.AddModelError(string.Empty, $"Item #{i + 1}: no puedes recibir más de lo pendiente ({pendiente})");
                    continue;
                }

                itemsValidos.Add((detalle, itemInput, itemInput.CantidadRecibir));
            }

            if (itemsValidos.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Debes seleccionar al menos un producto para recibir");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // ==== PROCESAR RECEPCIÓN ====
            using var tx = _context.Database.BeginTransaction();

            // Bloqueo de la orden y revalidación con datos frescos: dos personas recibiendo la misma orden a la vez
            // ya no pueden meter dos veces la mercadería al inventario.
            if (_context.Database.IsRelational())
            {
                _context.OrdenesCompra.Where(o => o.Id == orden.Id)
                    .ExecuteUpdate(u => u.SetProperty(o => o.Estado, o => o.Estado));
                _context.Entry(orden).Reload();

                var conflicto = orden.Estado != "Enviada" && orden.Estado != "RecibidaParcial";
                foreach (var (detalle, _, cantidad) in itemsValidos)
                {
                    _context.Entry(detalle).Reload();
                    if (cantidad > detalle.Cantidad - detalle.CantidadRecibida) conflicto = true;
                }
                if (conflicto)
                {
                    tx.Rollback();
                    _context.ChangeTracker.Clear();
                    TempData["Error"] = "Esta orden se recibió mientras llenabas el formulario (otra persona ya registró la recepción). Revisa el detalle de la orden.";
                    return RedirectToPage("/OrdenesCompra/Details", new { id = Input.OrdenId });
                }

                foreach (var pid in itemsValidos.Select(x => x.detalle.ProductoId).Distinct())
                {
                    var p = Productos.FirstOrDefault(x => x.Id == pid);
                    if (p != null) _context.Entry(p).Reload();
                }
            }

            int itemsRecibidos = 0;
            var cambiosCosto = new List<string>();
            decimal cantidadTotalRecibida = 0;
            var cambiosPrecio = new List<string>();
            var totalAntes = orden.Total;
            bool ordenModificada = false;

            foreach (var (detalle, itemInput, cantidad) in itemsValidos)
            {
                detalle.CantidadRecibida += cantidad;

                var producto = Productos.FirstOrDefault(p => p.Id == detalle.ProductoId);

                // Precio realmente facturado: si cambió, la línea de la orden se corrige (y con ella el total
                // y el saldo por pagar al proveedor). Este precio es el que alimenta el costo del producto.
                var precioRecibido = Math.Round(itemInput.PrecioUnitario, 4);
                if (Math.Abs(precioRecibido - detalle.PrecioUnitario) > 0.00005m)
                {
                    cambiosPrecio.Add($"{producto?.Nombre ?? "Producto"}: precio {detalle.PrecioUnitario:N2} → {precioRecibido:N2}");
                    detalle.PrecioUnitario = precioRecibido;
                    var bruto = detalle.Cantidad * precioRecibido;
                    var conDescuento = Math.Max(0m, bruto - detalle.Descuento);
                    var iva = Math.Round(conDescuento * (detalle.ImpuestoPorcentaje / 100m), 2);
                    detalle.Subtotal = bruto;
                    detalle.Total = conDescuento + iva;
                    ordenModificada = true;
                }

                // Costo unitario de entrada (neto de descuento y sin ISV), en moneda local
                var costoNeto = detalle.Cantidad > 0
                    ? Math.Max(0m, (detalle.Cantidad * detalle.PrecioUnitario - detalle.Descuento) / detalle.Cantidad)
                    : detalle.PrecioUnitario;
                var costoEntrada = costoNeto * orden.TipoCambio;

                if (producto != null)
                {
                    var stockPrevio = producto.Stock;
                    var costoPrevio = producto.PrecioCompra;

                    // Kardex: entrada por compra al costo de la orden (moneda local)
                    InventarioHelper.Registrar(_context, producto, cantidad, "Compra",
                        orden.Numero, orden.Id, currentUser?.Id,
                        costoEntrada, "Recepción de orden de compra");

                    if (Input.RecalcularCosto)
                    {
                        // El costo de entrada ya viene en moneda local (tipo de cambio de la orden)
                        var precioVentaPrevio = producto.PrecioVenta;
                        var precioMayoristaPrevio = producto.PrecioMayorista;

                        // Promedio ponderado (por defecto) o último costo pagado
                        producto.PrecioCompra = Input.MetodoCosto == "Ultimo"
                            ? Math.Round(costoEntrada, 4)
                            : InventarioHelper.CostoPromedio(stockPrevio, costoPrevio, cantidad, costoEntrada);

                        // Reajuste del precio de venta manteniendo el mismo margen (proporción venta/costo)
                        if (Input.ActualizarPrecioVenta && costoPrevio > 0 && precioVentaPrevio > 0
                            && producto.PrecioCompra != costoPrevio)
                        {
                            var factor = producto.PrecioCompra / costoPrevio;
                            producto.PrecioVenta = Math.Round(precioVentaPrevio * factor, 2);
                            if (precioMayoristaPrevio > 0)
                                producto.PrecioMayorista = Math.Round(precioMayoristaPrevio * factor, 2);
                        }

                        if (producto.PrecioCompra != costoPrevio || producto.PrecioVenta != precioVentaPrevio)
                        {
                            cambiosCosto.Add($"{producto.Nombre}: costo L. {costoPrevio:N2} → L. {producto.PrecioCompra:N2}"
                                + (producto.PrecioVenta != precioVentaPrevio
                                    ? $", precio L. {precioVentaPrevio:N2} → L. {producto.PrecioVenta:N2}" : ""));
                        }
                    }
                }

                itemsRecibidos++;
                cantidadTotalRecibida += cantidad;
            }

            // ==== DETERMINAR NUEVO ESTADO ====
            var todosLosDetalles = _context.DetalleOrdenesCompra
                .Where(d => d.OrdenCompraId == orden.Id)
                .ToList();

            if (ordenModificada)
            {
                orden.Subtotal = todosLosDetalles.Sum(d => d.Subtotal);
                orden.Descuento = todosLosDetalles.Sum(d => d.Descuento);
                orden.Impuestos = todosLosDetalles.Sum(d => Math.Max(0m, d.Total - (d.Subtotal - d.Descuento)));
                orden.Total = todosLosDetalles.Sum(d => d.Total);
                // El saldo sigue el cambio del total (lo ya pagado o devuelto se conserva)
                orden.Saldo = Math.Max(0m, orden.Saldo + (orden.Total - totalAntes));
                orden.Notas = (string.IsNullOrWhiteSpace(orden.Notas) ? "" : orden.Notas + "\n")
                    + $"[Recepción {DateTime.Now:dd/MM/yyyy HH:mm}] Precio ajustado a la factura del proveedor — " + string.Join("; ", cambiosPrecio)
                    + $". Total: L. {totalAntes:N2} → L. {orden.Total:N2}";
            }

            bool todoRecibido = todosLosDetalles.All(d => d.CantidadRecibida >= d.Cantidad);

            if (todoRecibido)
            {
                orden.Estado = "Recibida";
                orden.FechaRecepcion = DateTime.Now;
            }
            else
            {
                orden.Estado = "RecibidaParcial";
            }

            orden.UsuarioRecibioId = currentUser?.Id;

            // Nota de recepción
            if (!string.IsNullOrWhiteSpace(Input.Notas))
            {
                var notaExistente = orden.Notas ?? "";
                orden.Notas = string.IsNullOrWhiteSpace(notaExistente)
                    ? $"[Recepción {DateTime.Now:dd/MM/yyyy HH:mm}] {Input.Notas}"
                    : $"{notaExistente}\n[Recepción {DateTime.Now:dd/MM/yyyy HH:mm}] {Input.Notas}";
            }

            _context.SaveChanges();
            tx.Commit();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Recibir mercancía",
                $"Recibió {cantidadTotalRecibida} unidades de la orden {orden.Numero}. Estado: {orden.Estado}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Mercancía recibida correctamente. Estado: {orden.Estado}"
                + (cambiosPrecio.Count > 0 ? $". Total de la orden ajustado a L. {orden.Total:N2}" : "")
                + (cambiosCosto.Count > 0 ? ". Cambios: " + string.Join("; ", cambiosCosto) : "");
            return RedirectToPage("/OrdenesCompra/Details", new { id = orden.Id });
        }

        private void CargarDatos(Models.OrdenCompra orden)
        {
            Orden = orden;
            Proveedor = _context.Proveedores.FirstOrDefault(p => p.Id == orden.ProveedorId) ?? new Proveedor();
            Items = _context.DetalleOrdenesCompra.Where(d => d.OrdenCompraId == orden.Id).ToList();
            Productos = _context.Productos.ToList();
        }
    }
}