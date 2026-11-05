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
