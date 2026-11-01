using System.Security.Cryptography;
using Kirkenta.Helpers;
using Kirkenta.Services;
using Xunit;

namespace Kirkenta.Tests
{
    /// <summary>
    /// Endurecimiento de seguridad: respaldos cifrados, sesiones ligadas a la contraseña y filtro de archivos al restaurar.
    /// Van en la colección "BD" porque el cifrado y el cache de permisos son estáticos (no se pueden correr en paralelo).
    /// </summary>
    [Collection("BD")]
    public class SeguridadAvanzadaTests
    {
        private const string ClavePrueba = "ClaveDePruebaMuyLarga2026";

        private static string Temporal(string sufijo) =>
            Path.Combine(Path.GetTempPath(), "kk-seg-" + Guid.NewGuid().ToString("N") + sufijo);

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1024 * 1024 - 1)]
        [InlineData(1024 * 1024)]
        [InlineData(1024 * 1024 + 1)]
        [InlineData(3 * 1024 * 1024 + 17)]
        public void Cifrado_ida_y_vuelta_devuelve_exactamente_el_mismo_contenido(int tamano)
        {
            var origen = Temporal(".zip"); var cifrado = Temporal(".cif"); var descifrado = Temporal(".out");
            try
            {
                var datos = RandomNumberGenerator.GetBytes(tamano);
                File.WriteAllBytes(origen, datos);

                RespaldoCifrado.Configurar(ClavePrueba);
                Assert.True(RespaldoCifrado.Activo);
                RespaldoCifrado.CifrarArchivo(origen, cifrado);

                Assert.True(RespaldoCifrado.EstaCifrado(cifrado));
                Assert.False(RespaldoCifrado.EstaCifrado(origen) && tamano > 8);

                RespaldoCifrado.DescifrarArchivo(cifrado, descifrado);
                Assert.Equal(datos, File.ReadAllBytes(descifrado));
            }
            finally
            {
                RespaldoCifrado.Configurar(null);
                foreach (var f in new[] { origen, cifrado, descifrado }) { try { File.Delete(f); } catch { } }
            }
        }

        [Fact]
        public void El_archivo_cifrado_no_contiene_el_texto_original()
        {
            var origen = Temporal(".zip"); var cifrado = Temporal(".cif");
            try
            {
                var secreto = System.Text.Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("CLIENTE-CONFIDENCIAL-RTN-0801", 200)));
                File.WriteAllBytes(origen, secreto);
                RespaldoCifrado.Configurar(ClavePrueba);
                RespaldoCifrado.CifrarArchivo(origen, cifrado);

                var contenido = File.ReadAllBytes(cifrado);
                Assert.DoesNotContain("CONFIDENCIAL", System.Text.Encoding.Latin1.GetString(contenido));
            }
            finally
            {
                RespaldoCifrado.Configurar(null);
                foreach (var f in new[] { origen, cifrado }) { try { File.Delete(f); } catch { } }
            }
        }

        [Fact]
        public void Con_otra_clave_no_se_puede_descifrar()
        {
            var origen = Temporal(".zip"); var cifrado = Temporal(".cif"); var salida = Temporal(".out");
            try
            {
                File.WriteAllBytes(origen, RandomNumberGenerator.GetBytes(5000));
                RespaldoCifrado.Configurar(ClavePrueba);
                RespaldoCifrado.CifrarArchivo(origen, cifrado);

                RespaldoCifrado.Configurar("UnaClaveDistintaTotalmente99");
                Assert.Throws<InvalidDataException>(() => RespaldoCifrado.DescifrarArchivo(cifrado, salida));
            }
            finally
            {
                RespaldoCifrado.Configurar(null);
                foreach (var f in new[] { origen, cifrado, salida }) { try { File.Delete(f); } catch { } }
            }
        }

        [Fact]
        public void Sin_clave_configurada_un_respaldo_cifrado_no_se_abre()
        {
            var origen = Temporal(".zip"); var cifrado = Temporal(".cif"); var salida = Temporal(".out");
            try
            {
                File.WriteAllBytes(origen, RandomNumberGenerator.GetBytes(100));
                RespaldoCifrado.Configurar(ClavePrueba);
                RespaldoCifrado.CifrarArchivo(origen, cifrado);

                RespaldoCifrado.Configurar(null);
                Assert.False(RespaldoCifrado.Activo);
                Assert.Throws<InvalidOperationException>(() => RespaldoCifrado.DescifrarArchivo(cifrado, salida));
            }
            finally
            {
                RespaldoCifrado.Configurar(null);
                foreach (var f in new[] { origen, cifrado, salida }) { try { File.Delete(f); } catch { } }
            }
        }

        [Fact]
        public void Un_respaldo_alterado_o_recortado_se_detecta()
        {
            var origen = Temporal(".zip"); var cifrado = Temporal(".cif"); var alterado = Temporal(".alt"); var recortado = Temporal(".rec"); var salida = Temporal(".out");
            try
            {
                File.WriteAllBytes(origen, RandomNumberGenerator.GetBytes(2 * 1024 * 1024 + 500));
                RespaldoCifrado.Configurar(ClavePrueba);
                RespaldoCifrado.CifrarArchivo(origen, cifrado);

                var bytes = File.ReadAllBytes(cifrado);

                // un solo byte cambiado en medio del archivo
                var copia = (byte[])bytes.Clone();
                copia[copia.Length / 2] ^= 0x01;
                File.WriteAllBytes(alterado, copia);
                Assert.Throws<InvalidDataException>(() => RespaldoCifrado.DescifrarArchivo(alterado, salida));

                // el archivo cortado (le falta el último bloque)
                File.WriteAllBytes(recortado, bytes.Take(bytes.Length - 600).ToArray());
                Assert.Throws<InvalidDataException>(() => RespaldoCifrado.DescifrarArchivo(recortado, salida));
            }
            finally
            {
                RespaldoCifrado.Configurar(null);
                foreach (var f in new[] { origen, cifrado, alterado, recortado, salida }) { try { File.Delete(f); } catch { } }
            }
        }

        [Fact]
        public void Sello_de_sesion_cambia_cuando_cambia_la_contrasena()
        {
            var a = SesionHelper.CalcularSello("hash-uno");
            Assert.Equal(a, SesionHelper.CalcularSello("hash-uno"));
            Assert.NotEqual(a, SesionHelper.CalcularSello("hash-dos"));
            Assert.Equal(24, a.Length);
            Assert.DoesNotContain("hash-uno", a);
        }

        [Fact]
        public void Una_sesion_abierta_deja_de_valer_al_cambiar_la_contrasena()
        {
            using var ctx = TestDb.Crear();
            var u = TestDb.AgregarUsuario(ctx, "ana", "Admin");
            var selloOriginal = SesionHelper.CalcularSello(u.PasswordHash);

            Assert.True(PermisoHelper.SesionVigente(ctx, "ana", selloOriginal));
            Assert.False(PermisoHelper.SesionVigente(ctx, "ana", "otro-sello"));
            Assert.False(PermisoHelper.SesionVigente(ctx, "ana", null));
            Assert.False(PermisoHelper.SesionVigente(ctx, "no-existe", selloOriginal));

            // alguien (o el mismo usuario) cambia la contraseña: la cookie vieja ya no sirve
            u.PasswordHash = "otro-hash-nuevo";
            ctx.SaveChanges();

            Assert.False(PermisoHelper.SesionVigente(ctx, "ana", selloOriginal));
            Assert.True(PermisoHelper.SesionVigente(ctx, "ana", SesionHelper.CalcularSello("otro-hash-nuevo")));
        }

        [Theory]
        [InlineData("wwwroot/uploads/empleados/E1/foto.png", true)]
        [InlineData("wwwroot/uploads/cierres/C1/boleta.pdf", true)]
        [InlineData("wwwroot/images/logo.png", true)]
        [InlineData("App_Data/licencia.dat", true)]
        [InlineData("wwwroot/uploads/pagina.html", false)]
        [InlineData("wwwroot/uploads/x/script.js", false)]
        [InlineData("wwwroot/uploads/logo.svg", false)]
        [InlineData("wwwroot/images/icono.svg", true)]
        [InlineData("wwwroot/uploads/programa.exe", false)]
        [InlineData("App_Data/primer-acceso.txt", false)]
        [InlineData("wwwroot\\uploads\\malo.PS1", false)]
        public void Al_restaurar_se_omiten_archivos_ejecutables_y_la_clave_de_primer_acceso(string ruta, bool permitido)
        {
            Assert.Equal(permitido, RespaldoSistemaService.ArchivoPermitidoAlRestaurar(ruta));
        }
    }
}
