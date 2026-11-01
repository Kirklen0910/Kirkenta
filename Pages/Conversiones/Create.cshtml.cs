using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Conversiones
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

        public List<Producto> Productos { get; set; } = new();
        public List<string> Unidades { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Elige el producto que se va a fraccionar")]
            public int ProductoOrigenId { get; set; }

            [Range(0.0001, double.MaxValue, ErrorMessage = "La cantidad a sacar debe ser mayor a 0")]
            public decimal CantidadOrigen { get; set; } = 1;

            /// <summary>"Existente" o "Nuevo".</summary>
            public string Destino { get; set; } = "Nuevo";
            public int? ProductoDestinoId { get; set; }
            [StringLength(150)]
            public string? NuevoNombre { get; set; }
            [StringLength(20)]
            public string? NuevaUnidad { get; set; }
            public decimal NuevoPrecioVenta { get; set; }

            [Range(0.0001, double.MaxValue, ErrorMessage = "La cantidad resultante debe ser mayor a 0")]
            public decimal CantidadDestino { get; set; }

            [Required(ErrorMessage = "El motivo es obligatorio")]
            [StringLength(1000, MinimumLength = 5, ErrorMessage = "Escribe un motivo (mínimo 5 caracteres)")]
            public string Motivo { get; set; } = string.Empty;
        }

        private bool TienePermiso(string accion)
        {
            var user = _context.Usuarios.AsNoTracking().FirstOrDefault(u => u.Username == User.Identity!.Name);
            var rol = user?.Rol ?? "Pendiente";
            return rol == "Admin" || PermisoHelper.TienePermiso(_context, rol, "Inventario", "Bajas", accion);
        }

        private void Cargar()
        {
            Productos = _context.Productos.AsNoTracking().Where(p => p.Activo).OrderBy(p => p.Nombre).ToList();
            Unidades = _context.UnidadesMedida.AsNoTracking().Select(u => u.Nombre).OrderBy(n => n).ToList();
        }

        public IActionResult OnGet(int? origen)
        {
            if (!TienePermiso("crear"))
            {
                TempData["Error"] = "No tienes permiso para solicitar conversiones";
                return RedirectToPage("/Conversiones/Index");
            }
            if (origen.HasValue) Input.ProductoOrigenId = origen.Value;
            Cargar();
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!TienePermiso("crear"))
            {
                TempData["Error"] = "No tienes permiso para solicitar conversiones";
                return RedirectToPage("/Conversiones/Index");
            }
            Cargar();

            var nuevo = Input.Destino != "Existente";
            if (nuevo && string.IsNullOrWhiteSpace(Input.NuevoNombre))
                ModelState.AddModelError("Input.NuevoNombre", "Escribe el nombre del producto nuevo");
            if (!nuevo && !Input.ProductoDestinoId.HasValue)
                ModelState.AddModelError("Input.ProductoDestinoId", "Elige el producto que recibirá lo convertido");
            if (!nuevo && Input.ProductoDestinoId == Input.ProductoOrigenId)
                ModelState.AddModelError("Input.ProductoDestinoId", "El destino debe ser distinto al producto de origen");
            if (nuevo && Input.NuevoPrecioVenta < 0)
                ModelState.AddModelError("Input.NuevoPrecioVenta", "El precio no puede ser negativo");

            var origenProd = _context.Productos.AsNoTracking().FirstOrDefault(p => p.Id == Input.ProductoOrigenId);
            if (origenProd == null)
                ModelState.AddModelError("Input.ProductoOrigenId", "Producto de origen no encontrado");
            else if (Input.CantidadOrigen > origenProd.Stock)
                ModelState.AddModelError("Input.CantidadOrigen", $"No hay suficiente: stock actual {origenProd.Stock:N2}");

            if (!ModelState.IsValid) return Page();

            var user = _context.Usuarios.First(u => u.Username == User.Identity!.Name);
            var conv = new ConversionInventario
            {
                Numero = "TMP-" + Guid.NewGuid().ToString("N"),
                Fecha = DateTime.Now,
                ProductoOrigenId = Input.ProductoOrigenId,
                CantidadOrigen = Input.CantidadOrigen,
                ProductoDestinoId = nuevo ? null : Input.ProductoDestinoId,
                NuevoProductoNombre = nuevo ? Input.NuevoNombre!.Trim() : null,
                NuevoProductoUnidad = nuevo ? Input.NuevaUnidad : null,
                NuevoProductoPrecioVenta = nuevo ? Input.NuevoPrecioVenta : 0,
                CantidadDestino = Input.CantidadDestino,
                Motivo = Input.Motivo.Trim(),
                Estado = "Pendiente",
                UsuarioSolicitaId = user.Id
            };
            _context.ConversionesInventario.Add(conv);
            _context.SaveChanges();
            conv.Numero = $"CNV-{conv.Id:D5}";
            _context.SaveChanges();

            ActividadHelper.Registrar(_context, user.Id, "Solicitar conversión de inventario",
                $"Solicitó conversión {conv.Numero}: {conv.CantidadOrigen} x '{origenProd!.Nombre}' → {conv.CantidadDestino}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Conversión {conv.Numero} solicitada. Queda pendiente de aprobación.";
            return RedirectToPage("/Conversiones/Index");
        }
    }
}
