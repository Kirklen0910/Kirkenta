using Kirkenta.Services;
using Xunit;

namespace Kirkenta.Tests
{
    public class RespaldoConfigTests
    {
        [Fact]
        public void Validar_acepta_configuracion_normal_y_rechaza_datos_malos()
        {
            Assert.Null(RespaldoConfigStore.Validar(new RespaldoConfig { Hora = "02:00", Conservar = 14, CopiaExterna = null }));
            Assert.Null(RespaldoConfigStore.Validar(new RespaldoConfig { Hora = "23:30", Conservar = 30, CopiaExterna = Path.GetTempPath() }));
            Assert.NotNull(RespaldoConfigStore.Validar(new RespaldoConfig { Hora = "25:00", Conservar = 14 }));
            Assert.NotNull(RespaldoConfigStore.Validar(new RespaldoConfig { Hora = "abc", Conservar = 14 }));
            Assert.NotNull(RespaldoConfigStore.Validar(new RespaldoConfig { Hora = "02:00", Conservar = 0 }));
            Assert.NotNull(RespaldoConfigStore.Validar(new RespaldoConfig { Hora = "02:00", Conservar = 14, CopiaExterna = "respaldos-relativos" }));
        }

        [Fact]
        public void Copia_externa_copia_y_conserva_solo_los_ultimos()
        {
            var origen = Path.Combine(Path.GetTempPath(), "kk-orig-" + Guid.NewGuid().ToString("N"));
            var destino = Path.Combine(Path.GetTempPath(), "kk-dest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(origen);
            try
            {
                Directory.CreateDirectory(destino);
                // tres respaldos viejos ya en el destino
                for (int i = 1; i <= 3; i++)
                {
                    var viejo = Path.Combine(destino, $"kirkenta-datos_2026010{i}_020000.zip");
                    File.WriteAllText(viejo, "x");
                    File.SetLastWriteTime(viejo, new DateTime(2026, 1, i));
                }
                var nuevo = Path.Combine(origen, "kirkenta-datos_20261025_020000.zip");
                File.WriteAllText(nuevo, "nuevo");

                var (ok, _) = RespaldoConfigStore.CopiarAExterna(nuevo, destino, 2);

                Assert.True(ok);
                var quedan = Directory.GetFiles(destino, "kirkenta-datos_*.zip").Select(Path.GetFileName).OrderBy(x => x).ToList();
                Assert.Equal(2, quedan.Count);
                Assert.Contains("kirkenta-datos_20261025_020000.zip", quedan);
            }
            finally
            {
                try { Directory.Delete(origen, true); } catch { }
                try { Directory.Delete(destino, true); } catch { }
            }
        }

        [Fact]
        public void Probar_carpeta_escribe_y_borra_el_archivo_de_prueba()
        {
            var carpeta = Path.Combine(Path.GetTempPath(), "kk-prueba-" + Guid.NewGuid().ToString("N"));
            try
            {
                var (ok, _) = RespaldoConfigStore.ProbarCarpeta(carpeta);
                Assert.True(ok);
                Assert.Empty(Directory.GetFiles(carpeta));
            }
            finally { try { Directory.Delete(carpeta, true); } catch { } }
        }
    }
}
