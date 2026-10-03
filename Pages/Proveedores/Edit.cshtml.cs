using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Proveedores
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<Moneda> Monedas { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

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
            public string? Pais { get; set; }

            [Required]
            public string CondicionPago { get; set; } = "Contado";

            [Range(0, 365)]
            public int DiasCredito { get; set; } = 0;

            [Range(0, double.MaxValue)]
            public decimal LimiteCredito { get; set; } = 0;

            public int MonedaId { get; set; } = 1;

            [StringLength(100)]
            public string? Banco { get; set; }

            [StringLength(50)]
            public string? CuentaBancaria { get; set; }

            [StringLength(500)]
            public string? Notas { get; set; }

            public bool Activo { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "ProveedoresEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar proveedores";
                return RedirectToPage("/Proveedores/Index");
            }

            Monedas = _context.Monedas.Where(m => m.Activa).OrderBy(m => m.Codigo).ToList();

            var p = _context.Proveedores.FirstOrDefault(x => x.Id == id);
            if (p == null)
            {
                TempData["Error"] = "Proveedor no encontrado";
                return RedirectToPage("/Proveedores/Index");
            }

            Input = new InputModel
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                RazonSocial = p.RazonSocial,
                RTN = p.RTN,
                Contacto = p.Contacto,
                TelefonoContacto = p.TelefonoContacto,
                Telefono = p.Telefono,
                Email = p.Email,
                Direccion = p.Direccion,
                Ciudad = p.Ciudad,
                Pais = p.Pais,
                CondicionPago = p.CondicionPago,
                DiasCredito = p.DiasCredito,
                LimiteCredito = p.LimiteCredito,
                MonedaId = p.MonedaId,
                Banco = p.Banco,
                CuentaBancaria = p.CuentaBancaria,
                Notas = p.Notas,
                Activo = p.Activo
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Compras", "ProveedoresEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar proveedores";
                return RedirectToPage("/Proveedores/Index");
            }

            Monedas = _context.Monedas.Where(m => m.Activa).OrderBy(m => m.Codigo).ToList();

            if (!ModelState.IsValid)
                return Page();

            var p = _context.Proveedores.FirstOrDefault(x => x.Id == Input.Id);
            if (p == null)
            {
                TempData["Error"] = "Proveedor no encontrado";
                return RedirectToPage("/Proveedores/Index");
            }

            // Validar código duplicado
            if (!string.IsNullOrWhiteSpace(Input.Codigo) &&
                _context.Proveedores.Any(x => x.Codigo == Input.Codigo && x.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Codigo", $"El código '{Input.Codigo}' ya existe");
                return Page();
            }

            // Coherencia crédito
            if (Input.CondicionPago == "Contado")
            {
                Input.DiasCredito = 0;
                Input.LimiteCredito = 0;
            }

            p.Codigo = Input.Codigo;
            p.Nombre = Input.Nombre;
            p.RazonSocial = Input.RazonSocial;
            p.RTN = Input.RTN;
            p.Contacto = Input.Contacto;
            p.TelefonoContacto = Input.TelefonoContacto;
            p.Telefono = Input.Telefono;
            p.Email = Input.Email;
            p.Direccion = Input.Direccion;
            p.Ciudad = Input.Ciudad;
            p.Pais = Input.Pais;
            p.CondicionPago = Input.CondicionPago;
            p.DiasCredito = Input.DiasCredito;
            p.LimiteCredito = Input.LimiteCredito;
            p.MonedaId = Input.MonedaId;
            p.Banco = Input.Banco;
            p.CuentaBancaria = Input.CuentaBancaria;
            p.Notas = Input.Notas;
            p.Activo = Input.Activo;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar proveedor",
                $"Editó al proveedor '{p.Nombre}' ({p.Codigo})",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Proveedor '{p.Nombre}' actualizado";
            return RedirectToPage("/Proveedores/Index");
        }
    }
}