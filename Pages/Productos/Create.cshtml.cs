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
        public List<Impuesto> Impuestos { get; set; } = new();
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

            [Range(0, double.MaxValue, ErrorMessage = "No puede ser negativo")]
            public decimal PrecioCompra { get; set; } = 0;
            [Range(0, double.MaxValue, ErrorMessage = "No puede ser negativo")]
            public decimal PrecioVenta { get; set; } = 0;
            [Range(0, double.MaxValue, ErrorMessage = "No puede ser negativo")]
            public decimal PrecioMayorista { get; set; } = 0;
            [Range(0, double.MaxValue, ErrorMessage = "No puede ser negativo")]
            public decimal Stock { get; set; } = 0;
            [Range(0, double.MaxValue, ErrorMessage = "No puede ser negativo")]
            public decimal StockMinimo { get; set; } = 0;
            public string UnidadMedida { get; set; } = "Unidad";
            public bool Activo { get; set; } = true;

            public bool PermiteDescuento { get; set; } = true;
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
            // El impuesto predeterminado del sistema viene preseleccionado
            Input.ImpuestoId = Impuestos.FirstOrDefault(i => i.EsPredeterminado)?.Id;
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

            if (Input.ImpuestoId.HasValue && !Impuestos.Any(i => i.Id == Input.ImpuestoId.Value))
            {
                ModelState.AddModelError("Input.ImpuestoId", "El impuesto seleccionado no es válido");
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(Input.CodigoBarras) &&
                _context.Productos.Any(p => p.CodigoBarras == Input.CodigoBarras))
            {
                ModelState.AddModelError("Input.CodigoBarras", "Este código de barras ya existe en otro producto");
                return Page();
            }

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
                ImpuestoId = Input.ImpuestoId,
                PrecioCompra = Input.PrecioCompra,
                PrecioVenta = Input.PrecioVenta,
                PrecioMayorista = Input.PrecioMayorista,
                Stock = 0,
                StockMinimo = Input.StockMinimo,
                UnidadMedida = Input.UnidadMedida,
                Activo = Input.Activo,
                PermiteDescuento = Input.PermiteDescuento,
                FechaCreacion = DateTime.Now
            };

            _context.Productos.Add(producto);
            _context.SaveChanges();

            // Stock inicial: entra por el kardex
            if (Input.Stock != 0)
            {
                InventarioHelper.Registrar(_context, producto, Input.Stock, "StockInicial",
                    producto.SKU, producto.Id, currentUser?.Id, producto.PrecioCompra, "Stock inicial al crear el producto");
                _context.SaveChanges();
            }

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
            Impuestos = _context.Impuestos.Where(i => i.Activo).OrderBy(i => i.Porcentaje).ThenBy(i => i.Nombre).ToList();
            SKUPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Producto");
        }
    }
}