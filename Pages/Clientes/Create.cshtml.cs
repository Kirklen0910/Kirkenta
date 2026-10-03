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
            public string? RTN { get; set; }

            [EmailAddress(ErrorMessage = "Email inválido")]
            [StringLength(150)]
            public string? Email { get; set; }

            [StringLength(30)]
            public string? Telefono { get; set; }

            [StringLength(300)]
            public string? Direccion { get; set; }

            [StringLength(100)]
            public string? Ciudad { get; set; }

            [StringLength(80)]
            public string? Pais { get; set; } = "Honduras";

            public string TipoCliente { get; set; } = "Regular";
            public decimal LimiteCredito { get; set; } = 0;
            public int DiasCredito { get; set; } = 0;
            public string? Notas { get; set; }
            public bool Activo { get; set; } = true;
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

            if (!ModelState.IsValid)
                return Page();

            // Auto-generar código si no viene
            if (string.IsNullOrWhiteSpace(Input.Codigo))
            {
                var count = _context.Clientes.Count();
                Input.Codigo = $"CLI-{(count + 1):D4}";
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