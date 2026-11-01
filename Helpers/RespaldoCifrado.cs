using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Cifrado opcional de los respaldos. Si en la configuración existe Seguridad:ClaveRespaldos, cada respaldo
    /// que crea el sistema queda cifrado con AES-256-GCM (por bloques de 1 MB, con autenticación) y la clave se
    /// deriva con PBKDF2-SHA256 (210 000 vueltas + sal aleatoria por archivo). Sin la clave el archivo es ilegible:
    /// si roban la memoria USB o la carpeta externa donde se copia el respaldo, no pueden leer los datos.
    ///
    /// IMPORTANTE: quien pierda la clave pierde la posibilidad de restaurar esos respaldos. Se debe guardar
    /// la clave fuera de la computadora del sistema.
    ///
    /// Formato: "KKBK1\n" | sal (16) | bloques: [largo (4, LE)] [etiqueta (16)] [datos cifrados].
    /// El último bloque es el único con menos de 1 MB (puede ser de 0 bytes); el contador y la marca de "último"
    /// van autenticados, así que no se puede truncar, reordenar ni recortar el archivo sin que se detecte.
    /// </summary>
    public static class RespaldoCifrado
    {
        private static readonly byte[] Cabecera = Encoding.ASCII.GetBytes("KKBK1\n");
        private const int TamanoBloque = 1024 * 1024;
        private const int TamanoSal = 16;
        private const int TamanoNonce = 12;
        private const int TamanoEtiqueta = 16;
        private const int Iteraciones = 210_000;

        private static string? _clave;

        /// <summary>Se llama una vez al arrancar con el valor de Seguridad:ClaveRespaldos (vacío = sin cifrado).</summary>
        public static void Configurar(string? clave)
        {
            _clave = string.IsNullOrWhiteSpace(clave) ? null : clave.Trim();
        }

        /// <summary>true si los respaldos nuevos se están cifrando.</summary>
        public static bool Activo => _clave != null;

        public static bool EstaCifrado(string ruta)
        {
            try
            {
                using var fs = File.OpenRead(ruta);
                var inicio = new byte[Cabecera.Length];
                return LeerCompleto(fs, inicio, inicio.Length) == inicio.Length && inicio.AsSpan().SequenceEqual(Cabecera);
            }
            catch
            {
                return false;
            }
        }

        public static void CifrarArchivo(string origen, string destino)
        {
            if (_clave == null) throw new InvalidOperationException("No hay clave de respaldos configurada.");

            var sal = RandomNumberGenerator.GetBytes(TamanoSal);
            var llave = DerivarClave(sal, _clave);
            try
            {
                using var aes = new AesGcm(llave, TamanoEtiqueta);
                using var entrada = File.OpenRead(origen);
                using var salida = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None);

                salida.Write(Cabecera, 0, Cabecera.Length);
                salida.Write(sal, 0, sal.Length);

                var plano = new byte[TamanoBloque];
                var cifrado = new byte[TamanoBloque];
                var nonce = new byte[TamanoNonce];
                var aad = new byte[9];
                var etiqueta = new byte[TamanoEtiqueta];
                var largoBytes = new byte[4];
                ulong contador = 0;

                while (true)
                {
                    var leidos = LeerCompleto(entrada, plano, plano.Length);
                    var ultimo = leidos < TamanoBloque;
                    Preparar(nonce, aad, contador, ultimo);

                    aes.Encrypt(nonce, plano.AsSpan(0, leidos), cifrado.AsSpan(0, leidos), etiqueta, aad);

                    BinaryPrimitives.WriteInt32LittleEndian(largoBytes, leidos);
                    salida.Write(largoBytes, 0, largoBytes.Length);
                    salida.Write(etiqueta, 0, etiqueta.Length);
                    salida.Write(cifrado, 0, leidos);

                    contador++;
                    if (ultimo) break;
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(llave);
            }
        }

        public static void DescifrarArchivo(string origen, string destino)
        {
            if (_clave == null)
                throw new InvalidOperationException(
                    "Este respaldo está cifrado y falta la clave de respaldos (Seguridad:ClaveRespaldos). " +
                    "Configúrala con la misma clave con que se creó el respaldo.");

            using var entrada = File.OpenRead(origen);

            var cab = new byte[Cabecera.Length];
            if (LeerCompleto(entrada, cab, cab.Length) != cab.Length || !cab.AsSpan().SequenceEqual(Cabecera))
                throw new InvalidDataException("El archivo no es un respaldo cifrado de Kirkenta.");

            var sal = new byte[TamanoSal];
            if (LeerCompleto(entrada, sal, sal.Length) != sal.Length)
                throw new InvalidDataException("El respaldo cifrado está incompleto.");

            var llave = DerivarClave(sal, _clave);
            try
            {
                using var aes = new AesGcm(llave, TamanoEtiqueta);
                using var salida = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None);

                var cifrado = new byte[TamanoBloque];
                var plano = new byte[TamanoBloque];
                var nonce = new byte[TamanoNonce];
                var aad = new byte[9];
                var etiqueta = new byte[TamanoEtiqueta];
                var largoBytes = new byte[4];
                ulong contador = 0;

                while (true)
                {
                    if (LeerCompleto(entrada, largoBytes, largoBytes.Length) != largoBytes.Length)
                        throw new InvalidDataException("El respaldo cifrado está incompleto (le falta el final).");

                    var largo = BinaryPrimitives.ReadInt32LittleEndian(largoBytes);
                    if (largo < 0 || largo > TamanoBloque)
                        throw new InvalidDataException("El respaldo cifrado está dañado.");

                    if (LeerCompleto(entrada, etiqueta, etiqueta.Length) != etiqueta.Length
                        || LeerCompleto(entrada, cifrado, largo) != largo)
                        throw new InvalidDataException("El respaldo cifrado está incompleto.");

                    var ultimo = largo < TamanoBloque;
                    Preparar(nonce, aad, contador, ultimo);

                    try
                    {
                        aes.Decrypt(nonce, cifrado.AsSpan(0, largo), etiqueta, plano.AsSpan(0, largo), aad);
                    }
                    catch (CryptographicException)
                    {
                        throw new InvalidDataException("La clave de respaldos es incorrecta o el archivo está dañado.");
                    }

                    salida.Write(plano, 0, largo);
                    contador++;

                    if (ultimo)
                    {
                        if (entrada.ReadByte() != -1)
                            throw new InvalidDataException("El respaldo cifrado tiene datos de más: está dañado.");
                        break;
                    }
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(llave);
            }
        }

        /// <summary>Descifra a un archivo temporal (el que llama debe borrarlo cuando termine) y devuelve su ruta.</summary>
        public static string DescifrarATemporal(string ruta)
        {
            var temporal = Path.Combine(Path.GetTempPath(), "kirkenta-descifrado-" + Guid.NewGuid().ToString("N") + ".zip");
            try
            {
                DescifrarArchivo(ruta, temporal);
                return temporal;
            }
            catch
            {
                try { File.Delete(temporal); } catch { }
                throw;
            }
        }

        private static byte[] DerivarClave(byte[] sal, string clave)
            => Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, 32);

        private static void Preparar(byte[] nonce, byte[] aad, ulong contador, bool ultimo)
        {
            Array.Clear(nonce, 0, nonce.Length);
            BinaryPrimitives.WriteUInt64BigEndian(nonce.AsSpan(4, 8), contador);
            BinaryPrimitives.WriteUInt64BigEndian(aad.AsSpan(0, 8), contador);
            aad[8] = ultimo ? (byte)1 : (byte)0;
        }

        private static int LeerCompleto(Stream flujo, byte[] buffer, int cuantos)
        {
            var total = 0;
            while (total < cuantos)
            {
                var n = flujo.Read(buffer, total, cuantos - total);
                if (n == 0) break;
                total += n;
            }
            return total;
        }
    }
}
