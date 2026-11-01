using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class IntegridadNominaTests
    {
        private static RevisionIntegridad Paso(Kirkenta.Data.ApplicationDbContext ctx, string titulo)
        {
            var i = Enumerable.Range(0, IntegridadHelper.TotalPasos).First(x => IntegridadHelper.Info(x).Titulo == titulo);
            return IntegridadHelper.EjecutarPaso(ctx, i, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));
        }

        [Fact]
        public void Pago_de_nomina_que_no_suma_el_egreso_se_detecta()
        {
            using var ctx = TestDb.Crear();
            var mov = new MovimientoFinanciero { Numero = "MOV-1", Tipo = "Egreso", Monto = 1000m, Estado = "Activo", Origen = "Nomina", Concepto = "Nómina" };
            ctx.MovimientosFinancieros.Add(mov);
            ctx.SaveChanges();
            ctx.PagosNominaEmpleado.AddRange(
                new PagoNominaEmpleado { Numero = "PN-1", NominaId = 1, EmpleadoId = 1, DetalleNominaId = 1, Monto = 600m, MovimientoId = mov.Id, Estado = "Pagado" },
                new PagoNominaEmpleado { Numero = "PN-2", NominaId = 1, EmpleadoId = 2, DetalleNominaId = 2, Monto = 300m, MovimientoId = mov.Id, Estado = "Pagado" });
            ctx.SaveChanges();

            var r = Paso(ctx, "Pagos de nómina con egreso que cuadra");
            Assert.Equal(1, r.Total);

            mov.Monto = 900m;
            ctx.SaveChanges();
            Assert.True(Paso(ctx, "Pagos de nómina con egreso que cuadra").Ok);
        }
    }
}
