using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Bajas
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public BajaInventario Baja { get; set; } = new();
        public Producto Producto { get; set; } = new();
        public bool EsAdmin { get; set; }
        public string UsuarioSolicita { get; set; } = "";
        public string UsuarioAprueba { get; set; } = "";
        public string UsuarioRevierte { get; set; } = "";

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            EsAdmin = currentRol == "Admin";

            var baja = _context.BajasInventario.FirstOrDefault(b => b.Id == id);
            if (baja == null)
            {
                TempData["Error"] = "Baja no encontrada";
                return RedirectToPage("/Bajas/Index");
            }

            Baja = baja;
            Producto = _context.Productos.FirstOrDefault(p => p.Id == baja.ProductoId) ?? new Producto();

            UsuarioSolicita = _context.Usuarios.FirstOrDefault(u => u.Id == baja.UsuarioSolicitaId)?.Username ?? "—";
            if (baja.UsuarioApruebaId.HasValue)
            {
                UsuarioAprueba = _context.Usuarios.FirstOrDefault(u => u.Id == baja.UsuarioApruebaId.Value)?.Username ?? "—";
            }
            if (baja.UsuarioRevierteId.HasValue)
            {
                UsuarioRevierte = _context.Usuarios.FirstOrDefault(u => u.Id == baja.UsuarioRevierteId.Value)?.Username ?? "—";
            }

            return Page();
        }
    }
}