using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Clientes
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
            [Rtn]
            public string? RTN { get => _rtn; set => _rtn = DocumentoHonduras.FormatearRtn(value); }
            private string? _rtn;

            [EmailAddress(ErrorMessage = "Email inválido")]
            public string? Email { get; set; }

            [StringLength(30)]
            [Telefono]
            public string? Telefono { get; set; }

            [StringLength(300)]
            public string? Direccion { get; set; }

            [StringLength(100)]
            public string? Ciudad { get; set; }

            [StringLength(80)]
            public string? Pais { get; set; }

            public string TipoCliente { get; set; } = "Regular";
            [Range(0, double.MaxValue, ErrorMessage = "El límite de crédito no puede ser negativo")]
            public decimal LimiteCredito { get; set; }
            [Range(0, 365, ErrorMessage = "Días de crédito entre 0 y 365")]
            public int DiasCredito { get; set; }
            public string? Notas { get; set; }
            public bool Activo { get; set; }

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

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Clientes", "editar") && currentRol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para editar clientes";
                return RedirectToPage("/Clientes/Index");
            }

            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);
            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado";
                return RedirectToPage("/Clientes/Index");
            }

            Input = new InputModel
            {
                Id = cliente.Id,
                Codigo = cliente.Codigo,
                Nombre = cliente.Nombre,
                RazonSocial = cliente.RazonSocial,
                RTN = cliente.RTN,
                Email = cliente.Email,
                Telefono = cliente.Telefono,
                Direccion = cliente.Direccion,
                Ciudad = cliente.Ciudad,
                Pais = cliente.Pais,
                TipoCliente = cliente.TipoCliente,
                LimiteCredito = cliente.LimiteCredito,
                DiasCredito = cliente.DiasCredito,
                Notas = cliente.Notas,
                Activo = cliente.Activo,
                EsExonerado = cliente.EsExonerado,
                ExoneradoConstancia = cliente.ExoneradoConstancia,
                ExoneradoOrdenCompraExenta = cliente.ExoneradoOrdenCompraExenta,
                ExoneradoRegistroSag = cliente.ExoneradoRegistroSag
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (!PermisoHelper.TienePermiso(_context, currentRol, "Ventas", "Clientes", "editar") && currentRol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para editar clientes";
                return RedirectToPage("/Clientes/Index");
            }

            ValidarExoneracion();
            if (!ModelState.IsValid)
                return Page();

            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == Input.Id);
            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado";
                return RedirectToPage("/Clientes/Index");
            }

            if (!string.IsNullOrWhiteSpace(Input.Codigo) && Input.Codigo != cliente.Codigo)
            {
                if (_context.Clientes.Any(c => c.Codigo == Input.Codigo && c.Id != cliente.Id))
                {
                    ModelState.AddModelError("Input.Codigo", "Este código ya lo usa otro cliente");
                    return Page();
                }
                cliente.Codigo = Input.Codigo;
            }

            if (!string.IsNullOrWhiteSpace(Input.RTN) && Input.RTN != cliente.RTN &&
                _context.Clientes.Any(c => (c.RTN == Input.RTN || c.RTN == DocumentoHonduras.SoloDigitos(Input.RTN)) && c.Id != cliente.Id))
            {
                ModelState.AddModelError("Input.RTN", "Ya existe otro cliente con este RTN");
                return Page();
            }

            cliente.Nombre = Input.Nombre;
            cliente.RazonSocial = Input.RazonSocial;
            cliente.RTN = Input.RTN;
            cliente.Email = Input.Email;
            cliente.Telefono = Input.Telefono;
            cliente.Direccion = Input.Direccion;
            cliente.Ciudad = Input.Ciudad;
            cliente.Pais = Input.Pais;
            cliente.TipoCliente = Input.TipoCliente;
            cliente.LimiteCredito = Input.LimiteCredito;
            cliente.DiasCredito = Input.DiasCredito;
            cliente.Notas = Input.Notas;
            cliente.Activo = Input.Activo;
            cliente.EsExonerado = Input.EsExonerado;
            cliente.ExoneradoConstancia = Input.ExoneradoConstancia;
            cliente.ExoneradoOrdenCompraExenta = Input.ExoneradoOrdenCompraExenta;
            cliente.ExoneradoRegistroSag = Input.ExoneradoRegistroSag;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar cliente",
                $"Editó al cliente '{cliente.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Cliente '{cliente.Nombre}' actualizado correctamente";
            return RedirectToPage("/Clientes/Index");
        }
    }
}