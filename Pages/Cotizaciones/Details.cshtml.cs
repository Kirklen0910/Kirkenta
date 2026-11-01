using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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
        public bool PuedeEditar { get; set; }
        public bool PuedeConvertir { get; set; }

        public IActionResult OnGet(int id)
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Cotizaciones", "ver")))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            PuedeEditar = PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Cotizaciones", "editar");
            PuedeConvertir = PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Facturas", "crear");

            var cotizacion = _context.Cotizaciones.FirstOrDefault(c => c.Id == id);
            if (cotizacion == null)
            {
                TempData["Error"] = "Cotización no encontrada";
                return RedirectToPage("/Cotizaciones/Index");
            }

            // Auto-marcar como Vencida si expiró y sigue en Borrador/Enviada/Aceptada
            if ((string.IsNullOrEmpty(cotizacion.Estado) || cotizacion.Estado == "Borrador" || cotizacion.Estado == "Enviada" || cotizacion.Estado == "Aceptada")
                && cotizacion.FechaVencimiento.HasValue
                && cotizacion.FechaVencimiento.Value.Date < DateTime.Today)
            {
                cotizacion.Estado = "Vencida";
                _context.SaveChanges();
            }
            else if (string.IsNullOrEmpty(cotizacion.Estado))
            {
                cotizacion.Estado = "Borrador";
                _context.SaveChanges();
            }

            Cotizacion = cotizacion;
            Cliente = _context.Clientes.FirstOrDefault(c => c.Id == cotizacion.ClienteId) ?? new Cliente();
            Items = _context.DetalleCotizaciones.Where(d => d.CotizacionId == id).ToList();
            Productos = _context.Productos.ToList();
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault() ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };

            return Page();
        }

        public IActionResult OnPostAceptar(int id)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Cotizaciones", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar cotizaciones";
                return RedirectToPage("/Cotizaciones/Details", new { id });
            }

            var cotizacion = _context.Cotizaciones.FirstOrDefault(c => c.Id == id);
            if (cotizacion != null && (cotizacion.Estado == "Borrador" || cotizacion.Estado == "Enviada"))
            {
                cotizacion.Estado = "Aceptada";
                _context.SaveChanges();
                TempData["Success"] = "Cotización marcada como Aceptada";
            }
            return RedirectToPage("/Cotizaciones/Details", new { id });
        }

        public IActionResult OnPostRechazar(int id)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Cotizaciones", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar cotizaciones";
                return RedirectToPage("/Cotizaciones/Details", new { id });
            }

            var cotizacion = _context.Cotizaciones.FirstOrDefault(c => c.Id == id);
            if (cotizacion != null && (cotizacion.Estado == "Borrador" || cotizacion.Estado == "Enviada"))
            {
                cotizacion.Estado = "Rechazada";
                _context.SaveChanges();
                TempData["Success"] = "Cotización marcada como Rechazada";
            }
            return RedirectToPage("/Cotizaciones/Details", new { id });
        }

        public IActionResult OnPostConvertir(int id)
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Facturas", "crear")))
            {
                TempData["Error"] = "No tienes permiso para realizar esta acción";
                return RedirectToPage("/Index");
            }

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

            // Validar stock antes de facturar
            var itemsCot = _context.DetalleCotizaciones.Where(d => d.CotizacionId == id).ToList();
            foreach (var it in itemsCot)
            {
                var prodCheck = _context.Productos.FirstOrDefault(p => p.Id == it.ProductoId);
                if (prodCheck != null && prodCheck.Stock < it.Cantidad)
                {
                    TempData["Error"] = $"Stock insuficiente de '{prodCheck.Nombre}'. Disponible: {prodCheck.Stock}, requerido: {it.Cantidad}."
                        + " Para vender más de lo que hay en existencia cóbrala en el POS: allí un supervisor puede autorizarlo.";
                    return RedirectToPage("/Cotizaciones/Details", new { id });
                }
            }

            using var transaction = _context.Database.BeginTransaction();

            // Reclamo atómico: una cotización solo se convierte una vez (doble clic / dos personas)
            if (_context.Database.IsRelational())
            {
                var reclamada = _context.Cotizaciones
                    .Where(c => c.Id == id && c.Estado == "Aceptada")
                    .ExecuteUpdate(u => u.SetProperty(c => c.Estado, "Aceptada"));
                if (reclamada == 0)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Esta cotización ya fue convertida.";
                    return RedirectToPage("/Cotizaciones/Details", new { id });
                }
            }

            var factura = new Factura
            {
                Numero = "",
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

            // Cliente exonerado de ISV: la factura sale sin ISV y con los datos de su constancia
            var clienteExo = _context.Clientes.FirstOrDefault(c => c.Id == factura.ClienteId && c.EsExonerado);
            if (clienteExo != null)
                Kirkenta.Helpers.Finanzas.ISVHelper.MarcarExonerada(factura, clienteExo);

            var (numeroAsignado, errorNumero, _) = FacturacionFiscalHelper.AsignarNumero(_context, factura);
            if (numeroAsignado == null)
            {
                transaction.Rollback();
                TempData["Error"] = errorNumero ?? "No se pudo asignar el número de factura.";
                return RedirectToPage("/Cotizaciones/Details", new { id });
            }

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
                    ImpuestoPorcentaje = factura.EsExonerada ? 0m : item.ImpuestoPorcentaje,
                    Subtotal = item.Subtotal,
                    Total = factura.EsExonerada ? item.Subtotal : item.Total
                });

                var producto = _context.Productos.FirstOrDefault(p => p.Id == item.ProductoId);
                if (producto != null)
                {
                    InventarioHelper.Registrar(_context, producto, -item.Cantidad, "FacturaCotizacion",
                        factura.Numero, factura.Id, currentUser?.Id, producto.PrecioCompra);
                }
            }

            cotizacion.Estado = "Convertida";
            cotizacion.FacturaId = factura.Id;
            cotizacion.FechaConversion = DateTime.Now;

            _context.SaveChanges();
            // Costo de ventas (contable) de la factura; no bloquea la emisión si falla
            var (movCostoFac, errCostoFac) = Kirkenta.Helpers.Finanzas.MovimientoAutomaticoHelper
                .RegistrarCostoFactura(_context, factura, currentUser?.Id ?? 0);
            if (movCostoFac == null && errCostoFac != null)
                Console.WriteLine($"[Factura {factura.Numero}] Costo de ventas no registrado: {errCostoFac}");

            transaction.Commit();

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