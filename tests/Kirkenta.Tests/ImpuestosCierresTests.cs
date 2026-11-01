using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Xunit;

namespace Kirkenta.Tests
{
    [Collection("BD")]
    public class ISVCalculoTests
    {
        private static ApplicationDbContext ConISV15()
        {
            var ctx = TestDb.Crear();
            ctx.Impuestos.Add(new Impuesto { Nombre = "ISV 15%", Porcentaje = 15m, EsPredeterminado = true, Activo = true });
            ctx.SaveChanges();
            return ctx;
        }

        private static List<ItemParaISV> UnItem(decimal cantidad, decimal precio) => new()
        {
            new ItemParaISV { ProductoId = 1, Cantidad = cantidad, PrecioUnitario = precio }
        };

        [Fact]
        public void Aplica_isv_predeterminado()
        {
            using var ctx = ConISV15();
            var r = ISVHelper.Calcular(ctx, UnItem(2, 100m));
            Assert.Equal(200m, r.Subtotal);
            Assert.Equal(30m, r.ISVTotal);
            Assert.Equal(230m, r.Total);
        }

        [Fact]
        public void Descuento_global_se_aplica_antes_del_isv()
        {
            using var ctx = ConISV15();
            var r = ISVHelper.Calcular(ctx, UnItem(2, 100m), descuentoGlobal: 20m);
            Assert.Equal(180m, r.Subtotal);
            Assert.Equal(27m, r.ISVTotal);
            Assert.Equal(207m, r.Total);
        }

        [Fact]
        public void Descuento_mayor_a_la_base_se_limita()
        {
            using var ctx = ConISV15();
            var r = ISVHelper.Calcular(ctx, UnItem(1, 50m), descuentoGlobal: 500m);
            Assert.Equal(0m, r.Subtotal);
            Assert.Equal(0m, r.ISVTotal);
            Assert.Equal(0m, r.Total);
        }

        [Fact]
        public void Cliente_exonerado_no_paga_isv()
        {
            using var ctx = ConISV15();
            var r = ISVHelper.Calcular(ctx, UnItem(2, 100m), exonerado: true);
            Assert.Equal(0m, r.ISVTotal);
            Assert.Equal(200m, r.BaseExoneradaTotal);
            Assert.Equal(0m, r.BaseExentaTotal);
            Assert.Equal(200m, r.Total);
        }

        [Fact]
        public void Envio_se_grava_con_la_tasa_predeterminada()
        {
            using var ctx = ConISV15();
            var r = ISVHelper.Calcular(ctx, UnItem(1, 100m), montoEnvio: 20m);
            Assert.Equal(120m, r.Subtotal);
            Assert.Equal(18m, r.ISVTotal);
            Assert.Equal(138m, r.Total);
        }
    }

    [Collection("BD")]
    public class CierreContableTests
    {
        [Fact]
        public void Mes_sin_registro_esta_abierto()
        {
            using var ctx = TestDb.Crear();
            var (ok, error) = CierreContableHelper.ValidarFecha(ctx, new DateTime(2026, 3, 10));
            Assert.True(ok);
            Assert.Null(error);
        }

        [Fact]
        public void Mes_cerrado_bloquea_y_reabierto_permite()
        {
            using var ctx = TestDb.Crear();

            var (okCierre, errCierre, _) = CierreContableHelper.Cerrar(ctx, 2026, 1, 1);
            Assert.True(okCierre, errCierre);
            Assert.True(CierreContableHelper.EstaCerrado(ctx, new DateTime(2026, 1, 15)));
            Assert.False(CierreContableHelper.ValidarFecha(ctx, new DateTime(2026, 1, 15)).ok);
            Assert.True(CierreContableHelper.ValidarFecha(ctx, new DateTime(2026, 2, 1)).ok);

            var (okRe, _) = CierreContableHelper.Reabrir(ctx, 2026, 1, 1, "Corrección de un asiento");
            Assert.True(okRe);
            Assert.True(CierreContableHelper.ValidarFecha(ctx, new DateTime(2026, 1, 15)).ok);
        }

        [Fact]
        public void No_se_puede_cerrar_dos_veces_ni_reabrir_sin_motivo()
        {
            using var ctx = TestDb.Crear();
            Assert.True(CierreContableHelper.Cerrar(ctx, 2026, 5, 1).ok);
            Assert.False(CierreContableHelper.Cerrar(ctx, 2026, 5, 1).ok);
            Assert.False(CierreContableHelper.Reabrir(ctx, 2026, 5, 1, "  ").ok);
        }

        [Fact]
        public void Mes_invalido_se_rechaza()
        {
            using var ctx = TestDb.Crear();
            Assert.False(CierreContableHelper.Cerrar(ctx, 2026, 13, 1).ok);
        }
    }

    public class AntiguedadEmpleadoTests
    {
        [Fact]
        public void Anios_cumplidos_respetan_el_aniversario()
        {
            var emp = new Empleado { FechaIngreso = new DateTime(2020, 6, 15) };
            Assert.Equal(5, EmpleadoHelper.CalcularAniosAntiguedad(emp, new DateTime(2026, 6, 14)));
            Assert.Equal(6, EmpleadoHelper.CalcularAniosAntiguedad(emp, new DateTime(2026, 6, 15)));
        }

        [Fact]
        public void Fecha_de_ingreso_futura_da_cero()
        {
            var emp = new Empleado { FechaIngreso = new DateTime(2030, 1, 1) };
            Assert.Equal(0, EmpleadoHelper.CalcularAniosAntiguedad(emp, new DateTime(2026, 1, 1)));
        }
    }
}
