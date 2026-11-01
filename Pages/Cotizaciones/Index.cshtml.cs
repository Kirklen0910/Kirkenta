using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Cotizaciones
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Estado { get; set; }

        [BindProperty(SupportsGet = true, Name = "p")]
        public int Pagina { get; set; } = 1;

        public PagerInfo Pager { get; set; } = new();
        public List<Cotizacion> Cotizaciones { get; set; } = new();
        public List<Cliente> Clientes { get; set; } = new();
        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Cotizaciones", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver cotizaciones";
                return RedirectToPage("/Index");
            }


            PuedeCrear = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Cotizaciones", "crear");
            PuedeEditar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Cotizaciones", "editar");
            PuedeEliminar = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Cotizaciones", "eliminar");

            // Auto-marcar como Vencidas si la fecha ya pasó y sigue en Borrador/Enviada/Aceptada
            var hoy = DateTime.Today;
            var pendientes = _context.Cotizaciones
                .Where(c => string.IsNullOrEmpty(c.Estado)
                         || ((c.Estado == "Borrador" || c.Estado == "Enviada" || c.Estado == "Aceptada")
                             && c.FechaVencimiento != null && c.FechaVencimiento.Value.Date < hoy))
                .ToList();
            foreach (var c in pendientes)
            {
                if (c.FechaVencimiento.HasValue && c.FechaVencimiento.Value.Date < hoy
                    && (string.IsNullOrEmpty(c.Estado) || c.Estado == "Borrador" || c.Estado == "Enviada" || c.Estado == "Aceptada"))
                    c.Estado = "Vencida";
                else if (string.IsNullOrEmpty(c.Estado))
                    c.Estado = "Borrador";
            }
            if (pendientes.Count > 0) _context.SaveChanges();

            var query = _context.Cotizaciones.AsQueryable();
            if (!string.IsNullOrWhiteSpace(Estado)) query = query.Where(c => c.Estado == Estado);
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                var idsCliente = _context.Clientes
                    .Where(x => x.Nombre.Contains(s) || (x.RTN != null && x.RTN.Contains(s)))
                    .Select(x => x.Id);
                query = query.Where(c => c.Numero.Contains(s) || idsCliente.Contains(c.ClienteId));
            }

            Pager = new PagerInfo(Pagina, 50, query.Count());
            Cotizaciones = query.OrderByDescending(c => c.Fecha).Skip(Pager.Skip).Take(Pager.PageSize).ToList();
            var ids = Cotizaciones.Select(c => c.ClienteId).Distinct().ToList();
            Clientes = _context.Clientes.Where(c => ids.Contains(c.Id)).ToList();

            return Page();
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
                return RedirectToPage("/Cotizaciones/Index");
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
                    return RedirectToPage("/Cotizaciones/Index");
                }
            }

            using var transaction = _context.Database.BeginTransaction();

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
                return RedirectToPage("/Cotizaciones/Index");
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