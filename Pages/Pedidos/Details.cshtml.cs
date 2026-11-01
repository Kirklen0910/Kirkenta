using Kirkenta.Helpers;
using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Pages.Pedidos
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Pedido Pedido { get; set; } = new();
        public Cliente Cliente { get; set; } = new();
        public string EmpresaNombre { get; set; } = "Mi Empresa";
        public List<DetallePedido> Items { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            return Cargar(id);
        }

        private IActionResult Cargar(int id)
        {
            if (!(PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Pedidos", "ver")))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var pedido = _context.Pedidos.FirstOrDefault(p => p.Id == id);
            if (pedido == null)
            {
                TempData["Error"] = "Pedido no encontrado";
                return RedirectToPage("/Pedidos/Index");
            }

            Pedido = pedido;
            Cliente = _context.Clientes.FirstOrDefault(c => c.Id == pedido.ClienteId) ?? new Cliente();
            Items = _context.DetallePedidos.Where(d => d.PedidoId == id).ToList();
            Productos = _context.Productos.ToList();
            EmpresaNombre = _context.ConfiguracionEmpresa.Select(e => e.Nombre).FirstOrDefault() ?? "Mi Empresa";

            return Page();
        }

        // ===== Cambios de estado =====
        public IActionResult OnPostEstado(int id, string nuevoEstado)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Pedidos", "editar"))
            {
                TempData["Error"] = "No tienes permiso para realizar esta acción";
                return RedirectToPage("/Pedidos/Index");
            }

            var pedido = _context.Pedidos.FirstOrDefault(p => p.Id == id);
            if (pedido == null)
            {
                TempData["Error"] = "Pedido no encontrado";
                return RedirectToPage("/Pedidos/Index");
            }

            var permitidos = new Dictionary<string, string[]>
            {
                ["Pendiente"] = new[] { "En preparacion", "Cancelado" },
                ["En preparacion"] = new[] { "Enviado", "Entregado", "Cancelado" },
                ["Enviado"] = new[] { "Entregado" },
                ["Entregado"] = Array.Empty<string>(),
                ["Cancelado"] = Array.Empty<string>(),
            };

            if (!permitidos.TryGetValue(pedido.Estado, out var siguientes) || !siguientes.Contains(nuevoEstado))
            {
                TempData["Error"] = $"No se puede pasar de '{pedido.Estado}' a '{nuevoEstado}'.";
                return RedirectToPage(new { id });
            }

            if (nuevoEstado == "Cancelado" && pedido.FacturaId != null)
            {
                TempData["Error"] = "El pedido ya fue facturado; anula la factura en lugar de cancelar el pedido.";
                return RedirectToPage(new { id });
            }

            var anterior = pedido.Estado;
            pedido.Estado = nuevoEstado;
            _context.SaveChanges();

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Cambiar estado de pedido",
                $"Pedido {pedido.Numero}: {anterior} → {nuevoEstado}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Pedido {pedido.Numero} ahora está '{nuevoEstado}'";
            return RedirectToPage(new { id });
        }

        // ===== Facturar el pedido =====
        public IActionResult OnPostFacturar(int id)
        {
            if (!PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Ventas", "Facturas", "crear"))
            {
                TempData["Error"] = "No tienes permiso para facturar";
                return RedirectToPage("/Pedidos/Index");
            }

            var pedido = _context.Pedidos.FirstOrDefault(p => p.Id == id);
            if (pedido == null)
            {
                TempData["Error"] = "Pedido no encontrado";
                return RedirectToPage("/Pedidos/Index");
            }
            if (pedido.FacturaId != null)
            {
                TempData["Error"] = "Este pedido ya fue facturado.";
                return RedirectToPage(new { id });
            }
            if (pedido.Estado == "Cancelado")
            {
                TempData["Error"] = "No se puede facturar un pedido cancelado.";
                return RedirectToPage(new { id });
            }

            var items = _context.DetallePedidos.Where(d => d.PedidoId == id).ToList();
            if (items.Count == 0)
            {
                TempData["Error"] = "El pedido no tiene productos.";
                return RedirectToPage(new { id });
            }

            // Validar stock antes de facturar
            foreach (var it in items)
            {
                var p = _context.Productos.FirstOrDefault(x => x.Id == it.ProductoId);
                if (p != null && p.Stock < it.Cantidad)
                {
                    TempData["Error"] = $"Stock insuficiente de '{p.Nombre}'. Disponible: {p.Stock}, requerido: {it.Cantidad}."
                        + " Para vender más de lo que hay en existencia cóbrala en el POS: allí un supervisor puede autorizarlo.";
                    return RedirectToPage(new { id });
                }
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            using var tx = _context.Database.BeginTransaction();

            // Reclamo atómico: un pedido se factura una sola vez (doble clic / dos personas)
            if (_context.Database.IsRelational())
            {
                var reclamado = _context.Pedidos
                    .Where(p => p.Id == id && p.FacturaId == null && p.Estado != "Cancelado")
                    .ExecuteUpdate(u => u.SetProperty(p => p.Estado, p => p.Estado));
                if (reclamado == 0)
                {
                    tx.Rollback();
                    TempData["Error"] = "Este pedido ya fue facturado o cancelado.";
                    return RedirectToPage(new { id });
                }
            }

            var factura = new Factura
            {
                Numero = "",
                ClienteId = pedido.ClienteId,
                PedidoId = pedido.Id,
                CotizacionId = pedido.CotizacionId,
                Fecha = DateTime.Now,
                FechaVencimiento = DateTime.Now.AddDays(30),
                Subtotal = pedido.Subtotal,
                Descuento = pedido.Descuento,
                Impuestos = pedido.Impuestos,
                Total = pedido.Total + pedido.MontoEnvio,
                Saldo = pedido.Total + pedido.MontoEnvio,
                Estado = "Emitida",
                Notas = $"Generada desde pedido {pedido.Numero}",
                UsuarioCreoId = usuario?.Id,
                RequiereEnvio = pedido.RequiereEnvio,
                MontoEnvio = pedido.MontoEnvio
            };

            // Cliente exonerado de ISV: la factura sale sin ISV y con los datos de su constancia
            var clienteExo = _context.Clientes.FirstOrDefault(c => c.Id == factura.ClienteId && c.EsExonerado);
            if (clienteExo != null)
                Kirkenta.Helpers.Finanzas.ISVHelper.MarcarExonerada(factura, clienteExo);

            var (numero, errorNumero, _) = FacturacionFiscalHelper.AsignarNumero(_context, factura);
            if (numero == null)
            {
                tx.Rollback();
                TempData["Error"] = errorNumero ?? "No se pudo asignar el número de factura.";
                return RedirectToPage(new { id });
            }

            _context.Facturas.Add(factura);
            _context.SaveChanges();

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
                    InventarioHelper.Registrar(_context, producto, -item.Cantidad, "FacturaPedido",
                        factura.Numero, factura.Id, usuario?.Id, producto.PrecioCompra);
                }
            }

            pedido.FacturaId = factura.Id;
            if (pedido.Estado == "Pendiente") pedido.Estado = "En preparacion";
            _context.SaveChanges();
            // Costo de ventas (contable) de la factura; no bloquea la emisión si falla
            var (movCostoFac, errCostoFac) = Kirkenta.Helpers.Finanzas.MovimientoAutomaticoHelper
                .RegistrarCostoFactura(_context, factura, usuario?.Id ?? 0);
            if (movCostoFac == null && errCostoFac != null)
                Console.WriteLine($"[Factura {factura.Numero}] Costo de ventas no registrado: {errCostoFac}");

            tx.Commit();

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Facturar pedido",
                $"Facturó el pedido {pedido.Numero} en la factura {factura.Numero}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Pedido facturado: {factura.Numero}";
            return RedirectToPage("/Facturas/Details", new { id = factura.Id });
        }
    }
}