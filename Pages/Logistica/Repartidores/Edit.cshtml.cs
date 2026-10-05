using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Logistica.Repartidores
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

        public string Codigo { get; set; } = "";
        public int RutasActivas { get; set; }

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El código es obligatorio")]
            [StringLength(20)]
            public string Codigo { get; set; } = "";

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(150)]
            public string Nombre { get; set; } = "";

            [StringLength(30)]
            public string? Telefono { get; set; }

            [StringLength(100)]
            public string? Vehiculo { get; set; }

            [StringLength(20)]
            public string? Placa { get; set; }

            [StringLength(50)]
            public string? Licencia { get; set; }

            [StringLength(300)]
            public string? Notas { get; set; }

            public bool Activo { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RepartidoresEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar repartidores";
                return RedirectToPage("/Logistica/Repartidores/Index");
            }

            var repartidor = _context.Repartidores.FirstOrDefault(r => r.Id == id);
            if (repartidor == null)
            {
                TempData["Error"] = "Repartidor no encontrado";
                return RedirectToPage("/Logistica/Repartidores/Index");
            }

            Codigo = repartidor.Codigo;

            Input = new InputModel
            {
                Id = repartidor.Id,
                Codigo = repartidor.Codigo,
                Nombre = repartidor.Nombre,
                Telefono = repartidor.Telefono,
                Vehiculo = repartidor.Vehiculo,
                Placa = repartidor.Placa,
                Licencia = repartidor.Licencia,
                Notas = repartidor.Notas,
                Activo = repartidor.Activo
            };

            RutasActivas = _context.Rutas
                .Count(r => r.RepartidorId == id
                         && (r.Estado == "Borrador" || r.Estado == "EnReparto"));

            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RepartidoresEdit", "editar"))
            {
                TempData["Error"] = "No tienes permiso para editar repartidores";
                return RedirectToPage("/Logistica/Repartidores/Index");
            }

            var repartidor = _context.Repartidores.FirstOrDefault(r => r.Id == Input.Id);
            if (repartidor == null)
            {
                TempData["Error"] = "Repartidor no encontrado";
                return RedirectToPage("/Logistica/Repartidores/Index");
            }

            Codigo = repartidor.Codigo;

            if (!ModelState.IsValid)
            {
                RutasActivas = _context.Rutas
                    .Count(r => r.RepartidorId == Input.Id
                             && (r.Estado == "Borrador" || r.Estado == "EnReparto"));
                return Page();
            }

            // Validar código único (excluyendo el actual)
            if (_context.Repartidores.Any(r => r.Codigo == Input.Codigo.Trim() && r.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Codigo", "Ya existe otro repartidor con este código");
                RutasActivas = _context.Rutas
                    .Count(r => r.RepartidorId == Input.Id
                             && (r.Estado == "Borrador" || r.Estado == "EnReparto"));
                return Page();
            }

            repartidor.Codigo = Input.Codigo.Trim();
            repartidor.Nombre = Input.Nombre;
            repartidor.Telefono = Input.Telefono;
            repartidor.Vehiculo = Input.Vehiculo;
            repartidor.Placa = Input.Placa;
            repartidor.Licencia = Input.Licencia;
            repartidor.Notas = Input.Notas;
            repartidor.Activo = Input.Activo;

            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Editar repartidor",
                $"Editó al repartidor {repartidor.Codigo} — {repartidor.Nombre}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Repartidor '{repartidor.Nombre}' actualizado correctamente";
            return RedirectToPage("/Logistica/Repartidores/Index");
        }
    }
}