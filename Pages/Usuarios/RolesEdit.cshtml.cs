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
        public EditorPermisos Editor { get; set; } = new();

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
            Editor = EditorPermisos.Crear(_context, id);

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
            Editor = EditorPermisos.Crear(_context, Input.Id);
            Editor.Estado = EstadoDesde(Input.Permisos);

            if (!ModelState.IsValid)
            {
                CargarPermisosDesdeInput();
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
                CargarPermisosDesdeInput();
                return Page();
            }

            // Datos del rol ANTES de cambiarlos (para la auditoría)
            var nombreAntes = rol.Nombre;
            var descripcionAntes = rol.Descripcion;
            var colorAntes = rol.Color;

            // Actualizar rol
            if (!rol.EsSistema)
            {
                rol.Nombre = Input.Nombre;
            }
            rol.Descripcion = Input.Descripcion;
            rol.Color = Input.Color;

            // Borrar permisos viejos (se guardan antes para comparar)
            var permisosViejos = _context.Permisos.Where(p => p.RolId == rol.Id).ToList();
            var antes = permisosViejos.ToDictionary(
                p => (p.Modulo, p.Submodulo ?? ""),
                p => PermisoTexto(p.PuedeVer, p.PuedeCrear, p.PuedeEditar, p.PuedeEliminar));
            _context.Permisos.RemoveRange(permisosViejos);
            var despues = new Dictionary<(string, string), string>();

            // Insertar permisos nuevos: UNA fila por cada módulo y submódulo de ModulosERP.
            // Lo que no llegó marcado en el formulario se guarda explícitamente en falso.
            // (Los checkboxes sin marcar no se envían, y antes eso dejaba filas sin crear,
            // que luego PermisoSeeder rellenaba copiando los permisos del módulo padre.)
            // Las claves enviadas que no existan en ModulosERP se ignoran.
            foreach (var (modulo, submodulos) in ModulosERP.Modulos)
            {
                Input.Permisos.TryGetValue(modulo, out var permisoModulo);

                _context.Permisos.Add(new Permiso
                {
                    RolId = rol.Id,
                    Modulo = modulo,
                    Submodulo = null,
                    PuedeVer = permisoModulo?.PuedeVer ?? false,
                    PuedeCrear = permisoModulo?.PuedeCrear ?? false,
                    PuedeEditar = permisoModulo?.PuedeEditar ?? false,
                    PuedeEliminar = permisoModulo?.PuedeEliminar ?? false
                });
                despues[(modulo, "")] = PermisoTexto(
                    permisoModulo?.PuedeVer ?? false, permisoModulo?.PuedeCrear ?? false,
                    permisoModulo?.PuedeEditar ?? false, permisoModulo?.PuedeEliminar ?? false);

                foreach (var submodulo in submodulos)
                {
                    PermisoModuloInput? permisoSub = null;
                    if (permisoModulo != null)
                    {
                        permisoModulo.Submodulos.TryGetValue(submodulo, out permisoSub);
                    }

                    _context.Permisos.Add(new Permiso
                    {
                        RolId = rol.Id,
                        Modulo = modulo,
                        Submodulo = submodulo,
                        PuedeVer = permisoSub?.PuedeVer ?? false,
                        PuedeCrear = permisoSub?.PuedeCrear ?? false,
                        PuedeEditar = permisoSub?.PuedeEditar ?? false,
                        PuedeEliminar = permisoSub?.PuedeEliminar ?? false
                    });
                    despues[(modulo, submodulo)] = PermisoTexto(
                        permisoSub?.PuedeVer ?? false, permisoSub?.PuedeCrear ?? false,
                        permisoSub?.PuedeEditar ?? false, permisoSub?.PuedeEliminar ?? false);
                }
            }

            // Un solo SaveChanges: EF lo ejecuta como una transacción (borrado + inserción)
            _context.SaveChanges();

            // Auditoría: qué cambió exactamente (permisos con antes → después)
            var cambiosPermisos = new List<string>();
            foreach (var (clave, nuevoTexto) in despues)
            {
                antes.TryGetValue(clave, out var textoAntes);
                textoAntes ??= "(sin fila)";
                if (textoAntes != nuevoTexto)
                {
                    var nombre = clave.Item2 == "" ? clave.Item1 : $"{clave.Item1}/{clave.Item2}";
                    cambiosPermisos.Add($"{nombre}: {textoAntes} → {nuevoTexto}");
                }
            }
            var cambiosRol = CambiosHelper.Describir(
                ("Nombre", nombreAntes, rol.Nombre),
                ("Descripción", descripcionAntes, rol.Descripcion),
                ("Color", colorAntes, rol.Color));
            var detalleAuditoria = $"Rol '{rol.Nombre}'. " +
                (cambiosRol.StartsWith("sin cambios") ? "" : cambiosRol + ". ") +
                (cambiosPermisos.Count == 0
                    ? "Permisos sin cambios."
                    : $"{cambiosPermisos.Count} permiso(s) modificados: " +
                      string.Join(" | ", cambiosPermisos.Take(25)) +
                      (cambiosPermisos.Count > 25 ? $" | … y {cambiosPermisos.Count - 25} más" : ""));
            ActividadHelper.Registrar(_context, currentUser.Id, "Editar rol y permisos",
                detalleAuditoria.Length > 1800 ? detalleAuditoria.Substring(0, 1800) : detalleAuditoria,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Rol '{rol.Nombre}' actualizado correctamente";
            return RedirectToPage("/Usuarios/Roles");
        }

        private static string PermisoTexto(bool ver, bool crear, bool editar, bool eliminar)
        {
            var partes = new List<string>();
            if (ver) partes.Add("ver");
            if (crear) partes.Add("crear");
            if (editar) partes.Add("editar");
            if (eliminar) partes.Add("eliminar");
            return partes.Count == 0 ? "ninguno" : string.Join("+", partes);
        }

        /// <summary>
        /// Si el formulario vuelve a mostrarse por un error de validación, repone en la vista
        /// lo que el usuario había marcado. Antes volvía con todo desmarcado, y si el usuario
        /// guardaba de nuevo, el rol se quedaba sin permisos.
        /// </summary>
        private void CargarPermisosDesdeInput()
        {
            PermisosExistentes = new Dictionary<string, PermisoModuloView>();

            foreach (var (modulo, permisoModulo) in Input.Permisos)
            {
                var vista = new PermisoModuloView
                {
                    PuedeVer = permisoModulo.PuedeVer,
                    PuedeCrear = permisoModulo.PuedeCrear,
                    PuedeEditar = permisoModulo.PuedeEditar,
                    PuedeEliminar = permisoModulo.PuedeEliminar
                };

                foreach (var (submodulo, permisoSub) in permisoModulo.Submodulos)
                {
                    vista.Submodulos[submodulo] = new PermisoModuloView
                    {
                        PuedeVer = permisoSub.PuedeVer,
                        PuedeCrear = permisoSub.PuedeCrear,
                        PuedeEditar = permisoSub.PuedeEditar,
                        PuedeEliminar = permisoSub.PuedeEliminar
                    };
                }

                PermisosExistentes[modulo] = vista;
            }
        }

        /// <summary>Lo marcado en el formulario, para volver a mostrarlo si hay un error.</summary>
        private static Dictionary<string, bool[]> EstadoDesde(Dictionary<string, PermisoModuloInput> permisos)
        {
            var r = new Dictionary<string, bool[]>();
            foreach (var (mod, p) in permisos)
            {
                r[mod] = new[] { p.PuedeVer, p.PuedeCrear, p.PuedeEditar, p.PuedeEliminar };
                foreach (var (sub, s) in p.Submodulos)
                    r[mod + "/" + sub] = new[] { s.PuedeVer, s.PuedeCrear, s.PuedeEditar, s.PuedeEliminar };
            }
            return r;
        }
    }
}