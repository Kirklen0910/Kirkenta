using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Usuarios
{
    public class RolesCreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RolesCreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Dictionary<string, List<string>> Modulos { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(50)]
            public string Nombre { get; set; } = string.Empty;

            [StringLength(255)]
            public string? Descripcion { get; set; }

            public string Color { get; set; } = "#4f46e5";

            public Dictionary<string, PermisoModuloInput> Permisos { get; set; } = new();
        }

        public class PermisoModuloInput
        {
            public bool PuedeVer { get; set; }
            public bool PuedeCrear { get; set; }
            public bool PuedeEditar { get; set; }
            public bool PuedeEliminar { get; set; }
            public Dictionary<string, PermisoModuloInput> Submodulos { get; set; } = new();
        }

        public IActionResult OnGet()
        {
            // 🔒 Solo Admin
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            Modulos = ModulosERP.Modulos;
            return Page();
        }

        public IActionResult OnPost()
        {
            // 🔒 Solo Admin
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            Modulos = ModulosERP.Modulos;

            if (!ModelState.IsValid)
                return Page();

            if (_context.Roles.Any(r => r.Nombre.ToLower() == Input.Nombre.ToLower()))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe un rol con este nombre");
                return Page();
            }

            // Crear el rol
            var rol = new Rol
            {
                Nombre = Input.Nombre,
                Descripcion = Input.Descripcion,
                Color = Input.Color,
                EsSistema = false,
                FechaCreacion = DateTime.Now
            };

            _context.Roles.Add(rol);
            _context.SaveChanges();

            // Guardar permisos
            foreach (var moduloKvp in Input.Permisos)
            {
                var modulo = moduloKvp.Key;
                var permisoModulo = moduloKvp.Value;

                // Permiso del módulo padre (Submodulo = NULL)
                var permiso = new Permiso
                {
                    RolId = rol.Id,
                    Modulo = modulo,
                    Submodulo = null,
                    PuedeVer = permisoModulo.PuedeVer,
                    PuedeCrear = permisoModulo.PuedeCrear,
                    PuedeEditar = permisoModulo.PuedeEditar,
                    PuedeEliminar = permisoModulo.PuedeEliminar
                };
                _context.Permisos.Add(permiso);

                // Permisos de submódulos
                foreach (var subKvp in permisoModulo.Submodulos)
                {
                    var submodulo = subKvp.Key;
                    var permisoSub = subKvp.Value;

                    var permisoSubmodulo = new Permiso
                    {
                        RolId = rol.Id,
                        Modulo = modulo,
                        Submodulo = submodulo,
                        PuedeVer = permisoSub.PuedeVer,
                        PuedeCrear = permisoSub.PuedeCrear,
                        PuedeEditar = permisoSub.PuedeEditar,
                        PuedeEliminar = permisoSub.PuedeEliminar
                    };
                    _context.Permisos.Add(permisoSubmodulo);
                }
            }

            _context.SaveChanges();

            TempData["Success"] = $"Rol '{rol.Nombre}' creado correctamente";
            return RedirectToPage("/Usuarios/Roles");
        }
    }
}