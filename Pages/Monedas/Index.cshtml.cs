using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Monedas
{
    /// <summary>Catálogo de monedas: crear, editar, activar/desactivar, elegir la predeterminada y eliminar las que no se usan.</summary>
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) { _context = context; }

        public List<Moneda> Monedas { get; set; } = new();
        public HashSet<int> EnUso { get; set; } = new();
        public bool PuedeCrear { get; private set; }
        public bool PuedeEditar { get; private set; }
        public bool PuedeEliminar { get; private set; }

        [BindProperty] public MonedaInput Input { get; set; } = new();

        public class MonedaInput
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El código es obligatorio (3 letras, ej. USD)")]
            [StringLength(3, MinimumLength = 3, ErrorMessage = "El código debe tener 3 letras")]
            public string Codigo { get; set; } = "";

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(50)]
            public string Nombre { get; set; } = "";

            [StringLength(5)]
            public string? Simbolo { get; set; }

            [Range(0.0001, 100000000, ErrorMessage = "El tipo de cambio debe ser mayor que 0")]
            public decimal TipoCambio { get; set; } = 1;

            public bool EsPredeterminada { get; set; }
            public bool Activa { get; set; } = true;
        }

        private const string Modulo = "Configuracion";
        private const string Sub = "Monedas";

        private bool Cargar()
        {
            var usr = User.Identity?.Name;
            if (!PermisoHelper.UsuarioTieneAcceso(_context, usr, Modulo, Sub)) return false;
            PuedeCrear = PermisoHelper.UsuarioTienePermiso(_context, usr, Modulo, Sub, "crear");
            PuedeEditar = PermisoHelper.UsuarioTienePermiso(_context, usr, Modulo, Sub, "editar");
            PuedeEliminar = PermisoHelper.UsuarioTienePermiso(_context, usr, Modulo, Sub, "eliminar");
            Monedas = _context.Monedas.OrderByDescending(m => m.EsPredeterminada).ThenBy(m => m.Codigo).ToList();
            foreach (var m in Monedas) if (EstaEnUso(m.Id)) EnUso.Add(m.Id);
            return true;
        }

        private bool EstaEnUso(int id) =>
            _context.Proveedores.Any(x => x.MonedaId == id)
            || _context.OrdenesCompra.Any(x => x.MonedaId == id)
            || _context.CuentasFinancieras.Any(x => x.MonedaId == id)
            || _context.PagosProveedor.Any(x => x.MonedaId == id)
            || _context.MovimientosFinancieros.Any(x => x.MonedaId == id);

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
                var m = _context.Monedas.FirstOrDefault(x => x.Id == editar.Value);
                if (m != null)
                    Input = new MonedaInput { Id = m.Id, Codigo = m.Codigo, Nombre = m.Nombre, Simbolo = m.Simbolo, TipoCambio = m.TipoCambio, EsPredeterminada = m.EsPredeterminada, Activa = m.Activa };
            }
            return Page();
        }

        public IActionResult OnPostGuardar()
        {
            if (!Cargar()) return SinPermiso();
            var esNueva = Input.Id == 0;
            if (esNueva ? !PuedeCrear : !PuedeEditar) return SinPermiso();
            if (!ModelState.IsValid) return Page();

            var codigo = (Input.Codigo ?? "").Trim().ToUpperInvariant();
            if (!Regex.IsMatch(codigo, "^[A-Z]{3}$"))
            {
                ModelState.AddModelError("", "El código debe ser de 3 letras (ej. HNL, USD, EUR).");
                return Page();
            }
            if (_context.Monedas.Any(m => m.Codigo == codigo && m.Id != Input.Id))
            {
                ModelState.AddModelError("", $"Ya existe una moneda con el código {codigo}.");
                return Page();
            }

            Moneda? moneda;
            if (esNueva) moneda = new Moneda();
            else
            {
                moneda = _context.Monedas.FirstOrDefault(m => m.Id == Input.Id);
                if (moneda == null) { TempData["Error"] = "Moneda no encontrada"; return RedirectToPage(); }
                if (!moneda.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase) && EstaEnUso(moneda.Id))
                {
                    ModelState.AddModelError("", "No se puede cambiar el código de una moneda que ya se usa en documentos. Crea otra moneda si hace falta.");
                    return Page();
                }
                if (moneda.EsPredeterminada && !Input.EsPredeterminada)
                {
                    ModelState.AddModelError("", "Siempre debe haber una moneda predeterminada: marca otra como predeterminada en lugar de quitar esta.");
                    return Page();
                }
            }

            var hayPredeterminada = _context.Monedas.Any(m => m.EsPredeterminada && m.Id != Input.Id);
            var serPredeterminada = Input.EsPredeterminada || !hayPredeterminada;

            moneda.Codigo = codigo;
            moneda.Nombre = Input.Nombre.Trim();
            moneda.Simbolo = string.IsNullOrWhiteSpace(Input.Simbolo) ? null : Input.Simbolo.Trim();
            moneda.TipoCambio = serPredeterminada ? 1m : Input.TipoCambio;
            moneda.Activa = serPredeterminada ? true : Input.Activa;
            moneda.EsPredeterminada = serPredeterminada;
            moneda.FechaActualizacion = DateTime.Now;

            if (serPredeterminada)
                foreach (var otra in _context.Monedas.Where(m => m.EsPredeterminada && m.Id != Input.Id).ToList())
                    otra.EsPredeterminada = false;

            if (esNueva) _context.Monedas.Add(moneda);
            _context.SaveChanges();

            Registrar(esNueva ? "Crear moneda" : "Editar moneda", $"{moneda.Codigo} - {moneda.Nombre} (cambio {moneda.TipoCambio:N4})");
            TempData["Success"] = esNueva ? $"Moneda {moneda.Codigo} creada" : $"Moneda {moneda.Codigo} actualizada";
            return RedirectToPage();
        }

        public IActionResult OnPostEstado(int id)
        {
            if (!Cargar() || !PuedeEditar) return SinPermiso();
            var m = _context.Monedas.FirstOrDefault(x => x.Id == id);
            if (m == null) { TempData["Error"] = "Moneda no encontrada"; return RedirectToPage(); }
            if (m.EsPredeterminada) { TempData["Error"] = "La moneda predeterminada no se puede desactivar."; return RedirectToPage(); }
            m.Activa = !m.Activa;
            m.FechaActualizacion = DateTime.Now;
            _context.SaveChanges();
            Registrar(m.Activa ? "Activar moneda" : "Desactivar moneda", m.Codigo);
            TempData["Success"] = $"Moneda {m.Codigo} {(m.Activa ? "activada" : "desactivada")}";
            return RedirectToPage();
        }

        public IActionResult OnPostEliminar(int id)
        {
            if (!Cargar() || !PuedeEliminar) return SinPermiso();
            var m = _context.Monedas.FirstOrDefault(x => x.Id == id);
            if (m == null) { TempData["Error"] = "Moneda no encontrada"; return RedirectToPage(); }
            if (m.EsPredeterminada || m.Id == 1) { TempData["Error"] = "La moneda predeterminada (o la base del sistema) no se puede eliminar."; return RedirectToPage(); }
            if (EstaEnUso(m.Id)) { TempData["Error"] = "No se puede eliminar: ya se usa en documentos. Mejor desactívala."; return RedirectToPage(); }
            var cod = m.Codigo;
            _context.Monedas.Remove(m);
            _context.SaveChanges();
            Registrar("Eliminar moneda", cod);
            TempData["Success"] = $"Moneda {cod} eliminada";
            return RedirectToPage();
        }

        private void Registrar(string accion, string detalle)
        {
            var u = _context.Usuarios.FirstOrDefault(x => x.Username == User.Identity!.Name);
            ActividadHelper.Registrar(_context, u?.Id ?? 0, accion, detalle, HttpContext.Connection.RemoteIpAddress?.ToString());
        }
    }
}
