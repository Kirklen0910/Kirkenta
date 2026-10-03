using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Pedidos
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

        public string Numero { get; set; } = "";

        public class InputModel
        {
            public int Id { get; set; }

            [Required]
            public string Estado { get; set; } = "Pendiente";

            public DateTime? FechaEntrega { get; set; }
            public string? Notas { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var pedido = _context.Pedidos.FirstOrDefault(p => p.Id == id);
            if (pedido == null)
            {
                TempData["Error"] = "Pedido no encontrado";
                return RedirectToPage("/Pedidos/Index");
            }

            Numero = pedido.Numero;

            Input = new InputModel
            {
                Id = pedido.Id,
                Estado = pedido.Estado,
                FechaEntrega = pedido.FechaEntrega,
                Notas = pedido.Notas
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            var pedido = _context.Pedidos.FirstOrDefault(p => p.Id == Input.Id);
            if (pedido == null)
            {
                TempData["Error"] = "Pedido no encontrado";
                return RedirectToPage("/Pedidos/Index");
            }

            pedido.Estado = Input.Estado;
            pedido.FechaEntrega = Input.FechaEntrega;
            pedido.Notas = Input.Notas;

            _context.SaveChanges();

            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Actualizar pedido",
                $"Cambió el estado del pedido {pedido.Numero} a '{pedido.Estado}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Pedido {pedido.Numero} actualizado";
            return RedirectToPage("/Pedidos/Details", new { id = pedido.Id });
        }
    }
}