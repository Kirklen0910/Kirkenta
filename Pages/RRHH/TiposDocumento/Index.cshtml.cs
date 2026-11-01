using System.ComponentModel.DataAnnotations;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.RRHH.TiposDocumento
{
    /// <summary>Tipos de documento que se pueden adjuntar al expediente de un empleado (contrato, DNI, carnet de salud, etc.).</summary>
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) { _context = context; }

        public static readonly string[] Categorias = { "Personales", "Antecedentes", "Referencias", "Academicos", "Contrato", "Medicos", "Otros" };

        public List<TipoDocumentoEmpleado> Tipos { get; set; } = new();
        public Dictionary<int, int> Usos { get; set; } = new();
        public bool PuedeCrear { get; private set; }
        public bool PuedeEditar { get; private set; }
        public bool PuedeEliminar { get; private set; }

        [BindProperty] public TipoInput Input { get; set; } = new();

        public class TipoInput
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(100)]
            public string Nombre { get; set; } = "";

            [StringLength(300)]
            public string? Descripcion { get; set; }

            [Required]
            [StringLength(50)]
            public string Categoria { get; set; } = "Otros";

            public bool EsObligatorio { get; set; }
            public bool RequiereVencimiento { get; set; }

            [Range(0, 3650, ErrorMessage = "Los días de alerta deben estar entre 0 y 3650")]
            public int? DiasAlertaVencimiento { get; set; }

            [Range(0, 1000)]
            public int Orden { get; set; }

            public bool Activo { get; set; } = true;
        }

        private const string Modulo = "Configuracion";
        private const string Sub = "TiposDocumento";

        private bool Cargar()
        {
            var usr = User.Identity?.Name;
            if (!PermisoHelper.UsuarioTieneAcceso(_context, usr, Modulo, Sub)) return false;
            PuedeCrear = PermisoHelper.UsuarioTienePermiso(_context, usr, Modulo, Sub, "crear");
            PuedeEditar = PermisoHelper.UsuarioTienePermiso(_context, usr, Modulo, Sub, "editar");
            PuedeEliminar = PermisoHelper.UsuarioTienePermiso(_context, usr, Modulo, Sub, "eliminar");
            Tipos = _context.TiposDocumentoEmpleado.OrderBy(t => t.Orden).ThenBy(t => t.Nombre).ToList();
            Usos = _context.AdjuntosEmpleado.GroupBy(a => a.TipoDocumentoId)
                .Select(g => new { g.Key, N = g.Count() }).ToDictionary(x => x.Key, x => x.N);
            return true;
        }

        private IActionResult SinPermiso()
        {
            TempData["Error"] = "No tienes permiso para acceder a esta sección";
            return RedirectToPage("/Index");
        }

        public IActionResult OnGet(int? editar)
        {
            if (!Cargar()) return SinPermiso();
            if (editar.HasValue && PuedeEditar)
            {
                var t = _context.TiposDocumentoEmpleado.FirstOrDefault(x => x.Id == editar.Value);
                if (t != null)
                    Input = new TipoInput
                    {
                        Id = t.Id, Nombre = t.Nombre, Descripcion = t.Descripcion, Categoria = t.Categoria,
                        EsObligatorio = t.EsObligatorio, RequiereVencimiento = t.RequiereVencimiento,
                        DiasAlertaVencimiento = t.DiasAlertaVencimiento, Orden = t.Orden, Activo = t.Activo
                    };
            }
            else
            {
                Input.Orden = (Tipos.Count == 0 ? 0 : Tipos.Max(x => x.Orden)) + 1;
            }
            return Page();
        }

        public IActionResult OnPostGuardar()
        {
            if (!Cargar()) return SinPermiso();
            var esNuevo = Input.Id == 0;
            if (esNuevo ? !PuedeCrear : !PuedeEditar) return SinPermiso();
            if (!ModelState.IsValid) return Page();

            var nombre = Input.Nombre.Trim();
            if (!Categorias.Contains(Input.Categoria)) Input.Categoria = "Otros";
            if (_context.TiposDocumentoEmpleado.Any(t => t.Nombre == nombre && t.Id != Input.Id))
            {
                ModelState.AddModelError("", $"Ya existe un tipo de documento llamado \"{nombre}\".");
                return Page();
            }

            TipoDocumentoEmpleado? t;
            if (esNuevo) t = new TipoDocumentoEmpleado { FechaCreacion = DateTime.Now };
            else
            {
                t = _context.TiposDocumentoEmpleado.FirstOrDefault(x => x.Id == Input.Id);
                if (t == null) { TempData["Error"] = "Tipo de documento no encontrado"; return RedirectToPage(); }
            }

            t.Nombre = nombre;
            t.Descripcion = string.IsNullOrWhiteSpace(Input.Descripcion) ? null : Input.Descripcion.Trim();
            t.Categoria = Input.Categoria;
            t.EsObligatorio = Input.EsObligatorio;
            t.RequiereVencimiento = Input.RequiereVencimiento;
            t.DiasAlertaVencimiento = Input.RequiereVencimiento ? (Input.DiasAlertaVencimiento ?? 30) : null;
            t.Orden = Input.Orden;
            t.Activo = Input.Activo;

            if (esNuevo) _context.TiposDocumentoEmpleado.Add(t);
            _context.SaveChanges();

            Registrar(esNuevo ? "Crear tipo de documento" : "Editar tipo de documento", t.Nombre);
            TempData["Success"] = esNuevo ? $"Tipo \"{t.Nombre}\" creado" : $"Tipo \"{t.Nombre}\" actualizado";
            return RedirectToPage();
        }

        public IActionResult OnPostEstado(int id)
        {
            if (!Cargar() || !PuedeEditar) return SinPermiso();
            var t = _context.TiposDocumentoEmpleado.FirstOrDefault(x => x.Id == id);
            if (t == null) { TempData["Error"] = "Tipo de documento no encontrado"; return RedirectToPage(); }
            t.Activo = !t.Activo;
            _context.SaveChanges();
            Registrar(t.Activo ? "Activar tipo de documento" : "Desactivar tipo de documento", t.Nombre);
            TempData["Success"] = $"\"{t.Nombre}\" {(t.Activo ? "activado" : "desactivado")}";
            return RedirectToPage();
        }

        public IActionResult OnPostEliminar(int id)
        {
            if (!Cargar() || !PuedeEliminar) return SinPermiso();
            var t = _context.TiposDocumentoEmpleado.FirstOrDefault(x => x.Id == id);
            if (t == null) { TempData["Error"] = "Tipo de documento no encontrado"; return RedirectToPage(); }
            if (t.EsSistema) { TempData["Error"] = "Los tipos que vienen con el sistema no se eliminan (puedes desactivarlos)."; return RedirectToPage(); }
            if (_context.AdjuntosEmpleado.Any(a => a.TipoDocumentoId == id))
            { TempData["Error"] = "No se puede eliminar: hay documentos de empleados de este tipo. Mejor desactívalo."; return RedirectToPage(); }
            var nombre = t.Nombre;
            _context.TiposDocumentoEmpleado.Remove(t);
            _context.SaveChanges();
            Registrar("Eliminar tipo de documento", nombre);
            TempData["Success"] = $"Tipo \"{nombre}\" eliminado";
            return RedirectToPage();
        }

        private void Registrar(string accion, string detalle)
        {
            var u = _context.Usuarios.FirstOrDefault(x => x.Username == User.Identity!.Name);
            ActividadHelper.Registrar(_context, u?.Id ?? 0, accion, detalle, HttpContext.Connection.RemoteIpAddress?.ToString());
        }
    }
}
