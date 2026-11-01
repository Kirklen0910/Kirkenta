using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Productos
{
    public class AjusteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AjusteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Producto Producto { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int ProductoId { get; set; }

            [Required(ErrorMessage = "Indica el stock real contado")]
            [Range(0, 100000000, ErrorMessage = "El stock no puede ser negativo")]
            public decimal StockReal { get; set; }

            [Required(ErrorMessage = "El motivo es obligatorio")]
            [StringLength(250)]
            public string Motivo { get; set; } = string.Empty;
        }

        private bool TienePermiso()
            => PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Inventario", "Productos", "editar");

        public IActionResult OnGet(int id)
        {
            if (!TienePermiso())
            {
                TempData["Error"] = "No tienes permiso para ajustar inventario";
                return RedirectToPage("/Productos/Index");
            }

            var p = _context.Productos.FirstOrDefault(x => x.Id == id);
            if (p == null)
            {
                TempData["Error"] = "Producto no encontrado";
                return RedirectToPage("/Productos/Index");
            }

            Producto = p;
            Input = new InputModel { ProductoId = p.Id, StockReal = p.Stock };
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!TienePermiso())
            {
                TempData["Error"] = "No tienes permiso para ajustar inventario";
                return RedirectToPage("/Productos/Index");
            }

            var p = _context.Productos.FirstOrDefault(x => x.Id == Input.ProductoId);
            if (p == null)
            {
                TempData["Error"] = "Producto no encontrado";
                return RedirectToPage("/Productos/Index");
            }

            Producto = p;
            if (!ModelState.IsValid) return Page();

            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var anterior = p.Stock;

            var mov = InventarioHelper.Ajustar(_context, p, Input.StockReal, "AjusteManual",
                currentUser?.Id, Input.Motivo, p.SKU, p.Id);

            if (mov == null)
            {
                TempData["Success"] = "El stock ya coincide, no se hizo ningún ajuste.";
                return RedirectToPage("/Productos/Kardex", new { productoId = p.Id });
            }

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Ajuste de inventario",
                $"Ajustó '{p.Nombre}' de {anterior:0.##} a {p.Stock:0.##}. Motivo: {Input.Motivo}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Stock de '{p.Nombre}' ajustado de {anterior:0.##} a {p.Stock:0.##}.";
            return RedirectToPage("/Productos/Kardex", new { productoId = p.Id });
        }
    }
}
