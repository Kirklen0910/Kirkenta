using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Configuracion
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EditModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public IFormFile? LogoFile { get; set; }

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(150)]
            public string Nombre { get; set; } = string.Empty;

            [StringLength(200)]
            public string? RazonSocial { get; set; }

            [StringLength(20)]
            public string? RTN { get; set; }

            [StringLength(300)]
            public string? Direccion { get; set; }

            [StringLength(100)]
            public string? Ciudad { get; set; }

            [StringLength(80)]
            public string? Pais { get; set; }

            [StringLength(30)]
            public string? Telefono { get; set; }

            [EmailAddress]
            public string? Email { get; set; }

            [StringLength(150)]
            public string? SitioWeb { get; set; }

            public string? LogoPath { get; set; }

            [StringLength(50)]
            public string? CAI { get; set; }

            [StringLength(30)]
            public string? RangoInicial { get; set; }

            [StringLength(30)]
            public string? RangoFinal { get; set; }

            public DateTime? FechaLimiteEmision { get; set; }

            public string? NotasFactura { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Index");
            }

            var empresa = _context.ConfiguracionEmpresa.FirstOrDefault();
            if (empresa == null)
            {
                empresa = new ConfiguracionEmpresa { Nombre = "Mi Empresa" };
                _context.ConfiguracionEmpresa.Add(empresa);
                _context.SaveChanges();
            }

            Input = new InputModel
            {
                Id = empresa.Id,
                Nombre = empresa.Nombre,
                RazonSocial = empresa.RazonSocial,
                RTN = empresa.RTN,
                Direccion = empresa.Direccion,
                Ciudad = empresa.Ciudad,
                Pais = empresa.Pais,
                Telefono = empresa.Telefono,
                Email = empresa.Email,
                SitioWeb = empresa.SitioWeb,
                LogoPath = empresa.LogoPath,
                CAI = empresa.CAI,
                RangoInicial = empresa.RangoInicial,
                RangoFinal = empresa.RangoFinal,
                FechaLimiteEmision = empresa.FechaLimiteEmision,
                NotasFactura = empresa.NotasFactura
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores";
                return RedirectToPage("/Index");
            }

            if (!ModelState.IsValid) return Page();

            var empresa = _context.ConfiguracionEmpresa.FirstOrDefault(e => e.Id == Input.Id);
            if (empresa == null)
            {
                TempData["Error"] = "Configuración no encontrada";
                return RedirectToPage("/Configuracion/Index");
            }

            // Procesar logo si se subió
            if (LogoFile != null && LogoFile.Length > 0)
            {
                // Validar tamaño (2 MB)
                if (LogoFile.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("LogoFile", "El archivo no puede pesar más de 2 MB");
                    return Page();
                }

                // Validar extensión
                var extensionesPermitidas = new[] { ".png", ".jpg", ".jpeg", ".webp" };
                var extension = Path.GetExtension(LogoFile.FileName).ToLowerInvariant();
                if (!extensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError("LogoFile", "Solo se permiten PNG, JPG o WEBP");
                    return Page();
                }

                // Guardar archivo
                var carpetaUploads = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(carpetaUploads))
                    Directory.CreateDirectory(carpetaUploads);

                var nombreArchivo = $"empresa-logo{extension}";
                var rutaCompleta = Path.Combine(carpetaUploads, nombreArchivo);

                // Eliminar archivo anterior si existe
                if (!string.IsNullOrEmpty(empresa.LogoPath))
                {
                    var rutaAnterior = Path.Combine(_env.WebRootPath, empresa.LogoPath.TrimStart('/').Replace("uploads/", "uploads/"));
                    if (System.IO.File.Exists(rutaAnterior))
                    {
                        try { System.IO.File.Delete(rutaAnterior); } catch { }
                    }
                }

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    LogoFile.CopyTo(stream);
                }

                // Guardar la ruta
                empresa.LogoPath = $"/uploads/{nombreArchivo}?v={DateTime.Now.Ticks}";
            }
            else
            {
                // Mantener el logo existente
                empresa.LogoPath = Input.LogoPath;
            }

            empresa.Nombre = Input.Nombre;
            empresa.RazonSocial = Input.RazonSocial;
            empresa.RTN = Input.RTN;
            empresa.Direccion = Input.Direccion;
            empresa.Ciudad = Input.Ciudad;
            empresa.Pais = Input.Pais;
            empresa.Telefono = Input.Telefono;
            empresa.Email = Input.Email;
            empresa.SitioWeb = Input.SitioWeb;
            empresa.CAI = Input.CAI;
            empresa.RangoInicial = Input.RangoInicial;
            empresa.RangoFinal = Input.RangoFinal;
            empresa.FechaLimiteEmision = Input.FechaLimiteEmision;
            empresa.NotasFactura = Input.NotasFactura;
            empresa.FechaActualizacion = DateTime.Now;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Actualizar configuración de empresa",
                "Actualizó los datos de la empresa",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Configuración actualizada correctamente";
            return RedirectToPage("/Configuracion/Index");
        }
    }
}