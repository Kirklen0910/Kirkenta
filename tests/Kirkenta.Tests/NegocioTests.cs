using Kirkenta.Helpers;
using Kirkenta.Services;
using Xunit;

namespace Kirkenta.Tests
{
    public class NegocioTests
    {
        // ───────── Sugerencia de compras ─────────
        [Theory]
        [InlineData(2, 30, 10, 20, 0, 50)]   // vende 2/día × 30 = 60 + mínimo 10 − stock 20 = 50
        [InlineData(2, 30, 10, 20, 15, 35)]  // lo que ya viene en camino se descuenta
        [InlineData(2, 30, 10, 100, 0, 0)]   // stock de sobra
        [InlineData(0, 30, 5, 3, 0, 2)]      // sin ventas: solo se repone hasta el mínimo
        [InlineData(0.3, 30, 0, 0, 0, 9)]    // 9.0 exacto
        [InlineData(0.31, 30, 0, 0, 0, 10)]  // 9.3 se redondea hacia arriba
        public void Sugerido_se_calcula_con_ventas_minimo_stock_y_pedidos_en_camino(
            double demanda, int cobertura, double minimo, double stock, double enCamino, double esperado)
        {
            var r = SugerenciaComprasHelper.Calcular((decimal)demanda, cobertura, (decimal)minimo, (decimal)stock, (decimal)enCamino);
            Assert.Equal((decimal)esperado, r);
        }

        // ───────── Respaldo ─────────
        [Fact]
        public void Sin_respaldo_avisa_en_rojo()
        {
            var a = RespaldoEstado.Evaluar(null, new DateTime(2026, 10, 25, 22, 0, 0));
            Assert.NotNull(a);
            Assert.Equal("danger", a!.Nivel);
        }

        [Fact]
        public void Respaldo_de_anoche_no_avisa()
        {
            var ahora = new DateTime(2026, 10, 25, 22, 0, 0);
            Assert.Null(RespaldoEstado.Evaluar(ahora.AddHours(-23), ahora));
            Assert.Null(RespaldoEstado.Evaluar(ahora.AddHours(-35), ahora));
        }

        [Fact]
        public void Respaldo_viejo_avisa_y_sube_de_nivel_a_los_3_dias()
        {
            var ahora = new DateTime(2026, 10, 25, 22, 0, 0);
            Assert.Equal("warning", RespaldoEstado.Evaluar(ahora.AddHours(-50), ahora)!.Nivel);
            Assert.Equal("danger", RespaldoEstado.Evaluar(ahora.AddDays(-4), ahora)!.Nivel);
        }

        [Fact]
        public void Leer_toma_el_zip_mas_reciente_e_ignora_archivos_vacios_u_otros()
        {
            var dir = Path.Combine(Path.GetTempPath(), "kk-resp-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var viejo = Path.Combine(dir, "kirkenta-1.zip");
                var nuevo = Path.Combine(dir, "kirkenta-2.zip");
                var vacio = Path.Combine(dir, "kirkenta-3.zip");
                File.WriteAllBytes(viejo, new byte[2048]);
                File.WriteAllBytes(nuevo, new byte[4096]);
                File.WriteAllBytes(vacio, new byte[10]);
                File.WriteAllText(Path.Combine(dir, "nota.txt"), "x");
                File.SetLastWriteTime(viejo, new DateTime(2026, 10, 20, 23, 0, 0));
                File.SetLastWriteTime(nuevo, new DateTime(2026, 10, 24, 23, 0, 0));
                File.SetLastWriteTime(vacio, new DateTime(2026, 10, 25, 23, 0, 0));

                var e = RespaldoEstado.Leer(dir);
                Assert.Equal(2, e.Cantidad);
                Assert.Equal(new DateTime(2026, 10, 24, 23, 0, 0), e.Ultimo);
                Assert.Equal(4096, e.UltimoBytes);
            }
            finally { Directory.Delete(dir, true); }
        }

        [Fact]
        public void Leer_carpeta_inexistente_no_falla()
        {
            var e = RespaldoEstado.Leer(Path.Combine(Path.GetTempPath(), "no-existe-" + Guid.NewGuid().ToString("N")));
            Assert.Null(e.Ultimo);
            Assert.Equal(0, e.Cantidad);
        }

