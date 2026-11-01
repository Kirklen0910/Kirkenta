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

            // Rol + permisos en una sola transacción: o se guarda todo o nada
            using var transaction = _context.Database.BeginTransaction();
            try
            {
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

                _context.Permisos.AddRange(ConstruirPermisos(rol.Id));
                _context.SaveChanges();

                transaction.Commit();

                TempData["Success"] = $"Rol '{rol.Nombre}' creado correctamente";
                return RedirectToPage("/Usuarios/Roles");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"[RolesCreate] Error al crear el rol: {ex.Message}");
                ModelState.AddModelError(string.Empty, "No se pudo crear el rol. Intenta de nuevo.");
                return Page();
            }
        }

        /// <summary>
        /// Construye UNA fila de permiso por cada módulo y submódulo definido en ModulosERP.
        /// Lo que no llegó marcado en el formulario se guarda explícitamente en falso.
        /// (Los checkboxes sin marcar no se envían, y antes eso dejaba filas sin crear,
        /// que luego PermisoSeeder rellenaba copiando los permisos del módulo padre.)
        /// Las claves enviadas que no existan en ModulosERP se ignoran.
        /// </summary>
        private List<Permiso> ConstruirPermisos(int rolId)
        {
            var lista = new List<Permiso>();

            foreach (var (modulo, submodulos) in ModulosERP.Modulos)
            {
                Input.Permisos.TryGetValue(modulo, out var permisoModulo);

                lista.Add(new Permiso
                {
                    RolId = rolId,
                    Modulo = modulo,
                    Submodulo = null,
                    PuedeVer = permisoModulo?.PuedeVer ?? false,
                    PuedeCrear = permisoModulo?.PuedeCrear ?? false,
                    PuedeEditar = permisoModulo?.PuedeEditar ?? false,
                    PuedeEliminar = permisoModulo?.PuedeEliminar ?? false
                });

                foreach (var submodulo in submodulos)
                {
                    PermisoModuloInput? permisoSub = null;
                    if (permisoModulo != null)
                    {
                        permisoModulo.Submodulos.TryGetValue(submodulo, out permisoSub);
                    }

                    lista.Add(new Permiso
                    {
                        RolId = rolId,
                        Modulo = modulo,
                        Submodulo = submodulo,
                        PuedeVer = permisoSub?.PuedeVer ?? false,
                        PuedeCrear = permisoSub?.PuedeCrear ?? false,
                        PuedeEditar = permisoSub?.PuedeEditar ?? false,
                        PuedeEliminar = permisoSub?.PuedeEliminar ?? false
                    });
                }
            }

            return lista;
        }
    }
}