using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class ExoneracionTests
    {
        private static ApplicationDbContext ConImpuesto15()
        {
            var ctx = TestDb.Crear();
            ctx.Impuestos.Add(new Impuesto { Nombre = "ISV 15%", Porcentaje = 15m, EsPredeterminado = true, Activo = true });
            ctx.SaveChanges();
            return ctx;
        }

        private static List<ItemParaISV> UnItem() => new()
        {
            new ItemParaISV { ProductoId = 1, Cantidad = 2, PrecioUnitario = 100m, Descuento = 0m }
        };

        [Fact]
        public void Cliente_normal_paga_ISV()
        {
            using var ctx = ConImpuesto15();
            var r = ISVHelper.Calcular(ctx, UnItem(), 0m, 0m, exonerado: false);
            Assert.Equal(30m, r.ISVTotal);
            Assert.Equal(230m, r.Total);
            Assert.Equal(0m, r.BaseExoneradaTotal);
        }

        [Fact]
        public void Cliente_exonerado_no_paga_ISV_ni_en_el_envio()
        {
            using var ctx = ConImpuesto15();
            var r = ISVHelper.Calcular(ctx, UnItem(), 0m, 50m, exonerado: true);
            Assert.Equal(0m, r.ISVTotal);
            Assert.Equal(250m, r.BaseExoneradaTotal);
            Assert.Equal(0m, r.BaseExentaTotal);
            Assert.Equal(250m, r.Total);
        }

        [Fact]
        public void Desglose_de_factura_exonerada_va_a_importe_exonerado_no_a_exento()
        {
            using var ctx = ConImpuesto15();
            var factura = new Factura { EsExonerada = true, Subtotal = 200m, Total = 200m, Impuestos = 0m };
            var lineas = new[]
            {
                new DetalleFactura { Cantidad = 2, PrecioUnitario = 100m, Subtotal = 200m, Total = 200m, ImpuestoPorcentaje = 0m }
            };

            var d = ISVHelper.DesglosarFactura(ctx, factura, lineas);
            Assert.Equal(200m, d.BaseExoneradaTotal);
            Assert.Equal(0m, d.BaseExentaTotal);
            Assert.Equal(0m, d.ISVTotal);
        }

        [Fact]
        public void Desglose_de_factura_no_exonerada_con_tasa_cero_es_exento()
        {
            using var ctx = ConImpuesto15();
            var factura = new Factura { EsExonerada = false, Subtotal = 200m, Total = 200m, Impuestos = 0m };
            var lineas = new[]
            {
                new DetalleFactura { Cantidad = 2, PrecioUnitario = 100m, Subtotal = 200m, Total = 200m, ImpuestoPorcentaje = 0m }
            };

            var d = ISVHelper.DesglosarFactura(ctx, factura, lineas);
            Assert.Equal(200m, d.BaseExentaTotal);
            Assert.Equal(0m, d.BaseExoneradaTotal);
        }

        [Fact]
        public void MarcarExonerada_quita_el_ISV_del_total_y_del_saldo_y_copia_la_constancia()
        {
            var cliente = new Cliente
            {
                Nombre = "Embajada",
                EsExonerado = true,
                ExoneradoConstancia = "C-123",
                ExoneradoOrdenCompraExenta = "OC-9",
                ExoneradoRegistroSag = "SAG-7"
            };
            var factura = new Factura { Subtotal = 200m, Impuestos = 30m, Total = 230m, Saldo = 230m };

            ISVHelper.MarcarExonerada(factura, cliente);

            Assert.True(factura.EsExonerada);
            Assert.Equal(0m, factura.Impuestos);
            Assert.Equal(200m, factura.Total);
            Assert.Equal(200m, factura.Saldo);
            Assert.Equal("C-123", factura.ExoneradoConstancia);
            Assert.Equal("OC-9", factura.ExoneradoOrdenCompraExenta);
            Assert.Equal("SAG-7", factura.ExoneradoRegistroSag);
        }

        [Fact]
        public void RecalcularFactura_no_vuelve_a_gravar_una_factura_exonerada()
        {
            using var ctx = ConImpuesto15();
            var factura = new Factura { EsExonerada = true, Total = 200m };
            var lineas = new List<DetalleFactura>
            {
                new DetalleFactura { ProductoId = 1, Cantidad = 2, PrecioUnitario = 100m, Subtotal = 200m, Total = 200m, ImpuestoPorcentaje = 0m }
            };

            Assert.False(ISVHelper.RecalcularFactura(ctx, factura, lineas));
            Assert.Equal(0m, lineas[0].ImpuestoPorcentaje);
            Assert.Equal(200m, factura.Total);
        }

        [Fact]
        public void Catalogo_de_tipos_de_documento_no_tiene_repetidos_y_trae_los_basicos()
        {
            var valores = TiposDocumento.Todos.Select(t => t.Valor).ToList();
            Assert.Equal(valores.Count, valores.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            foreach (var esperado in new[] { "Cotizacion", "Pedido", "Venta", "Factura", "Devolucion", "Cliente", "Producto", "OrdenCompra" })
                Assert.Contains(esperado, valores);
            Assert.All(TiposDocumento.Todos, t => Assert.False(string.IsNullOrWhiteSpace(t.Grupo)));
        }
    }
}
