using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Proveedores
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<Moneda> Monedas { get; set; } = new();
        public string NumeroPreview { get; set; } = "";

        public class InputModel
        {
            [StringLength(20)]
            public string? Codigo { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(150)]
            public string Nombre { get; set; } = string.Empty;

            [StringLength(200)]
            public string? RazonSocial { get; set; }

            [StringLength(20)]
            public string? RTN { get; set; }

            [StringLength(100)]
            public string? Contacto { get; set; }

            [StringLength(30)]
            public string? TelefonoContacto { get; set; }

            [StringLength(30)]
            public string? Telefono { get; set; }

            [EmailAddress(ErrorMessage = "Email inválido")]
            public string? Email { get; set; }

            [StringLength(300)]
            public string? Direccion { get; set; }

            [StringLength(100)]
            public string? Ciudad { get; set; }

            [StringLength(80)]
            public string? Pais { get; set; } = "Honduras";

            [Required]
            public string CondicionPago { get; set; } = "Contado";

            [Range(0, 365, ErrorMessage = "Días de crédito debe estar entre 0 y 365")]
            public int DiasCredito { get; set; } = 0;

            [Range(0, double.MaxValue)]
            public decimal LimiteCredito { get; set; } = 0;

            [Required]
            public int MonedaId { get; set; } = 1;

            [StringLength(100)]
            public string? Banco { get; set; }

            [StringLength(50)]
            public string? CuentaBancaria { get; set; }

            [StringLength(500)]
            public string? Notas { get; set; }

            public bool Activo { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "ProveedoresCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear proveedores";
                return RedirectToPage("/Proveedores/Index");
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "ProveedoresCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear proveedores";
                return RedirectToPage("/Proveedores/Index");
            }

            CargarDatos();

            // Validar coherencia
            if (Input.CondicionPago == "Contado")
            {
                Input.DiasCredito = 0;
                Input.LimiteCredito = 0;
            }

            if (!ModelState.IsValid)
                return Page();

            // Generar código con nomenclatura (respetando la serie configurada)
            if (string.IsNullOrWhiteSpace(Input.Codigo))
            {
                Input.Codigo = NumeroDocumentoHelper.GenerarSiguiente(_context, "Proveedor");
            }

            // Validar duplicado
            if (_context.Proveedores.Any(p => p.Codigo == Input.Codigo))
            {
                ModelState.AddModelError("Input.Codigo",
                    $"El código '{Input.Codigo}' ya existe. Deja el campo vacío para autogenerarlo.");
                return Page();
            }

            var proveedor = new Proveedor
            {
                Codigo = Input.Codigo,
                Nombre = Input.Nombre,
                RazonSocial = Input.RazonSocial,
                RTN = Input.RTN,
                Contacto = Input.Contacto,
                TelefonoContacto = Input.TelefonoContacto,
                Telefono = Input.Telefono,
                Email = Input.Email,
                Direccion = Input.Direccion,
                Ciudad = Input.Ciudad,
                Pais = Input.Pais,
                CondicionPago = Input.CondicionPago,
                DiasCredito = Input.DiasCredito,
                LimiteCredito = Input.LimiteCredito,
                MonedaId = Input.MonedaId,
                Banco = Input.Banco,
                CuentaBancaria = Input.CuentaBancaria,
                Notas = Input.Notas,
                Activo = Input.Activo,
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = currentUser?.Id,
                EmpresaId = 1
            };

            _context.Proveedores.Add(proveedor);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear proveedor",
                $"Creó al proveedor '{proveedor.Nombre}' ({proveedor.Codigo})",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Proveedor '{proveedor.Nombre}' creado con código {proveedor.Codigo}";
            return RedirectToPage("/Proveedores/Index");
        }

        private void CargarDatos()
        {
            Monedas = _context.Monedas
                .Where(m => m.Activa)
                .OrderBy(m => m.Codigo)
                .ToList();

            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Proveedor");
        }
    }
}