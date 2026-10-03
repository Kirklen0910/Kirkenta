using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.OrdenesCompra
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DetailsModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public Models.OrdenCompra Orden { get; set; } = new();
        public Proveedor Proveedor { get; set; } = new();
        public List<DetalleOrdenCompra> Items { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public List<AdjuntoCompra> Adjuntos { get; set; } = new();
        public Moneda? Moneda { get; set; }
        public ConfiguracionEmpresa Empresa { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "Ordenes", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver esta orden";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            var orden = _context.OrdenesCompra.FirstOrDefault(o => o.Id == id);
            if (orden == null)
            {
                TempData["Error"] = "Orden no encontrada";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            CargarDatos(orden);
            return Page();
        }

        public IActionResult OnPostSubirAdjunto(int id, IFormFile archivo, string tipoDoc)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "OrdenesEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para subir adjuntos";
                return RedirectToPage("/OrdenesCompra/Details", new { id });
            }

            var orden = _context.OrdenesCompra.FirstOrDefault(o => o.Id == id);
            if (orden == null)
            {
                TempData["Error"] = "Orden no encontrada";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            if (archivo == null || archivo.Length == 0)
            {
                TempData["Error"] = "Debes seleccionar un archivo";
                return RedirectToPage("/OrdenesCompra/Details", new { id });
            }

            if (archivo.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "El archivo no puede pesar más de 10 MB";
                return RedirectToPage("/OrdenesCompra/Details", new { id });
            }

            var extensionesPermitidas = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!extensionesPermitidas.Contains(extension))
            {
                TempData["Error"] = "Solo se permiten archivos PDF, JPG o PNG";
                return RedirectToPage("/OrdenesCompra/Details", new { id });
            }

            // Sanitizar el número de orden (no debería tener caracteres raros, pero por seguridad)
            var numeroSeguro = string.Join("_", orden.Numero.Split(Path.GetInvalidFileNameChars()));

            var carpeta = Path.Combine(_env.WebRootPath, "uploads", "compras", numeroSeguro);
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                archivo.CopyTo(stream);
            }

            var adjunto = new AdjuntoCompra
            {
                OrdenCompraId = orden.Id,
                TipoDocumento = string.IsNullOrWhiteSpace(tipoDoc) ? "Otro" : tipoDoc,
                NombreArchivo = archivo.FileName,
                RutaArchivo = $"/uploads/compras/{numeroSeguro}/{nombreArchivo}",
                TipoArchivo = extension.TrimStart('.'),
                TamanoKB = (int)(archivo.Length / 1024),
                FechaSubida = DateTime.Now,
                UsuarioId = currentUser?.Id
            };

            _context.AdjuntosCompras.Add(adjunto);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Subir adjunto a orden",
                $"Subió '{archivo.FileName}' a la orden {orden.Numero}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Archivo subido correctamente";
            return RedirectToPage("/OrdenesCompra/Details", new { id });
        }

        public IActionResult OnPostEliminarAdjunto(int id, int adjuntoId)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "OrdenesEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para eliminar adjuntos";
                return RedirectToPage("/OrdenesCompra/Details", new { id });
            }

            // ⚠️ Validar que el adjunto pertenezca a esta orden (evita IDOR)
            var adjunto = _context.AdjuntosCompras
                .FirstOrDefault(a => a.Id == adjuntoId && a.OrdenCompraId == id);

            if (adjunto == null)
            {
                TempData["Error"] = "Adjunto no encontrado";
                return RedirectToPage("/OrdenesCompra/Details", new { id });
            }

            // Eliminar archivo físico (si existe)
            if (!string.IsNullOrEmpty(adjunto.RutaArchivo))
            {
                try
                {
                    var rutaRelativa = adjunto.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var rutaFisica = Path.Combine(_env.WebRootPath, rutaRelativa);
                    if (System.IO.File.Exists(rutaFisica))
                    {
                        System.IO.File.Delete(rutaFisica);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al eliminar archivo físico: {ex.Message}");
                }
            }

            _context.AdjuntosCompras.Remove(adjunto);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar adjunto",
                $"Eliminó el adjunto '{adjunto.NombreArchivo}' de la orden {adjunto.OrdenCompraId}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Adjunto eliminado correctamente";
            return RedirectToPage("/OrdenesCompra/Details", new { id });
        }

        private void CargarDatos(Models.OrdenCompra orden)
        {
            Orden = orden;
            Proveedor = _context.Proveedores.FirstOrDefault(p => p.Id == orden.ProveedorId) ?? new Proveedor();
            Items = _context.DetalleOrdenesCompra.Where(d => d.OrdenCompraId == orden.Id).ToList();
            Productos = _context.Productos.ToList();
            Adjuntos = _context.AdjuntosCompras
                .Where(a => a.OrdenCompraId == orden.Id)
                .OrderByDescending(a => a.FechaSubida)
                .ToList();
            Moneda = _context.Monedas.FirstOrDefault(m => m.Id == orden.MonedaId);
            Empresa = _context.ConfiguracionEmpresa.FirstOrDefault()
                ?? new ConfiguracionEmpresa { Nombre = "Mi Empresa" };
        }
    }
}