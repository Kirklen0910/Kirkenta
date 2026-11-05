using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>Conteo rápido de la toma física: ubicar por código o nombre y contar fijando o sumando.</summary>
    [Collection("BD")]
    public class TomaFisicaConteoTests
    {
        private static (Kirkenta.Data.ApplicationDbContext db, int toma, Dictionary<string, int> det) Crear()
        {
            var db = TestDb.Crear();
            var prods = new[]
            {
                new Producto { Nombre = "Martillo de uña 16 oz", SKU = "MAR-16", CodigoBarras = "7401234500011", Stock = 10, PrecioCompra = 100 },
                new Producto { Nombre = "Tornillo galvanizado 1\"", SKU = "TOR-1", CodigoBarras = "7401234500028", Stock = 500, PrecioCompra = 1 },
                new Producto { Nombre = "Cinta métrica 5 m", SKU = "7401234500011X", Stock = 4, PrecioCompra = 60 },
            };
            db.Productos.AddRange(prods);
            var t = new TomaFisica { Numero = "TF-1", Descripcion = "Prueba" };
            db.TomasFisicas.Add(t);
            db.SaveChanges();
            var det = new Dictionary<string, int>();
            foreach (var p in prods)
            {
                var d = new DetalleTomaFisica { TomaFisicaId = t.Id, ProductoId = p.Id, StockSistema = p.Stock, CostoUnitario = p.PrecioCompra };
                db.DetallesTomaFisica.Add(d); db.SaveChanges();
                det[p.SKU!] = d.Id;
            }
            return (db, t.Id, det);
        }

        [Fact]
        public void ElCodigoEscaneado_UbicaAlInstante_ElProductoExacto()
        {
            var (db, toma, det) = Crear();
            using var _ = db;
            var idx = TomaFisicaConteoHelper.IndiceCodigos(db, toma);
            Assert.Equal(det["MAR-16"], idx["7401234500011"]);
            Assert.Equal(det["TOR-1"], idx["tor-1"]);
            // el código exacto va primero aunque otro producto lo contenga
            var r = TomaFisicaConteoHelper.Buscar(db, toma, "7401234500011");
            Assert.Equal("MAR-16", r[0].Sku);
        }

        [Fact]
        public void ElBuscador_IgnoraTildesYMayusculas_YUsaTodasLasPalabras()
        {
            var (db, toma, _) = Crear();
            using var __ = db;
            Assert.Equal("Cinta métrica 5 m", Assert.Single(TomaFisicaConteoHelper.Buscar(db, toma, "METRICA")).Nombre);
            Assert.Equal("TOR-1", Assert.Single(TomaFisicaConteoHelper.Buscar(db, toma, "galv torn")).Sku);
            Assert.Empty(TomaFisicaConteoHelper.Buscar(db, toma, "taladro"));
            Assert.Empty(TomaFisicaConteoHelper.Buscar(db, toma, "  "));
        }

        [Fact]
        public void Contar_FijaOSuma_YSePuedeBorrar()
        {
            var (db, toma, det) = Crear();
            using var _ = db;
            var id = det["TOR-1"];
            Assert.Equal(1m, TomaFisicaConteoHelper.Contar(db, toma, id, 1, sumar: true).item!.Contado);    // primer escaneo
            Assert.Equal(2m, TomaFisicaConteoHelper.Contar(db, toma, id, 1, sumar: true).item!.Contado);    // segundo escaneo
            Assert.Equal(102m, TomaFisicaConteoHelper.Contar(db, toma, id, 100, sumar: true).item!.Contado); // una caja de 100
            Assert.Equal(480m, TomaFisicaConteoHelper.Contar(db, toma, id, 480, sumar: false).item!.Contado);
            var t = TomaFisicaConteoHelper.Totales(db, toma);
            Assert.Equal(1, t.Contados); Assert.Equal(1, t.ConDiferencia); Assert.Equal(-20m, t.Impacto);
            Assert.Null(TomaFisicaConteoHelper.Contar(db, toma, id, null, sumar: false).item!.Contado);     // vuelve a pendiente
            Assert.Equal(0, TomaFisicaConteoHelper.Totales(db, toma).Contados);
        }

        [Fact]
        public void ProductosDeUnaConversion_HechaDespuesDeIniciar_EntranALaToma()
        {
            var (db, toma, _) = Crear();
            using var _db = db;
            var lance = db.Productos.First(p => p.SKU == "MAR-16");
            var conv = new ConversionInventario
            {
                Numero = "CNV-1", ProductoOrigenId = lance.Id, CantidadOrigen = 1, CantidadDestino = 20,
                NuevoProductoNombre = "Martillo por pieza", NuevoProductoUnidad = "Pieza", NuevoProductoPrecioVenta = 15m
            };
            db.ConversionesInventario.Add(conv); db.SaveChanges();
            Assert.Null(ConversionInventarioHelper.Aprobar(db, conv, 1));
            db.SaveChanges();

            var agregados = TomaFisicaConteoHelper.AgregarNuevos(db, toma);
            Assert.Equal(new[] { "Martillo por pieza" }, agregados);
            var it = Assert.Single(TomaFisicaConteoHelper.Buscar(db, toma, "por pieza"));
            Assert.Equal(20m, it.Sistema);                                        // entra con el stock que tiene ahora
            Assert.Empty(TomaFisicaConteoHelper.AgregarNuevos(db, toma));         // no se duplica
            Assert.Equal(4, TomaFisicaConteoHelper.Totales(db, toma).Total);
        }

        [Fact]
        public void TomaPorCategoria_SoloRecibeLosNuevosDeEsaCategoria()
        {
            using var db = TestDb.Crear();
            var herr = new Categoria { Nombre = "Herramientas", Activa = true };
            var pint = new Categoria { Nombre = "Pinturas", Activa = true };
            db.Categorias.AddRange(herr, pint); db.SaveChanges();
            var viejo = new Producto { Nombre = "Llave", Stock = 3, CategoriaId = herr.Id, FechaCreacion = DateTime.Now.AddDays(-5) };
            var otraCat = new Producto { Nombre = "Brocha", Stock = 2, CategoriaId = pint.Id, FechaCreacion = DateTime.Now.AddDays(-5) };
            db.Productos.AddRange(viejo, otraCat); db.SaveChanges();

            // toma nueva con su categoría guardada
            var t1 = new TomaFisica { Numero = "TF-A", CategoriaId = herr.Id, Fecha = DateTime.Now.AddDays(-1) };
            // toma de antes, sin el dato: solo tenía herramientas aunque ya existía la brocha
            var t2 = new TomaFisica { Numero = "TF-B", Fecha = DateTime.Now.AddDays(-1), Estado = "Cancelada" };
            db.TomasFisicas.AddRange(t1, t2); db.SaveChanges();
            db.DetallesTomaFisica.Add(new DetalleTomaFisica { TomaFisicaId = t1.Id, ProductoId = viejo.Id, StockSistema = 3 });
            db.SaveChanges();

            db.Productos.AddRange(new Producto { Nombre = "Serrucho", CategoriaId = herr.Id }, new Producto { Nombre = "Rodillo", CategoriaId = pint.Id });
            db.SaveChanges();
            Assert.Equal(new[] { "Serrucho" }, TomaFisicaConteoHelper.AgregarNuevos(db, t1.Id));

            t1.Estado = "Cancelada"; t2.Estado = "EnConteo"; db.SaveChanges();
            db.DetallesTomaFisica.Add(new DetalleTomaFisica { TomaFisicaId = t2.Id, ProductoId = viejo.Id, StockSistema = 3 });
            db.SaveChanges();
            Assert.Equal(new[] { "Serrucho" }, TomaFisicaConteoHelper.AgregarNuevos(db, t2.Id));   // deduce la categoría

            // una toma ya aplicada o cancelada no se toca
            Assert.Empty(TomaFisicaConteoHelper.AgregarNuevos(db, t1.Id));
        }

        [Fact]
        public void TomaDeTodoElInventario_RecibeLosNuevosDeCualquierCategoria()
        {
            var (db, toma, _) = Crear();
            using var _db = db;
            db.Productos.Add(new Producto { Nombre = "Producto nuevo", CategoriaId = null });
            db.Productos.Add(new Producto { Nombre = "Inactivo", Activo = false });
            db.SaveChanges();
            Assert.Equal(new[] { "Producto nuevo" }, TomaFisicaConteoHelper.AgregarNuevos(db, toma));
        }

        [Fact]
        public void Contar_RechazaNegativos_YTomasCerradas()
        {
            var (db, toma, det) = Crear();
            using var _ = db;
            Assert.NotNull(TomaFisicaConteoHelper.Contar(db, toma, det["MAR-16"], -3, sumar: false).error);
            Assert.NotNull(TomaFisicaConteoHelper.Contar(db, toma, det["MAR-16"], -1, sumar: true).error);  // quedaría en -1
            Assert.NotNull(TomaFisicaConteoHelper.Contar(db, toma, 99999, 1, sumar: false).error);
            db.TomasFisicas.Find(toma)!.Estado = "Aplicada"; db.SaveChanges();
            Assert.NotNull(TomaFisicaConteoHelper.Contar(db, toma, det["MAR-16"], 5, sumar: false).error);
        }
    }
}
