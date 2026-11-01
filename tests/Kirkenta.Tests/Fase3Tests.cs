using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class DevolucionProveedorTests
    {
        private static (ApplicationDbContext ctx, Producto prod, OrdenCompra orden, DetalleOrdenCompra det, Usuario user) Preparar(decimal saldo = 1000m)
        {
            var ctx = TestDb.Crear();
            var user = TestDb.AgregarUsuario(ctx, "compras", "Admin");
            var prod = new Producto { Nombre = "Resma", Stock = 10, PrecioCompra = 100m };
            ctx.Productos.Add(prod);
            ctx.SaveChanges();

            var orden = new OrdenCompra { Numero = "OC-1", ProveedorId = 1, Estado = "Recibida", Total = 1000m, Saldo = saldo, TipoCambio = 1m };
            ctx.OrdenesCompra.Add(orden);
            ctx.SaveChanges();

            var det = new DetalleOrdenCompra
            {
                OrdenCompraId = orden.Id, ProductoId = prod.Id, Cantidad = 10, CantidadRecibida = 10,
                PrecioUnitario = 100m, Subtotal = 1000m, Total = 1000m
            };
            ctx.DetalleOrdenesCompra.Add(det);
            ctx.SaveChanges();
            return (ctx, prod, orden, det, user);
        }

        [Fact]
        public void Devolucion_baja_stock_y_reduce_deuda()
        {
            var (ctx, prod, orden, det, user) = Preparar();
            using var _ = ctx;

            var (dev, error) = DevolucionProveedorHelper.Crear(ctx, new DevolucionProveedorHelper.Solicitud
            {
                OrdenCompraId = orden.Id, Motivo = "Dañado", UsuarioId = user.Id,
                Items = new() { new() { DetalleOrdenId = det.Id, Cantidad = 4 } }
            });

            Assert.Null(error);
            Assert.Equal(400m, dev!.Total);
            Assert.Equal(400m, dev.MontoAplicadoSaldo);
            Assert.Equal(0m, dev.MontoPorRecuperar);
            Assert.Equal(6, prod.Stock);
            Assert.Equal(600m, orden.Saldo);
            Assert.Contains(ctx.MovimientosInventario, m => m.Origen == "DevolucionProveedor" && m.Cantidad == -4);
        }

        [Fact]
        public void Orden_ya_pagada_deja_monto_por_recuperar()
        {
            var (ctx, _, orden, det, user) = Preparar(saldo: 0m);
            using var __ = ctx;

            var (dev, error) = DevolucionProveedorHelper.Crear(ctx, new DevolucionProveedorHelper.Solicitud
            {
                OrdenCompraId = orden.Id, Motivo = "Dañado", UsuarioId = user.Id,
                Items = new() { new() { DetalleOrdenId = det.Id, Cantidad = 2 } }
            });

            Assert.Null(error);
            Assert.Equal(0m, dev!.MontoAplicadoSaldo);
            Assert.Equal(200m, dev.MontoPorRecuperar);
        }

        [Fact]
        public void No_se_puede_devolver_mas_de_lo_recibido()
        {
            var (ctx, _, orden, det, user) = Preparar();
            using var __ = ctx;

            var (dev, error) = DevolucionProveedorHelper.Crear(ctx, new DevolucionProveedorHelper.Solicitud
            {
                OrdenCompraId = orden.Id, Motivo = "x", UsuarioId = user.Id,
                Items = new() { new() { DetalleOrdenId = det.Id, Cantidad = 11 } }
            });

            Assert.Null(dev);
            Assert.NotNull(error);
        }
    }

    public class PagerInfoTests
    {
        [Fact]
        public void Calcula_paginas_y_ajusta_pagina_fuera_de_rango()
        {
            var p = new PagerInfo(page: 99, pageSize: 50, total: 120);
            Assert.Equal(3, p.TotalPages);
            Assert.Equal(3, p.Page);
            Assert.Equal(100, p.Skip);

            var vacio = new PagerInfo(page: 0, pageSize: 50, total: 0);
            Assert.Equal(1, vacio.TotalPages);
            Assert.Equal(1, vacio.Page);
        }
    }
}
