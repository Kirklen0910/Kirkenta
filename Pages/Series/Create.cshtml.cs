using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Kirkenta.Pages.Series
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

        public class InputModel
        {
            [Required(ErrorMessage = "El tipo es obligatorio")]
            public string Tipo { get; set; } = string.Empty;

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(50)]
            public string Nombre { get; set; } = "Principal";

            [Required(ErrorMessage = "El prefijo es obligatorio")]
            [StringLength(20)]
            public string Prefijo { get; set; } = string.Empty;

            [StringLength(20)]
            public string? Sufijo { get; set; }

            [StringLength(5)]
            public string Separador { get; set; } = "-";

            [Range(1, 10)]
            public int LongitudNumero { get; set; } = 4;

            [Range(1, int.MaxValue)]
            public int SiguienteNumero { get; set; } = 1;

            [StringLength(100)]
            public string? FormatoPersonalizado { get; set; }

            public bool EsPredeterminada { get; set; } = false;
            public bool Activa { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores pueden crear series";
                return RedirectToPage("/Series/Index");
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";
            if (currentRol != "Admin")
            {
                TempData["Error"] = "Solo administradores pueden crear series";
                return RedirectToPage("/Series/Index");
            }

            if (!ModelState.IsValid) return Page();

            var serie = new SerieDocumento
            {
                Tipo = Input.Tipo,
                Nombre = Input.Nombre,
                Prefijo = Input.Prefijo,
                Sufijo = Input.Sufijo,
                Separador = Input.Separador,
                LongitudNumero = Input.LongitudNumero,
                SiguienteNumero = Input.SiguienteNumero,
                FormatoPersonalizado = Input.FormatoPersonalizado,
                EsPredeterminada = Input.EsPredeterminada,
                Activa = Input.Activa,
                FechaCreacion = DateTime.Now
            };

            // Si es predeterminada, quitar la predeterminada anterior del mismo tipo
            if (serie.EsPredeterminada)
            {
                var anteriores = _context.SeriesDocumentos
                    .Where(s => s.Tipo == serie.Tipo && s.EsPredeterminada)
                    .ToList();
                foreach (var s in anteriores) s.EsPredeterminada = false;
            }

            _context.SeriesDocumentos.Add(serie);
            _context.SaveChanges();

            ActividadHelper.Registrar(
                _context,
                currentUser?.Id ?? 0,
                "Crear serie",
                $"Creó la serie '{serie.Nombre}' para {serie.Tipo} con prefijo '{serie.Prefijo}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Serie '{serie.Nombre}' creada correctamente";
            return RedirectToPage("/Series/Index");
        }
    }
}