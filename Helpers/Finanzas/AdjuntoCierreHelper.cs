using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    public static class AdjuntoCierreHelper
    {
        public static readonly string[] ExtensionesPermitidas = { ".pdf", ".jpg", ".jpeg", ".png" };
        public const int TamanoMaximoMB = 10;

        public static (AdjuntoCierre? adjunto, string? error) Guardar(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            int cierreId,
            IFormFile archivo,
            string? descripcion,
            int? usuarioId)
        {
            try
            {
                if (archivo == null || archivo.Length == 0)
                    return (null, "Archivo vacío");

                if (archivo.Length > TamanoMaximoMB * 1024 * 1024)
                    return (null, $"El archivo no puede pesar más de {TamanoMaximoMB} MB");

                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                if (!ExtensionesPermitidas.Contains(extension))
                    return (null, $"Solo se permiten archivos: {string.Join(", ", ExtensionesPermitidas)}");

                // Crear carpeta del cierre
                var cierre = context.CierresCaja.FirstOrDefault(c => c.Id == cierreId);
                if (cierre == null)
                    return (null, "Cierre no encontrado");

                var carpeta = Path.Combine(env.WebRootPath, "uploads", "cierres", cierre.Numero);
                if (!Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                var nombreArchivo = $"{Guid.NewGuid()}{extension}";
                var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    archivo.CopyTo(stream);
                }

                var adjunto = new AdjuntoCierre
                {
                    CierreCajaId = cierreId,
                    NombreArchivo = archivo.FileName,
                    RutaArchivo = $"/uploads/cierres/{cierre.Numero}/{nombreArchivo}",
                    TipoArchivo = extension.TrimStart('.'),
                    TamanoKB = (int)(archivo.Length / 1024),
                    Descripcion = descripcion,
                    FechaSubida = DateTime.Now,
                    UsuarioId = usuarioId
                };

                context.AdjuntosCierre.Add(adjunto);
                context.SaveChanges();

                return (adjunto, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdjuntoCierreHelper] Error: {ex.Message}");
                return (null, ex.Message);
            }
        }

        public static bool Eliminar(ApplicationDbContext context, IWebHostEnvironment env, int adjuntoId)
        {
            try
            {
                var adjunto = context.AdjuntosCierre.FirstOrDefault(a => a.Id == adjuntoId);
                if (adjunto == null) return false;

                if (!string.IsNullOrEmpty(adjunto.RutaArchivo))
                {
                    var rutaRelativa = adjunto.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var rutaFisica = Path.Combine(env.WebRootPath, rutaRelativa);

                    if (File.Exists(rutaFisica))
                    {
                        try { File.Delete(rutaFisica); } catch { }
                    }
                }

                context.AdjuntosCierre.Remove(adjunto);
                context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdjuntoCierreHelper] Error al eliminar: {ex.Message}");
                return false;
            }
        }
    }
}