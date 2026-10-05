using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Logistica.Repartidores
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

        public string CodigoPreview { get; set; } = "";

        public class InputModel
        {
            [StringLength(20)]
            public string? Codigo { get; set; }

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

            public bool Activo { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RepartidoresCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear repartidores";
                return RedirectToPage("/Logistica/Repartidores/Index");
            }

            CodigoPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Repartidor");
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Logistica", "RepartidoresCreate", "crear"))
            {
                TempData["Error"] = "No tienes permiso para crear repartidores";
                return RedirectToPage("/Logistica/Repartidores/Index");
            }

            CodigoPreview = NumeroDocumentoHelper.PreviewSiguiente(_context, "Repartidor");

            if (!ModelState.IsValid) return Page();

            // Generar código si viene vacío
            if (string.IsNullOrWhiteSpace(Input.Codigo))
            {
                Input.Codigo = NumeroDocumentoHelper.GenerarSiguiente(_context, "Repartidor");
            }
            else
            {
                Input.Codigo = Input.Codigo.Trim();

                if (_context.Repartidores.Any(r => r.Codigo == Input.Codigo))
                {
                    ModelState.AddModelError("Input.Codigo", "Ya existe un repartidor con este código");
                    return Page();
                }
            }

            var repartidor = new Repartidor
            {
                Codigo = Input.Codigo!,
                Nombre = Input.Nombre,
                Telefono = Input.Telefono,
                Vehiculo = Input.Vehiculo,
                Placa = Input.Placa,
                Licencia = Input.Licencia,
                Notas = Input.Notas,
                Activo = Input.Activo,
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = currentUser?.Id,
                EmpresaId = 1
            };

            _context.Repartidores.Add(repartidor);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear repartidor",
                $"Creó al repartidor {repartidor.Codigo} — {repartidor.Nombre}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Repartidor '{repartidor.Nombre}' creado correctamente";
            return RedirectToPage("/Logistica/Repartidores/Index");
        }
    }
}