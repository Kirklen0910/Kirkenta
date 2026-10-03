using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Productos
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

        public List<Categoria> Categorias { get; set; } = new();
        public List<Impuesto> Impuestos { get; set; } = new();
        public List<UnidadMedida> Unidades { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

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
            public decimal PrecioCompra { get; set; }
            public decimal PrecioVenta { get; set; }
            public decimal PrecioMayorista { get; set; }
            public decimal Stock { get; set; }
            public decimal StockMinimo { get; set; }
            public string UnidadMedida { get; set; } = "Unidad";
            public bool Activo { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Productos/Index");
            }

            CargarDatos();

            var producto = _context.Productos.FirstOrDefault(p => p.Id == id);
            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado";
                return RedirectToPage("/Productos/Index");
            }

            Input = new InputModel
            {
                Id = producto.Id,
                SKU = producto.SKU,
                CodigoBarras = producto.CodigoBarras,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                CategoriaId = producto.CategoriaId,
                ImpuestoId = producto.ImpuestoId,
                PrecioCompra = producto.PrecioCompra,
                PrecioVenta = producto.PrecioVenta,
                PrecioMayorista = producto.PrecioMayorista,
                Stock = producto.Stock,
                StockMinimo = producto.StockMinimo,
                UnidadMedida = producto.UnidadMedida,
                Activo = producto.Activo
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Productos", "editar"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Productos/Index");
            }

            CargarDatos();

            if (!ModelState.IsValid) return Page();

            var producto = _context.Productos.FirstOrDefault(p => p.Id == Input.Id);
            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado";
                return RedirectToPage("/Productos/Index");
            }

            if (!string.IsNullOrWhiteSpace(Input.SKU) &&
                _context.Productos.Any(p => p.SKU == Input.SKU && p.Id != Input.Id))
            {
                ModelState.AddModelError("Input.SKU", "Este SKU ya existe");
                return Page();
            }

            producto.SKU = Input.SKU;
            producto.CodigoBarras = Input.CodigoBarras;
            producto.Nombre = Input.Nombre;
            producto.Descripcion = Input.Descripcion;
            producto.CategoriaId = Input.CategoriaId;
            producto.ImpuestoId = Input.ImpuestoId;
            producto.PrecioCompra = Input.PrecioCompra;
            producto.PrecioVenta = Input.PrecioVenta;
            producto.PrecioMayorista = Input.PrecioMayorista;
            producto.Stock = Input.Stock;
            producto.StockMinimo = Input.StockMinimo;
            producto.UnidadMedida = Input.UnidadMedida;
            producto.Activo = Input.Activo;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar producto",
                $"Editó el producto '{producto.Nombre}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Producto '{producto.Nombre}' actualizado";
            return RedirectToPage("/Productos/Index");
        }

        private void CargarDatos()
        {
            Categorias = _context.Categorias.Where(c => c.Activa).OrderBy(c => c.Nombre).ToList();
            Impuestos = _context.Impuestos.Where(i => i.Activo).OrderBy(i => i.Nombre).ToList();
            // 🔥 FIX: Cargar TODAS las unidades (activas o no) para que aparezcan
            Unidades = _context.UnidadesMedida.OrderBy(u => u.Nombre).ToList();
        }
    }
}