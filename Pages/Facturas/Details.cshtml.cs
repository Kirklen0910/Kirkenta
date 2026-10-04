using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Facturas
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Factura Factura { get; set; } = new();
        public Cliente Cliente { get; set; } = new();
        public List<DetalleFactura> Items { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public List<Pago> Pagos { get; set; } = new();
        public List<MetodoPago> MetodosPago { get; set; } = new();
        public ConfiguracionEmpresa Empresa { get; set; } = new();

        // ===== DESGLOSE ISV =====
        public ISVHelper.ResultadoISV DesgloseISV { get; set; } = new();
        public List<Impuesto> ImpuestosActivos { get; set; } = new();

        // ===== ALERTAS CAI =====
        public bool CaiPorVencer { get; set; }
        public bool CaiVencido { get; set; }
        public int DiasParaVencerCai { get; set; }
        public bool RangoCaiAgotado { get; set; }

        public IActionResult OnGet(int id)
        {
            var factura = _context.Facturas.FirstOrDefault(f => f.Id == id);
            if (factura == null)
            {
                TempData["Error"] = "Factura no encontrada";
                return RedirectToPage("/Facturas/Index");
            }

            Factura = factura;
            Cliente = _context.Clientes.FirstOrDefault(c => c.Id == factura.ClienteId) ?? new Cliente();
            Items = _context.DetalleFacturas.Where(d => d.FacturaId == id).ToList();
            Productos = _context.Productos.ToList();
            Pagos = _context.Pagos.Where(p => p.FacturaId == id).ToList();
            MetodosPago = _context.MetodosPago.ToList();
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault()
                ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };

            // ===== CARGAR DESGLOSE ISV =====
            ImpuestosActivos = ISVHelper.ObtenerTasasActivas(_context);

            var itemsParaISV = Items.Select(i => new ItemParaISV
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario,
                Descuento = i.Descuento,
                // Buscamos el ImpuestoId real del producto, no el % del detalle
                ImpuestoId = Productos.FirstOrDefault(p => p.Id == i.ProductoId)?.ImpuestoId
            }).ToList();

            DesgloseISV = ISVHelper.Calcular(_context, itemsParaISV, factura.Descuento);

            // ===== VALIDAR CAI =====
            if (!string.IsNullOrEmpty(Empresa.CAI) && Empresa.FechaLimiteEmision.HasValue)
            {
                var dias = (Empresa.FechaLimiteEmision.Value.Date - DateTime.Today).Days;
                DiasParaVencerCai = dias;
                CaiPorVencer = dias >= 0 && dias <= 30;
                CaiVencido = dias < 0;
            }

            // Validar si el rango CAI se agotó
            if (!string.IsNullOrEmpty(Empresa.RangoInicial) && !string.IsNullOrEmpty(Empresa.RangoFinal))
            {
                var facturaEnRango = EstaEnRango(factura.Numero, Empresa.RangoInicial, Empresa.RangoFinal);
                RangoCaiAgotado = !facturaEnRango;
            }

            return Page();
        }

        /// <summary>
        /// Valida si un número de factura está dentro del rango CAI autorizado.
        /// Compara solo los dígitos finales del número.
        /// </summary>
        private static bool EstaEnRango(string numeroFactura, string rangoInicial, string rangoFinal)
        {
            try
            {
                var numFactura = ExtraerDigitos(numeroFactura);
                var numInicial = ExtraerDigitos(rangoInicial);
                var numFinal = ExtraerDigitos(rangoFinal);

                if (numFactura == 0 || numInicial == 0 || numFinal == 0) return true;

                return numFactura >= numInicial && numFactura <= numFinal;
            }
            catch
            {
                return true; // Si no se puede validar, asumimos OK
            }
        }

        private static long ExtraerDigitos(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return 0;
            var soloDigitos = new string(texto.Where(char.IsDigit).ToArray());
            return long.TryParse(soloDigitos, out var num) ? num : 0;
        }
    }
}