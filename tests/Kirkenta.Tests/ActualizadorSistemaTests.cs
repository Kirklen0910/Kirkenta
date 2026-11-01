using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kirkenta.Services;
using Xunit;

namespace Kirkenta.Tests
{
    public class ActualizadorSistemaTests
    {
        private static (string Publica, string Privada) NuevasClaves()
        {
            using var rsa = RSA.Create(2048);
            return (rsa.ToXmlString(false), rsa.ToXmlString(true));
        }

        private static string Firmar(string privada, string texto)
        {
            using var rsa = RSA.Create();
            rsa.FromXmlString(privada);
            return Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(texto), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        private static JsonElement Manifiesto(string version, string archivo, string sha, string firma)
        {
            var json = JsonSerializer.Serialize(new { version, archivo, sha256 = sha, bytes = 1234, fecha = "2026-10-27", notas = "x", firma });
            return JsonDocument.Parse(json).RootElement;
        }

        private static readonly string Sha = new string('a', 64);
        private static string? Url(string nombre) => "https://github.com/u/r/releases/download/v1.0.9/" + nombre;

        [Fact]
        public void Manifiesto_firmado_correctamente_se_acepta()
        {
            var (pub, priv) = NuevasClaves();
            var firma = Firmar(priv, ActualizadorSistema.TextoFirmado("1.0.9", "Kirkenta-Setup-1.0.9.exe", Sha));
            var info = ActualizadorSistema.ValidarManifiesto(Manifiesto("1.0.9", "Kirkenta-Setup-1.0.9.exe", Sha, firma), pub, Url, out var error);
            Assert.NotNull(info);
            Assert.Equal("", error);
            Assert.Equal("1.0.9", info!.Version);
        }

        [Fact]
        public void Manifiesto_con_sha_cambiado_se_rechaza()
        {
            var (pub, priv) = NuevasClaves();
            var firma = Firmar(priv, ActualizadorSistema.TextoFirmado("1.0.9", "Kirkenta-Setup-1.0.9.exe", Sha));
            var otro = new string('b', 64);
            Assert.Null(ActualizadorSistema.ValidarManifiesto(Manifiesto("1.0.9", "Kirkenta-Setup-1.0.9.exe", otro, firma), pub, Url, out var error));
            Assert.Contains("firma", error);
        }

        [Fact]
        public void Manifiesto_firmado_con_otra_clave_se_rechaza()
        {
            var (pub, _) = NuevasClaves();
            var (_, privAjena) = NuevasClaves();
            var firma = Firmar(privAjena, ActualizadorSistema.TextoFirmado("1.0.9", "Kirkenta-Setup-1.0.9.exe", Sha));
            Assert.Null(ActualizadorSistema.ValidarManifiesto(Manifiesto("1.0.9", "Kirkenta-Setup-1.0.9.exe", Sha, firma), pub, Url, out _));
        }

        [Fact]
        public void Sin_clave_publica_nada_se_acepta()
        {
            var (_, priv) = NuevasClaves();
            var firma = Firmar(priv, ActualizadorSistema.TextoFirmado("1.0.9", "Kirkenta-Setup-1.0.9.exe", Sha));
            Assert.Null(ActualizadorSistema.ValidarManifiesto(Manifiesto("1.0.9", "Kirkenta-Setup-1.0.9.exe", Sha, firma), "", Url, out _));
        }

        [Theory]
        [InlineData("1.0.9", "Kirkenta-Setup-1.0.8.exe")]   // archivo de otra version
        [InlineData("1.0.9", "..\\malo.exe")]
        [InlineData("1.0", "Kirkenta-Setup-1.0.exe")]
        public void Nombres_o_versiones_raros_se_rechazan(string version, string archivo)
        {
            var (pub, priv) = NuevasClaves();
            var firma = Firmar(priv, ActualizadorSistema.TextoFirmado(version, archivo, Sha));
            Assert.Null(ActualizadorSistema.ValidarManifiesto(Manifiesto(version, archivo, Sha, firma), pub, Url, out _));
        }

        [Fact]
        public void Url_fuera_de_github_se_rechaza()
        {
            var (pub, priv) = NuevasClaves();
            var firma = Firmar(priv, ActualizadorSistema.TextoFirmado("1.0.9", "Kirkenta-Setup-1.0.9.exe", Sha));
            Assert.Null(ActualizadorSistema.ValidarManifiesto(Manifiesto("1.0.9", "Kirkenta-Setup-1.0.9.exe", Sha, firma), pub, _ => "https://malo.example.com/x.exe", out _));
        }

        [Theory]
        [InlineData("https://github.com/u/r/releases/download/v1/a.exe", true)]
        [InlineData("https://objects.githubusercontent.com/abc", true)]
        [InlineData("http://github.com/u/r/a.exe", false)]
        [InlineData("https://github.com.malo.com/a.exe", false)]
        [InlineData("https://evilgithubusercontent.com/a.exe", false)]
        [InlineData(null, false)]
        public void UrlPermitida_solo_github_por_https(string? url, bool esperado)
            => Assert.Equal(esperado, ActualizadorSistema.UrlPermitida(url));

        [Theory]
        [InlineData("1.0.8", "1.0.7", true)]
        [InlineData("1.0.7", "1.0.7", false)]
        [InlineData("1.0.6", "1.0.7", false)]
        [InlineData("1.1.0", "1.0.9", true)]
        [InlineData("2.0.0", "1.9.9", true)]
        [InlineData("basura", "1.0.0", false)]
        public void EsMasNueva_compara_versiones(string nueva, string actual, bool esperado)
            => Assert.Equal(esperado, ActualizadorSistema.EsMasNueva(nueva, Version.Parse(actual)));

        [Theory]
        [InlineData("usuario/kirkenta-releases", true)]
        [InlineData("", false)]
        [InlineData("sin-barra", false)]
        [InlineData("a/b/c", false)]
        [InlineData("a b/c", false)]
        public void Repositorio_valido(string repo, bool esperado)
            => Assert.Equal(esperado, ActualizadorSistema.EsRepositorioValido(repo));

        [Fact]
        public void EstadoActualizacion_avanza_y_permite_reintentar_tras_un_error()
        {
            Assert.True(EstadoActualizacion.Reiniciar("1.0.9"));
            Assert.False(EstadoActualizacion.Reiniciar("1.0.9"));   // ya hay una en curso

            EstadoActualizacion.Poner("descarga", 40, "Descargando");
            var a = EstadoActualizacion.Instantanea();
            Assert.True(a.Activa);
            Assert.Equal(40, a.Porcentaje);
            Assert.Equal("descarga", a.Fase);

            EstadoActualizacion.Poner("descarga", 500, "x");         // se limita a 100
            Assert.Equal(100, EstadoActualizacion.Instantanea().Porcentaje);

            EstadoActualizacion.Fallar("sin internet");
            var f = EstadoActualizacion.Instantanea();
            Assert.False(f.Activa);
            Assert.True(f.Error);
            Assert.Equal("sin internet", f.Mensaje);

            Assert.True(EstadoActualizacion.Reiniciar("1.0.9"));     // despues de un error se puede volver a intentar
            EstadoActualizacion.Fallar("fin de la prueba");
        }
    }
}
