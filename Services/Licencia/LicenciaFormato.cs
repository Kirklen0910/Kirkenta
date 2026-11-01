using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Kirkenta.Services.Licencia
{
    /// <summary>Contenido firmado de una licencia. Este archivo lo comparten el sistema y la herramienta del emisor.</summary>
    public class LicenciaDatos
    {
        public int V { get; set; } = 1;
        /// <summary>Identificador único de la licencia (GUID sin guiones).</summary>
        public string Id { get; set; } = "";
        public string Cliente { get; set; } = "";
        /// <summary>"demo" o "normal".</summary>
        public string Tipo { get; set; } = "demo";
        /// <summary>Días de vigencia. 0 = sin vencimiento.</summary>
        public int Dias { get; set; }
        /// <summary>Si es mayor que 0, la vigencia es de estos minutos (licencias cortas: 30 min, 1 h, 3 h, 12 h) y manda sobre Dias.
        /// Las licencias cortas llevan además Dias = 1, para que una versión vieja del sistema nunca las tome como "sin vencimiento".</summary>
        public int Minutos { get; set; }
        /// <summary>Código de la computadora (XXXX-XXXX-XXXX-XXXX) o "*" para cualquiera.</summary>
        public string Maquina { get; set; } = "*";
        public DateTime Emitida { get; set; }
        /// <summary>Fecha límite para activarla por primera vez (evita guardar licencias para usarlas después).</summary>
        public DateTime? ActivarAntes { get; set; }
        /// <summary>Si se indica, la vigencia cuenta desde esta fecha en vez de desde la activación.</summary>
        public DateTime? Inicio { get; set; }
    }

    /// <summary>Formato de la licencia: KRK1-&lt;datos&gt;.&lt;firma ECDSA P-256&gt; (texto que se pega en la pantalla de activación).</summary>
    public static class LicenciaFormato
    {
        public const string Prefijo = "KRK1-";

        private static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] DeB64(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
            return Convert.FromBase64String(s);
        }

        public static string Normalizar(string? texto) => new string((texto ?? "").Where(c => !char.IsWhiteSpace(c)).ToArray());

        public static string Firmar(LicenciaDatos d, ECDsa llavePrivada)
        {
            var payload = B64(JsonSerializer.SerializeToUtf8Bytes(d));
            var firma = llavePrivada.SignData(Encoding.ASCII.GetBytes(payload), HashAlgorithmName.SHA256);
            return Prefijo + payload + "." + B64(firma);
        }

        /// <summary>Verifica la firma y devuelve los datos; null (con el motivo en <paramref name="error"/>) si no sirve.</summary>
        public static LicenciaDatos? Leer(string? texto, string clavePublicaBase64, out string error)
        {
            error = "";
            try
            {
                var t = Normalizar(texto);
                if (!t.StartsWith(Prefijo, StringComparison.Ordinal)) { error = "El texto no parece una licencia de Kirkenta."; return null; }
                var partes = t[Prefijo.Length..].Split('.');
                if (partes.Length != 2) { error = "La licencia está incompleta o dañada."; return null; }

                using var ec = ECDsa.Create();
                ec.ImportSubjectPublicKeyInfo(Convert.FromBase64String(clavePublicaBase64), out _);
                if (!ec.VerifyData(Encoding.ASCII.GetBytes(partes[0]), DeB64(partes[1]), HashAlgorithmName.SHA256))
                {
                    error = "La licencia no es auténtica (firma inválida).";
                    return null;
                }

                var d = JsonSerializer.Deserialize<LicenciaDatos>(DeB64(partes[0]));
                if (d == null || string.IsNullOrWhiteSpace(d.Id)) { error = "La licencia está dañada."; return null; }
                if (d.Dias < 0 || d.Minutos < 0) { error = "La licencia tiene datos inválidos."; return null; }
                return d;
            }
            catch
            {
                error = "No se pudo leer la licencia. Cópiala completa, sin cortar ni agregar caracteres.";
                return null;
            }
        }
    }
}
