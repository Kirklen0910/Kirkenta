using System.IO.Compression;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Export;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class ExportarTodoTests
    {
        [Fact]
        public void LlevateTusDatos_UnCsvPorTabla_SinContrasenas()
        {
            var ctx = TestDb.Crear();
            ctx.Clientes.Add(new Cliente { Nombre = "Ferretería «El Martillo», S.A.", Codigo = "0007", LimiteCredito = 1500.5m, Activo = true });
            ctx.Usuarios.Add(new Usuario { Username = "cajero1", PasswordHash = "HASH-SECRETO-123", Rol = "Cajero", Activo = true });
            ctx.SaveChanges();

            using var ms = new MemoryStream();
            var tablas = ExportarTodoHelper.Generar(ctx, ms);
            ms.Position = 0;
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);

            Assert.NotNull(zip.GetEntry("LEEME.txt"));
            var clientes = tablas.Single(t => t.Titulo == nameof(Cliente));
            Assert.Equal(1, clientes.Filas);
            string Leer(string archivo) { using var r = new StreamReader(zip.GetEntry(archivo)!.Open()); return r.ReadToEnd(); }

            var csvClientes = Leer(clientes.Archivo);
            Assert.Contains("\"Ferretería «El Martillo», S.A.\"", csvClientes);    // comas protegidas
            Assert.Contains(",0007,", csvClientes);                                  // cero a la izquierda
            Assert.Contains("1500.5", csvClientes);                                  // punto decimal

            var csvUsuarios = Leer(tablas.Single(t => t.Titulo == nameof(Usuario)).Archivo);
            Assert.Contains("cajero1", csvUsuarios);
            Assert.DoesNotContain("HASH-SECRETO", csvUsuarios);
            Assert.DoesNotContain("PasswordHash", csvUsuarios);
        }

        [Fact]
        public void ListaDeArranque_PideLaPrimeraVenta()
        {
            var ctx = TestDb.Crear();
            Assert.False(ConfiguracionInicialHelper.Estado(ctx).Single(p => p.Clave == "venta").Completo);
            ctx.Ventas.Add(new Venta { Numero = "F-1", Total = 10 });
            ctx.SaveChanges();
            Assert.True(ConfiguracionInicialHelper.Estado(ctx).Single(p => p.Clave == "venta").Completo);
        }
    }
}
