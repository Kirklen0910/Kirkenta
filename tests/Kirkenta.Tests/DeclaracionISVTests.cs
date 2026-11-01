using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    public class DeclaracionISVTests
    {
        private static LineaFiscal Venta(decimal base15, decimal isv15, decimal exento = 0, bool anulada = false)
        {
            var l = new LineaFiscal { Documento = "F-1", Anulada = anulada };
            if (!anulada)
            {
                l.Agregar(15m, base15, isv15, false);
                if (exento > 0) l.Agregar(0m, exento, 0, false);
            }
            return l;
        }

        [Fact]
        public void Agregar_ClasificaPorTasa()
        {
            var l = new LineaFiscal();
            l.Agregar(15m, 100m, 15m, false);
            l.Agregar(18m, 200m, 36m, false);
            l.Agregar(0m, 50m, 0m, false);   // exento
            l.Agregar(0m, 70m, 0m, true);    // exonerado
            l.Agregar(5m, 10m, 0.5m, false); // otra tasa
            Assert.Equal((100m, 15m, 200m, 36m), (l.Gravado15, l.Isv15, l.Gravado18, l.Isv18));
            Assert.Equal((50m, 70m, 10m, 0.5m), (l.Exento, l.Exonerado, l.OtroGravado, l.OtroIsv));
            Assert.Equal(51.5m, l.IsvTotal);
        }

        [Fact]
        public void Resumir_IsvAPagar_DebitoMenosCredito()
        {
            var ventas = new[] { Venta(1000m, 150m, exento: 200m), Venta(400m, 60m), Venta(999m, 149.85m, anulada: true) };
            var compras = new[] { Venta(500m, 75m) };
            var r = DeclaracionISVHelper.Resumir(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), ventas, compras);
            Assert.Equal(210m, r.IsvDebito);          // la anulada no cuenta
            Assert.Equal(75m, r.IsvCredito);
            Assert.Equal(135m, r.IsvAPagar);
            Assert.Equal(0m, r.SaldoFavorSiguiente);
            Assert.Equal(2, r.FacturasEmitidas);
            Assert.Equal(1, r.FacturasAnuladas);
            Assert.Equal(200m, r.VentasExentas);
        }

        [Fact]
        public void Resumir_ConSaldoFavorYRetenciones_PuedeQuedarSaldoAFavor()
        {
            var ventas = new[] { Venta(1000m, 150m) };
            var compras = new[] { Venta(2000m, 300m) };
            var r = DeclaracionISVHelper.Resumir(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), ventas, compras, saldoFavorAnterior: 20m, retenciones: 5m);
            Assert.Equal(-175m, r.Resultado);
            Assert.Equal(0m, r.IsvAPagar);
            Assert.Equal(175m, r.SaldoFavorSiguiente);
        }

        [Fact]
        public void Resumir_IgnoraAjustesNegativos()
        {
            var r = DeclaracionISVHelper.Resumir(DateTime.Today, DateTime.Today, new[] { Venta(100m, 15m) }, Array.Empty<LineaFiscal>(), saldoFavorAnterior: -50m, retenciones: -10m);
            Assert.Equal(15m, r.IsvAPagar);
        }

        [Fact]
        public void Resumir_IsvDeDevoluciones_ReduceElDebito()
        {
            var r = DeclaracionISVHelper.Resumir(DateTime.Today, DateTime.Today, new[] { Venta(1000m, 150m) },
                Array.Empty<LineaFiscal>(), devoluciones: 115m, isvDevoluciones: 15m);
            Assert.Equal(135m, r.IsvAPagar);
        }

        [Collection("BD")]
        public class ConBaseDeDatos
        {
            [Fact]
            public void Ventas_ConDescuentoGeneral_DeclaraElIsvRealDeLaFactura()
            {
                using var db = TestDb.Crear();
                // Venta 2 x L100 con ISV 15% y descuento general L20: base 180, ISV 27, total 207
                var f = new Factura { Id = 1, Numero = "F-1", ClienteId = 1, Fecha = new DateTime(2026, 9, 10), Subtotal = 200m, Descuento = 20m, Impuestos = 27m, Total = 207m };
                db.Facturas.Add(f);
                db.DetalleFacturas.Add(new DetalleFactura { FacturaId = 1, ProductoId = 1, Cantidad = 2, PrecioUnitario = 100m, ImpuestoPorcentaje = 15m, Subtotal = 200m, Total = 230m });
                db.SaveChanges();

                var l = DeclaracionISVHelper.Ventas(db, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1)).Single();
                Assert.Equal(180m, l.Gravado15);
                Assert.Equal(27m, l.Isv15);
            }

            [Fact]
            public void Ventas_ConDescuentoYaEnLasLineas_NoLoRestaDosVeces()
            {
                using var db = TestDb.Crear();
                // POS: 2 x L100 con 10% de descuento general repartido en la línea (base 180, ISV 27, total 207).
                // La factura guarda Descuento = 20 (suma de descuentos de línea) y las líneas ya vienen netas.
                db.Facturas.Add(new Factura { Id = 1, Numero = "F-1", ClienteId = 1, Fecha = new DateTime(2026, 9, 10), Subtotal = 200m, Descuento = 20m, Impuestos = 27m, Total = 207m });
                db.DetalleFacturas.Add(new DetalleFactura { FacturaId = 1, ProductoId = 1, Cantidad = 2, PrecioUnitario = 100m, Descuento = 20m, ImpuestoPorcentaje = 15m, Subtotal = 180m, Total = 207m });
                db.SaveChanges();

                var l = DeclaracionISVHelper.Ventas(db, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1)).Single();
                Assert.Equal(180m, l.Gravado15);
                Assert.Equal(27m, l.Isv15);
            }

            [Fact]
            public void Compras_ConDescuentoEnLinea_CreditoFiscalSobreLaBaseNeta()
            {
                using var db = TestDb.Crear();
                // 10 x L100 con descuento de L100 en la línea: base 900, ISV 135, total 1035 (Subtotal guarda el bruto 1000)
                db.OrdenesCompra.Add(new OrdenCompra { Id = 1, Numero = "OC-1", ProveedorId = 1, Estado = "Recibida", Fecha = new DateTime(2026, 9, 5), FechaRecepcion = new DateTime(2026, 9, 5), Subtotal = 1000m, Descuento = 100m, Impuestos = 135m, Total = 1035m });
                db.DetalleOrdenesCompra.Add(new DetalleOrdenCompra { OrdenCompraId = 1, ProductoId = 1, Cantidad = 10, CantidadRecibida = 10, PrecioUnitario = 100m, Descuento = 100m, ImpuestoPorcentaje = 15m, Subtotal = 1000m, Total = 1035m });
                db.SaveChanges();

                var l = DeclaracionISVHelper.Compras(db, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1)).Single();
                Assert.Equal(900m, l.Gravado15);
                Assert.Equal(135m, l.Isv15);
                Assert.Equal(1035m, l.Total);
            }

            [Fact]
            public void Ventas_ConEnvioGravado_IncluyeElIsvDelEnvio()
            {
                using var db = TestDb.Crear();
                // 1 x L100 + envio L20, ISV 15%: base 120, ISV 18, total 138. El envio no tiene linea.
                db.Facturas.Add(new Factura { Id = 1, Numero = "F-1", ClienteId = 1, Fecha = new DateTime(2026, 9, 10), Subtotal = 120m, MontoEnvio = 20m, Impuestos = 18m, Total = 138m });
                db.DetalleFacturas.Add(new DetalleFactura { FacturaId = 1, ProductoId = 1, Cantidad = 1, PrecioUnitario = 100m, ImpuestoPorcentaje = 15m, Subtotal = 100m, Total = 115m });
                db.SaveChanges();

                var l = DeclaracionISVHelper.Ventas(db, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1)).Single();
                Assert.Equal(18m, l.IsvTotal);
                Assert.Equal(120m, l.GravadoTotal);
            }

            [Fact]
            public void IsvDevoluciones_ProporcionalAlIsvDeLaVenta()
            {
                using var db = TestDb.Crear();
                db.Ventas.Add(new Venta { Id = 1, Numero = "V-1", Impuestos = 15m, Total = 115m });
                db.Devoluciones.Add(new Devolucion { Numero = "D-1", VentaId = 1, ClienteId = 1, Fecha = new DateTime(2026, 9, 12), Total = 57.5m, Estado = "Aplicada" });
                db.SaveChanges();

                var isv = DeclaracionISVHelper.IsvDevolucionesDelPeriodo(db, new DateTime(2026, 9, 1), new DateTime(2026, 10, 1));
                Assert.Equal(7.5m, isv);
            }
        }
    }
}
