using System.Text;
using Kirkenta.Helpers.Migracion;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class MigracionAutomaticaTests
    {
        private const string Json = @"{
  ""impuestos"": [ { ""nombre"": ""ISV 15%"", ""porcentaje"": 15 } ],
  ""unidades"": [ { ""nombre"": ""Libra"", ""abreviatura"": ""lb"" } ],
  ""categorias"": [ { ""nombre"": ""Ferretería"" } ],
  ""productos"": [
    { ""codigo"": ""P1"", ""nombre"": ""Clavo"", ""categoria"": ""Ferretería"", ""precio"": 5, ""costo"": 2, ""existencia"": 100, ""unidad"": ""Libra"", ""impuesto"": ""15"" },
    { ""codigo"": ""P2"", ""precio"": 3 }
  ],
  ""clientes"": [ { ""codigo"": ""C1"", ""nombre"": ""Ana"", ""rtn"": ""08011990123456"", ""limite"": 1000, ""dias"": 30 } ],
  ""proveedores"": [ { ""nombre"": ""Casa X"", ""contacto"": ""Pepe"" } ],
  ""ventas"": [
    { ""factura"": ""F1"", ""fecha"": ""01/02/2023"", ""total"": 50, ""cliente"": ""Ana"", ""saldo"": 0 },
    { ""factura"": ""F2"", ""fecha"": ""05/02/2023"", ""total"": 300, ""cliente"": ""Ana"", ""saldo"": 300 }
  ],
  ""detalle_ventas"": [
    { ""factura"": ""F1"", ""codigo"": ""P1"", ""descripcion"": ""Clavo"", ""cantidad"": 10, ""precio"": 5 },
    { ""factura"": ""F2"", ""codigo"": ""P1"", ""descripcion"": ""Clavo"", ""cantidad"": 60, ""precio"": 5 }
  ],
  ""cuentas_por_cobrar"": [ { ""factura"": ""F2"", ""cliente"": ""Ana"", ""saldo"": 300 } ],
  ""cuentas_por_pagar"": [ { ""factura"": ""X1"", ""proveedor"": ""Casa X"", ""saldo"": 900 } ]
}";

        private static List<TablaOrigen> Leer()
        {
            var r = MigracionLector.Leer("sistema_viejo.json", new MemoryStream(Encoding.UTF8.GetBytes(Json)));
            Assert.Equal(10, r.Tablas.Count);
            return r.Tablas;
        }

        [Fact]
        public void Plan_ReconoceTodo_UneLineasYEvitaDuplicarSaldos()
        {
            var tablas = Leer();
            var plan = MigracionAutomaticaHelper.Planificar(tablas);

            PasoMigracion Paso(string nombre) => plan.Single(p => p.TablaNombre == nombre);

            Assert.Equal("Impuestos", Paso("impuestos").Destino);
            Assert.Equal("UnidadesMedida", Paso("unidades").Destino);
            Assert.Equal("Categorias", Paso("categorias").Destino);
            Assert.Equal("Productos", Paso("productos").Destino);
            Assert.Equal("Clientes", Paso("clientes").Destino);
            Assert.Equal("Proveedores", Paso("proveedores").Destino);
            Assert.Equal("@ventas", Paso("ventas").Destino);
            Assert.Equal("@cxp", Paso("cuentas_por_pagar").Destino);

            Assert.True(Paso("ventas").Incluir);
            Assert.NotNull(Paso("ventas").DetalleIndice);
            Assert.False(Paso("detalle_ventas").Incluir);           // se une a ventas
            Assert.False(Paso("cuentas_por_cobrar").Incluir);        // ventas ya trae el saldo
            Assert.True(Paso("cuentas_por_pagar").Incluir);

            // orden de ejecución: catálogos primero
            var incluidos = plan.Where(p => p.Incluir).Select(p => p.Destino).ToList();
            Assert.True(incluidos.IndexOf("Productos") < incluidos.IndexOf("Clientes"));
            Assert.True(incluidos.IndexOf("Clientes") < incluidos.IndexOf("@ventas"));
        }

        [Fact]
        public void Migrar_TodoDeUnaVez_DejaElSistemaListo()
        {
            var ctx = TestDb.Crear();
            var admin = TestDb.AgregarUsuario(ctx, "adm", "Admin");
            var tablas = Leer();
            var plan = MigracionAutomaticaHelper.Planificar(tablas);

            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, plan, admin.Id, true);
            Assert.True(res.Ok, res.ErrorFatal);
            Assert.True(res.Aplicado);
            Assert.Equal(8, res.Pasos.Count);

            // catálogos
            Assert.Single(ctx.Impuestos);
            Assert.Equal("Libra", ctx.UnidadesMedida.Single().Nombre);
            Assert.Equal("Ferretería", ctx.Categorias.Single().Nombre);

            // productos: el que no tiene nombre se descarta y se informa
            var prod = ctx.Productos.Single(p => p.SKU == "P1");
            Assert.Equal(5m, prod.PrecioVenta);
            Assert.Equal(2m, prod.PrecioCompra);
            Assert.Equal(100m, prod.Stock);                          // stock inicial; las ventas históricas NO lo descuentan
            Assert.Equal(15m, ctx.Impuestos.Single(i => i.Id == prod.ImpuestoId).Porcentaje);
            Assert.NotNull(prod.CategoriaId);
            var mov = Assert.Single(ctx.MovimientosInventario);
            Assert.Equal("StockInicial", mov.Origen);
            var pasoProd = res.Pasos.Single(p => p.Paso.Destino == "Productos");
            Assert.Equal(1, pasoProd.Res!.DescartadasTotal);

            // terceros
            var ana = ctx.Clientes.Single();
            Assert.Equal("C1", ana.Codigo);
            Assert.Equal(30, ana.DiasCredito);
            Assert.Equal("Pepe", ctx.Proveedores.Single().Contacto);

            // ventas: una pagada (histórica) y una a crédito cobrable, con sus líneas
            Assert.Equal(2, ctx.Ventas.Count());
            var f1 = ctx.Ventas.Single(v => v.Numero == "H-F1");
            Assert.False(f1.EsCredito);
            Assert.True(f1.MovimientoRegistrado);
            var f2 = ctx.Ventas.Single(v => v.Numero == "H-F2");
            Assert.True(f2.EsCredito);
            Assert.False(f2.MovimientoRegistrado);                   // así Finanzas → Cobros la deja cobrar
            Assert.Equal(300m, f2.SaldoPendiente);
            Assert.Equal(ana.Id, f2.ClienteId);
            Assert.Equal(new DateTime(2023, 3, 7), f2.FechaVencimientoCredito);
            Assert.Equal(2, ctx.DetalleVentas.Count());
            Assert.All(ctx.DetalleVentas, d => Assert.Equal(prod.Id, d.ProductoId));
            Assert.Empty(ctx.MovimientosFinancieros);

            // cuentas por pagar
            var oc = ctx.OrdenesCompra.Single();
            Assert.Equal("Recibida", oc.Estado);
            Assert.Equal(900m, oc.Saldo);
            Assert.Equal(ctx.Proveedores.Single().Id, oc.ProveedorId);
        }

        [Fact]
        public void Migrar_DosVeces_NoDuplicaNada()
        {
            var ctx = TestDb.Crear();
            var tablas = Leer();
            var plan = MigracionAutomaticaHelper.Planificar(tablas);
            Assert.True(MigracionAutomaticaHelper.Ejecutar(ctx, tablas, plan, null, true).Ok);

            var otra = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, true);
            Assert.True(otra.Ok, otra.ErrorFatal);
            Assert.Single(ctx.Productos.Where(p => p.SKU == "P1"));
            Assert.Single(ctx.Clientes);
            Assert.Single(ctx.Proveedores);
            Assert.Equal(2, ctx.Ventas.Count());
            Assert.Single(ctx.OrdenesCompra);
            Assert.Single(ctx.MovimientosInventario);
            Assert.Equal(100m, ctx.Productos.Single(p => p.SKU == "P1").Stock);
        }

        [Fact]
        public void Revision_SinBaseRelacional_NoGuardaNada()
        {
            var ctx = TestDb.Crear();
            var tablas = Leer();
            var res = MigracionAutomaticaHelper.Ejecutar(ctx, tablas, MigracionAutomaticaHelper.Planificar(tablas), null, false);
            Assert.False(res.Ok);
            Assert.Empty(ctx.Productos);
        }

        [Fact]
        public void Catalogos_NombreEnDescripcion_SeAjustaSolo()
        {
            var mapa = MigracionCatalogosHelper.Detectar(new List<string> { "Codigo", "Descripcion", "Precio" }, "Productos");
            Assert.Equal("Descripcion", mapa["Nombre"]);
            Assert.False(mapa.ContainsKey("Descripcion"));
        }
    }
}
