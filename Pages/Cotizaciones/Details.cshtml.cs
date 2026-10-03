using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Cotizaciones
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Cotizacion Cotizacion { get; set; } = new();
        public Cliente Cliente { get; set; } = new();
        public List<DetalleCotizacion> Items { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public ConfiguracionEmpresa Empresa { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var cotizacion = _context.Cotizaciones.FirstOrDefault(c => c.Id == id);
            if (cotizacion == null)
            {
                TempData["Error"] = "Cotización no encontrada";
                return RedirectToPage("/Cotizaciones/Index");
            }

            Cotizacion = cotizacion;
            Cliente = _context.Clientes.FirstOrDefault(c => c.Id == cotizacion.ClienteId) ?? new Cliente();
            Items = _context.DetalleCotizaciones.Where(d => d.CotizacionId == id).ToList();
            Productos = _context.Productos.ToList();
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault() ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };

            return Page();
        }

        public IActionResult OnPostConvertir(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var cotizacion = _context.Cotizaciones.FirstOrDefault(c => c.Id == id);

            if (cotizacion == null)
            {
                TempData["Error"] = "Cotización no encontrada";
                return RedirectToPage("/Cotizaciones/Index");
            }

            if (cotizacion.Estado != "Aceptada")
            {
                TempData["Error"] = "Solo se pueden convertir cotizaciones aceptadas";
                return RedirectToPage("/Cotizaciones/Details", new { id });
            }

            var numeroFactura = NumeroDocumentoHelper.GenerarSiguiente(_context, "Factura");

            var factura = new Factura
            {
                Numero = numeroFactura,
                ClienteId = cotizacion.ClienteId,
                CotizacionId = cotizacion.Id,
                Fecha = DateTime.Now,
                FechaVencimiento = DateTime.Now.AddDays(30),
                Subtotal = cotizacion.Subtotal,
                Descuento = cotizacion.Descuento,
                Impuestos = cotizacion.Impuestos,
                Total = cotizacion.Total,
                Saldo = cotizacion.Total,
                Estado = "Emitida",
                Notas = $"Generada desde cotización {cotizacion.Numero}",
                UsuarioCreoId = currentUser?.Id
            };

            _context.Facturas.Add(factura);
            _context.SaveChanges();

            var items = _context.DetalleCotizaciones.Where(d => d.CotizacionId == id).ToList();
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

                var producto = _context.Productos.FirstOrDefault(p => p.Id == item.ProductoId);
                if (producto != null)
                {
                    producto.Stock -= item.Cantidad;
                }
            }

            cotizacion.Estado = "Convertida";
            cotizacion.FacturaId = factura.Id;
            cotizacion.FechaConversion = DateTime.Now;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Convertir cotización a factura",
                $"Convirtió {cotizacion.Numero} en factura {factura.Numero}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cotización convertida en factura {factura.Numero}";
            return RedirectToPage("/Facturas/Details", new { id = factura.Id });
        }
    }
}