using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Usuarios
{
    public class RolesEditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RolesEditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Dictionary<string, List<string>> Modulos { get; set; } = new();

        public Dictionary<string, PermisoModuloView> PermisosExistentes { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(50)]
            public string Nombre { get; set; } = string.Empty;

            [StringLength(255)]
            public string? Descripcion { get; set; }

            public string Color { get; set; } = "#4f46e5";

            public bool EsSistema { get; set; }

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

        public class PermisoModuloView
        {
            public bool PuedeVer { get; set; }
            public bool PuedeCrear { get; set; }
            public bool PuedeEditar { get; set; }
            public bool PuedeEliminar { get; set; }
            public Dictionary<string, PermisoModuloView> Submodulos { get; set; } = new();
        }

        public IActionResult OnGet(int id)
        {
            // 🔒 Solo Admin
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            if (currentUser == null || currentUser.Rol != "Admin")
            {
                TempData["Error"] = "No tienes permiso para acceder a esta sección";
                return RedirectToPage("/Index");
            }

            var rol = _context.Roles.FirstOrDefault(r => r.Id == id);
            if (rol == null)
            {
                TempData["Error"] = "Rol no encontrado";
                return RedirectToPage("/Usuarios/Roles");
            }

            Modulos = ModulosERP.Modulos;

            Input = new InputModel
            {
                Id = rol.Id,
                Nombre = rol.Nombre,
                Descripcion = rol.Descripcion,
                Color = rol.Color,
                EsSistema = rol.EsSistema
            };

            // Cargar permisos existentes
            var permisosBd = _context.Permisos.Where(p => p.RolId == id).ToList();

            foreach (var permiso in permisosBd)
            {
                if (permiso.Submodulo == null)
                {
                    // Permiso del módulo padre
                    if (!PermisosExistentes.ContainsKey(permiso.Modulo))
                    {
                        PermisosExistentes[permiso.Modulo] = new PermisoModuloView();
                    }
                    PermisosExistentes[permiso.Modulo].PuedeVer = permiso.PuedeVer;
                    PermisosExistentes[permiso.Modulo].PuedeCrear = permiso.PuedeCrear;
                    PermisosExistentes[permiso.Modulo].PuedeEditar = permiso.PuedeEditar;
                    PermisosExistentes[permiso.Modulo].PuedeEliminar = permiso.PuedeEliminar;
                }
                else
                {
                    // Permiso de submódulo
                    if (!PermisosExistentes.ContainsKey(permiso.Modulo))
                    {
                        PermisosExistentes[permiso.Modulo] = new PermisoModuloView();
                    }
                    if (!PermisosExistentes[permiso.Modulo].Submodulos.ContainsKey(permiso.Submodulo))
                    {
                        PermisosExistentes[permiso.Modulo].Submodulos[permiso.Submodulo] = new PermisoModuloView();
                    }
                    PermisosExistentes[permiso.Modulo].Submodulos[permiso.Submodulo].PuedeVer = permiso.PuedeVer;
                    PermisosExistentes[permiso.Modulo].Submodulos[permiso.Submodulo].PuedeCrear = permiso.PuedeCrear;
                    PermisosExistentes[permiso.Modulo].Submodulos[permiso.Submodulo].PuedeEditar = permiso.PuedeEditar;
                    PermisosExistentes[permiso.Modulo].Submodulos[permiso.Submodulo].PuedeEliminar = permiso.PuedeEliminar;
                }
            }

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
            {
                return Page();
            }

            var rol = _context.Roles.FirstOrDefault(r => r.Id == Input.Id);
            if (rol == null)
            {
                TempData["Error"] = "Rol no encontrado";
                return RedirectToPage("/Usuarios/Roles");
            }

            // Verificar nombre duplicado
            if (_context.Roles.Any(r => r.Nombre.ToLower() == Input.Nombre.ToLower() && r.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Nombre", "Ya existe un rol con este nombre");
                return Page();
            }

            // Actualizar rol
            if (!rol.EsSistema)
            {
                rol.Nombre = Input.Nombre;
            }
            rol.Descripcion = Input.Descripcion;
            rol.Color = Input.Color;

            // Borrar permisos viejos
            var permisosViejos = _context.Permisos.Where(p => p.RolId == rol.Id);
            _context.Permisos.RemoveRange(permisosViejos);

            // Insertar permisos nuevos
            foreach (var moduloKvp in Input.Permisos)
            {
                var modulo = moduloKvp.Key;
                var permisoModulo = moduloKvp.Value;

                _context.Permisos.Add(new Permiso
                {
                    RolId = rol.Id,
                    Modulo = modulo,
                    Submodulo = null,
                    PuedeVer = permisoModulo.PuedeVer,
                    PuedeCrear = permisoModulo.PuedeCrear,
                    PuedeEditar = permisoModulo.PuedeEditar,
                    PuedeEliminar = permisoModulo.PuedeEliminar
                });

                foreach (var subKvp in permisoModulo.Submodulos)
                {
                    var submodulo = subKvp.Key;
                    var permisoSub = subKvp.Value;

                    _context.Permisos.Add(new Permiso
                    {
                        RolId = rol.Id,
                        Modulo = modulo,
                        Submodulo = submodulo,
                        PuedeVer = permisoSub.PuedeVer,
                        PuedeCrear = permisoSub.PuedeCrear,
                        PuedeEditar = permisoSub.PuedeEditar,
                        PuedeEliminar = permisoSub.PuedeEliminar
                    });
                }
            }

            _context.SaveChanges();

            TempData["Success"] = $"Rol '{rol.Nombre}' actualizado correctamente";
            return RedirectToPage("/Usuarios/Roles");
        }
    }
}