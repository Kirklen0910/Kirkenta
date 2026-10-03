using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Productos
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

        public List<Categoria> Categorias { get; set; } = new();
        public List<UnidadMedida> Unidades { get; set; } = new();
        public string SKUPreview { get; set; } = "";

        public class InputModel
        {
            [StringLength(50)]
            public string? SKU { get; set; }

            [StringLength(50)]
            public string? CodigoBarras { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(150)]
            public string Nombre { get; set; } = string.Empty;

            public string? Descripcion { get; set; }
            public int? CategoriaId { get; set; }
            public int? ImpuestoId { get; set; }

            public decimal PrecioCompra { get; set; } = 0;
            public decimal PrecioVenta { get; set; } = 0;
            public decimal PrecioMayorista { get; set; } = 0;
            public decimal Stock { get; set; } = 0;
            public decimal StockMinimo { get; set; } = 0;
            public string UnidadMedida { get; set; } = "Unidad";
            public bool Activo { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear productos";
                return RedirectToPage("/Productos/Index");
            }

            CargarDatos();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Productos/Index");
            }

            CargarDatos();

            if (!ModelState.IsValid) return Page();

            if (string.IsNullOrWhiteSpace(Input.SKU))
            {
                Input.SKU = NumeroDocumentoHelper.GenerarSiguiente(_context, "Producto");
            }
            else
            {
                if (_context.Productos.Any(p => p.SKU == Input.SKU))
                {
                    ModelState.AddModelError("Input.SKU", "Este SKU ya existe");
                    return Page();
                }
            }

            var producto = new Producto
            {
                SKU = Input.SKU,
                CodigoBarras = Input.CodigoBarras,
                Nombre = Input.Nombre,
                Descripcion = Input.Descripcion,
                CategoriaId = Input.CategoriaId,
                ImpuestoId = null,
                PrecioCompra = Input.PrecioCompra,
                PrecioVenta = Input.PrecioVenta,
                PrecioMayorista = Input.PrecioMayorista,
                Stock = Input.Stock,
                StockMinimo = Input.StockMinimo,
                UnidadMedida = Input.UnidadMedida,
                Activo = Input.Activo,
                FechaCreacion = DateTime.Now
            };

            _context.Productos.Add(producto);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear producto",
                $"Creó el producto '{producto.Nombre}' ({producto.SKU})",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Producto '{producto.Nombre}' creado correctamente";
            return RedirectToPage("/Productos/Index");
        }

        private void CargarDatos()
        {
            Categorias = _context.Categorias.Where(c => c.Activa).OrderBy(c => c.Nombre).ToList();
            Unidades = _context.UnidadesMedida.OrderBy(u => u.Nombre).ToList();
            SKUPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Producto");
        }
    }
}