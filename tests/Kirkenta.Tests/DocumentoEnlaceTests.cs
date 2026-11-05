using Kirkenta.Helpers;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class DocumentoEnlaceTests
    {
        [Fact]
        public void NumerosYNombres_LlevanASuDetalle_SoloSiExistenYSePuedenVer()
        {
            using var ctx = TestDb.Crear();
            TestDb.AgregarUsuario(ctx, "admin", "Admin");
            TestDb.AgregarUsuario(ctx, "sinrol", "Pendiente");
            var cli = new Cliente { Nombre = "Ana Pérez" };
            var gemelo1 = new Cliente { Nombre = "Juan López" };
            var gemelo2 = new Cliente { Nombre = "Juan López" };
            ctx.Clientes.AddRange(cli, gemelo1, gemelo2);
            ctx.SaveChanges();
            var v = new Venta { Numero = "VEN-0012", ClienteId = cli.Id, Total = 10, Estado = "Completada" };
            ctx.Ventas.Add(v);
            ctx.SaveChanges();
            var f = new Factura { Numero = "000-001-01-00000120", VentaId = v.Id, ClienteId = cli.Id, Total = 10, Estado = "Pagada" };
            ctx.Facturas.Add(f);
            ctx.SaveChanges();

            var docs = DocumentoEnlaceHelper.Documentos(ctx, "admin", new[] { "VEN-0012", "000-001-01-00000120", "VEN-9999", "SKU-123" });
            Assert.Equal($"/Ventas/Details/{v.Id}", docs["VEN-0012"]);
            Assert.Equal($"/Facturas/Details/{f.Id}", docs["000-001-01-00000120"]);
            Assert.Equal(2, docs.Count);                                   // lo que no existe no se enlaza

            var nombres = DocumentoEnlaceHelper.Terceros(ctx, "admin", new[] { "Ana Pérez", "Juan López", "Nadie" }, false);
            Assert.Equal($"/Clientes/Details/{cli.Id}", nombres["Ana Pérez"]);
            Assert.False(nombres.ContainsKey("Juan López"));               // dos clientes con el mismo nombre: no se adivina

            Assert.Empty(DocumentoEnlaceHelper.Documentos(ctx, "sinrol", new[] { "VEN-0012" }));   // sin permiso, sin enlace
        }
    }
}
