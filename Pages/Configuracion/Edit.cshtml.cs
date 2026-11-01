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
            [Rtn]
            public string? RTN { get => _rtn; set => _rtn = DocumentoHonduras.FormatearRtn(value); }
            private string? _rtn;

            [StringLength(300)]
            public string? Direccion { get; set; }

            [StringLength(100)]
            public string? Ciudad { get; set; }

            [StringLength(80)]
            public string? Pais { get; set; }

            [StringLength(30)]
            [Telefono]
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

            public bool FacturacionFiscalActiva { get; set; }

            [StringLength(50)]
            public string? NotaCreditoCAI { get; set; }

            [StringLength(30)]
            public string? NotaCreditoRangoInicial { get; set; }

            [StringLength(30)]
            public string? NotaCreditoRangoFinal { get; set; }

            public DateTime? NotaCreditoFechaLimite { get; set; }

            public string? NotasFactura { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (!PermisoHelper.TienePermiso(_context, currentRol, "Configuracion", "Empresa", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar o aprobar aquí";
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
                FacturacionFiscalActiva = empresa.FacturacionFiscalActiva,
                NotaCreditoCAI = empresa.NotaCreditoCAI,
                NotaCreditoRangoInicial = empresa.NotaCreditoRangoInicial,
                NotaCreditoRangoFinal = empresa.NotaCreditoRangoFinal,
                NotaCreditoFechaLimite = empresa.NotaCreditoFechaLimite,
                NotasFactura = empresa.NotasFactura
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (!PermisoHelper.TienePermiso(_context, currentRol, "Configuracion", "Empresa", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar o aprobar aquí";
                return RedirectToPage("/Index");
            }

            if (Input.FacturacionFiscalActiva)
            {
                var errorFiscal = FacturacionFiscalHelper.ValidarConfiguracion(
                    Input.CAI, Input.RangoInicial, Input.RangoFinal, Input.FechaLimiteEmision);
                if (errorFiscal != null)
                    ModelState.AddModelError("Input.CAI", errorFiscal);
            }

            if (Input.FacturacionFiscalActiva
                && NotaCreditoFiscalHelper.HayDatos(Input.NotaCreditoCAI, Input.NotaCreditoRangoInicial,
                                                    Input.NotaCreditoRangoFinal, Input.NotaCreditoFechaLimite))
            {
                var errorNc = FacturacionFiscalHelper.ValidarConfiguracion(
                    Input.NotaCreditoCAI, Input.NotaCreditoRangoInicial, Input.NotaCreditoRangoFinal, Input.NotaCreditoFechaLimite);
                if (errorNc != null)
                    ModelState.AddModelError("Input.NotaCreditoCAI", "Notas de crédito: " + errorNc);
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
                    var rutaRelativa = empresa.LogoPath.Split('?')[0].TrimStart('/');
                    var rutaAnterior = Path.Combine(_env.WebRootPath, rutaRelativa);
                    if (System.IO.File.Exists(rutaAnterior))
                    {
                        try { System.IO.File.Delete(rutaAnterior); } catch (IOException) { /* archivo en uso: se sobrescribe/queda huérfano */ } catch (UnauthorizedAccessException) { }
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
                // Mantener el logo existente (no confiar en la ruta enviada por el formulario)
            }

            // Auditoría: valores de antes vs después de los datos que importan (fiscales y de identidad)
            var cambiosConfig = CambiosHelper.Describir(
                ("Nombre", empresa.Nombre, Input.Nombre),
                ("Razón social", empresa.RazonSocial, Input.RazonSocial),
                ("RTN", empresa.RTN, Input.RTN),
                ("Facturación fiscal", empresa.FacturacionFiscalActiva, Input.FacturacionFiscalActiva),
                ("CAI", empresa.CAI, Input.CAI),
                ("Rango inicial", empresa.RangoInicial, Input.RangoInicial),
                ("Rango final", empresa.RangoFinal, Input.RangoFinal),
                ("Fecha límite", empresa.FechaLimiteEmision, Input.FechaLimiteEmision),
                ("CAI NC", empresa.NotaCreditoCAI, Input.NotaCreditoCAI),
                ("Rango inicial NC", empresa.NotaCreditoRangoInicial, Input.NotaCreditoRangoInicial),
                ("Rango final NC", empresa.NotaCreditoRangoFinal, Input.NotaCreditoRangoFinal),
                ("Fecha límite NC", empresa.NotaCreditoFechaLimite, Input.NotaCreditoFechaLimite),
                ("Dirección", empresa.Direccion, Input.Direccion),
                ("Teléfono", empresa.Telefono, Input.Telefono),
                ("Email", empresa.Email, Input.Email));

            empresa.Nombre = Input.Nombre;
            empresa.RazonSocial = Input.RazonSocial;
            empresa.RTN = Input.RTN;
            empresa.Direccion = Input.Direccion;
            empresa.Ciudad = Input.Ciudad;
            empresa.Pais = Input.Pais;
            empresa.Telefono = Input.Telefono;
            empresa.Email = Input.Email;
            empresa.SitioWeb = Input.SitioWeb;
            // Si cambió el CAI o el rango, el contador fiscal se reinicia al nuevo rango inicial
            bool cambioRango = !string.Equals(empresa.CAI, Input.CAI, StringComparison.Ordinal)
                            || !string.Equals(empresa.RangoInicial, Input.RangoInicial, StringComparison.Ordinal)
                            || !string.Equals(empresa.RangoFinal, Input.RangoFinal, StringComparison.Ordinal);

            empresa.CAI = Input.CAI;
            empresa.RangoInicial = Input.RangoInicial;
            empresa.RangoFinal = Input.RangoFinal;
            empresa.FechaLimiteEmision = Input.FechaLimiteEmision;
            empresa.FacturacionFiscalActiva = Input.FacturacionFiscalActiva;
            if (cambioRango) empresa.SiguienteNumeroFiscal = 0;

            bool cambioRangoNc = !string.Equals(empresa.NotaCreditoCAI, Input.NotaCreditoCAI, StringComparison.Ordinal)
                              || !string.Equals(empresa.NotaCreditoRangoInicial, Input.NotaCreditoRangoInicial, StringComparison.Ordinal)
                              || !string.Equals(empresa.NotaCreditoRangoFinal, Input.NotaCreditoRangoFinal, StringComparison.Ordinal);
            empresa.NotaCreditoCAI = Input.NotaCreditoCAI;
            empresa.NotaCreditoRangoInicial = Input.NotaCreditoRangoInicial;
            empresa.NotaCreditoRangoFinal = Input.NotaCreditoRangoFinal;
            empresa.NotaCreditoFechaLimite = Input.NotaCreditoFechaLimite;
            if (cambioRangoNc) empresa.SiguienteNumeroNotaCredito = 0;
            empresa.NotasFactura = Input.NotasFactura;
            empresa.FechaActualizacion = DateTime.Now;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Actualizar configuración de empresa",
                "Actualizó los datos de la empresa. " + cambiosConfig,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Configuración actualizada correctamente";
            return RedirectToPage("/Configuracion/Index");
        }
    }
}