        // ───────── Resumen diario por correo ─────────
        [Theory]
        [InlineData(19, 59, false)]  // todavía no es la hora
        [InlineData(20, 0, true)]
        [InlineData(23, 30, true)]   // si la app estaba apagada a las 8, lo manda cuando prende ese mismo día
        public void Resumen_diario_se_envia_despues_de_la_hora(int h, int m, bool esperado)
        {
            var ahora = new DateTime(2026, 10, 25, h, m, 0);
            Assert.Equal(esperado, ResumenDiarioService.ToqueEnviar(ahora, new TimeSpan(20, 0, 0), new DateTime(2026, 10, 24, 20, 1, 0)));
        }

        [Fact]
        public void Resumen_diario_no_se_repite_el_mismo_dia()
        {
            var ahora = new DateTime(2026, 10, 25, 21, 0, 0);
            Assert.False(ResumenDiarioService.ToqueEnviar(ahora, new TimeSpan(20, 0, 0), new DateTime(2026, 10, 25, 20, 0, 5)));
        }

        // ───────── Respaldo / restauración de datos ─────────
        [Fact]
        public void RespaldoValores_RoundTrip()
        {
            var ahora = new DateTime(2026, 10, 25, 13, 45, 12, 123);
            var casos = new (object valor, string tipo)[]
            {
                (ahora, "System.DateTime"),
                (1234.5678m, "System.Decimal"),
                (new byte[] { 1, 2, 250 }, "System.Byte[]"),
                ("Hola ñandú \"x\"", "System.String"),
                (42, "System.Int32"),
                (9000000000L, "System.Int64"),
                (true, "System.Boolean"),
                (Guid.Parse("11111111-2222-3333-4444-555555555555"), "System.Guid"),
                (new TimeSpan(1, 2, 3), "System.TimeSpan"),
            };
            foreach (var (valor, tipo) in casos)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(new[] { RespaldoSistemaService.ValorAJson(valor) });
                var el = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(json)![0];
                var vuelta = RespaldoSistemaService.JsonAValor(el, tipo);
                if (valor is byte[] b) Assert.Equal(b, (byte[])vuelta!);
                else Assert.Equal(valor, vuelta);
            }
        }

        [Fact]
        public void RespaldoValores_NuloVuelveDBNull()
        {
            var el = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>("[null]")![0];
            Assert.Equal(DBNull.Value, RespaldoSistemaService.JsonAValor(el, "System.String"));
            Assert.Null(RespaldoSistemaService.ValorAJson(null));
        }

        [Fact]
        public void RespaldoAutomatico_TocaUnaVezAlDia()
        {
            var hora = new TimeSpan(2, 0, 0);
            Assert.False(RespaldoAutomaticoService.Toca(new DateTime(2026, 10, 25, 1, 59, 0), hora, DateTime.MinValue));
            Assert.True(RespaldoAutomaticoService.Toca(new DateTime(2026, 10, 25, 2, 5, 0), hora, new DateTime(2026, 10, 24, 2, 5, 0)));
            Assert.False(RespaldoAutomaticoService.Toca(new DateTime(2026, 10, 25, 9, 0, 0), hora, new DateTime(2026, 10, 25, 2, 5, 0)));
        }

        [Fact]
        public void RespaldoGrupos_SinTablasRepetidas()
        {
            var todas = RespaldoSistemaService.Grupos.Values.SelectMany(x => x).ToList();
            Assert.Equal(todas.Count, todas.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            var set = RespaldoSistemaService.TablasDeGrupos(new[] { "Clientes", "NoExiste" });
            Assert.Single(set);
            Assert.Contains("Clientes", set);
        }

        [Fact]
        public void RespaldoError_SeRegistraYSeLimpia()
        {
            var dir = Path.Combine(Path.GetTempPath(), "kk-resp-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                Assert.Null(RespaldoEstado.LeerError(dir));
                RespaldoEstado.RegistrarError(dir, "disco lleno");
                var err = RespaldoEstado.LeerError(dir);
                Assert.NotNull(err);
                Assert.Equal("disco lleno", err!.Value.Mensaje);
                RespaldoEstado.LimpiarError(dir);
                Assert.Null(RespaldoEstado.LeerError(dir));
            }
            finally { Directory.Delete(dir, true); }
        }
    }
}
