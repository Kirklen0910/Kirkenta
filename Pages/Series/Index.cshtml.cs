using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Pages.Series
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<SerieDocumento> Series { get; set; } = new();
        public Dictionary<int, string> Previews { get; set; } = new();
        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }


        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            PuedeCrear = PermisoHelper.TienePermiso(_context, currentRol, "Configuracion", "Series", "crear");
            PuedeEditar = PermisoHelper.TienePermiso(_context, currentRol, "Configuracion", "Series", "editar");
            PuedeEliminar = PermisoHelper.TienePermiso(_context, currentRol, "Configuracion", "Series", "eliminar");
            if (!PermisoHelper.TienePermiso(_context, currentRol, "Configuracion", "Series", "ver"))
            {
                TempData["Error"] = "No tienes permiso para ver esta sección";
                return RedirectToPage("/Index");
            }

            Series = _context.SeriesDocumentos.OrderBy(s => s.Tipo).ToList();

            foreach (var serie in Series)
            {
                var numero = serie.SiguienteNumero.ToString().PadLeft(serie.LongitudNumero, '0');
                var prefijo = serie.Prefijo ?? "";
                var sufijo = serie.Sufijo ?? "";
                var sep = serie.Separador ?? "-";
                var anio = DateTime.Now.Year.ToString();
                var mes = DateTime.Now.Month.ToString("D2");
                var dia = DateTime.Now.Day.ToString("D2");

                string preview;
                if (!string.IsNullOrWhiteSpace(serie.FormatoPersonalizado))
                {
                    preview = serie.FormatoPersonalizado
                        .Replace("{PREFIX}", prefijo)
                        .Replace("{SUFFIX}", sufijo)
                        .Replace("{SEP}", sep)
                        .Replace("{YEAR}", anio)
                        .Replace("{MONTH}", mes)
                        .Replace("{DAY}", dia)
                        .Replace("{NUM}", numero);
                }
                else
                {
                    preview = prefijo + sep + numero;
                }

                Previews[serie.Id] = preview;
            }

            return Page();
        }
    }
}