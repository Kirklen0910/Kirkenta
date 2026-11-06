using Kirkenta.Data;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Kirkenta.Services;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class AnulacionPagoProveedorTests
    {
        private const decimal SaldoBanco = 5000m;

        private sealed class Escena
        {
            public required ApplicationDbContext Ctx;
            public required OrdenCompra Orden;
            public required MetodoPago Metodo;
            public required CuentaFinanciera Banco;

            public decimal SaldoOrden => Ctx.OrdenesCompra.First(o => o.Id == Orden.Id).Saldo;
            public decimal SaldoCuenta => Ctx.CuentasFinancieras.First(c => c.Id == Banco.Id).SaldoActual;
            public string EstadoOrden => Ctx.OrdenesCompra.First(o => o.Id == Orden.Id).Estado;

            public PagoProveedor Pagar(decimal monto, bool enFinanzas = true)
            {
                var r = PagoProveedorService.Registrar(Ctx, new PagoProveedorService.Solicitud
                {
                    OrdenCompraId = Orden.Id,
                    Monto = monto,
                    MetodoPagoId = Metodo.Id,
                    RegistrarEnFinanzas = enFinanzas
                }, 1);
                Assert.True(r.Ok, r.Error);
                return r.Pago!;
            }
        }

        private static Escena Crear(decimal total, string estado = "Recibida", decimal impuestos = 0m, decimal tipoCambio = 1m)
        {
            var ctx = TestDb.Crear();
            var metodo = new MetodoPago { Nombre = "Transferencia", Tipo = "Transferencia" };
            var banco = new CuentaFinanciera { Codigo = "B1", Nombre = "Banco", Tipo = "Banco", SaldoActual = SaldoBanco, Activa = true };
            var orden = new OrdenCompra
            {
                Numero = "OC-1", ProveedorId = 1, Estado = estado, Total = total, Saldo = total,
                Impuestos = impuestos, TipoCambio = tipoCambio, FechaRecepcion = DateTime.Now
            };
            ctx.AddRange(metodo, banco, orden);
            ctx.CategoriasFinancieras.Add(new CategoriaFinanciera { Nombre = "Pago a proveedores", Tipo = "Egreso", Activa = true });
            ctx.SaveChanges();
            return new Escena { Ctx = ctx, Orden = orden, Metodo = metodo, Banco = banco };
        }

        [Fact]
        public void Pago_total_regresa_la_orden_a_Recibida_y_revierte_el_egreso()
        {
            var e = Crear(1000m);
            using var _ = e.Ctx;
            var pago = e.Pagar(1000m);
            Assert.Equal("Pagada", e.EstadoOrden);
            Assert.Equal(SaldoBanco - 1000m, e.SaldoCuenta);

            var r = PagoProveedorService.Anular(e.Ctx, pago.Id, "  Se pagó a otra orden  ", 7);

            Assert.True(r.Ok, r.Error);
            Assert.Equal(1000m, e.SaldoOrden);
            Assert.Equal("Recibida", e.EstadoOrden);
            Assert.Equal(SaldoBanco, e.SaldoCuenta);

            var p = e.Ctx.PagosProveedor.First(x => x.Id == pago.Id);
            Assert.Equal("Anulado", p.Estado);
            Assert.Equal("Se pagó a otra orden", p.MotivoAnulacion);
            Assert.Equal(7, p.UsuarioAnuloId);
            Assert.NotNull(p.FechaAnulacion);

            var mov = e.Ctx.MovimientosFinancieros.Single(m => m.Origen == "PagoProveedor" && m.OrigenId == pago.Id);
            Assert.Equal("Anulado", mov.Estado);
            Assert.Equal(7, mov.UsuarioAnuloId);
            Assert.Contains("Se pagó a otra orden", mov.MotivoAnulacion);
        }

        [Fact]
        public void Pago_parcial_suma_solo_su_monto_y_respeta_los_otros_pagos()
        {
            var e = Crear(1000m);
            using var _ = e.Ctx;
            var primero = e.Pagar(300.25m);
            var segundo = e.Pagar(200.10m);

            Assert.True(PagoProveedorService.Anular(e.Ctx, primero.Id, "Error", 1).Ok);

            Assert.Equal(1000m - 200.10m, e.SaldoOrden);
            Assert.Equal("Recibida", e.EstadoOrden);
            Assert.Equal(SaldoBanco - 200.10m, e.SaldoCuenta);
            Assert.Equal("Activo", e.Ctx.PagosProveedor.First(p => p.Id == segundo.Id).Estado);
            Assert.Equal("Activo", e.Ctx.MovimientosFinancieros.Single(m => m.OrigenId == segundo.Id).Estado);
        }

        [Fact]
        public void Una_orden_RecibidaParcial_sigue_igual_aunque_estuviera_pagada_por_completo()
        {
            var e = Crear(1000m, estado: "RecibidaParcial");
            using var _ = e.Ctx;
            var pago = e.Pagar(1000m);
            Assert.Equal("RecibidaParcial", e.EstadoOrden);

            Assert.True(PagoProveedorService.Anular(e.Ctx, pago.Id, "Error", 1).Ok);

            Assert.Equal(1000m, e.SaldoOrden);
            Assert.Equal("RecibidaParcial", e.EstadoOrden);
        }

        [Fact]
        public void Anular_quitando_un_pago_de_una_orden_Pagada_con_varios_pagos_la_reabre()
        {
            var e = Crear(500m);
            using var _ = e.Ctx;
            e.Pagar(200m);
            var ultimo = e.Pagar(300m);
            Assert.Equal("Pagada", e.EstadoOrden);

            Assert.True(PagoProveedorService.Anular(e.Ctx, ultimo.Id, "Error", 1).Ok);

            Assert.Equal(300m, e.SaldoOrden);
            Assert.Equal("Recibida", e.EstadoOrden);
        }

        [Fact]
        public void Doble_anulacion_no_devuelve_dos_veces()
        {
            var e = Crear(1000m);
            using var _ = e.Ctx;
            var pago = e.Pagar(400m);

            Assert.True(PagoProveedorService.Anular(e.Ctx, pago.Id, "Error", 1).Ok);
            var segunda = PagoProveedorService.Anular(e.Ctx, pago.Id, "Otra vez", 2);

            Assert.False(segunda.Ok);
            Assert.Contains("ya fue anulado", segunda.Error);
            Assert.Equal(1000m, e.SaldoOrden);
            Assert.Equal(SaldoBanco, e.SaldoCuenta);
            Assert.Equal("Error", e.Ctx.PagosProveedor.First().MotivoAnulacion);
        }

        [Fact]
        public void Mes_actual_cerrado_bloquea_la_anulacion_sin_tocar_nada()
        {
            var e = Crear(1000m);
            using var _ = e.Ctx;
            var pago = e.Pagar(1000m);
            var hoy = DateTime.Today;
            CierreContableHelper.Cerrar(e.Ctx, hoy.Year, hoy.Month, 1);

            var r = PagoProveedorService.Anular(e.Ctx, pago.Id, "Error", 1);

            Assert.False(r.Ok);
            Assert.Contains("CERRADO", r.Error);
            Assert.Equal(0m, e.SaldoOrden);
            Assert.Equal("Pagada", e.EstadoOrden);
            Assert.Equal(SaldoBanco - 1000m, e.SaldoCuenta);
            Assert.Equal("Activo", e.Ctx.PagosProveedor.First().Estado);
            Assert.Equal("Activo", e.Ctx.MovimientosFinancieros.Single().Estado);
        }

        [Fact]
        public void Pago_de_un_mes_cerrado_no_se_anula_aunque_el_mes_actual_este_abierto()
        {
            var e = Crear(1000m);
            using var _ = e.Ctx;
            var pago = e.Pagar(1000m);
            var mesPasado = DateTime.Today.AddMonths(-1);
            pago.Fecha = mesPasado;
            e.Ctx.SaveChanges();
            CierreContableHelper.Cerrar(e.Ctx, mesPasado.Year, mesPasado.Month, 1);

            var r = PagoProveedorService.Anular(e.Ctx, pago.Id, "Error", 1);

            Assert.False(r.Ok);
            Assert.Equal(0m, e.SaldoOrden);
            Assert.Equal("Activo", e.Ctx.PagosProveedor.First().Estado);
        }

        [Fact]
        public void Movimiento_en_mes_cerrado_tambien_bloquea()
        {
            var e = Crear(1000m);
            using var _ = e.Ctx;
            var pago = e.Pagar(1000m);
            var mesPasado = DateTime.Today.AddMonths(-1);
            e.Ctx.MovimientosFinancieros.Single().Fecha = mesPasado;
            e.Ctx.SaveChanges();
            CierreContableHelper.Cerrar(e.Ctx, mesPasado.Year, mesPasado.Month, 1);

            Assert.False(PagoProveedorService.Anular(e.Ctx, pago.Id, "Error", 1).Ok);
            Assert.Equal("Activo", e.Ctx.MovimientosFinancieros.Single().Estado);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Requiere_motivo(string? motivo)
        {
            var e = Crear(1000m);
            using var _ = e.Ctx;
            var pago = e.Pagar(400m);

            var r = PagoProveedorService.Anular(e.Ctx, pago.Id, motivo, 1);

            Assert.False(r.Ok);
            Assert.Equal(600m, e.SaldoOrden);
        }

        [Fact]
        public void Pago_inexistente_devuelve_error()
        {
            var e = Crear(1000m);
            using var _ = e.Ctx;

            Assert.False(PagoProveedorService.Anular(e.Ctx, 999, "Error", 1).Ok);
        }

        [Fact]
        public void Pago_sin_movimiento_en_finanzas_se_anula_sin_tocar_la_cuenta()
        {
            var e = Crear(1000m);
            using var _ = e.Ctx;
            var pago = e.Pagar(400m, enFinanzas: false);

            Assert.True(PagoProveedorService.Anular(e.Ctx, pago.Id, "Error", 1).Ok);

            Assert.Equal(1000m, e.SaldoOrden);
            Assert.Equal(SaldoBanco, e.SaldoCuenta);
        }

        [Fact]
        public void En_moneda_extranjera_la_orden_recupera_dolares_y_la_cuenta_lempiras()
        {
            var e = Crear(100m, tipoCambio: 24.5m);
            using var _ = e.Ctx;
            var pago = e.Pagar(100m);
            Assert.Equal(SaldoBanco - 2450m, e.SaldoCuenta);

            Assert.True(PagoProveedorService.Anular(e.Ctx, pago.Id, "Error", 1).Ok);

            Assert.Equal(100m, e.SaldoOrden);
            Assert.Equal(SaldoBanco, e.SaldoCuenta);
        }

        [Fact]
        public void Despues_de_anular_la_orden_se_puede_volver_a_pagar()
        {
            var e = Crear(1000m);
            using var _ = e.Ctx;
            var pago = e.Pagar(1000m);
            Assert.True(PagoProveedorService.Anular(e.Ctx, pago.Id, "Error", 1).Ok);

            e.Pagar(1000m);

            Assert.Equal(0m, e.SaldoOrden);
            Assert.Equal("Pagada", e.EstadoOrden);
            Assert.Equal(SaldoBanco - 1000m, e.SaldoCuenta);
        }

        [Fact]
        public void Un_pago_anulado_no_reduce_las_compras_pendientes_ni_el_isv_pagado()
        {
            var e = Crear(1150m, impuestos: 150m);
            using var _ = e.Ctx;
            var pago = e.Pagar(1150m);
            var corte = DateTime.Today.AddDays(1);
            Assert.Empty(DevengoHelper.ComprasPendientes(e.Ctx, corte));
            Assert.Equal(150m, ContabilidadHelper.IsvComprasPagado(e.Ctx, corte));

            Assert.True(PagoProveedorService.Anular(e.Ctx, pago.Id, "Error", 1).Ok);

            var pendiente = Assert.Single(DevengoHelper.ComprasPendientes(e.Ctx, corte));
            Assert.Equal(1150m, pendiente.Pendiente);
            Assert.Equal(150m, pendiente.Isv);
            Assert.Equal(0m, ContabilidadHelper.IsvComprasPagado(e.Ctx, corte));
        }

        [Fact]
        public void Un_pago_parcial_anulado_tampoco_cuenta_en_el_pendiente_de_una_orden_con_otro_pago()
        {
            var e = Crear(1000m);
            using var _ = e.Ctx;
            var anulado = e.Pagar(300m);
            e.Pagar(200m);

            Assert.True(PagoProveedorService.Anular(e.Ctx, anulado.Id, "Error", 1).Ok);

            var pendiente = Assert.Single(DevengoHelper.ComprasPendientes(e.Ctx, DateTime.Today.AddDays(1)));
            Assert.Equal(800m, pendiente.Pendiente);
            Assert.Equal(e.SaldoOrden, pendiente.Pendiente);
        }
    }
}
