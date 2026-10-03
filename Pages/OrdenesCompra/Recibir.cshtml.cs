using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
            public List<ItemInput> Items { get; set; } = new();
        }

        public class ItemInput
        {
            public int DetalleId { get; set; }
            public int ProductoId { get; set; }
            public bool Recibir { get; set; }
            public decimal CantidadRecibir { get; set; }
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
                CantidadRecibir = d.Cantidad - d.CantidadRecibida
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
            int itemsRecibidos = 0;
            decimal cantidadTotalRecibida = 0;

            foreach (var (detalle, itemInput, cantidad) in itemsValidos)
            {
                detalle.CantidadRecibida += cantidad;

                var producto = Productos.FirstOrDefault(p => p.Id == itemInput.ProductoId);
                if (producto != null)
                {
                    producto.Stock += cantidad;

                    if (Input.RecalcularCosto)
                    {
                        // ⚠️ Convertir a moneda local usando el tipo de cambio de la orden
                        producto.PrecioCompra = detalle.PrecioUnitario * orden.TipoCambio;
                    }
                }

                itemsRecibidos++;
                cantidadTotalRecibida += cantidad;
            }

            // ==== DETERMINAR NUEVO ESTADO ====
            var todosLosDetalles = _context.DetalleOrdenesCompra
                .Where(d => d.OrdenCompraId == orden.Id)
                .ToList();

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

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Recibir mercancía",
                $"Recibió {cantidadTotalRecibida} unidades de la orden {orden.Numero}. Estado: {orden.Estado}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Mercancía recibida correctamente. Estado: {orden.Estado}";
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