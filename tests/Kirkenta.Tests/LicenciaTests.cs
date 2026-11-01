using System.Security.Cryptography;
using Kirkenta.Services.Licencia;
using Xunit;

namespace Kirkenta.Tests
{
    public class LicenciaTests
    {
        private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        private const string Maq = "ABCD-1234-ABCD-1234";

        private static LicenciaDatos Demo(int dias = 7) => new()
        {
            Id = "abc", Cliente = "Cliente", Tipo = "demo", Dias = dias, Maquina = Maq, Emitida = T0, ActivarAntes = T0.AddDays(3)
        };

        private static LicenciaEstadoInterno Estado(DateTime activada, DateTime? vista = null) => new()
        {
            LicId = "abc", ActivadaUtc = activada, UltimaVistaUtc = vista ?? activada
        };

        [Fact]
        public void Firma_y_lectura_funcionan_y_detectan_alteraciones()
        {
            using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var pub = Convert.ToBase64String(ec.ExportSubjectPublicKeyInfo());
            var texto = LicenciaFormato.Firmar(Demo(15), ec);

            var d = LicenciaFormato.Leer(texto, pub, out var err);
            Assert.NotNull(d);
            Assert.Equal(15, d!.Dias);
            Assert.Equal(Maq, d.Maquina);

            // Con saltos de línea y espacios (como llega por correo o WhatsApp) sigue valiendo
            Assert.NotNull(LicenciaFormato.Leer(texto.Insert(20, "\r\n ").Insert(60, " "), pub, out _));

            // Cambiar un solo carácter de los datos invalida la firma
            var partes = texto.Split('.');
            var datosAlterados = partes[0][..^2] + (partes[0][^2] == 'A' ? "B" : "A") + partes[0][^1];
            Assert.Null(LicenciaFormato.Leer(datosAlterados + "." + partes[1], pub, out err));
            Assert.NotEqual("", err);

            // Una licencia firmada con OTRA llave no vale
            using var otra = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            Assert.Null(LicenciaFormato.Leer(LicenciaFormato.Firmar(Demo(), otra), pub, out _));

            Assert.Null(LicenciaFormato.Leer("hola", pub, out _));
            Assert.Null(LicenciaFormato.Leer(null, pub, out _));
        }

        [Fact]
        public void Demo_de_7_dias_se_agota_aunque_la_maquina_haya_estado_apagada()
        {
            var lic = Demo(7);
            var st = Estado(T0); // activada el día 1, el sistema la vio por última vez ese día

            // Apagada 6 días y se enciende: aún queda 1 día
            var v6 = LicenciaService.Evaluar(lic, st, T0.AddDays(6), Maq, true);
            Assert.True(v6.Permitido);
            Assert.Equal(1, v6.DiasRestantes);

            // Apagada 8 días: el calendario real ya cumplió la demo
            var v8 = LicenciaService.Evaluar(lic, st, T0.AddDays(8), Maq, true);
            Assert.False(v8.Permitido);
            Assert.Equal("vencida", v8.Motivo);

            // Justo en el límite
            Assert.False(LicenciaService.Evaluar(lic, st, T0.AddDays(7), Maq, true).Permitido);
            Assert.True(LicenciaService.Evaluar(lic, st, T0.AddDays(7).AddMinutes(-1), Maq, true).Permitido);
        }

        [Fact]
        public void Retroceder_el_reloj_se_detecta_y_el_bloqueo_no_se_quita_volviendo_la_fecha()
        {
            var lic = Demo(30);
            var st = Estado(T0, T0.AddDays(10));            // el sistema ya vio el día 10

            Assert.False(LicenciaService.HuboRetroceso(lic, st, T0.AddDays(10).AddMinutes(-3)));  // ajuste normal de reloj
            Assert.True(LicenciaService.HuboRetroceso(lic, st, T0.AddDays(2)));                    // lo regresaron al día 2

            st.RelojAlterado = true; st.BloqueoLicId = lic.Id;
            var v = LicenciaService.Evaluar(lic, st, T0.AddDays(11), Maq, true);   // aunque la fecha vuelva a ser normal
            Assert.False(v.Permitido);
            Assert.Equal("reloj", v.Motivo);
        }

        [Fact]
        public void Aunque_se_regrese_el_reloj_el_vencimiento_usa_la_fecha_mas_avanzada_vista()
        {
            var lic = Demo(7);
            var st = Estado(T0, T0.AddDays(9));    // ya se había visto el día 9 (la demo ya venció)
            var v = LicenciaService.Evaluar(lic, st, T0.AddDays(1), Maq, true);
            Assert.False(v.Permitido);
        }

