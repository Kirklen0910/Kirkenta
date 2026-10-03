using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Kirkenta.Pages.Bajas
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

        public string NumeroPreview { get; set; } = "";
        public string ProductosJson { get; set; } = "[]";

        public class InputModel
        {
            [Required(ErrorMessage = "Debes seleccionar un producto")]
            public int ProductoId { get; set; }

            [Required(ErrorMessage = "La cantidad es obligatoria")]
            [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
            public decimal Cantidad { get; set; }

            [Required(ErrorMessage = "El tipo de baja es obligatorio")]
            public string TipoBaja { get; set; } = string.Empty;

            [Required(ErrorMessage = "El motivo es obligatorio")]
            [StringLength(1000, MinimumLength = 10, ErrorMessage = "El motivo debe tener al menos 10 caracteres")]
            public string Motivo { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Bajas", "crear"))
            {
                TempData["Error"] = "No tienes permiso para registrar bajas";
                return RedirectToPage("/Bajas/Index");
            }

            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Baja");
            ProductosJson = ObtenerProductosJson();
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin" && !PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Bajas", "crear"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Bajas/Index");
            }

            NumeroPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Baja");
            ProductosJson = ObtenerProductosJson();

            if (!ModelState.IsValid) return Page();

            var producto = _context.Productos.FirstOrDefault(p => p.Id == Input.ProductoId);
            if (producto == null)
            {
                ModelState.AddModelError("Input.ProductoId", "Producto no encontrado");
                return Page();
            }

            // Validar que no exceda el stock
            if (Input.Cantidad > producto.Stock)
            {
                ModelState.AddModelError("Input.Cantidad", $"No puedes dar de baja más de lo disponible. Stock actual: {producto.Stock}");
                return Page();
            }

            // Crear la baja (queda en estado Pendiente)
            var numero = NumeroDocumentoHelper.GenerarSiguiente(_context, "Baja");
            var valorUnitario = producto.PrecioCompra;
            var valorTotal = valorUnitario * Input.Cantidad;

            var baja = new BajaInventario
            {
                Numero = numero,
                Fecha = DateTime.Now,
                ProductoId = Input.ProductoId,
                Cantidad = Input.Cantidad,
                TipoBaja = Input.TipoBaja,
                Motivo = Input.Motivo,
                ValorUnitario = valorUnitario,
                ValorTotal = valorTotal,
                Estado = "Pendiente",
                UsuarioSolicitaId = currentUser!.Id
            };

            _context.BajasInventario.Add(baja);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser.Id,
                "Solicitar baja de inventario",
                $"Solicitó baja {baja.Numero}: {baja.Cantidad} x '{producto.Nombre}' por {baja.TipoBaja}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Baja {baja.Numero} solicitada. Pendiente de aprobación.";
            return RedirectToPage("/Bajas/Index");
        }

        private string ObtenerProductosJson()
        {
            var productos = _context.Productos
                .Where(p => p.Activo && p.Stock > 0)
                .Select(p => new
                {
                    id = p.Id,
                    nombre = p.Nombre,
                    sku = p.SKU ?? "",
                    stock = p.Stock,
                    precioCompra = p.PrecioCompra
                })
                .ToList();

            return JsonSerializer.Serialize(productos);
        }
    }
}