using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.RRHH
{
    public static class AdjuntoEmpleadoHelper
    {
        public static readonly string[] ExtensionesPermitidas = { ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".doc" };
        public const int TamanoMaximoMB = 15;

        public static (AdjuntoEmpleado? adjunto, string? error) Guardar(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            int empleadoId,
            int tipoDocumentoId,
            IFormFile archivo,
            string? descripcion,
            DateTime? fechaDocumento,
            DateTime? fechaVencimiento,
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
                    return (null, $"Solo se permiten: {string.Join(", ", ExtensionesPermitidas)}");

                var empleado = context.Empleados.FirstOrDefault(e => e.Id == empleadoId);
                if (empleado == null) return (null, "Empleado no encontrado");

                var carpeta = Path.Combine(env.WebRootPath, "uploads", "empleados", empleado.Codigo);
                if (!Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                var nombreArchivo = $"{Guid.NewGuid()}{extension}";
                var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    archivo.CopyTo(stream);
                }

                var vigente = true;
                if (fechaVencimiento.HasValue && fechaVencimiento.Value.Date < DateTime.Today)
                {
                    vigente = false;
                }

                var adjunto = new AdjuntoEmpleado
                {
                    EmpleadoId = empleadoId,
                    TipoDocumentoId = tipoDocumentoId,
                    NombreArchivo = archivo.FileName,
                    RutaArchivo = $"/uploads/empleados/{empleado.Codigo}/{nombreArchivo}",
                    TipoArchivo = extension.TrimStart('.'),
                    TamanoKB = (int)(archivo.Length / 1024),
                    Descripcion = descripcion,
                    FechaDocumento = fechaDocumento,
                    FechaVencimiento = fechaVencimiento,
                    Vigente = vigente,
                    FechaSubida = DateTime.Now,
                    UsuarioSubioId = usuarioId
                };

                context.AdjuntosEmpleado.Add(adjunto);
                context.SaveChanges();

                return (adjunto, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdjuntoEmpleadoHelper] Error: {ex.Message}");
                return (null, ex.Message);
            }
        }

        public static (bool ok, string? error) GuardarFotoPerfil(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            int empleadoId,
            IFormFile archivo)
        {
            try
            {
                if (archivo == null || archivo.Length == 0)
                    return (false, "Archivo vacío");

                if (archivo.Length > 5 * 1024 * 1024)
                    return (false, "La foto no puede pesar más de 5 MB");

                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                var extensionesFoto = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (!extensionesFoto.Contains(extension))
                    return (false, "La foto debe ser JPG, PNG o WEBP");

                var empleado = context.Empleados.FirstOrDefault(e => e.Id == empleadoId);
                if (empleado == null) return (false, "Empleado no encontrado");

                var carpeta = Path.Combine(env.WebRootPath, "uploads", "empleados", empleado.Codigo);
                if (!Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                var nombreArchivo = $"foto_{Guid.NewGuid()}{extension}";
                var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    archivo.CopyTo(stream);
                }

                // Eliminar foto anterior
                if (!string.IsNullOrEmpty(empleado.FotoPath))
                {
                    try
                    {
                        var rutaAnterior = Path.Combine(env.WebRootPath, empleado.FotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(rutaAnterior)) File.Delete(rutaAnterior);
                    }
                    catch { }
                }

                empleado.FotoPath = $"/uploads/empleados/{empleado.Codigo}/{nombreArchivo}";
                context.SaveChanges();

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static bool Eliminar(ApplicationDbContext context, IWebHostEnvironment env, int adjuntoId)
        {
            try
            {
                var adjunto = context.AdjuntosEmpleado.FirstOrDefault(a => a.Id == adjuntoId);
                if (adjunto == null) return false;

                if (!string.IsNullOrEmpty(adjunto.RutaArchivo))
                {
                    var rutaFisica = Path.Combine(env.WebRootPath, adjunto.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(rutaFisica))
                    {
                        try { File.Delete(rutaFisica); } catch { }
                    }
                }

                context.AdjuntosEmpleado.Remove(adjunto);
                context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdjuntoEmpleadoHelper] Error al eliminar: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verifica si un adjunto está vencido y actualiza su estado.
        /// </summary>
        public static void ActualizarVigencias(ApplicationDbContext context)
        {
            var hoy = DateTime.Today;
            var vencidos = context.AdjuntosEmpleado
                .Where(a => a.Vigente
                         && a.FechaVencimiento.HasValue
                         && a.FechaVencimiento.Value.Date < hoy)
                .ToList();

            foreach (var adj in vencidos)
            {
                adj.Vigente = false;
            }

            if (vencidos.Count > 0)
                context.SaveChanges();
        }
    }
}