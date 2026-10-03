using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Categorias
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Categoria> Categorias { get; set; } = new();
        public Dictionary<int, int> ConteoProductos { get; set; } = new();
        public bool PuedeCrear { get; set; }

        public void OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            PuedeCrear = currentRol == "Admin" || PermisoHelper.TienePermiso(_context, currentRol, "Inventario", "Categorias", "crear");

            Categorias = _context.Categorias.OrderBy(c => c.Nombre).ToList();
            ConteoProductos = _context.Productos
                .Where(p => p.CategoriaId != null)
                .GroupBy(p => p.CategoriaId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}