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

            public bool PermiteDescuento { get; set; } = true;
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
                Activo = producto.Activo,
                PermiteDescuento = producto.PermiteDescuento
            };
            IncluirImpuestoActual(producto.ImpuestoId);

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
            IncluirImpuestoActual(Input.ImpuestoId);

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

            if (Input.ImpuestoId.HasValue && !_context.Impuestos.Any(i => i.Id == Input.ImpuestoId.Value))
            {
                ModelState.AddModelError("Input.ImpuestoId", "El impuesto seleccionado no es válido");
                return Page();
            }

            // Valores de antes, para dejar el cambio en la bitácora
            var antes = (producto.Nombre, producto.SKU, producto.PrecioCompra, producto.PrecioVenta,
                         producto.PrecioMayorista, producto.ImpuestoId, producto.CategoriaId,
                         producto.StockMinimo, producto.Activo, producto.Stock);
            var antesPermiteDescuento = producto.PermiteDescuento;

            producto.SKU = Input.SKU;
            producto.CodigoBarras = Input.CodigoBarras;
            producto.Nombre = Input.Nombre;
            producto.Descripcion = Input.Descripcion;
            producto.CategoriaId = Input.CategoriaId;
            producto.ImpuestoId = Input.ImpuestoId;
            producto.PrecioCompra = Input.PrecioCompra;
            producto.PrecioVenta = Input.PrecioVenta;
            producto.PrecioMayorista = Input.PrecioMayorista;
            // El stock jamás se pisa directo: todo cambio deja rastro en el kardex
            if (Input.Stock < 0 && Input.Stock != producto.Stock)
            {
                ModelState.AddModelError("Input.Stock", "El stock no puede ser negativo");
                return Page();
            }
            if (Input.PrecioCompra < 0 || Input.PrecioVenta < 0 || Input.PrecioMayorista < 0)
            {
                ModelState.AddModelError(string.Empty, "Los precios no pueden ser negativos");
                return Page();
            }
            InventarioHelper.Ajustar(_context, producto, Input.Stock, "AjusteManual", currentUser?.Id,
                "Ajuste desde la edición del producto");
            producto.StockMinimo = Input.StockMinimo;
            producto.UnidadMedida = Input.UnidadMedida;
            producto.Activo = Input.Activo;
            producto.PermiteDescuento = Input.PermiteDescuento;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar producto",
                $"Editó el producto '{producto.Nombre}'. " + CambiosHelper.Describir(
                    ("Nombre", antes.Nombre, producto.Nombre),
                    ("SKU", antes.SKU, producto.SKU),
                    ("Precio compra", antes.PrecioCompra, producto.PrecioCompra),
                    ("Precio venta", antes.PrecioVenta, producto.PrecioVenta),
                    ("Precio mayorista", antes.PrecioMayorista, producto.PrecioMayorista),
                    ("Impuesto", antes.ImpuestoId, producto.ImpuestoId),
                    ("Categoría", antes.CategoriaId, producto.CategoriaId),
                    ("Stock mínimo", antes.StockMinimo, producto.StockMinimo),
                    ("Activo", antes.Activo, producto.Activo),
                    ("Admite descuento", antesPermiteDescuento, producto.PermiteDescuento),
                    ("Stock", antes.Stock, producto.Stock)),
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Producto '{producto.Nombre}' actualizado";
            return RedirectToPage("/Productos/Index");
        }

        /// <summary>Si el producto tiene un impuesto ya inactivo, se mantiene visible para no perderlo al guardar.</summary>
        private void IncluirImpuestoActual(int? impuestoId)
        {
            if (impuestoId.HasValue && !Impuestos.Any(i => i.Id == impuestoId.Value))
            {
                var actual = _context.Impuestos.FirstOrDefault(i => i.Id == impuestoId.Value);
                if (actual != null) Impuestos.Add(actual);
            }
        }

        private void CargarDatos()
        {
            Categorias = _context.Categorias.Where(c => c.Activa).OrderBy(c => c.Nombre).ToList();
            Impuestos = _context.Impuestos.Where(i => i.Activo).OrderBy(i => i.Porcentaje).ThenBy(i => i.Nombre).ToList();
            // 🔥 FIX: Cargar TODAS las unidades (activas o no) para que aparezcan
            Unidades = _context.UnidadesMedida.OrderBy(u => u.Nombre).ToList();
        }
    }
}