using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Facturas
{
    public class CreateFromVentaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateFromVentaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Venta Venta { get; set; } = new();
        public Cliente? Cliente { get; set; }
        public string NumeroPreview { get; set; } = "";

        public class InputModel
        {
            public int VentaId { get; set; }
            public DateTime? FechaVencimiento { get; set; }
            public bool AnularVenta { get; set; }
        }

        public IActionResult OnGet(int ventaId)
        {
            var venta = _context.Ventas.FirstOrDefault(v => v.Id == ventaId);
            if (venta == null)
            {
                TempData["Error"] = "Venta no encontrada";
                return RedirectToPage("/Ventas/Index");
            }

            Venta = venta;
            Cliente = venta.ClienteId.HasValue ? _context.Clientes.FirstOrDefault(c => c.Id == venta.ClienteId.Value) : null;
            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Factura");

            Input = new InputModel
            {
                VentaId = venta.Id,
                FechaVencimiento = DateTime.Now.AddDays(30)
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var venta = _context.Ventas.FirstOrDefault(v => v.Id == Input.VentaId);
            if (venta == null)
            {
                TempData["Error"] = "Venta no encontrada";
                return RedirectToPage("/Ventas/Index");
            }

            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);

            // Generar número
            var numeroFactura = NumeroDocumentoHelper.GenerarSiguiente(_context, "Factura");

            var factura = new Factura
            {
                Numero = numeroFactura,
                ClienteId = venta.ClienteId ?? 1,
                VentaId = venta.Id,
                Fecha = DateTime.Now,
                FechaVencimiento = Input.FechaVencimiento,
                Subtotal = venta.Subtotal,
                Descuento = venta.Descuento,
                Impuestos = venta.Impuestos,
                Total = venta.Total,
                Saldo = venta.Total,
                Estado = "Emitida",
                Notas = $"Generada desde venta {venta.Numero}",
                UsuarioCreoId = currentUser?.Id
            };

            _context.Facturas.Add(factura);
            _context.SaveChanges();

            // Copiar items
            var items = _context.DetalleVentas.Where(d => d.VentaId == venta.Id).ToList();
            foreach (var item in items)
            {
                _context.DetalleFacturas.Add(new DetalleFactura
                {
                    FacturaId = factura.Id,
                    ProductoId = item.ProductoId,
                    Descripcion = item.Descripcion,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Descuento = item.Descuento,
                    ImpuestoPorcentaje = item.ImpuestoPorcentaje,
                    Subtotal = item.Subtotal,
                    Total = item.Total
                });
            }

            if (Input.AnularVenta)
            {
                venta.Estado = "Anulada";
            }

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Facturar venta",
                $"Generó factura {factura.Numero} desde venta {venta.Numero}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Factura {factura.Numero} generada correctamente";
            return RedirectToPage("/Facturas/Details", new { id = factura.Id });
        }
    }
}