using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>Pruebas de la auditoría de Finanzas (octubre 2026).</summary>
    [Collection("BD")]
    public class FinanzasAuditoriaTests
    {
        private static CuentaFinanciera NuevaCaja(ApplicationDbContext ctx, decimal saldo = 0m, string codigo = "C1")
        {
            var c = new CuentaFinanciera { Codigo = codigo, Nombre = "Caja " + codigo, Tipo = "Caja", Subtipo = "POS", SaldoActual = saldo, Activa = true };
            ctx.CuentasFinancieras.Add(c);
            ctx.SaveChanges();
            return c;
        }

        [Fact]
        public void SaldoHelper_aplica_y_revierte_ingreso_egreso_y_transferencia()
        {
            using var ctx = TestDb.Crear();
            var a = NuevaCaja(ctx, 100m, "A");
            var b = NuevaCaja(ctx, 0m, "B");

            var ing = new MovimientoFinanciero { Tipo = "Ingreso", CuentaId = a.Id, Monto = 50m, Estado = "Activo", Concepto = "x", Numero = "M1" };
            SaldoHelper.Aplicar(ctx, ing); ctx.SaveChanges();
            Assert.Equal(150m, ctx.CuentasFinancieras.First(c => c.Id == a.Id).SaldoActual);

            var tr = new MovimientoFinanciero { Tipo = "Transferencia", CuentaId = a.Id, CuentaDestinoId = b.Id, Monto = 40m, Estado = "Activo", Concepto = "x", Numero = "M2" };
            SaldoHelper.Aplicar(ctx, tr); ctx.SaveChanges();
            Assert.Equal(110m, ctx.CuentasFinancieras.First(c => c.Id == a.Id).SaldoActual);
            Assert.Equal(40m, ctx.CuentasFinancieras.First(c => c.Id == b.Id).SaldoActual);

            tr.Estado = "Anulado";
            SaldoHelper.Revertir(ctx, tr); ctx.SaveChanges();
            Assert.Equal(150m, ctx.CuentasFinancieras.First(c => c.Id == a.Id).SaldoActual);
            Assert.Equal(0m, ctx.CuentasFinancieras.First(c => c.Id == b.Id).SaldoActual);
        }

        [Fact]
        public void Movimiento_automatico_rechaza_montos_negativos()
        {
            using var ctx = TestDb.Crear();
            var caja = NuevaCaja(ctx, 0m);
            ctx.CategoriasFinancieras.Add(new CategoriaFinanciera { Tipo = "Ingreso", Nombre = "Ventas al contado", Activa = true });
            ctx.SaveChanges();

            var (mov, error) = MovimientoAutomaticoHelper.RegistrarIngresoVenta(ctx, 1, "VEN-1", -10m, 1, "Efectivo", caja.Id);
            Assert.Null(mov);
            Assert.Contains("negativo", error);
            Assert.Equal(0m, ctx.CuentasFinancieras.First().SaldoActual);
        }

        [Fact]
        public void Numero_sin_serie_no_repite_si_se_borraron_documentos()
        {
            using var ctx = TestDb.Crear();
            // Existen VEN-0001..VEN-0003 y se borran los dos primeros (p. ej. al deshacer un lote migrado):
            // contar documentos daría 1 y el siguiente (VEN-0002) ya no choca, pero con 0003 y 0005 sí chocaría.
            ctx.Ventas.Add(new Venta { Numero = "VEN-0003", Total = 1, Estado = "Completada" });
            ctx.Ventas.Add(new Venta { Numero = "VEN-0005", Total = 1, Estado = "Completada" });
            ctx.SaveChanges();

            var n = NumeroDocumentoHelper.GenerarSiguiente(ctx, "Venta");
            Assert.Equal("VEN-0006", n);
        }

        [Fact]
        public void Apertura_de_caja_solo_permite_una_activa_a_la_vez()
        {
            using var ctx = TestDb.Crear();
            var a = NuevaCaja(ctx, 0m, "A");
            var b = NuevaCaja(ctx, 0m, "B");

            var (ap1, err1) = AperturaHelper.Abrir(ctx, a.Id, 100m, null, 1);
            Assert.NotNull(ap1);
            Assert.Null(err1);

            var (ap2, err2) = AperturaHelper.Abrir(ctx, b.Id, 50m, null, 1);
            Assert.Null(ap2);
            Assert.Contains("otra caja abierta", err2);

            var (ap3, err3) = AperturaHelper.Abrir(ctx, a.Id, -5m, null, 1);
            Assert.Null(ap3);
            Assert.NotNull(err3);
        }

        [Fact]
        public void Devolucion_de_venta_pagada_con_saldo_a_favor_regresa_a_saldo_a_favor_y_no_sale_efectivo()
        {
            using var ctx = TestDb.Crear();
            var user = TestDb.AgregarUsuario(ctx, "cajero", "Admin");
            var caja = NuevaCaja(ctx, 100m);
            var cliente = new Cliente { Nombre = "Cliente Uno" };
            ctx.Clientes.Add(cliente);
            var prod = new Producto { Nombre = "Cuaderno", Stock = 8, PrecioVenta = 50m, PrecioCompra = 30m };
            ctx.Productos.Add(prod);
            ctx.SaveChanges();

            // Venta de 100 pagada 100% con saldo a favor
            var venta = new Venta
            {
                Numero = "VEN-9", Subtotal = 100m, Total = 100m, Estado = "Completada", ClienteId = cliente.Id,
                CostoRegistrado = false, MovimientoRegistrado = true, SaldoFavorAplicado = 100m
            };
            ctx.Ventas.Add(venta);
            ctx.SaveChanges();
            var det = new DetalleVenta { VentaId = venta.Id, ProductoId = prod.Id, Cantidad = 2, PrecioUnitario = 50m, Subtotal = 100m, Total = 100m, CostoUnitarioSnapshot = 30m, CostoTotalLinea = 60m };
            ctx.DetalleVentas.Add(det);
            ctx.SaveChanges();

            var (dev, error) = DevolucionVentaHelper.Crear(ctx, new DevolucionVentaHelper.Solicitud
            {
                VentaId = venta.Id, Motivo = "Devolución", UsuarioId = user.Id, FormaReembolso = "Efectivo",
                Items = new() { new() { DetalleVentaId = det.Id, Cantidad = 1 } }
            });

            Assert.Null(error);
            Assert.Equal(0m, dev!.MontoReembolsado);
            Assert.Equal(50m, dev.MontoSaldoFavor);
            Assert.Equal(50m, SaldoFavorHelper.Saldo(ctx, cliente.Id));
            Assert.Equal(100m, ctx.CuentasFinancieras.First(c => c.Id == caja.Id).SaldoActual); // la caja no entregó nada
        }
    }
}
