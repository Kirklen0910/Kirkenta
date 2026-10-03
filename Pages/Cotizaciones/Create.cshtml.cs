using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Kirkenta.Pages.Cotizaciones
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
        public string ClientesJson { get; set; } = "[]";

        public class InputModel
        {
            public int ClienteId { get; set; }
            public int Validez { get; set; } = 30;
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
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Cotizaciones", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear cotizaciones";
                return RedirectToPage("/Cotizaciones/Index");
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Cotizaciones", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Cotizaciones/Index");
            }

            CargarDatos();

            if (Input.ClienteId <= 0 || Input.Items == null || Input.Items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Selecciona un cliente y al menos un producto");
                return Page();
            }

            var numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "Cotizacion");

            decimal subtotal = 0, descuento = 0, impuestos = 0, total = 0;
            var detalles = new List<DetalleCotizacion>();

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

                detalles.Add(new DetalleCotizacion
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

            var cotizacion = new Cotizacion
            {
                Numero = numero,
                ClienteId = Input.ClienteId,
                Fecha = DateTime.Now,
                FechaVencimiento = DateTime.Now.AddDays(Input.Validez),
                Subtotal = subtotal,
                Descuento = descuento,
                Impuestos = impuestos,
                Total = total,
                Estado = "Borrador",
                Notas = Input.Notas,
                Validez = Input.Validez,
                UsuarioCreoId = currentUser?.Id
            };

            _context.Cotizaciones.Add(cotizacion);
            _context.SaveChanges();

            foreach (var d in detalles)
            {
                d.CotizacionId = cotizacion.Id;
                _context.DetalleCotizaciones.Add(d);
            }
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear cotización",
                $"Creó la cotización {cotizacion.Numero} por L. {cotizacion.Total:N2}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cotización {cotizacion.Numero} creada correctamente";
            return RedirectToPage("/Cotizaciones/Index");
        }

        private void CargarDatos()
        {
            Clientes = _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Cotizacion");

            // Productos JSON
            var productos = _context.Productos
                .Where(p => p.Activo)
                .Select(p => new { p.Id, p.Nombre, p.SKU, p.Stock, p.PrecioVenta, p.ImpuestoId })
                .ToList();

            var impuestosDict = _context.Impuestos.ToDictionary(i => i.Id, i => i.Porcentaje);

            var productosList = productos.Select(p => new
            {
                id = p.Id,
                nombre = p.Nombre,
                sku = p.SKU,
                stock = p.Stock,
                precio = p.PrecioVenta,
                impuesto = p.ImpuestoId.HasValue && impuestosDict.ContainsKey(p.ImpuestoId.Value) ? impuestosDict[p.ImpuestoId.Value] : 0m
            }).ToList();

            ProductosJson = JsonSerializer.Serialize(productosList);

            // Clientes JSON para el buscador
            var clientesList = Clientes.Select(c => new
            {
                id = c.Id,
                nombre = c.Nombre,
                rtn = c.RTN ?? "",
                codigo = c.Codigo ?? ""
            }).ToList();

            ClientesJson = JsonSerializer.Serialize(clientesList);
        }
    }
}