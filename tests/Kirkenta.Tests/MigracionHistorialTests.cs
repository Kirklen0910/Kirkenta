using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Migracion;
using Kirkenta.Models;
using Xunit;
using static Kirkenta.Helpers.Migracion.MigracionHistorialHelper;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class MigracionHistorialTests
    {
        private static TablaOrigen Tabla(string nombre, string[] headers, params string[][] filas)
        {
            var t = new TablaOrigen { Nombre = nombre, Headers = headers.ToList() };
            foreach (var f in filas)
            {
                var d = new Dictionary<string, string>();
                for (int i = 0; i < headers.Length; i++) d[headers[i]] = i < f.Length ? f[i] : "";
                t.Rows.Add(d);
            }
            return t;
        }

        private static Opciones Op(string tipo, TablaOrigen t, string terceros = "crear")
        {
            var mapa = Detectar(t.Headers, Campos(tipo)).Where(p => p.Value != null).ToDictionary(p => p.Key, p => p.Value!);
            return new Opciones { Tipo = tipo, Mapa = mapa, TerceroNoExiste = terceros };
        }

        [Fact]
        public void ParsearFecha_FormatosComunes()
        {
            Assert.Equal(new DateTime(2024, 3, 5), ParsearFecha("05/03/2024"));
            Assert.Equal(new DateTime(2024, 3, 5), ParsearFecha("2024-03-05"));
            Assert.Equal(new DateTime(2024, 3, 5, 10, 30, 0), ParsearFecha("2024-03-05T10:30:00"));
            Assert.Equal(new DateTime(2021, 1, 1), ParsearFecha("44197"));
            Assert.Null(ParsearFecha("no es fecha"));
        }

        [Fact]
        public void SaldosPorCobrar_CreanVentaCobrable_YCreanClienteNuevo()
        {
            var ctx = TestDb.Crear();
            ctx.Clientes.Add(new Cliente { Nombre = "Ferretería López", Codigo = "C1", DiasCredito = 30 });
            ctx.SaveChanges();

            var t = Tabla("cxc", new[] { "Factura", "Cliente", "Fecha", "Saldo" },
                new[] { "F-100", "FERRETERIA LOPEZ", "01/02/2024", "1,500.50" },
                new[] { "F-101", "Cliente Nuevo SA", "10/02/2024", "800" },
                new[] { "F-102", "Cliente Nuevo SA", "11/02/2024", "0" });

            var prev = Procesar(ctx, t, Op(TipoCxC, t), false, null);
            Assert.True(prev.Ok);
            Assert.Equal(2, prev.Creados);
            Assert.Equal(1, prev.TercerosPorCrear);
            Assert.Empty(ctx.Ventas);                       // la revisión no guarda nada

            var r = Procesar(ctx, t, Op(TipoCxC, t), true, null);
            Assert.True(r.Aplicado);
            Assert.Equal(1, r.TercerosCreados);
            Assert.Equal(2, ctx.Ventas.Count());

            var v = ctx.Ventas.First(x => x.Numero == "SI-F-100");
            Assert.True(v.EsCredito);
            Assert.Equal(1500.50m, v.SaldoPendiente);
            Assert.Equal(1500.50m, v.Total);
            Assert.False(v.MovimientoRegistrado);            // necesario para poder cobrarla en Finanzas
            Assert.True(v.CostoRegistrado);
            Assert.NotNull(v.LoteMigracion);
            Assert.Equal(new DateTime(2024, 3, 2), v.FechaVencimientoCredito);   // fecha + 30 días del cliente
            Assert.Equal(ctx.Clientes.First(c => c.Codigo == "C1").Id, v.ClienteId);
            Assert.Empty(ctx.DetalleVentas);
            Assert.Equal(2, ctx.Clientes.Count());
        }

        [Fact]
        public void ReimportarElMismoArchivo_NoDuplica()
        {
            var ctx = TestDb.Crear();
            var t = Tabla("cxc", new[] { "Factura", "Cliente", "Saldo" }, new[] { "A1", "Pedro", "100" });
            Assert.True(Procesar(ctx, t, Op(TipoCxC, t), true, null).Aplicado);
            var otra = Procesar(ctx, t, Op(TipoCxC, t), true, null);
            Assert.False(otra.Aplicado);
            Assert.Equal(1, otra.YaImportados);
            Assert.Single(ctx.Ventas);
            Assert.Single(ctx.Clientes);
        }

        [Fact]
        public void SaldosPorPagar_CreanOrdenRecibidaPagable()
        {
            var ctx = TestDb.Crear();
            var t = Tabla("cxp", new[] { "Factura", "Proveedor", "Fecha", "Saldo", "Total" },
                new[] { "P-1", "Distribuidora X", "15/01/2024", "2000", "5000" });
            var r = Procesar(ctx, t, Op(TipoCxP, t), true, null);
            Assert.True(r.Aplicado, string.Join(" | ", r.Errores));

            var o = Assert.Single(ctx.OrdenesCompra);
            Assert.Equal("Recibida", o.Estado);
            Assert.Equal(2000m, o.Saldo);
            Assert.Equal(5000m, o.Total);
            Assert.Equal("SI-CXP-P-1", o.Numero);
            Assert.Equal(ctx.Proveedores.Single().Id, o.ProveedorId);
        }

        [Fact]
        public void HistorialVentas_NoTocaStockNiFinanzas_YBloqueaAnulacionYDevolucion()
        {
            var ctx = TestDb.Crear();
            var admin = TestDb.AgregarUsuario(ctx, "adm", "Admin");
            var prod = new Producto { SKU = "TOR1", Nombre = "Tornillo", Stock = 50, PrecioCompra = 1, PrecioVenta = 2 };
            ctx.Productos.Add(prod); ctx.SaveChanges();

            var cab = Tabla("ventas", new[] { "Factura", "Fecha", "Total", "Cliente" },
                new[] { "V-1", "03/03/2023", "230", "" },
                new[] { "V-2", "04/03/2023", "100", "" });
            var det = Tabla("detalle", new[] { "Factura", "Codigo", "Descripcion", "Cantidad", "Precio" },
                new[] { "V-1", "TOR1", "Tornillo", "100", "2" },
                new[] { "V-1", "XXX", "Cosa vieja", "1", "30" });

            var op = Op(TipoVentas, cab, "sin");
            op.Detalle = det;
            op.MapaDetalle = Detectar(det.Headers, CamposDetalle()).Where(p => p.Value != null).ToDictionary(p => p.Key, p => p.Value!);

            var r = Procesar(ctx, cab, op, true, admin.Id);
            Assert.True(r.Aplicado, string.Join(" | ", r.Errores));
            Assert.Equal(2, ctx.Ventas.Count());
            Assert.Equal(3, r.LineasCreadas);                      // 2 líneas de V-1 + 1 genérica de V-2

            Assert.Equal(50m, ctx.Productos.First(p => p.SKU == "TOR1").Stock);   // el stock NO cambia
            Assert.Empty(ctx.MovimientosFinancieros);

            var v1 = ctx.Ventas.First(v => v.Numero == "H-V-1");
            Assert.True(v1.MovimientoRegistrado);
            Assert.False(v1.EsCredito);
            Assert.Equal(0m, v1.SaldoPendiente);
            Assert.Equal(prod.Id, ctx.DetalleVentas.First(d => d.VentaId == v1.Id && d.Cantidad == 100).ProductoId);
            var generico = ctx.Productos.First(p => p.SKU == GenericoSku);
            Assert.False(generico.Activo);
            Assert.Contains(ctx.DetalleVentas, d => d.VentaId == v1.Id && d.ProductoId == generico.Id && d.Descripcion == "Cosa vieja");

            var (okAnul, _) = AnulacionVentaHelper.Anular(ctx, v1.Id, "prueba", admin.Id);
            Assert.False(okAnul);
            Assert.Equal("Completada", ctx.Ventas.First(v => v.Id == v1.Id).Estado);
        }

        [Fact]
        public void DeshacerLote_BorraLoImportado_PeroConservaLoCobrado()
        {
            var ctx = TestDb.Crear();
            var t = Tabla("cxc", new[] { "Factura", "Cliente", "Saldo" },
                new[] { "A", "Ana", "100" }, new[] { "B", "Beto", "200" });
            var r = Procesar(ctx, t, Op(TipoCxC, t), true, null);
            var vB = ctx.Ventas.First(v => v.Numero == "SI-B");
            ctx.Pagos.Add(new Pago { VentaId = vB.Id, ClienteId = vB.ClienteId ?? 0, Monto = 50, MetodoPagoId = 1, Fecha = DateTime.Now });
            ctx.SaveChanges();

            var (borrados, conservados) = DeshacerLote(ctx, r.Lote!);
            Assert.Equal(1, borrados);
            Assert.Single(conservados);
            Assert.Single(ctx.Ventas);
            Assert.Equal("SI-B", ctx.Ventas.Single().Numero);
        }

        [Fact]
        public void Kardex_ImportaHistorial_SinCambiarStock_Y_NoDuplica()
        {
            var ctx = TestDb.Crear();
            ctx.Productos.Add(new Producto { SKU = "A1", Nombre = "Clavo", Stock = 10 });
            ctx.SaveChanges();

            var t = Tabla("kardex", new[] { "Codigo", "Fecha", "Tipo", "Cantidad", "Costo" },
                new[] { "A1", "01/01/2023", "Compra", "100", "1.5" },
                new[] { "A1", "05/01/2023", "Venta", "30", "1.5" },
                new[] { "ZZZ", "05/01/2023", "Venta", "3", "1" });

            var r = Procesar(ctx, t, Op(TipoKardex, t), true, null);
            Assert.True(r.Aplicado, string.Join(" | ", r.Errores));
            Assert.Equal(2, r.Creados);
            Assert.Equal(1, r.Omitidos);
            Assert.Equal(10m, ctx.Productos.Single().Stock);
            var mov = ctx.MovimientosInventario.OrderBy(m => m.Fecha).ToList();
            Assert.Equal(100m, mov[0].Cantidad);
            Assert.Equal(-30m, mov[1].Cantidad);
            Assert.All(mov, m => Assert.Equal(OrigenKardex, m.Origen));

            var otra = Procesar(ctx, t, Op(TipoKardex, t), true, null);
            Assert.False(otra.Aplicado);
            Assert.Equal(2, ctx.MovimientosInventario.Count());

            var (borrados, _) = DeshacerLote(ctx, r.Lote!);
            Assert.Equal(2, borrados);
            Assert.Empty(ctx.MovimientosInventario);
        }

        [Fact]
        public void Errores_FaltaColumnaRequerida_NoGuardaNada()
        {
            var ctx = TestDb.Crear();
            var t = Tabla("cxc", new[] { "Cliente", "Saldo" }, new[] { "Ana", "100" });
            var r = Procesar(ctx, t, Op(TipoCxC, t), true, null);
            Assert.False(r.Ok);
            Assert.Empty(ctx.Ventas);
        }
    }
}
