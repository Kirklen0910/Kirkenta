using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Finanzas.Cierres
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CreateModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<CuentaFinanciera> Cuentas { get; set; } = new();
        public List<CuentaFinanciera> CuentasBancarias { get; set; } = new();
        public List<Usuario> Usuarios { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Debes seleccionar una cuenta")]
            public int CuentaId { get; set; }

            [Required]
            public DateTime Fecha { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "Debes ingresar el efectivo contado")]
            [Range(0, double.MaxValue, ErrorMessage = "El monto no puede ser negativo")]
            public decimal EfectivoContado { get; set; }

            [StringLength(500)]
            public string? Notas { get; set; }

            public decimal Tolerancia { get; set; } = 20.00m;

            public decimal? MontoRetiroBanco { get; set; }
            public int? CuentaBancoDestinoId { get; set; }
            public string? ReferenciaRetiro { get; set; }

            public decimal? MontoFondoCaja { get; set; }

            public decimal? MontoEntregaAdmin { get; set; }
            public int? UsuarioRecibeEntregaId { get; set; }
            public string? NombreRecibeEntrega { get; set; }
            public string? ReferenciaEntrega { get; set; }

            public List<IFormFile>? Adjuntos { get; set; }
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Cierres/Index");
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "CierresCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Cierres/Index");
            }

            CargarDatos();

            if (!ModelState.IsValid) return Page();

            // Validar distribuciones
            var totalDistribuido = (Input.MontoRetiroBanco ?? 0) + (Input.MontoFondoCaja ?? 0) + (Input.MontoEntregaAdmin ?? 0);

            if (Math.Abs(totalDistribuido - Input.EfectivoContado) > 0.01m)
            {
                ModelState.AddModelError(string.Empty,
                    $"La suma de las distribuciones (L. {totalDistribuido:N2}) no coincide con el efectivo contado (L. {Input.EfectivoContado:N2})");
                return Page();
            }

            // ===== VALIDACIÓN: período contable cerrado =====
            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(_context, Input.Fecha);
            if (!periodoOk)
            {
                ModelState.AddModelError(string.Empty, periodoError!);
                return Page();
            }

            // Registrar cierre
            var (cierre, error) = CierreHelper.Registrar(
                _context,
                Input.CuentaId,
                Input.Fecha,
                Input.EfectivoContado,
                Input.Notas,
                Input.Tolerancia,
                currentUser!.Id
            );

            if (error != null)
            {
                ModelState.AddModelError(string.Empty, error);
                return Page();
            }

            // Registrar distribuciones
            var distribuciones = new List<DistribucionInput>();

            if (Input.MontoRetiroBanco.HasValue && Input.MontoRetiroBanco > 0 && Input.CuentaBancoDestinoId.HasValue)
            {
                distribuciones.Add(new DistribucionInput
                {
                    Tipo = "RetiroBanco",
                    Monto = Input.MontoRetiroBanco.Value,
                    CuentaDestinoId = Input.CuentaBancoDestinoId,
                    Referencia = Input.ReferenciaRetiro
                });
            }

            if (Input.MontoFondoCaja.HasValue && Input.MontoFondoCaja > 0)
            {
                distribuciones.Add(new DistribucionInput
                {
                    Tipo = "FondoCaja",
                    Monto = Input.MontoFondoCaja.Value,
                    DestinoDescripcion = "Fondo para el próximo día"
                });
            }

            if (Input.MontoEntregaAdmin.HasValue && Input.MontoEntregaAdmin > 0)
            {
                distribuciones.Add(new DistribucionInput
                {
                    Tipo = "EntregaAdmin",
                    Monto = Input.MontoEntregaAdmin.Value,
                    UsuarioRecibeId = Input.UsuarioRecibeEntregaId,
                    NombreRecibe = Input.NombreRecibeEntrega,
                    Referencia = Input.ReferenciaEntrega
                });
            }

            var (ok, distError) = DistribucionHelper.RegistrarDistribuciones(
                _context, cierre.Id, Input.CuentaId, Input.EfectivoContado,
                distribuciones, currentUser.Id);

            if (!ok)
            {
                TempData["Warning"] = $"Cierre registrado pero hubo un problema con las distribuciones: {distError}";
            }

            // Subir adjuntos
            if (Input.Adjuntos != null && Input.Adjuntos.Count > 0)
            {
                foreach (var archivo in Input.Adjuntos)
                {
                    AdjuntoCierreHelper.Guardar(_context, _env, cierre.Id, archivo, null, currentUser.Id);
                }
            }

            ActividadHelper.Registrar(
                _context,
                currentUser.Id,
                "Cierre de caja",
                $"Cerró caja {cierre.Numero}. Esperado: L. {cierre.EfectivoEsperado:N2}, Contado: L. {cierre.EfectivoContado:N2}, Diferencia: L. {cierre.Diferencia:N2}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cierre {cierre.Numero} registrado. Resultado: {cierre.Resultado}";
            return RedirectToPage("/Finanzas/Cierres/Details", new { id = cierre.Id });
        }

        private void CargarDatos()
        {
            Cuentas = _context.CuentasFinancieras
                .Where(c => c.Tipo == "Caja" && c.Activa)
                .OrderBy(c => c.Nombre)
                .ToList();

            CuentasBancarias = _context.CuentasFinancieras
                .Where(c => c.Tipo == "Banco" && c.Activa)
                .OrderBy(c => c.Nombre)
                .ToList();

            Usuarios = _context.Usuarios
                .Where(u => u.Activo)
                .OrderBy(u => u.Username)
                .ToList();
        }
    }
}