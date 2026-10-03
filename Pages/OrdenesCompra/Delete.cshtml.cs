using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.OrdenesCompra
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Models.OrdenCompra Orden { get; set; } = new();
        public string ProveedorNombre { get; set; } = "";

        public IActionResult OnGet(int id)
        {
            var orden = _context.OrdenesCompra.FirstOrDefault(o => o.Id == id);
            if (orden == null)
            {
                TempData["Error"] = "Orden no encontrada";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            if (orden.Estado != "Borrador")
            {
                TempData["Error"] = "Solo se pueden eliminar órdenes en estado Borrador";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            Orden = orden;
            ProveedorNombre = _context.Proveedores.FirstOrDefault(p => p.Id == orden.ProveedorId)?.Nombre ?? "—";
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            var orden = _context.OrdenesCompra.FirstOrDefault(o => o.Id == id);
            if (orden == null)
            {
                TempData["Error"] = "Orden no encontrada";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            if (orden.Estado != "Borrador")
            {
                TempData["Error"] = "Solo se pueden eliminar órdenes en estado Borrador";
                return RedirectToPage("/OrdenesCompra/Index");
            }

            var numero = orden.Numero;
            _context.OrdenesCompra.Remove(orden);
            _context.SaveChanges();

            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Eliminar orden de compra",
                $"Eliminó la orden {numero}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Orden {numero} eliminada";
            return RedirectToPage("/OrdenesCompra/Index");
        }
    }
}