        [Fact]
        public void Licencia_sin_vencimiento_no_se_bloquea_por_fecha()
        {
            var lic = Demo(0); lic.Tipo = "normal";
            var st = Estado(T0, T0.AddDays(400));
            Assert.False(LicenciaService.HuboRetroceso(lic, st, T0));
            Assert.True(LicenciaService.Evaluar(lic, st, T0, Maq, true).Permitido);
        }

        [Fact]
        public void Licencias_por_6_meses_1_3_y_5_anios_respetan_su_duracion()
        {
            foreach (var dias in new[] { 183, 365, 1095, 1825 })
            {
                var lic = Demo(dias); lic.Tipo = "normal";
                var st = Estado(T0);
                Assert.True(LicenciaService.Evaluar(lic, st, T0.AddDays(dias - 1), Maq, true).Permitido);
                Assert.False(LicenciaService.Evaluar(lic, st, T0.AddDays(dias + 1), Maq, true).Permitido);
            }
        }

        [Fact]
        public void Inicio_fijo_cuenta_desde_esa_fecha_y_no_desde_la_activacion()
        {
            var lic = Demo(30); lic.Inicio = T0;            // la vigencia empezó el día 0
            var st = Estado(T0.AddDays(20));                 // pero se activó (o se perdió el estado) el día 20
            var v = LicenciaService.Evaluar(lic, st, T0.AddDays(21), Maq, true);
            Assert.True(v.Permitido);
            Assert.Equal(9, v.DiasRestantes);
        }

        [Fact]
        public void Licencia_de_otra_computadora_no_sirve_pero_la_libre_si()
        {
            var lic = Demo(); var st = Estado(T0);
            var otra = LicenciaService.Evaluar(lic, st, T0.AddDays(1), "ZZZZ-0000-ZZZZ-0000", true);
            Assert.False(otra.Permitido);
            Assert.Equal("maquina", otra.Motivo);

            lic.Maquina = "*";
            Assert.True(LicenciaService.Evaluar(lic, st, T0.AddDays(1), "ZZZZ-0000-ZZZZ-0000", true).Permitido);

            // El código se compara sin importar guiones ni mayúsculas
            Assert.Equal(LicenciaService.NormCodigo("abcd-1234-abcd-1234"), LicenciaService.NormCodigo(Maq));
        }

        [Fact]
        public void Sin_licencia_bloquea_y_con_licencias_desactivadas_no_bloquea()
        {
            Assert.False(LicenciaService.Evaluar(null, null, T0, Maq, true).Permitido);
            Assert.True(LicenciaService.Evaluar(null, null, T0, Maq, false).Permitido);
        }

        [Fact]
        public void Licencias_cortas_de_minutos_y_horas_vencen_a_tiempo()
        {
            foreach (var minutos in new[] { 30, 60, 180, 720 })
            {
                var lic = Demo(1); lic.Minutos = minutos;      // las cortas llevan Dias = 1 por seguridad
                var st = Estado(T0);

                var antes = LicenciaService.Evaluar(lic, st, T0.AddMinutes(minutos - 1), Maq, true);
                Assert.True(antes.Permitido);
                Assert.Equal(1, antes.DiasRestantes);

                var despues = LicenciaService.Evaluar(lic, st, T0.AddMinutes(minutos + 1), Maq, true);
                Assert.False(despues.Permitido);
                Assert.Equal("vencida", despues.Motivo);
            }
        }

        [Fact]
        public void Texto_de_tiempo_restante_es_legible()
        {
            Assert.Equal("40 minuto(s)", LicenciaService.Texto(TimeSpan.FromMinutes(40)));
            Assert.Equal("2 hora(s)", LicenciaService.Texto(TimeSpan.FromHours(2)));
            Assert.Equal("1 hora(s) 15 minuto(s)", LicenciaService.Texto(TimeSpan.FromMinutes(75)));
            Assert.Equal("3 día(s)", LicenciaService.Texto(TimeSpan.FromDays(2.5)));
        }

        [Fact]
        public void Retroceso_de_reloj_tambien_bloquea_las_licencias_cortas()
        {
            var lic = Demo(1); lic.Minutos = 180;
            var st = Estado(T0, T0.AddHours(2));
            Assert.True(LicenciaService.HuboRetroceso(lic, st, T0.AddMinutes(30)));
            Assert.False(LicenciaService.HuboRetroceso(lic, st, T0.AddHours(2).AddMinutes(-2)));
        }

        [Fact]
        public void Codigo_de_maquina_es_estable_y_tiene_el_formato_esperado()
        {
            var dir = Path.Combine(Path.GetTempPath(), "kk-lic-" + Guid.NewGuid().ToString("N"));
            try
            {
                var a = LicenciaService.ObtenerCodigoMaquina(dir);
                var b = LicenciaService.ObtenerCodigoMaquina(dir);
                Assert.Equal(a, b);
                Assert.Matches(@"^[0-9A-F]{4}(-[0-9A-F]{4}){3}$", a);
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }
    }
}
