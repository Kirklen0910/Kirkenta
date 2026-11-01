using Kirkenta.Data;
using Kirkenta.Helpers.Logistica;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class LogisticaRutaTests
    {
        private static (ApplicationDbContext ctx, Ruta ruta, Envio a, Envio b) RutaEnReparto()
        {
            var ctx = TestDb.Crear();
            var ruta = new Ruta { Numero = "RUT-1", Estado = "EnReparto", TotalParadas = 2 };
            ctx.Rutas.Add(ruta);
            ctx.SaveChanges();
            var a = new Envio { Numero = "ENV-1", DireccionEntrega = "Col. A", Estado = "EnRuta", RutaId = ruta.Id, OrdenParada = 1 };
            var b = new Envio { Numero = "ENV-2", DireccionEntrega = "Col. B", Estado = "EnRuta", RutaId = ruta.Id, OrdenParada = 2 };
            ctx.Envios.AddRange(a, b);
            ctx.SaveChanges();
            return (ctx, ruta, a, b);
        }

        [Fact]
        public void Reagendar_un_fallido_en_ruta_activa_deja_que_la_ruta_se_complete()
        {
            var (ctx, ruta, a, b) = RutaEnReparto();
            using var _ = ctx;

            Assert.True(EnvioHelper.MarcarFallido(ctx, a.Id, "No estaba", null).ok);
            Assert.True(EnvioHelper.Reagendar(ctx, a.Id, null, null).ok);
            Assert.Equal(1, ruta.TotalParadas);
            Assert.Equal(0, ruta.ParadasFallidas);

            Assert.True(EnvioHelper.MarcarEntregado(ctx, b.Id, "Ana", null, null, null, null).ok);
            Assert.Equal("Completada", ruta.Estado);
        }

        [Fact]
        public void Cerrar_ruta_descuenta_las_paradas_devueltas_a_pendiente()
        {
            var (ctx, ruta, a, b) = RutaEnReparto();
            using var _ = ctx;

            Assert.True(EnvioHelper.MarcarEntregado(ctx, a.Id, "Ana", null, null, null, null).ok);
            Assert.True(RutaHelper.Cerrar(ctx, ruta.Id, null, null).ok);

            Assert.Equal(1, ruta.TotalParadas);
            Assert.Equal(1, ruta.ParadasEntregadas);
            Assert.Equal("Pendiente", b.Estado);
            Assert.Null(b.RutaId);
        }
    }
}
