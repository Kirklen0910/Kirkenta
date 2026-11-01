using System.ComponentModel.DataAnnotations;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Activos
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(150)]
            public string Nombre { get; set; } = "";

            [StringLength(500)]
            public string? Descripcion { get; set; }

            [Required]
            public string Categoria { get; set; } = "Mobiliario";

            public DateTime FechaAdquisicion { get; set; } = DateTime.Today;

            [Range(0.01, 999999999, ErrorMessage = "El costo debe ser mayor a 0")]
            public decimal CostoAdquisicion { get; set; }

            [Range(0, 999999999, ErrorMessage = "El valor residual no es válido")]
            public decimal ValorResidual { get; set; }

            [Range(1, 600, ErrorMessage = "La vida útil debe estar entre 1 y 600 meses")]
            public int VidaUtilMeses { get; set; } = 60;

            [StringLength(100)] public string? Ubicacion { get; set; }
            [StringLength(100)] public string? Responsable { get; set; }
            [StringLength(80)] public string? NumeroSerie { get; set; }
        }

        [BindProperty] public InputModel Input { get; set; } = new();
        public string Codigo { get; set; } = "";
        public bool EsNuevo => Input.Id == 0;
        public bool FinancieroBloqueado { get; set; }

        private bool Autorizado(string accion) =>
            PermisoHelper.UsuarioTienePermiso(_context, User.Identity?.Name, "Activos", "Index", accion);

        public IActionResult OnGet(int? id)
        {
            if (id.HasValue)
            {
                if (!Autorizado("editar")) return Denegar();
                var a = _context.ActivosFijos.FirstOrDefault(x => x.Id == id.Value);
                if (a == null) { TempData["Error"] = "Activo no encontrado"; return RedirectToPage("/Activos/Index"); }
                if (a.Estado != "Activo") { TempData["Error"] = "Un activo dado de baja no se puede editar"; return RedirectToPage("/Activos/Details", new { id = a.Id }); }

                Codigo = a.Codigo;
                FinancieroBloqueado = a.DepreciacionAcumulada > 0;
                Input = new InputModel
                {
                    Id = a.Id, Nombre = a.Nombre, Descripcion = a.Descripcion, Categoria = a.Categoria,
                    FechaAdquisicion = a.FechaAdquisicion, CostoAdquisicion = a.CostoAdquisicion,
                    ValorResidual = a.ValorResidual, VidaUtilMeses = a.VidaUtilMeses,
                    Ubicacion = a.Ubicacion, Responsable = a.Responsable, NumeroSerie = a.NumeroSerie
                };
            }
            else
            {
                if (!Autorizado("crear")) return Denegar();
                Codigo = NumeroDocumentoHelper.PreviewSiguiente(_context, "ActivoFijo");
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!Autorizado(Input.Id == 0 ? "crear" : "editar")) return Denegar();

            if (!Kirkenta.Pages.Activos.IndexModel.Categorias.Contains(Input.Categoria))
                ModelState.AddModelError("Input.Categoria", "Categoría no válida");
            if (Input.ValorResidual >= Input.CostoAdquisicion)
                ModelState.AddModelError("Input.ValorResidual", "El valor residual debe ser menor al costo");

            ActivoFijo? existente = null;
            if (Input.Id != 0)
            {
                existente = _context.ActivosFijos.FirstOrDefault(x => x.Id == Input.Id);
                if (existente == null) { TempData["Error"] = "Activo no encontrado"; return RedirectToPage("/Activos/Index"); }
                if (existente.Estado != "Activo") { TempData["Error"] = "Un activo dado de baja no se puede editar"; return RedirectToPage("/Activos/Index"); }
                Codigo = existente.Codigo;
                FinancieroBloqueado = existente.DepreciacionAcumulada > 0;
                if (FinancieroBloqueado)
                {
                    // Datos financieros congelados: ya hay depreciación registrada
                    Input.CostoAdquisicion = existente.CostoAdquisicion;
                    Input.ValorResidual = existente.ValorResidual;
                    Input.VidaUtilMeses = existente.VidaUtilMeses;
                    Input.FechaAdquisicion = existente.FechaAdquisicion;
                    ModelState.Remove("Input.CostoAdquisicion");
                    ModelState.Remove("Input.ValorResidual");
                    ModelState.Remove("Input.VidaUtilMeses");
                    ModelState.Remove("Input.FechaAdquisicion");
                }
            }
            else
            {
                Codigo = NumeroDocumentoHelper.PreviewSiguiente(_context, "ActivoFijo");
            }

            if (!ModelState.IsValid) return Page();

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);

            if (existente == null)
            {
                var activo = new ActivoFijo
                {
                    Codigo = NumeroDocumentoHelper.GenerarSiguiente(_context, "ActivoFijo"),
                    Nombre = Input.Nombre.Trim(),
                    Descripcion = Input.Descripcion,
                    Categoria = Input.Categoria,
                    FechaAdquisicion = Input.FechaAdquisicion.Date,
                    CostoAdquisicion = Input.CostoAdquisicion,
                    ValorResidual = Input.ValorResidual,
                    VidaUtilMeses = Input.VidaUtilMeses,
                    Ubicacion = Input.Ubicacion,
                    Responsable = Input.Responsable,
                    NumeroSerie = Input.NumeroSerie,
                    Estado = "Activo"
                };
                _context.ActivosFijos.Add(activo);
                _context.SaveChanges();
                ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Crear activo fijo",
                    $"Registró el activo {activo.Codigo} - {activo.Nombre} por L. {activo.CostoAdquisicion:N2}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
                TempData["Success"] = $"Activo {activo.Codigo} registrado";
                return RedirectToPage("/Activos/Details", new { id = activo.Id });
            }

            existente.Nombre = Input.Nombre.Trim();
            existente.Descripcion = Input.Descripcion;
            existente.Categoria = Input.Categoria;
            existente.FechaAdquisicion = Input.FechaAdquisicion.Date;
            existente.CostoAdquisicion = Input.CostoAdquisicion;
            existente.ValorResidual = Input.ValorResidual;
            existente.VidaUtilMeses = Input.VidaUtilMeses;
            existente.Ubicacion = Input.Ubicacion;
            existente.Responsable = Input.Responsable;
            existente.NumeroSerie = Input.NumeroSerie;
            _context.SaveChanges();

            ActividadHelper.Registrar(_context, usuario?.Id ?? 0, "Editar activo fijo",
                $"Editó el activo {existente.Codigo} - {existente.Nombre}",
                HttpContext.Connection.RemoteIpAddress?.ToString());
            TempData["Success"] = $"Activo {existente.Codigo} actualizado";
            return RedirectToPage("/Activos/Details", new { id = existente.Id });
        }

        private IActionResult Denegar()
        {
            TempData["Error"] = "No tienes permiso para esta acción";
            return RedirectToPage("/Activos/Index");
        }
    }
}
