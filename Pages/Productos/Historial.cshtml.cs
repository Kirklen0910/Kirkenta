﻿using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Productos
{
    /// <summary>
    /// Historial de un producto: compras (orden, proveedor, costo) y ventas
    /// (venta, factura, cliente, cantidad, costo, precio y utilidad).
    /// </summary>
    public class HistorialModel : PageModel
    {
        private const int MaxFilas = 500;

        private readonly ApplicationDbContext _context;

        public HistorialModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? Desde { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? Hasta { get; set; }

        public Producto Producto { get; set; } = new();
        public bool PuedeVerCompras { get; set; }
        public bool PuedeVerVentas { get; set; }

        public List<CompraItem> Compras { get; set; } = new();
        public List<VentaItem> Ventas { get; set; } = new();

        // ===== KPIs compras (solo órdenes vigentes: sin Borrador ni Cancelada) =====
        public decimal CompraUnidades { get; set; }
        public decimal CompraInvertido { get; set; }
        public decimal CompraCostoPromedio { get; set; }
        public decimal? CompraUltimoCosto { get; set; }

        // ===== KPIs ventas (solo ventas no anuladas, netas de devoluciones) =====
        public decimal VentaUnidades { get; set; }
        public decimal VentaMonto { get; set; }
        public decimal VentaUtilidad { get; set; }
        public decimal VentaMargenPct { get; set; }
        public int VentaLineasSinCosto { get; set; }

        public class CompraItem
        {
            public int OrdenId { get; set; }
            public string NumeroOrden { get; set; } = "";
            public DateTime Fecha { get; set; }
            public DateTime? FechaRecepcion { get; set; }
            public int ProveedorId { get; set; }
            public string ProveedorNombre { get; set; } = "—";
            public decimal Cantidad { get; set; }
            public decimal CantidadRecibida { get; set; }
            public decimal CostoUnitario { get; set; }
            public decimal Total { get; set; }
            public string Estado { get; set; } = "";
            public bool Vigente { get; set; }
        }

        public class VentaItem
        {
            public int VentaId { get; set; }
            public string VentaNumero { get; set; } = "";
            public int? FacturaId { get; set; }
            public string? FacturaNumero { get; set; }
            public DateTime Fecha { get; set; }
            public int? ClienteId { get; set; }
            public string ClienteNombre { get; set; } = "Consumidor final";
            public decimal Cantidad { get; set; }
            public decimal Devuelta { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Descuento { get; set; }
            public string Cajero { get; set; } = "—";
            public string MetodoPago { get; set; } = "—";
            public decimal? CostoUnitario { get; set; }
            public decimal Subtotal { get; set; }
            public decimal? CostoTotal { get; set; }
            public decimal? Utilidad { get; set; }
            public string Estado { get; set; } = "";
            public bool Vigente { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var usuario = User.Identity?.Name;
            PuedeVerCompras = PermisoHelper.UsuarioTienePermiso(_context, usuario, "Compras", "Index", "ver");
            PuedeVerVentas = PermisoHelper.UsuarioTienePermiso(_context, usuario, "Ventas", "Index", "ver");

            if (!PuedeVerCompras && !PuedeVerVentas
                && !PermisoHelper.UsuarioTienePermiso(_context, usuario, "Inventario", "Productos", "ver"))
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var producto = _context.Productos.FirstOrDefault(p => p.Id == id);
            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado";
                return RedirectToPage("/Productos/Index");
            }
            Producto = producto;

            if (PuedeVerCompras) CargarCompras(id);
            if (PuedeVerVentas) CargarVentas(id);

            return Page();
        }

        private void CargarCompras(int productoId)
        {
            var q = from d in _context.DetalleOrdenesCompra
                    join o in _context.OrdenesCompra on d.OrdenCompraId equals o.Id
                    where d.ProductoId == productoId
                    select new { d, o };

            if (Desde.HasValue) q = q.Where(x => x.o.Fecha >= Desde.Value.Date);
            if (Hasta.HasValue) q = q.Where(x => x.o.Fecha < Hasta.Value.Date.AddDays(1));

            var filas = q.OrderByDescending(x => x.o.Fecha).ThenByDescending(x => x.d.Id)
                .Take(MaxFilas).ToList();

            var provIds = filas.Select(x => x.o.ProveedorId).Distinct().ToList();
            var proveedores = _context.Proveedores.Where(p => provIds.Contains(p.Id))
                .ToDictionary(p => p.Id, p => p.Nombre);

            Compras = filas.Select(x => new CompraItem
            {
                OrdenId = x.o.Id,
                NumeroOrden = x.o.Numero,
                Fecha = x.o.Fecha,
                FechaRecepcion = x.o.FechaRecepcion,
                ProveedorId = x.o.ProveedorId,
                ProveedorNombre = proveedores.TryGetValue(x.o.ProveedorId, out var n) ? n : "—",
                Cantidad = x.d.Cantidad,
                CantidadRecibida = x.d.CantidadRecibida,
                CostoUnitario = x.d.PrecioUnitario,
                Total = x.d.Total,
                Estado = x.o.Estado,
                Vigente = x.o.Estado != "Borrador" && x.o.Estado != "Cancelada"
            }).ToList();

            var vigentes = Compras.Where(c => c.Vigente).ToList();
            CompraUnidades = vigentes.Sum(c => c.Cantidad);
            CompraInvertido = vigentes.Sum(c => c.Total);
            CompraCostoPromedio = CompraUnidades > 0
                ? vigentes.Sum(c => c.Cantidad * c.CostoUnitario) / CompraUnidades
                : 0;
            CompraUltimoCosto = vigentes.Count > 0 ? vigentes[0].CostoUnitario : (decimal?)null;
        }

        private void CargarVentas(int productoId)
        {
            var q = from d in _context.DetalleVentas
                    join v in _context.Ventas on d.VentaId equals v.Id
                    where d.ProductoId == productoId
                    select new { d, v };

            if (Desde.HasValue) q = q.Where(x => x.v.Fecha >= Desde.Value.Date);
            if (Hasta.HasValue) q = q.Where(x => x.v.Fecha < Hasta.Value.Date.AddDays(1));

            var filas = q.OrderByDescending(x => x.v.Fecha).ThenByDescending(x => x.d.Id)
                .Take(MaxFilas).ToList();

            var clienteIds = filas.Where(x => x.v.ClienteId.HasValue)
                .Select(x => x.v.ClienteId!.Value).Distinct().ToList();
            var clientes = _context.Clientes.Where(c => clienteIds.Contains(c.Id))
                .ToDictionary(c => c.Id, c => c.Nombre);

            var ventaIds = filas.Select(x => x.v.Id).Distinct().ToList();
            var facturas = _context.Facturas
                .Where(f => f.VentaId != null && ventaIds.Contains(f.VentaId.Value))
                .Select(f => new { f.Id, f.Numero, f.Estado, VentaId = f.VentaId!.Value })
                .ToList()
                .GroupBy(f => f.VentaId)
                .ToDictionary(g => g.Key,
                    g => g.OrderBy(f => f.Estado == "Anulada").ThenByDescending(f => f.Id).First());

            var detalleIds = filas.Select(x => x.d.Id).ToList();
            var devueltas = (from dd in _context.DetalleDevoluciones
                             join dev in _context.Devoluciones on dd.DevolucionId equals dev.Id
                             where dd.DetalleVentaId != null
                                   && detalleIds.Contains(dd.DetalleVentaId.Value)
                                   && dev.Estado == "Aplicada"
                             select new { DetalleVentaId = dd.DetalleVentaId!.Value, dd.Cantidad })
                .ToList()
                .GroupBy(x => x.DetalleVentaId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));

            // Quién vendió y cómo se pagó cada venta
            var cajeroIds = filas.Where(x => x.v.UsuarioCreoId.HasValue).Select(x => x.v.UsuarioCreoId!.Value).Distinct().ToList();
            var cajeros = _context.Usuarios.Where(u => cajeroIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.Username);
            var ventaIdsMetodo = filas.Select(x => x.v.Id).Distinct().ToList();
            var metodosPorVenta = (from p in _context.Pagos
                                   join m in _context.MetodosPago on p.MetodoPagoId equals m.Id
                                   where p.VentaId != null && ventaIdsMetodo.Contains(p.VentaId.Value)
                                   select new { VentaId = p.VentaId!.Value, m.Nombre })
                .ToList()
                .GroupBy(x => x.VentaId)
                .ToDictionary(g => g.Key, g => string.Join(" + ", g.Select(x => x.Nombre).Distinct()));

            foreach (var x in filas)
            {
                var vigente = x.v.Estado != "Anulada" && x.v.Estado != "Cancelada";
                var devuelta = devueltas.TryGetValue(x.d.Id, out var dv) ? dv : 0m;
                // Líneas anteriores al registro de costos tienen snapshot 0: costo desconocido.
                var costoConocido = x.d.CostoUnitarioSnapshot > 0 || x.d.CostoTotalLinea > 0;

                facturas.TryGetValue(x.v.Id, out var fac);

                Ventas.Add(new VentaItem
                {
                    VentaId = x.v.Id,
                    VentaNumero = x.v.Numero,
                    FacturaId = fac?.Id,
                    FacturaNumero = fac?.Numero,
                    Fecha = x.v.Fecha,
                    ClienteId = x.v.ClienteId,
                    ClienteNombre = x.v.ClienteId.HasValue && clientes.TryGetValue(x.v.ClienteId.Value, out var cn)
                        ? cn : "Consumidor final",
                    Cantidad = x.d.Cantidad,
                    Devuelta = devuelta,
                    PrecioUnitario = x.d.PrecioUnitario,
                    Descuento = x.d.Descuento,
                    Cajero = x.v.UsuarioCreoId.HasValue && cajeros.TryGetValue(x.v.UsuarioCreoId.Value, out var cj) ? cj : "—",
                    MetodoPago = metodosPorVenta.TryGetValue(x.v.Id, out var mp) ? mp : (x.v.EsCredito ? "Crédito" : "—"),
                    CostoUnitario = costoConocido ? x.d.CostoUnitarioSnapshot : (decimal?)null,
                    Subtotal = x.d.Subtotal,
                    CostoTotal = costoConocido ? x.d.CostoTotalLinea : (decimal?)null,
                    Utilidad = costoConocido ? x.d.Subtotal - x.d.CostoTotalLinea : (decimal?)null,
                    Estado = x.v.Estado,
                    Vigente = vigente
                });
            }

            // KPIs netos de devoluciones (proporcional a lo devuelto en cada línea)
            decimal ventaConCosto = 0;
            foreach (var v in Ventas.Where(v => v.Vigente))
            {
                var factor = v.Cantidad > 0 ? Math.Max(v.Cantidad - v.Devuelta, 0) / v.Cantidad : 0;
                VentaUnidades += Math.Max(v.Cantidad - v.Devuelta, 0);
                VentaMonto += v.Subtotal * factor;
                if (v.Utilidad.HasValue)
                {
                    VentaUtilidad += v.Utilidad.Value * factor;
                    ventaConCosto += v.Subtotal * factor;
                }
                else
                {
                    VentaLineasSinCosto++;
                }
            }
            VentaMargenPct = ventaConCosto > 0 ? VentaUtilidad / ventaConCosto * 100 : 0;
        }
    }
}
