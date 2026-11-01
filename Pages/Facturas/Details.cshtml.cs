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

        public ISVHelper.ResultadoISV DesgloseISV { get; set; } = new();
        public List<Impuesto> ImpuestosActivos { get; set; } = new();

        public bool FiscalActiva { get; set; }
        public bool PuedeAnular { get; set; }
        public bool CaiPorVencer { get; set; }
        public bool CaiVencido { get; set; }
        public int DiasParaVencerCai { get; set; }
        public bool RangoCaiAgotado { get; set; }

        public IActionResult OnGet(int id)
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Facturas", "ver")))
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
            PuedeAnular = factura.Estado != "Anulada" && !factura.VentaId.HasValue
                && PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Facturas", "eliminar");
            Cliente = _context.Clientes.FirstOrDefault(c => c.Id == factura.ClienteId) ?? new Cliente();
            Items = _context.DetalleFacturas.Where(d => d.FacturaId == id).ToList();
            Productos = _context.Productos.ToList();
            Pagos = _context.Pagos.Where(p => p.FacturaId == id).ToList();
            MetodosPago = _context.MetodosPago.ToList();
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault()
                ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };

            ImpuestosActivos = ISVHelper.ObtenerTasasActivas(_context);

            // Desglose fiscal con los porcentajes guardados en la factura (no los del producto de hoy)
            DesgloseISV = ISVHelper.DesglosarFactura(_context, factura, Items);

            FiscalActiva = Empresa.FacturacionFiscalActiva;

            // Alertas solo si la facturación fiscal está activa (si no, no hay mensajes de CAI)
            if (FiscalActiva && !string.IsNullOrEmpty(Empresa.CAI) && Empresa.FechaLimiteEmision.HasValue)
            {
                var dias = (Empresa.FechaLimiteEmision.Value.Date - DateTime.Today).Days;
                DiasParaVencerCai = dias;
                CaiPorVencer = dias >= 0 && dias <= 30;
                CaiVencido = dias < 0;
            }

            // Se compara contra el rango con el que se emitió la factura (snapshot), no el actual
            if (FiscalActiva && !string.IsNullOrEmpty(factura.RangoInicial) && !string.IsNullOrEmpty(factura.RangoFinal))
            {
                var facturaEnRango = EstaEnRango(factura.Numero, factura.RangoInicial, factura.RangoFinal);
                RangoCaiAgotado = !facturaEnRango;
            }

            return Page();
        }

        public IActionResult OnPostAnular(int id, string? motivo)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Facturas", "eliminar"))
            {
                TempData["Error"] = "No tienes permiso para anular facturas";
                return RedirectToPage("/Facturas/Details", new { id });
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var (ok, error) = AnulacionFacturaHelper.Anular(_context, id, motivo ?? "", usuario?.Id ?? 0);
            if (!ok)
            {
                TempData["Error"] = error ?? "No se pudo anular la factura.";
                return RedirectToPage("/Facturas/Details", new { id });
            }

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Anular factura",
                $"Anuló la factura {id}: {motivo}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Factura anulada. Se devolvió el inventario y se revirtieron los movimientos financieros.";
            return RedirectToPage("/Facturas/Details", new { id });
        }

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
                return true;
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