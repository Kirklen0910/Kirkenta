using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class FacturaProveedorRepetidaTests
    {
        [Fact]
        public void FacturaRepetida_SoloElMismoProveedorYElNumeroExacto()
        {
            using var ctx = TestDb.Crear();
            var a = new Proveedor { Nombre = "Distribuidora A", RTN = "08019000000011" };
            var b = new Proveedor { Nombre = "Distribuidora B", RTN = "08019000000022" };
            ctx.Proveedores.AddRange(a, b);
            ctx.SaveChanges();
            // como en la base real: una factura cuyo número contiene a otro más corto
            ctx.OrdenesCompra.Add(new OrdenCompra { Numero = "ORD-0002", ProveedorId = a.Id, Estado = "Recibida",
                Notas = "Factura del proveedor N° 000-001-0000032456 (fecha 03/11/2026). Compra registrada como ya recibida." });
            ctx.OrdenesCompra.Add(new OrdenCompra { Numero = "ORD-0003", ProveedorId = a.Id, Estado = "Recibida",
                Notas = "Factura del proveedor N° 777. Datos leídos con IA y revisados por el usuario." });
            ctx.SaveChanges();

            Assert.Null(FacturaProveedorRepetida.OrdenConLaFactura(ctx, a.Id, "000-001-000003245"));     // otra factura: «…3245» ≠ «…32456»
            Assert.Equal("ORD-0002", FacturaProveedorRepetida.OrdenConLaFactura(ctx, a.Id, "000-001-0000032456"));
            Assert.Equal("ORD-0003", FacturaProveedorRepetida.OrdenConLaFactura(ctx, a.Id, " 777 "));
            Assert.Null(FacturaProveedorRepetida.OrdenConLaFactura(ctx, b.Id, "000-001-0000032456"));    // otro proveedor (otro RTN): se permite
        }
    }
}
