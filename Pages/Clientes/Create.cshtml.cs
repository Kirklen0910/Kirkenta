using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Kirkenta.Pages.Clientes
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
            [Rtn]
            public string? RTN { get => _rtn; set => _rtn = DocumentoHonduras.FormatearRtn(value); }
            private string? _rtn;

            [EmailAddress(ErrorMessage = "Email inválido")]
            [StringLength(150)]
            public string? Email { get; set; }

            [StringLength(30)]
            [Telefono]
            public string? Telefono { get; set; }

            [StringLength(300)]
            public string? Direccion { get; set; }

            [StringLength(100)]
            public string? Ciudad { get; set; }

            [StringLength(80)]
            public string? Pais { get; set; } = "Honduras";

            public string TipoCliente { get; set; } = "Regular";
            [Range(0, double.MaxValue, ErrorMessage = "El límite de crédito no puede ser negativo")]
            public decimal LimiteCredito { get; set; } = 0;
            [Range(0, 365, ErrorMessage = "Días de crédito entre 0 y 365")]
            public int DiasCredito { get; set; } = 0;
            public string? Notas { get; set; }
            public bool Activo { get; set; } = true;

            public bool EsExonerado { get; set; }

            [StringLength(50)]
            public string? ExoneradoConstancia { get; set; }

            [StringLength(50)]
            public string? ExoneradoOrdenCompraExenta { get; set; }

            [StringLength(50)]
            public string? ExoneradoRegistroSag { get; set; }
        }

        // Un cliente exonerado debe tener su constancia y su registro SAG (son los que se imprimen en la factura)
        private void ValidarExoneracion()
        {
            Input.ExoneradoConstancia = Input.ExoneradoConstancia?.Trim();
            Input.ExoneradoOrdenCompraExenta = Input.ExoneradoOrdenCompraExenta?.Trim();
            Input.ExoneradoRegistroSag = Input.ExoneradoRegistroSag?.Trim();

            if (!Input.EsExonerado)
            {
                Input.ExoneradoConstancia = null;
                Input.ExoneradoOrdenCompraExenta = null;
                Input.ExoneradoRegistroSag = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(Input.ExoneradoConstancia))
                ModelState.AddModelError("Input.ExoneradoConstancia", "Indica el número de constancia de exoneración");
            if (string.IsNullOrWhiteSpace(Input.ExoneradoRegistroSag))
                ModelState.AddModelError("Input.ExoneradoRegistroSag", "Indica el número de registro SAG");
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Clientes", "crear") && currentRol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para crear clientes";
                return RedirectToPage("/Clientes/Index");
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Clientes", "crear") && currentRol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para crear clientes";
                return RedirectToPage("/Clientes/Index");
            }

            ValidarExoneracion();
            if (!ModelState.IsValid)
                return Page();

            if (!string.IsNullOrWhiteSpace(Input.RTN) && _context.Clientes.Any(c => c.RTN == Input.RTN || c.RTN == DocumentoHonduras.SoloDigitos(Input.RTN)))
            {
                ModelState.AddModelError("Input.RTN", "Ya existe un cliente con este RTN");
                return Page();
            }

            // Auto-generar código con la serie "Cliente" (Configuración → Series); si no hay serie, CLI-0001
            if (string.IsNullOrWhiteSpace(Input.Codigo))
            {
                Input.Codigo = NumeroDocumentoHelper.GenerarSiguiente(_context, "Cliente");
                var n = _context.Clientes.Count() + 1;
                while (_context.Clientes.Any(c => c.Codigo == Input.Codigo))
                {
                    Input.Codigo = $"CLI-{n:D4}";
                    n++;
                }
            }

            // Verificar duplicado
            if (_context.Clientes.Any(c => c.Codigo == Input.Codigo))
            {
                ModelState.AddModelError("Input.Codigo", "Este código ya existe");
                return Page();
            }

            var cliente = new Cliente
            {
                Codigo = Input.Codigo,
                Nombre = Input.Nombre,
                RazonSocial = Input.RazonSocial,
                RTN = Input.RTN,
                Email = Input.Email,
                Telefono = Input.Telefono,
                Direccion = Input.Direccion,
                Ciudad = Input.Ciudad,
                Pais = Input.Pais,
                TipoCliente = Input.TipoCliente,
                LimiteCredito = Input.LimiteCredito,
                DiasCredito = Input.DiasCredito,
                Notas = Input.Notas,
                Activo = Input.Activo,
                EsExonerado = Input.EsExonerado,
                ExoneradoConstancia = Input.ExoneradoConstancia,
                ExoneradoOrdenCompraExenta = Input.ExoneradoOrdenCompraExenta,
                ExoneradoRegistroSag = Input.ExoneradoRegistroSag,
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = currentUser?.Id
            };

            _context.Clientes.Add(cliente);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear cliente",
                $"Creó al cliente '{cliente.Nombre}' ({cliente.Codigo})",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cliente '{cliente.Nombre}' creado correctamente";
            return RedirectToPage("/Clientes/Index");
        }
    }
}