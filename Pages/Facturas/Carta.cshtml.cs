using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Facturas
{
    /// <summary>Factura en tamaño carta (imprimir o guardar como PDF desde el navegador).</summary>
    public class CartaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CartaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Factura Factura { get; set; } = new();
        public Cliente? Cliente { get; set; }
        public ConfiguracionEmpresa Empresa { get; set; } = new();
        public List<(DetalleFactura det, string nombre)> Items { get; set; } = new();
        public string? MetodoPago { get; set; }
        public ISVHelper.ResultadoISV Desglose { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            if (!PermisoHelper.UsuarioTieneAcceso(_context, User.Identity?.Name, "Ventas", "Facturas"))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var factura = _context.Facturas.FirstOrDefault(f => f.Id == id);
            if (factura == null)
            {
                TempData["Error"] = "Factura no encontrada";
                return RedirectToPage("/Facturas/Index");
            }

            Factura = factura;
            Cliente = _context.Clientes.FirstOrDefault(c => c.Id == factura.ClienteId);
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault() ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };

            var productos = _context.Productos.ToDictionary(p => p.Id, p => p.Nombre);
            Items = _context.DetalleFacturas.Where(d => d.FacturaId == id).ToList()
                .Select(d => (d, !string.IsNullOrWhiteSpace(d.Descripcion)
                    ? d.Descripcion!
                    : (productos.TryGetValue(d.ProductoId, out var n) ? n : $"Producto {d.ProductoId}")))
                .ToList();

            // Desglose fiscal (exento / gravado / ISV por tasa) con los porcentajes guardados en la factura
            Desglose = ISVHelper.DesglosarFactura(_context, factura, Items.Select(x => x.det));

            var pago = _context.Pagos.Where(p => p.FacturaId == id || (factura.VentaId != null && p.VentaId == factura.VentaId))
                .OrderByDescending(p => p.Id).FirstOrDefault();
            if (pago != null)
                MetodoPago = _context.MetodosPago.FirstOrDefault(m => m.Id == pago.MetodoPagoId)?.Nombre;

            return Page();
        }
    }
}
